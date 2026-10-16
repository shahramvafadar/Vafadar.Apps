using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdDeviceSessionProofs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GrantsHash",
                table: "HouseholdMembershipEvents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "AdmissionOperationId",
                table: "HouseholdDevices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdmissionProof",
                table: "HouseholdDevices",
                type: "character varying(65536)",
                maxLength: 65536,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SessionOperationId",
                table: "HouseholdDevices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionProof",
                table: "HouseholdDevices",
                type: "character varying(65536)",
                maxLength: 65536,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrantsHash",
                table: "HouseholdMembershipEvents");

            migrationBuilder.DropColumn(
                name: "AdmissionOperationId",
                table: "HouseholdDevices");

            migrationBuilder.DropColumn(
                name: "AdmissionProof",
                table: "HouseholdDevices");

            migrationBuilder.DropColumn(
                name: "SessionOperationId",
                table: "HouseholdDevices");

            migrationBuilder.DropColumn(
                name: "SessionProof",
                table: "HouseholdDevices");
        }
    }
}
