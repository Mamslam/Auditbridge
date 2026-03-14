using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditBridge.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Sprint8MobileFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // GPS on findings
        migrationBuilder.AddColumn<double>(
            name: "latitude",
            table: "audit_findings",
            type: "double precision",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "longitude",
            table: "audit_findings",
            type: "double precision",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "location_name",
            table: "audit_findings",
            type: "varchar(255)",
            nullable: true);

        // Signatures on reports
        migrationBuilder.AddColumn<string>(
            name: "auditor_signature_data",
            table: "audit_reports",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "signed_by_auditor_at",
            table: "audit_reports",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "auditee_signature_data",
            table: "audit_reports",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "signed_by_auditee_at",
            table: "audit_reports",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "latitude",       table: "audit_findings");
        migrationBuilder.DropColumn(name: "longitude",      table: "audit_findings");
        migrationBuilder.DropColumn(name: "location_name",  table: "audit_findings");

        migrationBuilder.DropColumn(name: "auditor_signature_data",  table: "audit_reports");
        migrationBuilder.DropColumn(name: "signed_by_auditor_at",    table: "audit_reports");
        migrationBuilder.DropColumn(name: "auditee_signature_data",  table: "audit_reports");
        migrationBuilder.DropColumn(name: "signed_by_auditee_at",    table: "audit_reports");
    }
}
