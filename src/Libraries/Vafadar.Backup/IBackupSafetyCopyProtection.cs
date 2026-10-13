namespace Vafadar.Backup;

/// <summary>Protects internal device-local safety copies independently of optional portable backup passwords.</summary>
public interface IBackupSafetyCopyProtection
{
    /// <summary>Protects one complete in-memory package before it is uploaded to internal storage.</summary>
    Task<byte[]> ProtectAsync(ReadOnlyMemory<byte> package, CancellationToken cancellationToken);

    /// <summary>Opens an existing local safety envelope without generating a replacement key.</summary>
    Task<byte[]> OpenAsync(ReadOnlyMemory<byte> envelope, CancellationToken cancellationToken);
}
