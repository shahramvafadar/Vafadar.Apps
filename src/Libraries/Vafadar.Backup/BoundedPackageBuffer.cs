using System.Security.Cryptography;

namespace Vafadar.Backup;

/// <summary>A seekable package buffer whose writes cannot exceed the captured bound; disposal clears allocated bytes.</summary>
internal sealed class BoundedPackageBuffer(long maximum) : Stream
{
    private readonly MemoryStream buffer = new();
    /// <inheritdoc />
    public override bool CanRead => true;
    /// <inheritdoc />
    public override bool CanSeek => true;
    /// <inheritdoc />
    public override bool CanWrite => true;
    /// <inheritdoc />
    public override long Length => buffer.Length;
    /// <inheritdoc />
    public override long Position { get => buffer.Position; set { Check(value); buffer.Position = value; } }
    /// <summary>Copies the completed packet into caller-owned memory.</summary>
    internal byte[] ToArray() => buffer.ToArray();
    /// <inheritdoc />
    public override void Flush() => buffer.Flush();
    /// <inheritdoc />
    public override int Read(byte[] data, int offset, int count) => buffer.Read(data, offset, count);
    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        var position = checked((origin == SeekOrigin.Begin ? 0 : origin == SeekOrigin.Current ? Position : Length) + offset);
        Check(position); return buffer.Seek(offset, origin);
    }
    /// <inheritdoc />
    public override void SetLength(long value) { Check(value); buffer.SetLength(value); }
    /// <inheritdoc />
    public override void Write(byte[] data, int offset, int count) { Check(checked(Position + count)); buffer.Write(data, offset, count); }
    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> data) { Check(checked(Position + data.Length)); buffer.Write(data); }
    /// <inheritdoc />
    public override Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); Write(data, offset, count); return Task.CompletedTask; }
    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    { ct.ThrowIfCancellationRequested(); Write(data.Span); return ValueTask.CompletedTask; }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { CryptographicOperations.ZeroMemory(buffer.GetBuffer()); buffer.Dispose(); }
        base.Dispose(disposing);
    }
    private void Check(long value)
    { if (value < 0 || value > maximum) throw new BackupException(BackupError.InvalidFormat, "Backup package exceeds its memory bound."); }
}
