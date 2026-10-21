using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>Owns the original balance review across input edits, native consent and one accepted financial write.</summary>
public sealed class AccountReconciliationSession(ZananceStore store)
{
    private long _revision;
    private int _busy;
    private Func<bool>? _reviewCurrent;
    /// <summary>Owns the observed balance's raw currency, unit and parser independently of display preferences.</summary>
    public MoneyDraftInput ObservedMoney { get; } = new();
    /// <summary>Gets the original still-current comparison; input edits remove its adjustment authority.</summary>
    public ReconciliationReadReview? Review { get; private set; }
    /// <summary>Gets whether this operation committed even if its post-commit notification failed.</summary>
    public bool Committed { get; private set; }

    /// <summary>Retires original consent after amount, date or account navigation changes, without clearing drafts.</summary>
    public void Invalidate() { Interlocked.Increment(ref _revision); Review = null; _reviewCurrent = null; }

    /// <summary>Compares the signed original raw balance without reparsing through current display preferences.</summary>
    public Task<ReconciliationReadReview?> CompareObservedAsync(Guid accountId, string currency, DateOnly date)
    {
        var input = ObservedMoney; var edit = input.Revision;
        if (input.NeedsReview(currency)) throw new InvalidOperationException("The observed balance requires complete monetary reentry.");
        var text = input.Text.Trim(); var negative = text.StartsWith('-') || text.StartsWith('−');
        if (input.Context is not { } context || !context.TryParse(negative ? text[1..] : text, out var observed))
            throw new FormatException("The original observed balance is invalid.");
        return CompareAsync(accountId, negative ? -observed : observed, date,
            () => input.Revision == edit && input.MatchesCurrent(currency), originalCurrency: currency);
    }

    /// <summary>Publishes only an unchanged original comparison; a match marks metadata under the same guarded writer.</summary>
    public async Task<ReconciliationReadReview?> CompareAsync(Guid accountId, long observed, DateOnly date, Func<bool>? isCurrent = null, string? originalCurrency = null)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return null;
        Invalidate(); Committed = false; var revision = _revision;
        try
        {
            var review = await store.ReadReconciliationAsync(accountId, observed, date);
            if (revision != _revision || isCurrent?.Invoke() == false) return null;
            if (originalCurrency is not null && !string.Equals(originalCurrency, review.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The account currency changed before its original balance comparison.");
            if (review.Result.Difference == 0)
            {
                try { await store.SaveReconciliationAsync(review, null, isCurrent: () => revision == _revision && isCurrent?.Invoke() != false); Committed = true; }
                catch (CommittedWriteNotificationException<SaveResult>) { Committed = true; throw; }
            }
            else { Review = review; _reviewCurrent = isCurrent; }
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
            var originalCurrent = _reviewCurrent;
            if (review is null || string.IsNullOrWhiteSpace(originalReason) || originalCurrent?.Invoke() == false) return null;
            if (!await confirm()) return null;
            if (revision != _revision || !ReferenceEquals(review, Review) || originalReason != reason() || originalCurrent?.Invoke() == false)
            { Invalidate(); return null; }
            try
            {
                var result = await store.SaveReconciliationAsync(review, originalReason,
                    isCurrent: () => revision == _revision && ReferenceEquals(review, Review) && originalReason == reason() && originalCurrent?.Invoke() != false);
                Committed = result.Succeeded;
                return result;
            }
            catch (CommittedWriteNotificationException<SaveResult>) { Committed = true; throw; }
            finally { if (Committed) Invalidate(); }
        }
        finally { Volatile.Write(ref _busy, 0); }
    }
}
