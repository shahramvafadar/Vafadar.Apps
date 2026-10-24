using Microsoft.Data.Sqlite;
using Vafadar.Data;

namespace Vafadar.Zanance.Performance;

/// <summary>Verifies complete workload preservation after explicitly accepted round-trip writes.</summary>
internal static class WorkloadSnapshotVerifier
{
    /// <summary>Allows only a validated opaque Home revision change; original snapshots remain immutable.</summary>
    internal static async Task VerifyAsync(SqliteConnection before, SqliteConnection after,
        bool acceptedWrites, CancellationToken cancellationToken)
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
        await SqliteSnapshotVerifier.VerifyAsync(normalized, after, cancellationToken);
        if (await ReadRevision(before, cancellationToken) != originalRevision)
            throw new InvalidDataException("Original workload snapshot was modified.");
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
