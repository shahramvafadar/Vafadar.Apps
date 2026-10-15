using Microsoft.Data.Sqlite;
using Vafadar.Data;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>Portable packages contain finances, never a device's sync identity, cursor, queue, staging or reviews.</summary>
internal sealed class PersonalSyncPortablePolicy : ISqlitePortableSnapshotPolicy
{
    /// <inheritdoc />
    public async Task PrepareAsync(SqliteConnection connection, string schema, CancellationToken cancellationToken)
    {
        if (schema != "main" && (schema.Length != 41 || !schema.StartsWith("portable_", StringComparison.Ordinal) ||
            !Guid.TryParseExact(schema[9..], "N", out _)))
            throw new InvalidOperationException("Only the exporter-owned portable schema can be sanitized.");
        var prefix = "\"" + schema + "\".";
        // These are ordinary tables, not virtual/shadow tables. ON clears deleted payload/index bytes
        // in the freshly exported logical memory image; DELETE alone would leave private free-page data.
        await using (var secure = connection.CreateCommand())
        { secure.CommandText = "PRAGMA " + prefix + "secure_delete=ON;"; await secure.ExecuteNonQueryAsync(cancellationToken); }
        foreach (var table in PersonalSyncStore.PortableMetadataTables)
        {
            await using var exists = connection.CreateCommand(); exists.CommandText = "SELECT 1 FROM " + prefix + "sqlite_schema WHERE type='table' AND name=$name;";
            exists.Parameters.AddWithValue("$name", table);
            if (await exists.ExecuteScalarAsync(cancellationToken) is null) continue;
            await using var clear = connection.CreateCommand(); clear.CommandText = "DELETE FROM " + prefix + "\"" + table + "\";";
            await clear.ExecuteNonQueryAsync(cancellationToken);
        }
        await using var sequence = connection.CreateCommand();
        sequence.CommandText = "SELECT 1 FROM " + prefix + "sqlite_schema WHERE type='table' AND name='sqlite_sequence';";
        if (await sequence.ExecuteScalarAsync(cancellationToken) is null) return;
        sequence.CommandText = "DELETE FROM " + prefix + "sqlite_sequence WHERE name='SyncOutbox';";
        await sequence.ExecuteNonQueryAsync(cancellationToken);
    }
}
