using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Finance.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlanContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "CancellationDeadline",
                table: "Schedules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ContractEnd",
                table: "Schedules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractProvider",
                table: "Schedules",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractReference",
                table: "Schedules",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ContractRenews",
                table: "Schedules",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReviewDate",
                table: "Schedules",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationDeadline",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ContractEnd",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ContractProvider",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ContractReference",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ContractRenews",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ReviewDate",
                table: "Schedules");
        }
    }
}
