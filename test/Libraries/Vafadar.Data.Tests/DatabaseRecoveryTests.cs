using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Standard recovery primitives and actual SQLite metadata reject changed keys, contexts and persistence authority.</summary>
public sealed class DatabaseRecoveryTests : IDisposable
{
    private readonly TemporaryDirectory directory = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    public void Dispose() => directory.Dispose();

    [Fact, Trait("AT", "AT-238")]
    public void Public_material_protects_new_generation_without_private_owner_and_never_repeats_ciphertext()
    {
        using var material = DatabaseRecoveryMaterial.Create(Guid.NewGuid());
        var identity = new DatabaseRecoveryIdentity(material.Identity.InstallationId, material.Identity.RecoveryId, material.Identity.ExportPublicKey());
        var binding = Binding(identity); var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            var first = identity.Protect(key, binding); var second = identity.Protect(key, binding);
            Assert.Equal(384, first.Length); Assert.NotEqual(first, second);
            var restored = material.Open(first, binding);
            try { Assert.Equal(key, restored); } finally { CryptographicOperations.ZeroMemory(restored); }
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [Theory, InlineData(0), InlineData(1), InlineData(2), InlineData(3), Trait("AT", "AT-238")]
    public void Wrong_profile_generation_installation_or_private_key_never_unwraps(int changed)
    {
        using var material = DatabaseRecoveryMaterial.Create(Guid.NewGuid());
        using var another = DatabaseRecoveryMaterial.Create(material.Identity.InstallationId);
        var original = Binding(material.Identity); var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            var envelope = material.Identity.Protect(key, original);
            var wrong = new DatabaseKeyBinding(changed == 0 ? Guid.NewGuid() : original.ProfileId,
                changed == 1 ? Guid.NewGuid() : original.InstallationId, changed == 2 ? Guid.NewGuid() : original.GenerationId);
            Assert.ThrowsAny<CryptographicException>(() => (changed == 3 ? another : material).Open(envelope, wrong));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [Fact, Trait("AT", "AT-238")]
    public void Damaged_truncated_and_retired_material_fail_closed()
    {
        var material = DatabaseRecoveryMaterial.Create(Guid.NewGuid()); var binding = Binding(material.Identity);
        var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            var envelope = material.Identity.Protect(key, binding); envelope[10] ^= 1;
            // Native RSA providers use platform-specific CryptographicException subclasses.
            Assert.ThrowsAny<CryptographicException>(() => material.Open(envelope, binding));
            Assert.Throws<CryptographicException>(() => material.Open(envelope.AsSpan(0, 383), binding));
            material.Dispose(); Assert.Throws<ObjectDisposedException>(() => material.Open(envelope, binding));
        }
        finally { material.Dispose(); CryptographicOperations.ZeroMemory(key); }
    }

    [Fact, Trait("AT", "AT-238")]
    public void Weak_or_noncanonical_public_material_is_refused()
    {
        using var weak = RSA.Create(2048); using var strong = RSA.Create(3072);
        Assert.Throws<CryptographicException>(() => new DatabaseRecoveryIdentity(Guid.NewGuid(), Guid.NewGuid(), weak.ExportSubjectPublicKeyInfo()));
        Assert.Throws<CryptographicException>(() => new DatabaseRecoveryIdentity(Guid.NewGuid(), Guid.NewGuid(), [.. strong.ExportSubjectPublicKeyInfo(), 0]));
        Assert.Throws<ArgumentException>(() => DatabaseRecoveryMaterial.Create(Guid.Empty));
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Existing_mapping_requalifies_native_flush_and_recovers_exact_key_without_replacing_ciphertext()
    {
        using var material = DatabaseRecoveryMaterial.Create(Guid.NewGuid()); var persistence = new Durability();
        var catalogue = Catalogue(material.Identity, persistence); await catalogue.CreateNewAsync(Ct);
        var binding = Binding(material.Identity); var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            await catalogue.CaptureAsync(binding, key, Ct); var before = await File.ReadAllBytesAsync(PathOfCatalogue, Ct);
            var syncs = persistence.FileSyncs; await catalogue.CaptureAsync(binding, key, Ct);
            Assert.True(persistence.FileSyncs > syncs); Assert.Equal(before, await File.ReadAllBytesAsync(PathOfCatalogue, Ct));
            var restored = await catalogue.RecoverAsync(binding, material, Ct);
            try { Assert.Equal(key, restored); } finally { CryptographicOperations.ZeroMemory(restored); }
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Changed_native_key_or_ciphertext_cannot_replace_existing_mapping()
    {
        using var material = DatabaseRecoveryMaterial.Create(Guid.NewGuid()); var catalogue = Catalogue(material.Identity, new Durability());
        await catalogue.CreateNewAsync(Ct); var binding = Binding(material.Identity); var key = RandomNumberGenerator.GetBytes(32);
        var changed = RandomNumberGenerator.GetBytes(32);
        try
        {
            await catalogue.CaptureAsync(binding, key, Ct);
            await Assert.ThrowsAsync<CryptographicException>(() => catalogue.CaptureAsync(binding, changed, Ct));
            await using var connection = new SqliteConnection($"Data Source={PathOfCatalogue};Pooling=False"); await connection.OpenAsync(Ct);
            using var command = connection.CreateCommand(); command.CommandText = "UPDATE recovery_envelope SET envelope=zeroblob(384)";
            await command.ExecuteNonQueryAsync(Ct); await connection.CloseAsync();
            await Assert.ThrowsAnyAsync<CryptographicException>(() => catalogue.CaptureAsync(binding, key, Ct));
            await Assert.ThrowsAnyAsync<CryptographicException>(() => catalogue.RecoverAsync(binding, material, Ct));
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(changed); }
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Changed_confirmation_cannot_return_any_recovered_key()
    {
        using var material = DatabaseRecoveryMaterial.Create(Guid.NewGuid()); var catalogue = Catalogue(material.Identity, new Durability());
        await catalogue.CreateNewAsync(Ct); var binding = Binding(material.Identity); var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            await catalogue.CaptureAsync(binding, key, Ct);
            await using var connection = new SqliteConnection($"Data Source={PathOfCatalogue};Pooling=False"); await connection.OpenAsync(Ct);
            using var command = connection.CreateCommand(); command.CommandText = "UPDATE recovery_envelope SET confirmation=zeroblob(32)";
            await command.ExecuteNonQueryAsync(Ct); await connection.CloseAsync();
            await Assert.ThrowsAsync<CryptographicException>(() => catalogue.RecoverAsync(binding, material, Ct));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Failed_native_capture_keeps_mapping_for_exact_requalification_without_preparing_replacement_key()
    {
        using var material = DatabaseRecoveryMaterial.Create(Guid.NewGuid()); var persistence = new Durability();
        var catalogue = Catalogue(material.Identity, persistence); await catalogue.CreateNewAsync(Ct);
        var binding = Binding(material.Identity); var native = new Keys(); var decorated = new RecoveryGenerationKeyStore(native, catalogue);
        persistence.Fail = true;
        await Assert.ThrowsAsync<IOException>(() => decorated.PrepareAsync(binding, Ct));
        Assert.Equal(1, native.Generated); persistence.Fail = false; await decorated.PrepareAsync(binding, Ct);
        Assert.Equal(1, native.Generated);
        var restored = await catalogue.RecoverAsync(binding, material, Ct);
        try { Assert.Equal(native.Key, restored); } finally { CryptographicOperations.ZeroMemory(restored); CryptographicOperations.ZeroMemory(native.Key); }
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Missing_metadata_and_missing_mapping_never_manufacture_recovery_authority()
    {
        using var material = DatabaseRecoveryMaterial.Create(Guid.NewGuid()); var catalogue = Catalogue(material.Identity, new Durability()); var binding = Binding(material.Identity);
        await Assert.ThrowsAsync<IOException>(() => catalogue.RecoverAsync(binding, material, Ct)); Assert.False(File.Exists(PathOfCatalogue));
        await catalogue.CreateNewAsync(Ct); await Assert.ThrowsAsync<IOException>(() => catalogue.RecoverAsync(binding, material, Ct));
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Vault_missing_or_invalid_recovery_key_does_not_create_an_existing_private_vault()
    {
        using var material = DatabaseRecoveryMaterial.Create(Guid.NewGuid());
        var vault = new SqlCipherRecoveryVault(directory.Combine("recovery.cipher.db"), material.Identity, "4.19.0", new Durability(), () => { });
        await Assert.ThrowsAsync<CryptographicException>(() => vault.OpenAsync(new byte[31], Ct));
        await Assert.ThrowsAsync<IOException>(() => vault.OpenAsync(new byte[32], Ct));
        Assert.False(File.Exists(directory.Combine("recovery.cipher.db")));
    }
    [Fact, Trait("AT", "AT-238")]
    public async Task Cold_public_identity_read_preserves_exact_metadata_without_private_material_or_writes()
    {
        using var material = DatabaseRecoveryMaterial.Create(Guid.NewGuid());
        var durability = new Durability();
        await Catalogue(material.Identity, durability).CreateNewAsync(Ct);
        var before = await File.ReadAllBytesAsync(PathOfCatalogue, Ct);
        var syncs = durability.FileSyncs;
        var read = await SqliteRecoveryEnvelopeCatalogue.ReadExistingIdentityAsync(PathOfCatalogue, durability, () => { }, Ct);
        Assert.Equal(material.Identity.InstallationId, read.InstallationId);
        Assert.Equal(material.Identity.RecoveryId, read.RecoveryId);
        Assert.Equal(material.Identity.ExportPublicKey(), read.ExportPublicKey());
        Assert.Equal(before, await File.ReadAllBytesAsync(PathOfCatalogue, Ct));
        Assert.Equal(syncs, durability.FileSyncs);
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Missing_public_identity_never_creates_recovery_metadata()
    {
        await Assert.ThrowsAsync<IOException>(() => SqliteRecoveryEnvelopeCatalogue.ReadExistingIdentityAsync(
            PathOfCatalogue, new Durability(), () => { }, Ct));
        Assert.False(File.Exists(PathOfCatalogue));
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Cold_public_identity_read_rejects_foreign_schema_even_with_valid_public_identity()
    {
        using var material = DatabaseRecoveryMaterial.Create(Guid.NewGuid());
        var durability = new Durability();
        await Catalogue(material.Identity, durability).CreateNewAsync(Ct);
        await using (var connection = new SqliteConnection($"Data Source={PathOfCatalogue};Pooling=False"))
        {
            await connection.OpenAsync(Ct);
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE foreign_row (id INTEGER)";
            await command.ExecuteNonQueryAsync(Ct);
        }
        await Assert.ThrowsAsync<InvalidDataException>(() => SqliteRecoveryEnvelopeCatalogue.ReadExistingIdentityAsync(
            PathOfCatalogue, durability, () => { }, Ct));
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Existing_only_vault_reader_never_creates_missing_private_material()
    {
        var missing = directory.Combine("missing-private.cipher.db");
        await Assert.ThrowsAsync<IOException>(() => SqlCipherRecoveryVault.OpenExistingAsync(missing, Guid.NewGuid(),
            new byte[32], "4.19.0", new Durability(), () => { }, Ct));
        Assert.False(File.Exists(missing));
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Existing_only_vault_reader_requires_captured_installation_before_native_access()
    {
        var accessed = false;
        await Assert.ThrowsAsync<ArgumentException>(() => SqlCipherRecoveryVault.OpenExistingAsync(directory.Combine("vault.cipher.db"),
            Guid.Empty, new byte[32], "4.19.0", new Durability(), () => accessed = true, Ct));
        Assert.False(accessed);
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Retention_removes_exact_mapping_and_requalifies_visible_delete_without_touching_other_generations()
    {
        using var material = DatabaseRecoveryMaterial.Create(Guid.NewGuid()); var persistence = new Durability();
        var catalogue = Catalogue(material.Identity, persistence); await catalogue.CreateNewAsync(Ct);
        var retired = Binding(material.Identity); var retained = Binding(material.Identity);
        var first = RandomNumberGenerator.GetBytes(32); var second = RandomNumberGenerator.GetBytes(32);
        try
        {
            await catalogue.CaptureAsync(retired, first, Ct); await catalogue.CaptureAsync(retained, second, Ct);
            var captured = await catalogue.CaptureRetentionBindingAsync(retired, first, Ct);
            persistence.Fail = true;
            await Assert.ThrowsAsync<IOException>(() => catalogue.RemoveRetentionBindingAsync(retired, captured, _ => Task.CompletedTask, Ct));
            persistence.Fail = false;
            await catalogue.RemoveRetentionBindingAsync(retired, captured, _ => Task.CompletedTask, Ct);
            Assert.Equal([retained], await catalogue.ReadBindingsAsync(Ct));
            var recovered = await catalogue.RecoverAsync(retained, material, Ct);
            try { Assert.Equal(second, recovered); } finally { CryptographicOperations.ZeroMemory(recovered); }
        }
        finally { CryptographicOperations.ZeroMemory(first); CryptographicOperations.ZeroMemory(second); }
    }

    [Fact, Trait("AT", "AT-238")]
    public async Task Retention_refuses_missing_authority_and_changed_mapping_before_any_delete()
    {
        using var material = DatabaseRecoveryMaterial.Create(Guid.NewGuid()); var catalogue = Catalogue(material.Identity, new Durability());
        await catalogue.CreateNewAsync(Ct); var binding = Binding(material.Identity); var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            await catalogue.CaptureAsync(binding, key, Ct); var captured = await catalogue.CaptureRetentionBindingAsync(binding, key, Ct);
            await Assert.ThrowsAsync<IOException>(() => catalogue.RemoveRetentionBindingAsync(binding, captured,
                _ => throw new IOException("Fictitious unresolved retained reference"), Ct));
            Assert.Equal([binding], await catalogue.ReadBindingsAsync(Ct));
            await using (var connection = new SqliteConnection($"Data Source={PathOfCatalogue};Pooling=False"))
            {
                await connection.OpenAsync(Ct); using var command = connection.CreateCommand();
                command.CommandText = "UPDATE recovery_envelope SET confirmation=zeroblob(32)"; await command.ExecuteNonQueryAsync(Ct);
            }
            await Assert.ThrowsAsync<IOException>(() => catalogue.RemoveRetentionBindingAsync(binding, captured, _ => Task.CompletedTask, Ct));
            Assert.Equal([binding], await catalogue.ReadBindingsAsync(Ct));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    private string PathOfCatalogue => directory.Combine("recovery-envelope.sqlite");
    private SqliteRecoveryEnvelopeCatalogue Catalogue(DatabaseRecoveryIdentity identity, Durability persistence) => new(PathOfCatalogue, identity, persistence, () => { });
    private static DatabaseKeyBinding Binding(DatabaseRecoveryIdentity identity) => new(Guid.NewGuid(), identity.InstallationId, Guid.NewGuid());
    private sealed class Durability : IDatabaseGenerationFileDurability
    {
        internal bool Fail; internal int FileSyncs;
        public long AvailableBytes => long.MaxValue;
        public Task SynchronizeFileAsync(string path, CancellationToken ct) { FileSyncs++; if (Fail) throw new IOException("Injected synchronization interruption."); return Task.CompletedTask; }
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => Task.CompletedTask;
    }
    private sealed class Keys : IDatabaseGenerationKeyStore
    {
        internal byte[] Key = []; internal int Generated;
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) { if (Key.Length == 0) { Key = RandomNumberGenerator.GetBytes(32); Generated++; } return Task.CompletedTask; }
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => Task.FromResult((byte[])Key.Clone());
    }
}
