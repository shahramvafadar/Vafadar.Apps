using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdActualCostLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HouseholdContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IdentityId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    PaidByContactId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PaymentKind = table.Column<int>(type: "INTEGER", nullable: false),
                    EntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RefundOfCostId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdCosts_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdCosts_HouseholdContacts_PaidByContactId",
                        column: x => x.PaidByContactId,
                        principalTable: "HouseholdContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdCosts_HouseholdCosts_RefundOfCostId",
                        column: x => x.RefundOfCostId,
                        principalTable: "HouseholdCosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromContactId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToContactId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    EntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UsesDestinationAmount = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdSettlements_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdSettlements_HouseholdContacts_FromContactId",
                        column: x => x.FromContactId,
                        principalTable: "HouseholdContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdSettlements_HouseholdContacts_ToContactId",
                        column: x => x.ToContactId,
                        principalTable: "HouseholdContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdAllocations",
                columns: table => new
                {
                    CostId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdAllocations", x => new { x.CostId, x.ContactId });
                    table.ForeignKey(
                        name: "FK_HouseholdAllocations_HouseholdContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "HouseholdContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdAllocations_HouseholdCosts_CostId",
                        column: x => x.CostId,
                        principalTable: "HouseholdCosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdAllocations_ContactId",
                table: "HouseholdAllocations",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdCosts_Date",
                table: "HouseholdCosts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdCosts_EntryId",
                table: "HouseholdCosts",
                column: "EntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdCosts_PaidByContactId",
                table: "HouseholdCosts",
                column: "PaidByContactId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdCosts_RefundOfCostId",
                table: "HouseholdCosts",
                column: "RefundOfCostId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdSettlements_Date",
                table: "HouseholdSettlements",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdSettlements_EntryId",
                table: "HouseholdSettlements",
                column: "EntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdSettlements_FromContactId",
                table: "HouseholdSettlements",
                column: "FromContactId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdSettlements_ToContactId",
                table: "HouseholdSettlements",
                column: "ToContactId");
            migrationBuilder.Sql(ReadFrozen("CostCaptureUp"));
            migrationBuilder.Sql(ReadFrozen("CostWriteUp"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReadFrozen("CostWriteDown"));
            migrationBuilder.Sql(ReadFrozen("CostCaptureDown"));
            migrationBuilder.DropTable(
                name: "HouseholdAllocations");

            migrationBuilder.DropTable(
                name: "HouseholdSettlements");

            migrationBuilder.DropTable(
                name: "HouseholdCosts");

            migrationBuilder.DropTable(
                name: "HouseholdContacts");
        }

        /// <summary>Uses immutable reviewed SQL; later generators cannot rewrite an applied migration.</summary>
        private static string ReadFrozen(string name)
        {
            using var source = typeof(HouseholdActualCostLedger).Assembly.GetManifestResourceStream($"Vafadar.Zanance.Household.{name}.v1.sql")
                ?? throw new InvalidOperationException("The household cost boundary is missing.");
            using var reader = new System.IO.StreamReader(source);
            return reader.ReadToEnd();
        }
    }
}
