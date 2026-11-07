using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Complete original bill source and native command lifetimes reject changed ownership without accepted money.</summary>
public sealed class SettlementOwnershipSnapshotTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = OccurrenceOriginFailureTests.Day;
    internal static async Task<Guid> SeedAsync(FlowFixture f)
    {
        var seed = await OccurrenceOwnershipSnapshotTests.CandidateAsync(f);
        await using var db = await OccurrenceOriginFailureTests.Factory(f).CreateDbContextAsync(Ct);
        await db.Entries.Where(e => e.Id == seed.Entry.Id).ExecuteUpdateAsync(e => e.SetProperty(x => x.ScheduleId, seed.Plan), Ct);
        return seed.Plan;
    }
    private static SettlementViewModel Editor(FlowFixture f) => new(f.Store, f.Translator, f.Localization, f.Time, f.Platform);
    private static SettlementResult Review(AdvanceSettlementDraft source) => AdvanceSettlement.Compute(source.Plan, source.Entries, Day, Day, 234);

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public async Task Edited_original_plan_account_entry_or_tags_cannot_authorize_a_bill(int field)
    {
        using var f = new FlowFixture(); var id = await SeedAsync(f); var source = await f.Store.ReadAdvanceSettlementDraftAsync(id, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        if (field == 0) source.Plan.Name = "Changed caller plan";
        if (field == 1) source.Account.Name = "Changed caller account";
        if (field == 2) source.Entries.Single().Note = "Changed caller note";
        if (field == 3) source.Entries.Single().Tags.Add("Changed caller tag");
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ValidateAdvanceSettlementDraftAsync(source, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveAdvanceSettlementAsync(source.Plan, Day, Day, Review(source), "refused", Ct,
            expectedPath: source.DatabasePath, expectedCurrency: "EUR", original: source));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Changed_persisted_account_or_complete_history_requires_a_new_review(bool history)
    {
        using var f = new FlowFixture(); var id = await SeedAsync(f); var source = await f.Store.ReadAdvanceSettlementDraftAsync(id, Ct);
        await using (var db = await OccurrenceOriginFailureTests.Factory(f).CreateDbContextAsync(Ct))
        {
            if (history) await db.Entries.ExecuteUpdateAsync(e => e.SetProperty(x => x.Payee, "New original source payee"), Ct);
            else await db.Accounts.ExecuteUpdateAsync(a => a.SetProperty(x => x.Name, "New original source caption"), Ct);
        }
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ValidateAdvanceSettlementDraftAsync(source, Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveAdvanceSettlementAsync(source.Plan, Day, Day, Review(source), "refused", Ct,
            expectedPath: source.DatabasePath, expectedCurrency: "EUR", original: source));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task An_equal_copied_plan_is_not_the_original_review_authority()
    {
        using var f = new FlowFixture(); var id = await SeedAsync(f); var source = await f.Store.ReadAdvanceSettlementDraftAsync(id, Ct);
        var copy = JsonSerializer.Deserialize<Schedule>(JsonSerializer.Serialize(source.Plan))!;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveAdvanceSettlementAsync(copy, Day, Day, Review(source), "refused", Ct,
            expectedPath: source.DatabasePath, expectedCurrency: "EUR", original: source));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Retiring_inside_the_bound_writer_refuses_before_any_correction_is_saved()
    {
        using var f = new FlowFixture(); var id = await SeedAsync(f); var source = await f.Store.ReadAdvanceSettlementDraftAsync(id, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var checks = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveAdvanceSettlementAsync(source.Plan, Day, Day, Review(source), "refused", Ct,
            expectedPath: source.DatabasePath, expectedCurrency: "EUR", isCurrent: () => ++checks < 2, original: source));
        Assert.Equal(2, checks); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Delayed_discard_cannot_navigate_a_retired_owner_or_a_newer_draft(bool changedDraft)
    {
        using var f = new FlowFixture(); var id = await SeedAsync(f); var model = Editor(f); await model.LoadAsync(id); model.ActualText = "123.4500";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => { entered.TrySetResult(); return answer.Task; };
        var cancel = model.CancelCommand.ExecuteAsync(null); await entered.Task.WaitAsync(Ct);
        if (changedDraft) model.ActualText = "234.5600"; else model.SetActive(false);
        answer.SetResult(true); await cancel;
        Assert.Empty(f.Platform.Routes); Assert.Equal(changedDraft ? "234.5600" : "123.4500", model.ActualText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Failed_cancel_navigation_covers_the_original_draft_and_explicit_retry_recovers_without_writing()
    {
        using var f = new FlowFixture(); var id = await SeedAsync(f); var model = Editor(f); await model.LoadAsync(id); model.ActualText = "123.4500";
        var before = await GoalChooserFlowTests.SnapshotAsync(f); f.Platform.Confirmations.Enqueue(true);
        f.Platform.PendingNavigation = () => throw new IOException("Original parent return failed");
        await model.CancelCommand.ExecuteAsync(null);
        Assert.True(model.Loading.HasFailed); Assert.False(model.CanEdit); Assert.Equal("123.4500", model.ActualText);
        f.Platform.PendingNavigation = null; await model.ReloadCommand.ExecuteAsync(null);
        Assert.True(model.Loading.IsReady); Assert.True(model.CanEdit); Assert.Equal("123.4500", model.ActualText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Reentrant_page_retirement_during_original_caption_publication_cannot_uncover_the_form()
    {
        using var f = new FlowFixture(); var id = await SeedAsync(f); var model = Editor(f); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        model.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(model.PlanName)) model.SetActive(false); };
        await model.LoadAsync(id);
        Assert.False(model.Loading.IsReady); Assert.False(model.CanEdit); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
