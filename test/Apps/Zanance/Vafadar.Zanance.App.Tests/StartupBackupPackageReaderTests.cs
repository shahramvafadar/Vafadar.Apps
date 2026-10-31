using Vafadar.Zanance.App.Security;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual borrowed stream reads preserve the portable bound, original cancellation and temporary buffer ownership.</summary>
public sealed class StartupBackupPackageReaderTests
{
    /// <summary>Small reads return the complete original bytes without disposing their borrowed source.</summary>
    [Fact]
    public async Task Complete_borrowed_input_returns_independent_bytes_and_keeps_source_open()
    {
        byte[] original = [1, 2, 3]; using var source = new MemoryStream(original, writable: false);
        var result = await StartupBackupPackageReader.ReadAsync(source, TestContext.Current.CancellationToken, 3);
        Assert.Equal(original, result); Assert.NotSame(original, result); Assert.True(source.CanRead);
    }

    /// <summary>Already retired input does not even invoke the provider's first native read.</summary>
    [Fact]
    public async Task Retired_request_never_starts_a_provider_read()
    {
        using var ct = new CancellationTokenSource(); ct.Cancel(); using var source = new ObservedStream();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => StartupBackupPackageReader.ReadAsync(source, ct.Token));
        Assert.Equal(0, source.Reads);
    }

    /// <summary>A provider that ignores cancellation cannot return a late package or retain an uncleared borrowed chunk.</summary>
    [Fact]
    public async Task Retirement_during_native_read_refuses_late_bytes_and_clears_borrowed_memory()
    {
        using var ct = new CancellationTokenSource(); using var source = new ObservedStream { DuringRead = ct.Cancel };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => StartupBackupPackageReader.ReadAsync(source, ct.Token));
        Assert.Equal(1, source.Reads); Assert.All(source.LastChunk.ToArray(), value => Assert.Equal((byte)0, value));
    }

    /// <summary>Native read errors clear all borrowed chunk memory instead of exposing a partly loaded recovery selection.</summary>
    [Fact]
    public async Task Failure_after_partial_read_clears_original_temporary_chunk()
    {
        using var source = new ObservedStream { ThrowOnSecondRead = true };
        await Assert.ThrowsAsync<IOException>(() => StartupBackupPackageReader.ReadAsync(source, TestContext.Current.CancellationToken));
        Assert.Equal(2, source.Reads); Assert.All(source.LastChunk.ToArray(), value => Assert.Equal((byte)0, value));
    }

    /// <summary>A non-seekable provider cannot evade the same cumulative bound with multiple short reads.</summary>
    [Fact]
    public async Task Nonseekable_growing_input_refuses_after_original_cumulative_limit()
    {
        using var source = new ObservedStream { Repeat = true };
        await Assert.ThrowsAsync<IOException>(() => StartupBackupPackageReader.ReadAsync(source, TestContext.Current.CancellationToken, 3));
        Assert.Equal(2, source.Reads); Assert.All(source.LastChunk.ToArray(), value => Assert.Equal((byte)0, value));
    }

    /// <summary>Internal diagnostic limits can narrow the original allowance but never remove or enlarge it.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(StartupBackupPackageReader.MaximumBytes + 1)]
    public async Task Invalid_or_enlarged_limit_refuses_before_read(long maximum)
    {
        using var source = new ObservedStream();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => StartupBackupPackageReader.ReadAsync(source, TestContext.Current.CancellationToken, maximum));
        Assert.Equal(0, source.Reads);
    }

    /// <summary>Empty input retains its original interpretation; the protected portable validator decides its validity.</summary>
    [Fact]
    public async Task Empty_input_does_not_invent_a_portable_validation_result()
    {
        using var source = new MemoryStream();
        Assert.Empty(await StartupBackupPackageReader.ReadAsync(source, TestContext.Current.CancellationToken));
    }

    private sealed class ObservedStream : Stream
    {
        internal Action? DuringRead { get; init; }
        internal bool ThrowOnSecondRead { get; init; }
        internal bool Repeat { get; init; }
        internal int Reads { get; private set; }
        internal Memory<byte> LastChunk { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Reads++; LastChunk = buffer;
            if (ThrowOnSecondRead && Reads == 2) throw new IOException("Original input read failed.");
            if (Reads > 1 && !Repeat) return ValueTask.FromResult(0);
            buffer.Span[0] = 7; buffer.Span[1] = 8; DuringRead?.Invoke(); return ValueTask.FromResult(2);
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
