using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AuditBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_trail",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_trail", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    plan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "starter"),
                    stripe_customer_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    stripe_subscription_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "fr"),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "referential_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    color_hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    icon = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referential_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    clerk_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    role = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "referentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referentials", x => x.id);
                    table.ForeignKey(
                        name: "FK_referentials_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_referentials_referential_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "referential_categories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "audits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    referential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    auditor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_org_name = table.Column<string>(type: "text", nullable: false),
                    client_email = table.Column<string>(type: "text", nullable: false),
                    client_token = table.Column<string>(type: "text", nullable: true),
                    client_token_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    scope = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audits", x => x.id);
                    table.ForeignKey(
                        name: "FK_audits_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_audits_referentials_referential_id",
                        column: x => x.referential_id,
                        principalTable: "referentials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_audits_users_auditor_id",
                        column: x => x.auditor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    referential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "text", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_template_sections_referentials_referential_id",
                        column: x => x.referential_id,
                        principalTable: "referentials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_capas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_id = table.Column<Guid>(type: "uuid", nullable: true),
                    question_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    root_cause = table.Column<string>(type: "text", nullable: true),
                    action_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assigned_to_email = table.Column<string>(type: "text", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    evidence_path = table.Column<string>(type: "text", nullable: true),
                    ai_generated = table.Column<bool>(type: "boolean", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_capas", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_capas_audits_audit_id",
                        column: x => x.audit_id,
                        principalTable: "audits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    conformity_score = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    total_questions = table.Column<int>(type: "integer", nullable: true),
                    conform_count = table.Column<int>(type: "integer", nullable: true),
                    non_conform_count = table.Column<int>(type: "integer", nullable: true),
                    partial_count = table.Column<int>(type: "integer", nullable: true),
                    na_count = table.Column<int>(type: "integer", nullable: true),
                    critical_nc = table.Column<int>(type: "integer", nullable: false),
                    major_nc = table.Column<int>(type: "integer", nullable: false),
                    minor_nc = table.Column<int>(type: "integer", nullable: false),
                    executive_summary = table.Column<string>(type: "text", nullable: true),
                    ai_narrative = table.Column<string>(type: "text", nullable: true),
                    report_data = table.Column<string>(type: "jsonb", nullable: false),
                    pdf_storage_path = table.Column<string>(type: "text", nullable: true),
                    pdf_sha256 = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_reports_audits_audit_id",
                        column: x => x.audit_id,
                        principalTable: "audits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    referential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "text", nullable: true),
                    question = table.Column<string>(type: "text", nullable: false),
                    guidance = table.Column<string>(type: "text", nullable: true),
                    answer_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    answer_options = table.Column<string>(type: "jsonb", nullable: true),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criticality = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expected_evidence = table.Column<string[]>(type: "text[]", nullable: true),
                    tags = table.Column<string[]>(type: "text[]", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_template_questions_referentials_referential_id",
                        column: x => x.referential_id,
                        principalTable: "referentials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_template_questions_template_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "template_sections",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "audit_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answered_by = table.Column<Guid>(type: "uuid", nullable: true),
                    answered_by_client = table.Column<bool>(type: "boolean", nullable: false),
                    answer_value = table.Column<string>(type: "text", nullable: true),
                    answer_notes = table.Column<string>(type: "text", nullable: true),
                    conformity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    auditor_comment = table.Column<string>(type: "text", nullable: true),
                    is_flagged = table.Column<bool>(type: "boolean", nullable: false),
                    ai_analysis = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_responses", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_responses_audits_audit_id",
                        column: x => x.audit_id,
                        principalTable: "audits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_audit_responses_template_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "template_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_capas_audit_id",
                table: "audit_capas",
                column: "audit_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_reports_audit_id",
                table: "audit_reports",
                column: "audit_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_responses_audit_id",
                table: "audit_responses",
                column: "audit_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_responses_question_id",
                table: "audit_responses",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_audits_auditor_id",
                table: "audits",
                column: "auditor_id");

            migrationBuilder.CreateIndex(
                name: "IX_audits_org_id",
                table: "audits",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "IX_audits_referential_id",
                table: "audits",
                column: "referential_id");

            migrationBuilder.CreateIndex(
                name: "IX_referential_categories_slug",
                table: "referential_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_referentials_category_id",
                table: "referentials",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_referentials_OrganizationId",
                table: "referentials",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_template_questions_referential_id",
                table: "template_questions",
                column: "referential_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_questions_section_id",
                table: "template_questions",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_sections_referential_id",
                table: "template_sections",
                column: "referential_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_clerk_id",
                table: "users",
                column: "clerk_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_organization_id",
                table: "users",
                column: "organization_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_capas");

            migrationBuilder.DropTable(
                name: "audit_reports");

            migrationBuilder.DropTable(
                name: "audit_responses");

            migrationBuilder.DropTable(
                name: "audit_trail");

            migrationBuilder.DropTable(
                name: "audits");

            migrationBuilder.DropTable(
                name: "template_questions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "template_sections");

            migrationBuilder.DropTable(
                name: "referentials");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropTable(
                name: "referential_categories");
        }
    }
}
