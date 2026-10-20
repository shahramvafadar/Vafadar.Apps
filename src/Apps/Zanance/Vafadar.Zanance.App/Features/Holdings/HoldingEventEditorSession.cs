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
    private readonly object _placeGate = new();
    private Task? _placeIdle;
    /// <summary>Gets the original immutable source/choice packet.</summary>
    public HoldingEventReadReview? Review { get; private set; }
    /// <summary>Gets whether the original financial operation is known to have committed.</summary>
    public bool Committed { get; private set; }
    /// <summary>Retires pending continuations without changing raw editor fields.</summary>
    public void Retire() { Interlocked.Increment(ref _revision); Review = null; }
    /// <summary>Loads one original event/type, refusing publication into a retired or accepted form.</summary>
    public async Task<HoldingEventReadReview?> LoadAsync(Guid typeId, Guid? eventId)
    {
        Task? place;
        lock (_placeGate) place = _placeIdle;
        // A new explicit query retires the prompt owner and waits for its metadata boundary, without polling or replay.
        if (place is not null) await place;
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
    /// <summary>Creates one original place without marking the separate financial event committed.</summary>
    public async Task<HoldingLocationCreation?> CreateLocationAsync(HoldingEventReadReview original, Guid id, string name, Func<bool> isCurrent)
    {
        var idle = BeginPlaceWork(); if (idle is null) return null;
        try
        {
            var revision = _revision;
            if (Committed || !ReferenceEquals(original, Review)) return null;
            bool Current() => revision == _revision && ReferenceEquals(original, Review) && !Committed && isCurrent();
            var result = await store.CreateReviewedLocationAsync(original, id, name, Current);
            // Metadata acceptance is known even if the view departed during its change notification.
            if (Current()) Review = result.Review;
            return result;
        }
        finally { EndPlaceWork(idle); }
    }
    /// <summary>Adopts only a known accepted place through an explicit original-owner read, never another create.</summary>
    public async Task<HoldingEventReadReview?> RefreshCreatedLocationAsync(HoldingLocationCreation created, Func<bool> isCurrent)
    {
        var idle = BeginPlaceWork(); if (idle is null) return null;
        try
        {
            var revision = _revision;
            if (Committed || !isCurrent()) return null;
            var result = await store.ReadCreatedLocationReviewAsync(created);
            if (revision != _revision || Committed || !isCurrent()) return null;
            Review = result; return result;
        }
        finally { EndPlaceWork(idle); }
    }
    /// <summary>Publishes one metadata wait handle together with its ownership claim.</summary>
    private TaskCompletionSource? BeginPlaceWork()
    {
        lock (_placeGate)
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return null;
            var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _placeIdle = idle.Task; return idle;
        }
    }
    /// <summary>Releases the exact completed metadata boundary before waking a pending original query.</summary>
    private void EndPlaceWork(TaskCompletionSource idle)
    {
        lock (_placeGate) { Volatile.Write(ref _busy, 0); _placeIdle = null; }
        idle.TrySetResult();
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
