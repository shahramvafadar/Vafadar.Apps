using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class AiConsentAndCredits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiCredits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrantKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAvailable = table.Column<long>(type: "bigint", nullable: false),
                    GiftedAvailable = table.Column<long>(type: "bigint", nullable: false),
                    PurchasedReserved = table.Column<long>(type: "bigint", nullable: false),
                    GiftedReserved = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCredits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiCredits_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    MaximumUnits = table.Column<long>(type: "bigint", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiOffers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadDigest = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProtectedPayload = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    PurchasedReserved = table.Column<long>(type: "bigint", nullable: false),
                    GiftedReserved = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    ChargedUnits = table.Column<long>(type: "bigint", nullable: false),
                    ProtectedText = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StatusCheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiRequests_AiOffers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "AiOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiCredits_RequestId_Kind",
                table: "AiCredits",
                columns: new[] { "RequestId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiCredits_UserId",
                table: "AiCredits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiCredits_UserId_GrantKey",
                table: "AiCredits",
                columns: new[] { "UserId", "GrantKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiOffers_UserId_ExpiresAt",
                table: "AiOffers",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiRequests_OfferId",
                table: "AiRequests",
                column: "OfferId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiRequests_UserId_CreatedAt",
                table: "AiRequests",
                columns: new[] { "UserId", "CreatedAt" });

            // D-298: immutable credits and literal consent are database invariants, including direct runtime-role SQL.
            migrationBuilder.Sql("""
                ALTER TABLE "AiCredits" ADD CONSTRAINT "CK_AiCredits_EventShape" CHECK (
                  ("Kind" = 0 AND "GrantKey" IS NOT NULL AND "RequestId" IS NULL AND "PurchasedAvailable" > 0 AND "GiftedAvailable" = 0 AND "PurchasedReserved" = 0 AND "GiftedReserved" = 0) OR
                  ("Kind" = 1 AND "GrantKey" IS NOT NULL AND "RequestId" IS NULL AND "GiftedAvailable" > 0 AND "PurchasedAvailable" = 0 AND "PurchasedReserved" = 0 AND "GiftedReserved" = 0) OR
                  ("Kind" = 2 AND "GrantKey" IS NULL AND "RequestId" IS NOT NULL AND "PurchasedAvailable" <= 0 AND "GiftedAvailable" <= 0 AND "PurchasedReserved" = -"PurchasedAvailable" AND "GiftedReserved" = -"GiftedAvailable" AND "PurchasedReserved" + "GiftedReserved" > 0) OR
                  ("Kind" = 3 AND "GrantKey" IS NULL AND "RequestId" IS NOT NULL AND "PurchasedAvailable" = 0 AND "GiftedAvailable" = 0 AND "PurchasedReserved" <= 0 AND "GiftedReserved" <= 0) OR
                  ("Kind" = 4 AND "GrantKey" IS NULL AND "RequestId" IS NOT NULL AND "PurchasedAvailable" >= 0 AND "GiftedAvailable" >= 0 AND "PurchasedReserved" = -"PurchasedAvailable" AND "GiftedReserved" = -"GiftedAvailable" AND "PurchasedAvailable" + "GiftedAvailable" > 0));
                ALTER TABLE "AiOffers" ADD CONSTRAINT "CK_AiOffers_Bounds" CHECK ("MaximumUnits" > 0 AND "MaximumUnits" <= 1000000000000 AND "Purpose" IN (0,1));
                ALTER TABLE "AiRequests" ADD CONSTRAINT "CK_AiRequests_Bounds" CHECK ("PurchasedReserved" >= 0 AND "GiftedReserved" >= 0 AND ("PurchasedReserved" + "GiftedReserved" BETWEEN 1 AND 1000000000000 OR ("State" = 3 AND "PurchasedReserved" = 0 AND "GiftedReserved" = 0)) AND "ChargedUnits" BETWEEN 0 AND "PurchasedReserved" + "GiftedReserved" AND (("State" = 0 AND "ChargedUnits" = 0 AND "ProtectedText" IS NULL) OR ("State" = 1 AND "ProtectedText" IS NOT NULL) OR ("State" IN (2,3) AND "ChargedUnits" = 0 AND "ProtectedText" IS NULL)));
                ALTER TABLE "AiCredits" ADD CONSTRAINT "FK_AiCredits_Request" FOREIGN KEY ("RequestId") REFERENCES "AiRequests"("Id") ON DELETE RESTRICT;
                CREATE FUNCTION zanance_ai_immutable() RETURNS trigger LANGUAGE plpgsql AS $body$
                BEGIN
                  RAISE EXCEPTION 'Original analysis credit or offer is immutable' USING ERRCODE = '23514';
                END $body$;
                CREATE TRIGGER "AiCredits_AppendOnly" BEFORE UPDATE OR DELETE ON "AiCredits" FOR EACH ROW EXECUTE FUNCTION zanance_ai_immutable();
                CREATE TRIGGER "AiOffers_Immutable" BEFORE UPDATE ON "AiOffers" FOR EACH ROW EXECUTE FUNCTION zanance_ai_immutable();
                CREATE FUNCTION zanance_ai_request_original() RETURNS trigger LANGUAGE plpgsql AS $body$
                BEGIN
                  IF ROW(NEW."Id",NEW."UserId",NEW."OfferId",NEW."PayloadDigest",NEW."ProtectedPayload",NEW."PurchasedReserved",NEW."GiftedReserved",NEW."CreatedAt") IS DISTINCT FROM
                     ROW(OLD."Id",OLD."UserId",OLD."OfferId",OLD."PayloadDigest",OLD."ProtectedPayload",OLD."PurchasedReserved",OLD."GiftedReserved",OLD."CreatedAt") OR OLD."State" <> 0 THEN
                    RAISE EXCEPTION 'Original analysis consent or terminal result is immutable' USING ERRCODE = '23514';
                  END IF;
                  RETURN NEW;
                END $body$;
                CREATE TRIGGER "AiRequests_Original" BEFORE UPDATE ON "AiRequests" FOR EACH ROW EXECUTE FUNCTION zanance_ai_request_original();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS \"AiRequests_Original\" ON \"AiRequests\"; DROP FUNCTION IF EXISTS zanance_ai_request_original(); DROP TRIGGER IF EXISTS \"AiCredits_AppendOnly\" ON \"AiCredits\"; DROP TRIGGER IF EXISTS \"AiOffers_Immutable\" ON \"AiOffers\"; DROP FUNCTION IF EXISTS zanance_ai_immutable();");

            migrationBuilder.DropTable(
                name: "AiCredits");

            migrationBuilder.DropTable(
                name: "AiRequests");

            migrationBuilder.DropTable(
                name: "AiOffers");
        }
    }
}
