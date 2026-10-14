using System.Security.Cryptography;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Actual recovery metadata and retirement journal preserve unrelated mappings through ordered deletion.</summary>
public sealed class RecoveryRetirementTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-239")]
    public async Task Exact_target_removal_preserves_other_profile_recovery_and_repeats_only_durability()
    {
        using var fixture = new Fixture(); await fixture.InitializeAsync();
        await fixture.Catalogue.CaptureRetirementAsync(fixture.Request, fixture.Journal, fixture.Keys, Ct);
        await fixture.Journal.CompareAndAdvanceAsync(fixture.Request, fixture.Durability, Ct);
        await fixture.Catalogue.RemoveRetiredAsync(fixture.Request.Advance(), fixture.Journal, Ct);
        await fixture.Catalogue.VerifyRetiredAsync(fixture.Request.Advance(), fixture.Journal, Ct);
        await Assert.ThrowsAsync<IOException>(() => fixture.Catalogue.RecoverAsync(fixture.Target, fixture.Material, Ct));
        var other = await fixture.Catalogue.RecoverAsync(fixture.Other, fixture.Material, Ct);
        try { Assert.Equal(fixture.Keys.Values[fixture.Other], other); } finally { CryptographicOperations.ZeroMemory(other); }
        var syncs = fixture.Durability.Syncs;
        await fixture.Catalogue.RemoveRetiredAsync(fixture.Request.Advance(), fixture.Journal, Ct);
        Assert.True(fixture.Durability.Syncs > syncs);
    }

    [Fact, Trait("AT", "AT-239")]
    public async Task Failed_native_sync_after_delete_retains_other_keys_and_requalifies_same_request()
    {
        using var fixture = new Fixture(); await fixture.InitializeAsync();
        await fixture.Catalogue.CaptureRetirementAsync(fixture.Request, fixture.Journal, fixture.Keys, Ct);
        await fixture.Journal.CompareAndAdvanceAsync(fixture.Request, fixture.Durability, Ct);
        fixture.Durability.FailRecovery = true;
        await Assert.ThrowsAsync<IOException>(() => fixture.Catalogue.RemoveRetiredAsync(fixture.Request.Advance(), fixture.Journal, Ct));
        Assert.Equal(fixture.Request.Advance(), await fixture.Journal.ReadAsync(Ct));
        fixture.Durability.FailRecovery = false;
        await fixture.Catalogue.RemoveRetiredAsync(fixture.Request.Advance(), fixture.Journal, Ct);
        var other = await fixture.Catalogue.RecoverAsync(fixture.Other, fixture.Material, Ct);
        CryptographicOperations.ZeroMemory(other);
    }

    [Fact, Trait("AT", "AT-239")]
    public async Task New_target_generation_after_capture_refuses_deletion_and_preserves_all_mappings()
    {
        using var fixture = new Fixture(); await fixture.InitializeAsync();
        await fixture.Catalogue.CaptureRetirementAsync(fixture.Request, fixture.Journal, fixture.Keys, Ct);
        var next = new DatabaseKeyBinding(fixture.Target.ProfileId, fixture.Target.InstallationId, Guid.NewGuid());
        var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            await fixture.Catalogue.CaptureAsync(next, key, Ct);
            await Assert.ThrowsAsync<IOException>(() => fixture.Catalogue.CaptureRetirementAsync(fixture.Request, fixture.Journal, new AdditionalKeys(fixture.Keys, next, key), Ct));
            await fixture.Journal.CompareAndAdvanceAsync(fixture.Request, fixture.Durability, Ct);
            await Assert.ThrowsAsync<IOException>(() => fixture.Catalogue.RemoveRetiredAsync(fixture.Request.Advance(), fixture.Journal, Ct));
            var restored = await fixture.Catalogue.RecoverAsync(fixture.Target, fixture.Material, Ct); CryptographicOperations.ZeroMemory(restored);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [Fact, Trait("AT", "AT-239")]
    public async Task Wrong_native_key_cannot_authorize_retirement_capture()
    {
        using var fixture = new Fixture(); await fixture.InitializeAsync();
        var original = fixture.Keys.Values[fixture.Target]; fixture.Keys.Values[fixture.Target] = RandomNumberGenerator.GetBytes(32);
        try { await Assert.ThrowsAsync<CryptographicException>(() => fixture.Catalogue.CaptureRetirementAsync(fixture.Request, fixture.Journal, fixture.Keys, Ct)); }
        finally { CryptographicOperations.ZeroMemory(fixture.Keys.Values[fixture.Target]); fixture.Keys.Values[fixture.Target] = original; }
        Assert.False(await fixture.Journal.HasRecoveryInventoryAsync(Ct));
    }

    [Fact, Trait("AT", "AT-239")]
    public async Task Missing_capture_wrong_phase_and_another_request_cannot_authorize_cleanup()
    {
        using var fixture = new Fixture(); await fixture.InitializeAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Catalogue.RemoveRetiredAsync(fixture.Request, fixture.Journal, Ct));
        await fixture.Journal.CompareAndAdvanceAsync(fixture.Request, fixture.Durability, Ct);
        await Assert.ThrowsAsync<IOException>(() => fixture.Catalogue.RemoveRetiredAsync(fixture.Request.Advance(), fixture.Journal, Ct));
        var another = new ProfileRetirementCheckpoint(Guid.NewGuid().ToString("N"), fixture.Other.ProfileId, fixture.Other.InstallationId, Guid.NewGuid());
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Catalogue.VerifyRetiredAsync(another, fixture.Journal, Ct));
    }
    private sealed class Fixture : IDisposable
    {
        private readonly TemporaryDirectory directory = new();
        internal readonly DatabaseRecoveryMaterial Material = DatabaseRecoveryMaterial.Create(Guid.NewGuid());
        internal readonly Durability Durability = new(); internal readonly Keys Keys = new();
        internal DatabaseKeyBinding Target = null!, Other = null!;
        internal ProfileRetirementCheckpoint Request = null!;
        internal SqliteProfileRetirementJournal Journal = null!;
        internal SqliteRecoveryEnvelopeCatalogue Catalogue = null!;
        internal async Task InitializeAsync()
        {
            Target = new(Guid.NewGuid(), Material.Identity.InstallationId, Guid.NewGuid());
            Other = new(Guid.NewGuid(), Material.Identity.InstallationId, Guid.NewGuid());
            Request = new(Guid.NewGuid().ToString("N"), Target.ProfileId, Target.InstallationId, Guid.NewGuid());
            Journal = new(directory.Combine("retirement.sqlite"), Request, () => { });
            await Journal.CreateNewAsync(Durability, Ct);
            Catalogue = new(directory.Combine("recovery.sqlite"), Material.Identity, Durability, () => { });
            await Catalogue.CreateNewAsync(Ct);
            foreach (var binding in new[] { Target, Other })
            { Keys.Values.Add(binding, RandomNumberGenerator.GetBytes(32)); await Catalogue.CaptureAsync(binding, Keys.Values[binding], Ct); }
        }
        public void Dispose() { foreach (var key in Keys.Values.Values) CryptographicOperations.ZeroMemory(key); Material.Dispose(); directory.Dispose(); }
    }
    private sealed class Keys : IDatabaseGenerationKeyStore
    {
        internal readonly Dictionary<DatabaseKeyBinding, byte[]> Values = [];
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Retirement cannot prepare replacement keys.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => Task.FromResult((byte[])Values[binding].Clone());
    }
    private sealed class AdditionalKeys(Keys inner, DatabaseKeyBinding binding, byte[] key) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding value, CancellationToken ct) => throw new IOException("Retirement cannot prepare keys.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding value, CancellationToken ct) => value == binding ? Task.FromResult((byte[])key.Clone()) : inner.ReadAsync(value, ct);
    }
    private sealed class Durability : IDatabaseGenerationFileDurability
    {
        internal int Syncs; internal bool FailRecovery;
        public long AvailableBytes => long.MaxValue;
        public Task SynchronizeFileAsync(string path, CancellationToken ct)
        { Syncs++; if (FailRecovery && Path.GetFileName(path) == "recovery.sqlite") throw new IOException("Injected recovery synchronization failure."); return Task.CompletedTask; }
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
