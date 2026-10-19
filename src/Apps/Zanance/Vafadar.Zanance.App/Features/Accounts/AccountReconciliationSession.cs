using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>Owns the original balance review across input edits, native consent and one accepted financial write.</summary>
public sealed class AccountReconciliationSession(ZananceStore store)
{
    private long _revision;
    private int _busy;
    /// <summary>Gets the original still-current comparison; input edits remove its adjustment authority.</summary>
    public ReconciliationReadReview? Review { get; private set; }
    /// <summary>Gets whether this operation committed even if its post-commit notification failed.</summary>
    public bool Committed { get; private set; }

    /// <summary>Retires original consent after amount, date or account navigation changes, without clearing drafts.</summary>
    public void Invalidate() { Interlocked.Increment(ref _revision); Review = null; }

    /// <summary>Publishes only an unchanged original comparison; a match marks metadata under the same guarded writer.</summary>
    public async Task<ReconciliationReadReview?> CompareAsync(Guid accountId, long observed, DateOnly date)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return null;
        Invalidate(); Committed = false; var revision = _revision;
        try
        {
            var review = await store.ReadReconciliationAsync(accountId, observed, date);
            if (revision != _revision) return null;
            if (review.Result.Difference == 0)
            {
                try { await store.SaveReconciliationAsync(review, null, isCurrent: () => revision == _revision); Committed = true; }
                catch (CommittedWriteNotificationException<SaveResult>) { Committed = true; throw; }
            }
            else Review = review;
            return review;
        }
        finally { Volatile.Write(ref _busy, 0); }
    }

    /// <summary>Confirms the exact review/reason; cancellation and changed pending drafts never write money.</summary>
    public async Task<SaveResult?> AdjustAsync(Func<Task<bool>> confirm, Func<string> reason)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return null;
        Committed = false;
        try
        {
            var review = Review; var revision = _revision; var originalReason = reason();
            if (review is null || string.IsNullOrWhiteSpace(originalReason)) return null;
            if (!await confirm()) return null;
            if (revision != _revision || !ReferenceEquals(review, Review) || originalReason != reason())
            { Invalidate(); return null; }
            try
            {
                var result = await store.SaveReconciliationAsync(review, originalReason,
                    isCurrent: () => revision == _revision && ReferenceEquals(review, Review) && originalReason == reason());
                Committed = result.Succeeded;
                return result;
            }
            catch (CommittedWriteNotificationException<SaveResult>) { Committed = true; throw; }
            finally { if (Committed) Invalidate(); }
        }
        finally { Volatile.Write(ref _busy, 0); }
    }
}
