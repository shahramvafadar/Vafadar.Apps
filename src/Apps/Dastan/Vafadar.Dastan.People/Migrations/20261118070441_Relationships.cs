using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <inheritdoc />
    public partial class Relationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Circles",
                columns: table => new
                {
                    Number = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Circles", x => x.Number);
                    table.ForeignKey(
                        name: "FK_Circles_Visitors_Owner",
                        column: x => x.Owner,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Relationships",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    From = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    To = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    FromScope = table.Column<int>(type: "INTEGER", nullable: false),
                    FromCircle = table.Column<long>(type: "INTEGER", nullable: true),
                    ToScope = table.Column<int>(type: "INTEGER", nullable: false),
                    ToCircle = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relationships_Visitors_From",
                        column: x => x.From,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Relationships_Visitors_To",
                        column: x => x.To,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CircleMembers",
                columns: table => new
                {
                    Circle = table.Column<long>(type: "INTEGER", nullable: false),
                    Member = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircleMembers", x => new { x.Circle, x.Member });
                    table.ForeignKey(
                        name: "FK_CircleMembers_Circles_Circle",
                        column: x => x.Circle,
                        principalTable: "Circles",
                        principalColumn: "Number",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CircleMembers_Visitors_Member",
                        column: x => x.Member,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CircleMembers_Member",
                table: "CircleMembers",
                column: "Member");

            migrationBuilder.CreateIndex(
                name: "IX_Circles_Owner",
                table: "Circles",
                column: "Owner");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_From",
                table: "Relationships",
                column: "From");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_To",
                table: "Relationships",
                column: "To");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CircleMembers");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "Circles");
        }
    }
}
