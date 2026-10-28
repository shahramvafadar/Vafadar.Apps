namespace Vafadar.Data;

/// <summary>Identifies the original bounded staging-space refusal without exposing a path, key or source error.</summary>
public sealed class InsufficientDatabaseStorageException : IOException
{
    /// <summary>Creates only the fixed storage classification; the caller's required-space policy remains unchanged.</summary>
    public InsufficientDatabaseStorageException()
        : base("Insufficient space for bounded encrypted restore staging.")
    {
    }
}
