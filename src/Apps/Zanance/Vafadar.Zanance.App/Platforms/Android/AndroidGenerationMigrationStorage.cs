using Android.OS;
using Android.Systems;
using System.Security.Cryptography;
using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Final generation-key effects using the original Android Keystore and immutable envelope store.</summary>
public sealed class AndroidGenerationKeyStore(string privateDirectory) : IDatabaseGenerationKeyStore
{
    private readonly AndroidDatabaseKeyEnvelopeStore store = new(privateDirectory);

    /// <inheritdoc />
    public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (store.ContainsEnvelope(binding))
        {
            var existing = store.ReadKey(binding); CryptographicOperations.ZeroMemory(existing); return;
        }
        AndroidDatabaseKeyProtector.PrepareWrappingKey(binding.InstallationId);
        var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            store.PublishNew(binding, AndroidDatabaseKeyProtector.Protect(key, binding));
            var reopened = store.ReadKey(binding);
            try { if (!CryptographicOperations.FixedTimeEquals(key, reopened)) throw new CryptographicException("Published generation key differs."); }
            finally { CryptographicOperations.ZeroMemory(reopened); }
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }, cancellationToken);

    /// <inheritdoc />
    public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken) => Task.Run(() =>
    { cancellationToken.ThrowIfCancellationRequested(); return store.ReadKey(binding); }, cancellationToken);
}

/// <summary>Actual Android file/directory fsync and volume availability within one trusted captured private directory.</summary>
public sealed class AndroidGenerationFileDurability : IDatabaseGenerationFileDurability
{
    private readonly string directory;

    /// <summary>Captures an existing private root whose ancestors belong to the local installation owner.</summary>
    public AndroidGenerationFileDurability(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (!Path.IsPathFullyQualified(directory)) throw new ArgumentException("Durability requires an absolute private directory.");
        this.directory = Path.GetFullPath(directory); ValidateDirectory();
    }

    /// <inheritdoc />
    public long AvailableBytes { get { ValidateDirectory(); using var volume = new StatFs(directory); return volume.AvailableBytes; } }

    /// <inheritdoc />
    public Task SynchronizeFileAsync(string path, CancellationToken cancellationToken) => Task.Run(() =>
    {
        ValidateDirectory(); cancellationToken.ThrowIfCancellationRequested();
        if (Path.GetDirectoryName(Path.GetFullPath(path)) != directory)
            throw new IOException("Cannot synchronize an unrelated generation.");
        using var descriptor = Os.Open(path, OsConstants.ORdwr | OsConstants.ONofollow | OsConstants.ONonblock, 0)!;
        try
        {
            using var stat = Os.Fstat(descriptor)!;
            if (!OsConstants.S_ISREG(stat.StMode)) throw new IOException("Generation is not a regular file.");
            Os.Fsync(descriptor);
        }
        finally { Os.Close(descriptor); }
        SyncDirectory();
    }, cancellationToken);

    /// <inheritdoc />
    public Task SynchronizeDirectoryAsync(CancellationToken cancellationToken) => Task.Run(() =>
    { cancellationToken.ThrowIfCancellationRequested(); ValidateDirectory(); SyncDirectory(); }, cancellationToken);

    private void ValidateDirectory()
    {
        if (!Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Captured durable directory is missing or redirected.");
    }

    private void SyncDirectory()
    {
        using var descriptor = Os.Open(directory, OsConstants.ORdonly | OsConstants.ONofollow | OsConstants.ONonblock, 0)!;
        try
        {
            using var stat = Os.Fstat(descriptor)!;
            if (!OsConstants.S_ISDIR(stat.StMode)) throw new IOException("Captured root is not a directory.");
            Os.Fsync(descriptor);
        }
        finally { Os.Close(descriptor); }
    }
}
