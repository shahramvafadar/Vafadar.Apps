using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Original receipt source and cached rights bound to the actual configured file.</summary>
public sealed class ReceiptReadReview
{
    private readonly CommercialFileAccess _access;
    /// <summary>Retains the actual configured original file for destination checks.</summary>
    internal readonly string DatabasePath;
    /// <summary>Retains the complete original entry packet, independent of its audit timestamp.</summary>
    internal readonly string? EntryPacket;
    /// <summary>Retains the complete original attachment metadata and encoded content fingerprint.</summary>
    internal readonly string? AttachmentPacket;
    /// <summary>Captures original identities/content separately from transient native recognition.</summary>
    internal ReceiptReadReview(string path, CommercialFileAccess access, LedgerEntry? entry, EntryAttachment? attachment)
    {
        DatabasePath = path; _access = access; EntryId = entry?.Id; AttachmentId = attachment?.Id;
        EntryPacket = entry is null ? null : JsonSerializer.Serialize(entry);
        AttachmentPacket = attachment is null ? null : Packet(attachment);
        Attachment = attachment;
    }
    /// <summary>Gets the existing original entry, or null for a new receipt draft.</summary>
    public Guid? EntryId { get; }
    /// <summary>Gets the reviewed original attachment identity, never an attachment in another entry.</summary>
    public Guid? AttachmentId { get; }
    /// <summary>Gets the detached stored compact file; transient camera originals are never stored here.</summary>
    public EntryAttachment? Attachment { get; }
    /// <summary>Checks an editor's actual-file identity before any destination row query.</summary>
    public bool IsOriginalFile(string path) => string.Equals(DatabasePath, path, StringComparison.Ordinal);
    /// <summary>Rechecks original cached rights, including inactive-to-active changes.</summary>
    public void EnsureCurrent() { _access.EnsureSnapshotCurrent(); _access.DemandFeature(CommercialFeature.ReceiptReading); }
    /// <summary>Captures every original metadata field and full-content identity without logging file bytes.</summary>
    internal static string Packet(EntryAttachment attachment) => JsonSerializer.Serialize(new
    { attachment.Id, attachment.EntryId, attachment.FileName, attachment.ContentType, attachment.CreatedAt,
        attachment.UpdatedAt, Hash = Convert.ToHexString(SHA256.HashData(attachment.Data)) });
}

/// <summary>Original recognition authority and source validation; it never writes entries, settings or attachments.</summary>
public sealed class ReceiptWorkStore(IDbContextFactory<ZananceDbContext> factory, ICommercialWriteAccessSource source)
{
    /// <summary>Checks access before camera/picker/OCR; an existing source is read coherently with its owning expense.</summary>
    public async Task<ReceiptReadReview> CaptureAsync(Guid? entryId = null, Guid? attachmentId = null,
        CancellationToken cancellationToken = default)
    {
        if ((entryId is null) != (attachmentId is null)) throw new ArgumentException("Provide both original receipt identities.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(source, path);
        access.EnsureSnapshotCurrent(); access.DemandFeature(CommercialFeature.ReceiptReading);
        LedgerEntry? entry = null; EntryAttachment? attachment = null;
        if (entryId is not null)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            entry = await db.Entries.AsNoTracking().SingleOrDefaultAsync(item => item.Id == entryId, cancellationToken);
            attachment = await db.Attachments.AsNoTracking().SingleOrDefaultAsync(item => item.Id == attachmentId, cancellationToken);
            if (entry is null || entry.Kind != EntryKind.Expense || attachment is null || attachment.EntryId != entry.Id)
                throw new InvalidOperationException("The original receipt source is unavailable.");
        }
        var result = new ReceiptReadReview(path, access, entry, attachment); result.EnsureCurrent(); return result;
    }
    /// <summary>Rejects changed source content/metadata, existing entry or current file before late native results publish.</summary>
    public async Task ValidateAsync(ReceiptReadReview reviewed, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewed); reviewed.EnsureCurrent();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (reviewed.DatabasePath != DatabaseFileIdentity.ConfiguredPath(db))
            throw new InvalidOperationException("The original receipt file changed.");
        if (reviewed.EntryId is not null)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var entry = await db.Entries.AsNoTracking().SingleOrDefaultAsync(item => item.Id == reviewed.EntryId, cancellationToken);
            var attachment = await db.Attachments.AsNoTracking().SingleOrDefaultAsync(item => item.Id == reviewed.AttachmentId, cancellationToken);
            if (entry is null || attachment is null || JsonSerializer.Serialize(entry) != reviewed.EntryPacket
                || ReceiptReadReview.Packet(attachment) != reviewed.AttachmentPacket)
                throw new InvalidOperationException("The original receipt source changed; read it again explicitly.");
        }
        reviewed.EnsureCurrent();
    }
}
