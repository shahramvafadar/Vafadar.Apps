using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <inheritdoc />
    public partial class ExternalLogins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalLogins",
                columns: table => new
                {
                    Provider = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    VisitorId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLogins", x => new { x.Provider, x.Key });
                    table.ForeignKey(
                        name: "FK_ExternalLogins_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_VisitorId",
                table: "ExternalLogins",
                column: "VisitorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalLogins");
        }
    }
}
