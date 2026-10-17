using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class PersonalMarketSourceDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonalMarketSources",
                columns: table => new
                {
                    Sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Digest = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProtectedDocument = table.Column<string>(type: "character varying(131072)", maxLength: 131072, nullable: false),
                    Bytes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalMarketSources", x => x.Sequence);
                    table.ForeignKey(
                        name: "FK_PersonalMarketSources_SyncSpaces_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "SyncSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalMarketSources_ProfileId_DocumentId",
                table: "PersonalMarketSources",
                columns: new[] { "ProfileId", "DocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalMarketSources_ProfileId_Sequence",
                table: "PersonalMarketSources",
                columns: new[] { "ProfileId", "Sequence" });
            // Independent writers cannot rewrite accepted source history. Profile erasure may still cascade DELETE.
            migrationBuilder.Sql("""
                ALTER TABLE "PersonalMarketSources" ADD CONSTRAINT "CK_PersonalMarketSources_Bounds"
                CHECK ("Bytes" BETWEEN 1 AND 65536 AND "Digest" ~ '^[0-9a-f]{64}$' AND length("ProtectedDocument") BETWEEN 1 AND 131072);
                CREATE FUNCTION refuse_personal_market_source_update() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Original market source documents are immutable' USING ERRCODE = '23514'; END;
                $$;
                CREATE TRIGGER "PersonalMarketSources_Immutable" BEFORE UPDATE ON "PersonalMarketSources"
                FOR EACH ROW EXECUTE FUNCTION refuse_personal_market_source_update();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonalMarketSources");
            migrationBuilder.Sql("DROP FUNCTION refuse_personal_market_source_update();");
        }
    }
}
