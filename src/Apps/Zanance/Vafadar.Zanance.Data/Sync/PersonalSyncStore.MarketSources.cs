using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>Original file/device/identity companion cutoff, separate from frozen financial rows and their cursors.</summary>
internal sealed record MarketSourceLocalWork(SyncLocalAuthority Authority, long After, long ThroughRowId, MarketReferenceDocument[] Outgoing);

public sealed partial class PersonalSyncStore
{
    /// <summary>Captures only accepted immutable source history under the actual still-enabled original financial binding.</summary>
    internal async Task<MarketSourceLocalWork> CaptureMarketSourcesAsync(LocalSyncPeer peer, string identity, ProfileSynchronizationKind kind, CancellationToken ct,
        long? outgoingThrough = null)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var access = Access(db);
        return await Task.Run(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var binding = await db.Set<SyncBindingRow>().AsNoTracking().SingleOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Original source synchronization binding unavailable.");
            var authority = new SyncLocalAuthority(Generation(db), binding.ProfileId, binding.DeviceId, identity, peer.BindingKey(identity), kind);
            binding = await RequireBindingAsync(db, authority, ct);
            // SQLite's owned append-only table position seals a finite local cutoff; newer native confirmations wait for the next sync.
            // It is only an operation-local bound, never a transferred identity, durable feed cursor or authorization proof.
            var cutoff = outgoingThrough ?? await db.Database.SqlQueryRaw<long>("SELECT COALESCE(MAX(rowid), 0) AS Value FROM MarketReferenceReceipts").SingleAsync(ct);
            if (cutoff < 0) throw new InvalidDataException("Original source cutoff is invalid.");
            var rows = await db.MarketReferenceReceipts.FromSqlInterpolated($"SELECT * FROM MarketReferenceReceipts WHERE rowid <= {cutoff}").AsNoTracking()
                .Where(row => !db.Set<SyncMarketSourceSend>().Any(sent => sent.DocumentId == row.Id && sent.Acknowledged))
                .OrderBy(row => row.CreatedAt).ThenBy(row => row.Id).Take(8).ToArrayAsync(ct);
            var documents = rows.Select(MarketReferenceDocument.From).ToArray();
            foreach (var document in documents)
            {
                var digest = MarketReferenceDocuments.Digest(document, (time ?? TimeProvider.System).GetUtcNow());
                var pending = await db.Set<SyncMarketSourceSend>().AsNoTracking().SingleOrDefaultAsync(row => row.DocumentId == document.Id, ct);
                if (pending is not null && pending.Digest != digest) throw new InvalidDataException("Original source continuation differs.");
            }
            access.EnsureSnapshotCurrent(); await RequireBindingAsync(db, authority, ct);
            return new MarketSourceLocalWork(authority, binding.MarketSourceCursor, cutoff, documents);
        }, ct);
    }
    /// <summary>Persists only exact known original acknowledgments; timeout/absence cannot mark a document sent.</summary>
    internal async Task AcknowledgeMarketSourcesAsync(MarketSourceLocalWork work, MarketSourceAcknowledgment acknowledgment, CancellationToken ct)
    {
        if (acknowledgment.Version != 1 || acknowledgment.DocumentIds is null ||
            !acknowledgment.DocumentIds.SequenceEqual(work.Outgoing.Select(document => document.Id)))
            throw new InvalidDataException("Original source acknowledgment differs.");
        await using var db = await factory.CreateDbContextAsync(ct); var access = Access(db);
        await using var transaction = await db.Database.BeginTransactionAsync(ct); await RequireBindingAsync(db, work.Authority, ct);
        foreach (var document in work.Outgoing)
        {
            var current = await db.MarketReferenceReceipts.AsNoTracking().SingleOrDefaultAsync(row => row.Id == document.Id, ct);
            if (current is null || MarketReferenceDocument.From(current) != document) throw new InvalidDataException("Original acknowledged source changed.");
            await MarkSourceAcknowledgedAsync(db, document, ct);
        }
        access.EnsureSnapshotCurrent(); await RequireBindingAsync(db, work.Authority, ct);
        await db.SaveChangesAsync(ct); access.EnsureSnapshotCurrent(); await transaction.CommitAsync(ct);
    }
    /// <summary>Imports only immutable source documents and their separate cursor; even an absent target cannot cause a financial write.</summary>
    internal async Task ImportMarketSourcesAsync(MarketSourceLocalWork work, MarketSourcePage page, CancellationToken ct)
    {
        if (page.Version != 1 || page.Events is null || page.Events.Length > 8 || page.Through < work.After ||
            page.More && page.Events.Length == 0 || page.Events.Any(item => item is null || item.Document is null) ||
            page.Events.Select(item => item.Document.Id).Distinct().Count() != page.Events.Length)
            throw new InvalidDataException("Original source feed is invalid.");
        var cursor = work.After;
        foreach (var item in page.Events)
        {
            if (item.Sequence <= cursor || item.Sequence > page.Through) throw new InvalidDataException("Original source feed sequence differs.");
            cursor = item.Sequence; _ = MarketReferenceDocuments.Digest(item.Document, (time ?? TimeProvider.System).GetUtcNow());
        }
        if (!page.More && cursor != page.Through) throw new InvalidDataException("Original source feed horizon differs.");
        await using var db = await factory.CreateDbContextAsync(ct); var access = Access(db);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var binding = await RequireBindingAsync(db, work.Authority, ct);
        if (binding.MarketSourceCursor != work.After) throw new InvalidOperationException("Original source feed was already advanced.");
        foreach (var item in page.Events)
        {
            var document = item.Document;
            var current = await db.MarketReferenceReceipts.AsNoTracking().SingleOrDefaultAsync(row => row.Id == document.Id, ct);
            if (current is null) db.MarketReferenceReceipts.Add(document.ToReceipt());
            else if (MarketReferenceDocument.From(current) != document) throw new InvalidDataException("Original source identity collision.");
            await MarkSourceAcknowledgedAsync(db, document, ct);
        }
        binding.MarketSourceCursor = cursor; access.EnsureSnapshotCurrent();
        await db.SaveChangesAsync(ct); access.EnsureSnapshotCurrent(); await transaction.CommitAsync(ct);
    }
    private async Task MarkSourceAcknowledgedAsync(ZananceDbContext db, MarketReferenceDocument document, CancellationToken ct)
    {
        var digest = MarketReferenceDocuments.Digest(document, (time ?? TimeProvider.System).GetUtcNow());
        var sent = await db.Set<SyncMarketSourceSend>().SingleOrDefaultAsync(row => row.DocumentId == document.Id, ct);
        if (sent is null) { sent = new() { DocumentId = document.Id, Digest = digest }; db.Add(sent); }
        if (sent.Digest != digest) throw new InvalidDataException("Original source acknowledgment collision.");
        sent.Acknowledged = true;
    }
}
