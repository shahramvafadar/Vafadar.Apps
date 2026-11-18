using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <inheritdoc />
    public partial class PhotoObjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Objection",
                table: "PhotoTags",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Objection",
                table: "PhotoTags");
        }
    }
}
