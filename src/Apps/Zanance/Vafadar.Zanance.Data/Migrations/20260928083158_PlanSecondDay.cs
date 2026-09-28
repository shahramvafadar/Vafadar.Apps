using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlanSecondDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SecondDay",
                table: "Schedules",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecondDay",
                table: "Schedules");
        }
    }
}
