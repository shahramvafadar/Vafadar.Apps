using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <inheritdoc />
    public partial class Publication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublicationAskedAt",
                table: "Trees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Trees",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PublicationConsents",
                columns: table => new
                {
                    Tree = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Account = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationConsents", x => new { x.Tree, x.Account });
                    table.ForeignKey(
                        name: "FK_PublicationConsents_Trees_Tree",
                        column: x => x.Tree,
                        principalTable: "Trees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicationConsents_Visitors_Account",
                        column: x => x.Account,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicationConsents_Account",
                table: "PublicationConsents",
                column: "Account");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicationConsents");

            migrationBuilder.DropColumn(
                name: "PublicationAskedAt",
                table: "Trees");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Trees");
        }
    }
}
