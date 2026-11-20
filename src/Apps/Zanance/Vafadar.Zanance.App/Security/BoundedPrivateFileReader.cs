using System.Security.Cryptography;

namespace Vafadar.Zanance.App.Security;

/// <summary>Reads private data from a borrowed stream and clears temporary buffers before returning.</summary>
internal static class BoundedPrivateFileReader
{
    /// <summary>Returns the complete original bytes within the caller's format-specific limit.</summary>
    internal static async Task<byte[]> ReadAsync(Stream input, long maximumBytes, CancellationToken ct,
        Func<Exception> limitExceeded, Action? validateOwner = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumBytes);
        ct.ThrowIfCancellationRequested();
        validateOwner?.Invoke();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        try
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                // Read at most one sentinel byte beyond the caller's complete-file bound.
                var remaining = maximumBytes - buffer.Length;
                var request = remaining >= chunk.Length ? chunk.Length : (int)remaining + 1;
                var count = await input.ReadAsync(chunk.AsMemory(0, request), ct).ConfigureAwait(false);
                // A provider may finish a native read after cancellation; never publish that late input.
                ct.ThrowIfCancellationRequested();
                validateOwner?.Invoke();
                if (count == 0) break;
                if (buffer.Length + count > maximumBytes) throw limitExceeded();
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
        try { buffer.Write(bytes); }
        finally
        {
            // MemoryStream copies the data on growth, leaving the superseded allocation intact.
            if (!ReferenceEquals(previous, buffer.GetBuffer()))
                CryptographicOperations.ZeroMemory(previous);
        }
    }
}
