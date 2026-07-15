using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faq.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "faq");

            migrationBuilder.CreateTable(
                name: "faq_category",
                schema: "faq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resort_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_faq_category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "faq_category_translation",
                schema: "faq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    faq_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_faq_category_translation", x => x.id);
                    table.ForeignKey(
                        name: "fk_faq_category_translation_faq_category_faq_category_id",
                        column: x => x.faq_category_id,
                        principalSchema: "faq",
                        principalTable: "faq_category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "faq_item",
                schema: "faq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resort_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_faq_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_faq_item_faq_category_category_id",
                        column: x => x.category_id,
                        principalSchema: "faq",
                        principalTable: "faq_category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_faq_item_faq_item_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "faq",
                        principalTable: "faq_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "faq_item_translation",
                schema: "faq",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    faq_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    question = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    answer_html_sanitized = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_faq_item_translation", x => x.id);
                    table.ForeignKey(
                        name: "fk_faq_item_translation_faq_item_faq_item_id",
                        column: x => x.faq_item_id,
                        principalSchema: "faq",
                        principalTable: "faq_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_faq_category_resort_order",
                schema: "faq",
                table: "faq_category",
                columns: new[] { "resort_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_faq_category_key",
                schema: "faq",
                table: "faq_category",
                columns: new[] { "resort_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_faq_category_translation_lang",
                schema: "faq",
                table: "faq_category_translation",
                columns: new[] { "faq_category_id", "language_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_faq_item_category_order",
                schema: "faq",
                table: "faq_item",
                columns: new[] { "category_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_faq_item_parent",
                schema: "faq",
                table: "faq_item",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ux_faq_item_translation_lang",
                schema: "faq",
                table: "faq_item_translation",
                columns: new[] { "faq_item_id", "language_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "faq_category_translation",
                schema: "faq");

            migrationBuilder.DropTable(
                name: "faq_item_translation",
                schema: "faq");

            migrationBuilder.DropTable(
                name: "faq_item",
                schema: "faq");

            migrationBuilder.DropTable(
                name: "faq_category",
                schema: "faq");
        }
    }
}
