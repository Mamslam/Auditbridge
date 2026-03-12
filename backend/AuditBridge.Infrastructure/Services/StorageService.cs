using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AuditBridge.Infrastructure.Services;

/// <summary>
/// Integrates with Supabase Storage for evidence file management.
///
/// Bucket: "audit-evidence"
/// Path convention: {orgId}/{auditId}/{uuid}/{filename}
///
/// Config keys:
///   Supabase:Url             — e.g. https://xxx.supabase.co
///   Supabase:ServiceRoleKey  — service role JWT
/// </summary>
public class StorageService(IConfiguration config, IHttpClientFactory httpClientFactory)
{
    private const string Bucket = "audit-evidence";

    private string BaseUrl => config["Supabase:Url"]
        ?? throw new InvalidOperationException("Supabase:Url not configured.");
    private string ServiceKey => config["Supabase:ServiceRoleKey"]
        ?? throw new InvalidOperationException("Supabase:ServiceRoleKey not configured.");

    /// <summary>
    /// Generate a signed URL for a direct client upload (PUT).
    /// Expires in 10 minutes.
    /// </summary>
    public async Task<SignedUploadUrl> GetSignedUploadUrlAsync(
        Guid orgId, Guid auditId, string fileName, CancellationToken ct = default)
    {
        var fileId = Guid.NewGuid().ToString("N");
        var storagePath = $"{orgId}/{auditId}/{fileId}/{SanitizeFileName(fileName)}";

        var http = httpClientFactory.CreateClient("supabase");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ServiceKey);
        http.DefaultRequestHeaders.Add("apikey", ServiceKey);

        // Supabase signed URL for upload: POST /storage/v1/object/sign/{bucket}/{path}
        var requestUrl = $"{BaseUrl}/storage/v1/object/sign/{Bucket}/{storagePath}";
        var body = JsonSerializer.Serialize(new { expiresIn = 600 }); // 10 min
        var response = await http.PostAsync(requestUrl,
            new StringContent(body, Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Supabase Storage signing failed ({response.StatusCode}): {err}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var signedUrl = doc.RootElement.GetProperty("signedURL").GetString()!;

        return new SignedUploadUrl(
            SignedUrl: $"{BaseUrl}{signedUrl}",
            StoragePath: storagePath,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(10));
    }

    /// <summary>
    /// Generate a short-lived signed URL for downloading a file (GET).
    /// Expires in 1 hour.
    /// </summary>
    public async Task<string> GetSignedDownloadUrlAsync(string storagePath, CancellationToken ct = default)
    {
        var http = httpClientFactory.CreateClient("supabase");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ServiceKey);
        http.DefaultRequestHeaders.Add("apikey", ServiceKey);

        var requestUrl = $"{BaseUrl}/storage/v1/object/sign/{Bucket}/{storagePath}";
        var body = JsonSerializer.Serialize(new { expiresIn = 3600 }); // 1 hour
        var response = await http.PostAsync(requestUrl,
            new StringContent(body, Encoding.UTF8, "application/json"), ct);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var signedUrl = doc.RootElement.GetProperty("signedURL").GetString()!;
        return $"{BaseUrl}{signedUrl}";
    }

    /// <summary>
    /// Delete a file from storage. Non-throwing — logs warning if delete fails.
    /// </summary>
    public async Task<bool> DeleteFileAsync(string storagePath, CancellationToken ct = default)
    {
        try
        {
            var http = httpClientFactory.CreateClient("supabase");
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ServiceKey);
            http.DefaultRequestHeaders.Add("apikey", ServiceKey);

            var requestUrl = $"{BaseUrl}/storage/v1/object/{Bucket}/{storagePath}";
            var response = await http.DeleteAsync(requestUrl, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private static string SanitizeFileName(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        var name = Path.GetFileNameWithoutExtension(fileName);
        // Remove special chars, keep alphanumeric, dots, dashes
        var safe = new string(name.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return $"{safe}{ext}".TrimStart('.');
    }
}

public record SignedUploadUrl(string SignedUrl, string StoragePath, DateTimeOffset ExpiresAt);
