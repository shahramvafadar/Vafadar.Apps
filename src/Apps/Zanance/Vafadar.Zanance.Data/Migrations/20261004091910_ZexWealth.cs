using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <summary>
    /// Enhancement ZEX, phase 5: quantity goals (holding type and an optional location), the assumed price of a
    /// contribution plan for quantity goals, and saved forecast snapshots. Existing goals and plans keep their values.
    /// </summary>
    public partial class ZexWealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssetTypeId",
                table: "Goals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "Goals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AssumedPricePerUnitMilli",
                table: "ContributionPlans",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ForecastSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BaseDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Horizon = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    AccountIds = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Minimum = table.Column<long>(type: "INTEGER", nullable: false),
                    MinimumDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    UnknownCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Assumptions = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AppVersion = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastSnapshots_CreatedAt",
                table: "ForecastSnapshots",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForecastSnapshots");

            migrationBuilder.DropColumn(
                name: "AssetTypeId",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "AssumedPricePerUnitMilli",
                table: "ContributionPlans");
        }
    }
}
