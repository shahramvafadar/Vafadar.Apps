using System.Security.Cryptography;
using Vafadar.Backup;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Checks actual SQLite ancestry, native Windows key bindings and authenticated safety ownership discovery.</summary>
public sealed class ProfileRetirementReferenceDiscoveryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-235")]
    public async Task Retained_restore_and_safety_references_are_captured_without_foreign_profile_assets()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new Fixture(); await fixture.InitializeAsync();
        var protectedBytes = await new DeviceKeySafetyCopyProtection(fixture.Identity.ProfileId, fixture.Identity.InstallationId,
            fixture.Keys, () => { }).ProtectAsync(new byte[] { 1, 2, 3 }, Ct);
        await File.WriteAllBytesAsync(fixture.In("safety", "safety-owned.vbs"), protectedBytes, Ct);
        var other = fixture.Other;
        var foreignBytes = await new DeviceKeySafetyCopyProtection(other.ProfileId, other.InstallationId, fixture.Keys, () => { })
            .ProtectAsync(new byte[] { 4, 5, 6 }, Ct);
        await File.WriteAllBytesAsync(fixture.In("safety", "safety-foreign.vbs"), foreignBytes, Ct);
        await File.WriteAllTextAsync(fixture.In("portable", BackupFileName.Create("pro.example~" + fixture.Request.LegacyProfileId, DateTimeOffset.UnixEpoch)), "Owned portable", Ct);
        await File.WriteAllTextAsync(fixture.In("portable", BackupFileName.Create("pro.example~" + other.LegacyProfileId, DateTimeOffset.UnixEpoch)), "Foreign portable", Ct);
        await fixture.CaptureAsync(); var inventory = await fixture.Journal.ReadArtifactsAsync(Ct);
        Assert.Contains(inventory, item => item.Name.EndsWith(".before-restore.cipher.db", StringComparison.Ordinal));
        Assert.Contains(inventory, item => item.Name.EndsWith(".preferences.cipher.db", StringComparison.Ordinal));
        Assert.Contains(inventory, item => item.Name == "safety-owned.vbs");
        Assert.DoesNotContain(inventory, item => item.Name == "safety-foreign.vbs" || item.Name.StartsWith(other.ProfileId.ToString("N"), StringComparison.Ordinal));
        Assert.Single(inventory, item => item.Root == "portable");
        Assert.Equal(3, inventory.Count(item => item.Root == "keys")); // initial, restored and owned safety keys
        Assert.Equal(fixture.Request, await fixture.Journal.ReadAsync(Ct));
    }

    [Theory, InlineData("unknown-generation"), InlineData("malformed-backup"), Trait("AT", "AT-235")]
    public async Task Unknown_profile_namespace_artifacts_refuse_inventory_without_deleting_anything(string kind)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new Fixture(); await fixture.InitializeAsync();
        var path = kind == "unknown-generation" ? fixture.Original + "." + Guid.NewGuid().ToString("N") + ".cipher.db"
            : fixture.In("portable", "pro.example~" + fixture.Request.LegacyProfileId + "_bad.vbak");
        await File.WriteAllTextAsync(path, "Retain unknown ownership.", Ct);
        await Assert.ThrowsAsync<IOException>(() => fixture.CaptureAsync());
        Assert.Equal("Retain unknown ownership.", await File.ReadAllTextAsync(path, Ct));
        await Assert.ThrowsAsync<IOException>(() => fixture.Journal.ReadArtifactsAsync(Ct));
    }

    [Fact, Trait("AT", "AT-235")]
    public async Task Matching_but_tampered_safety_header_never_establishes_ownership()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new Fixture(); await fixture.InitializeAsync();
        var envelope = await new DeviceKeySafetyCopyProtection(fixture.Identity.ProfileId, fixture.Identity.InstallationId,
            fixture.Keys, () => { }).ProtectAsync(new byte[] { 1, 2, 3 }, Ct);
        envelope[^1] ^= 1; var path = fixture.In("safety", "safety-tampered.vbs");
        await File.WriteAllBytesAsync(path, envelope, Ct);
        await Assert.ThrowsAnyAsync<CryptographicException>(() => fixture.CaptureAsync());
        Assert.Equal(envelope, await File.ReadAllBytesAsync(path, Ct));
        await Assert.ThrowsAsync<IOException>(() => fixture.Journal.ReadArtifactsAsync(Ct));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly TemporaryDirectory folder = new();
        internal readonly Dictionary<string, string> Roots = new();
        internal DatabaseProfileIdentity Identity = null!, Other = null!;
        internal ProfileRetirementCheckpoint Request = null!;
        internal WindowsGenerationKeyStore Keys = null!;
        internal SqliteDatabaseIdentityCatalogue Identities = null!;
        internal SqliteProfileRetirementJournal Journal = null!;
        internal WindowsProfileRetirementFiles Files = null!;
        internal WindowsGenerationFileDurability Durability = null!;
        internal string Original => In("database", "owned.db");
        internal string In(string root, string name) => Path.Combine(Roots[root], name);
        internal async Task InitializeAsync()
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
            foreach (var root in new[] { "database", "keys", "portable", "safety" })
            { Roots.Add(root, folder.Combine(root)); Directory.CreateDirectory(Roots[root]); }
            Files = new(Roots, () => { }); Keys = new(Roots["keys"], () => { }); Durability = new(Roots["database"]);
            var legacy = Guid.NewGuid().ToString("N"); var other = Guid.NewGuid().ToString("N");
            Identities = new(In("database", "identities.sqlite"), () => { });
            await Identities.CreateNewAsync(["", legacy, other], Durability, Ct);
            var rows = await Identities.ReadAsync(Ct); Identity = rows.Single(row => row.LegacyProfileId == legacy); Other = rows.Single(row => row.LegacyProfileId == other);
            Request = new(legacy, Identity.ProfileId, Identity.InstallationId, Guid.NewGuid());
            Journal = new(In("database", "retirement.sqlite"), Request, () => { }); await Journal.CreateNewAsync(Durability, Ct);
            var history = new SqliteProfileRestoreHistory(Original + ".restore-history.sqlite", Identity.InitialCandidateKey, () => { });
            await history.CreateNewAsync(Durability, Ct);
            var restored = Guid.NewGuid();
            await history.ReserveAsync(new(Identity.ProfileId, Identity.CandidateGeneration, restored), Ct);
            foreach (var generation in new[] { Identity.CandidateGeneration, restored })
            {
                await Keys.PrepareAsync(new(Identity.ProfileId, Identity.InstallationId, generation), Ct);
                var candidate = Original + "." + generation.ToString("N") + ".cipher.db";
                await File.WriteAllTextAsync(candidate, "Fictitious owned ciphertext fixture.", Ct);
                await File.WriteAllTextAsync(candidate + ".before-restore.cipher.db", "Fictitious rollback fixture.", Ct);
                await File.WriteAllTextAsync(candidate + ".preferences.cipher.db", "Fictitious preference fixture.", Ct);
            }
        }
        internal Task CaptureAsync() => ProfileRetirementReferenceDiscovery.CaptureAsync(Journal, Identities, Original, "pro.example", Files, Keys, Durability, () => { }, Ct);
        public void Dispose() => folder.Dispose();
    }
}
