using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Vafadar.Zanance.Api.Migrations;

/// <summary>Allows one bounded retirement of original analysis content for a pending account deletion.</summary>
[DbContext(typeof(ServiceDbContext))]
[Migration("20261121035847_AllowConfirmedAnalysisContentRetirement")]
public sealed class AllowConfirmedAnalysisContentRetirement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => ReplaceGuard(migrationBuilder, """
        IF OLD."State" = 1 AND NEW."ProtectedPayload" = '' AND NEW."ProtectedText" = '' AND
           (OLD."ProtectedPayload" <> '' OR OLD."ProtectedText" <> '') THEN
          retired := OLD;
          retired."ProtectedPayload" := '';
          retired."ProtectedText" := '';
          IF NEW IS NOT DISTINCT FROM retired AND
             EXISTS (SELECT 1 FROM "AccountDeletionRequests" WHERE "UserId" = OLD."UserId") AND
             EXISTS (SELECT 1 FROM "AiPilotDeletions" WHERE "RequestId" = OLD."Id" AND "ConfirmedAt" >= "StartedAt") THEN
            RETURN NEW;
          END IF;
        END IF;
        """, """DECLARE retired "AiRequests"%ROWTYPE;""");

    protected override void Down(MigrationBuilder migrationBuilder) => ReplaceGuard(migrationBuilder, "", "");

    private static void ReplaceGuard(MigrationBuilder migrationBuilder, string retirementGuard, string declaration) =>
        migrationBuilder.Sql($"""
            CREATE OR REPLACE FUNCTION zanance_ai_request_original() RETURNS trigger LANGUAGE plpgsql AS $body$
            {declaration}
            BEGIN
              {retirementGuard}
              IF ROW(NEW."Id",NEW."UserId",NEW."OfferId",NEW."PayloadDigest",NEW."ProtectedPayload",NEW."PurchasedReserved",NEW."GiftedReserved",NEW."CreatedAt") IS DISTINCT FROM
                 ROW(OLD."Id",OLD."UserId",OLD."OfferId",OLD."PayloadDigest",OLD."ProtectedPayload",OLD."PurchasedReserved",OLD."GiftedReserved",OLD."CreatedAt") OR OLD."State" <> 0 THEN
                RAISE EXCEPTION 'Original analysis consent or terminal result is immutable' USING ERRCODE = '23514';
              END IF;
              RETURN NEW;
            END $body$;
            """);
}
