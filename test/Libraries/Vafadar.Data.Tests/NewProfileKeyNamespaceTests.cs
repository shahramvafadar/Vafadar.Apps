using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Vafadar.Data.Tests;

/// <summary>Retained pre-key new-profile intent cannot adopt an occupied generation namespace.</summary>
public sealed class NewProfileKeyNamespaceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vafadar-new-key-namespace-" + Guid.NewGuid().ToString("N"));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Creates only this independent operational-metadata fixture root.</summary>
    public NewProfileKeyNamespaceTests() => Directory.CreateDirectory(root);

    [Theory]
    [InlineData(false, "", true)]
    [InlineData(true, "", true)]
    [InlineData(false, "-journal", false)]
    [InlineData(false, "-wal", false)]
    [InlineData(false, "-shm", false)]
    [InlineData(true, "-journal", false)]
    [InlineData(true, "-wal", false)]
    [InlineData(true, "-shm", false)]
    [InlineData(false, "", false)]
    public async Task Retained_original_intent_refuses_unknown_namespace_before_keys(bool rollback, string suffix, bool directory)
    {
        var original = Path.Combine(root, "new.db");
        var location = new LocalDatabaseLocation<UnusedContext>(original, protectionRequired: true);
        using var owner = await location.Access.AcquireExclusiveAsync(Ct);
        var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(root, "identities.sqlite"), owner.Validate);
        await identities.CreateNewAsync([""], Ct);
        var identity = Assert.Single(await identities.ReadAsync(Ct));
        var journal = new SqliteDatabaseGenerationJournal(original + ".new-profile.sqlite", identity.InitialMigration);
        await journal.CreateNewAsync(Ct);
        var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, owner.Validate);
        await history.CreateInitialIntentAsync(identity.InitialMigration, journal, Ct);
        var candidate = original + "." + identity.CandidateGeneration.ToString("N") + ".cipher.db";
        var target = (rollback ? candidate + ".rollback.db" : candidate) + suffix;
        byte[] sentinel = [12, 34, 56, 78];
        if (directory) Directory.CreateDirectory(target);
        else if (suffix.Length > 0) await File.WriteAllBytesAsync(target, sentinel, Ct);
        var before = CaptureFiles();
        var keys = new RefusingKeys();
        var durability = new NoEffectsDurability();
        int schemas = 0;

        var failure = await Record.ExceptionAsync(async () =>
        {
            using var generation = await SqlCipherNewProfile.OpenAsync(location, identities, "", original, false,
                keys, durability, (_, _) => { schemas++; throw new InvalidOperationException("Unexpected schema construction."); },
                owner, owner.Validate, 1024 * 1024, "4.19.0", Ct);
        });

        bool occupied = directory || suffix.Length > 0;
        Assert.True(occupied ? failure is IOException : failure is InvalidOperationException,
            $"Expected pre-key refusal; prepared={keys.Prepared}, reads={keys.Reads}, error={failure?.GetType().Name ?? "none"}.");
        Assert.Equal(occupied ? 0 : 1, keys.Prepared); Assert.Equal(0, keys.Reads);
        Assert.Equal(0, durability.Calls); Assert.Equal(0, schemas);
        Assert.Equal(identity.InitialMigration, await journal.ReadAsync(Ct));
        Assert.Equal(before.OrderBy(row => row.Key), CaptureFiles().OrderBy(row => row.Key));
        Assert.False(File.Exists(original)); Assert.False(File.Exists(original + ".generation.sqlite"));
        Assert.False(location.IsProtected);
        if (directory) Assert.Empty(Directory.EnumerateFileSystemEntries(target));
        else if (occupied) Assert.Equal(sentinel, await File.ReadAllBytesAsync(target, Ct));
        else Assert.False(File.Exists(target));
    }

    private Dictionary<string, string> CaptureFiles() => Directory.EnumerateFiles(root)
        .ToDictionary(path => Path.GetFileName(path)!, path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));

    /// <summary>Deletes only this unique fixture below the process-local temporary root.</summary>
    public void Dispose()
    {
        var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(root).StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Fixture cleanup escaped its temporary root.");
        Directory.Delete(root, recursive: true);
    }

    private sealed class UnusedContext() : LocalDbContext(new DbContextOptionsBuilder().Options);
    private sealed class RefusingKeys : IDatabaseGenerationKeyStore
    {
        internal int Prepared, Reads;
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken)
        { Prepared++; throw new InvalidOperationException("Unexpected key preparation reached."); }
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken)
        { Reads++; throw new InvalidOperationException("Unexpected key read reached."); }
    }
    private sealed class NoEffectsDurability : IDatabaseGenerationFileDurability
    {
        internal int Calls;
        public long AvailableBytes { get { Calls++; throw new InvalidOperationException("Unexpected storage effect."); } }
        public Task SynchronizeFileAsync(string path, CancellationToken cancellationToken)
        { Calls++; throw new InvalidOperationException("Unexpected synchronization."); }
        public Task SynchronizeDirectoryAsync(CancellationToken cancellationToken)
        { Calls++; throw new InvalidOperationException("Unexpected synchronization."); }
    }
}
