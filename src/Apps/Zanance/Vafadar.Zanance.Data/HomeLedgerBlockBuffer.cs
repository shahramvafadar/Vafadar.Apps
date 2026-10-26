namespace Vafadar.Zanance.Data;

/// <summary>Owns one bounded plaintext expansion buffer only through one covered block-read call.</summary>
internal sealed class HomeLedgerBlockBuffer : IDisposable
{
    private readonly Func<int, byte[]> allocate;
    private byte[] bytes = [];
    private bool disposed;

    /// <summary>Accepts a fresh owned-array allocator; production never shares or retains this read's buffer.</summary>
    internal HomeLedgerBlockBuffer(Func<int, byte[]>? allocate = null) =>
        this.allocate = allocate ?? (static length => new byte[length]);

    /// <summary>Returns exactly the checked logical extent, growing only for a larger block and clearing retired capacity.</summary>
    internal Span<byte> GetSpan(int count)
    {
        if (disposed) throw new ObjectDisposedException(nameof(HomeLedgerBlockBuffer));
        if (count < 1 || count > HomeLedgerBlockCodec.MaxUncompressedBytes)
            throw new InvalidDataException("Invalid owned ledger expansion extent.");
        if (bytes.Length < count)
        {
            var capacity = Math.Max(4096, bytes.Length);
            while (capacity < count) capacity = checked(capacity * 2);
            var replacement = allocate(capacity);
            if (replacement is null || replacement.Length != capacity)
            {
                if (replacement is not null) Array.Clear(replacement);
                throw new InvalidOperationException("An owned buffer must have the requested bounded capacity.");
            }
            // D-547: no global pool or cross-read cache; retired financial bytes do not survive growth.
            Array.Clear(bytes);
            bytes = replacement;
        }
        return bytes.AsSpan(0, count);
    }

    /// <summary>Clears every owned byte, including an older longer block's tail, before the read returns or falls back.</summary>
    public void Dispose()
    {
        if (disposed) return;
        Array.Clear(bytes);
        bytes = [];
        disposed = true;
    }
}
