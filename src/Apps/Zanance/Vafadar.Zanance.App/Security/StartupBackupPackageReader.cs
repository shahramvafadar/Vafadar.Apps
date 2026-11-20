namespace Vafadar.Zanance.App.Security;

/// <summary>Reads backup input sequentially without seeking on a borrowed provider stream.</summary>
internal static class StartupBackupPackageReader
{
    /// <summary>The original portable-envelope allowance; callers cannot enlarge the existing limit.</summary>
    internal const long MaximumBytes = Vafadar.Backup.Security.DeviceSafetyEncryption.MaximumPackageBytes + 1024L;

    /// <summary>Returns an independent owned buffer and clears all temporary copies on success, failure or retirement.</summary>
    internal static async Task<byte[]> ReadAsync(Stream input, CancellationToken ct, long maximumBytes = MaximumBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumBytes, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumBytes, MaximumBytes);
        return await ReadCoreAsync(input, ct, maximumBytes).ConfigureAwait(false);
    }

    /// <summary>Uses the normal portable format envelope limit without applying the startup safety-copy limit.</summary>
    internal static Task<byte[]> ReadPortableAsync(Stream input, CancellationToken ct) =>
        ReadCoreAsync(input, ct, int.MaxValue);

    private static Task<byte[]> ReadCoreAsync(Stream input, CancellationToken ct, long maximumBytes) =>
        BoundedPrivateFileReader.ReadAsync(input, maximumBytes, ct,
            static () => new IOException("Portable recovery file exceeds its bound."));
}
