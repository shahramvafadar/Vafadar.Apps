using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Data.Tests;

public sealed class PlanStoreTests : IDisposable
{
    private static readonly DateOnly Today = new(2027, 3, 20);
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;
    private readonly PlanStore _plans;
    private readonly AutoPostProcessor _processor;

    public PlanStoreTests()
    {
        _services = new ServiceCollection().AddZananceData(_directory.Combine("zanance.db")).BuildServiceProvider();
        _services.MigrateLocalDatabase<ZananceDbContext>();
        _store = _services.GetRequiredService<ZananceStore>();
        _plans = _services.GetRequiredService<PlanStore>();
        _processor = _services.GetRequiredService<AutoPostProcessor>();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _services.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Dispose();
    }

    [Fact]
    [Trait("AT", "AT-30")]
    public async Task Running_auto_post_twice_creates_one_unreviewed_entry_per_occurrence()
    {
        var plan = await NewPlanAsync(autoPost: true);

        var first = await _processor.RunAsync(Today, Ct);
        var second = await _processor.RunAsync(Today, Ct);
        await Task.WhenAll(_processor.RunAsync(Today, Ct), _processor.RunAsync(Today, Ct));

        var entries = await _store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(3, first.Posted);
        Assert.Equal(0, second.Posted);
        Assert.Equal(3, entries.Count);
        Assert.All(entries, e => Assert.Equal(ReviewState.Unreviewed, e.Review));
        Assert.All(entries, e => Assert.Equal(plan.Id, e.ScheduleId));
    }

    [Fact]
    [Trait("AT", "AT-31")]
    public async Task Deleting_an_automatic_entry_does_not_bring_it_back()
    {
        await NewPlanAsync(autoPost: true);
        await _processor.RunAsync(Today, Ct);
        var march = (await _store.GetEntriesAsync(cancellationToken: Ct)).First();

        await _store.DeleteEntryAsync(march.Id, Ct);
        var rerun = await _processor.RunAsync(Today, Ct);

        Assert.Equal(0, rerun.Posted);
        Assert.Equal(1, rerun.NeedsReview);
        Assert.Equal(2, (await _store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task Undo_of_a_deleted_settlement_settles_the_occurrence_again()
    {
        var plan = await NewPlanAsync(autoPost: true);
        await _processor.RunAsync(Today, Ct);
        var entry = (await _store.GetEntriesAsync(cancellationToken: Ct)).First();

        var deleted = await _store.DeleteEntryAsync(entry.Id, Ct);
        await _store.RestoreEntriesAsync(deleted, Ct);

        var state = (await _plans.GetStatesAsync(plan.Id, Ct)).Single(s => s.OriginalDate == entry.OccurrenceDate);
        Assert.Equal(OccurrenceStatus.Settled, state.Status);
        Assert.Equal(entry.Id, state.EntryId);
    }

    [Fact]
    [Trait("AT", "AT-33")]
    public async Task Returning_after_months_posts_each_missed_occurrence_once_and_nothing_before_enabling()
    {
        var plan = await NewPlanAsync(autoPost: true, autoPostFrom: new DateOnly(2027, 2, 1));

        var result = await _processor.RunAsync(new DateOnly(2027, 6, 30), Ct);

        var dates = (await _store.GetEntriesAsync(cancellationToken: Ct)).Select(e => e.OccurrenceDate).Order();
        Assert.Equal([new DateOnly(2027, 2, 5), new DateOnly(2027, 3, 5), new DateOnly(2027, 4, 5), new DateOnly(2027, 5, 5), new DateOnly(2027, 6, 5)], dates);
        Assert.Equal(1, result.NeedsReview);
        Assert.Equal(plan.Id, (await _store.GetEntriesAsync(cancellationToken: Ct)).First().ScheduleId);
    }

    [Fact]
    public async Task Estimated_plans_are_never_posted_automatically()
    {
        await NewPlanAsync(autoPost: true, mode: AmountMode.Estimated);

        var result = await _processor.RunAsync(Today, Ct);

        Assert.Equal(0, result.Posted);
        Assert.Equal(3, result.NeedsReview);
        Assert.Empty(await _store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    [Trait("AT", "AT-29")]
    public async Task Linking_an_existing_entry_settles_the_occurrence_without_a_second_entry()
    {
        var plan = await NewPlanAsync(autoPost: false);
        var manual = new LedgerEntry { Kind = EntryKind.Expense, AccountId = plan.AccountId, Amount = 95_000, Date = new DateOnly(2027, 3, 4) };
        await _store.SaveEntryAsync(manual, Ct);
        var march = Occurrences.Between(plan, [], new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 31), Today).Single();

        await _plans.LinkAsync(march, manual.Id, Ct);

        var states = await _plans.GetStatesAsync(plan.Id, Ct);
        var reloaded = Occurrences.Between(plan, states, new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 31), Today).Single();
        Assert.Equal(OccurrenceView.Settled, reloaded.Status);
        Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(plan.Id, (await _store.GetEntryAsync(manual.Id, Ct))!.ScheduleId);

        // Unsettling keeps the user's own entry and only removes the link.
        await _plans.UnsettleAsync(reloaded, Ct);
        Assert.Null((await _store.GetEntryAsync(manual.Id, Ct))!.ScheduleId);
    }

    [Fact]
    [Trait("AT", "AT-25")]
    public async Task Split_moves_later_settlements_to_the_new_plan_and_keeps_history()
    {
        var plan = await NewPlanAsync(autoPost: true);
        await _processor.RunAsync(Today, Ct);

        var next = PlanActions.SplitFrom(plan, new DateOnly(2027, 3, 1));
        next.Amount = 99_000;
        await _plans.SaveSplitAsync(plan, next, Ct);

        var entries = await _store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(2, entries.Count(e => e.ScheduleId == plan.Id));
        Assert.Equal(new DateOnly(2027, 3, 5), entries.Single(e => e.ScheduleId == next.Id).OccurrenceDate);
        Assert.All(entries, e => Assert.Equal(95_000, e.Amount));
        Assert.Single(await _plans.GetStatesAsync(next.Id, Ct));
    }

    [Fact]
    public async Task Plans_with_history_cannot_be_deleted()
    {
        var plan = await NewPlanAsync(autoPost: true);
        var unused = await NewPlanAsync(autoPost: false);
        await _processor.RunAsync(Today, Ct);

        Assert.False(await _plans.DeleteScheduleAsync(plan.Id, Ct));
        Assert.True(await _plans.DeleteScheduleAsync(unused.Id, Ct));
        Assert.Single(await _plans.GetSchedulesAsync(Ct));
    }

    [Fact]
    [Trait("AT", "AT-66")]
    public async Task Partial_payments_keep_the_occurrence_open_with_the_outstanding_rest_until_the_final_payment()
    {
        var plan = await NewPlanAsync(autoPost: false);
        async Task<Occurrence> MarchAsync() =>
            Occurrences.Between(plan, await _plans.GetStatesAsync(plan.Id, Ct), new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 31), Today).Single();

        await _plans.PayPartAsync(await MarchAsync(), Occurrences.CreateEntry(await MarchAsync(), 40_000, new DateOnly(2027, 3, 2), ReviewState.Confirmed), Ct);
        await _plans.PayPartAsync(await MarchAsync(), Occurrences.CreateEntry(await MarchAsync(), 30_000, new DateOnly(2027, 3, 4), ReviewState.Confirmed), Ct);

        var march = await MarchAsync();
        Assert.True(march.IsOpen);
        Assert.Equal(70_000, march.Paid);
        Assert.Equal(25_000, march.Outstanding);
        Assert.Equal(2, (await _plans.GetPartialPaymentsAsync(march, Ct)).Count);

        // The forecast only expects the outstanding rest, since the parts are already in the ledger.
        var forecast = Core.Forecasts.ForecastCalculator.Compute(await _store.GetAccountsAsync(cancellationToken: Ct), await _store.GetEntriesAsync(cancellationToken: Ct), [plan], await _plans.GetStatesAsync(plan.Id, Ct), Today, new DateOnly(2027, 3, 31));
        Assert.Equal(-25_000, forecast.Single().Items.Single(i => i.OriginalDate == new DateOnly(2027, 3, 5)).Effect);

        // A payment reaching the rest settles it; the automatic posting never adds the full amount on top.
        await _plans.PayPartAsync(march, Occurrences.CreateEntry(march, 25_000, new DateOnly(2027, 3, 6), ReviewState.Confirmed), Ct);
        Assert.Equal(OccurrenceView.Settled, (await MarchAsync()).Status);
        Assert.Equal(95_000, (await _store.GetEntriesAsync(cancellationToken: Ct)).Where(e => e.OccurrenceDate == new DateOnly(2027, 3, 5)).Sum(e => e.Amount));
    }

    [Fact]
    public async Task Deleting_or_restoring_a_partial_payment_changes_only_the_paid_amount_and_blocks_auto_post()
    {
        var plan = await NewPlanAsync(autoPost: true, autoPostFrom: new DateOnly(2027, 3, 1));
        async Task<Occurrence> MarchAsync() =>
            Occurrences.Between(plan, await _plans.GetStatesAsync(plan.Id, Ct), new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 31), Today).Single();
        var part = Occurrences.CreateEntry(await MarchAsync(), 10_000, new DateOnly(2027, 3, 1), ReviewState.Confirmed);
        await _plans.PayPartAsync(await MarchAsync(), part, Ct);

        Assert.Equal(0, (await _processor.RunAsync(Today, Ct)).Posted);

        var deleted = await _store.DeleteEntryAsync(part.Id, Ct);
        Assert.Equal(0, (await MarchAsync()).Paid);
        Assert.True((await MarchAsync()).IsOpen);

        await _store.RestoreEntriesAsync(deleted, Ct);
        Assert.Equal(10_000, (await MarchAsync()).Paid);
        Assert.True((await MarchAsync()).IsOpen);
    }

    [Fact]
    public async Task Resuming_a_plan_keeps_an_occurrence_settled_in_advance_and_never_posts_it_again()
    {
        var plan = await NewPlanAsync(autoPost: true);
        var april = Occurrences.Between(plan, [], new DateOnly(2027, 4, 1), new DateOnly(2027, 4, 30), Today).Single();
        Assert.True((await _plans.SettleAsync(april, Occurrences.CreateEntry(april, 95_000, Today, ReviewState.Confirmed), Ct)).Succeeded);

        PlanActions.Pause(plan, new DateOnly(2027, 3, 25));
        await _plans.SaveScheduleAsync(plan, Ct);
        var continuation = PlanActions.Resume(plan, new DateOnly(2027, 4, 1))!;
        Assert.True(await _plans.SaveSplitAsync(plan, continuation, Ct));

        await _processor.RunAsync(new DateOnly(2027, 5, 10), Ct);
        var entries = await _store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Single(entries, e => e.OccurrenceDate == new DateOnly(2027, 4, 5));
        Assert.Single(entries, e => e.OccurrenceDate == new DateOnly(2027, 5, 5) && e.ScheduleId == continuation.Id);
        Assert.Equal(continuation.Id, entries.Single(e => e.OccurrenceDate == new DateOnly(2027, 4, 5)).ScheduleId);
    }

    [Fact]
    public async Task A_this_and_future_change_is_refused_when_recorded_occurrences_would_not_fit_the_new_dates()
    {
        var plan = await NewPlanAsync(autoPost: false);
        var april = Occurrences.Between(plan, [], new DateOnly(2027, 4, 1), new DateOnly(2027, 4, 30), Today).Single();
        await _plans.SettleAsync(april, Occurrences.CreateEntry(april, 95_000, Today, ReviewState.Confirmed), Ct);

        // Another day of the month: April 5 is no longer a date of the plan, so the change is refused and nothing moves.
        var moved = PlanActions.SplitFrom(plan, new DateOnly(2027, 3, 21));
        moved.Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 10) };
        Assert.False(await _plans.SaveSplitAsync(plan, moved, Ct));
        Assert.Single(await _plans.GetSchedulesAsync(Ct));

        // The same dates with a new amount are fine.
        var reloaded = (await _plans.GetSchedulesAsync(Ct)).Single();
        var priced = PlanActions.SplitFrom(reloaded, new DateOnly(2027, 3, 21));
        priced.Amount = 99_000;
        Assert.True(await _plans.SaveSplitAsync(reloaded, priced, Ct));
        Assert.Equal(priced.Id, (await _store.GetEntriesAsync(cancellationToken: Ct)).Single().ScheduleId);
    }

    [Fact]
    public async Task A_settlement_entry_without_its_state_is_repaired_and_not_posted_again()
    {
        var plan = await NewPlanAsync(autoPost: true);
        // An interrupted settle: the entry is saved, the state is not.
        var march = Occurrences.Between(plan, [], new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 31), Today).Single();
        var entry = Occurrences.CreateEntry(march, 95_000, march.DueDate, ReviewState.Confirmed);
        entry.ScheduleId = plan.Id; entry.OccurrenceDate = march.OriginalDate;
        Assert.True((await _store.SaveEntryAsync(entry, Ct)).Succeeded);

        Assert.Equal(1, await _plans.RepairSettlementsAsync(Ct));
        Assert.Equal(0, await _plans.RepairSettlementsAsync(Ct));
        await _processor.RunAsync(Today, Ct);
        Assert.Single(await _store.GetEntriesAsync(cancellationToken: Ct), e => e.OccurrenceDate == march.OriginalDate);
    }

    private async Task<Schedule> NewPlanAsync(bool autoPost, AmountMode mode = AmountMode.Fixed, DateOnly? autoPostFrom = null)
    {
        var account = new Account { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 12, 1) };
        await _store.SaveAccountAsync(account, Ct);
        var plan = new Schedule
        {
            Name = "Rent",
            Kind = EntryKind.Expense,
            AccountId = account.Id,
            AmountMode = mode,
            Amount = 95_000,
            Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 1, 5) },
            AutoPost = autoPost,
            AutoPostFrom = autoPostFrom,
        };
        await _plans.SaveScheduleAsync(plan, Ct);
        return plan;
    }
}
