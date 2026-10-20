using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>Owns one original split review across editable shares, writer waits and known committed outcomes.</summary>
public sealed class SplitEditorSession(ZananceStore store)
{
    private long _revision;
    private int _busy;
    /// <summary>Gets the unchanged financial source; editable shares remain outside it.</summary>
    public SplitReadReview? Review { get; private set; }
    /// <summary>Gets whether money committed even if the subsequent notification failed.</summary>
    public bool Committed { get; private set; }
    /// <summary>Retires pending continuations without discarding the form's raw shares.</summary>
    public void Retire() { Interlocked.Increment(ref _revision); Review = null; }

    /// <summary>Publishes only the original still-owned file/group read; callers must preserve their own drafts.</summary>
    public async Task<SplitReadReview?> LoadAsync(Guid entryId)
    {
        if (Committed || Volatile.Read(ref _busy) != 0) return null;
        Retire(); var revision = _revision;
        var review = await store.ReadSplitReviewAsync(entryId);
        if (revision != _revision) return null;
        review.EnsureCurrent(); Review = review; return review;
    }

    /// <summary>Saves exact reviewed shares or joins once; changed pending input cannot become a different accepted operation.</summary>
    public async Task<SaveResult?> SaveAsync(IReadOnlyList<(Guid? CategoryId, long Amount)> shares, bool join, Func<bool> isCurrent)
    {
        ArgumentNullException.ThrowIfNull(shares); ArgumentNullException.ThrowIfNull(isCurrent);
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return null;
        try
        {
            var review = Review; var revision = _revision;
            if (review is null || Committed) return null;
            bool Current() => revision == _revision && ReferenceEquals(review, Review) && isCurrent();
            try
            {
                var result = join ? await store.JoinReviewedSplitAsync(review, isCurrent: Current)
                    : await store.SaveReviewedSplitAsync(review, shares, isCurrent: Current);
                Committed = result.Succeeded; return result;
            }
            catch (CommittedWriteNotificationException<SaveResult>) { Committed = true; throw; }
            finally { if (Committed) Retire(); }
        }
        finally { Volatile.Write(ref _busy, 0); }
    }
}
