using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Housekeeping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxReplayAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "amenity_soap",
                schema: "housekeeping",
                table: "housekeeping_ticket",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "amenity_toothbrush",
                schema: "housekeeping",
                table: "housekeeping_ticket",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "amenity_towel",
                schema: "housekeeping",
                table: "housekeeping_ticket",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "amenity_water",
                schema: "housekeeping",
                table: "housekeeping_ticket",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "note",
                schema: "housekeeping",
                table: "housekeeping_ticket",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preferred_time",
                schema: "housekeeping",
                table: "housekeeping_ticket",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preferred_time_text",
                schema: "housekeeping",
                table: "housekeeping_ticket",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "service_type",
                schema: "housekeeping",
                table: "housekeeping_ticket",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "amenity_soap",
                schema: "housekeeping",
                table: "housekeeping_ticket");

            migrationBuilder.DropColumn(
                name: "amenity_toothbrush",
                schema: "housekeeping",
                table: "housekeeping_ticket");

            migrationBuilder.DropColumn(
                name: "amenity_towel",
                schema: "housekeeping",
                table: "housekeeping_ticket");

            migrationBuilder.DropColumn(
                name: "amenity_water",
                schema: "housekeeping",
                table: "housekeeping_ticket");

            migrationBuilder.DropColumn(
                name: "note",
                schema: "housekeeping",
                table: "housekeeping_ticket");

            migrationBuilder.DropColumn(
                name: "preferred_time",
                schema: "housekeeping",
                table: "housekeeping_ticket");

            migrationBuilder.DropColumn(
                name: "preferred_time_text",
                schema: "housekeeping",
                table: "housekeeping_ticket");

            migrationBuilder.DropColumn(
                name: "service_type",
                schema: "housekeeping",
                table: "housekeeping_ticket");
        }
    }
}
