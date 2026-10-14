using System.ComponentModel;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Vafadar.Data;

/// <summary>Captures and removes only exact journaled native Windows file objects in named private roots.</summary>
/// <remarks>The host supplies complete reference discovery and exclusive maintenance ownership.
/// This class does not enumerate filenames, classify profile ownership or erase physical storage sectors.</remarks>
[SupportedOSPlatform("windows")]
public sealed class WindowsProfileRetirementFiles : IProfileRetirementReferenceFiles
{
    private readonly IReadOnlyDictionary<string, string> roots;
    private readonly Action validate;

    /// <summary>Captures actual host-owned local roots and the current maintenance ownership check.</summary>
    public WindowsProfileRetirementFiles(IReadOnlyDictionary<string, string> privateRoots, Action validateAccess)
    {
        ArgumentNullException.ThrowIfNull(privateRoots); ArgumentNullException.ThrowIfNull(validateAccess);
        roots = privateRoots.ToDictionary(row => row.Key, row => Path.GetFullPath(row.Value), StringComparer.Ordinal);
        validate = validateAccess;
        foreach (var row in privateRoots)
        {
            if (!Path.IsPathFullyQualified(row.Value)) throw new ArgumentException("Retirement roots must be absolute.");
            using var directory = new WindowsPrivateFiles(roots[row.Key]);
        }
        validate();
    }

    /// <summary>Fingerprints one explicitly discovered owned file through its no-follow native handle.</summary>
    public async Task<ProfileRetirementArtifact> CaptureAsync(string root, string name, CancellationToken ct = default)
    {
        var sample = new ProfileRetirementArtifact(root, name, "0", 0, new string('0', 64));
        Check(ct); var path = Resolve(sample);
        using var directory = new WindowsPrivateFiles(roots[root]);
        using var handle = directory.OpenForDeletion(path);
        using var stream = new FileStream(handle, FileAccess.Read);
        var length = stream.Length; var identity = WindowsPrivateFiles.FileIdentity(handle);
        var digest = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false));
        Check(ct);
        if (stream.Length != length) throw new IOException("Captured retirement file changed while hashing.");
        return new(root, name, identity, length, digest, directory.DirectoryIdentity);
    }

    /// <summary>Lists bounded direct entries in the actual host-owned private root; never applies a deletion pattern.</summary>
    public Task<IReadOnlyList<string>> ListNamesAsync(string root, CancellationToken ct = default)
    {
        Check(ct); var path = roots.TryGetValue(root, out var value) ? value : throw new IOException("Unknown private root.");
        using var directory = new WindowsPrivateFiles(path);
        var names = Directory.EnumerateFileSystemEntries(path).Select(entry => Path.GetFileName(entry)!).Take(4097).ToArray();
        if (names.Length > 4096) throw new IOException("Private reference listing exceeds its bound.");
        Check(ct); return Task.FromResult<IReadOnlyList<string>>(names!);
    }

    /// <summary>Reads only a bounded regular owned file or its prefix through the same verified native handle.</summary>
    public async Task<byte[]> ReadAsync(string root, string name, int maximumBytes, bool prefix, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        var sample = new ProfileRetirementArtifact(root, name, "0", 0, new string('0', 64));
        Check(ct); var path = Resolve(sample); using var directory = new WindowsPrivateFiles(roots[root]);
        using var handle = directory.OpenFile(path, writable: false); using var stream = new FileStream(handle, FileAccess.Read);
        if (!prefix && stream.Length > maximumBytes) throw new IOException("Private reference file exceeds its read bound.");
        var result = new byte[checked((int)Math.Min(stream.Length, maximumBytes))];
        try { await stream.ReadExactlyAsync(result, ct).ConfigureAwait(false); Check(ct); return result; }
        catch { CryptographicOperations.ZeroMemory(result); throw; }
    }

    /// <summary>Preflights the captured set, then deletes matching objects and synchronizes actual directories.</summary>
    public Task RemoveAsync(ProfileRetirementCheckpoint expected, SqliteProfileRetirementJournal journal,
        IDatabaseGenerationFileDurability metadataDurability, CancellationToken ct = default) =>
        ProfileRetirementFileCleanup.RemoveAsync(expected, journal, metadataDurability,
            (artifact, delete, token) => CheckFileAsync(artifact, journal, delete, token),
            token => VerifyRemovedAsync(journal, token), validate, ct);

    /// <summary>Verifies every captured namespace entry is absent, propagating access and redirection failures.</summary>
    public Task VerifyRemovedAsync(SqliteProfileRetirementJournal journal, CancellationToken ct = default) => VerifyCoreAsync(journal, ct);

    /// <summary>Removes one exact captured object only while its caller's immutable retention authority remains durable.</summary>
    /// <remarks>DPAPI system master keys are outside these owned roots and are never removed.</remarks>
    public async Task RemoveRetainedArtifactAsync(ProfileRetirementArtifact artifact,
        Func<CancellationToken, Task> requireDurableAuthority, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(requireDurableAuthority);
        Check(ct); await requireDurableAuthority(ct).ConfigureAwait(false);
        var path = Resolve(artifact);
        using var directory = new WindowsPrivateFiles(roots[artifact.Root]);
        if (directory.DirectoryIdentity != artifact.RootIdentity)
            throw new IOException("Captured retention root was replaced.");
        try
        {
            using var handle = directory.OpenForDeletion(path);
            using var stream = new FileStream(handle, FileAccess.Read);
            if (WindowsPrivateFiles.FileIdentity(handle) != artifact.NativeIdentity || stream.Length != artifact.Length)
                throw new IOException("Captured retention file was replaced or changed.");
            var digest = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false));
            if (digest != artifact.Sha256 || stream.Length != artifact.Length)
                throw new IOException("Captured retention bytes changed.");
            await requireDurableAuthority(ct).ConfigureAwait(false); Check(ct);
            // Windows deletes the inspected handle's object rather than resolving its name again.
            WindowsPrivateFiles.DeleteOnClose(handle);
        }
        catch (IOException error) when (MissingFile(error)) { }
        directory.FlushDirectory(); Check(ct);
    }

    private async Task VerifyCoreAsync(SqliteProfileRetirementJournal journal, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(journal);
        foreach (var artifact in await journal.ReadArtifactsAsync(ct).ConfigureAwait(false))
        {
            Check(ct); var path = Resolve(artifact); journal.RequireArtifactPath(path);
            using var directory = new WindowsPrivateFiles(roots[artifact.Root]);
            if (directory.DirectoryIdentity != artifact.RootIdentity) throw new IOException("Captured retirement root was replaced.");
            try { using var handle = directory.OpenForDeletion(path); throw new IOException("Captured retirement file remains."); }
            catch (IOException error) when (MissingFile(error)) { }
            directory.FlushDirectory(); Check(ct);
        }
    }

    private async Task CheckFileAsync(ProfileRetirementArtifact artifact, SqliteProfileRetirementJournal journal, bool delete, CancellationToken ct)
    {
        Check(ct); var path = Resolve(artifact); journal.RequireArtifactPath(path);
        using var directory = new WindowsPrivateFiles(roots[artifact.Root]);
        if (directory.DirectoryIdentity != artifact.RootIdentity) throw new IOException("Captured retirement root was replaced.");
        try
        {
            using var handle = directory.OpenForDeletion(path);
            using var stream = new FileStream(handle, FileAccess.Read);
            if (WindowsPrivateFiles.FileIdentity(handle) != artifact.NativeIdentity || stream.Length != artifact.Length)
                throw new IOException("Retirement file object was replaced or changed.");
            var digest = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false));
            Check(ct);
            if (digest != artifact.Sha256 || stream.Length != artifact.Length) throw new IOException("Retirement file content changed.");
            if (delete) WindowsPrivateFiles.DeleteOnClose(handle);
        }
        catch (IOException error) when (MissingFile(error)) { }
        if (delete) directory.FlushDirectory(); Check(ct);
    }
    private string Resolve(ProfileRetirementArtifact artifact) => roots.TryGetValue(artifact.Root, out var root)
        ? Path.Combine(root, artifact.Name) : throw new IOException("Unknown captured private root.");
    private void Check(CancellationToken ct) { ct.ThrowIfCancellationRequested(); validate(); }
    private static bool MissingFile(IOException error) => error.InnerException is Win32Exception { NativeErrorCode: 2 };
}
