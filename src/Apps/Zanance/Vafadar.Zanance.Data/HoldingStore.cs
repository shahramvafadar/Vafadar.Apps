using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>The outcome of saving or deleting a holding event: done, or the date and quantity that make it impossible.</summary>
/// <param name="Conflict">Where the history would become negative (design §7.5); <see langword="null"/> when saved.</param>
/// <param name="Errors">Errors of the linked money entries.</param>
public sealed record HoldingSaveResult(HoldingConflict? Conflict, IReadOnlyList<LedgerError> Errors)
{
    /// <summary>The successful result.</summary>
    public static HoldingSaveResult Success { get; } = new(null, []);

    /// <summary>Gets a value indicating whether the change was saved.</summary>
    public bool Succeeded => Conflict is null && Errors.Count == 0;
}

/// <summary>What a delete removed, so Undo can put every part back (ZEX-AS14, AT17).</summary>
public sealed record HoldingGroup(IReadOnlyList<AssetEvent> Events, IReadOnlyList<LedgerEntry> Entries)
{
    /// <summary>Gets process-local original file/dependent ownership; callers cannot manufacture Undo authority.</summary>
    internal HoldingDeletionReceipt? Receipt { get; init; }
}

/// <summary>The original deletion's removed group, or the history conflict that prevented every mutation.</summary>
public sealed record HoldingDeleteResult(HoldingGroup? Removed, HoldingConflict? Conflict);

/// <summary>
/// Reads and writes quantity holdings (design §7): asset types, locations, events and valuations. A purchase or sale is
/// saved in one database transaction together with its money entry and fee (shared <c>GroupId</c>); every change is
/// checked against the full history of the holding, so no quantity is ever negative on any date (ZEX-AS15, AT16).
/// </summary>
public sealed partial class HoldingStore(IDbContextFactory<ZananceDbContext> contextFactory, ICommercialWriteAccessSource commercialAccess)
{
    /// <summary>Raised after holdings were written.</summary>
    public event EventHandler? Changed;

    /// <summary>Publishes an already committed reviewed price without repeating its financial write.</summary>
    internal void NotifyReferenceChanged() => Changed?.Invoke(this, EventArgs.Empty);

    /// <summary>Returns the asset types, ordered for display.</summary>
    public async Task<List<AssetType>> GetTypesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.AssetTypes.AsNoTracking().OrderBy(t => t.IsArchived).ThenBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(cancellationToken);
    }

    /// <summary>Returns the locations, ordered for display.</summary>
    public async Task<List<AssetLocation>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.AssetLocations.AsNoTracking().OrderBy(l => l.IsArchived).ThenBy(l => l.SortOrder).ThenBy(l => l.Name).ToListAsync(cancellationToken);
    }

    /// <summary>Returns the events, optionally of one type, oldest first.</summary>
    public async Task<List<AssetEvent>> GetEventsAsync(Guid? assetTypeId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.AssetEvents.AsNoTracking();
        if (assetTypeId is { } id)
        {
            query = query.Where(e => e.AssetTypeId == id);
        }

        return [.. (await query.ToListAsync(cancellationToken)).OrderBy(e => e.Date).ThenBy(e => e.CreatedAt)];
    }

    /// <summary>Returns the valuations, optionally of one type.</summary>
    public async Task<List<AssetValuation>> GetValuationsAsync(Guid? assetTypeId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.AssetValuations.AsNoTracking();
        if (assetTypeId is { } id)
        {
            query = query.Where(v => v.AssetTypeId == id);
        }

        return [.. (await query.ToListAsync(cancellationToken)).OrderByDescending(v => v.Date).ThenByDescending(v => v.CreatedAt)];
    }

    /// <summary>
    /// Returns the location used by default, creating it with <paramref name="name"/> when none exists (design §7.1:
    /// a default location always exists).
    /// </summary>
    public async Task<AssetLocation> EnsureDefaultLocationAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        // D-122: default-location scaffolding also supports retained quantity corrections. It grants no new
        // holding/event/price capability; those writes check their own operation in this same store.
        write.DemandFeature(CommercialFeature.Corrections);
        var first = await db.AssetLocations.AsNoTracking().Where(l => !l.IsArchived).OrderBy(l => l.SortOrder).ThenBy(l => l.Name).FirstOrDefaultAsync(cancellationToken);
        if (first is not null)
        {
            return first;
        }

        var location = new AssetLocation { Name = name };
        db.AssetLocations.Add(location);
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return location;
    }

    /// <summary>
    /// Inserts or updates an asset type. Its dimension cannot change once events exist (grams never become coins).
    /// </summary>
    /// <returns><see langword="false"/> when the change of dimension was refused.</returns>
    public async Task<bool> SaveTypeAsync(AssetType type, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(type);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        var existing = await db.AssetTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == type.Id, cancellationToken);
        write.DemandFeature(existing is null ? CommercialFeature.ManageHoldings : CommercialFeature.Corrections);
        if (existing is null)
        {
            db.AssetTypes.Add(type);
        }
        else
        {
            if (existing.Dimension != type.Dimension && await db.AssetEvents.AnyAsync(e => e.AssetTypeId == type.Id, cancellationToken))
            {
                return false;
            }

            db.AssetTypes.Update(type);
        }

        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Inserts or updates a location.</summary>
    public async Task SaveLocationAsync(AssetLocation location, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        var existing = await db.AssetLocations.AnyAsync(l => l.Id == location.Id, cancellationToken);
        write.DemandFeature(existing ? CommercialFeature.Corrections : CommercialFeature.ManageHoldings);
        if (existing)
        {
            db.AssetLocations.Update(location);
        }
        else
        {
            db.AssetLocations.Add(location);
        }

        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Saves a holding event together with its linked money entries (the asset purchase or sale entry and a fee
    /// expense), all in one transaction. The event and the entries get one <c>GroupId</c>; entries that belonged to the
    /// group before and are not passed again are removed. Nothing is saved when the history of the holding would become
    /// negative on any date or a money entry is invalid.
    /// </summary>
    public Task<HoldingSaveResult> SaveEventAsync(AssetEvent assetEvent, IReadOnlyList<LedgerEntry> entries, CancellationToken cancellationToken = default) =>
        SaveEventAsync(assetEvent, entries, recordPurchasePrice: false, cancellationToken);

    /// <summary>
    /// Saves the event/group and, when requested by the purchase editor, its derived price in the same transaction.
    /// The price uses the actual stored type currency, never a caller-supplied manual-price entitlement bypass.
    /// </summary>
    public Task<HoldingSaveResult> SaveEventAsync(AssetEvent assetEvent, IReadOnlyList<LedgerEntry> entries,
        bool recordPurchasePrice, CancellationToken cancellationToken = default) =>
        SaveEventCoreAsync(assetEvent, entries, recordPurchasePrice, cancellationToken);

    private async Task<HoldingSaveResult> SaveEventCoreAsync(AssetEvent assetEvent, IReadOnlyList<LedgerEntry> entries,
        bool recordPurchasePrice, CancellationToken cancellationToken, HoldingEventReadReview? review = null, Func<bool>? isCurrent = null, Action? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(assetEvent);
        ArgumentNullException.ThrowIfNull(entries);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        // D-122: acquire the existing operation's writer before full-history validation as well as permissions.
        // Independent sales/corrections must not both validate the same quantity then commit a negative history.
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        void ValidatePending()
        {
            review?.EnsureCurrent();
            if (isCurrent?.Invoke() == false) throw new HoldingEventReviewChangedException("Original pending holding draft changed.");
        }
        ValidatePending();
        if (review is not null)
        {
            review.ValidateWriter(db, await HoldingEventReadReview.ReadAsync(db, review.Inputs.Type.Id, review.EventId, cancellationToken));
            var proposedIds = entries.Select(e => e.Id).ToList();
            var originalIds = review.Inputs.Entries.Select(e => e.Id).ToList();
            if (await db.Entries.AnyAsync(e => proposedIds.Contains(e.Id) && !originalIds.Contains(e.Id), cancellationToken))
                throw new HoldingEventReviewChangedException("An unreviewed existing money entry cannot join this holding event.");
        }
        var existing = await db.AssetEvents.AnyAsync(e => e.Id == assetEvent.Id, cancellationToken);
        if (review is { EventId: null } && existing)
            throw new HoldingEventReviewChangedException("An unreviewed existing holding event cannot become this new event.");
        if (write.Enforced)
        {
            var quantityCorrection = assetEvent.Kind == AssetEventKind.Correction && entries.Count == 0
                && assetEvent.GroupId is null && assetEvent.BasisAmount is null && assetEvent.ProceedsAmount is null
                && !string.IsNullOrWhiteSpace(assetEvent.Reason)
                && await db.AssetTypes.AnyAsync(t => t.Id == assetEvent.AssetTypeId, cancellationToken);
            write.DemandFeature(existing || quantityCorrection ? CommercialFeature.Corrections : CommercialFeature.ManageHoldings);
        }

        // The history as it would be after the change, for this type only.
        var others = await db.AssetEvents.AsNoTracking().Where(e => e.AssetTypeId == assetEvent.AssetTypeId && e.Id != assetEvent.Id).ToListAsync(cancellationToken);
        if (HoldingsLedger.FindConflict([.. others, assetEvent]) is { } conflict)
        {
            return new HoldingSaveResult(conflict, []);
        }

        if (entries.Count > 0)
        {
            assetEvent.GroupId ??= Guid.CreateVersion7();
        }

        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.Id, cancellationToken);
        var categories = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);
        var ids = entries.Select(e => e.Id).ToList();
        var existingEntries = await db.Entries.AsNoTracking().Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToListAsync(cancellationToken);
        var removedIds = assetEvent.GroupId is { } originalGroup
            ? await db.Entries.AsNoTracking().Where(e => e.GroupId == originalGroup && !ids.Contains(e.Id)).Select(e => e.Id).ToListAsync(cancellationToken)
            : [];
        // D-409: a holding fee correction follows the same final-refund source rules as an ordinary expense.
        var storedRefunds = await db.Entries.AsNoTracking().Where(e => e.Kind == EntryKind.Refund && e.RefundOfId != null
            && ids.Contains(e.RefundOfId.Value) && !ids.Contains(e.Id) && !removedIds.Contains(e.Id)).ToListAsync(cancellationToken);
        var finalRefunds = storedRefunds.Concat(entries.Where(e => e.Kind == EntryKind.Refund)).ToList();
        foreach (var entry in entries)
        {
            entry.GroupId = assetEvent.GroupId;
            var errors = LedgerValidator.Validate(entry, accounts, categories, isNew: !existingEntries.Contains(entry.Id));
            if (errors.Count > 0)
            {
                return new HoldingSaveResult(null, errors);
            }
            var sourceErrors = LedgerValidator.ValidateRefundSource(entry, finalRefunds, accounts);
            if (sourceErrors.Count > 0) return new HoldingSaveResult(null, sourceErrors);
        }

        if (existing)
        {
            db.AssetEvents.Update(assetEvent);
        }
        else
        {
            db.AssetEvents.Add(assetEvent);
        }

        // Entries of the group that are no longer part of it (e.g. a fee set to zero) go with the change.
        if (assetEvent.GroupId is { } group)
        {
            await db.Entries.Where(e => e.GroupId == group && !ids.Contains(e.Id)).ExecuteDeleteAsync(cancellationToken);
        }

        foreach (var entry in entries)
        {
            if (existingEntries.Contains(entry.Id))
            {
                db.Entries.Update(entry);
            }
            else
            {
                db.Entries.Add(entry);
            }
        }

        if (recordPurchasePrice && assetEvent.Kind == AssetEventKind.Purchase && assetEvent.BasisAmount is { } cost)
        {
            // D-122: an existing purchase correction may derive its corrected price even after expiry. Manual
            // price creation remains a separate paid operation. Retain the editor's existing per-type/date rule.
            var type = await db.AssetTypes.AsNoTracking().SingleAsync(t => t.Id == assetEvent.AssetTypeId, cancellationToken);
            var valuation = await db.AssetValuations.OrderByDescending(v => v.CreatedAt).FirstOrDefaultAsync(v => v.AssetTypeId == type.Id
                && v.Source == ValuationSource.Purchase && v.Date == assetEvent.Date, cancellationToken);
            if (valuation is null)
            {
                valuation = new AssetValuation { AssetTypeId = type.Id, CurrencyCode = type.PriceCurrencyCode,
                    Source = ValuationSource.Purchase, Date = assetEvent.Date };
                db.AssetValuations.Add(valuation);
            }
            valuation.PricePerUnitMilli = AssetValuationService.PricePerUnitMilliOf(cost, assetEvent.Quantity);
        }

        ValidatePending(); write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        ValidatePending(); write.EnsureCurrent();
        await write.CommitAsync(cancellationToken);
        // D-643: retain the exact accepted original draft before a following notification can deliver newer text.
        return CommittedWriteNotification.Publish(() =>
        { accepted?.Invoke(); Changed?.Invoke(this, EventArgs.Empty); }, HoldingSaveResult.Success);
    }

    /// <summary>
    /// Deletes an event with every entry of its group (a purchase with its payment and fee), unless the remaining history
    /// would become negative (for example deleting a purchase that a later sale needs). Returns what was removed for Undo.
    /// </summary>
    public async Task<(HoldingGroup? Removed, HoldingConflict? Conflict)> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await DeleteEventCoreAsync(id, cancellationToken);
        return (result.Removed, result.Conflict);
    }

    private async Task<HoldingDeleteResult> DeleteEventCoreAsync(Guid id, CancellationToken cancellationToken,
        HoldingEventReadReview? review = null, Func<bool>? isCurrent = null)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        write.DemandFeature(CommercialFeature.DeleteData);
        if (review is not null)
            review.ValidateWriter(db, await HoldingEventReadReview.ReadAsync(db, review.Inputs.Type.Id, review.EventId, cancellationToken));
        if (isCurrent?.Invoke() == false) throw new HoldingEventReviewChangedException("Original holding deletion retired.");
        var assetEvent = await db.AssetEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (assetEvent is null)
        {
            return new(null, null);
        }

        var remaining = await db.AssetEvents.AsNoTracking().Where(e => e.AssetTypeId == assetEvent.AssetTypeId && e.Id != id).ToListAsync(cancellationToken);
        if (HoldingsLedger.FindConflict(remaining) is { } conflict)
        {
            return new(null, conflict);
        }

        if (assetEvent.GroupId is { } owned && await db.AssetEvents.AnyAsync(e => e.GroupId == owned && e.Id != id, cancellationToken))
            throw new HoldingEventReviewChangedException("The original money group is shared by another holding event.");
        var entries = assetEvent.GroupId is { } group
            ? await db.Entries.AsNoTracking().Where(e => e.GroupId == group).ToListAsync(cancellationToken)
            : [];
        var removed = new HoldingGroup([HoldingEventReadReview.CopyEvent(assetEvent)], entries.Select(e => e.Copy()).ToList());
        var receipt = await HoldingDeletionReceipt.BeforeAsync(db, removed, cancellationToken);
        if (entries.Count > 0)
        {
            var entryIds = entries.Select(e => e.Id).ToList();
            await db.Entries.Where(e => entryIds.Contains(e.Id)).ExecuteDeleteAsync(cancellationToken);
        }

        await db.AssetEvents.Where(e => e.Id == id).ExecuteDeleteAsync(cancellationToken);
        write.EnsureCurrent();
        removed = removed with { Receipt = await receipt.AfterAsync(db, cancellationToken) };
        if (isCurrent?.Invoke() == false) throw new HoldingEventReviewChangedException("Original holding deletion retired before commit.");
        await write.CommitAsync(cancellationToken);
        return CommittedWriteNotification.Publish(() => Changed?.Invoke(this, EventArgs.Empty), new HoldingDeleteResult(removed, null));
    }

    /// <summary>
    /// Imports a holdings file (ZEX-S0409): types, locations, events and prices whose ids are new are added in one
    /// transaction; existing ids are skipped, so importing twice creates nothing twice. Nothing is saved when an event
    /// refers to an unknown type or location, or the history of a type would become negative on any date.
    /// </summary>
    public async Task<(int Imported, int Skipped, HoldingConflict? Conflict)> ImportAsync(HoldingsCsvContent content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        // Import preserves owned data even after downgrade; selected read-only new-work classification is ENT-03.
        write.DemandFeature(CommercialFeature.BackupRestore);
        var typeIds = (await db.AssetTypes.Select(t => t.Id).ToListAsync(cancellationToken)).ToHashSet();
        var locationIds = (await db.AssetLocations.Select(l => l.Id).ToListAsync(cancellationToken)).ToHashSet();
        var eventIds = (await db.AssetEvents.Select(e => e.Id).ToListAsync(cancellationToken)).ToHashSet();
        var valuationIds = (await db.AssetValuations.Select(v => v.Id).ToListAsync(cancellationToken)).ToHashSet();

        var types = content.Types.Where(t => !typeIds.Contains(t.Id)).DistinctBy(t => t.Id).ToList();
        var locations = content.Locations.Where(l => !locationIds.Contains(l.Id)).DistinctBy(l => l.Id).ToList();
        var events = content.Events.Where(e => !eventIds.Contains(e.Id)).DistinctBy(e => e.Id).ToList();
        var valuations = content.Valuations.Where(v => !valuationIds.Contains(v.Id)).DistinctBy(v => v.Id).ToList();
        var skipped = content.Types.Count + content.Locations.Count + content.Events.Count + content.Valuations.Count - types.Count - locations.Count - events.Count - valuations.Count;

        // The history after the import, per type, must never be negative.
        var all = await db.AssetEvents.AsNoTracking().ToListAsync(cancellationToken);
        if (HoldingsLedger.FindConflict([.. all, .. events]) is { } conflict)
        {
            return (0, skipped, conflict);
        }

        db.AssetTypes.AddRange(types);
        db.AssetLocations.AddRange(locations);
        db.AssetEvents.AddRange(events);
        db.AssetValuations.AddRange(valuations);
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        write.EnsureCurrent();
        await write.CommitAsync(cancellationToken);
        return CommittedWriteNotification.Publish(() => Changed?.Invoke(this, EventArgs.Empty),
            (types.Count + locations.Count + events.Count + valuations.Count, skipped, (HoldingConflict?)null));
    }

    /// <summary>
    /// Converts a current-file legacy account only when the supplied value exactly matches its original recorded balance.
    /// Interactive previews must retain <see cref="AccountConversionReview"/> and use the reviewed overload instead.
    /// </summary>
    public async Task<AssetType?> ConvertAccountAsync(Guid accountId, long balance, DateOnly date, string location, CancellationToken cancellationToken = default)
    {
        var review = await ReadAccountConversionAsync(accountId, date, cancellationToken);
        if (review is null) return null;
        if (review.Balance != balance) throw new AccountConversionChangedException("The supplied conversion value differs from the original balance.");
        return await ConvertAccountAsync(review, location, () => true, cancellationToken);
    }

    /// <summary>
    /// Converts only the confirmed original account/file/history into one valued unit and archives that account atomically.
    /// Original nonpositive values remain unknown; accepted results survive post-commit notification failures.
    /// </summary>
    public async Task<AssetType> ConvertAccountAsync(AccountConversionReview review, string location, Func<bool> isCurrent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(isCurrent);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var result = await Task.Run(async () =>
        {
            review.Begin(); var committed = false;
            try
            {
                review.RequireFile(db);
                await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
                write.DemandFeature(CommercialFeature.ManageHoldings);
                await review.ValidateAsync(db, cancellationToken);
                if (!isCurrent()) throw new AccountConversionChangedException("The original conversion confirmation retired.");
                var account = await db.Accounts.SingleAsync(a => a.Id == review.AccountId, cancellationToken);
                if (account.Type != Core.Accounts.AccountType.Asset || account.IsArchived)
                    throw new AccountConversionChangedException("The original account is no longer an active valued asset.");
                var place = await db.AssetLocations.Where(l => !l.IsArchived).OrderBy(l => l.SortOrder).FirstOrDefaultAsync(cancellationToken);
                if (place is null)
                {
                    place = new AssetLocation { Name = location };
                    db.AssetLocations.Add(place);
                }

                var type = new AssetType
                {
                    Name = account.Name,
                    Kind = AssetKind.Other,
                    Dimension = AssetDimension.Count,
                    PriceCurrencyCode = account.CurrencyCode,
                    SortOrder = await db.AssetTypes.CountAsync(cancellationToken),
                };
                db.AssetTypes.Add(type);
                var known = review.Balance > 0 ? review.Balance : (long?)null;
                db.AssetEvents.Add(new AssetEvent
                {
                    AssetTypeId = type.Id,
                    LocationId = place.Id,
                    Kind = AssetEventKind.Opening,
                    Quantity = Quantities.PerGramOrUnit,
                    Date = review.Date,
                    BasisAmount = known,
                });
                if (known is { } value)
                {
                    db.AssetValuations.Add(new AssetValuation
                    {
                        AssetTypeId = type.Id,
                        CurrencyCode = account.CurrencyCode,
                        Date = review.Date,
                        PricePerUnitMilli = AssetValuationService.PricePerUnitMilliOf(value, Quantities.PerGramOrUnit),
                    });
                }

                account.IsArchived = true;
                write.EnsureCurrent();
                await db.SaveChangesAsync(cancellationToken);
                await review.ValidateAsync(db, cancellationToken, ownArchiveUpdatedAt: account.UpdatedAt);
                write.EnsureCurrent();
                if (!isCurrent()) throw new AccountConversionChangedException("The original conversion retired before commit.");
                await write.CommitAsync(cancellationToken); committed = true;
                return type;
            }
            finally { review.End(committed); }
        }, cancellationToken);
        // Preserve the original caller's notification context after the worker's known commit and consumed review.
        return CommittedWriteNotification.Publish(() => Changed?.Invoke(this, EventArgs.Empty), result);
    }

    /// <summary>Returns the asset type whose purchase or sale owns the entries of <paramref name="groupId"/>, if any.</summary>
    public async Task<Guid?> FindTypeOfGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.AssetEvents.AsNoTracking().Where(e => e.GroupId == groupId).Select(e => (Guid?)e.AssetTypeId).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Puts a deleted group back unchanged (Undo).</summary>
    public Task RestoreAsync(HoldingGroup group, CancellationToken cancellationToken = default) =>
        RestoreHoldingCoreAsync(group, cancellationToken);

    private async Task RestoreHoldingCoreAsync(HoldingGroup group, CancellationToken cancellationToken, Func<bool>? isCurrent = null)
    {
        ArgumentNullException.ThrowIfNull(group);
        var receipt = group.Receipt ?? throw new HoldingEventReviewChangedException("Original file-bound holding deletion receipt required.");
        var committed = false;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        write.DemandFeature(CommercialFeature.Corrections);
        receipt.Begin(db, group);
        try
        {
            await receipt.ValidateAsync(db, cancellationToken);
            if (isCurrent?.Invoke() == false) throw new HoldingEventReviewChangedException("Original holding Undo retired.");
            var copies = receipt.Copy(group);
            var typeId = copies.Events.Single().AssetTypeId;
            var remaining = await db.AssetEvents.AsNoTracking().Where(e => e.AssetTypeId == typeId).ToListAsync(cancellationToken);
            if (HoldingsLedger.FindConflict([.. remaining, .. copies.Events]) is not null)
                throw new HoldingEventReviewChangedException("Restoring the original holding would invalidate quantity history.");
            db.AssetEvents.AddRange(copies.Events); db.Entries.AddRange(copies.Entries);
            write.EnsureCurrent();
            await db.SaveChangesAsync(cancellationToken);
            await receipt.RelinkAsync(db, cancellationToken);
            await receipt.ValidateRestoredAsync(db, group, cancellationToken);
            write.EnsureCurrent();
            if (isCurrent?.Invoke() == false) throw new HoldingEventReviewChangedException("Original holding Undo retired before commit.");
            await write.CommitAsync(cancellationToken); committed = true;
            CommittedWriteNotification.Publish(() => Changed?.Invoke(this, EventArgs.Empty));
        }
        finally { receipt.End(committed); }
    }

    /// <summary>Inserts or updates a valuation; a price never changes a quantity.</summary>
    public Task SaveValuationAsync(AssetValuation valuation, CancellationToken cancellationToken = default) =>
        SaveValuationCoreAsync(valuation, cancellationToken);

    private async Task SaveValuationCoreAsync(AssetValuation valuation, CancellationToken cancellationToken,
        HoldingPriceReview? review = null, Func<bool>? isCurrent = null, Action? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(valuation);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        if (review is not null) await review.ValidateAsync(db, cancellationToken);
        if (isCurrent?.Invoke() == false) throw new HoldingEventReviewChangedException("Original price draft retired.");
        var existing = await db.AssetValuations.AnyAsync(v => v.Id == valuation.Id, cancellationToken);
        if (review is not null && existing) throw new HoldingEventReviewChangedException("Original new price identity exists.");
        write.DemandFeature(existing ? CommercialFeature.Corrections : CommercialFeature.ManageHoldings);
        if (existing)
        {
            db.AssetValuations.Update(valuation);
        }
        else
        {
            db.AssetValuations.Add(valuation);
        }

        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        write.EnsureCurrent();
        if (isCurrent?.Invoke() == false) throw new HoldingEventReviewChangedException("Original price operation retired before commit.");
        await write.CommitAsync(cancellationToken);
        // Receipt publication belongs after the real commit and before ordinary notifications (D-644).
        CommittedWriteNotification.Publish(() => { accepted?.Invoke(); Changed?.Invoke(this, EventArgs.Empty); });
    }

    /// <summary>Deletes a valuation.</summary>
    public Task DeleteValuationAsync(Guid id, CancellationToken cancellationToken = default) =>
        DeleteValuationCoreAsync(id, cancellationToken);

    private async Task DeleteValuationCoreAsync(Guid id, CancellationToken cancellationToken,
        HoldingPriceReview? review = null, Func<bool>? isCurrent = null, Action? accepted = null)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken);
        if (review is not null) await review.ValidateAsync(db, cancellationToken);
        if (isCurrent?.Invoke() == false) throw new HoldingEventReviewChangedException("Original price confirmation retired.");
        write.DemandFeature(CommercialFeature.DeleteData);
        await db.AssetValuations.Where(v => v.Id == id).ExecuteDeleteAsync(cancellationToken);
        if (isCurrent?.Invoke() == false) throw new HoldingEventReviewChangedException("Original price operation retired before commit.");
        await write.CommitAsync(cancellationToken);
        // Receipt publication belongs after the real commit and before ordinary notifications (D-644).
        CommittedWriteNotification.Publish(() => { accepted?.Invoke(); Changed?.Invoke(this, EventArgs.Empty); });
    }

    /// <summary>Returns the category of fees, for the fee expense of a purchase or sale (ZEX-P09).</summary>
    public async Task<Guid?> FeesCategoryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Categories.AsNoTracking().Where(c => c.SystemKey == DefaultCategories.Fees && c.Kind == CategoryKind.Expense).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(cancellationToken);
    }
}
