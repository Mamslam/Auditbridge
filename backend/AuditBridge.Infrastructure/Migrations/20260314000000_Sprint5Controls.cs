using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint5Controls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── controls ───────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "controls",
                columns: table => new
                {
                    id          = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id      = table.Column<Guid>(type: "uuid", nullable: false),
                    code        = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title       = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category    = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    owner       = table.Column<string>(type: "text", nullable: true),
                    status      = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    created_at  = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at  = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_controls", x => x.id);
                    table.ForeignKey(
                        name: "FK_controls_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_controls_org_id_code",
                table: "controls",
                columns: new[] { "org_id", "code" },
                unique: true);

            // ── control_mappings ───────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "control_mappings",
                columns: table => new
                {
                    id              = table.Column<Guid>(type: "uuid", nullable: false),
                    control_id      = table.Column<Guid>(type: "uuid", nullable: false),
                    referential_id  = table.Column<Guid>(type: "uuid", nullable: false),
                    section_id      = table.Column<Guid>(type: "uuid", nullable: true),
                    question_id     = table.Column<Guid>(type: "uuid", nullable: true),
                    notes           = table.Column<string>(type: "text", nullable: true),
                    created_at      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_mappings", x => x.id);
                    table.ForeignKey(
                        name: "FK_control_mappings_controls_control_id",
                        column: x => x.control_id,
                        principalTable: "controls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_control_mappings_referentials_referential_id",
                        column: x => x.referential_id,
                        principalTable: "referentials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_control_mappings_control_id",
                table: "control_mappings",
                column: "control_id");

            migrationBuilder.CreateIndex(
                name: "IX_control_mappings_referential_id",
                table: "control_mappings",
                column: "referential_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("control_mappings");
            migrationBuilder.DropTable("controls");
        }
    }
}
