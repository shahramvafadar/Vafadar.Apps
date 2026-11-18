using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <inheritdoc />
    public partial class Trees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trees",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trees_Visitors_Owner",
                        column: x => x.Owner,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Tree = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Born = table.Column<int>(type: "INTEGER", nullable: true),
                    Died = table.Column<int>(type: "INTEGER", nullable: true),
                    Deceased = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Persons_Trees_Tree",
                        column: x => x.Tree,
                        principalTable: "Trees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Persons_Visitors_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TreeChanges",
                columns: table => new
                {
                    Number = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tree = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    By = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    What = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreeChanges", x => x.Number);
                    table.ForeignKey(
                        name: "FK_TreeChanges_Trees_Tree",
                        column: x => x.Tree,
                        principalTable: "Trees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreeChanges_Visitors_By",
                        column: x => x.By,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TreeMembers",
                columns: table => new
                {
                    Tree = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Account = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreeMembers", x => new { x.Tree, x.Account });
                    table.ForeignKey(
                        name: "FK_TreeMembers_Trees_Tree",
                        column: x => x.Tree,
                        principalTable: "Trees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreeMembers_Visitors_Account",
                        column: x => x.Account,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Kinships",
                columns: table => new
                {
                    Number = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tree = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    From = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    To = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kinships", x => x.Number);
                    table.ForeignKey(
                        name: "FK_Kinships_Persons_From",
                        column: x => x.From,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Kinships_Persons_To",
                        column: x => x.To,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Kinships_Trees_Tree",
                        column: x => x.Tree,
                        principalTable: "Trees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kinships_From",
                table: "Kinships",
                column: "From");

            migrationBuilder.CreateIndex(
                name: "IX_Kinships_To",
                table: "Kinships",
                column: "To");

            migrationBuilder.CreateIndex(
                name: "IX_Kinships_Tree",
                table: "Kinships",
                column: "Tree");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_CreatedBy",
                table: "Persons",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Tree",
                table: "Persons",
                column: "Tree");

            migrationBuilder.CreateIndex(
                name: "IX_TreeChanges_By",
                table: "TreeChanges",
                column: "By");

            migrationBuilder.CreateIndex(
                name: "IX_TreeChanges_Tree",
                table: "TreeChanges",
                column: "Tree");

            migrationBuilder.CreateIndex(
                name: "IX_TreeMembers_Account",
                table: "TreeMembers",
                column: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_Trees_Owner",
                table: "Trees",
                column: "Owner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kinships");

            migrationBuilder.DropTable(
                name: "TreeChanges");

            migrationBuilder.DropTable(
                name: "TreeMembers");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "Trees");
        }
    }
}
