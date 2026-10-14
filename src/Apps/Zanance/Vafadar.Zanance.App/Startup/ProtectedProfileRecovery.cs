using System.Security.Cryptography;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Startup;

/// <summary>Verifies and re-enrolls every captured profile recovery binding while actual startup remains covered.</summary>
/// <remarks>The final host supplies explicit app/device authorization and complete native reference verification,
/// including retained database/preference/safety generations. This service never guesses file ownership, creates
/// profiles, changes preferences, publishes a session or silently falls back from missing recovery authority.</remarks>
public sealed class ProtectedProfileRecovery(LocalDatabaseLocation<ZananceDbContext> location,
    ProfileService profiles, string identityPath,
    Func<ReadOnlyMemory<byte>, CancellationToken, Task<DatabaseRecoveryMaterial>> unlockMaterial,
    SqliteRecoveryEnvelopeCatalogue catalogue,
    IDatabaseGenerationKeyRecovery native,
    Func<DatabaseKeyBinding, string, ReadOnlyMemory<byte>, DatabaseAccessGate.ExclusiveLease, CancellationToken, Task> verifyReferences)
{
    private readonly SemaphoreSlim operations = new(1, 1);

    /// <summary>Unlocks existing optional material, preflights the complete captured inventory and imports the same verified keys.</summary>
    public async Task RecoverAsync(ReadOnlyMemory<byte> recoveryKey, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct = default)
    {
        await operations.WaitAsync(ct).ConfigureAwait(false);
        var retained = new List<(DatabaseKeyBinding Binding, string Original, byte[] Key)>();
        try
        {
            location.Access.RequireDrainedOwner(owner); ct.ThrowIfCancellationRequested();
            if (location.IsProtected) throw new InvalidOperationException("Recovery requires the covered unpublished startup endpoint.");
            var storage = profiles.CaptureStorage(); storage.Validate();
            if (location.Path != storage.CurrentOriginalPath) throw new IOException("Recovery endpoint differs from the captured selected original.");
            var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate);
            var rows = await identities.ReadAsync(ct).ConfigureAwait(false);
            if (!rows.Select(row => row.LegacyProfileId).Order(StringComparer.Ordinal).SequenceEqual(storage.OriginalPaths.Keys.Order(StringComparer.Ordinal))
                || rows.Any(row => row.InstallationId != catalogue.Identity.InstallationId))
                throw new IOException("Recovery identity catalogue differs from the captured original profiles.");
            var bindings = await catalogue.ReadBindingsAsync(ct).ConfigureAwait(false);
            if (rows.Any(row => !bindings.Contains(row.InitialCandidateKey)))
                throw new IOException("Complete profile recovery inventory is missing an original binding.");
            using var material = await unlockMaterial(recoveryKey, ct).ConfigureAwait(false);
            foreach (var binding in bindings)
            {
                Check(); var profile = rows.SingleOrDefault(row => row.ProfileId == binding.ProfileId)
                    ?? throw new IOException("Recovery mapping belongs to an unknown profile.");
                var original = storage.OriginalPaths[profile.LegacyProfileId];
                var key = await catalogue.RecoverAsync(binding, material, ct).ConfigureAwait(false);
                try
                {
                    await verifyReferences(binding, original, key, owner, ct).ConfigureAwait(false); Check();
                    retained.Add((binding, original, key)); key = [];
                }
                finally { CryptographicOperations.ZeroMemory(key); }
            }
            await RecheckAsync().ConfigureAwait(false);
            // All mappings/references are verified before the first native write. Interrupted imports
            // retain immutable entries; a retry verifies the same catalogue and native objects again.
            foreach (var entry in retained)
            {
                await RecheckAsync().ConfigureAwait(false);
                await native.ImportRecoveredAsync(entry.Binding, entry.Key, async (owned, token) =>
                {
                    await RecheckAsync().ConfigureAwait(false);
                    await verifyReferences(entry.Binding, entry.Original, owned, owner, token).ConfigureAwait(false);
                    Check();
                }, ct).ConfigureAwait(false);
            }
            await RecheckAsync().ConfigureAwait(false);

            void Check()
            { ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner); storage.Validate(); if (location.IsProtected) throw new IOException("Startup publication changed during recovery."); }
            async Task RecheckAsync()
            {
                Check();
                if (!(await identities.ReadAsync(ct).ConfigureAwait(false)).SequenceEqual(rows)
                    || !(await catalogue.ReadBindingsAsync(ct).ConfigureAwait(false)).SequenceEqual(bindings))
                    throw new IOException("Recovery profiles or generation inventory changed.");
                Check();
            }
        }
        finally
        {
            foreach (var entry in retained) CryptographicOperations.ZeroMemory(entry.Key);
            operations.Release();
        }
    }
}
