using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuestAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxReplayAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_replay_operation",
                schema: "guest_access",
                columns: table => new
                {
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    replayed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    replayed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_replay_operation", x => x.operation_id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_replay_audit",
                schema: "guest_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    dead_lettered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    error_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    replayed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_replay_audit", x => x.id);
                    table.ForeignKey(
                        name: "fk_outbox_replay_audit_outbox_replay_operation_operation_id",
                        column: x => x.operation_id,
                        principalSchema: "guest_access",
                        principalTable: "outbox_replay_operation",
                        principalColumn: "operation_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_dead_letter",
                schema: "guest_access",
                table: "outbox_message",
                column: "dead_lettered_at",
                filter: "dead_lettered_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_replay_message_time",
                schema: "guest_access",
                table: "outbox_replay_audit",
                columns: new[] { "message_id", "replayed_at" });

            migrationBuilder.CreateIndex(
                name: "ux_outbox_replay_operation_message",
                schema: "guest_access",
                table: "outbox_replay_audit",
                columns: new[] { "operation_id", "message_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_replay_operation_time",
                schema: "guest_access",
                table: "outbox_replay_operation",
                column: "replayed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_replay_audit",
                schema: "guest_access");

            migrationBuilder.DropTable(
                name: "outbox_replay_operation",
                schema: "guest_access");

            migrationBuilder.DropIndex(
                name: "ix_outbox_dead_letter",
                schema: "guest_access",
                table: "outbox_message");
        }
    }
}
