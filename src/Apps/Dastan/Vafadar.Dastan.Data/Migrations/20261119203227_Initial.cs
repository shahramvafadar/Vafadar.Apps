using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Dastan.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "delivery");

            migrationBuilder.CreateTable(
                name: "Mail",
                schema: "delivery",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    To = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Language = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Html = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextTry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GaveUpAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mail", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Notices",
                schema: "delivery",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Language = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Explanation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Mail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    GoodFaith = table.Column<bool>(type: "boolean", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Decision = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                schema: "delivery",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    On = table.Column<DateOnly>(type: "date", nullable: false),
                    Collection = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: true),
                    Language = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Statement = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SettledBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SettledOn = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mail_NextTry",
                schema: "delivery",
                table: "Mail",
                column: "NextTry",
                filter: "(\"SentAt\" IS NULL AND \"GaveUpAt\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_SettledAt_ReceivedAt",
                schema: "delivery",
                table: "Notices",
                columns: new[] { "SettledAt", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_SettledOn_On",
                schema: "delivery",
                table: "Reports",
                columns: new[] { "SettledOn", "On" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mail",
                schema: "delivery");

            migrationBuilder.DropTable(
                name: "Notices",
                schema: "delivery");

            migrationBuilder.DropTable(
                name: "Reports",
                schema: "delivery");
        }
    }
}
