using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <summary>
    /// Enhancement ZEX, phase 3: quantity holdings – asset types, locations, events (opening, purchase, sale, transfer,
    /// gift, outflow, correction) and dated valuations. New tables only; the new entry kinds 6 and 7 (asset purchase and
    /// sale) need no column.
    /// </summary>
    public partial class ZexHoldings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Dimension = table.Column<int>(type: "INTEGER", nullable: false),
                    Metal = table.Column<int>(type: "INTEGER", nullable: false),
                    PurityPer10000 = table.Column<int>(type: "INTEGER", nullable: true),
                    UnitWeightMg = table.Column<long>(type: "INTEGER", nullable: true),
                    CountUnitName = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Divisible = table.Column<bool>(type: "INTEGER", nullable: false),
                    PriceCurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssetTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToLocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Quantity = table.Column<long>(type: "INTEGER", nullable: false),
                    BasisAmount = table.Column<long>(type: "INTEGER", nullable: true),
                    ProceedsAmount = table.Column<long>(type: "INTEGER", nullable: true),
                    IsIncrease = table.Column<bool>(type: "INTEGER", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetEvents_AssetLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "AssetLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetEvents_AssetLocations_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "AssetLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetEvents_AssetTypes_AssetTypeId",
                        column: x => x.AssetTypeId,
                        principalTable: "AssetTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetValuations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssetTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PricePerUnitMilli = table.Column<long>(type: "INTEGER", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetValuations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetValuations_AssetTypes_AssetTypeId",
                        column: x => x.AssetTypeId,
                        principalTable: "AssetTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_AssetTypeId_Date",
                table: "AssetEvents",
                columns: new[] { "AssetTypeId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_GroupId",
                table: "AssetEvents",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_LocationId",
                table: "AssetEvents",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_ToLocationId",
                table: "AssetEvents",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetValuations_AssetTypeId_Date",
                table: "AssetValuations",
                columns: new[] { "AssetTypeId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetEvents");

            migrationBuilder.DropTable(
                name: "AssetValuations");

            migrationBuilder.DropTable(
                name: "AssetLocations");

            migrationBuilder.DropTable(
                name: "AssetTypes");
        }
    }
}
