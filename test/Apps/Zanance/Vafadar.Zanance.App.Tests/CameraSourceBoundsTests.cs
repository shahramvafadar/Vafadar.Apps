using Vafadar.Testing;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual source bytes remain bounded even when a camera file changes after its early length admission.</summary>
[Trait("AT", "AT-171")]
public sealed class CameraSourceBoundsTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Two original admissible lengths grow past the same original byte ceiling before the real file read.</summary>
    [Theory]
    [InlineData(8)]
    [InlineData(AttachmentFiles.MaxSourceBytes - 1)]
    public async Task Camera_growth_after_length_admission_is_refused(int admittedLength)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("fictitious-camera.jpg");
        await using (var source = File.Create(path)) source.SetLength(admittedLength);
        var admitted = new FileInfo(path).Length;
        Assert.InRange(admitted, 1, AttachmentFiles.MaxSourceBytes);
        // Reproduce an external writer's growth between the adapter's stat and its original file read.
        await using (var externalWriter = File.OpenWrite(path)) externalWriter.SetLength(AttachmentFiles.MaxSourceBytes + 1L);
        await Assert.ThrowsAsync<InvalidDataException>(() => AttachmentFiles.ReadCameraSourceAsync(path));
        Assert.Equal(AttachmentFiles.MaxSourceBytes + 1L, new FileInfo(path).Length);
    }

    /// <summary>The exact original maximum and a small source retain every actual input byte.</summary>
    [Theory]
    [InlineData(4096)]
    [InlineData(AttachmentFiles.MaxSourceBytes)]
    public async Task Admissible_camera_sources_are_not_truncated_or_modified(int length)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Combine("fictitious-camera.jpg");
        await using (var source = File.Create(path))
        {
            source.SetLength(length);
            await source.WriteAsync(new byte[] { 0xFF, 0xD8, 0x45, 0x58 }, Ct);
            source.Position = length - 2;
            await source.WriteAsync(new byte[] { 0xFF, 0xD9 }, Ct);
        }
        var bytes = await AttachmentFiles.ReadCameraSourceAsync(path);
        Assert.Equal(length, bytes.Length);
        Assert.Equal(new byte[] { 0xFF, 0xD8, 0x45, 0x58 }, bytes[..4]);
        Assert.Equal(new byte[] { 0xFF, 0xD9 }, bytes[^2..]);
        Assert.True(bytes.AsSpan(4, length - 6).IndexOfAnyExcept((byte)0) < 0);
    }

    /// <summary>Source acquisition errors remain errors and are not converted into empty photos or cancellation.</summary>
    [Fact]
    public async Task Missing_camera_source_preserves_the_original_io_failure()
    {
        using var directory = new TemporaryDirectory();
        await Assert.ThrowsAsync<FileNotFoundException>(() => AttachmentFiles.ReadCameraSourceAsync(directory.Combine("absent.jpg")));
    }

    /// <summary>The original picker reader rejects actual bytes without trusting seekable lengths or taking stream ownership.</summary>
    [Fact]
    public async Task Nonseekable_source_keeps_the_original_bound_and_caller_ownership()
    {
        using var source = new NonseekableSource(AttachmentFiles.MaxSourceBytes + 1L);
        await Assert.ThrowsAsync<InvalidDataException>(() => AttachmentFiles.ReadSourceAsync(source));
        Assert.InRange(source.BytesRead, AttachmentFiles.MaxSourceBytes + 1L, AttachmentFiles.MaxSourceBytes + 81920L);
        Assert.False(source.Disposed);
    }

    /// <summary>Generates finite fictitious source bytes and rejects every unsupported metadata/seek operation.</summary>
    private sealed class NonseekableSource(long remaining) : Stream
    {
        internal long BytesRead { get; private set; }
        internal bool Disposed { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = (int)Math.Min(count, remaining);
            buffer.AsSpan(offset, read).Clear(); remaining -= read; BytesRead += read; return read;
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = (int)Math.Min(buffer.Length, remaining);
            buffer.Span[..read].Clear(); remaining -= read; BytesRead += read; return ValueTask.FromResult(read);
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
}
