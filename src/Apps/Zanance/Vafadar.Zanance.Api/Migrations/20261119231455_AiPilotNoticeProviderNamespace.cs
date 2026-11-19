using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AiPilotNoticeProviderNamespace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AiPilotOpenAiNotices",
                table: "AiPilotOpenAiNotices");

            migrationBuilder.DropIndex(
                name: "IX_AiPilotOpenAiNotices_DeliveryId",
                table: "AiPilotOpenAiNotices");

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "AiPilotOpenAiNotices",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "openai");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AiPilotOpenAiNotices",
                table: "AiPilotOpenAiNotices",
                columns: new[] { "Provider", "ResponseId" });

            migrationBuilder.CreateIndex(
                name: "IX_AiPilotOpenAiNotices_Provider_DeliveryId",
                table: "AiPilotOpenAiNotices",
                columns: new[] { "Provider", "DeliveryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AiPilotOpenAiNotices",
                table: "AiPilotOpenAiNotices");

            migrationBuilder.DropIndex(
                name: "IX_AiPilotOpenAiNotices_Provider_DeliveryId",
                table: "AiPilotOpenAiNotices");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "AiPilotOpenAiNotices");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AiPilotOpenAiNotices",
                table: "AiPilotOpenAiNotices",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_AiPilotOpenAiNotices_DeliveryId",
                table: "AiPilotOpenAiNotices",
                column: "DeliveryId",
                unique: true);
        }
    }
}
