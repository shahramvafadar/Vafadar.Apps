using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <inheritdoc />
    public partial class Belongings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    By = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Purpose = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastDay = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    UsedBy = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    UsedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invitations_Visitors_By",
                        column: x => x.By,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invitations_Visitors_UsedBy",
                        column: x => x.UsedBy,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ReadingPositions",
                columns: table => new
                {
                    VisitorId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Collection = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingPositions", x => new { x.VisitorId, x.Collection });
                    table.ForeignKey(
                        name: "FK_ReadingPositions_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedPages",
                columns: table => new
                {
                    Number = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VisitorId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Collection = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PageId = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedPages", x => x.Number);
                    table.ForeignKey(
                        name: "FK_SavedPages_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_By",
                table: "Invitations",
                column: "By");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_UsedBy",
                table: "Invitations",
                column: "UsedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPages_VisitorId_Collection_Kind_PageId",
                table: "SavedPages",
                columns: new[] { "VisitorId", "Collection", "Kind", "PageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropTable(
                name: "ReadingPositions");

            migrationBuilder.DropTable(
                name: "SavedPages");
        }
    }
}
