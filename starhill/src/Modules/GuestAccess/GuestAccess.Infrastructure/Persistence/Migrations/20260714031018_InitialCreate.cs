using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "guest_access");

            migrationBuilder.CreateTable(
                name: "guest_session",
                schema: "guest_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_key_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    preferred_language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest_session", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guest_visit",
                schema: "guest_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resort_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest_visit", x => x.id);
                    table.CheckConstraint("ck_guest_visit_closed_at", "(status = 'Active' AND closed_at IS NULL) OR (status IN ('Closed', 'Expired') AND closed_at IS NOT NULL)");
                    table.CheckConstraint("ck_guest_visit_expiry_after_seen", "expires_at >= last_seen_at");
                    table.ForeignKey(
                        name: "fk_guest_visit_guest_session_guest_session_id",
                        column: x => x.guest_session_id,
                        principalSchema: "guest_access",
                        principalTable: "guest_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_guest_session_key_hash",
                schema: "guest_access",
                table: "guest_session",
                column: "session_key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guest_visit_active_expiry",
                schema: "guest_access",
                table: "guest_visit",
                column: "expires_at",
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ux_guest_visit_active",
                schema: "guest_access",
                table: "guest_visit",
                columns: new[] { "guest_session_id", "room_id" },
                unique: true,
                filter: "status = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guest_visit",
                schema: "guest_access");

            migrationBuilder.DropTable(
                name: "guest_session",
                schema: "guest_access");
        }
    }
}
