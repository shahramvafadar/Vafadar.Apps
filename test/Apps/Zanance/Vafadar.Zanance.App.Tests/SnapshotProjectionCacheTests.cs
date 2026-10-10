using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Filtered presentation reuse must preserve the complete ordered source and remain snapshot-local.</summary>
public sealed class SnapshotProjectionCacheTests
{
    [Fact, Trait("AT", "AT-109")]
    public void Repeated_filters_reuse_rows_without_limiting_or_reordering_the_complete_source()
    {
        var entries = Enumerable.Range(1, 100_000).Select(i => new LedgerEntry { Amount = i }).ToArray();
        var calls = 0;
        var cache = new SnapshotProjectionCache<LedgerEntry, Guid, Row>(e => e.Id, e => { calls++; return new(e.Id, e.Amount); });
        var first = entries.Select(cache.Get).ToArray();
        var filtered = entries.Where(e => e.Amount % 2 == 0).Reverse().Select(cache.Get).ToArray();
        var allAgain = entries.Select(cache.Get).ToArray();

        Assert.Equal(100_000, calls);
        Assert.Equal(entries.Select(e => e.Id), allAgain.Select(r => r.Id));
        Assert.Equal(entries.Where(e => e.Amount % 2 == 0).Reverse().Select(e => e.Id), filtered.Select(r => r.Id));
        Assert.All(first.Zip(allAgain), pair => Assert.Same(pair.First, pair.Second));
        Assert.All(entries, entry => Assert.Equal(entry.Amount, cache.Get(entry).Amount));
    }

    [Fact, Trait("AT", "AT-109")]
    public void A_fresh_snapshot_does_not_reuse_stale_amounts_or_selection_for_the_same_id()
    {
        var original = new LedgerEntry { Amount = 100 };
        var first = new SnapshotProjectionCache<LedgerEntry, Guid, Row>(e => e.Id, e => new(e.Id, e.Amount)).Get(original);
        first.Selected = true;
        var updated = original.Copy();
        updated.Amount = 200;
        var next = new SnapshotProjectionCache<LedgerEntry, Guid, Row>(e => e.Id, e => new(e.Id, e.Amount)).Get(updated);

        Assert.NotSame(first, next);
        Assert.Equal(200, next.Amount);
        Assert.False(next.Selected);
        Assert.Equal(100, original.Amount);
        Assert.True(first.Selected);
    }

    [Fact, Trait("AT", "AT-109")]
    public void A_failed_projection_is_retried_without_publishing_a_partial_cached_object()
    {
        var calls = 0;
        var entry = new LedgerEntry { Amount = 123 };
        var cache = new SnapshotProjectionCache<LedgerEntry, Guid, Row>(e => e.Id, e =>
        {
            if (++calls == 1) { throw new IOException("Fictitious presentation failure"); }
            return new(e.Id, e.Amount);
        });

        Assert.Throws<IOException>(() => cache.Get(entry));
        var row = cache.Get(entry);
        Assert.Same(row, cache.Get(entry));
        Assert.Equal(2, calls);
        Assert.Equal(123, row.Amount);
    }

    [Fact, Trait("AT", "AT-109")]
    public void Equal_values_in_different_transactions_keep_independent_selection()
    {
        var entries = new[] { new LedgerEntry { Amount = 100 }, new LedgerEntry { Amount = 100 } };
        var cache = new SnapshotProjectionCache<LedgerEntry, Guid, Row>(e => e.Id, e => new(e.Id, e.Amount));
        var first = cache.Get(entries[0]);
        var second = cache.Get(entries[1]);
        first.Selected = true;

        Assert.NotSame(first, second);
        Assert.False(second.Selected);
        Assert.True(cache.Get(entries[0]).Selected);
    }

    private sealed class Row(Guid id, long amount)
    {
        public Guid Id { get; } = id;
        public long Amount { get; } = amount;
        public bool Selected { get; set; }
    }
}
