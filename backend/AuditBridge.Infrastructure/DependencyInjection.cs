using AuditBridge.Application.UseCases.Organizations;
using AuditBridge.Application.UseCases.Users;
using AuditBridge.Domain.Interfaces;
using AuditBridge.Infrastructure.Middleware;
using AuditBridge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuditBridge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // HTTP Context Accessor for RLS interceptor
        services.AddHttpContextAccessor();

        // EF Core interceptor for RLS
        services.AddSingleton<RlsDbConnectionInterceptor>();

        // PostgreSQL via Supabase
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(3);
                npgsql.CommandTimeout(30);
            });
            options.AddInterceptors(sp.GetRequiredService<RlsDbConnectionInterceptor>());
        });

        // Unit of Work & Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application Use Cases
        services.AddScoped<CreateOrganizationUseCase>();
        services.AddScoped<GetOrganizationUseCase>();
        services.AddScoped<SyncClerkUserUseCase>();

        return services;
    }
}
