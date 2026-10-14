using System.Security.Cryptography;
using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Keeps the native device key authoritative while optionally capturing newly prepared keys for recovery.</summary>
/// <remarks>Enabling recovery supplies public metadata only; ordinary reads never use recovery secrets or prepare keys.</remarks>
internal sealed class AndroidRecoveryKeyStore(AndroidGenerationKeyStore native) : IDatabaseGenerationKeyStore, IDatabaseGenerationKeyRecovery
{
    private SqliteRecoveryEnvelopeCatalogue? recovery;

    /// <summary>Attaches only a fully validated, explicitly enabled public catalogue under the host's maintenance owner.</summary>
    internal void Attach(SqliteRecoveryEnvelopeCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        if (recovery is not null && recovery.Identity.RecoveryId != catalogue.Identity.RecoveryId)
            throw new InvalidOperationException("Changing recovery identity requires explicit enrolled-key migration.");
        recovery = catalogue;
    }

    /// <inheritdoc />
    public async Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct)
    {
        await native.PrepareAsync(binding, ct).ConfigureAwait(false);
        if (recovery is not { } catalogue) return;
        var key = await native.ReadAsync(binding, ct).ConfigureAwait(false);
        try { await catalogue.CaptureAsync(binding, key, ct).ConfigureAwait(false); }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    /// <inheritdoc />
    public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => native.ReadAsync(binding, ct);

    /// <inheritdoc />
    public Task ImportRecoveredAsync(DatabaseKeyBinding binding, ReadOnlyMemory<byte> recoveredKey,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> verifyExistingGeneration, CancellationToken ct) =>
        native.ImportRecoveredAsync(binding, recoveredKey, async (owned, token) =>
        {
            await verifyExistingGeneration(owned, token).ConfigureAwait(false);
            if (recovery is { } catalogue) await catalogue.CaptureAsync(binding, owned, token).ConfigureAwait(false);
        }, ct);
}
