using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersonalSyncJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncBinding",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IdentityId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    PeerKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RemoteName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    GenerationPath = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    SchemaHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Applying = table.Column<bool>(type: "INTEGER", nullable: false),
                    Cursor = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSuccessAt = table.Column<long>(type: "INTEGER", nullable: true),
                    LastFailure = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncBinding", x => x.Id);
                    table.CheckConstraint("CK_SyncBinding_Singleton", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "SyncBlobParts",
                columns: table => new
                {
                    Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Part = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncBlobParts", x => new { x.Hash, x.Part });
                });

            migrationBuilder.CreateTable(
                name: "SyncBlobTransfers",
                columns: table => new
                {
                    Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Length = table.Column<long>(type: "INTEGER", nullable: false),
                    Manifest = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncBlobTransfers", x => x.Hash);
                });

            migrationBuilder.CreateTable(
                name: "SyncClientRound",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    OperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RemoteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Cutoff = table.Column<long>(type: "INTEGER", nullable: false),
                    Phase = table.Column<int>(type: "INTEGER", nullable: false),
                    PartCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SentParts = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ThroughCursor = table.Column<long>(type: "INTEGER", nullable: true),
                    ReadCursor = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncClientRound", x => x.Id);
                    table.CheckConstraint("CK_SyncClientRound_Singleton", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "SyncConflicts",
                columns: table => new
                {
                    TableName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RecordKey = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    LocalSequence = table.Column<long>(type: "INTEGER", nullable: false),
                    UseProposal = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncConflicts", x => new { x.TableName, x.RecordKey });
                });

            migrationBuilder.CreateTable(
                name: "SyncIncoming",
                columns: table => new
                {
                    TableName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RecordKey = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncIncoming", x => new { x.TableName, x.RecordKey });
                });

            migrationBuilder.CreateTable(
                name: "SyncKnown",
                columns: table => new
                {
                    TableName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RecordKey = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncKnown", x => new { x.TableName, x.RecordKey });
                });

            migrationBuilder.CreateTable(
                name: "SyncOutbox",
                columns: table => new
                {
                    Sequence = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TableName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    RecordKey = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncOutbox", x => x.Sequence);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncOutbox_TableName_RecordKey_Sequence",
                table: "SyncOutbox",
                columns: new[] { "TableName", "RecordKey", "Sequence" });

            // Replay this migration's frozen contract, never a future runtime model's changed columns.
            migrationBuilder.Sql(ReadCaptureSql("Vafadar.Zanance.Sync.JournalUp.v1.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReadCaptureSql("Vafadar.Zanance.Sync.JournalDown.v1.sql"));
            migrationBuilder.DropTable(
                name: "SyncBinding");

            migrationBuilder.DropTable(
                name: "SyncBlobParts");

            migrationBuilder.DropTable(
                name: "SyncBlobTransfers");

            migrationBuilder.DropTable(
                name: "SyncClientRound");

            migrationBuilder.DropTable(
                name: "SyncConflicts");

            migrationBuilder.DropTable(
                name: "SyncIncoming");

            migrationBuilder.DropTable(
                name: "SyncKnown");

            migrationBuilder.DropTable(
                name: "SyncOutbox");
        }

        private static string ReadCaptureSql(string name)
        {
            using var resource = typeof(PersonalSyncJournal).Assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException("Frozen personal sync capture contract is missing.");
            using var reader = new System.IO.StreamReader(resource);
            return reader.ReadToEnd();
        }
    }
}
