using Vafadar.Core.Settings;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Checks actual retained ancestry before native key effects; ordinary SQLite does not substitute for cipher verification.</summary>
public sealed class RetainedRestoreTests
{
    [Theory, InlineData(0), InlineData(-1), InlineData(1), InlineData(2)]
    public void Invalid_later_reference_is_refused_without_creating_metadata_or_accessing_keys(int variant)
    {
        using var root = new TemporaryDirectory();
        var identity = new DatabaseProfileIdentity(Guid.NewGuid(), "", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var keys = new Keys();
        var reference = variant switch
        {
            0 => new ProfileRestoreReference(0, identity.CandidateGeneration, Guid.NewGuid(), false),
            -1 => new ProfileRestoreReference(-1, identity.CandidateGeneration, Guid.NewGuid(), false),
            1 => new ProfileRestoreReference(1, identity.CandidateGeneration, identity.CandidateGeneration, false),
            _ => new ProfileRestoreReference(1, identity.CandidateGeneration, Guid.Empty, false)
        };
        Assert.Throws<ArgumentException>(() => Create(root, identity, reference, keys));
        Assert.Empty(Directory.EnumerateFileSystemEntries(root.Path));
        Assert.Equal(0, keys.Reads); Assert.Equal(0, keys.Preparations);
    }

    [Fact]
    public async Task Unrecorded_restore_reference_is_refused_before_reading_its_preference_key()
    {
        using var root = new TemporaryDirectory(); var ct = TestContext.Current.CancellationToken;
        var catalogue = new SqliteDatabaseIdentityCatalogue(root.Combine("identities.sqlite"), () => { });
        await catalogue.CreateNewAsync([""], ct);
        var identity = (await catalogue.ReadAsync(ct)).Single();
        var original = root.Combine("zanance.db");
        await new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, () => { }).CreateNewAsync(ct);
        var keys = new Keys();
        var receiver = Create(root, identity, new(1, identity.CandidateGeneration, Guid.NewGuid(), false), keys);
        await Assert.ThrowsAsync<InvalidOperationException>(() => receiver.ResumeAsync(ct));
        Assert.Equal(0, keys.Reads); Assert.Equal(0, keys.Preparations);
        Assert.False(File.Exists(original));
        Assert.Equal(2, Directory.EnumerateFiles(root.Path).Count());
    }

    private static SqlCipherPackageRestoreReceiver Create(TemporaryDirectory root, DatabaseProfileIdentity identity, ProfileRestoreReference reference, Keys keys) =>
        SqlCipherRetainedRestore.Create(identity, root.Combine("zanance.db"), reference, keys, new Durability(), new InMemorySettingsStore(),
            ["localization.language"], () => { }, (_, _) => throw new IOException("Unexpected upgrade."),
            _ => throw new IOException("Unexpected publication."), _ => throw new IOException("Unexpected preference persistence."), 1024 * 1024, "4.19.0");

    private sealed class Keys : IDatabaseGenerationKeyStore
    {
        internal int Reads; internal int Preparations;
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) { Preparations++; throw new IOException("Unexpected key preparation."); }
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) { Reads++; throw new IOException("Unexpected key read."); }
    }
    private sealed class Durability : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => throw new IOException("Unexpected storage effect.");
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => throw new IOException("Unexpected storage effect.");
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => throw new IOException("Unexpected storage effect.");
    }
}
