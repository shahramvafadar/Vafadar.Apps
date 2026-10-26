using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

#nullable disable

namespace Vafadar.Zanance.Data.Migrations
{
    /// <summary>Prevents caller and foreign-key conflict policies from rejecting derived dirty-block markers.</summary>
    public partial class HomeLedgerDirtyBlockConflictGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var operation in new[] { "INSERT", "UPDATE", "DELETE" })
            {
                migrationBuilder.Sql($"DROP TRIGGER HomeLedgerReadBlocks_Entries_{operation};");
                migrationBuilder.Sql(TriggerSql(operation));
            }
        }

        /// <summary>Frozen corrected trigger definition, also required by the complete-row reader's schema guard.</summary>
        internal static string TriggerSql(string operation)
        {
            if (operation is not ("INSERT" or "UPDATE" or "DELETE"))
                throw new ArgumentOutOfRangeException(nameof(operation));
            // D-562: an outer OR ABORT/FAIL/ROLLBACK or a foreign-key action can override a
            // trigger's OR IGNORE. Avoid the duplicate altogether inside the same SQLite writer.
            static string Marker(string row) => $"""
                INSERT INTO HomeLedgerDirtyBlocks(BlockId)
                SELECT {row}.rowid >> 9
                WHERE NOT EXISTS (SELECT 1 FROM HomeLedgerDirtyBlocks WHERE BlockId = ({row}.rowid >> 9));
                """;
            var oldBucket = operation == "INSERT" ? "" : Marker("OLD");
            var newBucket = operation == "DELETE" ? "" : Marker("NEW");
            return $"""
                CREATE TRIGGER HomeLedgerReadBlocks_Entries_{operation} AFTER {operation} ON Entries
                BEGIN
                    UPDATE HomeLedgerReadState SET Revision = lower(hex(randomblob(16))) WHERE Id = 1;
                    {oldBucket}
                    {newBucket}
                END;
                """;
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var operation in new[] { "INSERT", "UPDATE", "DELETE" })
                migrationBuilder.Sql($"DROP TRIGGER HomeLedgerReadBlocks_Entries_{operation};");
            // Restore the exact published historical definitions, without rebuilding any source table.
            foreach (var statement in new HomeLedgerReadBlocks().UpOperations.OfType<SqlOperation>())
                if (statement.Sql.TrimStart().StartsWith("CREATE TRIGGER ", StringComparison.Ordinal))
                    migrationBuilder.Sql(statement.Sql);
        }
    }
}
