namespace Vafadar.Backup;

/// <summary>
/// Why a backup could not be created or restored.
/// </summary>
public enum BackupError
{
    /// <summary>The file is not a backup package.</summary>
    InvalidFormat = 1,

    /// <summary>The package was written by a newer version of the backup library.</summary>
    UnsupportedFormatVersion = 2,

    /// <summary>The package is encrypted and no password was given.</summary>
    PasswordRequired = 3,

    /// <summary>The password is wrong, or the encrypted package was modified or damaged.</summary>
    InvalidPasswordOrCorrupted = 4,

    /// <summary>The content does not match its checksum.</summary>
    Corrupted = 5,

    /// <summary>The package belongs to a different app.</summary>
    WrongApp = 6,

    /// <summary>The package was created by a newer version of the app, whose data this version may not understand.</summary>
    CreatedByNewerAppVersion = 7,
}

/// <summary>
/// Thrown when a backup cannot be created or restored for a reason the user can act on.
/// </summary>
public sealed class BackupException : Exception
{
    /// <summary>Creates the exception.</summary>
    public BackupException(BackupError error, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Error = error;
    }

    /// <summary>Gets the reason.</summary>
    public BackupError Error { get; }
}

/// <summary>
/// Thrown by <see cref="IBackupStorage"/> implementations when the storage service fails.
/// </summary>
public sealed class BackupStorageException : Exception
{
    /// <summary>Creates the exception.</summary>
    public BackupStorageException(string storageId, string message, int? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StorageId = storageId;
        StatusCode = statusCode;
    }

    /// <summary>Gets the <see cref="IBackupStorage.Id"/> of the failing storage.</summary>
    public string StorageId { get; }

    /// <summary>Gets the HTTP status code, for web based storage.</summary>
    public int? StatusCode { get; }
}
