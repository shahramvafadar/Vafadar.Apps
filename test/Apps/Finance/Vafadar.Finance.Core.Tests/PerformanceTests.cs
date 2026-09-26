using System.Diagnostics;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Forecasts;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;
using Vafadar.Finance.Core.Reports;

namespace Vafadar.Finance.Core.Tests;

/// <summary>
/// The calculations behind Home, Transactions, Reports and Forecast stay fast with 10,000 entries, 20 accounts and 100 plans (Q-02). Limits are
/// generous so slow CI machines do not fail; they catch accidental quadratic behaviour.
/// </summary>
public sealed class PerformanceTests
{
    private const int EntryCount = 10_000;
    private static readonly DateOnly Today = new(2027, 6, 15);
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(2);

    [Fact]
    public void Ten_thousand_entries_twenty_accounts_and_a_hundred_plans_are_calculated_quickly()
    {
        var ledger = new LedgerBuilder();
        var accounts = Enumerable.Range(0, 20).Select(i => ledger.Account($"Account {i}", 1_000, openingDate: new DateOnly(2024, 1, 1))).ToList();
        var categories = Enumerable.Range(0, 20).Select(_ => Guid.CreateVersion7()).ToList();
        var random = new Random(42);
        for (var i = 0; i < EntryCount; i++)
        {
            var kind = (i % 10) switch { 0 => EntryKind.Income, 1 => EntryKind.Refund, 2 => EntryKind.Transfer, _ => EntryKind.Expense };
            var date = new DateOnly(2024, 1, 1).AddDays(random.Next(0, 900));
            var entry = ledger.Add(kind, accounts[random.Next(accounts.Count)], random.Next(1, 500), date, categories[random.Next(categories.Count)]);
            if (kind == EntryKind.Transfer)
            {
                entry.CategoryId = null;
                entry.ToAccountId = accounts[(accounts.IndexOf(accounts.First(a => a.Id == entry.AccountId)) + 1) % accounts.Count].Id;
            }
        }

        var byId = ledger.Accounts.ToDictionary(a => a.Id);
        var month = new LedgerFilter(new DateOnly(2027, 6, 1), new DateOnly(2027, 6, 30));
        var plans = Enumerable.Range(0, 100).Select(i => new Schedule
        {
            Name = $"Plan {i}",
            AccountId = accounts[i % accounts.Count].Id,
            Amount = 1_000,
            Rule = new RecurrenceRule { Frequency = i % 2 == 0 ? Frequency.Monthly : Frequency.Weekly, Start = new DateOnly(2025, 1, 1 + (i % 28)) },
        }).ToList();

        Measure(() => LedgerCalculator.TotalBalances(ledger.Accounts, ledger.Entries, Today));
        Measure(() => LedgerCalculator.Totals(ledger.Accounts, ledger.Entries, month));
        Measure(() => LedgerCalculator.ExpenseByCategory(ledger.Accounts, ledger.Entries, month, id => id));
        Measure(() => EntrySearch.ByDay(EntrySearch.Apply(ledger.Entries, new EntryFilter(Text: "12.5"), _ => "Food", byId), byId));
        Measure(() => ReportCalculator.MonthlyTrend(ledger.Accounts, ledger.Entries, Today, 12, PeriodCalendar.Gregorian, "EUR"));
        Measure(() => ReportCalculator.AccountMovements(ledger.Accounts, ledger.Entries, new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31)));
        Measure(() => ForecastCalculator.Compute(ledger.Accounts, ledger.Entries, plans, [], Today, Today.AddDays(90)));
    }

    private static void Measure(Func<object> calculation)
    {
        var watch = Stopwatch.StartNew();
        Assert.NotNull(calculation());
        watch.Stop();
        Assert.True(watch.Elapsed < Limit, $"Took {watch.Elapsed.TotalMilliseconds:0} ms.");
    }
}
