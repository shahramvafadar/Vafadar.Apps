using System.Collections;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Qualifies original recent-row selection without scanning or sorting the older captured ledger again.</summary>
public sealed class HomeRecentEntriesTests
{
    private static readonly DateOnly Today = new(2026, 10, 19);

    [Theory, Trait("AT", "AT-383")]
    [InlineData(0), InlineData(1), InlineData(2), InlineData(3)]
    public void A_short_eligible_history_retains_every_original_row(int count)
    {
        var entries = Enumerable.Range(0, count).Select(index => Entry(Today.AddDays(-index))).ToList();
        var recent = HomeRecentEntries.FromNewestFirst(entries, Today);
        Assert.Equal(count, recent.Count);
        for (var index = 0; index < count; index++) Assert.Same(entries[index], recent[index]);
    }

    [Fact, Trait("AT", "AT-383")]
    public void Equal_date_and_timestamp_rows_retain_the_captured_stable_order()
    {
        var entries = Enumerable.Range(0, 4).Select(_ => Entry(Today)).ToList();
        var recent = HomeRecentEntries.FromNewestFirst(entries, Today);
        Assert.Equal(entries.Take(3).Select(entry => entry.Id), recent.Select(entry => entry.Id));
        Assert.Equal(4, entries.Count);
    }

    [Fact, Trait("AT", "AT-383")]
    public void Future_prefix_is_skipped_without_losing_today_or_traversing_the_older_suffix()
    {
        LedgerEntry[] prefix = [Entry(Today.AddDays(2)), Entry(Today.AddDays(1)), Entry(Today),
            Entry(Today.AddDays(-1)), Entry(Today.AddDays(-2))];
        var entries = new BoundedHistory(prefix, 100000);
        var recent = HomeRecentEntries.FromNewestFirst(entries, Today);
        Assert.Equal(5, entries.Reads);
        Assert.Equal(prefix.Skip(2).Select(entry => entry.Id), recent.Select(entry => entry.Id));
    }

    [Fact, Trait("AT", "AT-383")]
    public void Three_current_rows_do_not_read_the_remaining_large_history()
    {
        LedgerEntry[] prefix = [Entry(Today), Entry(Today), Entry(Today.AddDays(-1))];
        var entries = new BoundedHistory(prefix, 100000);
        var recent = HomeRecentEntries.FromNewestFirst(entries, Today);
        Assert.Equal(3, entries.Reads);
        for (var index = 0; index < 3; index++) Assert.Same(prefix[index], recent[index]);
    }

    [Fact, Trait("AT", "AT-383")]
    public void A_future_only_history_does_not_publish_a_recent_row()
    {
        Assert.Empty(HomeRecentEntries.FromNewestFirst([Entry(Today.AddDays(2)), Entry(Today.AddDays(1))], Today));
    }

    [Fact, Trait("AT", "AT-383")]
    public void Entry_kind_review_and_other_metadata_do_not_filter_or_change_recent_records()
    {
        var transfer = Entry(Today); transfer.Kind = EntryKind.Transfer; transfer.Review = ReviewState.Unreviewed;
        var refund = Entry(Today); refund.Kind = EntryKind.Refund; refund.Tags.Add("fictitious-tag");
        var adjustment = Entry(Today); adjustment.Kind = EntryKind.Adjustment;
        LedgerEntry[] entries = [transfer, refund, adjustment];
        var recent = HomeRecentEntries.FromNewestFirst(entries, Today);
        Assert.Collection(recent, row => Assert.Same(transfer, row), row => Assert.Same(refund, row), row => Assert.Same(adjustment, row));
        Assert.Equal(ReviewState.Unreviewed, transfer.Review); Assert.Equal("fictitious-tag", Assert.Single(refund.Tags));
    }

    [Fact, Trait("AT", "AT-383")]
    public void Minimum_and_maximum_dates_use_the_captured_date_without_extra_calendar_arithmetic()
    {
        var oldest = Entry(DateOnly.MinValue); var newest = Entry(DateOnly.MaxValue);
        Assert.Same(oldest, Assert.Single(HomeRecentEntries.FromNewestFirst([newest, oldest], DateOnly.MinValue)));
        Assert.Same(newest, HomeRecentEntries.FromNewestFirst([newest, oldest], DateOnly.MaxValue)[0]);
    }

    private static LedgerEntry Entry(DateOnly date) => new()
    {
        AccountId = Guid.NewGuid(), Date = date, Amount = 123, Title = "Fictitious recent row",
        CreatedAt = new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.Zero)
    };

    /// <summary>Fails if selection reads beyond its eligible prefix or silently falls back to full enumeration.</summary>
    private sealed class BoundedHistory(IReadOnlyList<LedgerEntry> prefix, int count) : IReadOnlyList<LedgerEntry>
    {
        public int Reads { get; private set; }
        public int Count => count;
        public LedgerEntry this[int index]
        {
            get
            {
                if (index >= prefix.Count) throw new InvalidOperationException("Older history was read after three eligible rows.");
                Reads++; return prefix[index];
            }
        }
        public IEnumerator<LedgerEntry> GetEnumerator() => throw new InvalidOperationException("Full history enumeration is not required.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
