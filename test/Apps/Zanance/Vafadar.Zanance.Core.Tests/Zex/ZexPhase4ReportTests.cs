using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Tests.Zex;

/// <summary>Drill-down parity, aggregated entries and the period-end review of enhancement ZEX phase 4.</summary>
public sealed class ZexPhase4ReportTests
{
    private static readonly DateOnly Oct1 = new(2026, 10, 1);

    [Fact]
    public void S0601_the_drill_down_of_a_category_total_lists_only_entries_of_that_currency_with_the_same_sum()
    {
        var ledger = new LedgerBuilder();
        var euro = ledger.Account("Main", 5_000);
        var dollar = ledger.Account("Dollar", 5_000, currency: "USD");
        var food = Guid.NewGuid();
        ledger.Add(EntryKind.Expense, euro, 500, Oct1.AddDays(2), food);
        ledger.Add(EntryKind.Expense, euro, 320, Oct1.AddDays(5), food);
        ledger.Add(EntryKind.Expense, dollar, 700, Oct1.AddDays(5), food);
        var filter = new LedgerFilter(Oct1, Oct1.AddDays(30));

        var total = LedgerCalculator.ExpenseByCategory(ledger.Accounts, ledger.Entries, filter, id => id).Single(c => c.CurrencyCode == "EUR").Net;
        var accounts = ledger.Accounts.ToDictionary(a => a.Id);
        var listed = EntrySearch.Apply(ledger.Entries, new EntryFilter(Oct1, Oct1.AddDays(30), KindFilter.Expenses, CategoryIds: [food], InTotalsOnly: true, CurrencyCode: "EUR"), _ => null, accounts).ToList();

        Assert.Equal(82_000, total);
        Assert.Equal(2, listed.Count);
        Assert.Equal(-total, EntrySearch.NetByCurrency(listed, accounts)["EUR"]);
    }

    [Fact]
    public void S0611_link_and_replace_keeps_the_difference_and_september_stays_412()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Main", 5_000, openingDate: new DateOnly(2026, 8, 1));
        var groceries = Guid.NewGuid();
        var aggregate = ledger.Add(EntryKind.Expense, checking, 412, new DateOnly(2026, 9, 30), groceries);
        aggregate.IsAggregated = true;
        aggregate.AggregatedFrom = new DateOnly(2026, 9, 1);
        aggregate.AggregatedTo = new DateOnly(2026, 9, 30);
        var details = new[] { 60m, 70m, 65m, 80m, 55m, 65m }
            .Select((amount, i) => ledger.Add(EntryKind.Expense, checking, amount, new DateOnly(2026, 9, 3 + (i * 4)), groceries))
            .ToList();

        var overlap = Assert.Single(AggregatedEntries.Find(details, ledger.Entries));
        var reduced = AggregatedEntries.Replace(overlap)!;
        ledger.Entries[ledger.Entries.IndexOf(aggregate)] = reduced;

        Assert.Equal((39_500, 1_700), (overlap.DetailedTotal, reduced.Amount));
        Assert.Equal(41_200, LedgerCalculator.Totals(ledger.Accounts, ledger.Entries, new LedgerFilter(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30))).Single().NetExpense);
        Assert.Equal(41_200, aggregate.Amount);
    }

    [Fact]
    public void S0611_entries_of_another_category_or_outside_the_range_do_not_overlap()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Main", 5_000, openingDate: new DateOnly(2026, 8, 1));
        var aggregate = ledger.Add(EntryKind.Expense, checking, 412, new DateOnly(2026, 9, 30), Guid.NewGuid());
        aggregate.IsAggregated = true;
        aggregate.AggregatedFrom = new DateOnly(2026, 9, 1);
        aggregate.AggregatedTo = new DateOnly(2026, 9, 30);
        var other = ledger.Add(EntryKind.Expense, checking, 50, new DateOnly(2026, 9, 10), Guid.NewGuid());
        var later = ledger.Add(EntryKind.Expense, checking, 50, new DateOnly(2026, 10, 2), aggregate.CategoryId);

        Assert.Empty(AggregatedEntries.Find([other, later], ledger.Entries));
    }

    [Fact]
    public void S0610_the_review_of_the_last_month_is_due_until_finished_and_keeps_its_steps()
    {
        var today = new DateOnly(2026, 10, 4);
        var first = new DateOnly(2026, 1, 1);

        var due = PeriodReview.Due(null, today, PeriodCalendar.Gregorian, 1, first)!;
        var progress = PeriodReview.Toggle(due, ReviewStep.Plans, true);
        var partly = PeriodReview.Due(progress, today, PeriodCalendar.Gregorian, 1, first)!;

        Assert.Equal("2026-09", due.Key);
        Assert.Equal([ReviewStep.Plans], partly.Done);
        Assert.Null(PeriodReview.Due(PeriodReview.Finish(partly), today, PeriodCalendar.Gregorian, 1, first));
        Assert.NotNull(PeriodReview.Due("2026-09", new DateOnly(2026, 11, 2), PeriodCalendar.Gregorian, 1, first));
        Assert.Null(PeriodReview.Due(null, today, PeriodCalendar.Gregorian, 1, new DateOnly(2026, 10, 1)));
    }

    [Fact]
    public void S0611_aggregated_entries_and_due_dates_survive_the_csv_round_trip()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Main", 5_000);
        var aggregate = ledger.Add(EntryKind.Expense, checking, 412, Oct1);
        aggregate.IsAggregated = true;
        aggregate.AggregatedFrom = new DateOnly(2026, 9, 1);
        aggregate.AggregatedTo = new DateOnly(2026, 9, 30);
        aggregate.ReimbursableAmount = 10_000;
        aggregate.ReimbursementDueDate = new DateOnly(2026, 11, 1);

        var rows = Csv.Read(CsvExport.Write(ledger.Entries, ledger.Accounts.ToDictionary(a => a.Id), _ => string.Empty, includeNotes: true), ',');
        var imported = Assert.Single(CsvImport.PreviewOwn(rows, ledger.Accounts, [], _ => string.Empty, [])).Entry!;

        Assert.True(imported.IsAggregated);
        Assert.Equal((aggregate.AggregatedFrom, aggregate.AggregatedTo), (imported.AggregatedFrom, imported.AggregatedTo));
        Assert.Equal(new DateOnly(2026, 11, 1), imported.ReimbursementDueDate);
    }

    [Fact]
    public void K14_possible_duplicates_ignore_parts_of_one_group()
    {
        var ledger = new LedgerBuilder();
        var checking = ledger.Account("Main", 5_000);
        var first = ledger.Add(EntryKind.Expense, checking, 20, Oct1);
        ledger.Add(EntryKind.Expense, checking, 20, Oct1);
        var group = Guid.NewGuid();
        var splitA = ledger.Add(EntryKind.Expense, checking, 5, Oct1);
        var splitB = ledger.Add(EntryKind.Expense, checking, 5, Oct1);
        splitA.GroupId = splitB.GroupId = group;

        var duplicates = DataStatus.Duplicates(ledger.Entries);

        Assert.Single(duplicates);
        Assert.NotEqual(first.Id, duplicates[0].Id);
    }
}
