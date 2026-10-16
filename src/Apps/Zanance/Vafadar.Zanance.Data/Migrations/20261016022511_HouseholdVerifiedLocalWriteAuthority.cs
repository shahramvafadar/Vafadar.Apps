using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdVerifiedLocalWriteAuthority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AccessObservedAtUnixSeconds",
                table: "HouseholdLocalBinding",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccessRetention",
                table: "HouseholdLocalBinding",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "HostExpiresAtUnixSeconds",
                table: "HouseholdLocalBinding",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RemoteAccessRefused",
                table: "HouseholdLocalBinding",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RotationRequired",
                table: "HouseholdLocalBinding",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
            migrationBuilder.Sql(ReadBoundary("Vafadar.Zanance.Household.WriteUp.v1.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReadBoundary("Vafadar.Zanance.Household.WriteDown.v1.sql"));
            migrationBuilder.DropColumn(
                name: "AccessObservedAtUnixSeconds",
                table: "HouseholdLocalBinding");

            migrationBuilder.DropColumn(
                name: "AccessRetention",
                table: "HouseholdLocalBinding");

            migrationBuilder.DropColumn(
                name: "HostExpiresAtUnixSeconds",
                table: "HouseholdLocalBinding");

            migrationBuilder.DropColumn(
                name: "RemoteAccessRefused",
                table: "HouseholdLocalBinding");

            migrationBuilder.DropColumn(
                name: "RotationRequired",
                table: "HouseholdLocalBinding");
        }

        /// <summary>Frozen original financial-table boundary; later model changes require an additive reviewed migration.</summary>
        private static string ReadBoundary(string name)
        {
            using var resource = typeof(HouseholdVerifiedLocalWriteAuthority).Assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException("The original household financial-write boundary is missing.");
            using var reader = new StreamReader(resource); return reader.ReadToEnd();
        }
    }
}
