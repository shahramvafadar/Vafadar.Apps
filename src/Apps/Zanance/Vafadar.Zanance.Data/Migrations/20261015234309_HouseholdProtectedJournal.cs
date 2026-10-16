using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class HouseholdProtectedJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HouseholdLocalAttemptParts",
                columns: table => new
                {
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    PartJson = table.Column<string>(type: "TEXT", maxLength: 2097152, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdLocalAttemptParts", x => x.Index);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdLocalAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    OperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AttemptId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Cutoff = table.Column<long>(type: "INTEGER", nullable: false),
                    CaptureHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OpenJson = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    CommitJson = table.Column<string>(type: "TEXT", maxLength: 131072, nullable: false),
                    FinancialCursor = table.Column<long>(type: "INTEGER", nullable: false),
                    Parts = table.Column<int>(type: "INTEGER", nullable: false),
                    RemoteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Committed = table.Column<bool>(type: "INTEGER", nullable: false),
                    Abandoned = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdLocalAttempts", x => x.Id);
                    table.CheckConstraint("CK_HouseholdLocalAttempt_Singleton", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "HouseholdLocalBinding",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    SetupOperation = table.Column<Guid>(type: "TEXT", nullable: false),
                    GenerationPath = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    PeerKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IdentityId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    OwnerCreation = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PinnedSigningKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    PinnedWrappingKey = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    SignedInvitation = table.Column<string>(type: "TEXT", maxLength: 131072, nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    MembershipVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    MembershipHead = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Cursor = table.Column<long>(type: "INTEGER", nullable: false),
                    FinancialCursor = table.Column<long>(type: "INTEGER", nullable: false),
                    PublicationHead = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SendingDisabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdLocalBinding", x => x.Id);
                    table.CheckConstraint("CK_HouseholdLocalBinding_Singleton", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "HouseholdLocalMemberships",
                columns: table => new
                {
                    Version = table.Column<long>(type: "INTEGER", nullable: false),
                    ProofJson = table.Column<string>(type: "TEXT", maxLength: 524288, nullable: false),
                    Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdLocalMemberships", x => x.Version);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdLocalPackets",
                columns: table => new
                {
                    Cursor = table.Column<long>(type: "INTEGER", nullable: false),
                    ReceiptJson = table.Column<string>(type: "TEXT", maxLength: 2097152, nullable: false),
                    Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdLocalPackets", x => x.Cursor);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdLocalSecrets",
                columns: table => new
                {
                    Slot = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    Frame = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdLocalSecrets", x => x.Slot);
                    table.CheckConstraint("CK_HouseholdLocalSecret_FrameBound", "length(\"Frame\") BETWEEN 1 AND 8192");
                });

            migrationBuilder.CreateTable(
                name: "HouseholdLocalEvents",
                columns: table => new
                {
                    Cursor = table.Column<long>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    EventJson = table.Column<string>(type: "TEXT", maxLength: 2097152, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdLocalEvents", x => new { x.Cursor, x.Index });
                    table.ForeignKey(
                        name: "FK_HouseholdLocalEvents_HouseholdLocalPackets_Cursor",
                        column: x => x.Cursor,
                        principalTable: "HouseholdLocalPackets",
                        principalColumn: "Cursor",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseholdLocalAttemptParts");

            migrationBuilder.DropTable(
                name: "HouseholdLocalAttempts");

            migrationBuilder.DropTable(
                name: "HouseholdLocalBinding");

            migrationBuilder.DropTable(
                name: "HouseholdLocalEvents");

            migrationBuilder.DropTable(
                name: "HouseholdLocalMemberships");

            migrationBuilder.DropTable(
                name: "HouseholdLocalSecrets");

            migrationBuilder.DropTable(
                name: "HouseholdLocalPackets");
        }
    }
}
