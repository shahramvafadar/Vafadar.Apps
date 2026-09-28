using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class EntryTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Entries",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Entries");
        }
    }
}
