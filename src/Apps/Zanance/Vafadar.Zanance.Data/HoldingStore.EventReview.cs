using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class HoldingStore
{
    /// <summary>Reads the original holding editor source/choices in one coherent actual-file transaction.</summary>
    public async Task<HoldingEventReadReview> ReadEventReviewAsync(Guid typeId, Guid? eventId = null, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        var access = new CommercialFileAccess(commercialAccess, path); access.EnsureSnapshotCurrent();
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        var review = new HoldingEventReadReview(eventId, path, access, await HoldingEventReadReview.ReadAsync(db, typeId, eventId, ct));
        review.EnsureCurrent(); return review;
    }

    /// <summary>Adopts one explicitly created location without refreshing away an unchanged original financial review.</summary>
    public async Task<HoldingEventReadReview> ReviewAddedLocationAsync(HoldingEventReadReview review, Guid addedId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(review); review.EnsureCurrent();
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        var access = new CommercialFileAccess(commercialAccess, path); access.EnsureSnapshotCurrent();
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        var current = await HoldingEventReadReview.ReadAsync(db, review.Inputs.Type.Id, review.EventId, ct);
        review.ValidateAddedLocation(db, current, addedId);
        return new HoldingEventReadReview(review.EventId, path, access, current);
    }

    /// <summary>Saves one reviewed exact draft under the original atomic event/payment/fee/price writer.</summary>
    public async Task<HoldingSaveResult> SaveReviewedEventAsync(HoldingEventReadReview review, AssetEvent item,
        IReadOnlyList<LedgerEntry> entries, bool recordPurchasePrice, CancellationToken ct = default, Func<bool>? isCurrent = null)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(item); ArgumentNullException.ThrowIfNull(entries);
        review.BeginWrite(); var committed = false;
        var copy = HoldingEventReadReview.CopyEvent(item); var copies = entries.Select(e => e.Copy()).ToArray();
        try
        {
            if (item.AssetTypeId != review.Inputs.Type.Id || (review.EventId is { } id ? item.Id != id : review.Inputs.Events.Any(e => e.Id == item.Id)))
                throw new HoldingEventReviewChangedException("Reviewed holding event identity changed.");
            var original = review.Inputs.Events.SingleOrDefault(e => e.Id == review.EventId);
            if (item.GroupId != original?.GroupId || original is not null && item.Kind != original.Kind)
                throw new HoldingEventReviewChangedException("Original holding event kind or group changed.");
            var result = await SaveEventCoreAsync(copy, copies, recordPurchasePrice, ct, review, isCurrent);
            committed = result.Succeeded; return result;
        }
        catch (CommittedWriteNotificationException<HoldingSaveResult>) { committed = true; throw; }
        finally { review.EndWrite(committed); }
    }
}
