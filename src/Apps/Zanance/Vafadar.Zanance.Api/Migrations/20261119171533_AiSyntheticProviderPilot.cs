using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AiSyntheticProviderPilot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VendorInputTokens",
                table: "AiRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorOperationId",
                table: "AiRequests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VendorOutputTokens",
                table: "AiRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PilotRouteId",
                table: "AiOffers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiPilotRoutes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Provider = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Revision = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Actor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SelectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPilotRoutes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiOffers_PilotRouteId",
                table: "AiOffers",
                column: "PilotRouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_AiOffers_AiPilotRoutes_PilotRouteId",
                table: "AiOffers",
                column: "PilotRouteId",
                principalTable: "AiPilotRoutes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiOffers_AiPilotRoutes_PilotRouteId",
                table: "AiOffers");

            migrationBuilder.DropTable(
                name: "AiPilotRoutes");

            migrationBuilder.DropIndex(
                name: "IX_AiOffers_PilotRouteId",
                table: "AiOffers");

            migrationBuilder.DropColumn(
                name: "VendorInputTokens",
                table: "AiRequests");

            migrationBuilder.DropColumn(
                name: "VendorOperationId",
                table: "AiRequests");

            migrationBuilder.DropColumn(
                name: "VendorOutputTokens",
                table: "AiRequests");

            migrationBuilder.DropColumn(
                name: "PilotRouteId",
                table: "AiOffers");
        }
    }
}
