using AuditBridge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditBridge.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ReferentialCategory> ReferentialCategories => Set<ReferentialCategory>();
    public DbSet<Referential> Referentials => Set<Referential>();
    public DbSet<TemplateSection> TemplateSections => Set<TemplateSection>();
    public DbSet<TemplateQuestion> TemplateQuestions => Set<TemplateQuestion>();
    public DbSet<Audit> Audits => Set<Audit>();
    public DbSet<AuditResponse> AuditResponses => Set<AuditResponse>();
    public DbSet<AuditCapa> AuditCapas => Set<AuditCapa>();
    public DbSet<AuditReport> AuditReports => Set<AuditReport>();
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
