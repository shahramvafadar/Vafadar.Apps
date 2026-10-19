using System.Text;

namespace Vafadar.Zanance.Core.DataFiles;

/// <summary>Reads a complete selected text input with bounded allocation, retaining the original BOM-aware decoding.</summary>
public static class CsvInput
{
    /// <summary>Reads from the current stream position; preserves UTF-8/BOM behavior and leaves the caller-owned stream open.</summary>
    public static async Task<string> ReadTextAsync(Stream source, CsvReadLimits? limits = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var budget = limits ?? CsvReadLimits.Import; budget.Validate(); cancellationToken.ThrowIfCancellationRequested();
        using var bounded = new ByteBudgetStream(source, budget.MaximumBytes);
        using var reader = new StreamReader(bounded, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096);
        var result = new StringBuilder(); var chars = new char[8192]; int count;
        while ((count = await reader.ReadAsync(chars.AsMemory(), cancellationToken).ConfigureAwait(false)) != 0)
        {
            if (result.Length > budget.MaximumCharacters - count)
                throw new CsvReadLimitException(CsvReadLimit.Characters, budget.MaximumCharacters);
            result.Append(chars, 0, count);
        }
        return result.ToString();
    }

    /// <summary>Counts real reads, allowing only one sentinel byte beyond the budget; never closes the native source.</summary>
    private sealed class ByteBudgetStream(Stream source, int maximum) : Stream
    {
        private long _read;
        /// <inheritdoc />
        public override bool CanRead => source.CanRead;
        /// <inheritdoc />
        public override bool CanSeek => false;
        /// <inheritdoc />
        public override bool CanWrite => false;
        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();
        /// <inheritdoc />
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        /// <summary>Bounds each request even for a nonseekable provider that lies about its length.</summary>
        private int Request(int count) => (int)Math.Min(count, maximum - _read + 1);
        /// <summary>Refuses the complete read immediately at its first excess byte.</summary>
        private int Count(int count)
        {
            _read += count;
            if (_read > maximum) throw new CsvReadLimitException(CsvReadLimit.Bytes, maximum);
            return count;
        }
        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            var total = 0;
            do
            {
                var read = Count(source.Read(buffer, offset + total, Request(count - total)));
                total += read;
                if (read == 0) break;
            } while (_read < 4 && total < count);
            return total;
        }
        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var total = 0;
            // StreamReader needs the complete initial BOM to distinguish UTF-8/16/32; providers may return one byte.
            // Coalesce at most the first four bytes, still counting every byte against the original stream budget.
            do
            {
                var read = Count(await source.ReadAsync(buffer.Slice(total, Request(buffer.Length - total)), cancellationToken).ConfigureAwait(false));
                total += read;
                if (read == 0) break;
            } while (_read < 4 && total < buffer.Length);
            return total;
        }
        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        /// <inheritdoc />
        public override void Flush() => throw new NotSupportedException();
        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();
        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
