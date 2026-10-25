using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <inheritdoc />
    public partial class HomeLedgerReadBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // D-540: optional derived transport stays in the same encrypted file and writer.
            // A raw writer invalidates publication even when it bypasses managed maintenance.
            migrationBuilder.Sql("""
                CREATE TABLE HomeLedgerReadState (
                    Id INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
                    Revision TEXT NOT NULL CHECK (length(Revision) = 32),
                    PublishedRevision TEXT NULL,
                    SchemaVersion INTEGER NULL,
                    CodecVersion INTEGER NULL,
                    RowCount INTEGER NULL CHECK (RowCount >= 0)
                );
                INSERT INTO HomeLedgerReadState (Id, Revision) VALUES (1, lower(hex(randomblob(16))));
                CREATE TABLE HomeLedgerReadBlocks (
                    BlockId INTEGER NOT NULL PRIMARY KEY,
                    RowCount INTEGER NOT NULL CHECK (RowCount BETWEEN 1 AND 512),
                    Payload BLOB NULL,
                    Digest BLOB NULL CHECK (Digest IS NULL OR length(Digest) = 32)
                );
                CREATE TABLE HomeLedgerDirtyBlocks (BlockId INTEGER NOT NULL PRIMARY KEY);
                INSERT INTO HomeLedgerDirtyBlocks SELECT DISTINCT rowid >> 9 FROM Entries;
                """);
            foreach (var operation in new[] { "INSERT", "UPDATE", "DELETE" })
            {
                var oldBucket = operation == "INSERT" ? "" : "INSERT OR IGNORE INTO HomeLedgerDirtyBlocks VALUES (OLD.rowid >> 9);";
                var newBucket = operation == "DELETE" ? "" : "INSERT OR IGNORE INTO HomeLedgerDirtyBlocks VALUES (NEW.rowid >> 9);";
                migrationBuilder.Sql($"""
                    CREATE TRIGGER HomeLedgerReadBlocks_Entries_{operation} AFTER {operation} ON Entries
                    BEGIN
                        UPDATE HomeLedgerReadState SET Revision = lower(hex(randomblob(16))) WHERE Id = 1;
                        {oldBucket}
                        {newBucket}
                    END;
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var operation in new[] { "INSERT", "UPDATE", "DELETE" })
                migrationBuilder.Sql($"DROP TRIGGER HomeLedgerReadBlocks_Entries_{operation};");
            migrationBuilder.Sql("DROP TABLE HomeLedgerDirtyBlocks; DROP TABLE HomeLedgerReadBlocks; DROP TABLE HomeLedgerReadState;");
        }
    }
}
