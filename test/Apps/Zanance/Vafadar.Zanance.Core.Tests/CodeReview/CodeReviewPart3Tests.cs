using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.Core.Tests.CodeReview;

/// <summary>Regression tests of the code review, part 3: plans, budgets, forecasts and receipts.</summary>
public sealed class CodeReviewPart3Tests
{
    public static TheoryData<Frequency, int, EndKind> Rules() => new()
    {
        { Frequency.Daily, 3, EndKind.Never },
        { Frequency.Daily, 1, EndKind.AfterCount },
        { Frequency.Weekly, 2, EndKind.OnDate },
        { Frequency.Weekly, 1, EndKind.Never },
    };

    [Theory]
    [MemberData(nameof(Rules))]
    public void A_range_long_after_the_start_has_the_same_dates_and_numbers_as_counting_from_the_start(Frequency frequency, int interval, EndKind end)
    {
        // CR03-01: daily and weekly rules start at the step of the range instead of generating every earlier date.
        var rule = new RecurrenceRule
        {
            Frequency = frequency,
            Interval = interval,
            Start = new DateOnly(2020, 1, 7),
            End = end,
            Count = end == EndKind.AfterCount ? 2_000 : null,
            EndDate = end == EndKind.OnDate ? new DateOnly(2025, 4, 1) : null,
        };
        var from = new DateOnly(2025, 3, 10);
        var to = new DateOnly(2025, 4, 20);

        var expected = Recurrence.Between(rule, rule.Start, to).Where(d => d.Date >= from).ToList();

        Assert.NotEmpty(expected);
        Assert.Equal(expected, Recurrence.Between(rule, from, to));
    }

    [Fact]
    public void A_snapshot_compares_with_the_same_balances_as_the_ledger_day_by_day()
    {
        // CR03-02: the actual balances are built up day by day; they equal the balances read from the ledger, also for an
        // account that opens during the path and a transfer between two accounts of the scope.
        var ledger = new LedgerBuilder();
        var baseDate = new DateOnly(2026, 10, 1);
        var main = ledger.Account("Main", 1_000, openingDate: new DateOnly(2026, 1, 1));
        var later = ledger.Account("Later", 300, openingDate: baseDate.AddDays(5));
        ledger.Add(EntryKind.Expense, main, 40, baseDate.AddDays(2));
        ledger.Add(EntryKind.Income, main, 500, baseDate.AddDays(9));
        ledger.Transfer(main, later, 100, date: baseDate.AddDays(7));
        ledger.Add(EntryKind.Expense, later, 10, baseDate.AddDays(3));
        var snapshot = new ForecastSnapshot
        {
            Name = "Test",
            BaseDate = baseDate,
            Horizon = baseDate.AddDays(14),
            CurrencyCode = "EUR",
            AccountIds = $"{main.Id},{later.Id}",
            Path = string.Join(',', Enumerable.Repeat("0", 15)),
            CreatedAt = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
        };

        var comparison = SnapshotComparer.Compare(snapshot, ledger.Accounts, ledger.Entries, baseDate.AddDays(12));

        Assert.Equal(13, comparison.Days.Count);
        Assert.All(comparison.Days, day => Assert.Equal(
            LedgerCalculator.Balance(main, ledger.Entries, day.Date) + LedgerCalculator.Balance(later, ledger.Entries, day.Date),
            day.Actual));
    }

    [Fact]
    public void Budget_spending_beyond_the_range_of_a_number_is_an_error_not_a_wrong_number()
    {
        // CR03-03: checked like the other sums of the ledger.
        var ledger = new LedgerBuilder();
        var main = ledger.Account("Main", 0, openingDate: new DateOnly(2026, 1, 1));
        ledger.Add(EntryKind.Expense, main, 1, new DateOnly(2026, 10, 2)).Amount = long.MaxValue;
        ledger.Add(EntryKind.Expense, main, 1, new DateOnly(2026, 10, 3));

        Assert.Throws<OverflowException>(() => BudgetCalculator.NetExpense(ledger.Accounts, ledger.Entries, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), "EUR"));
    }

    [Fact]
    public void A_persian_total_keyword_with_arabic_letter_forms_is_recognised()
    {
        // CR03-04: PDFs and OCR often write ك and ي; "جمع کل" is still the total, not the last line with "مبلغ".
        var text = "فروشگاه نمونه\nجمع كل 125,000\nمبلغ خدمات 5,000";

        Assert.Equal(125_000m, ReceiptParser.Parse(text).Amount);
    }
}