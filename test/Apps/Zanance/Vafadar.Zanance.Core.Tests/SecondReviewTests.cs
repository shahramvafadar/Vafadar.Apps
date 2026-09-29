using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Regression tests for defects found in the full code review of 2026-09-29.</summary>
public sealed class SecondReviewTests
{
    private static readonly DateOnly Today = new(2026, 3, 10);
    private readonly Account _checking = new() { Name = "Checking", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 1, 1) };
    private readonly Account _rial = new() { Name = "Rial", CurrencyCode = "IRR", OpeningDate = new DateOnly(2026, 1, 1) };

    [Fact]
    public void Open_occurrences_before_a_this_and_future_change_stay_in_the_forecast()
    {
        var rent = new Schedule { Name = "Rent", Kind = EntryKind.Expense, AccountId = _checking.Id, Amount = 100_000, Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 3, 1) } };
        var next = PlanActions.SplitFrom(rent, new DateOnly(2026, 5, 1));

        var forecast = ForecastCalculator.Compute([_checking], [], [rent, next], [], Today, new DateOnly(2026, 5, 31)).Single();

        // 1 March (overdue), 1 April from the old slice and 1 May from the new one.
        Assert.Equal(3, forecast.Items.Count(i => i.ScheduleId is not null));
        Assert.Equal(-300_000, forecast.EndBalance);
    }

    [Fact]
    public void A_plan_the_user_simply_ended_leaves_the_forecast()
    {
        var gym = new Schedule { Name = "Gym", Kind = EntryKind.Expense, AccountId = _checking.Id, Amount = 4_000, Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 3, 1) } };
        PlanActions.End(gym, new DateOnly(2026, 3, 5));

        Assert.Empty(ForecastCalculator.Compute([_checking], [], [gym], [], Today, new DateOnly(2026, 5, 31)).Single().Items);
    }

    [Fact]
    public void An_unknown_transfer_between_two_currencies_makes_both_forecasts_incomplete()
    {
        var plan = new Schedule { Name = "Exchange", Kind = EntryKind.Transfer, AccountId = _checking.Id, ToAccountId = _rial.Id, AmountMode = AmountMode.Unknown, Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 3, 20) } };

        var forecasts = ForecastCalculator.Compute([_checking, _rial], [], [plan], [], Today, new DateOnly(2026, 3, 31));

        Assert.All(forecasts, f => Assert.True(f.IsIncomplete));
        Assert.Equal(2, forecasts.Count);
    }

    [Fact]
    public void A_partly_paid_transfer_between_currencies_moves_the_other_side_in_proportion()
    {
        var plan = new Schedule { Name = "Exchange", Kind = EntryKind.Transfer, AccountId = _checking.Id, ToAccountId = _rial.Id, Amount = 10_000, ToAmount = 5_000_000, Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2026, 3, 20) } };
        var part = new LedgerEntry { Kind = EntryKind.Transfer, AccountId = _checking.Id, ToAccountId = _rial.Id, Amount = 4_000, ToAmount = 2_000_000, Date = new DateOnly(2026, 3, 5), ScheduleId = plan.Id, OccurrenceDate = new DateOnly(2026, 3, 20), IsPartialPayment = true };

        var paid = new OccurrenceState { ScheduleId = plan.Id, OriginalDate = new DateOnly(2026, 3, 20), Status = OccurrenceStatus.Open, PaidAmount = 4_000 };

        var rial = ForecastCalculator.Compute([_checking, _rial], [part], [plan], [paid], Today, new DateOnly(2026, 3, 31)).Single(f => f.CurrencyCode == "IRR");

        Assert.Equal(3_000_000, rial.Items.Single(i => i.ScheduleId == plan.Id).Effect);
    }

    [Fact]
    public void An_account_that_opens_later_brings_its_opening_balance_on_that_day()
    {
        var future = new Account { Name = "New", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 4, 1), OpeningBalance = 50_000 };

        var forecast = ForecastCalculator.Compute([_checking, future], [], [], [], Today, new DateOnly(2026, 4, 30)).Single();

        Assert.Equal(50_000, forecast.EndBalance);
    }

    [Fact]
    public void A_non_monthly_bill_that_has_ended_gets_no_monthly_share()
    {
        var insurance = new Category { Kind = CategoryKind.Expense, SystemKey = "Insurance", SpendingType = SpendingType.NonMonthly };
        var plan = new Schedule
        {
            Name = "Insurance", Kind = EntryKind.Expense, AccountId = _checking.Id, CategoryId = insurance.Id, Amount = 60_000,
            Rule = new RecurrenceRule { Frequency = Frequency.Yearly, Start = new DateOnly(2024, 6, 1), End = EndKind.OnDate, EndDate = new DateOnly(2025, 6, 1) },
        };

        var flex = FlexCalculator.Summarize(10_000, [_checking], [], [plan], [], [insurance], new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), "EUR", Today);

        Assert.Equal(0, flex.NonMonthly.Planned);
    }

    [Fact]
    public void Own_csv_rows_with_another_currency_numeric_kinds_or_adjustments_without_direction_are_rejected()
    {
        var accounts = new Dictionary<Guid, Account> { [_checking.Id] = _checking };
        var expense = new LedgerEntry { Kind = EntryKind.Expense, AccountId = _checking.Id, Amount = 1_250, Date = new DateOnly(2026, 3, 1) };
        var header = CsvExport.Write([], accounts, _ => string.Empty, includeNotes: true).Split('\n')[0].TrimEnd('\r');
        var row = CsvExport.Write([expense], accounts, _ => string.Empty, includeNotes: true).Split('\n')[1].TrimEnd('\r');
        var cells = row.Split(',');

        string With(int column, string value)
        {
            var copy = (string[])cells.Clone();
            copy[column] = value;
            copy[0] = Guid.NewGuid().ToString();
            return string.Join(',', copy);
        }

        var text = string.Join('\n', header, With(4, "USD"), With(2, "7"), With(2, "Adjustment"));
        var preview = CsvImport.PreviewOwn(Csv.Read(text, ','), [_checking], [], _ => string.Empty, []);

        Assert.Equal(["Currency", "Kind", "Direction"], preview.Select(p => p.Error));
    }

    [Fact]
    public void Very_long_amounts_are_invalid_instead_of_crashing()
    {
        Assert.False(MoneyAmount.TryParse("9999999999999999999999999999", Currencies.Euro, System.Globalization.CultureInfo.InvariantCulture, out _));
        Assert.False(CsvImport.TryAmount("9999999999999999999999999999", "EUR", '.', out _));
    }

    [Fact]
    public void A_refund_to_an_account_in_another_currency_is_invalid()
    {
        var purchase = new LedgerEntry { Kind = EntryKind.Expense, AccountId = _checking.Id, Amount = 10_000, Date = new DateOnly(2026, 3, 1) };
        var refund = new LedgerEntry { Kind = EntryKind.Refund, AccountId = _rial.Id, Amount = 5_000, Date = new DateOnly(2026, 3, 2), RefundOfId = purchase.Id };
        var accounts = new Dictionary<Guid, Account> { [_checking.Id] = _checking, [_rial.Id] = _rial };

        Assert.Contains(LedgerError.RefundCurrencyMismatch, LedgerValidator.Validate(refund, accounts, new Dictionary<Guid, Category>(), purchase));
    }
}
