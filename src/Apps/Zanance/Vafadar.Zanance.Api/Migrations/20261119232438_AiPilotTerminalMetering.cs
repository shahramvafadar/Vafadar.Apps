using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AiPilotTerminalMetering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PilotMeteredCostMicros",
                table: "AiRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PilotMeteredTariff",
                table: "AiRequests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VendorCachedInputTokens",
                table: "AiRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VendorThinkingTokens",
                table: "AiRequests",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PilotMeteredCostMicros",
                table: "AiRequests");

            migrationBuilder.DropColumn(
                name: "PilotMeteredTariff",
                table: "AiRequests");

            migrationBuilder.DropColumn(
                name: "VendorCachedInputTokens",
                table: "AiRequests");

            migrationBuilder.DropColumn(
                name: "VendorThinkingTokens",
                table: "AiRequests");
        }
    }
}
