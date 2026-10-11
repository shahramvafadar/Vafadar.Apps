using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>
/// ZEX-S0503, AT28 (05 §5): with a weekly budget, a monthly budget with rollover, a custom plan rule, tagged entries, a
/// balance goal pinned to Home and a 20 g holding, switching Advanced → Simple → Advanced changes no number of Home,
/// Budget, Goals, Accounts and reports, and no setting other than the mode.
/// </summary>
public sealed class ZexModeSwitchTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 10, 14);
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };
    private readonly TemporaryDirectory _directory = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        SqliteTestPools.Clear(_directory);
        _directory.Dispose();
    }

    [Fact]
    public async Task AT28_switching_the_mode_and_back_changes_no_number_and_no_setting()
    {
        await using var services = Install();
        var store = services.GetRequiredService<ZananceStore>();
        await SeedAsync(services);
        var settings = await store.GetSettingsAsync(Ct);
        settings.Mode = ExperienceMode.Advanced;
        await store.SaveSettingsAsync(settings, Ct);

        var advanced = await NumbersAsync(services);
        var settingsBefore = SettingsWithoutMode(await store.GetSettingsAsync(Ct));

        await SwitchAsync(store, ExperienceMode.Simple);
        var simple = await NumbersAsync(services);
        await SwitchAsync(store, ExperienceMode.Advanced);
        var back = await NumbersAsync(services);

        Assert.Equal(advanced, simple);
        Assert.Equal(advanced, back);
        Assert.Equal(settingsBefore, SettingsWithoutMode(await store.GetSettingsAsync(Ct)));

        // Each item of the data set stays visible in Simple, at least as a summary (05 §5).
        var simpleSettings = new ZananceSettings { Mode = ExperienceMode.Simple };
        Assert.True(simpleSettings.ShowsExisting(Feature.BudgetPeriods, hasData: true));
        Assert.True(simpleSettings.ShowsExisting(Feature.BudgetOptions, hasData: true));
        Assert.True(simpleSettings.ShowsExisting(Feature.PlanRules, hasData: true));
        Assert.True(simpleSettings.Shows(Feature.HoldingsList));
    }

    // The mode is switched the way the Settings page does it: only the mode column changes.
    private static async Task SwitchAsync(ZananceStore store, ExperienceMode mode)
    {
        var settings = await store.GetSettingsAsync(Ct);
        settings.Mode = mode;
        await store.SaveSettingsAsync(settings, Ct);
    }

    private static string SettingsWithoutMode(ZananceSettings settings)
    {
        var values = typeof(ZananceSettings).GetProperties()
            .Where(p => p.Name is not (nameof(ZananceSettings.Mode) or nameof(ZananceSettings.UpdatedAt)))
            .ToDictionary(p => p.Name, p => p.GetValue(settings)?.ToString());
        return JsonSerializer.Serialize(values, Json);
    }

    // The numbers Home, Budget, Goals, Accounts and the reports show, read from the database as the pages do.
    private static async Task<string> NumbersAsync(ServiceProvider services)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        var goals = services.GetRequiredService<GoalStore>();
        var holdings = services.GetRequiredService<HoldingStore>();
        var accounts = await store.GetAccountsAsync(cancellationToken: Ct);
        var entries = await store.GetEntriesAsync(cancellationToken: Ct);
        var schedules = await plans.GetSchedulesAsync(Ct);
        var states = await plans.GetStatesAsync(cancellationToken: Ct);
        var goalList = await goals.GetGoalsAsync(Ct);
        var contributions = await goals.GetContributionPlansAsync(Ct);
        var allocations = new List<GoalAllocation>();
        foreach (var goal in goalList)
        {
            allocations.AddRange(await goals.GetAllocationsAsync(goal.Id, Ct));
        }

        var types = await holdings.GetTypesAsync(Ct);
        var events = await holdings.GetEventsAsync(cancellationToken: Ct);
        var valuations = await holdings.GetValuationsAsync(cancellationToken: Ct);
        var budgets = await store.GetBudgetsAsync(Ct);
        var month = new LedgerFilter(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        var numbers = new
        {
            Balances = accounts.Select(a => LedgerCalculator.Balance(a, entries, Today)).ToList(),
            Budgets = budgets.Select(b => BudgetCalculator.NetExpense(accounts, entries, BudgetPeriods.Range(b, 1).First, BudgetPeriods.Range(b, 1).Last, b.CurrencyCode, b.AccountIds)).ToList(),
            Goals = GoalProgressService.Evaluate(goalList, allocations, accounts, entries, contributions, Today, events, types).Select(p => p.ToString()).ToList(),
            Surplus = KpiCatalog.Surplus(accounts, entries, month).Select(s => s.ToString()).ToList(),
            NetWorth = NetWorthCalculator.Compute(accounts, entries, types, events, valuations, Today).ToString(),
            Forecast = ForecastCalculator.Compute(accounts, entries, schedules, states, Today, Today.AddDays(60)).Select(f => (f.CurrencyCode, f.Minimum, f.EndBalance)).ToList(),
            Plan = schedules.Select(s => string.Join(",", Recurrence.Next(s.Rule, Today, 4).Select(o => o.Date))).ToList(),
            Tagged = entries.Count(e => e.Tags.Contains("holiday")),
            Grams = HoldingsLedger.Quantity(events, types.Single().Id, Today),
        };
        return JsonSerializer.Serialize(numbers, Json);
    }

    private static async Task SeedAsync(ServiceProvider services)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var cash = new Account { Name = "Checking", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningBalance = 3_000_00, OpeningDate = new DateOnly(2026, 1, 1) };
        await store.SaveAccountAsync(cash, Ct);
        await store.EnsureDefaultCategoriesAsync(Ct);
        var food = (await store.GetCategoriesAsync(Ct)).First(c => c.Kind == Core.Categories.CategoryKind.Expense);
        foreach (var (day, amount, tag) in new[] { (3, 40_00, "holiday"), (9, 25_00, "holiday"), (12, 60_00, "car") })
        {
            await store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = cash.Id, Amount = amount, Date = new DateOnly(2026, 10, day), CategoryId = food.Id, Tags = [tag] }, Ct);
        }

        // A weekly budget and a monthly budget with rollover and envelopes.
        await store.SaveBudgetAsync(new Budget { Period = BudgetPeriod.Week, PeriodStart = new DateOnly(2026, 10, 12), CurrencyCode = "EUR", TotalLimit = 100_00 }, Ct);
        await store.SaveBudgetAsync(new Budget { Year = 2026, Month = 10, CurrencyCode = "EUR", TotalLimit = 500_00, Rollover = BudgetRollover.Surplus, Method = BudgetMethod.Envelopes }, Ct);

        // A plan with a custom rule: every second month on the last day.
        await services.GetRequiredService<PlanStore>().SaveScheduleAsync(new Schedule
        {
            Name = "Insurance",
            AccountId = cash.Id,
            Amount = 80_00,
            Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Interval = 2, DayRule = MonthDayRule.LastDayOfMonth, Start = new DateOnly(2026, 10, 31) },
        }, Ct);

        // A balance goal pinned to Home, and 20 g of gold.
        await services.GetRequiredService<GoalStore>().SaveGoalAsync(new Goal { Name = "Cushion", CurrencyCode = "EUR", Type = GoalType.AccountBalance, AccountId = cash.Id, TargetAmount = 5_000_00, HomePin = 1 }, Ct);
        var holdings = services.GetRequiredService<HoldingStore>();
        var gold = new AssetType { Name = "Gold", PriceCurrencyCode = "EUR" };
        await holdings.SaveTypeAsync(gold, Ct);
        var place = await holdings.EnsureDefaultLocationAsync("Safe", Ct);
        await holdings.SaveEventAsync(new AssetEvent { AssetTypeId = gold.Id, LocationId = place.Id, Kind = AssetEventKind.Opening, Quantity = 20_000, Date = new DateOnly(2026, 1, 1) }, [], Ct);
        await holdings.SaveValuationAsync(new AssetValuation { AssetTypeId = gold.Id, CurrencyCode = "EUR", Date = Today, PricePerUnitMilli = 100_000_000 }, Ct);
    }

    private ServiceProvider Install()
    {
        var services = new ServiceCollection()
            .AddZananceData(_directory.Combine("zanance.db"))
            .AddSingleton<IAppEnvironment>(new StaticAppEnvironment("pro.vafadar.zanance", "Zanance", new Version(1, 0, 0), "device", "Tests"))
            .AddSingleton<ISettingsStore, InMemorySettingsStore>()
            .BuildServiceProvider();
        services.MigrateLocalDatabase<ZananceDbContext>();
        return services;
    }
}
