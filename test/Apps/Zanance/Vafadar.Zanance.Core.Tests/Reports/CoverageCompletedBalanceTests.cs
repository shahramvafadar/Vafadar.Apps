using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>Essential coverage uses the completed usable-money net, independently of account enumeration order.</summary>
public sealed class CoverageCompletedBalanceTests
{
    private static readonly DateOnly Today = new(2026, 11, 6);
    private static readonly int[][] Orders = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];

    /// <summary>Offsetting independently supported account balances cannot reject a supported completed coverage net.</summary>
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(false, 4)]
    [InlineData(false, 5)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [InlineData(true, 4)]
    [InlineData(true, 5)]
    public void Completed_coverage_is_exact_for_each_account_order(bool negative, int permutation)
    {
        long[] balances = negative ? [long.MinValue, -1, 1] : [long.MaxValue, 1, -1];
        var source = balances.Select(Payment).ToArray();
        var ordered = Orders[permutation].Select(i => source[i]).ToArray();
        var result = Coverage(ordered);
        Assert.Equal(negative ? long.MinValue : long.MaxValue, result.UsableMoney);
        Assert.Equal(1, result.MonthlyEssential);
        Assert.Equal(negative ? CoverageStatus.BalanceNegative : CoverageStatus.Ok, result.Status);
        Assert.Equal(negative ? 0m : (decimal)long.MaxValue, result.Months);
        Assert.Equal(balances, source.Select(a => a.OpeningBalance).ToArray());
        Assert.Equal(Orders[permutation].Select(i => source[i].Id), ordered.Select(a => a.Id));
    }

    /// <summary>An actually unsupported completed net still refuses before publishing a plausible coverage ratio.</summary>
    [Theory]
    [InlineData(long.MaxValue, 1)]
    [InlineData(long.MinValue, -1)]
    public void Unsupported_completed_coverage_still_refuses(long first, long second) =>
        Assert.Throws<OverflowException>(() => Coverage([Payment(first), Payment(second)]));

    /// <summary>The wider accumulator retains archive, payment eligibility and case-insensitive currency selection.</summary>
    [Fact]
    public void Only_original_usable_currency_accounts_contribute()
    {
        var selected = Payment(100); selected.CurrencyCode = "eur";
        var archived = Payment(long.MaxValue); archived.IsArchived = true;
        var reserved = Payment(long.MaxValue); reserved.UsableForPayments = false;
        var foreign = Payment(long.MaxValue); foreign.CurrencyCode = "USD";
        var result = Coverage([selected, archived, reserved, foreign]);
        Assert.Equal(100, result.UsableMoney); Assert.Equal(1, result.MonthlyEssential);
        Assert.Equal(100m, result.Months); Assert.Equal(CoverageStatus.Ok, result.Status);
    }

    private static Account Payment(long amount) => new()
    {
        Name = "Coverage boundary account", CurrencyCode = "EUR", Type = AccountType.Checking,
        OpeningBalance = amount, OpeningDate = new DateOnly(2026, 1, 1), IncludeInTotals = true, UsableForPayments = true,
    };

    private static EssentialCoverage Coverage(IReadOnlyCollection<Account> accounts)
    {
        var category = new Category { Name = "Essential coverage boundary", Kind = CategoryKind.Expense, IsEssential = true };
        var plan = new Schedule
        {
            Name = "Two-month essential cost", AccountId = accounts.First().Id, CategoryId = category.Id,
            Kind = EntryKind.Expense, Amount = 2,
            Rule = new RecurrenceRule { Start = Today, Frequency = Frequency.Monthly, Interval = 2 },
        };
        return KpiCatalog.Coverage(accounts, [], [category], [plan], "EUR", Today, PeriodCalendar.Gregorian);
    }
}
