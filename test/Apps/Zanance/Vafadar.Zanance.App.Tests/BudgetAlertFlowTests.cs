using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Reminders;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-152: real alert orchestration, complete original SQLite calculations and explicit native publication effects.</summary>
[Trait("AT", "AT-152")]
public sealed class BudgetAlertFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid ScopeId = Guid.Parse("90909090-3434-5656-7878-121212121212");
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal bool Fail;
        public CommercialWriteAccess Capture(string databasePath) => Fail ? throw new InvalidOperationException("Owned read failed.") : Current;
    }
    private sealed class NativeScheduler : IReminderScheduler
    {
        public event EventHandler<string>? Tapped { add { } remove { } }
        public event EventHandler<SnoozeRequest>? SnoozeRequested { add { } remove { } }
        public bool IsSupported => true;
        internal bool Enabled = true;
        internal bool Accept = true;
        internal readonly List<ReminderNotification> Shown = [];
        internal readonly List<int> Removed = [];
        internal Func<Task>? OnShow;
        public void SetSnoozeActions(string oneHour, string tomorrow) { }
        public Task<bool> AreEnabledAsync() => Task.FromResult(Enabled);
        public Task<bool> RequestPermissionAsync() => Task.FromResult(Enabled);
        public Task ReplaceAllAsync(IReadOnlyList<ReminderNotification> reminders) => Task.CompletedTask;
        public async Task ShowAsync(ReminderNotification notification)
        {
            Shown.Add(notification); if (OnShow is not null) await OnShow();
            await ReminderPublication.RequireAcceptedAsync(Task.FromResult(Accept));
        }
        public Task RemoveAsync(int notificationId) { Removed.Add(notificationId); return Task.CompletedTask; }
    }
    private sealed record Review(AccessSource Source, ZananceStore Store, BudgetChoiceStore Choices, NativeScheduler Native,
        ReminderService Service, Budget[] Budgets, Account[] Accounts);
    private static async Task<Review> SeedAsync(FlowFixture f)
    {
        var category = new Category { Name = "Original flexible category" }; await f.Store.SaveCategoryAsync(category, Ct);
        var accounts = new[] { "EUR", "USD", "GBP" }.Select(currency => new Account
            { Name = "Original " + currency, CurrencyCode = currency, OpeningDate = new(2026, 1, 1) }).ToArray();
        foreach (var account in accounts)
        {
            await f.Store.SaveAccountAsync(account, Ct);
            await f.Store.SaveEntryAsync(new() { AccountId = account.Id, CategoryId = category.Id, Amount = 9000, Date = new(2026, 10, 8), Kind = EntryKind.Expense }, Ct);
            await f.Store.SaveEntryAsync(new() { AccountId = account.Id, Amount = 500, Date = new(2026, 10, 9), Kind = EntryKind.Refund }, Ct);
            await f.Store.SaveEntryAsync(new() { AccountId = account.Id, ToAccountId = account.Id, Amount = 20000,
                Date = new(2026, 10, 9), Kind = EntryKind.Transfer }, Ct);
        }
        var (year, month) = PeriodMath.MonthOf(new(2026, 10, 9), PeriodCalendar.Persian, 1);
        var budgets = new[] { new Budget { CurrencyCode = "EUR", Year = 2026, Month = 10, TotalLimit = 10000 },
            new Budget { CurrencyCode = "USD", Year = 2026, Month = 10, TotalLimit = 10000 },
            new Budget { CurrencyCode = "GBP", Year = 2026, Month = 10, TotalLimit = 10000, Method = BudgetMethod.Flex },
            new Budget { CurrencyCode = "EUR", Period = BudgetPeriod.Week, PeriodStart = new(2026, 10, 5), TotalLimit = 10000 },
            new Budget { CurrencyCode = "EUR", Calendar = PeriodCalendar.Persian, Year = year, Month = month, TotalLimit = 10000 },
            new Budget { CurrencyCode = "USD", Year = 2026, Month = 11, TotalLimit = 1 },
            new Budget { CurrencyCode = "USD", Year = 2026, Month = 9, TotalLimit = 1 } };
        foreach (var budget in budgets) await f.Store.SaveBudgetAsync(budget, Ct);
        await f.Store.GetSettingsAsync(Ct);
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var source = new AccessSource();
        var store = new ZananceStore(factory, f.Time, source); var choices = new BudgetChoiceStore(factory, source, f.Time); var native = new NativeScheduler();
        return new(source, store, choices, native, new(native, store, f.Services.GetRequiredService<PlanStore>(), f.Services.GetRequiredService<GoalStore>(),
            f.Services.GetRequiredService<HoldingStore>(), f.Preferences, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time,
            budgetChoices: choices), budgets, accounts);
    }
    private static void SetAccess(FlowFixture f, Review r, string mode)
    {
        if (mode == "inactive") { r.Source.Current = CommercialWriteAccess.Inactive; return; }
        var scope = new QuotaScope(QuotaScopeKind.PersonalProfile, ScopeId);
        r.Source.Current = new(f.Directory.Combine("zanance.db"), new(mode == "plus" ? ProductPlan.Plus : ProductPlan.Free,
            new(EntitlementScopeKind.PersonalProfile, ScopeId)), budgetSelection: mode == "missing" || mode == "plus" ? null : new(scope,
                mode == "empty" ? [] : [new(scope, r.Budgets[mode == "paid" ? 2 : 0].CurrencyCode, BudgetPeriod.Month, [])]));
    }
    private static async Task SqlAsync(FlowFixture f, string sql)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }
    private static string Key(Budget budget) => "budget.alert." + budget.Id.ToString("N");

    [Theory]
    [InlineData("inactive", 5), InlineData("plus", 5), InlineData("missing", 0), InlineData("one", 2), InlineData("empty", 0), InlineData("paid", 0)]
    public async Task Actual_alert_delivery_checks_all_original_calendars_and_scopes_but_only_eligible_current_definitions(string access, int expected)
    {
        using var f = new FlowFixture(); var r = await SeedAsync(f); SetAccess(f, r, access); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var snapshot = await r.Store.GetBudgetAlertSnapshotAsync(Ct); Assert.Equal(expected, snapshot.Candidates.Count);
        Assert.All(snapshot.Candidates, candidate => { Assert.Equal(10000, candidate.Status.Limit); Assert.Equal(8500, candidate.Status.Spent); });
        await r.Service.RefreshAsync(); Assert.Equal(expected, r.Native.Shown.Count); Assert.Empty(r.Native.Removed);
        Assert.Equal(snapshot.Candidates.Select(candidate => candidate.BudgetId).Order(), r.Native.Shown
            .Select(notification => BudgetReminderLink.Parse(notification.Link)!.BudgetId).Order());
        Assert.All(r.Native.Shown, notification => Assert.Equal(snapshot.ReminderScope, BudgetReminderLink.Parse(notification.Link)!.Scope));
        Assert.All(r.Native.Shown, notification => { Assert.Equal(f.Translator["Reminder_BudgetGeneric"], notification.Body); Assert.Null(notification.NotifyAt); Assert.False(notification.CanSnooze); });
        await r.Service.RefreshAsync(); Assert.Equal(expected, r.Native.Shown.Count);
        Assert.Null(f.Preferences.Get(Key(r.Budgets[5]))); Assert.Null(f.Preferences.Get(Key(r.Budgets[6])));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData("en"), InlineData("fa"), InlineData("de")]
    public async Task Explicit_details_use_original_currency_and_translations_without_touching_financial_data(string language)
    {
        using var f = new FlowFixture(); var r = await SeedAsync(f); SetAccess(f, r, "one");
        var settings = await f.Store.GetSettingsAsync(Ct); settings.NotificationsShowDetails = true; await f.Store.SaveSettingsAsync(settings, Ct);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await r.Service.RefreshAsync(); Assert.Equal(2, r.Native.Shown.Count);
        Assert.All(r.Native.Shown, notification => Assert.Equal(f.Translator.Format("Home_BudgetLeft",
            MoneyText.Format(1500, "EUR", f.Localization.CurrentCulture), MoneyText.Format(10000, "EUR", f.Localization.CurrentCulture)), notification.Body));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Theory]
    [InlineData("amount"), InlineData("opening"), InlineData("category"), InlineData("privacy"), InlineData("limits"), InlineData("date"), InlineData("rights"), InlineData("activation")]
    public async Task Inflight_changes_retract_exact_published_alert_and_never_consume_threshold_cache(string change)
    {
        using var f = new FlowFixture(); var r = await SeedAsync(f); if (change != "activation") SetAccess(f, r, "one");
        r.Native.OnShow = async () =>
        {
            r.Native.OnShow = null;
            switch (change)
            {
                case "amount": await SqlAsync(f, "UPDATE Entries SET Amount=Amount+1 WHERE Kind=1"); break;
                case "opening": await SqlAsync(f, "UPDATE Accounts SET OpeningDate='2026-10-09'"); break;
                case "category": await SqlAsync(f, "UPDATE Categories SET SpendingType=1"); break;
                case "privacy": await SqlAsync(f, "UPDATE Settings SET NotificationsShowDetails=1"); break;
                case "limits": await SqlAsync(f, "UPDATE Budgets SET TotalLimit=12345"); break;
                case "date": f.Time.Now = f.Time.Now.AddDays(1); break;
                default: SetAccess(f, r, "empty"); break;
            }
        };
        await r.Service.RefreshAsync(); var shown = Assert.Single(r.Native.Shown); Assert.Equal([shown.Id], r.Native.Removed);
        Assert.All(r.Budgets, budget => Assert.Null(f.Preferences.Get(Key(budget))));
    }

    [Fact]
    public async Task Native_partial_failure_retracts_and_can_retry_without_consuming_any_batch_level()
    {
        using var f = new FlowFixture(); var r = await SeedAsync(f); r.Native.OnShow = () => throw new InvalidOperationException("Owned publication failure.");
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await r.Service.RefreshAsync(); Assert.Single(r.Native.Shown); Assert.Single(r.Native.Removed);
        Assert.All(r.Budgets, budget => Assert.Null(f.Preferences.Get(Key(budget))));
        r.Native.OnShow = null; await r.Service.RefreshAsync(); Assert.Equal(6, r.Native.Shown.Count); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_later_batch_failure_retracts_every_new_alert_and_keeps_all_levels_retryable()
    {
        using var f = new FlowFixture(); var r = await SeedAsync(f);
        r.Native.OnShow = () => r.Native.Shown.Count == 2 ? throw new InvalidOperationException("Owned second publication failure.") : Task.CompletedTask;
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await r.Service.RefreshAsync(); Assert.Equal(2, r.Native.Shown.Count);
        Assert.Equal(r.Native.Shown.Select(notification => notification.Id), r.Native.Removed);
        Assert.All(r.Budgets, budget => Assert.Null(f.Preferences.Get(Key(budget)))); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Native_false_result_cannot_consume_levels_and_a_later_accepted_request_retries_the_actual_batch()
    {
        using var f = new FlowFixture(); var r = await SeedAsync(f); r.Native.Accept = false;
        var before = await GoalChooserFlowTests.SnapshotAsync(f); await r.Service.RefreshAsync(); Assert.Single(r.Native.Shown); Assert.Single(r.Native.Removed);
        Assert.All(r.Budgets, budget => Assert.Null(f.Preferences.Get(Key(budget))));
        r.Native.Accept = true; await r.Service.RefreshAsync(); Assert.Equal(6, r.Native.Shown.Count);
        Assert.Equal("1", f.Preferences.Get(Key(r.Budgets[0]))); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task A_committed_choice_during_publication_retracts_the_old_alert_and_next_refresh_uses_new_definitions()
    {
        using var f = new FlowFixture(); var r = await SeedAsync(f); SetAccess(f, r, "one"); var before = await GoalChooserFlowTests.SnapshotAsync(f, false);
        r.Native.OnShow = async () =>
        {
            r.Native.OnShow = null; var choice = await r.Choices.ReadAsync(Ct);
            await r.Choices.SaveGroupsAsync(choice, [choice.Groups.Single(group => group.CurrencyCode == "USD").RepresentativeId], Ct);
        };
        await r.Service.RefreshAsync(); Assert.Single(r.Native.Shown); Assert.Single(r.Native.Removed);
        Assert.Null(f.Preferences.Get(Key(r.Budgets[0]))); await r.Service.RefreshAsync(); Assert.Equal(2, r.Native.Shown.Count);
        Assert.Equal("1", f.Preferences.Get(Key(r.Budgets[1]))); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f, false));
    }

    [Fact]
    public async Task Denied_notification_permission_does_not_read_financial_data_or_change_alert_caches()
    {
        using var f = new FlowFixture(); var r = await SeedAsync(f); var before = await GoalChooserFlowTests.SnapshotAsync(f);
        r.Native.Enabled = false; r.Source.Fail = true; await r.Service.RefreshAsync();
        Assert.Empty(r.Native.Shown); Assert.Empty(r.Native.Removed); Assert.All(r.Budgets, budget => Assert.Null(f.Preferences.Get(Key(budget))));
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Refund_resets_level_only_after_valid_read_and_later_spending_realerts()
    {
        using var f = new FlowFixture(); var r = await SeedAsync(f); SetAccess(f, r, "one"); await r.Service.RefreshAsync();
        await f.Store.SaveEntryAsync(new() { AccountId = r.Accounts[0].Id, Kind = EntryKind.Refund, Amount = 3000, Date = new(2026, 10, 9) }, Ct);
        await r.Service.RefreshAsync(); Assert.Equal(2, r.Native.Shown.Count); Assert.Equal("0", f.Preferences.Get(Key(r.Budgets[0])));
        await f.Store.SaveEntryAsync(new() { AccountId = r.Accounts[0].Id, Kind = EntryKind.Expense, Amount = 6000, Date = new(2026, 10, 9) }, Ct);
        await r.Service.RefreshAsync(); Assert.Equal(4, r.Native.Shown.Count); Assert.Equal("2", f.Preferences.Get(Key(r.Budgets[0])));
    }

    [Fact]
    public async Task Retained_carry_requires_exact_account_definition_and_uses_all_original_historical_spending()
    {
        using var f = new FlowFixture(); var r = await SeedAsync(f);
        var historical = new Budget { CurrencyCode = "EUR", Year = 2026, Month = 9, TotalLimit = 10000, AccountIds = [r.Accounts[0].Id] };
        await f.Store.SaveBudgetAsync(historical, Ct); r.Budgets[0].Rollover = BudgetRollover.Surplus; await f.Store.SaveBudgetAsync(r.Budgets[0], Ct);
        Assert.Equal(0, (await f.Store.GetBudgetCarryAsync(r.Budgets[0], Ct)).Total);
        r.Budgets[0].AccountIds = [r.Accounts[0].Id]; await f.Store.SaveBudgetAsync(r.Budgets[0], Ct);
        await f.Store.SaveEntryAsync(new() { AccountId = r.Accounts[0].Id, Kind = EntryKind.Expense, Amount = 2000, Date = new(2026, 9, 8) }, Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var snapshot = await r.Store.GetBudgetAlertSnapshotAsync(Ct);
        var candidate = snapshot.Candidates.Single(item => item.BudgetId == r.Budgets[0].Id);
        Assert.Equal(18000, candidate.Status.Limit); Assert.Equal(8500, candidate.Status.Spent);
        Assert.Equal(8000, (await f.Store.GetBudgetCarryAsync(r.Budgets[0], Ct)).Total);
        await SqlAsync(f, "UPDATE Entries SET Amount=3000 WHERE Date='2026-09-08'");
        await Assert.ThrowsAsync<InvalidOperationException>(() => r.Store.ValidateBudgetAlertSnapshotAsync(snapshot, Ct));
        Assert.NotEqual(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    [Fact]
    public async Task Reader_rejects_foreign_file_and_explicit_choice_ABA_without_any_financial_write()
    {
        using var f = new FlowFixture(); var r = await SeedAsync(f); SetAccess(f, r, "one"); var snapshot = await r.Store.GetBudgetAlertSnapshotAsync(Ct);
        using var other = new FlowFixture(); await other.Store.GetSettingsAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => other.Store.ValidateBudgetAlertSnapshotAsync(snapshot, Ct));
        var choice = await r.Choices.ReadAsync(Ct); await r.Choices.SaveGroupsAsync(choice, [choice.Groups.Single(group => group.CurrencyCode == "EUR" && group.Period == BudgetPeriod.Month).RepresentativeId], Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => r.Store.ValidateBudgetAlertSnapshotAsync(snapshot, Ct));
        var storedReview = await r.Store.GetBudgetAlertSnapshotAsync(Ct);
        choice = await r.Choices.ReadAsync(Ct); await r.Choices.SaveGroupsAsync(choice, [choice.Groups.Single(group => group.CurrencyCode == "USD").RepresentativeId], Ct);
        choice = await r.Choices.ReadAsync(Ct); await r.Choices.SaveGroupsAsync(choice, [choice.Groups.Single(group => group.CurrencyCode == "EUR" && group.Period == BudgetPeriod.Month).RepresentativeId], Ct);
        // The original selected definition is back, but its durable review revision must not revive an older batch.
        await Assert.ThrowsAsync<InvalidOperationException>(() => r.Store.ValidateBudgetAlertSnapshotAsync(storedReview, Ct));
        var before = await GoalChooserFlowTests.SnapshotAsync(f); r.Source.Fail = true; await r.Service.RefreshAsync(); Assert.Empty(r.Native.Shown);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
