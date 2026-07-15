using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rules.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleSetResortUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_rule_set_resort",
                schema: "rules",
                table: "rule_set");

            migrationBuilder.CreateIndex(
                name: "ux_rule_set_resort",
                schema: "rules",
                table: "rule_set",
                column: "resort_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_rule_set_resort",
                schema: "rules",
                table: "rule_set");

            migrationBuilder.CreateIndex(
                name: "ix_rule_set_resort",
                schema: "rules",
                table: "rule_set",
                column: "resort_id");
        }
    }
}
