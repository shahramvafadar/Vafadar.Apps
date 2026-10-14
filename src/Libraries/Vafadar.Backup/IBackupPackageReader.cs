namespace Vafadar.Backup;

/// <summary>Validates a bounded portable package without reading or replacing live financial sources.</summary>
public interface IBackupPackageReader
{
    /// <summary>Runs the consumer only after app/version/checksum validation; borrowed decoded buffers are cleared afterwards.</summary>
    /// <remarks>Device-only safety files are excluded. The consumer must copy any data it retains beyond this call.</remarks>
    Task ReadValidatedPortableAsync(byte[] package, string? password, int maximumBytes,
        Func<BackupManifest, IReadOnlyDictionary<string, ReadOnlyMemory<byte>>, CancellationToken, Task> consume,
        CancellationToken ct = default);
}
