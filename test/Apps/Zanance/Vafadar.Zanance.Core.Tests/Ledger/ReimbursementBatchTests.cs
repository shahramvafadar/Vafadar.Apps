using System.Collections;
using System.Text.Json;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.Ledger;

/// <summary>Batch reimbursement calculations retain the single-entry contract without repeated full-ledger scans.</summary>
[Trait("AT", "AT-351")]
public sealed class ReimbursementBatchTests
{
    /// <summary>Legitimate large receivable sets must not require a complete history scan per expense.</summary>
    [Theory, InlineData(64), InlineData(1024)]
    public void Large_batch_has_a_linear_bound_on_original_ledger_visits(int count)
    {
        var entries = new List<LedgerEntry>();
        for (var index = 0; index < count; index++)
        {
            var expense = Expense(1000, new DateOnly(2026, 10, 1));
            entries.Add(expense); entries.Add(Refund(expense.Id, 200));
        }
        var counted = new CountedEntries(entries);
        var actual = EntryActions.OpenReimbursements(counted);
        Assert.Equal(count, actual.Count);
        Assert.All(actual, item => Assert.Equal(800, item.Open));
        Assert.InRange(counted.Visits, 0, 3L * entries.Count);
    }

    /// <summary>The unchanged single-entry API is the amount oracle; stable order and original identities remain intact.</summary>
    [Theory, InlineData(0), InlineData(7), InlineData(42)]
    public void Batch_matches_single_entry_amounts_and_preserves_stable_order_and_original_objects(int seed)
    {
        var random = new Random(seed); var entries = new List<LedgerEntry>();
        for (var index = 0; index < 30; index++)
        {
            var expense = Expense(random.Next(1, 1000), new DateOnly(2026, 10, 1).AddDays(index % 3));
            expense.Title = "Fictitious original " + index; expense.Tags = ["retained", index.ToString()];
            entries.Add(expense);
            entries.Add(Refund(expense.Id, random.Next(0, 1200)));
            entries.Add(Refund(expense.Id, random.Next(0, 100)));
            entries.Add(new LedgerEntry { Kind = EntryKind.IncomeReversal, Amount = 500, RefundOfId = expense.Id });
        }
        var before = JsonSerializer.Serialize(entries);
        var expected = entries.Where(entry => entry.Kind == EntryKind.Expense && entry.ReimbursableAmount is > 0)
            .Select(entry => (Expense: entry, Open: EntryActions.OpenReimbursement(entry, entries)))
            .Where(item => item.Open > 0).OrderBy(item => item.Expense.Date).ToArray();
        var actual = EntryActions.OpenReimbursements(entries);
        Assert.Equal(expected.Length, actual.Count);
        for (var index = 0; index < expected.Length; index++)
        { Assert.Same(expected[index].Expense, actual[index].Expense); Assert.Equal(expected[index].Open, actual[index].Open); }
        Assert.Equal(before, JsonSerializer.Serialize(entries));
    }

    /// <summary>Unrelated refund groups were never summed by the original batch and must not introduce new overflow.</summary>
    [Theory, InlineData("orphan"), InlineData("ordinary"), InlineData("zero"), InlineData("negative"), InlineData("income")]
    public void Refunds_without_an_eligible_original_expense_do_not_introduce_an_overflow(string target)
    {
        var active = Expense(100, new DateOnly(2026, 10, 1));
        var ignored = Expense(100, active.Date);
        ignored.ReimbursableAmount = target switch { "ordinary" => null, "zero" => 0, "negative" => -1, _ => 100 };
        if (target == "income") ignored.Kind = EntryKind.Income;
        var id = target == "orphan" ? Guid.NewGuid() : ignored.Id;
        var entries = new List<LedgerEntry> { active, Refund(id, long.MaxValue), Refund(id, 1) };
        if (target != "orphan") entries.Add(ignored);
        var result = Assert.Single(EntryActions.OpenReimbursements(entries));
        Assert.Same(active, result.Expense); Assert.Equal(100, result.Open);
    }

    /// <summary>Relevant groups retain the original checked refund sum in either direction.</summary>
    [Theory, InlineData(long.MaxValue, 1), InlineData(long.MinValue, -1)]
    public void Eligible_refund_sum_keeps_original_overflow_refusal(long first, long second)
    {
        var expense = Expense(100, new DateOnly(2026, 10, 1));
        LedgerEntry[] entries = [expense, Refund(expense.Id, first), Refund(expense.Id, second)];
        Assert.Throws<OverflowException>(() => EntryActions.OpenReimbursement(expense, entries));
        Assert.Throws<OverflowException>(() => EntryActions.OpenReimbursements(entries));
    }

    /// <summary>Grouping cannot reorder a group's checked additions and conceal an intermediate overflow.</summary>
    [Theory, InlineData(false), InlineData(true)]
    public void Refund_additions_retain_original_order_before_clamping(bool overflows)
    {
        var expense = Expense(100, new DateOnly(2026, 10, 1));
        LedgerEntry[] entries = [expense, Refund(expense.Id, long.MaxValue),
            Refund(expense.Id, overflows ? 1 : -long.MaxValue), Refund(expense.Id, overflows ? -long.MaxValue : 1)];
        if (overflows) Assert.Throws<OverflowException>(() => EntryActions.OpenReimbursements(entries));
        else Assert.Equal(99, Assert.Single(EntryActions.OpenReimbursements(entries)).Open);
    }

    /// <summary>Even repeated source identities retain each original request and its own amount.</summary>
    [Fact]
    public void Duplicate_expense_identity_does_not_duplicate_refund_money_or_replace_the_original_objects()
    {
        var first = Expense(100, new DateOnly(2026, 10, 1));
        var second = new LedgerEntry(first.Id) { Kind = EntryKind.Expense, ReimbursableAmount = 200, Date = first.Date };
        var actual = EntryActions.OpenReimbursements([first, second, Refund(first.Id, 30)]);
        Assert.Equal(2, actual.Count); Assert.Same(first, actual[0].Expense); Assert.Same(second, actual[1].Expense);
        Assert.Equal(70, actual[0].Open); Assert.Equal(170, actual[1].Open);
    }

    /// <summary>Creates a fictitious request without any store or application effect.</summary>
    private static LedgerEntry Expense(long requested, DateOnly date) => new()
    { Kind = EntryKind.Expense, Amount = requested, ReimbursableAmount = requested, Date = date };

    /// <summary>Creates a source refund, including deliberate invalid legacy amounts for refusal compatibility.</summary>
    private static LedgerEntry Refund(Guid expense, long amount) => new()
    { Kind = EntryKind.Refund, Amount = amount, RefundOfId = expense };

    /// <summary>Counts source visits without imposing a wall-clock threshold or duplicating financial arithmetic.</summary>
    private sealed class CountedEntries(IReadOnlyList<LedgerEntry> entries) : IReadOnlyCollection<LedgerEntry>
    {
        /// <summary>The number of original source rows actually visited.</summary>
        internal long Visits { get; private set; }
        /// <inheritdoc />
        public int Count => entries.Count;
        /// <inheritdoc />
        public IEnumerator<LedgerEntry> GetEnumerator()
        {
            foreach (var entry in entries) { Visits++; yield return entry; }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
