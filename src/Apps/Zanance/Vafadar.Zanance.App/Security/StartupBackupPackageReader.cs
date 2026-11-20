using System.Security.Cryptography;

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

    private static async Task<byte[]> ReadCoreAsync(Stream input, CancellationToken ct, long maximumBytes)
    {
        ArgumentNullException.ThrowIfNull(input);
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
                Append(buffer, chunk.AsSpan(0, count));
            }
            ct.ThrowIfCancellationRequested();
            return buffer.ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(chunk);
            CryptographicOperations.ZeroMemory(buffer.GetBuffer());
        }
    }

    /// <summary>Appends to the owned accumulator and clears the former backing array if it grows.</summary>
    internal static void Append(MemoryStream buffer, ReadOnlySpan<byte> bytes)
    {
        var previous = buffer.GetBuffer();
        try
        {
            buffer.Write(bytes);
        }
        finally
        {
            // MemoryStream copies the data on growth, leaving the superseded allocation intact.
            if (!ReferenceEquals(previous, buffer.GetBuffer()))
                CryptographicOperations.ZeroMemory(previous);
        }
    }
}
