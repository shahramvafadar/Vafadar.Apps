using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vafadar.Dastan.Store.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "editorial");

            migrationBuilder.CreateTable(
                name: "Collections",
                schema: "editorial",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Header = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentChanges",
                schema: "editorial",
                columns: table => new
                {
                    Number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DocumentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Language = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Change = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    By = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Before = table.Column<string>(type: "text", nullable: true),
                    After = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChanges", x => x.Number);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                schema: "editorial",
                columns: table => new
                {
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DocumentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Language = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => new { x.Kind, x.DocumentId, x.Language });
                });

            migrationBuilder.CreateTable(
                name: "Editors",
                schema: "editorial",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Password = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Stamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Failures = table.Column<int>(type: "integer", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Roles = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "writer,reviewer,publisher")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Editors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignIns",
                schema: "editorial",
                columns: table => new
                {
                    Number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EditorId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignIns", x => x.Number);
                });

            migrationBuilder.CreateTable(
                name: "Changes",
                schema: "editorial",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CollectionId = table.Column<string>(type: "character varying(200)", nullable: false),
                    Group = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RecordId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    By = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Before = table.Column<string>(type: "text", nullable: true),
                    After = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Changes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Changes_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalSchema: "editorial",
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Records",
                schema: "editorial",
                columns: table => new
                {
                    CollectionId = table.Column<string>(type: "character varying(200)", nullable: false),
                    Group = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RecordId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ReviewStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Records", x => new { x.CollectionId, x.Group, x.RecordId });
                    table.ForeignKey(
                        name: "FK_Records_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalSchema: "editorial",
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Revisions",
                schema: "editorial",
                columns: table => new
                {
                    CollectionId = table.Column<string>(type: "character varying(200)", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    PublishedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Json = table.Column<string>(type: "text", nullable: false),
                    Translations = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    By = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Revisions", x => new { x.CollectionId, x.Number });
                    table.ForeignKey(
                        name: "FK_Revisions_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalSchema: "editorial",
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Translations",
                schema: "editorial",
                columns: table => new
                {
                    CollectionId = table.Column<string>(type: "character varying(200)", nullable: false),
                    Language = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translations", x => new { x.CollectionId, x.Language });
                    table.ForeignKey(
                        name: "FK_Translations_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalSchema: "editorial",
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Changes_CollectionId_Group_RecordId_Version",
                schema: "editorial",
                table: "Changes",
                columns: new[] { "CollectionId", "Group", "RecordId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChanges_Kind_DocumentId_Number",
                schema: "editorial",
                table: "DocumentChanges",
                columns: new[] { "Kind", "DocumentId", "Number" });

            migrationBuilder.CreateIndex(
                name: "IX_Records_CollectionId_Group_Position",
                schema: "editorial",
                table: "Records",
                columns: new[] { "CollectionId", "Group", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Records_CollectionId_ReviewStatus",
                schema: "editorial",
                table: "Records",
                columns: new[] { "CollectionId", "ReviewStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_SignIns_EditorId_Number",
                schema: "editorial",
                table: "SignIns",
                columns: new[] { "EditorId", "Number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Changes",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "DocumentChanges",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "Documents",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "Editors",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "Records",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "Revisions",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "SignIns",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "Translations",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "Collections",
                schema: "editorial");
        }
    }
}
