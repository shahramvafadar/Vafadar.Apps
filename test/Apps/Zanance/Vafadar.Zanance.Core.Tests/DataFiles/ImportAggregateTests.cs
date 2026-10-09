using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.DataFiles;

/// <summary>Import coverage is exact and reductions preserve every original metadata field (D-72).</summary>
public sealed class ImportAggregateTests
{
    private static readonly DateOnly Start = new(2026, 9, 1);

    [Theory, InlineData("account"), InlineData("kind"), InlineData("category"), InlineData("before"), InlineData("after"), Trait("AT", "AT-79")]
    public void Different_account_kind_category_or_dates_never_reduce_the_aggregate(string difference)
    {
        var aggregate = Aggregate();
        var detail = Detail(aggregate);
        switch (difference)
        {
            case "account": detail.AccountId = Guid.NewGuid(); break;
            case "kind": detail.Kind = EntryKind.Income; break;
            case "category": detail.CategoryId = Guid.NewGuid(); break;
            case "before": detail.Date = Start.AddDays(-1); break;
            case "after": detail.Date = Start.AddDays(30); break;
        }
        Assert.Empty(AggregatedEntries.FindForImport([detail], [aggregate]));
    }

    [Fact, Trait("AT", "AT-79")]
    public void Both_range_boundaries_are_inclusive_and_repeated_ids_count_once()
    {
        var aggregate = Aggregate();
        var first = Detail(aggregate); first.Date = Start;
        var last = Detail(aggregate); last.Date = Start.AddDays(29);
        var overlap = Assert.Single(AggregatedEntries.FindForImport([first, first.Copy(), last], [aggregate]));
        Assert.Equal(2, overlap.Detailed.Count);
        Assert.Equal(20000, overlap.DetailedTotal);
    }

    [Fact, Trait("AT", "AT-79")]
    public void Import_reduction_ignores_old_details_and_already_imported_ids()
    {
        var aggregate = Aggregate(); aggregate.Amount = 31200;
        var old = Detail(aggregate);
        var added = Detail(aggregate); added.Amount = 5000;
        var overlap = Assert.Single(AggregatedEntries.FindForImport([old.Copy(), added], [aggregate, old]));
        Assert.Equal(added.Id, Assert.Single(overlap.Detailed).Id);
        Assert.Equal(26200, overlap.Remainder);
    }

    [Fact, Trait("AT", "AT-79")]
    public void Aggregate_copy_retains_plan_import_foreign_reimbursement_and_audit_metadata_without_sharing_tags()
    {
        var aggregate = Aggregate();
        aggregate.Source = EntrySource.Import; aggregate.ImportBatchId = Guid.NewGuid();
        aggregate.OriginalAmount = 45000; aggregate.OriginalCurrencyCode = "USD";
        aggregate.ScheduleId = Guid.NewGuid(); aggregate.OccurrenceDate = Start;
        aggregate.ReimbursableAmount = 1000; aggregate.ReimbursedBy = "Fictitious employer"; aggregate.ReimbursementDueDate = Start.AddMonths(1);
        aggregate.Tags = ["period"]; aggregate.CreatedAt = DateTimeOffset.Parse("2026-09-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        aggregate.UpdatedAt = aggregate.CreatedAt.AddDays(1);
        var remaining = AggregatedEntries.Replace(new AggregateOverlap(aggregate, [Detail(aggregate)]))!;
        Assert.Equal((aggregate.Id, 31200, aggregate.ImportBatchId, aggregate.Source), (remaining.Id, remaining.Amount, remaining.ImportBatchId, remaining.Source));
        Assert.Equal((aggregate.ScheduleId, aggregate.OccurrenceDate, aggregate.OriginalAmount, aggregate.OriginalCurrencyCode),
            (remaining.ScheduleId, remaining.OccurrenceDate, remaining.OriginalAmount, remaining.OriginalCurrencyCode));
        Assert.Equal((aggregate.ReimbursableAmount, aggregate.ReimbursedBy, aggregate.ReimbursementDueDate, aggregate.CreatedAt, aggregate.UpdatedAt),
            (remaining.ReimbursableAmount, remaining.ReimbursedBy, remaining.ReimbursementDueDate, remaining.CreatedAt, remaining.UpdatedAt));
        remaining.Tags.Add("detail");
        Assert.Equal(["period"], aggregate.Tags);
        Assert.False(AggregatedEntries.CanLink(aggregate));
    }

    private static LedgerEntry Aggregate() => new() { AccountId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Kind = EntryKind.Expense,
        Date = Start.AddDays(29), Amount = 41200, IsAggregated = true, AggregatedFrom = Start, AggregatedTo = Start.AddDays(29) };
    private static LedgerEntry Detail(LedgerEntry aggregate) => new() { AccountId = aggregate.AccountId, CategoryId = aggregate.CategoryId,
        Kind = EntryKind.Expense, Date = Start.AddDays(4), Amount = 10000 };
}
