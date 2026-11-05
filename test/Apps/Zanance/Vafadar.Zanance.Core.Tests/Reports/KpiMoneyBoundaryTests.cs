using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Reports;

/// <summary>KPI totals and even medians must preserve supported money without wrapped signs.</summary>
public sealed class KpiMoneyBoundaryTests
{
    private static readonly DateOnly Today = new(2026, 11, 5);

    /// <summary>Valid nonnegative gross components may still produce an unsupported signed surplus.</summary>
    [Theory]
    [InlineData(long.MaxValue, 0, 0, 1)]
    [InlineData(0, long.MaxValue, 2, 0)]
    public void An_unrepresentable_surplus_is_refused(long income, long reversals, long expense, long refunds) =>
        Assert.Throws<OverflowException>(() => Surplus(income, reversals, expense, refunds).Surplus);

    /// <summary>A wrapped surplus must not become a plausible but reversed percentage.</summary>
    [Fact]
    public void An_unrepresentable_surplus_rate_is_refused() =>
        Assert.Throws<OverflowException>(() => Surplus(long.MaxValue, 0, 0, 1).RatePercent);

    /// <summary>No eligible income retains the original unknown-rate policy without evaluating a deficit.</summary>
    [Fact]
    public void No_income_still_has_no_surplus_rate() =>
        Assert.Null(Surplus(0, long.MaxValue, 2, 0).RatePercent);

    /// <summary>Ordinary refund/reversal classification and half-away rate rounding stay unchanged.</summary>
    [Fact]
    public void Representable_surplus_and_rate_remain_exact()
    {
        var result = Surplus(10, 3, 8, 4);
        Assert.Equal(7, result.EligibleIncome); Assert.Equal(4, result.Consumption); Assert.Equal(3, result.Surplus); Assert.Equal(42.9m, result.RatePercent);
    }

    /// <summary>Signed net spending across periods may differ by more than one stored money value.</summary>
    [Theory]
    [InlineData(long.MaxValue, -1)]
    [InlineData(-long.MaxValue, 2)]
    public void An_unrepresentable_spending_change_is_refused(long current, long comparison) =>
        Assert.Throws<OverflowException>(() => new SpendingChange(null, current, comparison).Delta);

    /// <summary>The positive comparison branch must not hide an unsupported delta behind percentage rounding.</summary>
    [Fact]
    public void An_unrepresentable_change_percentage_is_refused() =>
        Assert.Throws<OverflowException>(() => new SpendingChange(null, -long.MaxValue, 2).DeltaPercent);

    /// <summary>A nonpositive comparison still has no percentage, even when its unused delta is unsupported.</summary>
    [Fact]
    public void A_nonpositive_comparison_remains_unknown() =>
        Assert.Null(new SpendingChange(null, long.MaxValue, -1).DeltaPercent);

    /// <summary>The midpoint of supported endpoints remains supported, including half-away ties.</summary>
    [Theory]
    [InlineData(long.MaxValue, long.MaxValue, long.MaxValue)]
    [InlineData(long.MinValue, long.MinValue, long.MinValue)]
    [InlineData(long.MaxValue - 1, long.MaxValue, long.MaxValue)]
    [InlineData(long.MinValue, long.MinValue + 1, long.MinValue)]
    public void An_even_median_does_not_wrap_before_averaging(long first, long second, long expected) =>
        Assert.Equal(expected, KpiCatalog.Median([first, second]));

    /// <summary>The original ordering is not changed and an exact negative half still rounds away from zero.</summary>
    [Fact]
    public void Median_keeps_input_ownership_and_the_original_half_policy()
    {
        long[] source = [long.MaxValue, long.MinValue];
        Assert.Equal(-1, KpiCatalog.Median(source)); Assert.Equal(long.MaxValue, source[0]); Assert.Equal(long.MinValue, source[1]);
    }

    /// <summary>Known essential spending plus a supported plan share cannot wrap into "not available".</summary>
    [Fact]
    public void An_unrepresentable_essential_month_is_refused() =>
        Assert.Throws<OverflowException>(() => Coverage(2));

    /// <summary>The exact supported essential-month limit retains its original available status.</summary>
    [Fact]
    public void An_exact_essential_month_limit_is_supported()
    {
        var result = Coverage(1); Assert.Equal(long.MaxValue, result.MonthlyEssential); Assert.Equal(CoverageStatus.Ok, result.Status); Assert.Equal(0m, result.Months);
    }

    /// <summary>Individually supported installments of distinct loans cannot wrap their same-currency total.</summary>
    [Fact]
    public void An_unrepresentable_combined_installment_total_is_refused()
    {
        var source = Payment("EUR"); var first = Loan("EUR", long.MaxValue); var second = Loan("EUR", 1);
        Assert.Throws<OverflowException>(() => KpiCatalog.Debt([source, first, second], [], [Installment(source, first, long.MaxValue), Installment(source, second, 1)], [], Today, Today, Today));
    }

    /// <summary>Separate currencies keep independently supported principal and unknown income ratios.</summary>
    [Fact]
    public void Installments_preserve_currency_partition_and_the_original_principal_split()
    {
        var eur = Payment("EUR"); var gbp = Payment("GBP"); var first = Loan("EUR", long.MaxValue); var second = Loan("GBP", long.MaxValue);
        var result = KpiCatalog.Debt([eur, gbp, first, second], [], [Installment(eur, first, long.MaxValue), Installment(gbp, second, long.MaxValue)], [], Today, Today, Today);
        foreach (var code in new[] { "EUR", "GBP" })
        {
            var row = Assert.Single(result, r => r.CurrencyCode == code); Assert.Equal(long.MaxValue, row.Installments); Assert.Equal(long.MaxValue, row.Principal); Assert.Equal(0, row.Interest); Assert.Null(row.RatioPercent);
        }
    }

    private static SurplusResult Surplus(long income, long reversals, long expense, long refunds) => new("EUR", income, reversals, expense, refunds, 0, 0, 0);
    private static Account Payment(string currency) => new() { Name = "Original payment source", CurrencyCode = currency, Type = AccountType.Checking, OpeningDate = new DateOnly(2026, 1, 1), OpeningBalance = 100, IncludeInTotals = true, UsableForPayments = true };
    private static Account Loan(string currency, long principal) => new() { Name = "Original distinct loan", CurrencyCode = currency, Type = AccountType.Loan, OpeningDate = new DateOnly(2026, 1, 1), OpeningBalance = -principal, UsableForPayments = false };
    private static Schedule Installment(Account source, Account loan, long amount) => new() { Name = "Original installment", AccountId = source.Id, ToAccountId = loan.Id, Kind = EntryKind.Transfer, Amount = amount, Rule = new RecurrenceRule { Frequency = Frequency.Once, Start = Today } };

    private static EssentialCoverage Coverage(long monthlyExpense)
    {
        var account = Payment("EUR"); var category = new Category { Name = "Original essential", Kind = CategoryKind.Expense, IsEssential = true };
        var entries = new[] { 8, 9, 10 }.Select(month => new LedgerEntry { AccountId = account.Id, CategoryId = category.Id, Kind = EntryKind.Expense, Amount = monthlyExpense, Date = new DateOnly(2026, month, 1) }).ToList();
        Schedule Plan(string name) => new() { Name = name, AccountId = account.Id, CategoryId = category.Id, Kind = EntryKind.Expense, Amount = long.MaxValue - 1, Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Interval = 2, Start = Today } };
        return KpiCatalog.Coverage([account], entries, [category], [Plan("First nonmonthly essential"), Plan("Second nonmonthly essential")], "EUR", Today, PeriodCalendar.Gregorian);
    }
}
