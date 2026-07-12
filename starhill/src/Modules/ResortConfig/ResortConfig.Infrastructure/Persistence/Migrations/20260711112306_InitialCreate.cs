using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResortConfig.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "resort_config");

            migrationBuilder.CreateTable(
                name: "resort",
                schema: "resort_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    logo_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resort", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resort_language",
                schema: "resort_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resort_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resort_language", x => x.id);
                    table.ForeignKey(
                        name: "fk_resort_language_resort_resort_id",
                        column: x => x.resort_id,
                        principalSchema: "resort_config",
                        principalTable: "resort",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "resort_settings",
                schema: "resort_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resort_id = table.Column<Guid>(type: "uuid", nullable: false),
                    faq_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    chat_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    housekeeping_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    require_rule_ack_for_faq = table.Column<bool>(type: "boolean", nullable: false),
                    require_rule_ack_for_chat = table.Column<bool>(type: "boolean", nullable: false),
                    require_rule_ack_for_housekeeping = table.Column<bool>(type: "boolean", nullable: false),
                    portal_window_minutes = table.Column<int>(type: "integer", nullable: false),
                    visit_idle_expiry_hours = table.Column<int>(type: "integer", nullable: false),
                    guest_web_base_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    max_message_length = table.Column<int>(type: "integer", nullable: false),
                    message_rate_limit_per_minute = table.Column<int>(type: "integer", nullable: false),
                    housekeeping_rate_limit_per_hour = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resort_settings", x => x.id);
                    table.CheckConstraint("ck_idle_expiry", "visit_idle_expiry_hours > 0");
                    table.CheckConstraint("ck_msg_len", "max_message_length > 0");
                    table.CheckConstraint("ck_portal_window", "portal_window_minutes > 0");
                    table.ForeignKey(
                        name: "fk_resort_settings_resort_resort_id",
                        column: x => x.resort_id,
                        principalSchema: "resort_config",
                        principalTable: "resort",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_lang_code",
                schema: "resort_config",
                table: "resort_language",
                columns: new[] { "resort_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_lang_default",
                schema: "resort_config",
                table: "resort_language",
                column: "resort_id",
                unique: true,
                filter: "is_default");

            migrationBuilder.CreateIndex(
                name: "ux_resort_settings_resort",
                schema: "resort_config",
                table: "resort_settings",
                column: "resort_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resort_language",
                schema: "resort_config");

            migrationBuilder.DropTable(
                name: "resort_settings",
                schema: "resort_config");

            migrationBuilder.DropTable(
                name: "resort",
                schema: "resort_config");
        }
    }
}
