using System.Security.Cryptography;
using Vafadar.Backup.Security;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Real native private files and metadata reject unsafe recovered safety/reference authority.</summary>
public sealed class ProfileRecoveryReferenceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact, Trait("AT", "AT-245")]
    public async Task Authentic_safety_only_generation_is_verified_without_database_or_native_key_creation()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new Fixture(); await fixture.InitializeAsync();
        await fixture.WriteSafetyAsync(); await fixture.Verifier.VerifyAsync(fixture.Binding, fixture.Original, fixture.Key, Ct);
        Assert.Single(Directory.EnumerateFiles(fixture.Safety));
    }

    [Fact, Trait("AT", "AT-245")]
    public async Task Tampered_matching_safety_content_is_refused()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new Fixture(); await fixture.InitializeAsync(); await fixture.WriteSafetyAsync();
        var path = Path.Combine(fixture.Safety, "packet.vbs"); var bytes = await File.ReadAllBytesAsync(path, Ct); bytes[^1] ^= 1;
        await File.WriteAllBytesAsync(path, bytes, Ct);
        await Assert.ThrowsAnyAsync<CryptographicException>(() => fixture.Verifier.VerifyAsync(fixture.Binding, fixture.Original, fixture.Key, Ct));
    }

    [Fact, Trait("AT", "AT-245")]
    public async Task Foreign_safety_header_does_not_establish_a_reference_for_this_key()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new Fixture(); await fixture.InitializeAsync(); await fixture.WriteSafetyAsync(Guid.NewGuid());
        await Assert.ThrowsAsync<IOException>(() => fixture.Verifier.VerifyAsync(fixture.Binding, fixture.Original, fixture.Key, Ct));
    }

    [Fact, Trait("AT", "AT-245")]
    public async Task Unknown_database_namespace_refuses_recovery_even_with_authentic_safety()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new Fixture(); await fixture.InitializeAsync(); await fixture.WriteSafetyAsync();
        await File.WriteAllTextAsync(fixture.Original + ".unknown.cipher.db", "Fictitious unclassified artifact", Ct);
        await Assert.ThrowsAsync<IOException>(() => fixture.Verifier.VerifyAsync(fixture.Binding, fixture.Original, fixture.Key, Ct));
    }

    [Fact, Trait("AT", "AT-245")]
    public async Task Missing_selected_generation_cannot_be_replaced_by_an_authentic_safety_packet()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new Fixture(); await fixture.InitializeAsync();
        fixture.Binding = fixture.Initial;
        await fixture.WriteSafetyAsync();
        await Assert.ThrowsAsync<IOException>(() => fixture.Verifier.VerifyAsync(fixture.Binding, fixture.Original, fixture.Key, Ct));
    }

    [Theory, Trait("AT", "AT-246")]
    [InlineData(DatabaseGenerationPhase.CandidateVerified)]
    [InlineData(DatabaseGenerationPhase.Selected)]
    public async Task Committed_candidate_is_required_before_history_selection_is_mirrored(DatabaseGenerationPhase phase)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var fixture = new Fixture(); await fixture.InitializeAsync(); await fixture.WriteSafetyAsync();
        var history = new SqliteProfileRestoreHistory(fixture.Original + ".restore-history.sqlite", fixture.Initial, () => { });
        var intent = new DatabaseGenerationCheckpoint(fixture.Binding.ProfileId, fixture.Initial.GenerationId, fixture.Binding.GenerationId);
        await history.ReserveAsync(intent, Ct);
        var candidate = fixture.Original + "." + fixture.Binding.GenerationId.ToString("N") + ".cipher.db";
        var journal = new SqliteDatabaseGenerationJournal(candidate + ".restore.sqlite", intent);
        await journal.CreateNewAsync(Ct); var state = intent;
        while (state.Phase < phase)
        {
            var next = state.Advance(state.ProfileId, state.CandidateGeneration, state.Phase + 1);
            await journal.CompareAndCommitAsync(state, next, Ct); state = next;
        }
        if (phase == DatabaseGenerationPhase.Selected)
        {
            // These deliberately unread files isolate the missing candidate gate; the verifier
            // must refuse before any attempt to authenticate other retained cipher references.
            await File.WriteAllTextAsync(candidate + ".before-restore.cipher.db", "Unopened fixture rollback", Ct);
            await File.WriteAllTextAsync(candidate + ".preferences.cipher.db", "Unopened fixture preferences", Ct);
        }
        Assert.False((await history.ReadAsync(Ct))[^1].Selected);
        var error = await Assert.ThrowsAsync<IOException>(() => fixture.Verifier.VerifyAsync(fixture.Binding, fixture.Original, fixture.Key, Ct));
        Assert.Contains("Selected recovery generation is missing", error.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(candidate));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly TemporaryDirectory directory = new();
        public string Original => directory.Combine("zanance.db");
        public string Safety => directory.Combine("safety");
        public byte[] Key { get; } = RandomNumberGenerator.GetBytes(32);
        public DatabaseKeyBinding Initial { get; private set; } = null!;
        public DatabaseKeyBinding Binding { get; set; } = null!;
        public ProfileRecoveryReferenceVerifier Verifier { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            Directory.CreateDirectory(Safety); var portable = directory.Combine("portable"); Directory.CreateDirectory(portable);
            var identities = new SqliteDatabaseIdentityCatalogue(directory.Combine("identities.sqlite"), () => { });
            await identities.CreateNewAsync([""], Ct); Initial = (await identities.ReadAsync(Ct)).Single().InitialCandidateKey;
            Binding = new(Initial.ProfileId, Initial.InstallationId, Guid.NewGuid());
            await new SqliteProfileRestoreHistory(Original + ".restore-history.sqlite", Initial, () => { }).CreateNewAsync(Ct);
            if (OperatingSystem.IsWindows())
                Verifier = new(identities, new WindowsProfileRetirementFiles(new Dictionary<string, string> { ["database"] = directory.Path,
                    ["safety"] = Safety, ["portable"] = portable }, () => { }), new WindowsGenerationFileDurability(directory.Path), [], "4.19.0", 1024 * 1024, () => { });
        }

        public async Task WriteSafetyAsync(Guid? profile = null)
        {
            var identity = new byte[48]; (profile ?? Binding.ProfileId).TryWriteBytes(identity.AsSpan(0, 16), bigEndian: true, out _);
            Binding.InstallationId.TryWriteBytes(identity.AsSpan(16, 16), bigEndian: true, out _);
            Binding.GenerationId.TryWriteBytes(identity.AsSpan(32, 16), bigEndian: true, out _);
            var envelope = DeviceSafetyEncryption.Protect("Fictitious safety bytes"u8, Key, identity);
            try { await File.WriteAllBytesAsync(Path.Combine(Safety, "packet.vbs"), envelope, Ct); }
            finally { CryptographicOperations.ZeroMemory(envelope); }
        }

        public void Dispose() { CryptographicOperations.ZeroMemory(Key); directory.Dispose(); }
    }
}
