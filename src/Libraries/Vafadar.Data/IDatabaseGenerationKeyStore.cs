namespace Vafadar.Data;

/// <summary>Platform-owned protected generation keys under a captured exclusive access lease.</summary>
public interface IDatabaseGenerationKeyStore
{
    /// <summary>Explicitly prepares or reopens an existing bound key; damaged entries never generate replacements.</summary>
    Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken);
    /// <summary>Reads only an existing bound key; the caller clears the returned owned 32-byte buffer.</summary>
    Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken cancellationToken);
}

/// <summary>Actual platform file/directory synchronization for a trusted captured private directory.</summary>
public interface IDatabaseGenerationFileDurability
{
    /// <summary>Reads available bytes for the captured volume before encrypted candidate and rollback creation.</summary>
    long AvailableBytes { get; }
    /// <summary>Synchronizes one closed owned encrypted file and its namespace before checkpoint acceptance.</summary>
    Task SynchronizeFileAsync(string path, CancellationToken cancellationToken);
    /// <summary>Synchronizes the captured directory after exact obsolete-file cleanup.</summary>
    Task SynchronizeDirectoryAsync(CancellationToken cancellationToken);
}
