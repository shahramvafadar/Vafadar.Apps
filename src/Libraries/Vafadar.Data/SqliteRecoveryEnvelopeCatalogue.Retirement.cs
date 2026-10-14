using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace Vafadar.Data;

public sealed partial class SqliteRecoveryEnvelopeCatalogue
{
    /// <summary>Reads all existing canonical recovery bindings without unwrapping keys or inferring omitted profiles.</summary>
    public async Task<IReadOnlyList<DatabaseKeyBinding>> ReadBindingsAsync(CancellationToken ct = default) =>
        (await ReadRecoveryRowsAsync(ct).ConfigureAwait(false)).Select(row => new DatabaseKeyBinding(row.Profile, identity.InstallationId, row.Generation)).ToArray();

    /// <summary>Authenticates every target recovery mapping with its retained native key before capturing removal authority.</summary>
    public async Task CaptureRetirementAsync(ProfileRetirementCheckpoint request, SqliteProfileRetirementJournal journal,
        IDatabaseGenerationKeyStore keys, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(keys); RequireRetirement(request); Check(ct);
        if (request.Phase != ProfileRetirementPhase.Authorized || await journal.ReadAsync(ct).ConfigureAwait(false) != request)
            throw new InvalidOperationException("Recovery capture requires exact Authorized retirement.");
        var rows = await ReadRecoveryRowsAsync(ct).ConfigureAwait(false);
        var target = rows.Where(row => row.Profile == request.ProfileId).ToArray();
        foreach (var row in target)
        {
            var binding = new DatabaseKeyBinding(row.Profile, identity.InstallationId, row.Generation);
            var key = await keys.ReadAsync(binding, ct).ConfigureAwait(false);
            try { Verify((row.Envelope, row.Confirmation), binding, key); Check(ct); }
            finally { CryptographicOperations.ZeroMemory(key); }
        }
        var capture = CaptureRows(target);
        if (CaptureRows((await ReadRecoveryRowsAsync(ct).ConfigureAwait(false)).Where(row => row.Profile == request.ProfileId).ToArray()) != capture)
            throw new IOException("Recovery references changed during authentication.");
        if (await journal.ReadAsync(ct).ConfigureAwait(false) != request) throw new IOException("Retirement changed during recovery authentication.");
        await journal.CaptureRecoveryAsync(capture, durability, ct).ConfigureAwait(false);
    }

    /// <summary>Removes only the captured target mappings after catalogue removal and before native key files disappear.</summary>
    /// <remarks>A previously observable delete after failed synchronization is requalified without touching other profiles.
    /// New/changed target mappings refuse cleanup; absence is accepted only under existing immutable retirement authority.</remarks>
    public async Task RemoveRetiredAsync(ProfileRetirementCheckpoint request, SqliteProfileRetirementJournal journal, CancellationToken ct = default)
    {
        RequireRetirement(request); Check(ct);
        if (request.Phase != ProfileRetirementPhase.CatalogueRemoved || await journal.SynchronizeAndReadAsync(durability, ct).ConfigureAwait(false) != request)
            throw new InvalidOperationException("Recovery removal requires exact CatalogueRemoved retirement.");
        var captured = await journal.ReadRecoveryAsync(ct).ConfigureAwait(false);
        if (captured.RecoveryId != identity.RecoveryId) throw new IOException("Retirement belongs to another recovery generation.");
        ProfileRecoveryRetirementCapture retained;
        await using (var connection = await OpenAsync(path, false, ct).ConfigureAwait(false))
        {
            using var transaction = connection.BeginTransaction(deferred: false);
            var rows = await ReadRowsCoreAsync(connection, transaction, ct).ConfigureAwait(false);
            var target = rows.Where(row => row.Profile == request.ProfileId).ToArray();
            if (target.Length != 0 && CaptureRows(target) != captured)
                throw new IOException("Target recovery mappings differ from captured authority.");
            if (target.Length == 0 && captured.Count == 0 && CaptureRows(target) != captured)
                throw new IOException("Empty recovery capture differs.");
            retained = CaptureRows(rows.Where(row => row.Profile != request.ProfileId).ToArray());
            if (await journal.ReadAsync(ct).ConfigureAwait(false) != request) throw new IOException("Retirement changed before recovery removal.");
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "DELETE FROM recovery_envelope WHERE profile=$profile";
            command.Parameters.AddWithValue("$profile", request.ProfileId.ToString("N"));
            if (await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != target.Length) throw new IOException("Recovery removal differs.");
            Check(ct); transaction.Commit();
        }
        await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false); await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false); Check(ct);
        var reopened = await ReadRecoveryRowsAsync(ct).ConfigureAwait(false);
        if (reopened.Any(row => row.Profile == request.ProfileId) || CaptureRows(reopened) != retained)
            throw new IOException("Recovery removal changed retained mappings.");
        if (await journal.ReadAsync(ct).ConfigureAwait(false) != request) throw new IOException("Retirement changed during recovery synchronization.");
    }

    /// <summary>Requires exact captured recovery identity and actual target absence before accepting artifact/identity retirement.</summary>
    public async Task VerifyRetiredAsync(ProfileRetirementCheckpoint request, SqliteProfileRetirementJournal journal, CancellationToken ct = default)
    {
        RequireRetirement(request); Check(ct);
        var recorded = await journal.ReadAsync(ct).ConfigureAwait(false);
        if (recorded.ProfileId != request.ProfileId || recorded.InstallationId != request.InstallationId
            || recorded.OperationId != request.OperationId || recorded.LegacyProfileId != request.LegacyProfileId)
            throw new InvalidOperationException("Recovery verification belongs to another retirement.");
        var capture = await journal.ReadRecoveryAsync(ct).ConfigureAwait(false);
        if (capture.RecoveryId != identity.RecoveryId || (await ReadRecoveryRowsAsync(ct).ConfigureAwait(false)).Any(row => row.Profile == request.ProfileId))
            throw new IOException("Retired recovery references remain or differ.");
    }

    private void RequireRetirement(ProfileRetirementCheckpoint request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.InstallationId != identity.InstallationId) throw new InvalidOperationException("Retirement belongs to another recovery installation.");
    }
    private async Task<RecoveryRow[]> ReadRecoveryRowsAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(path, false, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: true);
        return await ReadRowsCoreAsync(connection, transaction, ct).ConfigureAwait(false);
    }
    private async Task<RecoveryRow[]> ReadRowsCoreAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken ct)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT profile,generation,length(envelope),length(confirmation),envelope,confirmation FROM recovery_envelope ORDER BY profile,generation LIMIT 4097";
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false); var rows = new List<RecoveryRow>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            if (rows.Count >= 4096 || reader.GetFieldType(0) != typeof(string) || reader.GetFieldType(1) != typeof(string)
                || reader.GetInt64(2) != 384 || reader.GetInt64(3) != 32 || reader.GetFieldType(4) != typeof(byte[]) || reader.GetFieldType(5) != typeof(byte[])
                || !Guid.TryParseExact(reader.GetString(0), "N", out var profile) || profile == Guid.Empty || profile.ToString("N") != reader.GetString(0)
                || !Guid.TryParseExact(reader.GetString(1), "N", out var generation) || generation == Guid.Empty || generation.ToString("N") != reader.GetString(1))
                throw new IOException("Recovery reference inventory bounds/types/identity differ.");
            rows.Add(new(profile, generation, reader.GetFieldValue<byte[]>(4), reader.GetFieldValue<byte[]>(5)));
        }
        Check(ct); return [.. rows];
    }
    private ProfileRecoveryRetirementCapture CaptureRows(IReadOnlyList<RecoveryRow> rows)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.ASCII.GetBytes(identity.InstallationId.ToString("N") + identity.RecoveryId.ToString("N")));
        hash.AppendData(identity.ExportPublicKey());
        foreach (var row in rows)
        {
            hash.AppendData(Encoding.ASCII.GetBytes(row.Profile.ToString("N") + row.Generation.ToString("N")));
            hash.AppendData(row.Envelope); hash.AppendData(row.Confirmation);
        }
        return new(identity.RecoveryId, rows.Count, Convert.ToHexStringLower(hash.GetHashAndReset()));
    }
    private sealed record RecoveryRow(Guid Profile, Guid Generation, byte[] Envelope, byte[] Confirmation);
}
