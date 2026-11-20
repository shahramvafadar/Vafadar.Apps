using Vafadar.Zanance.App.Interaction;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Verifies the original selected stream's cancellation and completion boundary.</summary>
public sealed class HouseholdRecoveryFileWriteTests
{
    [Fact]
    public async Task Deadline_at_successful_close_does_not_hide_the_completed_save()
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var stream = new Destination(deadline.Cancel);
        byte[] original = [1, 2, 3];

        Assert.True(await HouseholdRecoveryFiles.WriteSelectedAsync(stream, original, deadline.Token));

        Assert.True(deadline.IsCancellationRequested);
        Assert.Equal(original, stream.ToArray());
        Assert.True(stream.Flushed);
        Assert.Equal(1, stream.Closes);
    }

    [Fact]
    public async Task Retired_save_closes_without_writing()
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.Cancel();
        var stream = new Destination(static () => { });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            HouseholdRecoveryFiles.WriteSelectedAsync(stream, new byte[] { 1 }, deadline.Token));

        Assert.Empty(stream.ToArray());
        Assert.False(stream.Flushed);
        Assert.Equal(1, stream.Closes);
    }

    [Fact]
    public async Task Failed_provider_close_does_not_confirm_the_save()
    {
        var stream = new Destination(static () => throw new IOException("The selected destination failed to close."));
        await Assert.ThrowsAsync<IOException>(() =>
            HouseholdRecoveryFiles.WriteSelectedAsync(stream, new byte[] { 1 }, TestContext.Current.CancellationToken));

        Assert.True(stream.Flushed);
        Assert.Equal(1, stream.Closes);
    }

    private sealed class Destination(Action onClose) : MemoryStream
    {
        internal bool Flushed { get; private set; }
        internal int Closes { get; private set; }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            Flushed = true;
            return base.FlushAsync(cancellationToken);
        }

        public override ValueTask DisposeAsync()
        {
            Closes++;
            Dispose();
            onClose();
            return ValueTask.CompletedTask;
        }
    }
}
