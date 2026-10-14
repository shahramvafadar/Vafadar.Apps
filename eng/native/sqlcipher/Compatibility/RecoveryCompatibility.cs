using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Data;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Exercises optional recovery with native SQLCipher, actual schema/finance and missing independently owned device envelopes.</summary>
internal static class RecoveryCompatibility
{
    internal static async Task<string> RunAsync(string folder, bool create,
        Func<string, IDatabaseGenerationKeyStore> createKeys, Func<string, IDatabaseGenerationFileDurability> createDurability,
        Func<Exception, bool>? isNativeMissing = null, bool reEnroll = false, bool nativeReopen = false)
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        if (create) { if (Directory.Exists(folder)) throw new IOException("Use a fresh independently owned recovery fixture."); Directory.CreateDirectory(folder); }
        var original = Path.Combine(folder, "zanance.db");
        using var services = new ServiceCollection().AddZananceData(original).BuildServiceProvider();
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var durability = createDurability(folder); var nativeKeys = createKeys(folder);
        var identityPath = Path.Combine(folder, "identities.sqlite");
        // This deterministic key belongs only to independent fictitious tests. It is never an application default,
        // written to a file, printed or used against the product's own storage.
        var recoveryKey = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        try
        {
            using var owner = await location.Access.AcquireExclusiveAsync();
            var identities = new SqliteDatabaseIdentityCatalogue(identityPath, owner.Validate);
            if (create) await identities.CreateNewAsync([""], durability);
            var identity = (await identities.ReadAsync()).Single();
            var publicPath = Path.Combine(folder, "recovery-public.json");
            DatabaseRecoveryIdentity publicIdentity;
            if (create)
            {
                using (var material = DatabaseRecoveryMaterial.Create(identity.InstallationId))
                {
                    publicIdentity = material.Identity;
                    var vault = new SqlCipherRecoveryVault(Path.Combine(folder, "recovery-private.cipher.db"), publicIdentity, "4.19.0", durability, owner.Validate);
                    await vault.CreateNewAsync(material, recoveryKey);
                }
                await File.WriteAllTextAsync(publicPath, JsonSerializer.Serialize(new[] { publicIdentity.InstallationId.ToString("N"),
                    publicIdentity.RecoveryId.ToString("N"), Convert.ToBase64String(publicIdentity.ExportPublicKey()) }, RecoveryJsonContext.Default.StringArray));
                await durability.SynchronizeFileAsync(publicPath, default); await durability.SynchronizeDirectoryAsync(default);
            }
            else
            {
                var values = JsonSerializer.Deserialize(await File.ReadAllTextAsync(publicPath), RecoveryJsonContext.Default.StringArray) ?? throw new IOException("Missing fixture public identity.");
                if (values.Length != 3 || Guid.ParseExact(values[0], "N") != identity.InstallationId) throw new IOException("Fixture installation differs.");
                publicIdentity = new(identity.InstallationId, Guid.ParseExact(values[1], "N"), Convert.FromBase64String(values[2]));
            }
            var envelopes = new SqliteRecoveryEnvelopeCatalogue(Path.Combine(folder, "recovery-envelopes.sqlite"), publicIdentity, durability, owner.Validate);
            var privateVault = new SqlCipherRecoveryVault(Path.Combine(folder, "recovery-private.cipher.db"), publicIdentity, "4.19.0", durability, owner.Validate);
            async Task BuildSchema(SqliteConnection connection, CancellationToken ct)
            {
                var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection, contextOwnsConnection: false).UseModel(ZananceDbContextModel.Instance).Options;
                await using var db = new ZananceDbContext(options); await db.Database.MigrateAsync(ct);
                if ((await db.Database.GetAppliedMigrationsAsync(ct)).Count() != 32) throw new IOException("Actual schema is incomplete.");
            }
            if (create)
            {
                await envelopes.CreateNewAsync();
                var protectedKeys = new RecoveryGenerationKeyStore(nativeKeys, envelopes);
                using var generation = await SqlCipherNewProfile.OpenAsync(location, identities, "", original, true, protectedKeys, durability,
                    BuildSchema, owner, owner.Validate, 16 * 1024 * 1024, "4.19.0");
                generation.Publish(location, owner);
                await using (var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner))
                {
                    db.Accounts.Add(new Account { Name = "Fictitious recovery بانک Ü", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = -321 });
                    await db.SaveChangesAsync();
                }
                LocalDatabasePools.ClearFile(generation.Path);
                await File.WriteAllTextAsync(Path.Combine(folder, "candidate.sha256"), Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(generation.Path))));
                var keyPath = Path.Combine(folder, $"{identity.ProfileId:N}-{identity.CandidateGeneration:N}.key");
                if (!File.Exists(keyPath)) throw new IOException("Native device envelope was not created.");
                File.Delete(keyPath); await durability.SynchronizeDirectoryAsync(default);
                var missing = false;
                try { var unexpected = await nativeKeys.ReadAsync(identity.InitialCandidateKey, default); CryptographicOperations.ZeroMemory(unexpected); }
                catch (Exception error) when (error is IOException or CryptographicException || isNativeMissing?.Invoke(error) == true) { missing = true; }
                if (!missing || File.Exists(original)) throw new IOException("Missing native key simulation differs or plaintext was created.");
                return "{\"PrivateVaultEncrypted\":true,\"DeviceEnvelopeMissing\":true,\"PublicOnlyGenerationCapture\":true}";
            }
            if (nativeReopen)
            {
                using var native = await SqlCipherNewProfile.OpenAsync(location, identities, "", original, false, new ExistingKeys(nativeKeys), durability,
                    (_, _) => throw new IOException("Native cold reopen cannot create replacement schema."), owner, owner.Validate, 16 * 1024 * 1024, "4.19.0");
                native.Publish(location, owner);
                await using (var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner))
                    if ((await db.Accounts.AsNoTracking().SingleAsync()).OpeningBalance != -321) throw new IOException("Native re-enrollment lost financial data.");
                LocalDatabasePools.ClearFile(native.Path);
                if (Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(native.Path))) != await File.ReadAllTextAsync(Path.Combine(folder, "candidate.sha256"))) throw new IOException("Native reopen changed ciphertext.");
                return "{\"NativeColdReadWithoutRecoverySecret\":true,\"OriginalMoneyPreserved\":true,\"CiphertextUnchanged\":true}";
            }
            var missingDevice = false;
            try { var unexpected = await nativeKeys.ReadAsync(identity.InitialCandidateKey, default); CryptographicOperations.ZeroMemory(unexpected); }
            catch (Exception error) when (error is IOException or CryptographicException || isNativeMissing?.Invoke(error) == true) { missingDevice = true; }
            if (!missingDevice) throw new IOException("Cold fixture did not retain its missing native envelope.");
            var wrong = (byte[])recoveryKey.Clone(); wrong[0] ^= 1;
            try
            {
                var refused = false;
                try { using var invalid = await privateVault.OpenAsync(wrong); }
                catch (Exception error) when (error is SqliteException or CryptographicException or InvalidDataException) { refused = true; }
                if (!refused) throw new IOException("Wrong recovery key was accepted.");
            }
            finally { CryptographicOperations.ZeroMemory(wrong); }
            var tamperedPath = Path.Combine(folder, "recovery-private.tampered.cipher.db");
            File.Copy(Path.Combine(folder, "recovery-private.cipher.db"), tamperedPath, overwrite: false);
            using (var output = new FileStream(tamperedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            { output.Position = 4096 + 128; var value = output.ReadByte(); if (value < 0) throw new IOException("Vault fixture page is missing."); output.Position--; output.WriteByte((byte)(value ^ 1)); output.Flush(true); }
            var damagedRefused = false;
            try { using var invalid = await new SqlCipherRecoveryVault(tamperedPath, publicIdentity, "4.19.0", durability, owner.Validate).OpenAsync(recoveryKey); }
            catch (Exception error) when (error is SqliteException or CryptographicException or InvalidDataException) { damagedRefused = true; }
            if (!damagedRefused) throw new IOException("Damaged private vault was accepted.");
            using var recovered = await privateVault.OpenAsync(recoveryKey);
            var explicitRecovery = new RecoveryOnlyKeys(envelopes, recovered);
            using var opened = await SqlCipherNewProfile.OpenAsync(location, identities, "", original, false, explicitRecovery, durability,
                (_, _) => throw new IOException("Existing recovery cannot build replacement schema."), owner, owner.Validate, 16 * 1024 * 1024, "4.19.0");
            opened.Publish(location, owner);
            List<Account> accounts;
            await using (var db = await services.GetRequiredService<ILocalDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(owner))
                accounts = await db.Accounts.AsNoTracking().ToListAsync();
            if (accounts.Count != 1 || accounts[0].OpeningBalance != -321 || accounts[0].Name != "Fictitious recovery بانک Ü")
                throw new IOException("Recovered actual financial data differs.");
            LocalDatabasePools.ClearFile(opened.Path);
            if (Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(opened.Path))) != await File.ReadAllTextAsync(Path.Combine(folder, "candidate.sha256")))
                throw new IOException("Recovery altered the existing encrypted financial database.");
            if (File.Exists(Path.Combine(folder, $"{identity.ProfileId:N}-{identity.CandidateGeneration:N}.key"))) throw new IOException("Recovery manufactured a device envelope.");
            if (reEnroll)
            {
                var key = await explicitRecovery.ReadAsync(identity.InitialCandidateKey, default);
                try
                {
                    if (nativeKeys is not IDatabaseGenerationKeyRecovery importer) throw new IOException("Native recovery importer is unavailable.");
                    await importer.ImportRecoveredAsync(identity.InitialCandidateKey, key, async (candidate, ct) =>
                    {
                        using var verified = await SqlCipherNewProfile.OpenAsync(location, identities, "", original, false,
                            new FixedKeys(identity.InitialCandidateKey, candidate), durability,
                            (_, _) => throw new IOException("Recovery import cannot create replacement schema."), owner, owner.Validate, 16 * 1024 * 1024, "4.19.0", ct);
                        if (verified.Binding != opened.Binding || verified.Path != opened.Path) throw new IOException("Verified recovery generation differs.");
                    }, default);
                }
                finally { CryptographicOperations.ZeroMemory(key); }
                return "{\"VerifiedNativeReEnrollment\":true,\"OriginalCiphertextPreserved\":true}";
            }
            return "{\"DamagedPrivateVaultRefused\":true,\"WrongRecoveryKeyRefused\":true,\"ColdRecoveryWithoutDeviceKey\":true,\"ActualModelAndNegativeMoneyPreserved\":true,\"CiphertextUnchanged\":true,\"NoReplacementDeviceKey\":true}";
        }
        finally { CryptographicOperations.ZeroMemory(recoveryKey); }
    }
    private sealed class ExistingKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Cold native reopen cannot prepare keys.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
    }
    private sealed class FixedKeys(DatabaseKeyBinding expected, ReadOnlyMemory<byte> key) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Verification cannot prepare keys.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => binding == expected ? Task.FromResult(key.ToArray()) : throw new IOException("Verification belongs to another key.");
    }
    private sealed class RecoveryOnlyKeys(SqliteRecoveryEnvelopeCatalogue catalogue, DatabaseRecoveryMaterial material) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Recovery cannot generate replacement keys.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => catalogue.RecoverAsync(binding, material, ct);
    }
}

[JsonSerializable(typeof(string[]))]
internal partial class RecoveryJsonContext : JsonSerializerContext;
