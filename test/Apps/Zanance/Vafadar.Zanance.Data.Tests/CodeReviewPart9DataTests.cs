using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Budgets;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Regression tests of the code review, part 9: plans and budget.</summary>
public sealed class CodeReviewPart9DataTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly ServiceProvider _services;
    private readonly ZananceStore _store;

    public CodeReviewPart9DataTests()
    {
        _services = new ServiceCollection().AddZananceData(_directory.Combine("zanance.db")).BuildServiceProvider();
        _services.MigrateLocalDatabase<ZananceDbContext>();
        _store = _services.GetRequiredService<ZananceStore>();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _services.Dispose();
        SqliteTestPools.Clear(_directory);
        _directory.Dispose();
    }

    [Fact]
    public async Task Copying_over_an_existing_budget_replaces_it_in_one_step()
    {
        // CR09: "Copy to next month" deleted the next month's budget and then saved the copy in a second step; a failing
        // second step lost the budget. Now the replacement is one transaction with one change notification.
        var october = new Budget { Year = 2026, Month = 10, Calendar = PeriodCalendar.Gregorian, CurrencyCode = "EUR", TotalLimit = 1_000_00 };
        var november = new Budget { Year = 2026, Month = 11, Calendar = PeriodCalendar.Gregorian, CurrencyCode = "EUR", TotalLimit = 500_00 };
        await _store.SaveBudgetAsync(october, Ct);
        await _store.SaveBudgetAsync(november, Ct);
        var changes = 0;
        _store.Changed += (_, _) => changes++;

        await _store.ReplaceBudgetAsync(november.Id, BudgetPlanning.CopyTo(october, 2026, 11), Ct);

        var replaced = await _store.GetBudgetAsync(2026, 11, PeriodCalendar.Gregorian, "EUR", Ct);
        Assert.NotNull(replaced);
        Assert.Equal(1_000_00, replaced.TotalLimit);
        Assert.NotEqual(november.Id, replaced.Id);
        Assert.Equal(2, (await _store.GetBudgetsAsync(Ct)).Count);
        Assert.Equal(1, changes);
    }
}
