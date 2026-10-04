using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <summary>
    /// Enhancement ZEX, phase 4: the last reconciliation date and a due date of money lent per account, due dates of
    /// reimbursements, aggregated entries with their range, essential categories (the default ones are marked), the
    /// explicit day-to-day spending estimate and the progress of the period-end review. Every column has a default, so
    /// existing numbers stay unchanged.
    /// </summary>
    public partial class ZexReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EssentialEstimate",
                table: "Settings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EssentialEstimateCurrency",
                table: "Settings",
                type: "TEXT",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EssentialEstimatePeriod",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReviewProgress",
                table: "Settings",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "AggregatedFrom",
                table: "Entries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "AggregatedTo",
                table: "Entries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAggregated",
                table: "Entries",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReimbursementDueDate",
                table: "Entries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEssential",
                table: "Categories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                table: "Accounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LastReconciledOn",
                table: "Accounts",
                type: "TEXT",
                nullable: true);

            // The default expense categories that are essential by default (DefaultCategories.IsEssential).
            migrationBuilder.Sql("UPDATE Categories SET IsEssential = 1 WHERE Kind = 0 AND SystemKey IN ('Housing', 'Food', 'Energy', 'Communication', 'Transport', 'Health', 'Insurance');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EssentialEstimate",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "EssentialEstimateCurrency",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "EssentialEstimatePeriod",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ReviewProgress",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "AggregatedFrom",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "AggregatedTo",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "IsAggregated",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "ReimbursementDueDate",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "IsEssential",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "LastReconciledOn",
                table: "Accounts");
        }
    }
}
