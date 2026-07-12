using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxClaimLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "claim_id",
                schema: "identity",
                table: "outbox_message",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "claimed_until",
                schema: "identity",
                table: "outbox_message",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_claimable",
                schema: "identity",
                table: "outbox_message",
                columns: new[] { "claimed_until", "next_attempt_at", "occurred_at" },
                filter: "processed_at IS NULL AND dead_lettered_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_claimable",
                schema: "identity",
                table: "outbox_message");

            migrationBuilder.DropColumn(
                name: "claim_id",
                schema: "identity",
                table: "outbox_message");

            migrationBuilder.DropColumn(
                name: "claimed_until",
                schema: "identity",
                table: "outbox_message");
        }
    }
}
