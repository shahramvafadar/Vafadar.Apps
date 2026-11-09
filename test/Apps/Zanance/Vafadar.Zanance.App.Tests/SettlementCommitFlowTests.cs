using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.App.Features.Plans;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original advance-bill writes and retained outcomes over real SQLite and application effects.</summary>
[Trait("AT", "AT-66")]
public sealed class SettlementCommitFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 9);
    private sealed class Factory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal bool RefuseRead;
        public ZananceDbContext CreateDbContext() => RefuseRead ? throw new IOException("Original unavailable file") : original.CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            RefuseRead ? Task.FromException<ZananceDbContext>(new IOException("Original unavailable file")) : original.CreateDbContextAsync(cancellationToken);
    }
    private static async Task<Schedule> SeedAsync(FlowFixture f)
    {
        await f.Store.EnsureDefaultCategoriesAsync(Ct);
        var account = new Account { Name = "Original utilities cash", CurrencyCode = "EUR", Type = AccountType.Checking,
            OpeningDate = Day.AddDays(-8), OpeningBalance = 100000 };
        await f.Store.SaveAccountAsync(account, Ct);
        var food = (await f.Store.GetCategoriesAsync(cancellationToken: Ct)).Single(c => c.SystemKey == "Food");
        var plan = new Schedule { Name = "Original utility advances", Kind = EntryKind.Expense, AccountId = account.Id,
            CategoryId = food.Id, Amount = 1000, Rule = new() { Start = Day.AddDays(-7) } };
        await f.Services.GetRequiredService<PlanStore>().SaveScheduleAsync(plan, Ct);
        var advance = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, CategoryId = food.Id,
            ScheduleId = plan.Id, Date = Day.AddDays(-7), Amount = 1000, Title = "Original advance", Note = "Retain this note" };
        Assert.True((await f.Store.SaveEntryAsync(advance, Ct)).Succeeded); return plan;
    }

    [Fact]
    public async Task Committed_notification_retains_the_original_settlement_result()
    {
        using var f = new FlowFixture(); var plan = await SeedAsync(f);
        var reviewed = AdvanceSettlement.Compute(plan, await f.Store.GetEntriesAsync(cancellationToken: Ct), Day.AddDays(-8), Day, 1400);
        f.Store.Changed += (_, _) => throw new InvalidOperationException("Original post-commit observer refusal");
        var accepted = await Assert.ThrowsAsync<CommittedWriteNotificationException<SaveResult>>(() =>
            f.Store.SaveAdvanceSettlementAsync(plan, Day.AddDays(-8), Day, reviewed, "Original bill difference", Ct));
        Assert.True(accepted.Result.Succeeded);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct);
        Assert.Equal(2, entries.Count);
        Assert.Equal(400, entries.Single(e => e.Title == "Original bill difference").Amount);
    }

    private static SettlementViewModel Editor(FlowFixture f) => new(f.Store, f.Translator, f.Localization, f.Time, f.Platform);

    [Theory]
    [InlineData("14", 400, EntryKind.Expense)]
    [InlineData("4", 600, EntryKind.Refund)]
    public async Task Post_commit_observer_failure_retires_the_form_before_a_return_retry(string actual, long amount, EntryKind kind)
    {
        using var f = new FlowFixture(); var plan = await SeedAsync(f); var vm = Editor(f);
        await vm.LoadAsync(plan.Id); vm.ActualText = actual;
        Assert.True(vm.CanSave); Assert.True(vm.CanEdit);
        f.Store.Changed += (_, _) => throw new InvalidOperationException("Original observer failure");
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.HasCommitted); Assert.False(vm.CanSave); Assert.False(vm.CanEdit); Assert.False(vm.IsDirty);
        Assert.Equal(f.Translator["Settlement_Recorded"], vm.SaveFeedback);
        Assert.Equal(f.Translator["Settlement_RecordedFollowUpFailed"], vm.Error);
        Assert.Equal(f.Translator["Common_Back"], vm.SaveText); Assert.Empty(f.Platform.Routes);
        vm.ActualText = "999"; await vm.SaveCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.Routes);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Equal(2, entries.Count);
        var posted = entries.Single(e => e.ScheduleId is null);
        Assert.Equal(kind, posted.Kind); Assert.Equal(amount, posted.Amount);
        Assert.False(vm.IsBusy);
    }

    [Theory]
    [InlineData("14")]
    [InlineData("4")]
    public async Task Failed_native_return_retries_only_navigation_and_cancel_never_discards_committed_money(string actual)
    {
        using var f = new FlowFixture(); var plan = await SeedAsync(f); var vm = Editor(f);
        await vm.LoadAsync(plan.Id); vm.ActualText = actual;
        f.Platform.PendingNavigation = () => throw new InvalidOperationException("Original native return failure");
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.HasCommitted); Assert.Equal(f.Translator["Editor_SavedReturnFailed"], vm.Error);
        Assert.False(vm.IsDirty); Assert.Equal(2, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
        f.Platform.PendingNavigation = null;
        await vm.SaveCommand.ExecuteAsync(null); await vm.CancelCommand.ExecuteAsync(null);
        Assert.Equal(3, f.Platform.Routes.Count); Assert.Empty(f.Platform.ConfirmationDialogs);
        Assert.Equal(2, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task Native_return_in_flight_keeps_one_execution_guard_for_both_actions()
    {
        using var f = new FlowFixture(); var plan = await SeedAsync(f); var vm = Editor(f);
        await vm.LoadAsync(plan.Id); vm.ActualText = "14";
        var returning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingNavigation = () => returning.Task;
        var first = vm.SaveCommand.ExecuteAsync(null);
        // SQLite may finish synchronously; await the actual accepted state before racing follow-through.
        for (var i = 0; i < 100 && !vm.HasCommitted; i++) await Task.Delay(10, Ct);
        Assert.True(vm.HasCommitted); Assert.True(vm.IsBusy); Assert.False(vm.CanEdit);
        await vm.SaveCommand.ExecuteAsync(null); await vm.CancelCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.Routes); Assert.Equal(2, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
        returning.SetResult(); await first; Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Changed_stored_advances_refuse_save_without_losing_the_draft()
    {
        using var f = new FlowFixture(); var plan = await SeedRejectedBillAsync(f); var vm = Editor(f);
        await vm.LoadAsync(plan.Id); vm.ActualText = "14"; Assert.True(vm.CanSave);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Entries.ExecuteUpdateAsync(rows => rows.SetProperty(e => e.Amount, 1100L), Ct);
        var changedSource = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.False(vm.HasCommitted); Assert.Equal("14", vm.ActualText); Assert.True(vm.IsDirty);
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.CanEdit); Assert.False(vm.CanSave);
        Assert.NotNull(vm.Error); Assert.Empty(f.Platform.Routes);
        Assert.Equal(changedSource, await GoalChooserFlowTests.SnapshotAsync(f));
        await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.Loading.IsReady); Assert.True(vm.CanEdit); Assert.True(vm.CanSave);
        Assert.Equal("14", vm.ActualText); Assert.True(vm.IsDirty); Assert.Null(vm.Error);
        Assert.Equal(changedSource, await GoalChooserFlowTests.SnapshotAsync(f));
        // Re-reviewing changed history is sufficient; never post a bill just to repeat successful money qualification.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unsaved_cancel_requires_the_original_discard_choice_and_never_posts(bool discard)
    {
        using var f = new FlowFixture(); var plan = await SeedAsync(f); var vm = Editor(f);
        await vm.LoadAsync(plan.Id); vm.ActualText = "14"; f.Platform.Confirmations.Enqueue(discard);
        await vm.CancelCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(discard ? 1 : 0, f.Platform.Routes.Count);
        Assert.Equal("14", vm.ActualText); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Invalid_period_and_amount_keep_both_field_errors_and_never_enter_the_writer()
    {
        using var f = new FlowFixture(); var plan = await SeedAsync(f); var vm = Editor(f);
        await vm.LoadAsync(plan.Id); vm.From = Day.AddDays(1); vm.To = Day; vm.ActualText = "-1";
        var refused = 0; vm.ValidationFailed += (_, _) => refused++;
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(1, refused); Assert.NotNull(vm.PeriodError); Assert.NotNull(vm.AmountError);
        Assert.False(vm.HasCommitted); Assert.Empty(f.Platform.Routes); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Rejected_SQL_insert_keeps_the_original_draft_for_one_explicit_successful_retry()
    {
        using var f = new FlowFixture(); var plan = await SeedRejectedBillAsync(f);
        var factory = new RejectedBillFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var vm = new SettlementViewModel(store, f.Translator, f.Localization, f.Time, f.Platform);
        await vm.LoadAsync(plan.Id); vm.ActualText = "14"; Assert.True(vm.CanSave);
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER refuse_bill BEFORE INSERT ON Entries WHEN NEW.ScheduleId IS NULL BEGIN SELECT RAISE(ABORT, 'original failure'); END;", Ct);
        var originalSource = await GoalChooserFlowTests.SnapshotAsync(f);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(factory.OriginalSqliteRefusal); Assert.Equal(1, factory.FailureCount);
        Assert.False(vm.HasCommitted); Assert.True(vm.Loading.HasFailed); Assert.False(vm.CanEdit); Assert.False(vm.CanSave);
        Assert.True(vm.IsDirty); Assert.Equal("14", vm.ActualText); Assert.NotNull(vm.Error); Assert.Empty(f.Platform.Routes);
        Assert.Equal(originalSource, await GoalChooserFlowTests.SnapshotAsync(f));
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER refuse_bill;", Ct);
        await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.Loading.IsReady); Assert.True(vm.CanEdit); Assert.True(vm.CanSave); Assert.Equal("14", vm.ActualText);
        Assert.Equal(originalSource, await GoalChooserFlowTests.SnapshotAsync(f));
        // This previously unexecuted post-refusal continuation needs an explicit source Retry before its one new acceptance.
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.HasCommitted); Assert.Single(f.Platform.Routes); Assert.Equal(1, factory.FailureCount);
        var entries = await f.Store.GetEntriesAsync(cancellationToken: Ct); Assert.Equal(2, entries.Count);
        var posted = Assert.Single(entries, e => e.ScheduleId is null);
        Assert.Equal(400, posted.Amount); Assert.Equal(EntryKind.Expense, posted.Kind); Assert.Equal(plan.AccountId, posted.AccountId);
        Assert.Equal(Day, posted.Date); Assert.Equal("Retain this note", Assert.Single(entries, e => e.ScheduleId is not null).Note);
        // This accepted fixture is terminal: no second Save/Undo/Reload.
    }

    /// <summary>Prepares only the two failed cases as raw fictitious input, without replaying successful setup commands.</summary>
    private static async Task<Schedule> SeedRejectedBillAsync(FlowFixture f)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        var account = new Account { Name = "Fictitious rejected bill", CurrencyCode = "EUR", Type = AccountType.Checking,
            OpeningDate = Day.AddDays(-8), OpeningBalance = 100000 };
        var plan = new Schedule { Name = "Fictitious retained utility bill", Kind = EntryKind.Expense, AccountId = account.Id,
            Amount = 1000, Rule = new() { Start = Day.AddDays(-7) } };
        db.Accounts.Add(account); db.Schedules.Add(plan);
        db.Entries.Add(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, ScheduleId = plan.Id,
            Date = Day.AddDays(-7), Amount = 1000, Title = "Original advance", Note = "Retain this note" });
        await db.SaveChangesAsync(Ct); return plan;
    }

    /// <summary>Observes the actual SQLite trigger refusal on original contexts; no read, writer or database is substituted.</summary>
    private sealed class RejectedBillFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal bool OriginalSqliteRefusal; internal int FailureCount;
        public ZananceDbContext CreateDbContext() => Observe(original.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default) => Observe(await original.CreateDbContextAsync(ct));
        private ZananceDbContext Observe(ZananceDbContext db)
        {
            db.SaveChangesFailed += (_, e) =>
            {
                FailureCount++;
                for (Exception? failure = e.Exception; failure is not null; failure = failure.InnerException)
                    if (failure is Microsoft.Data.Sqlite.SqliteException sql && sql.SqliteErrorCode == 19 && sql.Message.Contains("original failure", StringComparison.Ordinal))
                        OriginalSqliteRefusal = true;
            };
            return db;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Original_file_and_currency_cannot_be_replaced_at_the_bill_writer(bool currency)
    {
        using var f = new FlowFixture(); var plan = await SeedAsync(f);
        var draft = await f.Store.ReadAdvanceSettlementDraftAsync(plan.Id, Ct);
        var reviewed = AdvanceSettlement.Compute(plan, draft.Entries, Day.AddDays(-8), Day, 1400);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveAdvanceSettlementAsync(plan, Day.AddDays(-8), Day, reviewed, "reason", Ct,
            expectedPath: currency ? draft.DatabasePath : f.Directory.Combine("different.db"), expectedCurrency: currency ? "USD" : "EUR"));
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Draft_retirement_after_SQL_save_rolls_back_the_entire_extra_payment()
    {
        using var f = new FlowFixture(); var plan = await SeedAsync(f);
        var draft = await f.Store.ReadAdvanceSettlementDraftAsync(plan.Id, Ct);
        var reviewed = AdvanceSettlement.Compute(plan, draft.Entries, Day.AddDays(-8), Day, 1400); var checks = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveAdvanceSettlementAsync(plan, Day.AddDays(-8), Day, reviewed, "reason", Ct,
            expectedPath: draft.DatabasePath, expectedCurrency: "EUR", isCurrent: () => ++checks < 3));
        Assert.Equal(3, checks); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Failed_initial_read_remains_covered_and_retry_publishes_the_same_original_form()
    {
        using var f = new FlowFixture(); var plan = await SeedAsync(f);
        var factory = new Factory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()) { RefuseRead = true };
        var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var vm = new SettlementViewModel(store, f.Translator, f.Localization, f.Time, f.Platform);
        await vm.LoadAsync(plan.Id);
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.CanEdit); Assert.Null(vm.PlanName); Assert.NotNull(vm.Error);
        await vm.SaveCommand.ExecuteAsync(null); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        factory.RefuseRead = false; await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.Loading.IsReady); Assert.True(vm.CanEdit); Assert.Equal(plan.Name, vm.PlanName); Assert.Null(vm.Error);
    }

    [Fact]
    public async Task Accepted_form_cannot_reload_as_a_new_bill_or_retarget_another_financial_operation()
    {
        using var f = new FlowFixture(); var plan = await SeedAsync(f); var vm = Editor(f);
        await vm.LoadAsync(plan.Id); vm.ActualText = "14"; await vm.SaveCommand.ExecuteAsync(null);
        await vm.LoadAsync(Guid.NewGuid()); await vm.ReloadCommand.ExecuteAsync(null);
        Assert.True(vm.HasCommitted); Assert.False(vm.CanEdit); Assert.Equal(plan.Name, vm.PlanName);
        Assert.Equal(2, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }
}
