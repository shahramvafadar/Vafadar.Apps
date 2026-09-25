using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;
using Vafadar.Testing;

namespace Vafadar.Finance.Data.Tests;

public sealed class PlanStoreTests : IDisposable
{
    private static readonly DateOnly Today = new(2027, 3, 20);
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly FinanceStore _finance;
    private readonly PlanStore _plans;
    private readonly AutoPostProcessor _processor;

    public PlanStoreTests()
    {
        _services = new ServiceCollection().AddFinanceData(_directory.Combine("finance.db")).BuildServiceProvider();
        _services.MigrateLocalDatabase<FinanceDbContext>();
        _finance = _services.GetRequiredService<FinanceStore>();
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

        var entries = await _finance.GetEntriesAsync(cancellationToken: Ct);
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
        var march = (await _finance.GetEntriesAsync(cancellationToken: Ct)).First();

        await _finance.DeleteEntryAsync(march.Id, Ct);
        var rerun = await _processor.RunAsync(Today, Ct);

        Assert.Equal(0, rerun.Posted);
        Assert.Equal(1, rerun.NeedsReview);
        Assert.Equal(2, (await _finance.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task Undo_of_a_deleted_settlement_settles_the_occurrence_again()
    {
        var plan = await NewPlanAsync(autoPost: true);
        await _processor.RunAsync(Today, Ct);
        var entry = (await _finance.GetEntriesAsync(cancellationToken: Ct)).First();

        var deleted = await _finance.DeleteEntryAsync(entry.Id, Ct);
        await _finance.RestoreEntriesAsync(deleted, Ct);

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

        var dates = (await _finance.GetEntriesAsync(cancellationToken: Ct)).Select(e => e.OccurrenceDate).Order();
        Assert.Equal([new DateOnly(2027, 2, 5), new DateOnly(2027, 3, 5), new DateOnly(2027, 4, 5), new DateOnly(2027, 5, 5), new DateOnly(2027, 6, 5)], dates);
        Assert.Equal(1, result.NeedsReview);
        Assert.Equal(plan.Id, (await _finance.GetEntriesAsync(cancellationToken: Ct)).First().ScheduleId);
    }

    [Fact]
    public async Task Estimated_plans_are_never_posted_automatically()
    {
        await NewPlanAsync(autoPost: true, mode: AmountMode.Estimated);

        var result = await _processor.RunAsync(Today, Ct);

        Assert.Equal(0, result.Posted);
        Assert.Equal(3, result.NeedsReview);
        Assert.Empty(await _finance.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    [Trait("AT", "AT-29")]
    public async Task Linking_an_existing_entry_settles_the_occurrence_without_a_second_entry()
    {
        var plan = await NewPlanAsync(autoPost: false);
        var manual = new LedgerEntry { Kind = EntryKind.Expense, AccountId = plan.AccountId, Amount = 95_000, Date = new DateOnly(2027, 3, 4) };
        await _finance.SaveEntryAsync(manual, Ct);
        var march = Occurrences.Between(plan, [], new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 31), Today).Single();

        await _plans.LinkAsync(march, manual.Id, Ct);

        var states = await _plans.GetStatesAsync(plan.Id, Ct);
        var reloaded = Occurrences.Between(plan, states, new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 31), Today).Single();
        Assert.Equal(OccurrenceView.Settled, reloaded.Status);
        Assert.Single(await _finance.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(plan.Id, (await _finance.GetEntryAsync(manual.Id, Ct))!.ScheduleId);

        // Unsettling keeps the user's own entry and only removes the link.
        await _plans.UnsettleAsync(reloaded, Ct);
        Assert.Null((await _finance.GetEntryAsync(manual.Id, Ct))!.ScheduleId);
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

        var entries = await _finance.GetEntriesAsync(cancellationToken: Ct);
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

    private async Task<Schedule> NewPlanAsync(bool autoPost, AmountMode mode = AmountMode.Fixed, DateOnly? autoPostFrom = null)
    {
        var account = new Account { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 12, 1) };
        await _finance.SaveAccountAsync(account, Ct);
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
