namespace Vafadar.Data;

/// <summary>Publishes fully committed private operational metadata without exposing an incomplete final file.</summary>
internal static class SqliteMetadataPublication
{
    /// <summary>Creates or resumes a verified staged record and renames it without replacing existing metadata.</summary>
    internal static async Task CreateAsync(string path, Func<string, Task> create, Func<string, Task> verify,
        Action validate, IDatabaseGenerationFileDurability? durability, CancellationToken ct)
    {
        void Check() { ct.ThrowIfCancellationRequested(); validate(); }
        void RequireAbsent(string target)
        {
            foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
                if (File.Exists(target + suffix) || Directory.Exists(target + suffix))
                    throw new IOException("Private metadata already exists or is interrupted.");
        }
        Check(); RequireAbsent(path);
        var staged = path + ".creating";
        if (!File.Exists(staged)) { RequireAbsent(staged); await create(staged).ConfigureAwait(false); }
        Check(); await verify(staged).ConfigureAwait(false); Check();
        foreach (var suffix in new[] { "-journal", "-wal", "-shm" })
            if (File.Exists(staged + suffix) || Directory.Exists(staged + suffix))
                throw new IOException("Private metadata still has transaction sidecars.");
        // SQLite EXTRA already commits its transaction. Explicit platform durability also qualifies the
        // staged file and directory before/after rename; a managed-only caller makes no power-loss claim.
        if (durability is not null) await durability.SynchronizeFileAsync(staged, ct).ConfigureAwait(false);
        Check(); RequireAbsent(path); File.Move(staged, path, overwrite: false);
        if (durability is not null) await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
        Check(); await verify(path).ConfigureAwait(false); Check();
    }
}
