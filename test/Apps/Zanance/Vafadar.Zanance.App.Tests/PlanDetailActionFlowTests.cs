using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual original consent/writer/source failures, accepted transitions and preservation through real SQLite.</summary>
public sealed class PlanDetailActionFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Today = PlanDetailOriginalBoundaryTests.Today;
    private static PlanStore Plans(FlowFixture f) => PlanDetailOriginalBoundaryTests.Plans(f);
    private static PlanDetailActions Actor(FlowFixture f, Guid id)
    {
        var actor = new PlanDetailActions(Plans(f), f.Translator, f.Localization, f.Time, f.Platform);
        actor.SetOwner(id, true); return actor;
    }
    private static async Task<PlanDetailSnapshot> ReadAsync(FlowFixture f, Guid id) =>
        await Plans(f).ReadDetailSnapshotAsync(id, Today, ct: Ct);
    private static async Task<string> PacketAsync(FlowFixture f)
    {
        await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
        return JsonSerializer.Serialize(new { Accounts = await db.Accounts.AsNoTracking().OrderBy(a => a.Id).ToListAsync(Ct),
            Schedules = await db.Schedules.AsNoTracking().OrderBy(s => s.Id).ToListAsync(Ct),
            States = await db.OccurrenceStates.AsNoTracking().OrderBy(s => s.Id).ToListAsync(Ct),
            Entries = await db.Entries.AsNoTracking().OrderBy(e => e.Id).ToListAsync(Ct), Settings = await db.Settings.AsNoTracking().ToListAsync(Ct) });
    }

    [Theory]
    [InlineData(PlanDetailOperation.Pause)]
    [InlineData(PlanDetailOperation.End)]
    [InlineData(PlanDetailOperation.Delete)]
    public async Task Native_confirmation_cannot_outlive_its_original_active_owner(PlanDetailOperation operation)
    {
        using var f = new FlowFixture(); var ids = await PlanDetailOriginalBoundaryTests.SeedAsync(f); var source = await ReadAsync(f, ids.Plan);
        var before = await PacketAsync(f); var actor = Actor(f, ids.Plan);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => { entered.SetResult(); return answer.Task; };
        var pending = actor.RunAsync(source, operation, Today, () => true);
        await entered.Task.WaitAsync(Ct); actor.SetOwner(ids.Plan, false); answer.SetResult(true);
        Assert.Null(await pending); Assert.False(source.Source.Accepted); Assert.Equal(before, await PacketAsync(f));
        Assert.False(actor.IsBusy); Assert.Null(actor.AcceptedChange);
    }

    [Theory]
    [InlineData("day")]
    [InlineData("display")]
    [InlineData("query")]
    public async Task Changed_original_consent_context_cannot_apply_a_true_answer(string change)
    {
        using var f = new FlowFixture(); var ids = await PlanDetailOriginalBoundaryTests.SeedAsync(f); var source = await ReadAsync(f, ids.Plan);
        var before = await PacketAsync(f); var actor = Actor(f, ids.Plan);
        f.Platform.PendingConfirmation = () =>
        {
            if (change == "day") f.Time.Now = f.Time.Now.AddDays(1);
            if (change == "display") f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == "de"));
            if (change == "query") actor.SetOwner(Guid.NewGuid(), true);
            return Task.FromResult(true);
        };
        Assert.Null(await actor.RunAsync(source, PlanDetailOperation.Pause, Today, () => true));
        Assert.Equal(before, await PacketAsync(f)); Assert.False(source.Source.Accepted); Assert.Null(actor.AcceptedChange);
    }

    [Fact]
    public async Task Actual_source_edit_while_confirmation_waits_is_not_overwritten_by_the_old_answer()
    {
        using var f = new FlowFixture(); var ids = await PlanDetailOriginalBoundaryTests.SeedAsync(f); var source = await ReadAsync(f, ids.Plan);
        var actor = Actor(f, ids.Plan);
        f.Platform.PendingConfirmation = async () =>
        {
            await using var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct);
            await db.Schedules.ExecuteUpdateAsync(set => set.SetProperty(s => s.Note, "Changed during original consent"), Ct);
            return true;
        };
        Assert.Null(await actor.RunAsync(source, PlanDetailOperation.End, Today, () => true));
        var actual = Assert.Single(await Plans(f).GetSchedulesAsync(Ct));
        Assert.Equal(ScheduleState.Active, actual.State); Assert.Equal("Changed during original consent", actual.Note);
        Assert.False(source.Source.Accepted); Assert.NotNull(actor.Failure); Assert.NotNull(actor.Feedback);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_pending_original_confirmation_shares_no_second_action_and_cancel_never_acquires_a_writer()
    {
        using var f = new FlowFixture(); var ids = await PlanDetailOriginalBoundaryTests.SeedAsync(f); var source = await ReadAsync(f, ids.Plan);
        var actor = Actor(f, ids.Plan); var before = await PacketAsync(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => { entered.TrySetResult(); return answer.Task; };
        var pending = actor.RunAsync(source, PlanDetailOperation.Pause, Today, () => true); await entered.Task.WaitAsync(Ct);
        Assert.Null(await actor.RunAsync(source, PlanDetailOperation.Delete, Today, () => true));
        answer.SetResult(false); Assert.Null(await pending); Assert.Single(f.Platform.ConfirmationDialogs);
        Assert.False(source.Source.IsPending); Assert.Equal(before, await PacketAsync(f));
    }

    [Fact]
    public async Task Original_pause_resume_end_and_delete_reuse_domain_policies_without_posting_financial_entries()
    {
        using var f = new FlowFixture(); var ids = await PlanDetailOriginalBoundaryTests.SeedAsync(f); var actor = Actor(f, ids.Plan);
        var source = await ReadAsync(f, ids.Plan); f.Platform.Confirmations.Enqueue(true);
        var paused = await actor.RunAsync(source, PlanDetailOperation.Pause, Today, () => true);
        Assert.True(paused?.Accepted); Assert.Equal(ScheduleState.Paused, (await Plans(f).GetScheduleAsync(ids.Plan, Ct))!.State);
        source = await ReadAsync(f, ids.Plan);
        var resumed = await actor.RunAsync(source, PlanDetailOperation.Resume, Today.AddDays(1), () => true); Assert.True(resumed?.Accepted);
        var continuation = (await Plans(f).GetScheduleAsync(resumed!.DestinationId, Ct))!;
        Assert.Equal(ids.Plan, continuation.PreviousScheduleId); Assert.Equal(Today.AddDays(1), continuation.ActiveFrom);
        Assert.Equal(ScheduleState.Active, continuation.State); Assert.Equal(1000, continuation.Amount);
        actor.SetOwner(continuation.Id, true); source = await ReadAsync(f, continuation.Id); f.Platform.Confirmations.Enqueue(true);
        var ended = await actor.RunAsync(source, PlanDetailOperation.End, Today, () => true); Assert.True(ended?.Accepted);
        Assert.Equal(ScheduleState.Ended, (await Plans(f).GetScheduleAsync(continuation.Id, Ct))!.State);
        source = await ReadAsync(f, continuation.Id); f.Platform.Confirmations.Enqueue(true);
        var deleted = await actor.RunAsync(source, PlanDetailOperation.Delete, Today, () => true); Assert.True(deleted?.Accepted);
        Assert.Null(await Plans(f).GetScheduleAsync(continuation.Id, Ct)); Assert.Single(await Plans(f).GetSchedulesAsync(Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Empty(await Plans(f).GetStatesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(PlanDetailOperation.Pause)]
    [InlineData(PlanDetailOperation.Delete)]
    public async Task A_failed_original_notification_keeps_known_acceptance_and_cannot_resubmit_the_change(PlanDetailOperation operation)
    {
        using var f = new FlowFixture(); var ids = await PlanDetailOriginalBoundaryTests.SeedAsync(f); var source = await ReadAsync(f, ids.Plan);
        var actor = Actor(f, ids.Plan); f.Platform.Confirmations.Enqueue(true);
        Plans(f).Changed += (_, _) => throw new IOException("Owned postcommit notification refusal");
        var result = await actor.RunAsync(source, operation, Today, () => true);
        Assert.True(result?.Accepted); Assert.True(source.Source.Accepted); Assert.Same(result, actor.AcceptedChange);
        Assert.Equal(f.Translator["Plan_DetailChanged"], actor.Result); Assert.Equal(f.Translator["Plan_DetailChangedFollowUpFailed"], actor.Feedback);
        var after = await PacketAsync(f); Assert.Null(await actor.RunAsync(source, operation, Today, () => true));
        Assert.Equal(after, await PacketAsync(f)); Assert.Single(f.Platform.ConfirmationDialogs);
        actor.ClearReadFailure(); Assert.Equal(f.Translator["Plan_DetailChanged"], actor.Result);
    }

    [Fact]
    public async Task An_uncommitted_resume_retry_keeps_its_one_continuation_and_moves_only_original_recorded_ownership()
    {
        using var f = new FlowFixture(); var ids = await PlanDetailOriginalBoundaryTests.SeedAsync(f);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        {
            await db.Schedules.ExecuteUpdateAsync(set => set.SetProperty(s => s.State, ScheduleState.Paused).SetProperty(s => s.PausedFrom, (DateOnly?)Today), Ct);
            db.OccurrenceStates.Add(new() { ScheduleId = ids.Plan, OriginalDate = Today.AddMonths(1), Status = OccurrenceStatus.Open, Amount = 800 });
            await db.SaveChangesAsync(Ct);
        }
        var source = await ReadAsync(f, ids.Plan); var change = Plans(f).PrepareDetailChange(source, PlanDetailOperation.Resume, Today.AddDays(1));
        var destination = change.DestinationId;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Plans(f).ApplyDetailChangeAsync(change, () => false, Ct));
        Assert.False(change.Accepted); Assert.False(source.Source.IsPending); Assert.Single(await Plans(f).GetSchedulesAsync(Ct));
        Assert.True(await Plans(f).ApplyDetailChangeAsync(change, () => true, Ct)); Assert.True(change.Accepted); Assert.Equal(destination, change.DestinationId);
        var state = Assert.Single(await Plans(f).GetStatesAsync(cancellationToken: Ct)); Assert.Equal(destination, state.ScheduleId);
        Assert.Equal(Today.AddMonths(1), state.OriginalDate); Assert.Equal(800, state.Amount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Plans(f).ApplyDetailChangeAsync(change, () => true, Ct));
        Assert.Equal(2, (await Plans(f).GetSchedulesAsync(Ct)).Count); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(OccurrenceStatus.Open, true)]
    [InlineData(OccurrenceStatus.Skipped, false)]
    public async Task Original_history_free_deletion_distinguishes_open_overrides_from_recorded_history(OccurrenceStatus status, bool allowed)
    {
        using var f = new FlowFixture(); var ids = await PlanDetailOriginalBoundaryTests.SeedAsync(f);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(f).CreateDbContextAsync(Ct))
        { db.OccurrenceStates.Add(new() { ScheduleId = ids.Plan, OriginalDate = Today.AddMonths(1), Status = status, Amount = 800 }); await db.SaveChangesAsync(Ct); }
        var source = await ReadAsync(f, ids.Plan); Assert.Equal(allowed, source.CanDelete);
        if (!allowed)
        { Assert.Throws<InvalidOperationException>(() => Plans(f).PrepareDetailChange(source, PlanDetailOperation.Delete, Today)); Assert.Single(await Plans(f).GetStatesAsync(cancellationToken: Ct)); return; }
        var change = Plans(f).PrepareDetailChange(source, PlanDetailOperation.Delete, Today);
        Assert.True(await Plans(f).ApplyDetailChangeAsync(change, () => true, Ct)); Assert.True(change.Accepted);
        Assert.Empty(await Plans(f).GetSchedulesAsync(Ct)); Assert.Empty(await Plans(f).GetStatesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_known_original_receipt_retranslates_only_for_its_owner_without_another_confirmation_or_write()
    {
        using var f = new FlowFixture(); var ids = await PlanDetailOriginalBoundaryTests.SeedAsync(f); var source = await ReadAsync(f, ids.Plan);
        var actor = Actor(f, ids.Plan); f.Platform.Confirmations.Enqueue(true);
        Plans(f).Changed += (_, _) => throw new IOException("Owned postcommit notification refusal for receipt translation");
        var result = await actor.RunAsync(source, PlanDetailOperation.End, Today, () => true); Assert.True(result?.Accepted);
        var before = await PacketAsync(f);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.First(l => l.CultureName == "de"));
        actor.SetOwner(ids.Plan, false); actor.SetOwner(ids.Plan, true);
        Assert.Equal(f.Translator["Plan_DetailChanged"], actor.Result);
        Assert.Equal(f.Translator["Plan_DetailChangedFollowUpFailed"], actor.Feedback);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(before, await PacketAsync(f));
        actor.SetOwner(Guid.NewGuid(), true); Assert.Null(actor.Result); Assert.Null(actor.Feedback);
        Assert.Same(result, actor.AcceptedChange); Assert.Equal(before, await PacketAsync(f));
    }
}
