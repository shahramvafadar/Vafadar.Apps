using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Original group, dependent refunds and displayed account/category choices within one SQLite snapshot.</summary>
public sealed record SplitReviewInputs(List<LedgerEntry> Parts, List<LedgerEntry> Paybacks,
    List<Account> Accounts, List<Category> Categories, List<SplitAttachmentReview> Attachments);

/// <summary>Original attachment ownership/metadata without copying document contents into an editor review.</summary>
public sealed record SplitAttachmentReview(Guid Id, Guid EntryId, string FileName, string ContentType,
    int Size, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

/// <summary>The original split or join source changed; retain the draft and reopen before another financial write.</summary>
public sealed class SplitReviewChangedException(string message) : InvalidOperationException(message);

/// <summary>One original file-bound split review; editable shares never replace its retained source facts.</summary>
public sealed class SplitReadReview
{
    private readonly string _path, _packet;
    private readonly CommercialFileAccess _access;
    private int _state;

    /// <summary>Captures original immutable serialized facts before publishing editable parts.</summary>
    internal SplitReadReview(Guid entryId, string path, CommercialFileAccess access, SplitReviewInputs inputs)
    { EntryId = entryId; _path = path; _access = access; Inputs = inputs; _packet = Packet(inputs); }
    /// <summary>Gets the originally selected entry, even when the group order later changes.</summary>
    public Guid EntryId { get; }
    /// <summary>Gets original sources; changing any of them invalidates this review.</summary>
    public SplitReviewInputs Inputs { get; }
    /// <summary>Rejects changed in-memory sources or retired file access.</summary>
    public void EnsureCurrent()
    {
        _access.EnsureSnapshotCurrent();
        if (_packet != Packet(Inputs)) throw new SplitReviewChangedException("The original split review changed.");
    }
    /// <summary>Prevents concurrent or repeated writes of one reviewed operation.</summary>
    internal void BeginWrite()
    {
        EnsureCurrent();
        if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            throw new SplitReviewChangedException("The original split review is already being saved or consumed.");
    }
    /// <summary>Consumes accepted writes, including committed notification failures, while retaining failed drafts.</summary>
    internal void EndWrite(bool committed) => Volatile.Write(ref _state, committed ? 2 : 0);
    /// <summary>Checks the original complete group and financial dependencies under the actual writer.</summary>
    internal void ValidateWriter(ZananceDbContext db, SplitReviewInputs current)
    {
        EnsureCurrent();
        if (_path != DatabaseFileIdentity.ConfiguredPath(db) || _packet != Packet(current))
            throw new SplitReviewChangedException("The original split source changed; reopen before saving.");
    }
    /// <summary>Reads only the selected group and its dependents, with deterministic source ordering.</summary>
    internal static async Task<SplitReviewInputs> ReadAsync(ZananceDbContext db, Guid id, CancellationToken ct)
    {
        var entry = await db.Entries.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new SplitReviewChangedException("The original split entry is no longer available.");
        var parts = entry.GroupId is { } group
            ? await db.Entries.AsNoTracking().Where(e => e.GroupId == group).OrderBy(e => e.Id).ToListAsync(ct)
            : [entry];
        var ids = parts.Select(e => e.Id).ToList();
        return new(parts, await db.Entries.AsNoTracking().Where(e => e.RefundOfId != null && ids.Contains(e.RefundOfId.Value)).OrderBy(e => e.Id).ToListAsync(ct),
            await db.Accounts.AsNoTracking().Where(a => a.Id == entry.AccountId).OrderBy(a => a.Id).ToListAsync(ct),
            await db.Categories.AsNoTracking().OrderBy(c => c.Id).ToListAsync(ct),
            await db.Attachments.AsNoTracking().Where(a => ids.Contains(a.EntryId)).OrderBy(a => a.Id)
                .Select(a => new SplitAttachmentReview(a.Id, a.EntryId, a.FileName, a.ContentType, a.Data.Length, a.CreatedAt, a.UpdatedAt)).ToListAsync(ct));
    }
    private static string Packet(SplitReviewInputs inputs) => JsonSerializer.Serialize(inputs, SplitReviewJson.Default.SplitReviewInputs);
}

/// <summary>Explicit trim/AOT metadata for original split inputs; no stored schema change.</summary>
[JsonSerializable(typeof(SplitReviewInputs))]
internal sealed partial class SplitReviewJson : JsonSerializerContext;
