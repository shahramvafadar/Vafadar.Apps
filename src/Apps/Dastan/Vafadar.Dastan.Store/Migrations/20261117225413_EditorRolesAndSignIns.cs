using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.Store.Migrations
{
    /// <inheritdoc />
    public partial class EditorRolesAndSignIns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Roles",
                table: "Editors",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "writer,reviewer,publisher");

            migrationBuilder.CreateTable(
                name: "SignIns",
                columns: table => new
                {
                    Number = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EditorId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Succeeded = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignIns", x => x.Number);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignIns_EditorId_Number",
                table: "SignIns",
                columns: new[] { "EditorId", "Number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignIns");

            migrationBuilder.DropColumn(
                name: "Roles",
                table: "Editors");
        }
    }
}
