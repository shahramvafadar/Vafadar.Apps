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

            migrationBuilder.EnsureSchema(
                name: "published");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "Collections",
                schema: "editorial",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Header = table.Column<string>(type: "json", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comparisons",
                schema: "published",
                columns: table => new
                {
                    Number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComparisonId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Json = table.Column<string>(type: "json", nullable: true),
                    Translations = table.Column<string>(type: "json", nullable: true),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    By = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comparisons", x => x.Number);
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
                    Before = table.Column<string>(type: "json", nullable: true),
                    After = table.Column<string>(type: "json", nullable: true)
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
                    Body = table.Column<string>(type: "json", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => new { x.Kind, x.DocumentId, x.Language });
                });

            migrationBuilder.CreateTable(
                name: "EditorAccounts",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Roles = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "writer,reviewer,publisher"),
                    UserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditorAccounts", x => x.Id);
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
                    Before = table.Column<string>(type: "json", nullable: true),
                    After = table.Column<string>(type: "json", nullable: true)
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
                    Body = table.Column<string>(type: "json", nullable: false),
                    ReviewStatus = table.Column<string>(type: "text", nullable: true, computedColumnSql: "(\"Body\"::jsonb ->> 'reviewStatus')", stored: true),
                    Kind = table.Column<string>(type: "text", nullable: true, computedColumnSql: "coalesce(\"Body\"::jsonb ->> 'type', \"Body\"::jsonb ->> 'kind')", stored: true),
                    Label = table.Column<string>(type: "text", nullable: true, computedColumnSql: "coalesce(\"Body\"::jsonb ->> 'label', \"Body\"::jsonb ->> 'title')", stored: true),
                    SpoilerLevel = table.Column<int>(type: "integer", nullable: true, computedColumnSql: "CASE WHEN jsonb_typeof(\"Body\"::jsonb -> 'spoilerLevel') = 'number' THEN (\"Body\"::jsonb ->> 'spoilerLevel')::numeric::integer END", stored: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Records", x => new { x.CollectionId, x.Group, x.RecordId });
                    table.CheckConstraint("CK_Records_Body_Names_Itself", "(\"Body\"::jsonb ->> 'id') = \"RecordId\"");
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
                schema: "published",
                columns: table => new
                {
                    CollectionId = table.Column<string>(type: "character varying(200)", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    PublishedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Json = table.Column<string>(type: "json", nullable: false),
                    Translations = table.Column<string>(type: "json", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
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
                    Body = table.Column<string>(type: "json", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "RecordLinks",
                schema: "editorial",
                columns: table => new
                {
                    CollectionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FromGroup = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FromId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    ToGroup = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ToId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordLinks", x => new { x.CollectionId, x.FromGroup, x.FromId, x.Role, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_RecordLinks_Records_CollectionId_FromGroup_FromId",
                        columns: x => new { x.CollectionId, x.FromGroup, x.FromId },
                        principalSchema: "editorial",
                        principalTable: "Records",
                        principalColumns: new[] { "CollectionId", "Group", "RecordId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecordLinks_Records_CollectionId_ToGroup_ToId",
                        columns: x => new { x.CollectionId, x.ToGroup, x.ToId },
                        principalSchema: "editorial",
                        principalTable: "Records",
                        principalColumns: new[] { "CollectionId", "Group", "RecordId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Releases",
                schema: "published",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CollectionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: true),
                    By = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Releases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Releases_Revisions_CollectionId_Number",
                        columns: x => new { x.CollectionId, x.Number },
                        principalSchema: "published",
                        principalTable: "Revisions",
                        principalColumns: new[] { "CollectionId", "Number" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Changes_CollectionId_Group_RecordId_Version",
                schema: "editorial",
                table: "Changes",
                columns: new[] { "CollectionId", "Group", "RecordId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_Comparisons_ComparisonId_Number",
                schema: "published",
                table: "Comparisons",
                columns: new[] { "ComparisonId", "Number" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChanges_Kind_DocumentId_Number",
                schema: "editorial",
                table: "DocumentChanges",
                columns: new[] { "Kind", "DocumentId", "Number" });

            migrationBuilder.CreateIndex(
                name: "IX_EditorAccounts_NormalizedUserName",
                schema: "identity",
                table: "EditorAccounts",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecordLinks_CollectionId_ToGroup_ToId",
                schema: "editorial",
                table: "RecordLinks",
                columns: new[] { "CollectionId", "ToGroup", "ToId" });

            migrationBuilder.CreateIndex(
                name: "IX_Records_CollectionId_Group_Kind",
                schema: "editorial",
                table: "Records",
                columns: new[] { "CollectionId", "Group", "Kind" });

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
                name: "IX_Releases_CollectionId_Id",
                schema: "published",
                table: "Releases",
                columns: new[] { "CollectionId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Releases_CollectionId_Number",
                schema: "published",
                table: "Releases",
                columns: new[] { "CollectionId", "Number" });

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
                name: "Comparisons",
                schema: "published");

            migrationBuilder.DropTable(
                name: "DocumentChanges",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "Documents",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "EditorAccounts",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "RecordLinks",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "Releases",
                schema: "published");

            migrationBuilder.DropTable(
                name: "SignIns",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "Translations",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "Records",
                schema: "editorial");

            migrationBuilder.DropTable(
                name: "Revisions",
                schema: "published");

            migrationBuilder.DropTable(
                name: "Collections",
                schema: "editorial");
        }
    }
}
