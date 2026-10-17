using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class MarketSourceCompanionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketReferenceReceipts_Target_TargetId_TargetFingerprint_QuoteDigest",
                table: "MarketReferenceReceipts");

            migrationBuilder.AddColumn<long>(
                name: "MarketSourceCursor",
                table: "SyncBinding",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "SyncMarketSourcesSent",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Digest = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Acknowledged = table.Column<bool>(type: "INTEGER", nullable: false),
                    Envelope = table.Column<string>(type: "TEXT", maxLength: 524288, nullable: true),
                    Epoch = table.Column<long>(type: "INTEGER", nullable: false),
                    MembershipVersion = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncMarketSourcesSent", x => x.DocumentId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketReferenceReceipts_Target_TargetId_TargetFingerprint_QuoteDigest",
                table: "MarketReferenceReceipts",
                columns: new[] { "Target", "TargetId", "TargetFingerprint", "QuoteDigest" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncMarketSourcesSent");

            migrationBuilder.DropIndex(
                name: "IX_MarketReferenceReceipts_Target_TargetId_TargetFingerprint_QuoteDigest",
                table: "MarketReferenceReceipts");

            migrationBuilder.DropColumn(
                name: "MarketSourceCursor",
                table: "SyncBinding");

            migrationBuilder.CreateIndex(
                name: "IX_MarketReferenceReceipts_Target_TargetId_TargetFingerprint_QuoteDigest",
                table: "MarketReferenceReceipts",
                columns: new[] { "Target", "TargetId", "TargetFingerprint", "QuoteDigest" },
                unique: true);
        }
    }
}
