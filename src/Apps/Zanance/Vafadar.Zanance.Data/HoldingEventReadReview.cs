using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Original holding editor facts and displayed choices within one actual-file snapshot.</summary>
public sealed record HoldingEventReviewInputs(AssetType Type, List<AssetEvent> Events,
    List<AssetLocation> Locations, List<Account> Accounts, List<Category> Categories,
    List<LedgerEntry> Entries, List<LedgerEntry> Paybacks, List<AssetValuation> Valuations,
    List<SplitAttachmentReview> Attachments, HoldingEditorSettings Settings);

/// <summary>Only settings that select the original editor mode/default, without unrelated display preferences.</summary>
public sealed record HoldingEditorSettings(bool Advanced, Guid? DefaultAccountId);

/// <summary>The original holding source changed; retain raw input and reopen before saving.</summary>
public sealed class HoldingEventReviewChangedException(string message) : InvalidOperationException(message);

/// <summary>Owns one immutable original holding-event financial review and its consumed state.</summary>
public sealed class HoldingEventReadReview
{
    private readonly string _path, _packet;
    private readonly CommercialFileAccess _access;
    private int _state;
    /// <summary>Captures original inputs before the native editor receives mutable copies.</summary>
    internal HoldingEventReadReview(Guid? eventId, string path, CommercialFileAccess access, HoldingEventReviewInputs inputs)
    { EventId = eventId; _path = path; _access = access; Inputs = inputs; _packet = Packet(inputs); }
    /// <summary>Gets the original edited event, or null for exactly one new event.</summary>
    public Guid? EventId { get; }
    /// <summary>Gets original facts; mutation invalidates this review.</summary>
    public HoldingEventReviewInputs Inputs { get; }
    /// <summary>Checks original memory and actual-file authority.</summary>
    public void EnsureCurrent()
    {
        _access.EnsureSnapshotCurrent();
        if (_packet != Packet(Inputs)) throw new HoldingEventReviewChangedException("Original holding review changed.");
    }
    /// <summary>Claims one write, refusing consumed or concurrent use.</summary>
    internal void BeginWrite()
    {
        EnsureCurrent();
        if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            throw new HoldingEventReviewChangedException("Original holding review already saving or consumed.");
    }
    /// <summary>Retains known acceptance even when its notification fails.</summary>
    internal void EndWrite(bool committed) => Volatile.Write(ref _state, committed ? 2 : 0);
    /// <summary>Validates the complete original packet inside the existing financial writer.</summary>
    internal void ValidateWriter(ZananceDbContext db, HoldingEventReviewInputs current)
    {
        EnsureCurrent();
        if (_path != DatabaseFileIdentity.ConfiguredPath(db) || _packet != Packet(current))
            throw new HoldingEventReviewChangedException("Original holding source changed; reopen before saving.");
    }
    /// <summary>Allows only the one explicit new location; all original financial facts and choices must stay unchanged.</summary>
    internal void ValidateAddedLocation(ZananceDbContext db, HoldingEventReviewInputs current, Guid addedId)
    {
        var added = current.Locations.Where(l => l.Id == addedId).ToList();
        if (added.Count != 1 || Inputs.Locations.Any(l => l.Id == addedId)
            || current.Locations.Count != Inputs.Locations.Count + 1)
            throw new HoldingEventReviewChangedException("Only the original explicitly added location may be adopted.");
        ValidateWriter(db, current with { Locations = current.Locations.Where(l => l.Id != addedId).ToList() });
    }

    /// <summary>Copies an event without mutating the original reviewed object or introducing a new identity.</summary>
    public static AssetEvent CopyEvent(AssetEvent item) => new(item.Id)
    { AssetTypeId = item.AssetTypeId, LocationId = item.LocationId, ToLocationId = item.ToLocationId,
      Kind = item.Kind, Date = item.Date, Quantity = item.Quantity, BasisAmount = item.BasisAmount,
      ProceedsAmount = item.ProceedsAmount, IsIncrease = item.IsIncrease, GroupId = item.GroupId,
      Reason = item.Reason, Note = item.Note, CreatedAt = item.CreatedAt, UpdatedAt = item.UpdatedAt };

    /// <summary>Reads original quantity history, group/dependents and editor choices deterministically.</summary>
    internal static async Task<HoldingEventReviewInputs> ReadAsync(ZananceDbContext db, Guid typeId, Guid? eventId, CancellationToken ct)
    {
        if (eventId is { } id)
        {
            var original = await db.AssetEvents.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, ct)
                ?? throw new HoldingEventReviewChangedException("Original holding event no longer exists.");
            if (typeId != Guid.Empty && typeId != original.AssetTypeId)
                throw new HoldingEventReviewChangedException("Original holding event type changed.");
            typeId = original.AssetTypeId;
        }
        var type = await db.AssetTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Id == typeId, ct)
            ?? throw new HoldingEventReviewChangedException("Original holding type no longer exists.");
        var events = await db.AssetEvents.AsNoTracking().Where(e => e.AssetTypeId == typeId).OrderBy(e => e.Id).ToListAsync(ct);
        var group = events.SingleOrDefault(e => e.Id == eventId)?.GroupId;
        var entries = group is { } gid ? await db.Entries.AsNoTracking().Where(e => e.GroupId == gid).OrderBy(e => e.Id).ToListAsync(ct) : [];
        var ids = entries.Select(e => e.Id).ToList();
        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(ct) ?? new ZananceSettings();
        return new(type, events,
            await db.AssetLocations.AsNoTracking().OrderBy(e => e.Id).ToListAsync(ct),
            await db.Accounts.AsNoTracking().OrderBy(e => e.Id).ToListAsync(ct),
            await db.Categories.AsNoTracking().OrderBy(e => e.Id).ToListAsync(ct), entries,
            await db.Entries.AsNoTracking().Where(e => e.RefundOfId != null && ids.Contains(e.RefundOfId.Value)).OrderBy(e => e.Id).ToListAsync(ct),
            await db.AssetValuations.AsNoTracking().Where(e => e.AssetTypeId == typeId).OrderBy(e => e.Id).ToListAsync(ct),
            await db.Attachments.AsNoTracking().Where(a => ids.Contains(a.EntryId)).OrderBy(a => a.Id)
                .Select(a => new SplitAttachmentReview(a.Id, a.EntryId, a.FileName, a.ContentType, a.Data.Length, a.CreatedAt, a.UpdatedAt)).ToListAsync(ct),
            new HoldingEditorSettings(settings.Shows(Feature.HoldingEvents), settings.DefaultAccountId));
    }
    private static string Packet(HoldingEventReviewInputs inputs) => JsonSerializer.Serialize(inputs, HoldingEventReviewJson.Default.HoldingEventReviewInputs);
}

/// <summary>Explicit trim/AOT metadata for holding editor review; no stored schema change.</summary>
[JsonSerializable(typeof(HoldingEventReviewInputs))]
internal sealed partial class HoldingEventReviewJson : JsonSerializerContext;
