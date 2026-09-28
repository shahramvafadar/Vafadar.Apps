using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class WeekendShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WeekendDays",
                table: "Schedules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WeekendShift",
                table: "Schedules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeekendDays",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "WeekendShift",
                table: "Schedules");
        }
    }
}
