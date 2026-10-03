using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Core.Tests.Zex;

/// <summary>Shared definitions of phase 1: entry classes, checked sums and rate information (ZEX-S0104, S0106).</summary>
public sealed class ZexCalculationContractTests
{
    private static readonly DateOnly Today = new(2026, 10, 20);

    [Theory]
    [InlineData(EntryKind.Income, EntryClass.Income, true)]
    [InlineData(EntryKind.IncomeReversal, EntryClass.Income, true)]
    [InlineData(EntryKind.Expense, EntryClass.Consumption, true)]
    [InlineData(EntryKind.Refund, EntryClass.Consumption, true)]
    [InlineData(EntryKind.Transfer, EntryClass.Transfer, false)]
    [InlineData(EntryKind.Adjustment, EntryClass.Correction, false)]
    public void Every_kind_has_one_class(EntryKind kind, EntryClass expected, bool countsInResult)
    {
        Assert.Equal(expected, EntryClassification.Of(kind));
        Assert.Equal(countsInResult, EntryClassification.CountsInResult(kind));
    }

    [Fact]
    public void Totals_beyond_the_range_of_long_fail_instead_of_wrapping()
    {
        var ledger = new LedgerBuilder();
        var a = ledger.Account("A", 0m);
        var b = ledger.Account("B", 0m);
        a.OpeningBalance = long.MaxValue - 10;
        b.OpeningBalance = 100;

        Assert.Throws<OverflowException>(() => LedgerCalculator.TotalBalances(ledger.Accounts, ledger.Entries, Today));
    }

    [Fact]
    public void Converted_totals_name_the_date_and_freshness_of_each_rate()
    {
        var rates = new RateTable(
        [
            new ExchangeRate { FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = 0.92m, Date = Today.AddDays(-40) },
            new ExchangeRate { FromCurrencyCode = "GBP", ToCurrencyCode = "EUR", Rate = 1.15m, Date = Today.AddDays(-3), IsEstimate = true },
        ]);
        var balances = new Dictionary<string, long> { ["EUR"] = 1_000_00, ["USD"] = 100_00, ["GBP"] = 100_00 };

        var combined = rates.Combine(balances, "EUR", Today);

        Assert.Equal(1_000_00 + 92_00 + 115_00, combined.Total);
        Assert.True(combined.IsOutdated);
        Assert.True(combined.HasEstimate);
        var usd = combined.Rates.Single(r => r.CurrencyCode == "USD");
        Assert.Equal((Today.AddDays(-40), false, true), (usd.Date!.Value, usd.IsEstimate, usd.IsOutdated));
        var gbp = combined.Rates.Single(r => r.CurrencyCode == "GBP");
        Assert.Equal((true, false), (gbp.IsEstimate, gbp.IsOutdated));
    }

    [Theory]
    [InlineData(30, null, 31, true)]
    [InlineData(30, null, 30, false)]
    [InlineData(7, null, 8, true)]
    [InlineData(90, null, 60, false)]
    [InlineData(0, null, 400, false)]
    public void Freshness_counts_days_before_the_valuation_date(int days, int? monthStartDay, int age, bool outdated)
    {
        var freshness = new RateFreshness(days, monthStartDay is { } d ? new DateOnly(2026, 10, d) : null);

        Assert.Equal(outdated, freshness.IsOutdated(Today.AddDays(-age), Today));
    }

    [Fact]
    public void A_rate_of_the_current_financial_month_is_never_outdated()
    {
        var freshness = new RateFreshness(7, FinancialMonthStart: new DateOnly(2026, 10, 1));

        Assert.False(freshness.IsOutdated(new DateOnly(2026, 10, 2), Today));
        Assert.True(freshness.IsOutdated(new DateOnly(2026, 9, 28), Today));
    }

    [Fact]
    public void A_rate_dated_after_the_valuation_date_is_never_used()
    {
        var rates = new RateTable([new ExchangeRate { FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = 0.9m, Date = Today.AddDays(1) }]);

        Assert.False(rates.Combine(new Dictionary<string, long> { ["USD"] = 1_00 }, "EUR", Today).IsComplete);
    }

    [Fact]
    public void A_currency_lock_counts_every_stored_amount()
    {
        Assert.False(AccountCurrencyLock.None.IsLocked);
        Assert.True(new AccountCurrencyLock(0, 1, 0, 0, 0, false).IsLocked);
        Assert.True(new AccountCurrencyLock(0, 0, 0, 0, 0, true).IsLocked);
    }

    [Theory]
    [InlineData(AccountType.Cash, true, true)]
    [InlineData(AccountType.Checking, true, true)]
    [InlineData(AccountType.Savings, true, true)]
    [InlineData(AccountType.CreditCard, false, true)]
    [InlineData(AccountType.Loan, false, false)]
    [InlineData(AccountType.Lent, false, false)]
    [InlineData(AccountType.Asset, false, false)]
    public void Account_types_decide_usable_money_and_default_accounts(AccountType type, bool usable, bool canBeDefault)
    {
        Assert.Equal(usable, type.IsUsableByDefault());
        Assert.Equal(canBeDefault, type.CanBeDefault());
    }
}
