using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>A known original place creation, retaining its exact prior source and accepted identity for read-only adoption.</summary>
public sealed class HoldingLocationCreation
{
    /// <summary>Retains only a writer-proven accepted place and its coherent original/rebased reviews.</summary>
    internal HoldingLocationCreation(Guid id, string name, HoldingEventReadReview original, HoldingEventReadReview review)
    { Id = id; Name = name; Original = original; Review = review; }
    /// <summary>Gets the exact original accepted identity; retrying publication must never create it again.</summary>
    public Guid Id { get; }
    /// <summary>Gets the accepted trimmed name, independently of later prompt edits.</summary>
    public string Name { get; }
    /// <summary>Gets the original rebased review captured by the accepted writer.</summary>
    public HoldingEventReadReview Review { get; }
    /// <summary>Gets the prior complete source used to refuse any unrelated source change during read-only adoption.</summary>
    internal HoldingEventReadReview Original { get; }
}

public sealed partial class HoldingStore
{
    /// <summary>Creates exactly one absent place in the original reviewed file and commits its rebased review atomically.</summary>
    public Task<HoldingLocationCreation> CreateReviewedLocationAsync(HoldingEventReadReview review, Guid id,
        string name, Func<bool> isCurrent, CancellationToken ct = default) =>
        CreateReviewedLocationCoreAsync(review, id, name, isCurrent, false, ct);

    /// <summary>Creates only missing original default scaffolding under Corrections, granting no extra holding-management capability.</summary>
    public Task<HoldingLocationCreation> CreateReviewedDefaultLocationAsync(HoldingEventReadReview review, Guid id,
        string name, Func<bool> isCurrent, CancellationToken ct = default) =>
        CreateReviewedLocationCoreAsync(review, id, name, isCurrent, true, ct);

    /// <summary>Shares actual-file/owner validation while keeping explicit creation distinct from required default scaffolding.</summary>
    private async Task<HoldingLocationCreation> CreateReviewedLocationCoreAsync(HoldingEventReadReview review, Guid id,
        string name, Func<bool> isCurrent, bool missingDefault, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(name); ArgumentNullException.ThrowIfNull(isCurrent);
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(name) || name.Length > 60 || name != name.Trim())
            throw new ArgumentException("An original absent place identity and bounded trimmed name are required.");
        review.BeginWrite(); var committed = false;
        try
        {
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
            write.DemandFeature(missingDefault ? CommercialFeature.Corrections : CommercialFeature.ManageHoldings);
            var current = await HoldingEventReadReview.ReadAsync(db, review.Inputs.Type.Id, review.EventId, ct);
            review.ValidateWriter(db, current);
            if (missingDefault && current.Locations.Any(l => !l.IsArchived))
                throw new HoldingEventReviewChangedException("Only missing original default scaffolding is permitted.");
            if (!isCurrent() || current.Locations.Any(l => l.Id == id))
                throw new HoldingEventReviewChangedException("Original place prompt retired or its absent identity changed.");
            var location = new AssetLocation(id) { Name = name };
            db.AssetLocations.Add(location); write.EnsureCurrent(); await db.SaveChangesAsync(ct);
            var after = await HoldingEventReadReview.ReadAsync(db, review.Inputs.Type.Id, review.EventId, ct);
            review.ValidateAddedLocation(db, after, id);
            var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(commercialAccess, path);
            var result = new HoldingLocationCreation(id, name, review, new HoldingEventReadReview(review.EventId, path, access, after));
            write.EnsureCurrent(); result.Review.EnsureCurrent();
            if (!isCurrent()) throw new HoldingEventReviewChangedException("Original place prompt retired before commit.");
            await write.CommitAsync(ct); committed = true;
            return CommittedWriteNotification.Publish(() => Changed?.Invoke(this, EventArgs.Empty), result);
        }
        catch (CommittedWriteNotificationException<HoldingLocationCreation>) { committed = true; throw; }
        finally { review.EndWrite(committed); }
    }

    /// <summary>Reopens only the exact accepted place and unchanged original facts; no write or new identity is permitted.</summary>
    public async Task<HoldingEventReadReview> ReadCreatedLocationReviewAsync(HoldingLocationCreation created, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(created); created.Original.EnsureCurrent();
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        var current = await HoldingEventReadReview.ReadAsync(db, created.Original.Inputs.Type.Id, created.Original.EventId, ct);
        created.Original.ValidateAddedLocation(db, current, created.Id);
        var location = current.Locations.Single(l => l.Id == created.Id);
        if (location.Name != created.Name || location.IsArchived || location.SortOrder != 0)
            throw new HoldingEventReviewChangedException("The original accepted place changed; reopen before using it.");
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(commercialAccess, path);
        var result = new HoldingEventReadReview(created.Original.EventId, path, access, current);
        ct.ThrowIfCancellationRequested(); result.EnsureCurrent(); return result;
    }
}
