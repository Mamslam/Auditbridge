using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3FindingsEvidenceCapa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── audit_findings ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "audit_findings",
                columns: table => new
                {
                    id              = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_id        = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id     = table.Column<Guid>(type: "uuid", nullable: true),
                    response_id     = table.Column<Guid>(type: "uuid", nullable: true),
                    finding_type    = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title           = table.Column<string>(type: "text", nullable: false),
                    description     = table.Column<string>(type: "text", nullable: true),
                    observed_evidence = table.Column<string>(type: "text", nullable: true),
                    regulatory_ref  = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status          = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "open"),
                    created_at      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_findings", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_findings_audits_audit_id",
                        column: x => x.audit_id,
                        principalTable: "audits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_findings_audit_id",
                table: "audit_findings",
                column: "audit_id");

            // ── audit_evidence ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "audit_evidence",
                columns: table => new
                {
                    id              = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_id        = table.Column<Guid>(type: "uuid", nullable: false),
                    finding_id      = table.Column<Guid>(type: "uuid", nullable: true),
                    response_id     = table.Column<Guid>(type: "uuid", nullable: true),
                    capa_id         = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_by     = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name       = table.Column<string>(type: "text", nullable: false),
                    storage_path    = table.Column<string>(type: "text", nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    mime_type       = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description     = table.Column<string>(type: "text", nullable: true),
                    created_at      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_evidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_evidence_audits_audit_id",
                        column: x => x.audit_id,
                        principalTable: "audits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_audit_evidence_audit_findings_finding_id",
                        column: x => x.finding_id,
                        principalTable: "audit_findings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_evidence_audit_id",
                table: "audit_evidence",
                column: "audit_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_evidence_finding_id",
                table: "audit_evidence",
                column: "finding_id");

            // ── audit_capas: add finding_id column ────────────────────────────
            migrationBuilder.AddColumn<Guid>(
                name: "finding_id",
                table: "audit_capas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_capas_finding_id",
                table: "audit_capas",
                column: "finding_id");

            migrationBuilder.AddForeignKey(
                name: "FK_audit_capas_audit_findings_finding_id",
                table: "audit_capas",
                column: "finding_id",
                principalTable: "audit_findings",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // ── audit_capas: expand status column to 30 chars ─────────────────
            // (pending_verification is 23 chars — current constraint is 20)
            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "audit_capas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_audit_capas_audit_findings_finding_id", "audit_capas");
            migrationBuilder.DropIndex("IX_audit_capas_finding_id", "audit_capas");
            migrationBuilder.DropColumn("finding_id", "audit_capas");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "audit_capas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.DropTable("audit_evidence");
            migrationBuilder.DropTable("audit_findings");
        }
    }
}
