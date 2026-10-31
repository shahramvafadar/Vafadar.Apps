using System.Security.Cryptography;

namespace Vafadar.Zanance.App.Security;

/// <summary>Reads only the bounded borrowed portable input while its original presentation remains live.</summary>
internal static class StartupBackupPackageReader
{
    /// <summary>The original portable-envelope allowance; callers cannot enlarge the existing limit.</summary>
    internal const long MaximumBytes = Vafadar.Backup.Security.DeviceSafetyEncryption.MaximumPackageBytes + 1024L;

    /// <summary>Returns an independent owned buffer and clears all temporary copies on success, failure or retirement.</summary>
    internal static async Task<byte[]> ReadAsync(Stream input, CancellationToken ct, long maximumBytes = MaximumBytes)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumBytes, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumBytes, MaximumBytes);
        ct.ThrowIfCancellationRequested();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        try
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var count = await input.ReadAsync(chunk, ct).ConfigureAwait(false);
                // A provider may finish a native read after cancellation; never publish that late input.
                ct.ThrowIfCancellationRequested();
                if (count == 0) break;
                if (buffer.Length + count > maximumBytes) throw new IOException("Portable recovery file exceeds its bound.");
                await buffer.WriteAsync(chunk.AsMemory(0, count), ct).ConfigureAwait(false);
            }
            ct.ThrowIfCancellationRequested();
            return buffer.ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(chunk);
            if (buffer.TryGetBuffer(out var owned)) CryptographicOperations.ZeroMemory(owned.AsSpan());
        }
    }
}
