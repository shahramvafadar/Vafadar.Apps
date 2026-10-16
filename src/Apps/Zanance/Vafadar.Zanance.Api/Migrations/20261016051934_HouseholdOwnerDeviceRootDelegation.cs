using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdOwnerDeviceRootDelegation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HouseholdOwnerRootGrants",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipVersion = table.Column<long>(type: "bigint", nullable: false),
                    RootFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Ciphertext = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    Digest = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdOwnerRootGrants", x => new { x.SpaceId, x.DeviceId });
                    table.ForeignKey(
                        name: "FK_HouseholdOwnerRootGrants_HouseholdDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "HouseholdDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdOwnerRootGrants_HouseholdSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "HouseholdSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdOwnerRootGrants_DeviceId",
                table: "HouseholdOwnerRootGrants",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseholdOwnerRootGrants");
        }
    }
}
