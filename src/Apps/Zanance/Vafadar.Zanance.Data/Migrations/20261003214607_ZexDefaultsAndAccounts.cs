using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <summary>
    /// Enhancement ZEX, phase 1: the default currency for new items separate from the valuation currency (ZEX-P01), the
    /// rate freshness (ZEX-P06), the Home budget currency (ZEX-P02), display units and Home layout per profile (ZEX-P20),
    /// and per account "usable for payments" and country (ZEX-P17, P18). Existing data keeps its meaning: the default
    /// currency starts as today's report currency, and cash, checking and savings accounts are usable for payments.
    /// </summary>
    public partial class ZexDefaultsAndAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrencyCode",
                table: "Settings",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DisplayUnits",
                table: "Settings",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeBudgetCurrencyCode",
                table: "Settings",
                type: "TEXT",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeLayout",
                table: "Settings",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RateFreshnessDays",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<bool>(
                name: "ValuationCurrencyEnabled",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Accounts",
                type: "TEXT",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsableForPayments",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Existing data: the default currency was the report currency; cash (0), checking (1) and savings (2) pay bills.
            migrationBuilder.Sql("UPDATE Settings SET DefaultCurrencyCode = ReportCurrencyCode;");
            migrationBuilder.Sql("UPDATE Accounts SET UsableForPayments = 1 WHERE Type IN (0, 1, 2);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultCurrencyCode",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "DisplayUnits",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "HomeBudgetCurrencyCode",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "HomeLayout",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "RateFreshnessDays",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ValuationCurrencyEnabled",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "UsableForPayments",
                table: "Accounts");
        }
    }
}
