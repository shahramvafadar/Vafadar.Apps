namespace Vafadar.Zanance.Data.Tests;

/// <summary>The original private client must freeze disclosed aggregates before callbacks and asynchronous ownership waits.</summary>
public sealed class AiSubmissionSnapshotTests
{
    /// <summary>Every temporal boundary retains the complete literal entry payload despite a later caller-list mutation.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Submission_snapshot_preserves_entry_payload_before_each_external_wait(int phase)
    {
        var observation = await AiSubmissionSnapshotProbes.ObserveAsync(phase, TestContext.Current.CancellationToken);
        Assert.True(observation.Changed);
        Assert.NotEqual(observation.ExpectedPayload, observation.MutatedCallerPayload);
        Assert.Equal(observation.ExpectedPayload, observation.JournalPayload);
        Assert.Equal(phase == 1 ? 2 : 1, observation.PrivateReads);
        Assert.Equal(phase == 1 ? 2 : 1, observation.PrivateWriteAttempts);
        Assert.Equal(0, observation.DurableWrites);
        Assert.Equal(0, observation.CredentialCalls);
        Assert.Equal(0, observation.SourceRechecks);
        Assert.True(observation.RequestIdAssigned);
    }

    /// <summary>Moving snapshot capture cannot turn a cancelled waiter into another private intent or leak its semaphore.</summary>
    [Fact]
    public async Task Submission_snapshot_cancelled_waiter_preserves_original_owner_cleanup()
    {
        var observation = await AiSubmissionSnapshotProbes.ObserveCancellationAsync(TestContext.Current.CancellationToken);
        Assert.True(observation.OriginalCancellation);
        Assert.Equal(1, observation.PrivateReads);
        Assert.Equal(1, observation.PrivateWriteAttempts);
        Assert.Equal(0, observation.DurableWrites);
        Assert.Equal(0, observation.CredentialCalls);
        Assert.Equal(0, observation.SourceRechecks);
    }
}
