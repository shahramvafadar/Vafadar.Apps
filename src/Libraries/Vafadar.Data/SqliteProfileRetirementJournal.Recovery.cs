using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

public sealed partial class SqliteProfileRetirementJournal
{
    private const string RecoverySchema = "CREATE TABLE retirement_recovery (id INTEGER PRIMARY KEY CHECK(id=1), recovery TEXT NOT NULL, count INTEGER NOT NULL, digest TEXT NOT NULL)";

    /// <summary>Captures immutable authenticated recovery references before the reviewed catalogue removal.</summary>
    public async Task CaptureRecoveryAsync(ProfileRecoveryRetirementCapture capture, IDatabaseGenerationFileDurability durability, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(capture); ArgumentNullException.ThrowIfNull(durability);
        await using (var connection = await OpenAsync(false, ct).ConfigureAwait(false))
        {
            using var transaction = connection.BeginTransaction(deferred: false);
            if ((await ReadCoreAsync(connection, transaction, ct).ConfigureAwait(false)).Phase != ProfileRetirementPhase.Authorized)
                throw new InvalidOperationException("Recovery capture must precede catalogue removal.");
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "SELECT count(*) FROM sqlite_schema WHERE name='retirement_recovery'";
            if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) == 0)
            {
                command.CommandText = RecoverySchema + "; INSERT INTO retirement_recovery VALUES(1,$recovery,$count,$digest)";
                command.Parameters.AddWithValue("$recovery", capture.RecoveryId.ToString("N")); command.Parameters.AddWithValue("$count", capture.Count); command.Parameters.AddWithValue("$digest", capture.Digest);
                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            if (await ReadRecoveryCoreAsync(connection, transaction, ct).ConfigureAwait(false) != capture)
                throw new IOException("Recovery retirement capture cannot be replaced.");
            Check(ct); transaction.Commit();
        }
        await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false); await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false); Check(ct);
        if (await ReadRecoveryAsync(ct).ConfigureAwait(false) != capture) throw new IOException("Recovery capture readback differs.");
    }
    /// <summary>Reads only existing exact recovery capture; missing metadata cannot authorize an empty cleanup.</summary>
    public async Task<ProfileRecoveryRetirementCapture> ReadRecoveryAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(false, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: true);
        return await ReadRecoveryCoreAsync(connection, transaction, ct).ConfigureAwait(false);
    }
    /// <summary>Checks presence after strict journal validation; a configured host must separately refuse missing capture after publication.</summary>
    public async Task<bool> HasRecoveryInventoryAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(false, ct).ConfigureAwait(false);
        using var command = connection.CreateCommand(); command.CommandText = "SELECT count(*) FROM sqlite_schema WHERE name='retirement_recovery'";
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) == 1;
    }
    private async Task<ProfileRecoveryRetirementCapture> ReadRecoveryCoreAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken ct)
    {
        Check(ct); using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT sql FROM sqlite_schema WHERE name='retirement_recovery'";
        if (Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != RecoverySchema) throw new IOException("Recovery retirement capture is missing or modified.");
        command.CommandText = "SELECT id,recovery,count,digest FROM retirement_recovery";
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false) || reader.GetFieldType(0) != typeof(long) || reader.GetInt64(0) != 1
            || reader.GetFieldType(1) != typeof(string) || reader.GetFieldType(2) != typeof(long) || reader.GetFieldType(3) != typeof(string)
            || !Guid.TryParseExact(reader.GetString(1), "N", out var recovery) || recovery.ToString("N") != reader.GetString(1)
            || reader.GetInt64(2) is < 0 or > 4096) throw new IOException("Recovery retirement row differs.");
        ProfileRecoveryRetirementCapture result;
        try { result = new(recovery, checked((int)reader.GetInt64(2)), reader.GetString(3)); }
        catch (ArgumentException error) { throw new IOException("Recovery retirement fingerprint differs.", error); }
        if (await reader.ReadAsync(ct).ConfigureAwait(false)) throw new IOException("Recovery retirement row is not unique.");
        Check(ct); return result;
    }
}
