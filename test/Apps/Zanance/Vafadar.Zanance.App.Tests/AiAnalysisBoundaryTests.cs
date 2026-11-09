namespace Vafadar.Zanance.App.Tests;

public sealed partial class AiAnalysisStartupTests
{
    /// <summary>Current local access still permits the actual zero-activity aggregate without any native/provider effect.</summary>
    [Fact]
    public async Task Ai_boundary_current_local_review_remains_read_only()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, "review"); s.Runtime.Reset(false); s.Factory.Refuse = false;
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await s.Model.ReviewAsync();
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(1, s.Factory.Calls);
        Assert.Equal(1, s.Runtime.Captures); Assert.True(s.Runtime.Validations >= 1); Assert.NotNull(s.Model.Disclosure);
        Assert.True(s.Model.CanShare); Assert.False(s.Model.HasPendingOperation); Assert.True(s.Model.CanLeave);
        Assert.Equal(0, s.Runtime.Opens); Assert.Equal(0, s.Runtime.Authorizations); Assert.Empty(f.Platform.ConfirmationDialogs); s.Model.Retire();
    }

    /// <summary>The new inter-await guards retain current metadata inspection without sending any aggregate or credentials.</summary>
    [Fact]
    public async Task Ai_boundary_current_provider_metadata_remains_available()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, "provider"); s.Runtime.Reset(false);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await s.Model.CheckProviderAsync();
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(1, s.Runtime.Opens);
        Assert.Equal(1, s.Runtime.Remote.AvailabilityReads); Assert.Equal(1, s.Runtime.Remote.RecordReads);
        Assert.False(s.Model.HasPendingOperation); Assert.True(s.Model.CanCheckProvider); Assert.True(s.Model.CanLeave);
        Assert.Equal(0, s.Factory.Calls); Assert.Equal(0, s.Runtime.Authorizations); Assert.Empty(f.Platform.ConfirmationDialogs); s.Model.Retire();
    }

    /// <summary>A denied captured local access gate must be checked before the original aggregate reader is entered.</summary>
    [Fact]
    public async Task Ai_boundary_denied_local_access_precedes_source_entry()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, "review"); s.Runtime.Reset(false); s.Runtime.DenyAccess = true;
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await s.Model.ReviewAsync();
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(0, s.Factory.Calls);
        Assert.Equal(1, s.Runtime.Captures); Assert.Equal(1, s.Runtime.Validations); Assert.False(s.Model.HasPendingOperation);
        Assert.Null(s.Model.Disclosure); Assert.Empty(f.Platform.ConfirmationDialogs); s.Model.Retire();
    }

    /// <summary>Retirement after opening or reading public metadata cannot continue into another metadata/private-record request.</summary>
    [Theory]
    [InlineData("open")]
    [InlineData("availability")]
    public async Task Ai_boundary_retired_provider_stops_between_metadata_awaits(string boundary)
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, "provider"); s.Runtime.Reset(false);
        if (boundary == "open") s.Runtime.DuringOpen = () => { s.Model.RetireRoot(); return Task.CompletedTask; };
        else s.Runtime.Remote.DuringAvailability = () => { s.Model.RetireRoot(); return Task.CompletedTask; };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await s.Model.CheckProviderAsync();
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(1, s.Runtime.Opens);
        Assert.Equal(boundary == "open" ? 0 : 1, s.Runtime.Remote.AvailabilityReads); Assert.Equal(0, s.Runtime.Remote.RecordReads);
        Assert.False(s.Model.HasPendingOperation); Assert.True(s.Model.CanLeave); Assert.False(s.Model.CanCheckProvider);
        Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Equal(0, s.Factory.Calls); s.Model.Retire();
    }
}
