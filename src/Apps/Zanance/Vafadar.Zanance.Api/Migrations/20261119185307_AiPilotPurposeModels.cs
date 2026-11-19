using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AiPilotPurposeModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "AiPilotRoutes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Purpose",
                table: "AiPilotRoutes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AiPilotRoutes_Purpose_Id",
                table: "AiPilotRoutes",
                columns: new[] { "Purpose", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiPilotRoutes_Purpose_Id",
                table: "AiPilotRoutes");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "AiPilotRoutes");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "AiPilotRoutes");
        }
    }
}
