using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Concierge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "concierge");

            migrationBuilder.CreateTable(
                name: "conversation",
                schema: "concierge",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resort_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    last_message_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_guest_message_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_staff_message_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    unread_for_staff = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversation", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "internal_note",
                schema: "concierge",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resort_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_internal_note", x => x.id);
                    table.ForeignKey(
                        name: "fk_internal_note_conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalSchema: "concierge",
                        principalTable: "conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "message",
                schema: "concierge",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    sender_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    body = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_by_staff_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    read_by_guest_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalSchema: "concierge",
                        principalTable: "conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_conversation_resort_room_last",
                schema: "concierge",
                table: "conversation",
                columns: new[] { "resort_id", "room_id", "last_message_at" });

            migrationBuilder.CreateIndex(
                name: "ux_conversation_visit",
                schema: "concierge",
                table: "conversation",
                column: "guest_visit_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_internal_note_conversation_id",
                schema: "concierge",
                table: "internal_note",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_note_resort_room",
                schema: "concierge",
                table: "internal_note",
                columns: new[] { "resort_id", "room_id" });

            migrationBuilder.CreateIndex(
                name: "ix_message_conversation",
                schema: "concierge",
                table: "message",
                columns: new[] { "conversation_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "internal_note",
                schema: "concierge");

            migrationBuilder.DropTable(
                name: "message",
                schema: "concierge");

            migrationBuilder.DropTable(
                name: "conversation",
                schema: "concierge");
        }
    }
}
