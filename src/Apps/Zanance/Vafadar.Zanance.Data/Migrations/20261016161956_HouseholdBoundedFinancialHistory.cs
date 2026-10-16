using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdBoundedFinancialHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HouseholdLocalFinancialReferences",
                columns: table => new
                {
                    Version = table.Column<long>(type: "INTEGER", nullable: false),
                    Table = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Cursor = table.Column<long>(type: "INTEGER", nullable: false),
                    EventIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Offset = table.Column<long>(type: "INTEGER", nullable: false),
                    Length = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdLocalFinancialReferences", x => x.Version);
                    table.CheckConstraint("CK_HouseholdFinancialReference_Position", "\"Version\" > 0 AND \"Cursor\" > 0 AND \"EventIndex\" >= 0 AND ((\"Offset\" = -1 AND \"Length\" = 0) OR (\"Offset\" >= 4 AND \"Length\" BETWEEN 1 AND 16777216))");
                    table.ForeignKey(
                        name: "FK_HouseholdLocalFinancialReferences_HouseholdLocalPackets_Cursor",
                        column: x => x.Cursor,
                        principalTable: "HouseholdLocalPackets",
                        principalColumn: "Cursor",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdLocalEvents_RecordId_Cursor",
                table: "HouseholdLocalEvents",
                columns: new[] { "RecordId", "Cursor" });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdLocalFinancialReferences_Cursor",
                table: "HouseholdLocalFinancialReferences",
                column: "Cursor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseholdLocalFinancialReferences");

            migrationBuilder.DropIndex(
                name: "IX_HouseholdLocalEvents_RecordId_Cursor",
                table: "HouseholdLocalEvents");
        }
    }
}
