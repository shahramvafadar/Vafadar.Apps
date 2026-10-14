using System.Security.Cryptography;

namespace Vafadar.Data;

/// <summary>Protects each explicitly prepared native generation for optional recovery before its caller advances readiness.</summary>
/// <remarks>The public catalogue is supplied only after explicit recovery enablement. Ordinary reads still require
/// actual device keys; they never silently fall back to recovery or request its secret. All returned keys remain caller-owned.</remarks>
public sealed class RecoveryGenerationKeyStore(IDatabaseGenerationKeyStore inner, SqliteRecoveryEnvelopeCatalogue recovery) : IDatabaseGenerationKeyStore, IDatabaseGenerationKeyRecovery
{
    /// <inheritdoc />
    public async Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct)
    {
        await inner.PrepareAsync(binding, ct).ConfigureAwait(false);
        var key = await inner.ReadAsync(binding, ct).ConfigureAwait(false);
        try { await recovery.CaptureAsync(binding, key, ct).ConfigureAwait(false); }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
    /// <inheritdoc />
    public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
    /// <inheritdoc />
    public async Task ImportRecoveredAsync(DatabaseKeyBinding binding, ReadOnlyMemory<byte> key,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> verifyExistingGeneration, CancellationToken ct)
    {
        if (inner is not IDatabaseGenerationKeyRecovery importer) throw new InvalidOperationException("Native recovery import is unavailable.");
        await importer.ImportRecoveredAsync(binding, key, async (owned, token) =>
        {
            await verifyExistingGeneration(owned, token).ConfigureAwait(false);
            await recovery.CaptureAsync(binding, owned, token).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);
    }
}
