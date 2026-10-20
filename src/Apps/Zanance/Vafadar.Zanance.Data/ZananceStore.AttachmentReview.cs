using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Reads one original entry and its attachment metadata in a coherent actual-file transaction.</summary>
    public async Task<EntryAttachmentReview> ReadAttachmentReviewAsync(Guid entryId, CancellationToken ct = default,
        QuickResourceReview? displayed = null)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        if (displayed is not null)
        {
            displayed.TemplateChoice.Access.EnsureSnapshotCurrent();
            if (displayed.TemplateChoice.DatabasePath != DatabaseFileIdentity.ConfiguredPath(db) || displayed.SourceEntry?.Id != entryId)
                throw new InvalidOperationException("The displayed entry belongs to another attachment file.");
        }
        var access = new CommercialFileAccess(commercialAccess, DatabaseFileIdentity.ConfiguredPath(db));
        access.DemandFeature(CommercialFeature.History);
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        var inputs = await EntryAttachmentReview.ReadAsync(db, entryId, ct);
        if (displayed is not null && !displayed.MatchesSource(inputs.Entry))
            throw new InvalidOperationException("The displayed attachment entry changed before publication.");
        var review = new EntryAttachmentReview(DatabaseFileIdentity.ConfiguredPath(db), access, inputs);
        review.EnsureCurrent(); return review;
    }

    /// <summary>Reads only an attachment that still belongs to the exact original displayed entry and file.</summary>
    public async Task<EntryAttachment> ReadReviewedAttachmentAsync(EntryAttachmentReview review, Guid id, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(review);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        review.DemandFile(db);
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        review.Validate(db, await EntryAttachmentReview.ReadAsync(db, review.Inputs.Entry.Id, ct));
        var value = await db.Attachments.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id && a.EntryId == review.Inputs.Entry.Id, ct)
            ?? throw new InvalidOperationException("The original attachment is unavailable.");
        review.EnsureCurrent(); return value;
    }

    /// <summary>Adds one selected document only to its original unchanged entry, with current correction rights.</summary>
    public Task AddReviewedAttachmentAsync(EntryAttachmentReview review, EntryAttachment value, Func<bool> isCurrent,
        CancellationToken ct = default) => WriteReviewedAttachmentAsync(review, value, false, isCurrent, ct);

    /// <summary>Deletes only the exact original document bytes and metadata confirmed by the original owner.</summary>
    public Task DeleteReviewedAttachmentAsync(EntryAttachmentReview review, EntryAttachment value, Func<bool> isCurrent,
        CancellationToken ct = default) => WriteReviewedAttachmentAsync(review, value, true, isCurrent, ct);

    private async Task WriteReviewedAttachmentAsync(EntryAttachmentReview review, EntryAttachment value, bool deletion,
        Func<bool> isCurrent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(review); ArgumentNullException.ThrowIfNull(value); ArgumentNullException.ThrowIfNull(isCurrent);
        if (value.EntryId != review.Inputs.Entry.Id || value.Data.Length is 0 or > EntryAttachment.MaxBytes)
            throw new ArgumentException("The original attachment owner and bounded content are required.", nameof(value));
        var copy = value.Copy();
        static string Packet(EntryAttachment item) => System.Text.Json.JsonSerializer.Serialize(new
        { item.Id, item.EntryId, item.FileName, item.ContentType, item.CreatedAt, item.UpdatedAt,
            Size = item.Data.Length, Hash = Convert.ToHexString(SHA256.HashData(item.Data)) });
        var originalPacket = Packet(copy);
        void Current()
        {
            review.EnsureCurrent();
            if (!isCurrent() || originalPacket != Packet(value))
                throw new InvalidOperationException("The original attachment operation retired or changed.");
        }
        if (copy.EntryId != review.Inputs.Entry.Id || copy.Data.Length is 0 or > EntryAttachment.MaxBytes)
            throw new ArgumentException("The original attachment owner and bounded content are required.", nameof(value));
        review.BeginWrite(); var committed = false;
        try
        {
            Current();
            await using var db = await contextFactory.CreateDbContextAsync(ct);
            review.DemandFile(db);
            await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
            write.DemandFeature(deletion ? CommercialFeature.DeleteData : CommercialFeature.Corrections);
            review.Validate(db, await EntryAttachmentReview.ReadAsync(db, review.Inputs.Entry.Id, ct));
            var existing = await db.Attachments.SingleOrDefaultAsync(a => a.Id == copy.Id, ct);
            if (deletion)
            {
                if (existing is null || existing.EntryId != copy.EntryId || existing.FileName != copy.FileName
                    || existing.ContentType != copy.ContentType || existing.CreatedAt != copy.CreatedAt || existing.UpdatedAt != copy.UpdatedAt
                    || !CryptographicOperations.FixedTimeEquals(existing.Data, copy.Data))
                    throw new InvalidOperationException("The original confirmed attachment changed.");
                db.Attachments.Remove(existing);
            }
            else
            {
                if (existing is not null) throw new InvalidOperationException("The new attachment identity already exists.");
                db.Attachments.Add(copy);
            }
            await db.SaveChangesAsync(ct); Current(); write.EnsureCurrent();
            await write.CommitAsync(ct); committed = true;
            CommittedWriteNotification.Publish(OnChanged);
        }
        finally { review.EndWrite(committed); }
    }
}
