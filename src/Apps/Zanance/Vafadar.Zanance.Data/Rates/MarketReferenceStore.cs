using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.Data.Rates;

/// <summary>Original verified household context for this in-memory review, never a reusable write grant.</summary>
internal sealed record MarketHouseholdBoundary(Guid Space, Guid Device, long Version, string Head, string Identity, string Peer);

/// <summary>One immutable original file/quote/target review, with cached rights and no live getters across native consent.</summary>
public sealed class MarketValueReview
{
    private readonly string quote;
    internal MarketValueReview(string file, MarketQuote original, MarketAppliedMapping mapping, Guid? targetId, string? before,
        CommercialFileAccess access, DateTimeOffset now, MarketHouseholdBoundary? household)
    {
        File = file; quote = JsonSerializer.Serialize(original, MarketJson.Default.MarketQuote); Mapping = mapping;
        OriginalTargetId = targetId; Before = before; Access = access; IsStale = MarketReference.IsStale(original, now);
        QuoteDigest = MarketReference.Digest(original, now);
        Household = household;
    }
    /// <summary>Original confirmation operation; an uncertain completion resumes this id before creating any replacement.</summary>
    public Guid Id { get; } = Guid.CreateVersion7();
    /// <summary>Original configured financial file, never inferred from native SQLite reporting aliases.</summary>
    public string File { get; }
    /// <summary>A fresh copy of the exact source packet; mutable caller lists cannot replace consent.</summary>
    public MarketQuote Quote => JsonSerializer.Deserialize(quote, MarketJson.Default.MarketQuote)!;
    /// <summary>Immutable original effective value/date/unit mapping.</summary>
    public MarketAppliedMapping Mapping { get; }
    /// <summary>Whether the original review explicitly observed an aged cache; a later transition needs new confirmation.</summary>
    public bool IsStale { get; }
    /// <summary>Whether native confirmation must explain replacing an existing same-date row.</summary>
    public bool ReplacesExisting => OriginalTargetId is not null;
    internal Guid? OriginalTargetId { get; }
    internal string? Before { get; }
    internal string QuoteDigest { get; }
    internal CommercialFileAccess Access { get; }
    internal MarketHouseholdBoundary? Household { get; }
    internal void EnsureCurrent() => Access.EnsureSnapshotCurrent();
}

/// <summary>Original-target reviewed reference writers and immutable source documents; lookup/cache code has no write capability.</summary>
public sealed class MarketReferenceStore(IDbContextFactory<ZananceDbContext> factory, ICommercialWriteAccessSource access, TimeProvider time)
{
    /// <summary>Raised only after the reviewed write committed; subscribers never determine whether it committed.</summary>
    public event EventHandler? Changed;
    /// <summary>Reviews one explicit currency quote as an estimated user rate on the requested date, without changing stored rows.</summary>
    public async Task<MarketValueReview> ReviewRateAsync(MarketQuote quote, CancellationToken ct = default)
    {
        var original = Freeze(quote); if (original.Symbol.Unit != MarketUnit.Currency) throw new ArgumentException("A currency-pair reference is required.");
        await using var db = await factory.CreateDbContextAsync(ct); var file = DatabaseFileIdentity.ConfiguredPath(db);
        var rights = new CommercialFileAccess(access, file); rights.EnsureSnapshotCurrent(); rights.DemandFeature(CommercialFeature.RegionalDisplay);
        return await Task.Run(async () =>
        {
            await using var write = await CommercialWriteTransaction.OpenAsync(db, access, ct);
            var existing = await db.ExchangeRates.AsNoTracking().SingleOrDefaultAsync(r => r.Date == original.Request.OnOrBefore &&
                r.FromCurrencyCode == original.Symbol.Base && r.ToCurrencyCode == original.Symbol.Currency, ct);
            rights.EnsureSnapshotCurrent();
            return new MarketValueReview(file, original, new(1, MarketReferenceTarget.ExchangeRate, original.Request.OnOrBefore,
                original.Symbol.Currency, original.Symbol.Base, original.Observation.Value, null, null, false), existing?.Id,
                existing is null ? null : MarketApplication.Fingerprint(existing), rights, time.GetUtcNow(), await HouseholdBoundaryAsync(db, ct));
        }, ct);
    }
    /// <summary>Reviews an exact original asset definition and supported source unit, without reading/sending holding quantities.</summary>
    public async Task<MarketValueReview> ReviewPriceAsync(MarketQuote quote, Guid assetTypeId, bool exactItemAcknowledged, CancellationToken ct = default)
    {
        var original = Freeze(quote); await using var db = await factory.CreateDbContextAsync(ct); var file = DatabaseFileIdentity.ConfiguredPath(db);
        var rights = new CommercialFileAccess(access, file); rights.EnsureSnapshotCurrent();
        return await Task.Run(async () =>
        {
            await using var write = await CommercialWriteTransaction.OpenAsync(db, access, ct);
            var type = await db.AssetTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Id == assetTypeId, ct) ?? throw new InvalidOperationException("Original asset type unavailable.");
            var price = MarketApplication.PricePerUnitMilli(original, type, exactItemAcknowledged, time.GetUtcNow());
            var existing = await SameDayPriceAsync(db, assetTypeId, original.Request.OnOrBefore, ct); rights.EnsureSnapshotCurrent();
            rights.DemandFeature(existing is null ? CommercialFeature.ManageHoldings : CommercialFeature.Corrections);
            return new MarketValueReview(file, original, new(1, MarketReferenceTarget.AssetPrice, original.Request.OnOrBefore,
                type.PriceCurrencyCode, null, null, price, Definition(type), exactItemAcknowledged), existing?.Id,
                existing is null ? null : MarketApplication.Fingerprint(existing, type), rights, time.GetUtcNow(), await HouseholdBoundaryAsync(db, ct));
        }, ct);
    }

    /// <summary>Commits only original consent under the actual writer; no current provider or cached quote can substitute another value.</summary>
    public async Task<MarketReferenceReceipt> ApplyAsync(MarketValueReview review, bool staleReferenceAcknowledged, Action verifyNativeAccess, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(verifyNativeAccess); review.EnsureCurrent(); verifyNativeAccess();
        var result = await Task.Run(async () =>
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            if (DatabaseFileIdentity.ConfiguredPath(db) != review.File) throw new InvalidOperationException("Original reference target file changed.");
            await using var write = await CommercialWriteTransaction.OpenAsync(db, access, ct);
            review.EnsureCurrent(); verifyNativeAccess();
            if (await HouseholdBoundaryAsync(db, ct) != review.Household)
                throw new InvalidOperationException("Original reviewed household membership changed.");
            var completed = await db.MarketReferenceReceipts.AsNoTracking().SingleOrDefaultAsync(r => r.Id == review.Id, ct);
            if (completed is not null)
            {
                if (completed.QuoteDigest != review.QuoteDigest || completed.MappingJson != Encode(review.Mapping) || !await StillMatchesAsync(db, completed, ct))
                    throw new InvalidOperationException("Original completed reference target has changed.");
                ValidateDocument(completed); review.EnsureCurrent(); write.EnsureCurrent(); verifyNativeAccess(); return completed;
            }
            var quote = review.Quote; MarketReference.Validate(quote, time.GetUtcNow());
            if (MarketReference.Digest(quote, time.GetUtcNow()) != review.QuoteDigest ||
                MarketReference.IsStale(quote, time.GetUtcNow()) && !staleReferenceAcknowledged)
                throw new InvalidOperationException("Original reference consent needs a new explicit review.");
            Guid targetId; string fingerprint;
            if (review.Mapping.Target == MarketReferenceTarget.ExchangeRate)
            {
                write.DemandFeature(CommercialFeature.RegionalDisplay);
                var existing = await db.ExchangeRates.SingleOrDefaultAsync(r => r.Date == review.Mapping.Date &&
                    r.FromCurrencyCode == review.Mapping.FromCurrency && r.ToCurrencyCode == review.Mapping.Currency, ct);
                CheckOriginal(review, existing?.Id, existing is null ? null : MarketApplication.Fingerprint(existing));
                var row = existing ?? new ExchangeRate { FromCurrencyCode = quote.Symbol.Base, ToCurrencyCode = quote.Symbol.Currency };
                row.Date = review.Mapping.Date; row.Rate = quote.Observation.Value; row.IsEstimate = true;
                if (existing is null) db.ExchangeRates.Add(row); targetId = row.Id; fingerprint = MarketApplication.Fingerprint(row);
            }
            else
            {
                var assetId = review.Mapping.Asset?.Id ?? throw new InvalidOperationException("Original asset definition required.");
                var type = await db.AssetTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Id == assetId, ct) ?? throw new InvalidOperationException("Original asset definition unavailable.");
                if (Definition(type) != review.Mapping.Asset) throw new InvalidOperationException("Original asset definition changed.");
                var price = MarketApplication.PricePerUnitMilli(quote, type, review.Mapping.ExactItemAcknowledged, time.GetUtcNow());
                if (price != review.Mapping.PricePerUnitMilli) throw new InvalidOperationException("Original reference price differs.");
                var existing = await SameDayPriceAsync(db, assetId, review.Mapping.Date, ct);
                CheckOriginal(review, existing?.Id, existing is null ? null : MarketApplication.Fingerprint(existing, type));
                write.DemandFeature(existing is null ? CommercialFeature.ManageHoldings : CommercialFeature.Corrections);
                AssetValuation row;
                if (existing is null) { row = new AssetValuation { AssetTypeId = assetId, CurrencyCode = type.PriceCurrencyCode }; db.AssetValuations.Add(row); }
                else { row = await db.AssetValuations.SingleAsync(v => v.Id == existing.Id, ct); }
                row.Date = review.Mapping.Date; row.PricePerUnitMilli = price; row.Source = ValuationSource.Manual;
                targetId = row.Id; fingerprint = MarketApplication.Fingerprint(row, type);
            }
            var duplicate = await db.MarketReferenceReceipts.AsNoTracking().Where(r => r.Target == review.Mapping.Target && r.TargetId == targetId &&
                r.TargetFingerprint == fingerprint && r.QuoteDigest == review.QuoteDigest).OrderBy(r => r.AcceptedAt).FirstOrDefaultAsync(ct);
            var receipt = duplicate ?? new MarketReferenceReceipt(review.Id) { Target = review.Mapping.Target, TargetId = targetId,
                TargetFingerprint = fingerprint, QuoteDigest = review.QuoteDigest, QuoteJson = JsonSerializer.Serialize(quote, MarketJson.Default.MarketQuote),
                MappingJson = Encode(review.Mapping), AcceptedAt = time.GetUtcNow() };
            ValidateDocument(receipt);
            if (duplicate is null) db.MarketReferenceReceipts.Add(receipt);
            review.EnsureCurrent(); write.EnsureCurrent(); verifyNativeAccess();
            await db.SaveChangesAsync(ct); review.EnsureCurrent(); verifyNativeAccess(); await write.CommitAsync(ct); return receipt;
        }, ct);
        return CommittedWriteNotification.Publish(() => Changed?.Invoke(this, EventArgs.Empty), result);
    }

    /// <summary>Returns only source documents still bound to this exact current financial value/definition, never inferred matching cache data.</summary>
    public async Task<IReadOnlyList<MarketReferenceReceipt>> CurrentAsync(MarketReferenceTarget target, Guid targetId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var rights = new CommercialFileAccess(access, DatabaseFileIdentity.ConfiguredPath(db)); rights.EnsureSnapshotCurrent();
        // Only exact current-value documents are exposed; stream history rather than allocating an unbounded list first.
        var result = new List<MarketReferenceReceipt>();
        var rows = db.MarketReferenceReceipts.AsNoTracking().Where(r => r.Target == target && r.TargetId == targetId).AsAsyncEnumerable();
        await foreach (var row in rows.WithCancellation(ct))
            if (await StillMatchesAsync(db, row, ct)) { ValidateDocument(row); result.Add(row); }
        rights.EnsureSnapshotCurrent(); result.Sort((left, right) => right.AcceptedAt.CompareTo(left.AcceptedAt));
        return result;
    }
    /// <summary>Reads a bounded page of immutable saved sources, including superseded/deleted targets; no provider/cache lookup or financial write occurs.</summary>
    public async Task<IReadOnlyList<MarketSavedSource>> HistoryAsync(int offset = 0, int count = 20, CancellationToken ct = default, string? expectedFile = null)
    {
        if (offset is < 0 or > 1000000 || count is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(offset));
        await using var db = await factory.CreateDbContextAsync(ct);
        RequireFile(db, expectedFile);
        var rights = new CommercialFileAccess(access, DatabaseFileIdentity.ConfiguredPath(db)); rights.EnsureSnapshotCurrent();
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var result = await ReadHistoryAsync(db, offset, count, ct);
            ct.ThrowIfCancellationRequested(); rights.EnsureSnapshotCurrent(); RequireFile(db, expectedFile); return result;
        }, ct);
    }
    /// <summary>Reads complete original local asset definitions and bounded history from the captured actual file in one worker transaction.</summary>
    public async Task<MarketWorkspaceSnapshot> ReadWorkspaceAsync(string expectedFile, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(expectedFile);
        await using var db = await factory.CreateDbContextAsync(ct); RequireFile(db, expectedFile);
        var rights = new CommercialFileAccess(access, expectedFile); rights.EnsureSnapshotCurrent();
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var types = await db.AssetTypes.AsNoTracking().ToArrayAsync(ct);
            var history = await ReadHistoryAsync(db, 0, 20, ct);
            ct.ThrowIfCancellationRequested(); rights.EnsureSnapshotCurrent(); RequireFile(db, expectedFile);
            return new MarketWorkspaceSnapshot(types, history);
        }, ct);
    }
    private static void RequireFile(ZananceDbContext db, string? expectedFile)
    {
        if (expectedFile is not null && DatabaseFileIdentity.ConfiguredPath(db) != expectedFile)
            throw new InvalidOperationException("The original reference workspace file changed.");
    }
    private async Task<IReadOnlyList<MarketSavedSource>> ReadHistoryAsync(ZananceDbContext db, int offset, int count, CancellationToken ct)
    {
        var rows = await db.MarketReferenceReceipts.AsNoTracking().OrderByDescending(row => row.AcceptedAt).ThenByDescending(row => row.Id)
            .Skip(offset).Take(count).ToArrayAsync(ct);
        var result = new List<MarketSavedSource>(rows.Length);
        foreach (var row in rows) { ValidateDocument(row); result.Add(new(MarketReferenceDocument.From(row), await StillMatchesAsync(db, row, ct))); }
        return result;
    }
    private MarketQuote Freeze(MarketQuote quote)
    {
        MarketReference.Validate(quote, time.GetUtcNow());
        return JsonSerializer.Deserialize(JsonSerializer.SerializeToUtf8Bytes(quote, MarketJson.Default.MarketQuote), MarketJson.Default.MarketQuote)!;
    }
    private void ValidateDocument(MarketReferenceReceipt row) =>
        MarketReferenceDocuments.Validate(MarketReferenceDocument.From(row), time.GetUtcNow());
    private static async Task<bool> StillMatchesAsync(ZananceDbContext db, MarketReferenceReceipt receipt, CancellationToken ct)
    {
        if (receipt.Target == MarketReferenceTarget.ExchangeRate)
        { var row = await db.ExchangeRates.AsNoTracking().SingleOrDefaultAsync(r => r.Id == receipt.TargetId, ct); return row is not null && MarketApplication.Fingerprint(row) == receipt.TargetFingerprint; }
        if (receipt.Target != MarketReferenceTarget.AssetPrice) return false;
        var price = await db.AssetValuations.AsNoTracking().SingleOrDefaultAsync(v => v.Id == receipt.TargetId, ct); if (price is null) return false;
        var type = await db.AssetTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Id == price.AssetTypeId, ct);
        return type is not null && MarketApplication.Fingerprint(price, type) == receipt.TargetFingerprint;
    }
    private static void CheckOriginal(MarketValueReview review, Guid? id, string? fingerprint)
    { if (review.OriginalTargetId != id || review.Before != fingerprint) throw new InvalidOperationException("Original same-date rate or price changed; review again."); }
    private static async Task<AssetValuation?> SameDayPriceAsync(ZananceDbContext db, Guid typeId, DateOnly date, CancellationToken ct) =>
        AssetValuationService.PriceAt(await db.AssetValuations.AsNoTracking().Where(v => v.AssetTypeId == typeId && v.Date == date).ToListAsync(ct), typeId, date);
    private static MarketAssetDefinition Definition(AssetType type) => new(type.Id, type.Name, type.Kind, type.Dimension, type.Metal,
        type.PurityPer10000, type.UnitWeightMg, type.CountUnitName, type.Divisible, type.PriceCurrencyCode);
    private static string Encode(MarketAppliedMapping mapping) => JsonSerializer.Serialize(mapping, MarketMappingJson.Default.MarketAppliedMapping);
    private static async Task<MarketHouseholdBoundary?> HouseholdBoundaryAsync(ZananceDbContext db, CancellationToken ct)
    {
        var row = await db.Set<HouseholdLocalBinding>().AsNoTracking().SingleOrDefaultAsync(ct);
        return row is null ? null : new(row.SpaceId, row.DeviceId, row.MembershipVersion, row.MembershipHead, row.IdentityId, row.PeerKey);
    }
}

/// <summary>Historical source with an exact present-value comparison, never authority to recreate a missing financial target.</summary>
public sealed record MarketSavedSource(MarketReferenceDocument Document, bool MatchesCurrent)
{
    /// <inheritdoc />
    public override string ToString() => nameof(MarketSavedSource);
}

/// <summary>One original local reference workspace read, independent of private cache/network delivery and financial write consent.</summary>
public sealed record MarketWorkspaceSnapshot(IReadOnlyList<AssetType> Assets, IReadOnlyList<MarketSavedSource> History);
