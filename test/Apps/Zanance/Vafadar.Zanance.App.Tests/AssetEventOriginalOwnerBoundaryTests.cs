using Vafadar.Zanance.App.Features.Holdings;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-643: original asset-event frame, native discard and accepted raw draft keep their own lifetime.</summary>
public sealed class AssetEventOriginalOwnerBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    /// <summary>Actual departure disables old raw-field input, as well as the original Save authority.</summary>
    [Fact]
    public async Task Departed_original_asset_event_does_not_remain_editable()
    {
        using var f = new FlowFixture(); var (vm, _, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        vm.RetireReview(); Assert.False(vm.CanEdit); Assert.False(vm.CanSave);
    }
    /// <summary>An original discard answer cannot close text changed while the native dialog was pending.</summary>
    [Fact]
    public async Task Late_discard_answer_cannot_close_a_newer_original_event_note()
    {
        using var f = new FlowFixture(); var (vm, _, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        f.Platform.PendingConfirmation = () => { vm.Note = "D643 later unsaved note"; return Task.FromResult(true); };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.CancelCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Routes); Assert.Equal("D643 later unsaved note", vm.Note); Assert.True(vm.IsDirty);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>A newer raw note after an actual noncash opening cannot be mistaken for the accepted original payload.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Accepted_original_event_preserves_its_later_raw_note_without_reposting(bool notificationFails)
    {
        using var f = new FlowFixture(); var (vm, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var submittedNote = vm.Note;
        store.Changed += (_, _) => { vm.Note = "D643 note after actual acceptance"; if (notificationFails) throw new IOException("D643 original accepted event notification refusal."); };
        await vm.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(await store.GetEventsAsync(cancellationToken: Ct));
        Assert.Equal(submittedNote, saved.Note); Assert.Equal("D643 note after actual acceptance", vm.Note);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.False(vm.CanSave);
        Assert.True(vm.IsDirty, "The accepted original event swallowed the later raw note's dirty ownership.");
        Assert.Empty(f.Platform.Routes);
    }
}
