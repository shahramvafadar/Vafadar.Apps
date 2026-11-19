using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <inheritdoc />
    public partial class OperatorRestrictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ClosedByOperator",
                table: "Visitors",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAt",
                table: "Visitors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsVersion",
                table: "Visitors",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Restrictions",
                columns: table => new
                {
                    Number = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Account = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Target = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Ground = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Noticed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImposedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LiftedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restrictions", x => x.Number);
                    table.ForeignKey(
                        name: "FK_Restrictions_Visitors_Account",
                        column: x => x.Account,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Restrictions_Account",
                table: "Restrictions",
                column: "Account");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Restrictions");

            migrationBuilder.DropColumn(
                name: "ClosedByOperator",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedAt",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "TermsVersion",
                table: "Visitors");
        }
    }
}
