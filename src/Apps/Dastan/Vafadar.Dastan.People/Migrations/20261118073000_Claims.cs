using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <inheritdoc />
    public partial class Claims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Account",
                table: "Persons",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    Number = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tree = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Person = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Account = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Number);
                    table.ForeignKey(
                        name: "FK_Claims_Persons_Person",
                        column: x => x.Person,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Claims_Trees_Tree",
                        column: x => x.Tree,
                        principalTable: "Trees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Claims_Visitors_Account",
                        column: x => x.Account,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Account",
                table: "Persons",
                column: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Account",
                table: "Claims",
                column: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Person",
                table: "Claims",
                column: "Person");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Tree",
                table: "Claims",
                column: "Tree");

            migrationBuilder.AddForeignKey(
                name: "FK_Persons_Visitors_Account",
                table: "Persons",
                column: "Account",
                principalTable: "Visitors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Persons_Visitors_Account",
                table: "Persons");

            migrationBuilder.DropTable(
                name: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_Persons_Account",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "Account",
                table: "Persons");
        }
    }
}
