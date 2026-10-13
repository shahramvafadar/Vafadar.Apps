using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Creates the original schema only in memory and verifies native new-profile restart without plaintext finance files.</summary>
internal static class NewProfileCompatibility
{
    /// <summary>Pauses after durable key preparation, resumes the captured empty profile or cold-opens a saved write.</summary>
    internal static async Task<string> RunAsync(string folder, string phase)
    {
        var create = phase == "new-profile-create";
        if (create) { if (Directory.Exists(folder)) throw new IOException("Use a fresh new-profile fixture."); Directory.CreateDirectory(folder); }
        var original = Path.Combine(folder, "new.db");
        using var services = new ServiceCollection().AddZananceData(original).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = await location.Access.AcquireExclusiveAsync();
        var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), owner.Validate);
        if (create) await identities.CreateNewAsync([""], new AndroidGenerationFileDurability(folder));
        var identity = (await identities.ReadAsync()).Single();
        var envelope = Path.Combine(folder, identity.ProfileId.ToString("N") + "-" + identity.CandidateGeneration.ToString("N") + ".key");
        var actualKeys = new AndroidGenerationKeyStore(folder);
        IDatabaseGenerationKeyStore keys = create ? new KeyBarrier(actualKeys, folder) : phase == "new-profile-reopen" ? new ExistingKeys(actualKeys) : actualKeys;
        using var generation = await SqlCipherNewProfile.OpenAsync(location, identities, "", original, create, keys,
            new AndroidGenerationFileDurability(folder), async (connection, ct) =>
            {
                var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection, contextOwnsConnection: false).UseModel(ZananceDbContextModel.Instance).Options;
                await using var db = new ZananceDbContext(options); await db.Database.MigrateAsync(ct);
                if (await db.Accounts.AnyAsync(ct) || await db.Entries.AnyAsync(ct)) throw new IOException("Schema initialization created financial data.");
            }, owner, () => { }, 16 * 1024 * 1024, "4.19.0");
        generation.Publish(location, owner);
        var digest = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(envelope)));
        if (digest != await File.ReadAllTextAsync(Path.Combine(folder, "new-key-digest.txt"))) throw new IOException("New-profile restart replaced its prepared key envelope.");
        await using (var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner))
        {
            if ((await db.Database.GetAppliedMigrationsAsync()).Count() != 32 || await db.Accounts.AnyAsync()) throw new IOException("New profile schema differs.");
            if (phase == "new-profile-resume")
            {
                if (await db.Entries.AnyAsync()) throw new IOException("New profile opened with ledger data.");
                var settings = new ZananceSettings(); db.Settings.Add(settings); await db.SaveChangesAsync();
            }
            else if (await db.Settings.CountAsync() != 1) throw new IOException("Cold new-profile open lost saved data.");
        }
        if (File.Exists(original) || File.Exists(original + "-wal") || !File.Exists(generation.Path + ".rollback.db")) throw new IOException("New profile created plaintext or lost rollback.");
        return "{\"EncryptedNewProfileWithoutPlaintextOriginal\":true,\"Actual32Migrations\":true,\"ColdSavedDataPreserved\":true,\"PreparedEnvelopeUnchanged\":true}";
    }

    private sealed class KeyBarrier(AndroidGenerationKeyStore actual, string folder) : IDatabaseGenerationKeyStore
    {
        /// <inheritdoc />
        public async Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct)
        {
            await actual.PrepareAsync(binding, ct);
            var envelope = Path.Combine(folder, binding.ProfileId.ToString("N") + "-" + binding.GenerationId.ToString("N") + ".key");
            await File.WriteAllTextAsync(Path.Combine(folder, "new-key-digest.txt"), Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(envelope, ct))), ct);
            await File.WriteAllTextAsync(Path.Combine(folder, "new-profile-key.json"), "{\"ActualKeyPreparedBeforeSchema\":true}", ct);
            await Task.Delay(Timeout.Infinite, ct);
        }
        /// <inheritdoc />
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => actual.ReadAsync(binding, ct);
    }
    private sealed class ExistingKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
    {
        /// <inheritdoc />
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Cold new profile cannot prepare keys.");
        /// <inheritdoc />
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
    }
}
