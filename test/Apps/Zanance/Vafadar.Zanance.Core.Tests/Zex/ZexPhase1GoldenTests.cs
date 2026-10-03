using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Core.Tests.Zex;

/// <summary>
/// The golden examples of enhancement ZEX that phase 1 makes true (docs/enhancements/…/02 §11, ZEX-S0101). Later
/// examples (holdings, goals, KPIs) are added with their phases.
/// </summary>
public sealed class ZexPhase1GoldenTests
{
    private static readonly DateOnly Today = new(2026, 10, 3);

    [Fact]
    public void G01_totals_are_native_per_currency_and_never_mixed()
    {
        var ledger = new LedgerBuilder();
        ledger.Account("Main", 1_250m);
        ledger.Account("Savings", 750m, AccountType.Savings);
        ledger.Account("Dollar", 4_000m, currency: "USD");
        ledger.Account("Rial", 100_000_000m, currency: "IRR");

        var totals = LedgerCalculator.TotalBalances(ledger.Accounts, ledger.Entries, Today);

        Assert.Equal(3, totals.Count);
        Assert.Equal(2_000_00, totals["EUR"]);
        Assert.Equal(4_000_00, totals["USD"]);
        Assert.Equal(LedgerBuilder.Minor(100_000_000m, "IRR"), totals["IRR"]);
    }

    [Fact]
    public void G01_archived_accounts_leave_the_current_totals()
    {
        var ledger = new LedgerBuilder();
        ledger.Account("Main", 1_250m);
        ledger.Account("Savings", 750m, AccountType.Savings);
        ledger.Account("Old", 500m).IsArchived = true;

        Assert.Equal(2_000_00, LedgerCalculator.TotalBalances(ledger.Accounts, ledger.Entries, Today)["EUR"]);

        // An explicit account list is taken as it is, e.g. a report of a past period.
        var all = ledger.Accounts.Select(a => a.Id).ToList();
        Assert.Equal(2_500_00, LedgerCalculator.TotalBalances(ledger.Accounts, ledger.Entries, Today, all)["EUR"]);
    }

    [Fact]
    public void G02_quick_add_uses_the_valid_default_account()
    {
        var ledger = new LedgerBuilder();
        var main = ledger.Account("Main", 100m);

        var choice = EntryAccountContract.Choose(ledger.Accounts, explicitId: null, contextId: null, defaultId: main.Id);

        Assert.Equal(new EntryAccountChoice(main.Id, EntryAccountSource.Default), choice);
    }

    [Fact]
    public void G03_an_explicit_choice_wins_and_a_currency_change_is_reported()
    {
        var ledger = new LedgerBuilder();
        var main = ledger.Account("Main", 100m);
        var dollar = ledger.Account("Dollar", 100m, currency: "USD");

        var choice = EntryAccountContract.Choose(ledger.Accounts, explicitId: dollar.Id, contextId: null, defaultId: main.Id);

        Assert.Equal(dollar.Id, choice.AccountId);
        Assert.Equal(EntryAccountSource.Explicit, choice.Source);
        Assert.True(EntryAccountContract.ChangesCurrency(main, dollar));
        Assert.False(EntryAccountContract.ChangesCurrency(main, ledger.Account("Cash", 0m, AccountType.Cash)));
    }

    [Fact]
    public void G04_an_archived_default_gives_no_account_instead_of_a_fallback()
    {
        var ledger = new LedgerBuilder();
        var main = ledger.Account("Main", 100m);
        ledger.Account("Other", 100m);
        main.IsArchived = true;

        var choice = EntryAccountContract.Choose(ledger.Accounts, null, null, main.Id);

        Assert.Equal(new EntryAccountChoice(null, EntryAccountSource.None), choice);
    }

    [Theory]
    [InlineData(AccountType.Loan)]
    [InlineData(AccountType.Lent)]
    [InlineData(AccountType.Asset)]
    public void Debts_receivables_and_assets_are_never_the_default(AccountType type)
    {
        var ledger = new LedgerBuilder();
        var account = ledger.Account("Not money", 100m, type);

        Assert.False(EntryAccountContract.IsValidDefault(account));
        Assert.Null(EntryAccountContract.Choose(ledger.Accounts, null, null, account.Id).AccountId);
    }

    [Fact]
    public void A_template_of_an_archived_account_is_reported_and_no_other_account_is_guessed()
    {
        var ledger = new LedgerBuilder();
        var main = ledger.Account("Main", 100m);
        var old = ledger.Account("Old", 0m);
        old.IsArchived = true;

        var choice = EntryAccountContract.Choose(ledger.Accounts, null, contextId: old.Id, defaultId: main.Id);

        Assert.Equal(new EntryAccountChoice(null, EntryAccountSource.None, ContextUnavailable: true), choice);
    }

    [Fact]
    public void A_refund_only_takes_an_account_in_the_purchase_currency()
    {
        var ledger = new LedgerBuilder();
        var main = ledger.Account("Main", 100m);
        var dollar = ledger.Account("Dollar", 100m, currency: "USD");

        Assert.Equal(main.Id, EntryAccountContract.Choose(ledger.Accounts, null, null, main.Id, "EUR").AccountId);
        Assert.Null(EntryAccountContract.Choose(ledger.Accounts, null, null, dollar.Id, "EUR").AccountId);
    }

    [Fact]
    public void G05_a_cross_currency_transfer_with_fee_moves_money_and_counts_only_the_fee()
    {
        var ledger = new LedgerBuilder();
        var main = ledger.Account("Main", 500m);
        var dollar = ledger.Account("Dollar", 0m, currency: "USD");
        var transfer = ledger.Transfer(main, dollar, 100m, 110m);
        var fee = EntryActions.SyncTransferFee(transfer, null, 2_00, null)!;
        ledger.Entries.Add(fee);

        Assert.Equal(398_00, ledger.Balance(main));
        Assert.Equal(110_00, ledger.Balance(dollar));
        var totals = LedgerCalculator.Totals(ledger.Accounts, ledger.Entries, new LedgerFilter(LedgerBuilder.Day1, Today));
        var eur = Assert.Single(totals);
        Assert.Equal(("EUR", 0L, 2_00L), (eur.CurrencyCode, eur.NetIncome, eur.NetExpense));
    }

    [Fact]
    public void G06_a_missing_rate_makes_only_the_converted_total_incomplete()
    {
        var balances = new Dictionary<string, long> { ["EUR"] = 2_000_00, ["USD"] = 4_000_00 };

        var combined = new RateTable([]).Combine(balances, "EUR", Today);

        Assert.False(combined.IsComplete);
        Assert.Null(combined.Total);
        Assert.Equal(["USD"], combined.MissingCurrencies);
        var usd = Assert.Single(combined.Rates);
        Assert.True(usd.IsMissing);
    }

    [Fact]
    public void G17_a_month_without_income_has_a_negative_result()
    {
        var ledger = new LedgerBuilder();
        var main = ledger.Account("Main", 1_000m);
        ledger.Add(EntryKind.Expense, main, 450m);

        var totals = ledger.Totals();

        Assert.Equal(0, totals.NetIncome);
        Assert.Equal(-450_00, totals.Result);
    }
}
