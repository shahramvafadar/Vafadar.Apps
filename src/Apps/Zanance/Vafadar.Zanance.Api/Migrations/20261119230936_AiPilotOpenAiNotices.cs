using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AiPilotOpenAiNotices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiPilotOpenAiNotices",
                columns: table => new
                {
                    ResponseId = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    DeliveryId = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Handled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPilotOpenAiNotices", x => x.ResponseId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiPilotOpenAiNotices_DeliveryId",
                table: "AiPilotOpenAiNotices",
                column: "DeliveryId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiPilotOpenAiNotices");
        }
    }
}
