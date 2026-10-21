using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Importing;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Reads all new coverage for one detached original editor proposal, without a date-window guess or a write.</summary>
    public async Task<EntryAggregateReview> ReadEditorAggregatesAsync(EntryEditorReadSnapshot source,
        IReadOnlyList<LedgerEntry> proposal, IReadOnlyCollection<Guid> deleted, CancellationToken ct = default)
    {
        var batch = proposal.Select(e => e.Copy()).ToArray(); var removed = deleted.ToArray();
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        source.RequireFile(db);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            await ValidateEntryEditorUnderContextAsync(source, db, ct);
            var (overlaps, unavailable) = await ReadEditorAggregateInputsAsync(db, batch, removed, ct);
            source.RequireFile(db); return new EntryAggregateReview(source, batch, removed, overlaps, unavailable);
        }, ct);
    }

    private static async Task<(IReadOnlyList<AggregateOverlap> Overlaps, IReadOnlySet<Guid> Unavailable)> ReadEditorAggregateInputsAsync(
        ZananceDbContext db, IReadOnlyList<LedgerEntry> batch, IReadOnlyCollection<Guid> removed, CancellationToken ct)
    {
        var proposalIds = batch.Select(e => e.Id).Concat(removed).ToArray();
        // Ordinary edits read aggregate definitions and their own prior rows, not the entire ledger.
        // A changed aggregate additionally needs the actual details in its complete inclusive range.
        var stored = await db.Entries.AsNoTracking().Where(e => e.IsAggregated || proposalIds.Contains(e.Id)).ToListAsync(ct);
        foreach (var aggregate in batch.Where(e => e.IsAggregated))
        {
            var from = aggregate.AggregatedFrom ?? aggregate.Date; var to = aggregate.AggregatedTo ?? aggregate.Date;
            var details = await db.Entries.AsNoTracking().Where(e => !e.IsAggregated && e.AccountId == aggregate.AccountId
                && e.Kind == aggregate.Kind && e.CategoryId == aggregate.CategoryId && e.Date >= from && e.Date <= to).ToListAsync(ct);
            stored.AddRange(details);
        }
        stored = stored.DistinctBy(e => e.Id).ToList();
        var overlaps = AggregatedEntries.FindForEditor(batch, removed, stored);
        var linked = (await ReadImportLinksAsync(db, ct)).SelectMany(j => j.State.Adjustments).SelectMany(a => a.Detailed).Select(e => e.Id).ToHashSet();
        var consumed = overlaps.Where(o => o.Remainder == 0).Select(o => o.Aggregate.Id).ToArray();
        var documents = await db.Attachments.AsNoTracking().Where(a => consumed.Contains(a.EntryId)).Select(a => a.EntryId).ToListAsync(ct);
        var aggregateIds = overlaps.Select(o => o.Aggregate.Id).ToArray();
        var refunded = await db.Entries.AsNoTracking().Where(e => e.RefundOfId != null && aggregateIds.Contains(e.RefundOfId.Value))
            .Select(e => e.RefundOfId!.Value).ToListAsync(ct);
        var unavailable = overlaps.Where(o => !AggregatedEntries.CanLink(o.Aggregate) || documents.Contains(o.Aggregate.Id)
            || refunded.Contains(o.Aggregate.Id) || batch.Any(e => e.RefundOfId == o.Aggregate.Id)
            || o.Detailed.Any(e => linked.Contains(e.Id))).Select(o => o.Aggregate.Id).ToHashSet();
        return (overlaps, unavailable);
    }

    private static async Task ValidateAppliedEditorAggregatesAsync(ZananceDbContext db, EntryAggregateReview review,
        IReadOnlyList<LedgerEntry> batch, IReadOnlyCollection<Guid> removed, CancellationToken ct)
    {
        var original = review.Overlaps.Select(o => o.Aggregate).Concat(review.Overlaps.SelectMany(o => o.Detailed))
            .DistinctBy(e => e.Id).ToArray();
        var ids = original.Select(e => e.Id).ToArray();
        var saved = await db.Entries.AsNoTracking().Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, ct);
        foreach (var row in original)
        {
            var expected = batch.FirstOrDefault(e => e.Id == row.Id) ?? (removed.Contains(row.Id) ? null : row);
            if (expected is null ? saved.ContainsKey(row.Id) : !saved.TryGetValue(row.Id, out var actual) || !SameEntry(actual, expected))
                throw new EntryEditorSourceChangedException();
        }
    }

    /// <summary>Undoes only the original accepted editor adjustment within its original short deadline.</summary>
    public async Task UndoEditorAggregateAsync(EntryAggregateUndo original, CancellationToken ct = default)
    {
        original.Begin();
        try
        {
            var result = await TryUndoImportCoreAsync(original.JournalId, ct, original);
            if (result.Conflict) throw new InvalidOperationException("The original aggregate adjustment changed; Undo requires review.");
        }
        catch (CommittedWriteNotificationException<ImportUndoResult>) when (original.Accepted) { }
        finally { original.End(original.Accepted); }
    }

    private static async Task AddEditorAggregateJournalAsync(ZananceDbContext db, Guid journalId,
        IReadOnlyList<AggregateOverlap> selected, CancellationToken ct)
    {
        var ids = selected.SelectMany(o => o.Detailed).Select(e => e.Id).Concat(selected.Select(o => o.Aggregate.Id)).Distinct().ToArray();
        var saved = await db.Entries.AsNoTracking().Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, ct);
        var changes = selected.Select(o => new ImportAggregateState(o.Aggregate.Id, o.Aggregate.Copy(),
            saved.GetValueOrDefault(o.Aggregate.Id)?.Copy(), o.Detailed.Select(e => saved[e.Id].Copy()).ToList())).ToList();
        // The existing versioned journal already carries exact adjustment snapshots in portable backups.
        // Manual rows keep their real source and have no invented import batch identity.
        db.ImportLinks.Add(new ImportLinkBatch(journalId)
        { StateJson = JsonSerializer.Serialize(new ImportLinkState(1, [], changes), ImportLinkJson.Default.ImportLinkState) });
        await db.SaveChangesAsync(ct);
    }
}
