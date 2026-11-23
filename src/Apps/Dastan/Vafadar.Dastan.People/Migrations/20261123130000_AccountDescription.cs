using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <inheritdoc />
    public partial class AccountDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "About",
                schema: "personal",
                table: "Visitors",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AboutCircle",
                schema: "personal",
                table: "Visitors",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AboutScope",
                schema: "personal",
                table: "Visitors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Picture",
                schema: "personal",
                table: "Visitors",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "About",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "AboutCircle",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "AboutScope",
                schema: "personal",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "Picture",
                schema: "personal",
                table: "Visitors");
        }
    }
}
