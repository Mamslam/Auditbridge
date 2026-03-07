using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace AuditBridge.Infrastructure.Middleware;

/// <summary>
/// EF Core interceptor that sets the PostgreSQL session variable
/// app.current_org_id on each connection, enabling RLS policies.
/// </summary>
public class RlsDbConnectionInterceptor(
    IHttpContextAccessor httpContextAccessor,
    ILogger<RlsDbConnectionInterceptor> logger) : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var orgId = httpContextAccessor.HttpContext?.Items["CurrentOrgId"] as Guid?;
        if (orgId is null) return;

        try
        {
            await using var cmd = connection.CreateCommand();
            // Use SET LOCAL so the setting is scoped to the current transaction
            cmd.CommandText = $"SET LOCAL app.current_org_id = '{orgId}'";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to set RLS org context for org {OrgId}", orgId);
        }
    }
}
