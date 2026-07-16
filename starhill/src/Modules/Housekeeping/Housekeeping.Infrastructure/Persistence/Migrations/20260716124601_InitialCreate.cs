using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Housekeeping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "housekeeping");

            migrationBuilder.CreateTable(
                name: "housekeeping_ticket",
                schema: "housekeeping",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resort_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_guest_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_visit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completion_method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_housekeeping_ticket", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "housekeeping_event",
                schema: "housekeeping",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    housekeeping_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    new_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    actor_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_housekeeping_event", x => x.id);
                    table.ForeignKey(
                        name: "fk_housekeeping_event_housekeeping_tickets_housekeeping_ticket",
                        column: x => x.housekeeping_ticket_id,
                        principalSchema: "housekeeping",
                        principalTable: "housekeeping_ticket",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_hk_event_ticket",
                schema: "housekeeping",
                table: "housekeeping_event",
                columns: new[] { "housekeeping_ticket_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_hk_ticket_resort_status",
                schema: "housekeeping",
                table: "housekeeping_ticket",
                columns: new[] { "resort_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_hk_open_ticket_room",
                schema: "housekeeping",
                table: "housekeeping_ticket",
                column: "room_id",
                unique: true,
                filter: "status IN ('Requested','InProgress')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "housekeeping_event",
                schema: "housekeeping");

            migrationBuilder.DropTable(
                name: "housekeeping_ticket",
                schema: "housekeeping");
        }
    }
}
