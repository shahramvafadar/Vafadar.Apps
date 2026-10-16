using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdBoundedOutboundCipher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BaseVersion",
                table: "HouseholdLocalAttemptParts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "CiphertextBytes",
                table: "HouseholdLocalAttemptParts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Digest",
                table: "HouseholdLocalAttemptParts",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecordId",
                table: "HouseholdLocalAttemptParts",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SerializedBytes",
                table: "HouseholdLocalAttemptParts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Derive lookup headers from retained original bytes; never reseal or replace an
            // interrupted publication. The bounded reader independently rechecks every header.
            migrationBuilder.Sql("""
                UPDATE "HouseholdLocalAttemptParts" SET
                    "RecordId" = json_extract("PartJson", '$.recordId'),
                    "BaseVersion" = json_extract("PartJson", '$.baseVersion'),
                    "Digest" = json_extract("PartJson", '$.digest'),
                    "CiphertextBytes" = length(json_extract("PartJson", '$.ciphertext')),
                    "SerializedBytes" = length(CAST("PartJson" AS BLOB));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseVersion",
                table: "HouseholdLocalAttemptParts");

            migrationBuilder.DropColumn(
                name: "CiphertextBytes",
                table: "HouseholdLocalAttemptParts");

            migrationBuilder.DropColumn(
                name: "Digest",
                table: "HouseholdLocalAttemptParts");

            migrationBuilder.DropColumn(
                name: "RecordId",
                table: "HouseholdLocalAttemptParts");

            migrationBuilder.DropColumn(
                name: "SerializedBytes",
                table: "HouseholdLocalAttemptParts");
        }
    }
}
