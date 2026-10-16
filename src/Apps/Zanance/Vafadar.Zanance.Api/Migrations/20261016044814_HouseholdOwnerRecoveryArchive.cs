using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdOwnerRecoveryArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SignedEnrollment",
                table: "HouseholdRecovery",
                type: "character varying(16384)",
                maxLength: 16384,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecoveryEvidenceJson",
                table: "HouseholdMembershipEvents",
                type: "character varying(16384)",
                maxLength: 16384,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HouseholdRecoveryEpochs",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Epoch = table.Column<long>(type: "bigint", nullable: false),
                    MembershipVersion = table.Column<long>(type: "bigint", nullable: false),
                    Ciphertext = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    Digest = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Bytes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdRecoveryEpochs", x => new { x.SpaceId, x.EnrollmentId, x.Epoch });
                    table.ForeignKey(
                        name: "FK_HouseholdRecoveryEpochs_HouseholdRecovery_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "HouseholdRecovery",
                        principalColumn: "SpaceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdRecoverySeeds",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SignedDeclaration = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RootCiphertext = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    ExpectedVersion = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedHead = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ThroughEpoch = table.Column<long>(type: "bigint", nullable: false),
                    UploadedThroughEpoch = table.Column<long>(type: "bigint", nullable: false),
                    StoredBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdRecoverySeeds", x => new { x.SpaceId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_HouseholdRecoverySeeds_HouseholdSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "HouseholdSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdRecoverySeedEpochs",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Epoch = table.Column<long>(type: "bigint", nullable: false),
                    MembershipVersion = table.Column<long>(type: "bigint", nullable: false),
                    Ciphertext = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    Digest = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Bytes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdRecoverySeedEpochs", x => new { x.SpaceId, x.OperationId, x.Epoch });
                    table.ForeignKey(
                        name: "FK_HouseholdRecoverySeedEpochs_HouseholdRecoverySeeds_SpaceId_~",
                        columns: x => new { x.SpaceId, x.OperationId },
                        principalTable: "HouseholdRecoverySeeds",
                        principalColumns: new[] { "SpaceId", "OperationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdRecoverySeeds_SpaceId",
                table: "HouseholdRecoverySeeds",
                column: "SpaceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseholdRecoveryEpochs");

            migrationBuilder.DropTable(
                name: "HouseholdRecoverySeedEpochs");

            migrationBuilder.DropTable(
                name: "HouseholdRecoverySeeds");

            migrationBuilder.DropColumn(
                name: "SignedEnrollment",
                table: "HouseholdRecovery");

            migrationBuilder.DropColumn(
                name: "RecoveryEvidenceJson",
                table: "HouseholdMembershipEvents");
        }
    }
}
