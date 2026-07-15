using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rules.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rules");

            migrationBuilder.CreateTable(
                name: "rule_publication",
                schema: "rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resort_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    change_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_current = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rule_publication", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rule_set",
                schema: "rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resort_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rule_set", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rule_acknowledgement",
                schema: "rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resort_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_publication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    language_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rule_acknowledgement", x => x.id);
                    table.ForeignKey(
                        name: "fk_rule_acknowledgement_rule_publications_rule_publication_id",
                        column: x => x.rule_publication_id,
                        principalSchema: "rules",
                        principalTable: "rule_publication",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rule_publication_section",
                schema: "rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_publication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    require_scroll_end = table.Column<bool>(type: "boolean", nullable: false),
                    min_read_seconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rule_publication_section", x => x.id);
                    table.ForeignKey(
                        name: "fk_rule_publication_section_rule_publication_rule_publication_",
                        column: x => x.rule_publication_id,
                        principalSchema: "rules",
                        principalTable: "rule_publication",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rule_section",
                schema: "rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    require_scroll_end = table.Column<bool>(type: "boolean", nullable: false),
                    min_read_seconds = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rule_section", x => x.id);
                    table.ForeignKey(
                        name: "fk_rule_section_rule_sets_rule_set_id",
                        column: x => x.rule_set_id,
                        principalSchema: "rules",
                        principalTable: "rule_set",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rule_publication_section_translation",
                schema: "rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_publication_section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    body_html_sanitized = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rule_publication_section_translation", x => x.id);
                    table.ForeignKey(
                        name: "fk_rule_publication_section_translation_rule_publication_secti",
                        column: x => x.rule_publication_section_id,
                        principalSchema: "rules",
                        principalTable: "rule_publication_section",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rule_section_translation",
                schema: "rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    body_html_sanitized = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rule_section_translation", x => x.id);
                    table.ForeignKey(
                        name: "fk_rule_section_translation_rule_section_rule_section_id",
                        column: x => x.rule_section_id,
                        principalSchema: "rules",
                        principalTable: "rule_section",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rule_acknowledgement_rule_publication_id",
                schema: "rules",
                table: "rule_acknowledgement",
                column: "rule_publication_id");

            migrationBuilder.CreateIndex(
                name: "ux_rule_ack_visit_publication",
                schema: "rules",
                table: "rule_acknowledgement",
                columns: new[] { "guest_visit_id", "rule_publication_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rule_publication_resort_version",
                schema: "rules",
                table: "rule_publication",
                columns: new[] { "resort_id", "version" });

            migrationBuilder.CreateIndex(
                name: "ux_rule_publication_current",
                schema: "rules",
                table: "rule_publication",
                column: "resort_id",
                unique: true,
                filter: "is_current");

            migrationBuilder.CreateIndex(
                name: "ix_rule_publication_section_order",
                schema: "rules",
                table: "rule_publication_section",
                columns: new[] { "rule_publication_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_rule_pub_section_translation_lang",
                schema: "rules",
                table: "rule_publication_section_translation",
                columns: new[] { "rule_publication_section_id", "language_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rule_section_set_order",
                schema: "rules",
                table: "rule_section",
                columns: new[] { "rule_set_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_rule_section_translation_lang",
                schema: "rules",
                table: "rule_section_translation",
                columns: new[] { "rule_section_id", "language_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rule_set_resort",
                schema: "rules",
                table: "rule_set",
                column: "resort_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rule_acknowledgement",
                schema: "rules");

            migrationBuilder.DropTable(
                name: "rule_publication_section_translation",
                schema: "rules");

            migrationBuilder.DropTable(
                name: "rule_section_translation",
                schema: "rules");

            migrationBuilder.DropTable(
                name: "rule_publication_section",
                schema: "rules");

            migrationBuilder.DropTable(
                name: "rule_section",
                schema: "rules");

            migrationBuilder.DropTable(
                name: "rule_publication",
                schema: "rules");

            migrationBuilder.DropTable(
                name: "rule_set",
                schema: "rules");
        }
    }
}
