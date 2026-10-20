using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Original entry and attachment metadata, without loading document bytes for a list.</summary>
public sealed record EntryAttachmentInputs(LedgerEntry Entry, IReadOnlyList<SplitAttachmentReview> Attachments);

/// <summary>One original file and entry owns an attachment action through native selection or confirmation.</summary>
public sealed class EntryAttachmentReview
{
    private readonly string _path, _packet;
    private readonly CommercialFileAccess _access;
    private int _state;

    /// <summary>Captures immutable original file, access and serialized source facts before publication.</summary>
    internal EntryAttachmentReview(string path, CommercialFileAccess access, EntryAttachmentInputs inputs)
    { _path = path; _access = access; Inputs = inputs; _packet = JsonSerializer.Serialize(inputs); }

    /// <summary>Gets original source facts; mutating them invalidates the action.</summary>
    public EntryAttachmentInputs Inputs { get; }

    /// <summary>Checks the entry actually displayed before opening a native file picker.</summary>
    public bool Matches(LedgerEntry entry) => JsonSerializer.Serialize(entry) == JsonSerializer.Serialize(Inputs.Entry);

    /// <summary>Refuses retired rights and mutated original metadata, including inactive-policy transitions.</summary>
    public void EnsureCurrent()
    {
        _access.EnsureSnapshotCurrent();
        if (_packet != JsonSerializer.Serialize(Inputs)) throw new InvalidOperationException("The original attachment source changed.");
    }

    /// <summary>Admits only one unconsumed original writer.</summary>
    internal void BeginWrite()
    {
        EnsureCurrent();
        if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            throw new InvalidOperationException("The original attachment action is pending or consumed.");
    }

    /// <summary>Consumes known acceptance before notification while allowing an uncommitted original retry.</summary>
    internal void EndWrite(bool committed) => Volatile.Write(ref _state, committed ? 2 : 0);

    /// <summary>Rechecks the complete original source under its bound reader or writer.</summary>
    internal void Validate(ZananceDbContext db, EntryAttachmentInputs current)
    {
        DemandFile(db);
        if (_packet != JsonSerializer.Serialize(current))
            throw new InvalidOperationException("The original attachment entry or file changed; reopen it.");
    }

    /// <summary>Refuses another selected file before issuing any attachment or entry query against it.</summary>
    internal void DemandFile(ZananceDbContext db)
    {
        EnsureCurrent();
        if (_path != DatabaseFileIdentity.ConfiguredPath(db))
            throw new InvalidOperationException("The original attachment file changed.");
    }

    /// <summary>Reads only original source and metadata, with deterministic attachment ordering.</summary>
    internal static async Task<EntryAttachmentInputs> ReadAsync(ZananceDbContext db, Guid entryId, CancellationToken ct)
    {
        var entry = await db.Entries.AsNoTracking().SingleOrDefaultAsync(e => e.Id == entryId, ct)
            ?? throw new InvalidOperationException("The original attachment entry no longer exists.");
        var attachments = await db.Attachments.AsNoTracking().Where(a => a.EntryId == entryId).OrderBy(a => a.Id)
            .Select(a => new SplitAttachmentReview(a.Id, a.EntryId, a.FileName, a.ContentType, a.Data.Length, a.CreatedAt, a.UpdatedAt)).ToListAsync(ct);
        return new(entry, attachments);
    }
}
