using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Importing;

namespace Vafadar.Zanance.Data;

/// <summary>An explicit decision for a particular aggregate and the exact preview the user reviewed.</summary>
public sealed record ImportAggregateChoice(AggregateOverlap Preview, bool Link);

/// <summary>Why an overlap decision cannot be applied without a new review.</summary>
public enum ImportOverlapConflict
{
    /// <summary>Every overlap needs an explicit link or keep-both choice.</summary>
    MissingChoice,
    /// <summary>The preview no longer describes the rows that will be saved.</summary>
    ChangedPreview,
    /// <summary>A detail cannot reduce two aggregates, including a previous linked import.</summary>
    SharedDetail,
    /// <summary>A linked financial relationship needs a separate manual decision.</summary>
    LinkedAggregate,
}

/// <summary>The result of a safe import Undo; a conflict leaves every row unchanged.</summary>
public sealed record ImportUndoResult(int Removed, bool Conflict);

public sealed partial class ZananceStore
{
    /// <summary>
    /// Imports entries atomically. Existing ids are skipped. An overlapping file needs explicit choices through the
    /// overload below; no aggregate is changed or double-counted by default (IO-10/11, AT-79).
    /// </summary>
    public Task<ImportResult> ImportAsync(IReadOnlyList<LedgerEntry> entries, CancellationToken cancellationToken = default) =>
        ImportAsync(entries, [], cancellationToken);

    /// <summary>
    /// Saves all valid new rows and explicitly selected aggregate reductions in one transaction. Rechecks the entire
    /// preview against current data and records durable Undo before committing; never subtracts earlier details twice.
    /// </summary>
    public async Task<ImportResult> ImportAsync(IReadOnlyList<LedgerEntry> entries, IReadOnlyList<ImportAggregateChoice> choices,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(choices);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        write.DemandFeature(CommercialFeature.BackupRestore);
        var stored = await db.Entries.AsNoTracking().ToListAsync(cancellationToken);
        var journals = await ReadImportLinksAsync(db, cancellationToken);
        var known = stored.Select(e => e.Id).Concat(ConsumedIds(journals)).ToHashSet();
        var toAdd = entries.Where(e => !known.Contains(e.Id)).DistinctBy(e => e.Id).Select(e => e.Copy()).ToList();
        var skipped = entries.Count - toAdd.Count;
        if (toAdd.Count == 0) { return new ImportResult(null, 0, skipped, new Dictionary<Guid, IReadOnlyList<LedgerError>>()); }

        var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.Id, cancellationToken);
        var categories = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);
        var all = stored.Concat(toAdd).ToDictionary(e => e.Id);
        var refunded = stored.Where(e => e.RefundOfId is not null).GroupBy(e => e.RefundOfId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
        var errors = new Dictionary<Guid, IReadOnlyList<LedgerError>>();
        foreach (var entry in toAdd)
        {
            LedgerEntry? original = null;
            if (entry.RefundOfId is { } id && !all.TryGetValue(id, out original))
            {
                errors[entry.Id] = [LedgerError.RefundOriginalMissing];
                continue;
            }

            var otherRefunds = entry.RefundOfId is { } purchase ? refunded.GetValueOrDefault(purchase) : 0;
            var problems = LedgerValidator.Validate(entry, accounts, categories, original, otherRefunds);
            if (problems.Count > 0) { errors[entry.Id] = problems; }
            else if (entry.RefundOfId is { } counted) { refunded[counted] = checked(otherRefunds + entry.Amount); }
        }

        if (errors.Count > 0) { return new ImportResult(null, 0, skipped, errors); }
        var overlaps = AggregatedEntries.FindForImport(toAdd, stored);
        var conflict = ValidateImportChoices(overlaps, choices);
        var selected = choices.Where(c => c.Link).Select(c => c.Preview).ToList();
        if (conflict is null && selected.Any(o => !AggregatedEntries.CanLink(o.Aggregate)
                || all.Values.Any(e => e.RefundOfId == o.Aggregate.Id)))
        {
            conflict = ImportOverlapConflict.LinkedAggregate;
        }

        var previouslyLinked = journals.SelectMany(j => j.State.Adjustments).SelectMany(a => a.Detailed).Select(e => e.Id).ToHashSet();
        if (conflict is null && (selected.SelectMany(o => o.Detailed).GroupBy(e => e.Id).Any(g => g.Count() > 1)
                || selected.SelectMany(o => o.Detailed).Any(e => previouslyLinked.Contains(e.Id))))
        {
            conflict = ImportOverlapConflict.SharedDetail;
        }

        if (conflict is not null) { return new ImportResult(null, 0, skipped, errors) { Conflict = conflict }; }

        var batchId = Guid.CreateVersion7();
        var now = time.GetUtcNow();
        foreach (var entry in toAdd)
        {
            entry.ImportBatchId = batchId;
            entry.Source = EntrySource.Import;
            if (entry.CreatedAt == default) { entry.CreatedAt = now; }
        }

        var imported = toAdd.Select(e => e.Copy()).ToList();
        var adjustments = new List<ImportAggregateState>();
        foreach (var overlap in selected)
        {
            var added = toAdd.FirstOrDefault(e => e.Id == overlap.Aggregate.Id);
            var before = added is null ? stored.Single(e => e.Id == overlap.Aggregate.Id).Copy() : null;
            var source = added ?? before!;
            var after = AggregatedEntries.Replace(new AggregateOverlap(source, overlap.Detailed));
            if (added is not null)
            {
                toAdd.Remove(added);
                if (after is not null) { toAdd.Add(after); }
            }
            else if (after is not null) { db.Entries.Update(after); }
            else { db.Entries.Remove(source); }
            adjustments.Add(new ImportAggregateState(overlap.Aggregate.Id, before, after, overlap.Detailed.Select(e => e.Copy()).ToList()));
        }

        db.Entries.AddRange(toAdd);
        write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        if (adjustments.Count > 0)
        {
            // Only linked imports need extra snapshots. They contain metadata, never attachment bytes or credentials.
            var state = new ImportLinkState(1, imported, adjustments);
            db.ImportLinks.Add(new ImportLinkBatch(batchId) { StateJson = JsonSerializer.Serialize(state, ImportLinkJson.Default.ImportLinkState) });
            await db.SaveChangesAsync(cancellationToken);
        }

        await write.CommitAsync(cancellationToken);
        OnChanged();
        return new ImportResult(batchId, imported.Count, skipped, errors);
    }

    /// <summary>Returns ids of aggregates consumed by an active linked import, for idempotent own-CSV preview.</summary>
    public async Task<IReadOnlySet<Guid>> GetConsumedImportIdsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return ConsumedIds(await ReadImportLinksAsync(db, cancellationToken)).ToHashSet();
    }

    /// <summary>Returns batches including an imported aggregate that was completely replaced by details.</summary>
    public async Task<List<ImportBatchInfo>> GetImportBatchesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Entries.AsNoTracking().Where(e => e.ImportBatchId != null).ToListAsync(cancellationToken);
        var journals = await ReadImportLinksAsync(db, cancellationToken);
        rows.AddRange(journals.SelectMany(j => j.State.Imported));
        rows.AddRange(journals.SelectMany(j => j.State.Adjustments).Where(a => a.Before?.ImportBatchId is not null).Select(a => a.Before!));
        return [.. rows.Where(e => e.ImportBatchId is not null).DistinctBy(e => (e.ImportBatchId, e.Id)).GroupBy(e => e.ImportBatchId!.Value)
            .Select(g => new ImportBatchInfo(g.Key, g.Count(), g.Min(e => e.CreatedAt))).OrderByDescending(b => b.ImportedAt)];
    }

    /// <summary>Undoes a batch; throws when later edits require review. UI callers use <see cref="TryUndoImportAsync"/>.</summary>
    public async Task<int> UndoImportAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var result = await TryUndoImportAsync(batchId, cancellationToken);
        if (result.Conflict) { throw new InvalidOperationException("The import changed after its aggregate decisions; Undo requires review."); }
        return result.Removed;
    }

    /// <summary>
    /// Removes the imported details and restores original aggregates atomically, across restart. Rejects later edits,
    /// dependent refunds/imports or missing account/category identities rather than overwriting or unlinking data.
    /// </summary>
    public async Task<ImportUndoResult> TryUndoImportAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        write.DemandFeature(CommercialFeature.Corrections);
        var stored = await db.Entries.AsNoTracking().ToDictionaryAsync(e => e.Id, cancellationToken);
        var journals = await ReadImportLinksAsync(db, cancellationToken);
        var own = journals.FirstOrDefault(j => j.Batch.Id == batchId);
        var importedIds = stored.Values.Where(e => e.ImportBatchId == batchId).Select(e => e.Id)
            .Concat(own.State?.Imported.Select(e => e.Id) ?? []).ToHashSet();
        if (journals.Where(j => j.Batch.Id != batchId).Any(j => j.State.Adjustments.Any(a => a.Before?.ImportBatchId == batchId
                    || a.Detailed.Any(e => importedIds.Contains(e.Id))))
                || stored.Values.Any(e => e.RefundOfId is { } id && importedIds.Contains(id) && !importedIds.Contains(e.Id)))
        {
            return new ImportUndoResult(0, true);
        }

        if (own.State is { } state)
        {
            foreach (var change in state.Adjustments)
            {
                var id = change.AggregateId;
                if (change.After is null ? stored.ContainsKey(id) : !stored.TryGetValue(id, out var live) || !SameEntry(live, change.After))
                {
                    return new ImportUndoResult(0, true);
                }

                if (change.Detailed.Any(e => !importedIds.Contains(e.Id) && (!stored.TryGetValue(e.Id, out var detail) || !SameEntry(detail, e))))
                {
                    return new ImportUndoResult(0, true);
                }
            }

            foreach (var entry in state.Imported)
            {
                var adjustment = state.Adjustments.FirstOrDefault(a => a.AggregateId == entry.Id);
                var expected = adjustment is null ? entry : adjustment.After;
                if (expected is null ? stored.ContainsKey(entry.Id) : !stored.TryGetValue(entry.Id, out var live) || !SameEntry(live, expected))
                {
                    return new ImportUndoResult(0, true);
                }
            }

            var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(a => a.Id, cancellationToken);
            var categories = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);
            foreach (var before in state.Adjustments.Where(a => a.Before is not null).Select(a => a.Before!))
            {
                if (before.CategoryId is { } category && !categories.ContainsKey(category)
                    || LedgerValidator.Validate(before, accounts, categories, isNew: false).Count > 0) { return new ImportUndoResult(0, true); }
                if (stored.ContainsKey(before.Id)) { db.Entries.Update(before.Copy()); }
                else { db.Entries.Add(before.Copy()); }
            }

            db.ImportLinks.Remove(own.Batch);
        }

        var count = importedIds.Count;
        write.EnsureCurrent();
        await db.Entries.Where(e => e.ImportBatchId == batchId).ExecuteDeleteAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        OnChanged();
        return new ImportUndoResult(count, false);
    }

    private static ImportOverlapConflict? ValidateImportChoices(IReadOnlyList<AggregateOverlap> current, IReadOnlyList<ImportAggregateChoice> choices)
    {
        if (current.Count != choices.Count || choices.Select(c => c.Preview.Aggregate.Id).Distinct().Count() != choices.Count)
        {
            return ImportOverlapConflict.MissingChoice;
        }

        foreach (var overlap in current)
        {
            var choice = choices.FirstOrDefault(c => c.Preview.Aggregate.Id == overlap.Aggregate.Id);
            if (choice is null) { return ImportOverlapConflict.MissingChoice; }
            if (!SameEntry(overlap.Aggregate, choice.Preview.Aggregate) || overlap.Detailed.Count != choice.Preview.Detailed.Count
                    || overlap.Detailed.Any(e => !choice.Preview.Detailed.Any(d => d.Id == e.Id && SameEntry(d, e))))
            {
                return ImportOverlapConflict.ChangedPreview;
            }
        }

        return null;
    }

    private static bool SameEntry(LedgerEntry left, LedgerEntry right) => ComparableEntry(left) == ComparableEntry(right);

    private static string ComparableEntry(LedgerEntry entry)
    {
        var copy = entry.Copy();
        // Audit timestamps change when a later import is undone; semantic metadata must still match exactly.
        copy.CreatedAt = default;
        copy.UpdatedAt = default;
        return JsonSerializer.Serialize(copy, ImportLinkJson.Default.LedgerEntry);
    }

    private static IEnumerable<Guid> ConsumedIds(IEnumerable<(ImportLinkBatch Batch, ImportLinkState State)> journals) =>
        journals.SelectMany(j => j.State.Adjustments.Where(a => a.After is null).Select(a => a.AggregateId));

    private static async Task<List<(ImportLinkBatch Batch, ImportLinkState State)>> ReadImportLinksAsync(ZananceDbContext db, CancellationToken cancellationToken)
    {
        var batches = await db.ImportLinks.AsNoTracking().ToListAsync(cancellationToken);
        return batches.Select(batch =>
        {
            var state = JsonSerializer.Deserialize(batch.StateJson, ImportLinkJson.Default.ImportLinkState)
                ?? throw new InvalidOperationException("Import Undo journal is unreadable.");
            if (state.Version != 1) { throw new InvalidOperationException("Import Undo journal version is unsupported."); }
            return (batch, state);
        }).ToList();
    }
}
