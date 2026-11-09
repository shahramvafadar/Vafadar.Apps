namespace Vafadar.Data.Tests;

/// <summary>Cancellation raised inside actual native sorting must stop before the next original step or mismatch classification.</summary>
public sealed class SnapshotLiveSortCancellationTests
{
    [Fact]
    public async Task Snapshot_live_sort_expected_cancellation_never_starts_the_other_sort()
    {
        var result = await SnapshotSortCancellationScenario.RunAsync(true, 1024, "native-sort-host-expected-", TestContext.Current.CancellationToken);
        Assert.True(result.Cancelled); Assert.True(result.SourceExact); Assert.True(result.CallersUsable);
        Assert.True(result.ExpectedSortCalls > 0); Assert.Equal(nameof(OperationCanceledException), result.ErrorType);
        Assert.True(result.OriginalCancellationToken); Assert.Equal(0, result.ActualSortCalls);
    }

    [Fact]
    public async Task Snapshot_live_sort_actual_cancellation_precedes_content_mismatch_classification()
    {
        var result = await SnapshotSortCancellationScenario.RunAsync(false, 768, "native-sort-host-actual-", TestContext.Current.CancellationToken);
        Assert.True(result.Cancelled); Assert.True(result.SourceExact); Assert.True(result.CallersUsable);
        Assert.True(result.ExpectedSortCalls > 0); Assert.True(result.ActualSortCalls > 0);
        Assert.Equal(nameof(OperationCanceledException), result.ErrorType); Assert.True(result.OriginalCancellationToken);
    }
}
