using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdVerifiedCipherHeaders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BaseVersion",
                table: "HouseholdLocalEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Digest",
                table: "HouseholdLocalEvents",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecordId",
                table: "HouseholdLocalEvents",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SerializedBytes",
                table: "HouseholdLocalEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // These original event strings were already retained inside captured protected storage.
            // This only derives its bounded lookup columns; complete replay still authenticates the
            // original author/manifest/ciphertext before any financial state can advance.
            migrationBuilder.Sql("""
                UPDATE "HouseholdLocalEvents"
                SET "RecordId" = json_extract("EventJson", '$.recordId'),
                    "BaseVersion" = json_extract("EventJson", '$.baseVersion'),
                    "Digest" = json_extract("EventJson", '$.digest'),
                    "SerializedBytes" = length(CAST("EventJson" AS BLOB));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseVersion",
                table: "HouseholdLocalEvents");

            migrationBuilder.DropColumn(
                name: "Digest",
                table: "HouseholdLocalEvents");

            migrationBuilder.DropColumn(
                name: "RecordId",
                table: "HouseholdLocalEvents");

            migrationBuilder.DropColumn(
                name: "SerializedBytes",
                table: "HouseholdLocalEvents");
        }
    }
}
