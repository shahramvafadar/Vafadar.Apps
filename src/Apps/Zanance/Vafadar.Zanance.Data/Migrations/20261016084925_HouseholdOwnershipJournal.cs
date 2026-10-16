using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdOwnershipJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HouseholdLocalOwnership",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    OperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AnchorVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    BodyJson = table.Column<string>(type: "TEXT", maxLength: 2097152, nullable: false),
                    Digest = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdLocalOwnership", x => x.Id);
                    table.CheckConstraint("CK_HouseholdLocalOwnership_Bound", "length(CAST(\"BodyJson\" AS BLOB)) BETWEEN 1 AND 2097152");
                    table.CheckConstraint("CK_HouseholdLocalOwnership_Singleton", "\"Id\"=1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseholdLocalOwnership");
        }
    }
}
