using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Api.Migrations
{
    /// <inheritdoc />
    public partial class PersonalSyncService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProtectedName = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncDevices_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyncSpaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ProtectedName = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    SchemaHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Cursor = table.Column<long>(type: "bigint", nullable: false),
                    Initializing = table.Column<bool>(type: "boolean", nullable: false),
                    BootstrapDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationOperationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncSpaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncSpaces_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyncBlobs",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    Parts = table.Column<int>(type: "integer", nullable: false),
                    Complete = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncBlobs", x => new { x.SpaceId, x.Hash });
                    table.ForeignKey(
                        name: "FK_SyncBlobs_SyncSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "SyncSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncEvents",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    TableName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecordKey = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ProtectedBody = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncEvents", x => new { x.SpaceId, x.Sequence });
                    table.ForeignKey(
                        name: "FK_SyncEvents_SyncSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "SyncSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncRecords",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TableName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecordKey = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    ProtectedBody = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncRecords", x => new { x.SpaceId, x.TableName, x.RecordKey });
                    table.ForeignKey(
                        name: "FK_SyncRecords_SyncSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "SyncSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemaHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Committed = table.Column<bool>(type: "boolean", nullable: false),
                    Abandoned = table.Column<bool>(type: "boolean", nullable: false),
                    CommitCursor = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CompletionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncRounds_SyncDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "SyncDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncRounds_SyncSpaces_SpaceId",
                        column: x => x.SpaceId,
                        principalTable: "SyncSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncBlobPieces",
                columns: table => new
                {
                    SpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlobHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Length = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncBlobPieces", x => new { x.SpaceId, x.BlobHash, x.Index });
                    table.ForeignKey(
                        name: "FK_SyncBlobPieces_SyncBlobs_SpaceId_BlobHash",
                        columns: x => new { x.SpaceId, x.BlobHash },
                        principalTable: "SyncBlobs",
                        principalColumns: new[] { "SpaceId", "Hash" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncParts",
                columns: table => new
                {
                    RoundId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Bytes = table.Column<int>(type: "integer", nullable: false),
                    Rows = table.Column<int>(type: "integer", nullable: false),
                    ProtectedBody = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncParts", x => new { x.RoundId, x.Index });
                    table.ForeignKey(
                        name: "FK_SyncParts_SyncRounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "SyncRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncBlobPieces_FileId",
                table: "SyncBlobPieces",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncDevices_SessionId",
                table: "SyncDevices",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncDevices_UserId_Revoked",
                table: "SyncDevices",
                columns: new[] { "UserId", "Revoked" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncEvents_SpaceId_TableName_RecordKey_Sequence",
                table: "SyncEvents",
                columns: new[] { "SpaceId", "TableName", "RecordKey", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncRounds_DeviceId",
                table: "SyncRounds",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRounds_SpaceId_DeviceId_OperationId",
                table: "SyncRounds",
                columns: new[] { "SpaceId", "DeviceId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncSpaces_UserId",
                table: "SyncSpaces",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncSpaces_UserId_CreationOperationId",
                table: "SyncSpaces",
                columns: new[] { "UserId", "CreationOperationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncBlobPieces");

            migrationBuilder.DropTable(
                name: "SyncEvents");

            migrationBuilder.DropTable(
                name: "SyncParts");

            migrationBuilder.DropTable(
                name: "SyncRecords");

            migrationBuilder.DropTable(
                name: "SyncBlobs");

            migrationBuilder.DropTable(
                name: "SyncRounds");

            migrationBuilder.DropTable(
                name: "SyncDevices");

            migrationBuilder.DropTable(
                name: "SyncSpaces");
        }
    }
}
