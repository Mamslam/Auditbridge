using AuditBridge.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuditBridge.Infrastructure.Middleware;

/// <summary>
/// Injects the current organization ID into each PostgreSQL session
/// so that RLS policies can filter data by tenant.
/// </summary>
public class TenantIsolationMiddleware(RequestDelegate next, ILogger<TenantIsolationMiddleware> logger)
{
    private const string OrgIdClaimType = "org_id"; // Custom claim set by Clerk

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip public routes, webhook endpoints, and onboarding
        if (IsPublicPath(context.Request.Path) || IsOnboardingPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Unauthorized" });
            return;
        }

        // Extract organization ID from JWT claims
        var orgIdClaim = context.User.Claims
            .FirstOrDefault(c => c.Type == OrgIdClaimType || c.Type == "organizationId");

        if (orgIdClaim is null || !Guid.TryParse(orgIdClaim.Value, out var orgId))
        {
            // If no org yet (during onboarding), allow through without RLS enforcement
            // Onboarding endpoint creates the org, so it cannot have one yet
            if (IsOnboardingPath(context.Request.Path))
            {
                await next(context);
                return;
            }

            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { message = "Organization not found for user." });
            return;
        }

        // Inject org_id into PostgreSQL session for RLS policies
        try
        {
            var unitOfWork = context.RequestServices.GetRequiredService<IUnitOfWork>();
            // Execute SET LOCAL for per-request isolation
            // This is handled by the DbContext interceptor registered in startup
            context.Items["CurrentOrgId"] = orgId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set tenant context for org {OrgId}", orgId);
            context.Response.StatusCode = 500;
            return;
        }

        await next(context);
    }

    private static bool IsPublicPath(PathString path)
    {
        var publicPaths = new[]
        {
            "/api/auth/webhook-clerk",
            "/api/billing/webhook",
            "/health",
            "/swagger",
        };
        return publicPaths.Any(p => path.StartsWithSegments(p));
    }

    private static bool IsOnboardingPath(PathString path)
        => path.StartsWithSegments("/api/organizations") &&
           !path.ToString().Contains("/me");
}
