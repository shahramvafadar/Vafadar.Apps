using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Vafadar.Data.Tests;

/// <summary>Unavailable or non-file metadata cannot be mistaken for a fresh protected profile.</summary>
public sealed class NewProfileMetadataPresenceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vafadar-profile-metadata-" + Guid.NewGuid().ToString("N"));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Creates only the independent operational-metadata fixture root.</summary>
    public NewProfileMetadataPresenceTests() => Directory.CreateDirectory(root);

    [Theory, InlineData(false), InlineData(true)]
    public Task Final_journal_directory_never_authorizes_new_metadata_or_keys(bool retainedIntent) =>
        CheckAsync("final-directory", retainedIntent, expectedPrepare: 0);

    [Theory, InlineData("intent-directory", false), InlineData("history-directory", true)]
    public Task Other_metadata_directories_remain_refused(string kind, bool retainedIntent) =>
        CheckAsync(kind, retainedIntent, expectedPrepare: 0);

    [Fact]
    public Task Absent_final_with_retained_intent_keeps_original_key_entry() =>
        CheckAsync("absent-final", retainedIntent: true, expectedPrepare: 1);

    private async Task CheckAsync(string kind, bool retainedIntent, int expectedPrepare)
    {
        var original = Path.Combine(root, "new.db");
        var location = new LocalDatabaseLocation<UnusedContext>(original, protectionRequired: true);
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(root, "identities.sqlite"), owner.Validate);
        await identities.CreateNewAsync([""], Ct);
        var identity = Assert.Single(await identities.ReadAsync(Ct));
        var journal = new SqliteDatabaseGenerationJournal(original + ".new-profile.sqlite", identity.InitialMigration);
        if (retainedIntent)
        {
            await journal.CreateNewAsync(Ct);
            if (kind != "history-directory")
                await new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, owner.Validate)
                    .CreateInitialIntentAsync(identity.InitialMigration, journal, Ct);
        }
        var target = kind switch
        {
            "final-directory" => original + ".generation.sqlite",
            "intent-directory" => original + ".new-profile.sqlite",
            "history-directory" => original + ".restore-history.sqlite",
            _ => null
        };
        if (target is not null) Directory.CreateDirectory(target);
        var before = CaptureFiles();
        var beforeDirectories = Directory.GetDirectories(root).Order(StringComparer.Ordinal).ToArray();
        var keys = new StoppingKeys(); var durability = new CountingDurability(); int schemas = 0;

        var failure = await Record.ExceptionAsync(async () =>
        {
            using var generation = await SqlCipherNewProfile.OpenAsync(location, identities, "", original, !retainedIntent,
                keys, durability, (_, _) => { schemas++; throw new InvalidOperationException("Unexpected schema construction."); },
                owner, owner.Validate, 1024 * 1024, "4.19.0", Ct);
        });

        Assert.True(expectedPrepare == 0 ? failure is IOException : failure is InvalidOperationException,
            $"Expected metadata boundary; prepared={keys.Prepared}, reads={keys.Reads}, newMetadata={CaptureFiles().Count - before.Count}, sync={durability.Calls}, error={failure?.GetType().Name ?? "none"}.");
        Assert.Equal(expectedPrepare, keys.Prepared); Assert.Equal(0, keys.Reads);
        Assert.Equal(0, durability.Calls); Assert.Equal(0, schemas);
        Assert.Equal(before.OrderBy(row => row.Key), CaptureFiles().OrderBy(row => row.Key));
        Assert.Equal(beforeDirectories, Directory.GetDirectories(root).Order(StringComparer.Ordinal).ToArray());
        Assert.False(File.Exists(original)); Assert.False(location.IsProtected);
        if (target is not null) Assert.Empty(Directory.EnumerateFileSystemEntries(target));
    }

    private Dictionary<string, string> CaptureFiles() => Directory.EnumerateFiles(root)
        .ToDictionary(path => Path.GetFileName(path)!, path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));

    /// <summary>Deletes only this unique fixture below the current process-local temporary root.</summary>
    public void Dispose()
    {
        var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(root).StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase)) throw new IOException("Fixture cleanup escaped its temporary root.");
        Directory.Delete(root, recursive: true);
    }

    private sealed class UnusedContext() : LocalDbContext(new DbContextOptionsBuilder().Options);
    private sealed class StoppingKeys : IDatabaseGenerationKeyStore
    {
        internal int Prepared, Reads;
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct)
        { Prepared++; throw new InvalidOperationException("Deliberate synthetic key-boundary stop."); }
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct)
        { Reads++; throw new InvalidOperationException("Unexpected key read."); }
    }
    private sealed class CountingDurability : IDatabaseGenerationFileDurability
    {
        internal int Calls;
        public long AvailableBytes { get { Calls++; throw new InvalidOperationException("Unexpected storage query."); } }
        public Task SynchronizeFileAsync(string path, CancellationToken ct) { Calls++; return Task.CompletedTask; }
        public Task SynchronizeDirectoryAsync(CancellationToken ct) { Calls++; return Task.CompletedTask; }
    }
}
