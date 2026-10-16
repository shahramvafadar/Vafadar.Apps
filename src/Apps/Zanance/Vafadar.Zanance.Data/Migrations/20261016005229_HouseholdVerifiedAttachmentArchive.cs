using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdVerifiedAttachmentArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HouseholdLocalBlobReferences",
                columns: table => new
                {
                    Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PieceIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Length = table.Column<long>(type: "INTEGER", nullable: false),
                    Cursor = table.Column<long>(type: "INTEGER", nullable: false),
                    EventIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ElementIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    PieceHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdLocalBlobReferences", x => new { x.Hash, x.PieceIndex });
                    table.CheckConstraint("CK_HouseholdLocalBlobReference_Position", "\"Cursor\" > 0 AND \"Length\" > 0 AND \"PieceIndex\" >= 0 AND \"EventIndex\" >= 0 AND \"ElementIndex\" BETWEEN -1 AND 511");
                    table.ForeignKey(
                        name: "FK_HouseholdLocalBlobReferences_HouseholdLocalPackets_Cursor",
                        column: x => x.Cursor,
                        principalTable: "HouseholdLocalPackets",
                        principalColumn: "Cursor",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdLocalBlobReferences_Cursor_EventIndex",
                table: "HouseholdLocalBlobReferences",
                columns: new[] { "Cursor", "EventIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseholdLocalBlobReferences");
        }
    }
}
