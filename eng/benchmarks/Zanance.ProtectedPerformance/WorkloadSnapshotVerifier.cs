using Microsoft.Data.Sqlite;
using Vafadar.Data;

namespace Vafadar.Zanance.Performance;

/// <summary>Verifies complete workload preservation after explicitly accepted round-trip writes.</summary>
internal static class WorkloadSnapshotVerifier
{
    /// <summary>Actual protected-source schema cookies captured around the original workload, never inferred from portable metadata.</summary>
    internal sealed record SourceSchemaCookies(long Before, long After);

    /// <summary>Allows only a validated opaque Home revision change; original snapshots remain immutable.</summary>
    internal static async Task VerifyAsync(SqliteConnection before, SqliteConnection after,
        bool acceptedWrites, CancellationToken cancellationToken, SourceSchemaCookies? sourceSchemaCookies = null)
    {
        var originalRevision = await ReadRevision(before, cancellationToken);
        var currentRevision = await ReadRevision(after, cancellationToken);
        var revisionChanged = originalRevision != currentRevision;
        if (revisionChanged != acceptedWrites)
            throw new InvalidDataException("Workload revision does not match its accepted-write boundary.");
        if (!acceptedWrites)
        {
            await SqliteSnapshotVerifier.VerifyAsync(before, after, cancellationToken);
            return;
        }

        // D-523: Save/Delete deliberately invalidates Home even when every financial row returns
        // to its original value. Normalize only this one token in a disposable memory copy;
        // the production verifier still compares every schema/header/typed value and trigger.
        await using var normalized = new SqliteConnection("Data Source=:memory:;Pooling=False");
        await normalized.OpenAsync(cancellationToken);
        before.BackupDatabase(normalized);
        await using var update = normalized.CreateCommand();
        update.CommandText = "UPDATE HomeFinancialRevision SET Revision=$current WHERE Id=1 AND Revision=$original;";
        update.Parameters.AddWithValue("$current", currentRevision);
        update.Parameters.AddWithValue("$original", originalRevision);
        if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new InvalidDataException("Workload revision normalization must affect exactly one owned memory row.");
        // D-562: an accepted insert/delete round trip also advances the ledger cache's opaque
        // tokens. Normalize only those identities; payloads, buckets, counts and schema remain exact.
        var originalLedger = await ReadLedgerTokens(before, cancellationToken);
        var currentLedger = await ReadLedgerTokens(after, cancellationToken);
        if ((originalLedger is null) != (currentLedger is null))
            throw new InvalidDataException("Derived workload schema presence changed.");
        if (originalLedger is not null && currentLedger is not null)
        {
            // Physical protected-copy publication can leave the old derived cookie stale. An accepted
            // writer refresh is allowed only with independently captured unchanged source cookies.
            var comparedCookie = originalLedger.SchemaCookie;
            if (sourceSchemaCookies is not null)
            {
                if (sourceSchemaCookies.Before is < 0 or > int.MaxValue
                    || sourceSchemaCookies.Before != sourceSchemaCookies.After
                    || currentLedger.SchemaCookie != sourceSchemaCookies.After)
                    throw new InvalidDataException("Derived refresh must match the unchanged actual protected schema cookie.");
                comparedCookie = sourceSchemaCookies.After;
            }
            await using var ledger = normalized.CreateCommand();
            ledger.CommandText = """
                UPDATE HomeLedgerReadState SET Revision=$current,PublishedRevision=$published,SchemaVersion=$schema
                WHERE Id=1 AND Revision=$original AND PublishedRevision IS $originalPublished AND SchemaVersion IS $originalSchema;
                """;
            ledger.Parameters.AddWithValue("$current", currentLedger.Revision);
            ledger.Parameters.AddWithValue("$published", (object?)currentLedger.Published ?? DBNull.Value);
            ledger.Parameters.AddWithValue("$original", originalLedger.Revision);
            ledger.Parameters.AddWithValue("$originalPublished", (object?)originalLedger.Published ?? DBNull.Value);
            ledger.Parameters.AddWithValue("$schema", (object?)comparedCookie ?? DBNull.Value);
            ledger.Parameters.AddWithValue("$originalSchema", (object?)originalLedger.SchemaCookie ?? DBNull.Value);
            if (await ledger.ExecuteNonQueryAsync(cancellationToken) != 1)
                throw new InvalidDataException("Exactly one original derived memory identity is required.");
        }
        await SqliteSnapshotVerifier.VerifyAsync(normalized, after, cancellationToken);
        if (await ReadRevision(before, cancellationToken) != originalRevision
            || await ReadLedgerTokens(before, cancellationToken) != originalLedger)
            throw new InvalidDataException("Original workload snapshot was modified.");
    }

    private sealed record LedgerTokens(string Revision, string? Published, long? SchemaCookie);

    /// <summary>Requires exact singleton token storage without reading or changing financial rows.</summary>
    private static async Task<LedgerTokens?> ReadLedgerTokens(SqliteConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='HomeLedgerReadState';";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture) == 0) return null;
        command.CommandText = "SELECT typeof(Id),Id,typeof(Revision),Revision,typeof(PublishedRevision),PublishedRevision,typeof(SchemaVersion),SchemaVersion FROM HomeLedgerReadState;";
        await using var rows = await command.ExecuteReaderAsync(ct);
        if (!await rows.ReadAsync(ct) || rows.GetString(0) != "integer" || rows.GetInt64(1) != 1
            || rows.GetString(2) != "text" || rows.GetString(4) is not ("text" or "null")
            || rows.GetString(6) is not ("integer" or "null"))
            throw new InvalidDataException("Derived workload singleton identity or storage type differs.");
        var revision = rows.GetString(3);
        var published = rows.IsDBNull(5) ? null : rows.GetString(5);
        var schemaCookie = rows.IsDBNull(7) ? (long?)null : rows.GetInt64(7);
        if (revision.Length != 32 || revision.Any(c => !char.IsAsciiHexDigitLower(c))
            || published is not null && (published.Length != 32 || published.Any(c => !char.IsAsciiHexDigitLower(c)))
            || schemaCookie is < 0 or > int.MaxValue || await rows.ReadAsync(ct))
            throw new InvalidDataException("Derived workload revisions must remain bounded opaque tokens.");
        return new LedgerTokens(revision, published, schemaCookie);
    }

    /// <summary>Requires the original singleton integer identity and a complete lowercase hexadecimal token.</summary>
    private static async Task<string> ReadRevision(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT typeof(Id),Id,typeof(Revision),Revision FROM HomeFinancialRevision;";
        await using var rows = await command.ExecuteReaderAsync(cancellationToken);
        if (!await rows.ReadAsync(cancellationToken) || rows.GetString(0) != "integer" || rows.GetInt64(1) != 1
            || rows.GetString(2) != "text")
            throw new InvalidDataException("Workload revision identity or storage type differs.");
        var revision = rows.GetString(3);
        if (revision.Length != 32 || revision.Any(value => !char.IsAsciiHexDigitLower(value))
            || await rows.ReadAsync(cancellationToken))
            throw new InvalidDataException("Workload revision must remain one well-formed opaque token.");
        return revision;
    }
}
