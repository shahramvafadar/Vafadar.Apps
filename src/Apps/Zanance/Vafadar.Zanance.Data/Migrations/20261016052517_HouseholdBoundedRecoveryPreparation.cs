using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdBoundedRecoveryPreparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_HouseholdLocalSecret_FrameBound",
                table: "HouseholdLocalSecrets");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HouseholdLocalSecret_FrameBound",
                table: "HouseholdLocalSecrets",
                sql: "length(\"Frame\") BETWEEN 1 AND CASE WHEN \"Slot\"='pending-recovery-preparation' THEN 131072 ELSE 8192 END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_HouseholdLocalSecret_FrameBound",
                table: "HouseholdLocalSecrets");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HouseholdLocalSecret_FrameBound",
                table: "HouseholdLocalSecrets",
                sql: "length(\"Frame\") BETWEEN 1 AND 8192");
        }
    }
}
