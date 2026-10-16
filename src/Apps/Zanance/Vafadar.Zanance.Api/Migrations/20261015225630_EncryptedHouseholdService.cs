using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class EncryptedHouseholdService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HouseholdSpaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SchemaHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreationOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerSigningKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    OwnerWrappingKey = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    MembershipVersion = table.Column<long>(type: "bigint", nullable: false),
                    MembershipHead = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyEpoch = table.Column<long>(type: "bigint", nullable: false),
                    Cursor = table.Column<long>(type: "bigint", nullable: false),
                    PublicationHead = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    HostExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    StorageRetired = table.Column<bool>(type: "boolean", nullable: false),
                    RotationRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdSpaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdSpaces_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CapabilityHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClaimedUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ClaimedFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Accepted = table.Column<bool>(type: "boolean", nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignedInvitation = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    SignedClaim = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: true),
                    ClaimOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClaimedDeviceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdInvitations_HouseholdSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "HouseholdSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdMembers",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Accepted = table.Column<bool>(type: "boolean", nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdMembers", x => new { x.SpaceId, x.UserId });
                    table.ForeignKey(
                        name: "FK_HouseholdMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdMembers_HouseholdSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "HouseholdSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdMembershipEvents",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    PreviousHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SignedEvent = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    SignerSigningKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SignerWrappingKey = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TransferAcceptance = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: true),
                    SignedAcceptanceClaim = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: true),
                    ActorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdMembershipEvents", x => new { x.SpaceId, x.Version });
                    table.ForeignKey(
                        name: "FK_HouseholdMembershipEvents_HouseholdSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "HouseholdSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdRecords",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    KeyEpoch = table.Column<long>(type: "bigint", nullable: false),
                    MembershipVersion = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ciphertext = table.Column<string>(type: "character varying(1048576)", maxLength: 1048576, nullable: false),
                    Digest = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Bytes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdRecords", x => new { x.SpaceId, x.RecordId });
                    table.ForeignKey(
                        name: "FK_HouseholdRecords_HouseholdSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "HouseholdSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdRecovery",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SigningKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    WrappingKey = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    MembershipVersion = table.Column<long>(type: "bigint", nullable: false),
                    Ciphertext = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                    Digest = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdRecovery", x => x.SpaceId);
                    table.ForeignKey(
                        name: "FK_HouseholdRecovery_HouseholdSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "HouseholdSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SigningKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    WrappingKey = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdDevices_HouseholdMembers_SpaceId_UserId",
                        columns: x => new { x.SpaceId, x.UserId },
                        principalTable: "HouseholdMembers",
                        principalColumns: new[] { "SpaceId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdKeyGrants",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Epoch = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipVersion = table.Column<long>(type: "bigint", nullable: false),
                    Ciphertext = table.Column<string>(type: "character varying(32768)", maxLength: 32768, nullable: false),
                    Digest = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdKeyGrants", x => new { x.SpaceId, x.Epoch, x.DeviceId });
                    table.ForeignKey(
                        name: "FK_HouseholdKeyGrants_HouseholdDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "HouseholdDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdKeyGrants_HouseholdSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "HouseholdSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedCursor = table.Column<long>(type: "bigint", nullable: false),
                    MembershipVersion = table.Column<long>(type: "bigint", nullable: false),
                    KeyEpoch = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CommittedCursor = table.Column<long>(type: "bigint", nullable: true),
                    SignedCommit = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: true),
                    Abandoned = table.Column<bool>(type: "boolean", nullable: false),
                    Acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdRounds_HouseholdDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "HouseholdDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdRounds_HouseholdSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "HouseholdSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdEvents",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cursor = table.Column<long>(type: "bigint", nullable: false),
                    RecordId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RoundId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    BaseVersion = table.Column<long>(type: "bigint", nullable: false),
                    Ciphertext = table.Column<string>(type: "character varying(1048576)", maxLength: 1048576, nullable: false),
                    Digest = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Bytes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdEvents", x => new { x.SpaceId, x.Cursor, x.RecordId });
                    table.ForeignKey(
                        name: "FK_HouseholdEvents_HouseholdRounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "HouseholdRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdParts",
                columns: table => new
                {
                    RoundId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    RecordId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BaseVersion = table.Column<long>(type: "bigint", nullable: false),
                    Ciphertext = table.Column<string>(type: "character varying(1048576)", maxLength: 1048576, nullable: false),
                    Digest = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Bytes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdParts", x => new { x.RoundId, x.Index });
                    table.ForeignKey(
                        name: "FK_HouseholdParts_HouseholdRounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "HouseholdRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdDevices_SpaceId_UserId_Fingerprint",
                table: "HouseholdDevices",
                columns: new[] { "SpaceId", "UserId", "Fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdEvents_RoundId",
                table: "HouseholdEvents",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdInvitations_SpaceId_OperationId",
                table: "HouseholdInvitations",
                columns: new[] { "SpaceId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdKeyGrants_DeviceId",
                table: "HouseholdKeyGrants",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMembers_SpaceId_Role",
                table: "HouseholdMembers",
                columns: new[] { "SpaceId", "Role" },
                unique: true,
                filter: "\"Role\" = 3 AND NOT \"Revoked\"");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMembers_UserId",
                table: "HouseholdMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMembershipEvents_SpaceId_OperationId",
                table: "HouseholdMembershipEvents",
                columns: new[] { "SpaceId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdParts_RoundId_RecordId",
                table: "HouseholdParts",
                columns: new[] { "RoundId", "RecordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdRounds_DeviceId",
                table: "HouseholdRounds",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdRounds_SpaceId_CommittedCursor",
                table: "HouseholdRounds",
                columns: new[] { "SpaceId", "CommittedCursor" });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdRounds_SpaceId_DeviceId_OperationId",
                table: "HouseholdRounds",
                columns: new[] { "SpaceId", "DeviceId", "OperationId" },
                unique: true,
                filter: "\"CommittedCursor\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdRounds_SpaceId_DeviceId_OperationId_AttemptId",
                table: "HouseholdRounds",
                columns: new[] { "SpaceId", "DeviceId", "OperationId", "AttemptId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdSpaces_OwnerId",
                table: "HouseholdSpaces",
                column: "OwnerId",
                unique: true,
                filter: "NOT \"Deleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdSpaces_OwnerId_CreationOperationId",
                table: "HouseholdSpaces",
                columns: new[] { "OwnerId", "CreationOperationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseholdEvents");

            migrationBuilder.DropTable(
                name: "HouseholdInvitations");

            migrationBuilder.DropTable(
                name: "HouseholdKeyGrants");

            migrationBuilder.DropTable(
                name: "HouseholdMembershipEvents");

            migrationBuilder.DropTable(
                name: "HouseholdParts");

            migrationBuilder.DropTable(
                name: "HouseholdRecords");

            migrationBuilder.DropTable(
                name: "HouseholdRecovery");

            migrationBuilder.DropTable(
                name: "HouseholdRounds");

            migrationBuilder.DropTable(
                name: "HouseholdDevices");

            migrationBuilder.DropTable(
                name: "HouseholdMembers");

            migrationBuilder.DropTable(
                name: "HouseholdSpaces");
        }
    }
}
