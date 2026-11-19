using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <inheritdoc />
    public partial class OneLoginOfAProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExternalLogins_VisitorId",
                table: "ExternalLogins");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_VisitorId_Provider",
                table: "ExternalLogins",
                columns: new[] { "VisitorId", "Provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExternalLogins_VisitorId_Provider",
                table: "ExternalLogins");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_VisitorId",
                table: "ExternalLogins",
                column: "VisitorId");
        }
    }
}
