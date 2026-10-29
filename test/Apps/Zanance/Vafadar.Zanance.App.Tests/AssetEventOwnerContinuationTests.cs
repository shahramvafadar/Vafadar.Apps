using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-643: actual event root, pending read, accepted callback and native consent boundaries.</summary>
public sealed class AssetEventOwnerContinuationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    /// <summary>A replaced root cannot receive the original native discard answer.</summary>
    [Fact]
    public async Task Root_replacement_during_discard_keeps_original_data_and_draft()
    {
        using var f = new FlowFixture(); var (vm, _, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f); var owns = true;
        vm.BindRootOwner(() => owns); f.Platform.PendingConfirmation = () => { owns = false; return Task.FromResult(true); };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.CancelCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Routes); Assert.False(vm.CanEdit); Assert.False(vm.CanSave); Assert.True(vm.IsDirty);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Actual departure cancels the same pending original read instead of starting another or publishing it later.</summary>
    [Fact]
    public async Task Pending_original_reader_cancels_on_actual_close_without_publication()
    {
        using var f = new FlowFixture(); var (vm, _, type, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Before = async ct => { entered.TrySetResult(); await Task.Delay(Timeout.Infinite, ct); };
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var pending = vm.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" });
        await entered.Task.WaitAsync(Ct); Assert.False(vm.CanSave); Assert.True(vm.CanLeave);
        f.Platform.Confirmations.Enqueue(true); f.Platform.PendingNavigation = () => { vm.RetireReview(); return Task.CompletedTask; };
        await vm.CancelCommand.ExecuteAsync(null); await pending.WaitAsync(Ct); factory.Before = null;
        Assert.Single(f.Platform.Routes); Assert.False(vm.CanEdit); Assert.False(vm.CanReload);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Resuming a published original form rereads its source without resetting the independent raw numeric meaning.</summary>
    [Fact]
    public async Task Same_form_resume_preserves_raw_quantity_and_basis_contexts()
    {
        using var f = new FlowFixture(); var (vm, _, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        f.Localization.SetFormattingCulture("de-DE"); vm.QuantityText = ""; vm.QuantityText = "2,000";
        vm.BasisText = ""; vm.BasisText = "50,00"; vm.Note = "  D643 raw note  ";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); vm.RetireReview(); Assert.False(vm.CanSave);
        vm.SetViewActive(true);
        await vm.WaitForPublicationAsync().WaitAsync(Ct);
        Assert.True(vm.Loading.IsReady); Assert.True(vm.CanSave); Assert.Equal("2,000", vm.QuantityText);
        Assert.Equal("50,00", vm.BasisText); Assert.Equal("  D643 raw note  ", vm.Note); Assert.True(vm.IsDirty);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>A completed initial failure must wait for the actual explicit Retry, including after an appearance transition.</summary>
    [Fact]
    public async Task Initial_failure_is_not_retried_by_appearance()
    {
        using var f = new FlowFixture(); var (_, store, type, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var calls = 0; factory.Before = _ => { calls++; throw new IOException("D643 original initial read refusal."); };
        var vm = new AssetEventEditorViewModel(store, f.Store, new HoldingText(f.Translator, f.Localization, new DateFormatter(f.Localization)),
            f.Translator, f.Localization, f.Time, f.Platform);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Opening" });
        Assert.True(vm.Loading.HasFailed); Assert.Equal(1, calls); factory.Before = null;
        vm.SetViewActive(false); vm.SetViewActive(true); Assert.True(vm.Loading.HasFailed); Assert.Equal(1, calls);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.Loading.IsReady); Assert.True(vm.CanSave); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>One pending discard owns the form; direct repeated command invocation cannot open another dialog.</summary>
    [Fact]
    public async Task Pending_discard_disables_save_and_prevents_a_second_native_prompt()
    {
        using var f = new FlowFixture(); var (vm, _, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); var prompts = 0;
        f.Platform.PendingConfirmation = () => { prompts++; entered.TrySetResult(); return release.Task; };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var pending = vm.CancelCommand.ExecuteAsync(null);
        await entered.Task.WaitAsync(Ct); Assert.False(vm.CanSave); Assert.False(vm.CanLeave);
        await vm.CancelCommand.ExecuteAsync(null); Assert.Equal(1, prompts); release.SetResult(false); await pending;
        Assert.True(vm.CanSave); Assert.True(vm.CanLeave); Assert.Empty(f.Platform.Routes);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>An accepted-receipt effect failure remains known after the actual commit, before the ordinary notification.</summary>
    [Fact]
    public async Task Accepted_callback_failure_keeps_original_session_committed_without_duplicate_event()
    {
        using var f = new FlowFixture(); var (_, store, type, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        var session = new HoldingEventEditorSession(store); var review = await session.LoadAsync(type.Id, null, ct: Ct);
        var item = new Vafadar.Zanance.Core.Holdings.AssetEvent { AssetTypeId = type.Id, LocationId = review!.Inputs.Locations[0].Id,
            Kind = Vafadar.Zanance.Core.Holdings.AssetEventKind.Opening, Date = new(2026, 10, 1), Quantity = 1000 };
        await Assert.ThrowsAsync<CommittedWriteNotificationException<HoldingSaveResult>>(() =>
            session.SaveAsync(item, [], () => true, () => throw new IOException("D643 accepted callback refusal.")));
        Assert.True(session.Committed); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        Assert.Null(await session.SaveAsync(item, [], () => true)); Assert.Single(await store.GetEventsAsync(cancellationToken: Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>A source refusal keeps Close available and explicit Retry retains the same original raw form.</summary>
    [Fact]
    public async Task Source_refusal_allows_close_and_explicit_reread_without_reposting()
    {
        using var f = new FlowFixture(); var (vm, store, type, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        type.Note = "D643 changed original source"; await store.SaveTypeAsync(type, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null);
        Assert.False(vm.CanSave); Assert.False(vm.CanEdit); Assert.True(vm.CanLeave); Assert.True(vm.CanReload); Assert.True(vm.ShowSourceRetry);
        Assert.Equal("2", vm.QuantityText); Assert.Equal("50", vm.BasisText);
        await vm.ReloadCommand.ExecuteAsync(null); Assert.True(vm.CanSave); Assert.False(vm.ShowSourceRetry); Assert.Null(vm.Error);
        Assert.Equal("2", vm.QuantityText); Assert.Equal("50", vm.BasisText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Display changes retire an old native consent without reinterpreting the original financial inputs.</summary>
    [Fact]
    public async Task Display_change_during_discard_cannot_close_the_refreshed_form()
    {
        using var f = new FlowFixture(); var (vm, _, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        vm.SetMoneyPresentationActive(true);
        f.Platform.PendingConfirmation = () => { f.Localization.SetFormattingCulture("de-DE"); return Task.FromResult(true); };
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.CancelCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.Routes); Assert.Equal("2", vm.QuantityText); Assert.Equal("50", vm.BasisText); Assert.True(vm.CanSave);
        vm.SetMoneyPresentationActive(false); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
    /// <summary>Post-commit root retirement never navigates a different root or overwrites its later raw text.</summary>
    [Fact]
    public async Task Actual_acceptance_under_retired_root_keeps_newer_note_and_never_navigates()
    {
        using var f = new FlowFixture(); var (vm, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f); var owns = true;
        vm.BindRootOwner(() => owns); var note = vm.Note;
        store.Changed += (_, _) => { owns = false; vm.Note = "D643 late root note"; };
        await vm.SaveCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes); Assert.True(vm.IsDirty); Assert.False(vm.CanSave);
        Assert.Equal(note, Assert.Single(await store.GetEventsAsync(cancellationToken: Ct)).Note);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Actual receipt and newer-draft feedback follows live language changes without replaying the accepted event.</summary>
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Accepted_newer_draft_feedback_uses_live_language_without_another_save(bool notificationFails)
    {
        using var f = new FlowFixture(); var (vm, store, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        vm.SetMoneyPresentationActive(true);
        store.Changed += (_, _) => { vm.Note = "D643 later original note"; if (notificationFails) throw new IOException("D643 notification refusal."); };
        await vm.SaveCommand.ExecuteAsync(null); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(x => x.CultureName == "de"));
        Assert.Contains(f.Translator["AssetEvent_SavedDraftChanged"], vm.Error!); Assert.Equal(f.Translator["Common_Close"], vm.CloseText);
        if (notificationFails) Assert.Contains(f.Translator["AssetEvent_SavedNeedsReview"], vm.Error!);
        Assert.True(vm.IsDirty); Assert.False(vm.CanSave); Assert.Empty(f.Platform.Routes);
        vm.SetMoneyPresentationActive(false); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Live original choice/title captions must change language without replacing the current raw financial draft.</summary>
    [Fact]
    public async Task Live_caption_change_preserves_original_kind_and_raw_physical_values()
    {
        using var f = new FlowFixture(); var (vm, _, _, _) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        vm.SetMoneyPresentationActive(true); var kind = vm.KindIndex; var unit = vm.UnitIndex;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(x => x.CultureName == "de"));
        Assert.Equal(f.Translator["AssetEvent_NewTitle"], vm.Title);
        Assert.Equal(f.Translator["AssetEvent_Opening"], vm.KindNames[kind]); Assert.Equal(kind, vm.KindIndex); Assert.Equal(unit, vm.UnitIndex);
        Assert.Equal("2", vm.QuantityText); Assert.Equal("50", vm.BasisText); Assert.Equal("Original unchanged event draft", vm.Note);
        vm.SetMoneyPresentationActive(false); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Repeated native Retry cannot replace the loading-state owner of the same pending original reader.</summary>
    [Fact]
    public async Task Pending_source_retry_disables_another_retry_and_keeps_the_original_publication()
    {
        using var f = new FlowFixture(); var (vm, store, type, factory) = await HoldingLocationCreationFlowTests.ReadyAsync(f);
        type.Note = "D643 original source changed before Retry"; await store.SaveTypeAsync(type, Ct);
        await vm.SaveCommand.ExecuteAsync(null); Assert.True(vm.CanReload);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Before = async ct => { entered.TrySetResult(); await release.Task.WaitAsync(ct); };
        var pending = vm.ReloadCommand.ExecuteAsync(null); await entered.Task.WaitAsync(Ct);
        var enabled = vm.CanReload; var second = vm.ReloadCommand.ExecuteAsync(null);
        release.TrySetResult(); await Task.WhenAll(pending, second).WaitAsync(Ct); factory.Before = null;
        Assert.False(enabled, $"Another Retry was enabled while pending; final original source Ready={vm.Loading.IsReady}.");
        Assert.True(vm.Loading.IsReady); Assert.True(vm.CanSave); Assert.Equal("2", vm.QuantityText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

}
