using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Regression tests for defects found in the code review of 2026-09-28.</summary>
public sealed class ReviewFixTests
{
    private readonly Account _checking = new() { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1) };
    private readonly Account _savings = new() { Name = "Savings", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1) };
    private readonly Category _food = new() { Kind = CategoryKind.Expense, SystemKey = "Food" };

    [Fact]
    public void Adjustments_keep_their_direction_through_a_csv_export_and_import()
    {
        var decrease = new LedgerEntry { Kind = EntryKind.Adjustment, AccountId = _checking.Id, Amount = 4_000, Date = new DateOnly(2026, 5, 1), Direction = AdjustmentDirection.Decrease };
        var rows = Csv.Read(CsvExport.Write([decrease], new Dictionary<Guid, Account> { [_checking.Id] = _checking }, _ => string.Empty, includeNotes: true), ',');

        var imported = CsvImport.PreviewOwn(rows, [_checking, _savings], [_food], _ => "Food", []).Single();
        Assert.NotNull(imported.Entry);
        Assert.Equal(AdjustmentDirection.Decrease, imported.Entry!.Direction);
    }

    [Theory]
    [InlineData("12,50-", -1_250)]
    [InlineData("-12,50", -1_250)]
    [InlineData("12,50+", 1_250)]
    [InlineData("(12,50)", -1_250)]
    public void Csv_amounts_accept_one_leading_or_trailing_sign(string text, long expected)
    {
        Assert.True(CsvImport.TryAmount(text, "EUR", ',', out var minor));
        Assert.Equal(expected, minor);
    }

    [Theory]
    [InlineData("+-5")]
    [InlineData("--5")]
    [InlineData("(-5)")]
    [InlineData("5-5")]
    [InlineData("100000000000000000000")]
    public void Ambiguous_or_too_large_csv_amounts_are_rejected(string text) =>
        Assert.False(CsvImport.TryAmount(text, "EUR", ',', out _));

    [Fact]
    public void An_amount_too_large_for_minor_units_is_invalid_input_not_a_crash() =>
        Assert.False(MoneyAmount.TryParse("100000000000000000000", Currencies.Euro, System.Globalization.CultureInfo.InvariantCulture, out _));

    [Fact]
    public void A_purchase_with_refunds_or_an_amount_to_be_paid_back_cannot_be_split()
    {
        var purchase = new LedgerEntry { Kind = EntryKind.Expense, AccountId = _checking.Id, Amount = 10_000, Date = new DateOnly(2026, 5, 1) };
        var refund = EntryActions.CreateRefund(purchase, 8_000, _checking.Id, new DateOnly(2026, 5, 3));
        Assert.True(EntryActions.CanSplit([purchase]));
        Assert.True(EntryActions.HasPaybacks([purchase], [purchase, refund]));
        Assert.False(EntryActions.HasPaybacks([purchase], [purchase]));
        Assert.True(EntryActions.HasPaybacks([new LedgerEntry { Kind = EntryKind.Expense, AccountId = _checking.Id, Amount = 1_000, Date = new DateOnly(2026, 5, 1), ReimbursableAmount = 500 }], []));
    }

    [Fact]
    public void Partial_payments_of_an_open_occurrence_do_not_count_as_savings_in_plan_versus_actual()
    {
        var plan = new Schedule { Name = "Insurance", Kind = EntryKind.Expense, AccountId = _checking.Id, Amount = 100_000, Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 5, 10) } };
        var part = new LedgerEntry { Kind = EntryKind.Expense, AccountId = _checking.Id, Amount = 40_000, Date = new DateOnly(2026, 5, 8), ScheduleId = plan.Id, OccurrenceDate = new DateOnly(2026, 5, 10), IsPartialPayment = true };

        var row = ReportCalculator.PlanVsActual([plan], [], [part], new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31), new DateOnly(2026, 5, 20)).Single();
        Assert.Equal(0, row.SettledCount);
        Assert.Null(row.Variance);
    }

    [Fact]
    [Trait("AT", "AT-10")]
    public void A_plan_ended_when_its_account_is_archived_has_no_later_occurrences()
    {
        var plan = new Schedule { Name = "Rent", Kind = EntryKind.Expense, AccountId = _checking.Id, Amount = 90_000, AutoPost = true, Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 1, 5) } };
        var archivedOn = new DateOnly(2026, 5, 20);

        PlanActions.End(plan, archivedOn);

        Assert.Null(Occurrences.NextOpen(plan, [], archivedOn.AddDays(1), archivedOn));
        Assert.False(plan.AutoPost);
    }

    [Fact]
    public void A_transfer_between_two_accounts_in_scope_is_one_unreviewed_entry()
    {
        var transfer = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = _checking.Id, ToAccountId = _savings.Id, Amount = 5_000, Date = new DateOnly(2026, 5, 1), Review = ReviewState.Unreviewed };
        var summary = LedgerCalculator.Unreviewed([_checking, _savings], [transfer]).Single();
        Assert.Equal(1, summary.Count);
        Assert.Equal(0, summary.NetEffect);
    }
}
