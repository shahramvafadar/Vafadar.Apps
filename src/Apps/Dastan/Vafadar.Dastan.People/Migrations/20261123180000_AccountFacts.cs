using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <inheritdoc />
    public partial class AccountFacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BornCircle",
                schema: "personal",
                table: "Visitors",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BornScope",
                schema: "personal",
                table: "Visitors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BornYear",
                schema: "personal",
                table: "Visitors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "personal",
                table: "Visitors",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Languages",
                schema: "personal",
                table: "Visitors",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LanguagesCircle",
                schema: "personal",
                table: "Visitors",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LanguagesScope",
                schema: "personal",
                table: "Visitors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Link",
                schema: "personal",
                table: "Visitors",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LinkCircle",
                schema: "personal",
                table: "Visitors",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LinkScope",
                schema: "personal",
                table: "Visitors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "LivesCircle",
                schema: "personal",
                table: "Visitors",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LivesScope",
                schema: "personal",
                table: "Visitors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Town",
                schema: "personal",
                table: "Visitors",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BornCircle",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "BornScope",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "BornYear",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "Languages",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "LanguagesCircle",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "LanguagesScope",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "Link",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "LinkCircle",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "LinkScope",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "LivesCircle",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "LivesScope",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "Town",
                schema: "personal",
                table: "Visitors");
        }
    }
}
