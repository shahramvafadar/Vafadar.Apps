using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

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
public sealed record HoldingGroup(IReadOnlyList<AssetEvent> Events, IReadOnlyList<LedgerEntry> Entries);

/// <summary>
/// Reads and writes quantity holdings (design §7): asset types, locations, events and valuations. A purchase or sale is
/// saved in one database transaction together with its money entry and fee (shared <c>GroupId</c>); every change is
/// checked against the full history of the holding, so no quantity is ever negative on any date (ZEX-AS15, AT16).
/// </summary>
public sealed class HoldingStore(IDbContextFactory<ZananceDbContext> contextFactory)
{
    /// <summary>Raised after holdings were written.</summary>
    public event EventHandler? Changed;

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
        var first = await db.AssetLocations.AsNoTracking().Where(l => !l.IsArchived).OrderBy(l => l.SortOrder).ThenBy(l => l.Name).FirstOrDefaultAsync(cancellationToken);
        if (first is not null)
        {
            return first;
        }

        var location = new AssetLocation { Name = name };
        db.AssetLocations.Add(location);
        await db.SaveChangesAsync(cancellationToken);
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
        var existing = await db.AssetTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == type.Id, cancellationToken);
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

        await db.SaveChangesAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Inserts or updates a location.</summary>
    public async Task SaveLocationAsync(AssetLocation location, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.AssetLocations.AnyAsync(l => l.Id == location.Id, cancellationToken))
        {
            db.AssetLocations.Update(location);
        }
        else
        {
            db.AssetLocations.Add(location);
        }

        await db.SaveChangesAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Saves a holding event together with its linked money entries (the asset purchase or sale entry and a fee
    /// expense), all in one transaction. The event and the entries get one <c>GroupId</c>; entries that belonged to the
    /// group before and are not passed again are removed. Nothing is saved when the history of the holding would become
    /// negative on any date or a money entry is invalid.
    /// </summary>
    public async Task<HoldingSaveResult> SaveEventAsync(AssetEvent assetEvent, IReadOnlyList<LedgerEntry> entries, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assetEvent);
        ArgumentNullException.ThrowIfNull(entries);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

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
        foreach (var entry in entries)
        {
            entry.GroupId = assetEvent.GroupId;
            var errors = LedgerValidator.Validate(entry, accounts, categories, isNew: !existingEntries.Contains(entry.Id));
            if (errors.Count > 0)
            {
                return new HoldingSaveResult(null, errors);
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (await db.AssetEvents.AnyAsync(e => e.Id == assetEvent.Id, cancellationToken))
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

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return HoldingSaveResult.Success;
    }

    /// <summary>
    /// Deletes an event with every entry of its group (a purchase with its payment and fee), unless the remaining history
    /// would become negative (for example deleting a purchase that a later sale needs). Returns what was removed for Undo.
    /// </summary>
    public async Task<(HoldingGroup? Removed, HoldingConflict? Conflict)> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assetEvent = await db.AssetEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (assetEvent is null)
        {
            return (null, null);
        }

        var remaining = await db.AssetEvents.AsNoTracking().Where(e => e.AssetTypeId == assetEvent.AssetTypeId && e.Id != id).ToListAsync(cancellationToken);
        if (HoldingsLedger.FindConflict(remaining) is { } conflict)
        {
            return (null, conflict);
        }

        var entries = assetEvent.GroupId is { } group
            ? await db.Entries.AsNoTracking().Where(e => e.GroupId == group).ToListAsync(cancellationToken)
            : [];
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (entries.Count > 0)
        {
            var entryIds = entries.Select(e => e.Id).ToList();
            await db.Entries.Where(e => entryIds.Contains(e.Id)).ExecuteDeleteAsync(cancellationToken);
        }

        await db.AssetEvents.Where(e => e.Id == id).ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return (new HoldingGroup([assetEvent], entries), null);
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

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.AssetTypes.AddRange(types);
        db.AssetLocations.AddRange(locations);
        db.AssetEvents.AddRange(events);
        db.AssetValuations.AddRange(valuations);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return (types.Count + locations.Count + events.Count + valuations.Count, skipped, null);
    }

    /// <summary>
    /// Converts a legacy valued-asset account into a holding (ZEX-S0408), only after the user confirmed the preview: a
    /// count type named like the account in its currency, an opening holding of one unit at <paramref name="location"/>
    /// with the balance as value and cost basis, and the account archived (it stays restorable, its entries stay).
    /// Returns the new type, or <see langword="null"/> when the account is not an active asset account.
    /// </summary>
    public async Task<AssetType?> ConvertAccountAsync(Guid accountId, long balance, DateOnly date, string location, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (account is null || account.Type != Core.Accounts.AccountType.Asset || account.IsArchived)
        {
            return null;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
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
        var known = balance > 0 ? balance : (long?)null;
        db.AssetEvents.Add(new AssetEvent
        {
            AssetTypeId = type.Id,
            LocationId = place.Id,
            Kind = AssetEventKind.Opening,
            Quantity = Quantities.PerGramOrUnit,
            Date = date,
            BasisAmount = known,
        });
        if (known is { } value)
        {
            db.AssetValuations.Add(new AssetValuation
            {
                AssetTypeId = type.Id,
                CurrencyCode = account.CurrencyCode,
                Date = date,
                PricePerUnitMilli = AssetValuationService.PricePerUnitMilliOf(value, Quantities.PerGramOrUnit),
            });
        }

        account.IsArchived = true;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
        return type;
    }

    /// <summary>Returns the asset type whose purchase or sale owns the entries of <paramref name="groupId"/>, if any.</summary>
    public async Task<Guid?> FindTypeOfGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.AssetEvents.AsNoTracking().Where(e => e.GroupId == groupId).Select(e => (Guid?)e.AssetTypeId).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Puts a deleted group back unchanged (Undo).</summary>
    public async Task RestoreAsync(HoldingGroup group, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.AssetEvents.AddRange(group.Events);
        db.Entries.AddRange(group.Entries);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Inserts or updates a valuation; a price never changes a quantity.</summary>
    public async Task SaveValuationAsync(AssetValuation valuation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(valuation);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.AssetValuations.AnyAsync(v => v.Id == valuation.Id, cancellationToken))
        {
            db.AssetValuations.Update(valuation);
        }
        else
        {
            db.AssetValuations.Add(valuation);
        }

        await db.SaveChangesAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Deletes a valuation.</summary>
    public async Task DeleteValuationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.AssetValuations.Where(v => v.Id == id).ExecuteDeleteAsync(cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Returns the category of fees, for the fee expense of a purchase or sale (ZEX-P09).</summary>
    public async Task<Guid?> FeesCategoryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Categories.AsNoTracking().Where(c => c.SystemKey == DefaultCategories.Fees && c.Kind == CategoryKind.Expense).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(cancellationToken);
    }
}
