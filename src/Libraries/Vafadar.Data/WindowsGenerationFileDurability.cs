using System.Runtime.Versioning;

namespace Vafadar.Data;

/// <summary>Uses actual Windows file and directory FlushFileBuffers handles in one local private directory.</summary>
/// <remarks>Unsupported/denied synchronization fails closed. Successful API completion on the observed
/// filesystem is not physical-device power-loss acceptance or a universal filesystem durability claim.</remarks>
[SupportedOSPlatform("windows")]
public sealed class WindowsGenerationFileDurability : IDatabaseGenerationFileDurability
{
    private readonly string directory;
    private readonly string nativeDirectory;
    private readonly string nativeIdentity;

    /// <summary>Captures an existing local directory without creating, redirecting or traversing another storage owner.</summary>
    public WindowsGenerationFileDurability(string directory)
    {
        if (!Path.IsPathFullyQualified(directory)) throw new ArgumentException("Durability requires an absolute private directory.", nameof(directory));
        this.directory = Path.GetFullPath(directory);
        using var root = new WindowsPrivateFiles(this.directory);
        nativeDirectory = root.NativeDirectory;
        nativeIdentity = root.DirectoryIdentity;
    }
    /// <inheritdoc />
    public long AvailableBytes
    {
        get
        {
            using var root = new WindowsPrivateFiles(directory);
            RequireCapturedRoot(root);
            return new DriveInfo(Path.GetPathRoot(directory)!).AvailableFreeSpace;
        }
    }
    /// <inheritdoc />
    public Task SynchronizeFileAsync(string path, CancellationToken ct) => Task.Run(() =>
    {
        ct.ThrowIfCancellationRequested(); using var root = new WindowsPrivateFiles(directory);
        RequireCapturedRoot(root);
        using var handle = root.OpenFile(path, writable: true);
        WindowsPrivateFiles.FlushFile(handle); root.FlushDirectory(); ct.ThrowIfCancellationRequested();
    }, ct);
    /// <inheritdoc />
    public Task SynchronizeDirectoryAsync(CancellationToken ct) => Task.Run(() =>
    { ct.ThrowIfCancellationRequested(); using var root = new WindowsPrivateFiles(directory); RequireCapturedRoot(root); root.FlushDirectory(); ct.ThrowIfCancellationRequested(); }, ct);

    private void RequireCapturedRoot(WindowsPrivateFiles root)
    {
        if (root.DirectoryIdentity != nativeIdentity || !string.Equals(root.NativeDirectory, nativeDirectory, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Captured durability root was replaced or rebound.");
    }
}
