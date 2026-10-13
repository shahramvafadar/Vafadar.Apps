using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Joins actual identity, migration, native key storage and original protected factory across independent processes.</summary>
internal static class InitialGenerationCompatibility
{
    internal static async Task<string> RunAsync(string folder, bool create)
    {
        var original = Path.Combine(folder, "profile.db");
        if (create)
        {
            if (Directory.Exists(folder)) throw new IOException("Use a fresh initial-generation fixture.");
            Directory.CreateDirectory(folder); await GenerationMigrationCompatibility.CreateOriginalAsync(original);
        }
        using var services = new ServiceCollection().AddZananceData(original).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var owner = await location.Access.AcquireExclusiveAsync();
        try
        {
            var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), owner.Validate);
            if (create)
            {
                await identities.CreateNewAsync([""]);
                await SqlCipherProfileGeneration.InitializeJournalAsync(location, identities, "", original, owner, () => { });
            }
            var actualKeys = new AndroidGenerationKeyStore(folder);
            IDatabaseGenerationKeyStore keys = create ? actualKeys : new ExistingOnlyKeys(actualKeys);
            using var generation = await SqlCipherProfileGeneration.OpenInitialAsync(location, identities, "", original,
                keys, new AndroidGenerationFileDurability(folder), owner,
                () => { if (!Directory.Exists(folder)) throw new IOException("Retired fictitious profile."); }, 16 * 1024 * 1024, "4.19.0");
            generation.Publish(location, owner); owner.Dispose();
            var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            await using (var db = await factory.CreateDbContextAsync())
            {
                var account = await db.Accounts.SingleAsync(); var entry = await db.Entries.SingleAsync();
                if (account.OpeningBalance != 1000 || entry.Amount != 250) throw new IOException("Initial generation changed financial packets.");
                if (create) { account.Name = "Fictitious persisted protected"; await db.SaveChangesAsync(); }
                else if (account.Name != "Fictitious persisted protected") throw new IOException("Cold protected factory lost its saved write.");
            }
            if (File.Exists(original) || !File.Exists(generation.Path + ".rollback.db")) throw new IOException("Initial cleanup/rollback ownership differs.");
            if ((await File.ReadAllBytesAsync(generation.Path)).AsSpan().StartsWith("SQLite format 3"u8)) throw new IOException("Initial selected database remained plaintext.");
            generation.Dispose(); var refused = false;
            try { await using var unexpected = await factory.CreateDbContextAsync(); } catch (ObjectDisposedException) { refused = true; }
            if (!refused) throw new IOException("Disposed key owner allowed new factory access.");
            return create ? "{\"ActualIdentityMigrationFactoryJoined\":true,\"SelectedCiphertextAndRollback\":true,\"RetainedKeyDisposalRefusesAccess\":true}"
                : "{\"InitialGenerationColdReopened\":true,\"SavedWriteAndMoneyCorrect\":true,\"NoKeyPreparationOnRestart\":true,\"NoOriginalOrPlaintextFallback\":true,\"RetainedKeyDisposalRefusesAccess\":true}";
        }
        finally { owner.Dispose(); }
    }

    private sealed class ExistingOnlyKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Cold reopen must not prepare or replace a key.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
    }
}
