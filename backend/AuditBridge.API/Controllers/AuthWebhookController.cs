using AuditBridge.Application.DTOs;
using AuditBridge.Application.UseCases.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Svix;
using System.Text;

namespace AuditBridge.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthWebhookController(
    SyncClerkUserUseCase syncClerkUser,
    IConfiguration configuration,
    ILogger<AuthWebhookController> logger) : ControllerBase
{
    /// <summary>Clerk webhook — synchronizes user events to the database.</summary>
    [HttpPost("webhook-clerk")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ClerkWebhook(CancellationToken ct)
    {
        // Verify Svix signature
        var webhookSecret = configuration["Clerk:WebhookSecret"]
            ?? throw new InvalidOperationException("Clerk webhook secret not configured.");

        var svixId = Request.Headers["svix-id"].FirstOrDefault();
        var svixTimestamp = Request.Headers["svix-timestamp"].FirstOrDefault();
        var svixSignature = Request.Headers["svix-signature"].FirstOrDefault();

        if (svixId is null || svixTimestamp is null || svixSignature is null)
            return BadRequest(new { message = "Missing Svix headers." });

        string payload;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            payload = await reader.ReadToEndAsync(ct);
        }

        // Validate webhook signature
        try
        {
            var wh = new Webhook(webhookSecret);
            var headers = new System.Net.WebHeaderCollection
            {
                { "svix-id", svixId },
                { "svix-timestamp", svixTimestamp },
                { "svix-signature", svixSignature },
            };
            wh.Verify(payload, headers);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Clerk webhook signature verification failed: {Message}", ex.Message);
            return BadRequest(new { message = "Invalid webhook signature." });
        }

        // Parse event type
        using var doc = System.Text.Json.JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var eventType = root.GetProperty("type").GetString() ?? "";
        var data = root.GetProperty("data");

        var clerkId = data.GetProperty("id").GetString() ?? "";
        var emailAddresses = data.GetProperty("email_addresses");
        var primaryEmail = emailAddresses.EnumerateArray().FirstOrDefault().GetProperty("email_address").GetString() ?? "";
        var firstName = data.TryGetProperty("first_name", out var fn) ? fn.GetString() : null;
        var lastName = data.TryGetProperty("last_name", out var ln) ? ln.GetString() : null;
        var fullName = $"{firstName} {lastName}".Trim();

        await syncClerkUser.ExecuteAsync(new SyncClerkUserRequest(
            ClerkId: clerkId,
            Email: primaryEmail,
            FullName: string.IsNullOrEmpty(fullName) ? null : fullName,
            EventType: eventType), ct);

        return Ok(new { received = true });
    }
}
