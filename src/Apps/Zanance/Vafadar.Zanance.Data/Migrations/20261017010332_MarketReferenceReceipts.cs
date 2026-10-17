using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class MarketReferenceReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketReferenceReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Target = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    QuoteDigest = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    QuoteJson = table.Column<string>(type: "TEXT", maxLength: 32768, nullable: false),
                    MappingJson = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    AcceptedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketReferenceReceipts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketReferenceReceipts_Target_TargetId_AcceptedAt",
                table: "MarketReferenceReceipts",
                columns: new[] { "Target", "TargetId", "AcceptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketReferenceReceipts_Target_TargetId_TargetFingerprint_QuoteDigest",
                table: "MarketReferenceReceipts",
                columns: new[] { "Target", "TargetId", "TargetFingerprint", "QuoteDigest" },
                unique: true);

            // Source documents are immutable and bounded even for independent writers. They never mutate their financial targets.
            migrationBuilder.Sql("""
                CREATE TRIGGER MarketReferenceReceipts_Immutable BEFORE UPDATE ON MarketReferenceReceipts
                BEGIN SELECT RAISE(ABORT, 'Original market reference documents are immutable'); END;
                CREATE TRIGGER MarketReferenceReceipts_Bounds BEFORE INSERT ON MarketReferenceReceipts
                WHEN NEW.Target NOT IN (0, 1)
                  OR length(NEW.TargetFingerprint) != 64 OR NEW.TargetFingerprint GLOB '*[^0-9a-f]*'
                  OR length(NEW.QuoteDigest) != 64 OR NEW.QuoteDigest GLOB '*[^0-9a-f]*'
                  OR length(CAST(NEW.QuoteJson AS BLOB)) NOT BETWEEN 2 AND 32768
                  OR length(CAST(NEW.MappingJson AS BLOB)) NOT BETWEEN 2 AND 4096
                BEGIN SELECT RAISE(ABORT, 'Market reference document exceeds its original bounds'); END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketReferenceReceipts");
        }
    }
}
