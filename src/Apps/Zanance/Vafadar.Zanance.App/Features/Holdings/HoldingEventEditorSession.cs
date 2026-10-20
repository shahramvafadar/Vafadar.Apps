using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>Owns one original holding editor source and accepted financial result across native effects.</summary>
public sealed class HoldingEventEditorSession(HoldingStore store, Func<string, bool>? acceptsFile = null)
{
    private long _revision;
    private int _busy;
    private readonly object _placeGate = new();
    private Task? _placeIdle;
    private Guid? _defaultPlaceId;
    private HoldingLocationCreation? _knownDefault;
    /// <summary>Gets the original immutable source/choice packet.</summary>
    public HoldingEventReadReview? Review { get; private set; }
    /// <summary>Gets whether the original financial operation is known to have committed.</summary>
    public bool Committed { get; private set; }
    /// <summary>Retires pending continuations without changing raw editor fields.</summary>
    public void Retire() { Interlocked.Increment(ref _revision); Review = null; }
    /// <summary>Loads one original event/type, refusing publication into a retired or accepted form.</summary>
    public async Task<HoldingEventReadReview?> LoadAsync(Guid typeId, Guid? eventId, string? defaultPlaceName = null, Func<bool>? isCurrent = null)
    {
        Task? place;
        lock (_placeGate) place = _placeIdle;
        // A new explicit query retires the prompt owner and waits for its metadata boundary, without polling or replay.
        if (place is not null) await place;
        if (Committed || Volatile.Read(ref _busy) != 0) return null;
        Retire(); var revision = _revision;
        var result = _knownDefault is { } knownDefault
            ? await store.ReadCreatedLocationReviewAsync(knownDefault)
            : await store.ReadEventReviewAsync(typeId, eventId, acceptsFile: acceptsFile);
        if (revision != _revision) return null;
        result.EnsureCurrent(); Review = result;
        bool Current() => revision == _revision && !Committed && (isCurrent?.Invoke() ?? true);
        if (!Current()) return null;
        if (defaultPlaceName is not null && !result.Inputs.Locations.Any(l => !l.IsArchived))
        {
            _defaultPlaceId ??= Guid.CreateVersion7(); HoldingLocationCreation? accepted;
            try { accepted = await CreateLocationCoreAsync(result, _defaultPlaceId.Value, defaultPlaceName, Current, true); }
            catch (CommittedWriteNotificationException<HoldingLocationCreation> known) { accepted = known.Result; }
            if (accepted is null) return null;
            _knownDefault = accepted;
            if (!Current()) return null;
            // Default metadata is already accepted. A failed following read must never recreate it on Retry.
            result = await store.ReadCreatedLocationReviewAsync(accepted);
            if (!Current()) return null;
            Review = result;
        }
        return result;
    }
    /// <summary>Releases temporary default readback ownership only after the original complete native publication succeeds.</summary>
    internal void CompletePublication(HoldingEventReadReview review)
    {
        if (ReferenceEquals(Review, review) && !Committed) _knownDefault = null;
    }
    /// <summary>Waits only for this original metadata boundary before replacing a routed form owner.</summary>
    internal Task WaitForPlaceAsync()
    {
        lock (_placeGate) return _placeIdle ?? Task.CompletedTask;
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
    public Task<HoldingLocationCreation?> CreateLocationAsync(HoldingEventReadReview original, Guid id, string name, Func<bool> isCurrent) =>
        CreateLocationCoreAsync(original, id, name, isCurrent, false);

    /// <summary>Keeps one metadata owner for explicit creation or missing-default scaffolding without changing financial acceptance.</summary>
    private async Task<HoldingLocationCreation?> CreateLocationCoreAsync(HoldingEventReadReview original, Guid id, string name, Func<bool> isCurrent, bool missingDefault)
    {
        var idle = BeginPlaceWork(); if (idle is null) return null;
        try
        {
            var revision = _revision;
            if (Committed || !ReferenceEquals(original, Review)) return null;
            bool Current() => revision == _revision && ReferenceEquals(original, Review) && !Committed && isCurrent();
            var result = missingDefault
                ? await store.CreateReviewedDefaultLocationAsync(original, id, name, Current)
                : await store.CreateReviewedLocationAsync(original, id, name, Current);
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
