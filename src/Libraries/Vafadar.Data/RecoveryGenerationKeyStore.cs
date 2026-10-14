using System.Security.Cryptography;

namespace Vafadar.Data;

/// <summary>Protects each explicitly prepared native generation for optional recovery before its caller advances readiness.</summary>
/// <remarks>The public catalogue is supplied only after explicit recovery enablement. Ordinary reads still require
/// actual device keys; they never silently fall back to recovery or request its secret. All returned keys remain caller-owned.</remarks>
public sealed class RecoveryGenerationKeyStore(IDatabaseGenerationKeyStore inner, SqliteRecoveryEnvelopeCatalogue recovery) : IDatabaseGenerationKeyStore
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
}
