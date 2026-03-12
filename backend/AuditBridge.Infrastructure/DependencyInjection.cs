using AuditBridge.Application.UseCases.Organizations;
using AuditBridge.Application.UseCases.Users;
using AuditBridge.Domain.Interfaces;
using AuditBridge.Infrastructure.Middleware;
using AuditBridge.Infrastructure.Persistence;
using AuditBridge.Infrastructure.Seeds;
using AuditBridge.Infrastructure.Services;
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
        services.AddHttpContextAccessor();
        services.AddSingleton<RlsDbConnectionInterceptor>();

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

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<CreateOrganizationUseCase>();
        services.AddScoped<GetOrganizationUseCase>();
        services.AddScoped<SyncClerkUserUseCase>();

        services.AddScoped<ReferentialSeeder>();
        services.AddScoped<AiAnalysisService>();
        services.AddScoped<ReportService>();
        services.AddScoped<StorageService>();
        services.AddHttpClient("supabase");

        return services;
    }
}
