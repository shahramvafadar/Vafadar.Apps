using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Rates;

/// <summary>Persists only completely verified signed companion content inside its original household transaction, never a financial row.</summary>
internal static class VerifiedMarketSourcePersistence
{
    internal static async Task AcceptAsync(ZananceDbContext db, MarketReferenceDocument document, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Original verified source transaction required.");
        var now = db.WorkClock.GetUtcNow(); var digest = MarketReferenceDocuments.Digest(document, now);
        var original = await db.MarketReferenceReceipts.AsNoTracking().SingleOrDefaultAsync(row => row.Id == document.Id, ct);
        if (original is not null && MarketReferenceDocument.From(original) != document) throw new InvalidDataException("Original signed source identity differs.");
        if (original is null)
        {
            // DateTimeOffset uses the model's UTC-ticks convention. Exact metadata-only SQL cannot flush an unrelated tracked financial edit.
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO MarketReferenceReceipts (Id, Target, TargetId, TargetFingerprint, QuoteDigest, QuoteJson, MappingJson, AcceptedAt, CreatedAt, UpdatedAt)
                VALUES ({document.Id}, {(int)document.Target}, {document.TargetId}, {document.TargetFingerprint}, {document.QuoteDigest},
                    {document.QuoteJson}, {document.MappingJson}, {document.AcceptedAt.UtcTicks}, {now.UtcTicks}, {now.UtcTicks})
                """, ct);
        }
        var sent = await db.Set<SyncMarketSourceSend>().AsNoTracking().SingleOrDefaultAsync(row => row.DocumentId == document.Id, ct);
        if (sent is not null && sent.Digest != digest) throw new InvalidDataException("Original signed source continuation differs.");
        if (sent is null)
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO SyncMarketSourcesSent (DocumentId, Digest, Acknowledged, Epoch, MembershipVersion)
                VALUES ({document.Id}, {digest}, 1, 0, 0)
                """, ct);
        else if (!sent.Acknowledged)
            await db.Set<SyncMarketSourceSend>().Where(row => row.DocumentId == document.Id && row.Digest == digest)
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.Acknowledged, true), ct);
    }
}
