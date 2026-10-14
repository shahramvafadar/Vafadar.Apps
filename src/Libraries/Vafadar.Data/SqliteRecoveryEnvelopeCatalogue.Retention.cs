using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

public sealed partial class SqliteRecoveryEnvelopeCatalogue
{
    /// <summary>Captures one authenticated immutable mapping before durable native retention intent is published.</summary>
    public async Task<ProfileRecoveryRetirementCapture> CaptureRetentionBindingAsync(DatabaseKeyBinding binding,
        ReadOnlyMemory<byte> key, CancellationToken ct = default)
    {
        RequireBinding(binding); Check(ct);
        var rows = (await ReadRecoveryRowsAsync(ct).ConfigureAwait(false))
            .Where(row => row.Profile == binding.ProfileId && row.Generation == binding.GenerationId).ToArray();
        if (rows.Length != 1) throw new IOException("Referenced safety recovery mapping is missing.");
        Verify((rows[0].Envelope, rows[0].Confirmation), binding, key.Span);
        return CaptureRows(rows);
    }

    /// <summary>Removes only the exact captured single mapping after the host requalifies its durable unreferenced-key authority.</summary>
    /// <remarks>The callback must authenticate retained native ownership and exclude every financial/pending/safety reference.
    /// Repeated deletion after an interrupted native flush preserves all unrelated mappings.</remarks>
    public async Task RemoveRetentionBindingAsync(DatabaseKeyBinding binding, ProfileRecoveryRetirementCapture captured,
        Func<CancellationToken, Task> requireDurableUnreferencedAuthority, CancellationToken ct = default)
    {
        RequireBinding(binding); ArgumentNullException.ThrowIfNull(captured);
        ArgumentNullException.ThrowIfNull(requireDurableUnreferencedAuthority);
        if (captured.RecoveryId != identity.RecoveryId || captured.Count != 1)
            throw new IOException("Safety retention belongs to another recovery identity.");
        Check(ct); await requireDurableUnreferencedAuthority(ct).ConfigureAwait(false);
        ProfileRecoveryRetirementCapture retained;
        await using (var connection = await OpenAsync(path, false, ct).ConfigureAwait(false))
        {
            using var transaction = connection.BeginTransaction(deferred: false);
            var rows = await ReadRowsCoreAsync(connection, transaction, ct).ConfigureAwait(false);
            var target = rows.Where(row => row.Profile == binding.ProfileId && row.Generation == binding.GenerationId).ToArray();
            if (target.Length != 0 && CaptureRows(target) != captured)
                throw new IOException("Safety retention mapping changed after authentication.");
            retained = CaptureRows(rows.Where(row => row.Profile != binding.ProfileId || row.Generation != binding.GenerationId).ToArray());
            await requireDurableUnreferencedAuthority(ct).ConfigureAwait(false); Check(ct);
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "DELETE FROM recovery_envelope WHERE profile=$profile AND generation=$generation";
            Bind(command, binding);
            if (await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != target.Length)
                throw new IOException("Safety retention mapping removal differs.");
            transaction.Commit();
        }
        await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false);
        await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false); Check(ct);
        var remaining = await ReadRecoveryRowsAsync(ct).ConfigureAwait(false);
        if (remaining.Any(row => row.Profile == binding.ProfileId && row.Generation == binding.GenerationId) || CaptureRows(remaining) != retained)
            throw new IOException("Safety retention changed unrelated recovery mappings.");
    }
}
