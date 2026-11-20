using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AiPilotCreateRefusal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PilotCreateRefusal",
                table: "AiRequests",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PilotCreateRefusal",
                table: "AiRequests");
        }
    }
}
