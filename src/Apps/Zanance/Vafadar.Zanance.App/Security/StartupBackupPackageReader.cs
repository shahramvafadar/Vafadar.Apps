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

    /// <summary>Reads a startup backup while the original recovery authority still owns its source.</summary>
    internal static Task<byte[]> ReadAsync(Stream input, RecoverySetupAuthority authority) =>
        ReadPickedAsync(input, authority, authority.Token);

    /// <summary>Reads under the original startup authority and its linked native picker lifetime.</summary>
    internal static Task<byte[]> ReadPickedAsync(Stream input, RecoverySetupAuthority authority, CancellationToken pickerToken) =>
        ReadCoreAsync(input, pickerToken, MaximumBytes, authority.RequireCurrent);

    /// <summary>Uses the normal portable format envelope limit without applying the startup safety-copy limit.</summary>
    internal static Task<byte[]> ReadPortableAsync(Stream input, CancellationToken ct, Action validateSource) =>
        ReadCoreAsync(input, ct, int.MaxValue, validateSource);

    private static Task<byte[]> ReadCoreAsync(Stream input, CancellationToken ct, long maximumBytes,
        Action? validateSource = null) =>
        BoundedPrivateFileReader.ReadAsync(input, maximumBytes, ct,
            static () => new IOException("Portable recovery file exceeds its bound."), validateSource);
}
