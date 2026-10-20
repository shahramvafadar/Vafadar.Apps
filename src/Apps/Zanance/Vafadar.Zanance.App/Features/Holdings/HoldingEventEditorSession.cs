using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>Owns one original holding editor source and accepted financial result across native effects.</summary>
public sealed class HoldingEventEditorSession(HoldingStore store)
{
    private long _revision;
    private int _busy;
    /// <summary>Gets the original immutable source/choice packet.</summary>
    public HoldingEventReadReview? Review { get; private set; }
    /// <summary>Gets whether the original financial operation is known to have committed.</summary>
    public bool Committed { get; private set; }
    /// <summary>Retires pending continuations without changing raw editor fields.</summary>
    public void Retire() { Interlocked.Increment(ref _revision); Review = null; }
    /// <summary>Loads one original event/type, refusing publication into a retired or accepted form.</summary>
    public async Task<HoldingEventReadReview?> LoadAsync(Guid typeId, Guid? eventId)
    {
        if (Committed || Volatile.Read(ref _busy) != 0) return null;
        Retire(); var revision = _revision;
        var result = await store.ReadEventReviewAsync(typeId, eventId);
        if (revision != _revision) return null;
        result.EnsureCurrent(); Review = result; return result;
    }
    /// <summary>Adopts only an explicit new location, retaining every original financial source and raw draft.</summary>
    public async Task<HoldingEventReadReview?> AddLocationAsync(Guid addedId)
    {
        var original = Review; var revision = _revision;
        if (original is null || Committed || Volatile.Read(ref _busy) != 0) return null;
        var result = await store.ReviewAddedLocationAsync(original, addedId);
        if (revision != _revision || !ReferenceEquals(original, Review)) return null;
        Review = result; return result;
    }
    /// <summary>Accepts exact reviewed event/payment/fee/price once; failures before commit retain raw drafts.</summary>
    public async Task<HoldingSaveResult?> SaveAsync(AssetEvent item, IReadOnlyList<LedgerEntry> entries, Func<bool> isCurrent)
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return null;
        try
        {
            var review = Review; var revision = _revision;
            if (review is null || Committed) return null;
            bool Current() => revision == _revision && ReferenceEquals(review, Review) && isCurrent();
            try
            {
                var result = await store.SaveReviewedEventAsync(review, item, entries, true, isCurrent: Current);
                Committed = result.Succeeded; return result;
            }
            catch (CommittedWriteNotificationException<HoldingSaveResult>) { Committed = true; throw; }
            finally { if (Committed) Retire(); }
        }
        finally { Volatile.Write(ref _busy, 0); }
    }
}
