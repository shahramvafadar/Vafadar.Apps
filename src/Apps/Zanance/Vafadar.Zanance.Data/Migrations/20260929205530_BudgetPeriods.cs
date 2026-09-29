using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class BudgetPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Budgets_Year_Month_Calendar_CurrencyCode",
                table: "Budgets");

            migrationBuilder.AddColumn<DateOnly>(
                name: "FortnightStart",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Period",
                table: "Budgets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodStart",
                table: "Budgets",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_Period_Year_Month_PeriodStart_Calendar_CurrencyCode",
                table: "Budgets",
                columns: new[] { "Period", "Year", "Month", "PeriodStart", "Calendar", "CurrencyCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Budgets_Period_Year_Month_PeriodStart_Calendar_CurrencyCode",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "FortnightStart",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "Period",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                table: "Budgets");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_Year_Month_Calendar_CurrencyCode",
                table: "Budgets",
                columns: new[] { "Year", "Month", "Calendar", "CurrencyCode" },
                unique: true);
        }
    }
}
