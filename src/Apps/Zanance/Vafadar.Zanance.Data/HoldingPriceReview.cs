using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>The original displayed type, quantity history, prices and locations in one actual-file read.</summary>
public sealed record HoldingPriceInputs(AssetType Type, List<AssetEvent> Events,
    List<AssetValuation> Valuations, List<AssetLocation> Locations);

/// <summary>Owns one original price review; a successful write consumes it before publication.</summary>
public sealed class HoldingPriceReview
{
    private readonly string _path, _packet;
    private readonly CommercialFileAccess _access;
    private int _state;
    /// <summary>Captures original actual-file identity and immutable serialization before publication.</summary>
    internal HoldingPriceReview(string path, CommercialFileAccess access, HoldingPriceInputs inputs)
    { _path = path; _access = access; Inputs = inputs; _packet = Packet(inputs); }
    /// <summary>Gets the original facts; mutating them invalidates the review.</summary>
    public HoldingPriceInputs Inputs { get; }
    /// <summary>Claims exactly one original writer, refusing concurrency and consumed packets.</summary>
    internal void Begin()
    {
        _access.EnsureSnapshotCurrent();
        if (_packet != Packet(Inputs) || Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            throw new HoldingEventReviewChangedException("Original price review changed or was consumed.");
    }
    /// <summary>Retains consumed state after a known commit, including failed notifications.</summary>
    internal void End(bool committed) => Volatile.Write(ref _state, committed ? 2 : 0);
    /// <summary>Rechecks the original packet under the already captured actual SQLite writer.</summary>
    internal async Task ValidateAsync(ZananceDbContext db, CancellationToken ct)
    {
        _access.EnsureSnapshotCurrent();
        if (_path != DatabaseFileIdentity.ConfiguredPath(db) || _packet != Packet(Inputs)
            || _packet != Packet(await ReadAsync(db, Inputs.Type.Id, ct)))
            throw new HoldingEventReviewChangedException("Original displayed price facts changed; refresh explicitly.");
    }
    private static string Packet(HoldingPriceInputs inputs) => JsonSerializer.Serialize(inputs, HoldingPriceJson.Default.HoldingPriceInputs);
    /// <summary>Reads deterministic original price facts using the caller's existing read/write transaction.</summary>
    internal static async Task<HoldingPriceInputs> ReadAsync(ZananceDbContext db, Guid id, CancellationToken ct) => new(
        await db.AssetTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new HoldingEventReviewChangedException("Original holding type is unavailable."),
        await db.AssetEvents.AsNoTracking().Where(e => e.AssetTypeId == id).OrderBy(e => e.Id).ToListAsync(ct),
        await db.AssetValuations.AsNoTracking().Where(v => v.AssetTypeId == id).OrderBy(v => v.Id).ToListAsync(ct),
        await db.AssetLocations.AsNoTracking().OrderBy(l => l.Id).ToListAsync(ct));
}

/// <summary>Explicit price-review serialization metadata for trimmed native callers.</summary>
[JsonSerializable(typeof(HoldingPriceInputs))]
internal sealed partial class HoldingPriceJson : JsonSerializerContext;

public sealed partial class HoldingStore
{
    /// <summary>Captures the original displayed valuation facts coherently, without writer authorization.</summary>
    public async Task<HoldingPriceReview> ReadPriceReviewAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        var access = new CommercialFileAccess(commercialAccess, path); access.EnsureSnapshotCurrent();
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        var result = new HoldingPriceReview(path, access, await HoldingPriceReview.ReadAsync(db, id, ct));
        access.EnsureSnapshotCurrent(); return result;
    }

    /// <summary>Archives only the confirmed original type; never writes a retained whole-type copy.</summary>
    public async Task ArchiveReviewedTypeAsync(HoldingPriceReview review, Func<bool> isCurrent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(isCurrent);
        review.Begin(); var committed = false;
        try
        {
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
            write.DemandFeature(Vafadar.Zanance.Core.Commerce.CommercialFeature.Corrections);
            await review.ValidateAsync(db, ct);
            if (review.Inputs.Type.IsArchived || !isCurrent())
                throw new HoldingEventReviewChangedException("Original archive confirmation retired or type already archived.");
            var type = await db.AssetTypes.SingleAsync(t => t.Id == review.Inputs.Type.Id, ct);
            // Update only archive state on the actual writer's current entity; history and identity stay intact.
            type.IsArchived = true;
            write.EnsureCurrent(); await db.SaveChangesAsync(ct); write.EnsureCurrent();
            if (!isCurrent()) throw new HoldingEventReviewChangedException("Original archive retired before commit.");
            await write.CommitAsync(ct); committed = true;
            CommittedWriteNotification.Publish(() => Changed?.Invoke(this, EventArgs.Empty));
        }
        catch (CommittedWriteNotificationException) { committed = true; throw; }
        finally { review.End(committed); }
    }

    /// <summary>Saves exactly one new manual price against its original displayed facts and current draft.</summary>
    public async Task SaveReviewedValuationAsync(HoldingPriceReview review, AssetValuation valuation,
        Func<bool> isCurrent, CancellationToken ct = default, Action? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(valuation); ArgumentNullException.ThrowIfNull(isCurrent);
        review.Begin(); var committed = false;
        try
        {
            if (valuation.AssetTypeId != review.Inputs.Type.Id || valuation.CurrencyCode != review.Inputs.Type.PriceCurrencyCode
                || valuation.Source != ValuationSource.Manual || review.Inputs.Valuations.Any(v => v.Id == valuation.Id))
                throw new HoldingEventReviewChangedException("Original manual price identity changed.");
            var copy = new AssetValuation(valuation.Id) { AssetTypeId = valuation.AssetTypeId, Date = valuation.Date,
                CurrencyCode = valuation.CurrencyCode, PricePerUnitMilli = valuation.PricePerUnitMilli,
                Source = valuation.Source, Note = valuation.Note, CreatedAt = valuation.CreatedAt, UpdatedAt = valuation.UpdatedAt };
            await SaveValuationCoreAsync(copy, ct, review, isCurrent, accepted); committed = true;
        }
        catch (CommittedWriteNotificationException) { committed = true; throw; }
        finally { review.End(committed); }
    }

    /// <summary>Deletes only a price still equal to the original confirmed row and original displayed history.</summary>
    public async Task DeleteReviewedValuationAsync(HoldingPriceReview review, Guid id, Func<bool> isCurrent, CancellationToken ct = default, Action? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(isCurrent);
        review.Begin(); var committed = false;
        try
        {
            if (!review.Inputs.Valuations.Any(v => v.Id == id))
                throw new HoldingEventReviewChangedException("Confirmed price is outside the original review.");
            await DeleteValuationCoreAsync(id, ct, review, isCurrent, accepted); committed = true;
        }
        catch (CommittedWriteNotificationException) { committed = true; throw; }
        finally { review.End(committed); }
    }
}
