namespace Vafadar.Data.Tests;

/// <summary>Unknown candidate namespaces must refuse before any platform key preparation or read.</summary>
public sealed class MigrationKeyPreparationNamespaceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vafadar-key-namespace-" + Guid.NewGuid().ToString("N"));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Creates only an independent metadata fixture, without a financial database or real key.</summary>
    public MigrationKeyPreparationNamespaceTests() => Directory.CreateDirectory(root);

    [Theory]
    [InlineData(false, "", true)]
    [InlineData(true, "", true)]
    [InlineData(false, "-journal", false)]
    [InlineData(false, "-wal", false)]
    [InlineData(false, "-shm", false)]
    [InlineData(true, "-journal", false)]
    [InlineData(true, "-wal", false)]
    [InlineData(true, "-shm", false)]
    public async Task Existing_namespace_refuses_before_key_effects(bool rollback, string suffix, bool directory)
    {
        var state = new DatabaseGenerationCheckpoint(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var journal = new SqliteDatabaseGenerationJournal(Path.Combine(root, "generation.sqlite"), state);
        await journal.CreateNewAsync(Ct);
        var original = Path.Combine(root, "profile.db");
        byte[] sentinel = [12, 34, 56, 78];
        await File.WriteAllBytesAsync(original, sentinel, Ct);
        var keys = new CountingKeys();
        var session = new SqlCipherGenerationMigrationSession(state, Guid.NewGuid(), original, journal,
            keys, new NoEffectsDurability(), () => { }, 1024 * 1024, "4.19.0");
        var target = (rollback ? session.RollbackPath : session.CandidatePath) + suffix;
        if (directory) Directory.CreateDirectory(target);
        else await File.WriteAllBytesAsync(target, sentinel, Ct);

        var failure = await Record.ExceptionAsync(() => session.PrepareKeyAsync(state, Ct));

        Assert.True(failure is IOException,
            $"Expected namespace refusal; prepared={keys.Prepared}, reads={keys.Reads}, error={failure?.GetType().Name ?? "none"}.");
        Assert.Equal(0, keys.Prepared);
        Assert.Equal(0, keys.Reads);
        Assert.Equal(state, await journal.ReadAsync(Ct));
        Assert.Equal(sentinel, await File.ReadAllBytesAsync(original, Ct));
        if (directory) Assert.Empty(Directory.EnumerateFileSystemEntries(target));
        else Assert.Equal(sentinel, await File.ReadAllBytesAsync(target, Ct));
    }

    /// <summary>Removes only this unique fixture under the current process's temporary root.</summary>
    public void Dispose()
    {
        var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(root).StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Fixture cleanup escaped its temporary root.");
        Directory.Delete(root, recursive: true);
    }

    private sealed class CountingKeys : IDatabaseGenerationKeyStore
    {
        internal int Prepared, Reads;
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken)
        { Prepared++; return Task.CompletedTask; }
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken)
        { Reads++; return Task.FromResult(new byte[32]); }
    }

    private sealed class NoEffectsDurability : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => 1024 * 1024 * 1024;
        public Task SynchronizeFileAsync(string path, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SynchronizeDirectoryAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
