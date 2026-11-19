using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AiPilotDispatchBoundary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PilotDispatchClaimedAt",
                table: "AiRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PilotDispatchStage",
                table: "AiRequests",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PilotDispatchClaimedAt",
                table: "AiRequests");

            migrationBuilder.DropColumn(
                name: "PilotDispatchStage",
                table: "AiRequests");
        }
    }
}
