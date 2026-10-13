using Android.OS;
using Android.Systems;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Inspects exact UID-private Android directory/file objects and unlinks captured entries through their directory descriptor.</summary>
/// <remarks>The host supplies complete authenticated reference discovery and exclusive maintenance ownership.
/// This is namespace cleanup, not physical erasure or a defence against privileged/same-UID code.</remarks>
public sealed class AndroidProfileRetirementFiles
{
    private readonly IReadOnlyDictionary<string, string> roots;
    private readonly Action validate;

    /// <summary>Captures named absolute private roots without creating directories or inferring ownership.</summary>
    public AndroidProfileRetirementFiles(IReadOnlyDictionary<string, string> privateRoots, Action validateAccess)
    {
        ArgumentNullException.ThrowIfNull(privateRoots); ArgumentNullException.ThrowIfNull(validateAccess);
        if (privateRoots.Values.Any(path => !Path.IsPathFullyQualified(path))) throw new ArgumentException("Retirement roots must be absolute.");
        roots = privateRoots.ToDictionary(row => row.Key, row => Path.GetFullPath(row.Value), StringComparer.Ordinal);
        validate = validateAccess; validate();
    }

    /// <summary>Captures native root/file identities, complete length and SHA-256 using no-follow directory-relative handles.</summary>
    public async Task<ProfileRetirementArtifact> CaptureAsync(string root, string name, CancellationToken ct = default)
    {
        var sample = new ProfileRetirementArtifact(root, name, "0", 0, new string('0', 64));
        Check(ct); using var directory = OpenRoot(sample); using var handle = directory.OpenFile(name);
        var identity = Inspect(handle); using var stream = new FileStream(handle, FileAccess.Read);
        var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false));
        Check(ct); if (Inspect(handle) != identity) throw new IOException("Captured Android file changed while hashing.");
        return new(root, name, identity.Identity, identity.Length, hash, directory.Identity);
    }

    /// <summary>Uses the shared durable preflight/removal algorithm with actual Android descriptor-relative effects.</summary>
    public Task RemoveAsync(ProfileRetirementCheckpoint expected, SqliteProfileRetirementJournal journal,
        IDatabaseGenerationFileDurability metadataDurability, CancellationToken ct = default) =>
        ProfileRetirementFileCleanup.RemoveAsync(expected, journal, metadataDurability,
            (artifact, delete, token) => CheckFileAsync(artifact, journal, delete, token),
            token => VerifyRemovedAsync(journal, token), validate, ct);

    /// <summary>Verifies all captured entries are absent in their original native directory objects and synchronizes those directories.</summary>
    public async Task VerifyRemovedAsync(SqliteProfileRetirementJournal journal, CancellationToken ct = default)
    {
        foreach (var artifact in await journal.ReadArtifactsAsync(ct).ConfigureAwait(false))
        {
            Check(ct); journal.RequireArtifactPath(Resolve(artifact)); using var directory = OpenRoot(artifact);
            RequireRoot(directory, artifact);
            try { using var handle = directory.OpenFile(artifact.Name); throw new IOException("Captured Android retirement file remains."); }
            catch (IOException error) when (Missing(error)) { }
            directory.Sync(); Check(ct);
        }
    }

    private async Task CheckFileAsync(ProfileRetirementArtifact artifact, SqliteProfileRetirementJournal journal, bool delete, CancellationToken ct)
    {
        Check(ct); journal.RequireArtifactPath(Resolve(artifact)); using var directory = OpenRoot(artifact);
        RequireRoot(directory, artifact);
        try
        {
            using var handle = directory.OpenFile(artifact.Name); var identity = Inspect(handle);
            if (identity.Identity != artifact.NativeIdentity || identity.Length != artifact.Length)
                throw new IOException("Captured Android retirement object was changed or replaced.");
            using var stream = new FileStream(handle, FileAccess.Read);
            var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false));
            Check(ct);
            if (hash != artifact.Sha256 || Inspect(handle) != identity) throw new IOException("Captured Android retirement bytes changed.");
            // unlinkat removes a name, not an open descriptor. Recheck that the anchored namespace still
            // names the inspected object under the host's exclusive owner before calling the native API.
            using var named = directory.OpenFile(artifact.Name);
            if (Inspect(named) != identity) throw new IOException("Android retirement namespace changed.");
            if (delete) directory.Unlink(artifact.Name);
        }
        catch (IOException error) when (Missing(error)) { }
        if (delete) directory.Sync(); Check(ct);
    }

    private DirectoryOwner OpenRoot(ProfileRetirementArtifact artifact) => new(roots.TryGetValue(artifact.Root, out var path)
        ? path : throw new IOException("Unknown captured Android private root."));
    private string Resolve(ProfileRetirementArtifact artifact) => roots.TryGetValue(artifact.Root, out var root)
        ? Path.Combine(root, artifact.Name) : throw new IOException("Unknown captured Android private root.");
    private static void RequireRoot(DirectoryOwner directory, ProfileRetirementArtifact artifact)
    { if (directory.Identity != artifact.RootIdentity) throw new IOException("Captured Android retirement root was replaced."); }
    private void Check(CancellationToken ct) { ct.ThrowIfCancellationRequested(); validate(); }
    private static bool Missing(IOException error) => error.InnerException is Win32Exception { NativeErrorCode: 2 };

    /// <summary>Native file object facts, independent of path spelling and shared file offsets.</summary>
    private readonly record struct FileIdentity(string Identity, long Length);
    private static FileIdentity Inspect(SafeFileHandle handle)
    {
        using var descriptor = ParcelFileDescriptor.FromFd(handle.DangerousGetHandle().ToInt32())!;
        try
        {
            using var stat = Os.Fstat(descriptor.FileDescriptor!)!;
            if (!OsConstants.S_ISREG(stat.StMode) || stat.StNlink != 1 || stat.StUid != Android.OS.Process.MyUid())
                throw new IOException("Android retirement file is linked, non-regular or foreign-owned.");
            return new(IdentityOf(stat), stat.StSize);
        }
        finally { descriptor.Close(); }
    }
    private static string IdentityOf(StructStat stat) => $"{unchecked((ulong)stat.StDev):x16}{unchecked((ulong)stat.StIno):x16}";
    private static IOException NativeError() => new("Android private retirement operation failed.", new Win32Exception(Marshal.GetLastPInvokeError()));

    /// <summary>Owns an actual UID-private root descriptor; file opens and removals are relative to it.</summary>
    private sealed class DirectoryOwner : IDisposable
    {
        private readonly Java.IO.FileDescriptor descriptor;
        private readonly ParcelFileDescriptor owned;
        /// <summary>Native device/inode identity of the captured directory.</summary>
        internal string Identity { get; }
        /// <summary>Resolves the operating system's app-data alias, then opens a non-followed private directory.</summary>
        internal DirectoryOwner(string path)
        {
            using var original = Os.Lstat(path)!;
            if (!OsConstants.S_ISDIR(original.StMode)) throw new IOException("Android retirement root is redirected or non-directory.");
            using var file = new Java.IO.File(path); var canonical = file.CanonicalPath!;
            using var data = new Java.IO.File(Android.App.Application.Context.ApplicationInfo!.DataDir!);
            var appRoot = data.CanonicalPath!;
            if (!canonical.StartsWith(appRoot + "/", StringComparison.Ordinal)) throw new IOException("Retirement root is outside private app data.");
            descriptor = Os.Open(canonical, OsConstants.ORdonly | OsConstants.ONofollow | OsConstants.ONonblock, 0)!;
            try
            {
                using var stat = Os.Fstat(descriptor)!;
                if (!OsConstants.S_ISDIR(stat.StMode) || stat.StUid != Android.OS.Process.MyUid())
                    throw new IOException("Android retirement root is foreign-owned or non-directory.");
                if (IdentityOf(original) != IdentityOf(stat)) throw new IOException("Android retirement root changed while opening.");
                Identity = IdentityOf(stat); owned = ParcelFileDescriptor.Dup(descriptor)!;
            }
            catch { Os.Close(descriptor); descriptor.Dispose(); throw; }
        }
        /// <summary>Opens one direct no-follow regular file without following a replaced ancestor.</summary>
        internal SafeFileHandle OpenFile(string name)
        {
            var value = OpenAt(owned.Fd, name, OsConstants.ORdonly | OsConstants.ONofollow | OsConstants.ONonblock);
            if (value < 0) throw NativeError();
            var handle = new SafeFileHandle((IntPtr)value, ownsHandle: true);
            try { _ = Inspect(handle); return handle; } catch { handle.Dispose(); throw; }
        }
        /// <summary>Unlinks only the rechecked direct entry in this same owned directory.</summary>
        internal void Unlink(string name) { if (UnlinkAt(owned.Fd, name, 0) != 0) throw NativeError(); }
        /// <summary>Synchronizes the actual directory namespace.</summary>
        internal void Sync() => Os.Fsync(descriptor);
        public void Dispose() { owned.Close(); owned.Dispose(); Os.Close(descriptor); descriptor.Dispose(); }
    }
    [DllImport("libc", EntryPoint = "openat", SetLastError = true)]
    private static extern int OpenAt(int directory, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int flags);
    [DllImport("libc", EntryPoint = "unlinkat", SetLastError = true)]
    private static extern int UnlinkAt(int directory, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int flags);
}
