namespace Vafadar.Backup.Security;

/// <summary>Bounds opaque safety-file discovery by its nonsecret profile/installation header, without reading keys or package content.</summary>
/// <remarks>A matching header is only a candidate, never authentication. Original full-envelope authentication and
/// package validation must precede preview/restoration. Automatic copies remain a separate cohort.</remarks>
public static class DeviceSafetyCopyDiscovery
{
    /// <summary>Reads only 52 header bytes per bounded safety candidate under the caller's captured storage authority.</summary>
    public static async Task<IReadOnlyList<BackupFileInfo>> ListAsync(IBackupStorage storage,
        Guid profile, Guid installation, Action validate, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(storage); ArgumentNullException.ThrowIfNull(validate);
        if (profile == Guid.Empty || installation == Guid.Empty) throw new ArgumentException("Original local context is required.");
        validate(); ct.ThrowIfCancellationRequested();
        var files = await storage.ListAsync(ct).ConfigureAwait(false); validate();
        var result = new List<BackupFileInfo>();
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested(); validate();
            var name = file.FileName;
            if (name.Length != 43 || !name.StartsWith("safety-", StringComparison.Ordinal) || !name.EndsWith(".vbs", StringComparison.Ordinal)
                || !Guid.TryParseExact(name.AsSpan(7, 32), "N", out var opaque) || opaque == Guid.Empty
                || file.Size is not long size || size <= 80 || size > DeviceSafetyEncryption.MaximumPackageBytes + 80L) continue;
            await using var stream = await storage.OpenReadAsync(file.Id, ct).ConfigureAwait(false); validate();
            if (!stream.CanSeek || stream.Length != size) continue;
            var header = new byte[52];
            try { await stream.ReadExactlyAsync(header, ct).ConfigureAwait(false); }
            catch (EndOfStreamException) { continue; }
            validate();
            if (!DeviceSafetyEncryption.IsEncrypted(header)
                || new Guid(header.AsSpan(4, 16), bigEndian: true) != profile
                || new Guid(header.AsSpan(20, 16), bigEndian: true) != installation
                || new Guid(header.AsSpan(36, 16), bigEndian: true) == Guid.Empty) continue;
            result.Add(file);
        }
        validate();
        return result.OrderByDescending(file => file.CreatedAt).ThenBy(file => file.FileName, StringComparer.Ordinal).ToArray();
    }
}
