using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Data;

/// <summary>An exact deleted group, excluding its process-local receipt from serialization.</summary>
internal sealed record HoldingUndoPayload(List<AssetEvent> Events, List<LedgerEntry> Entries);
/// <summary>An explicit refund relationship released by this deletion, never inferred from amounts.</summary>
internal sealed record HoldingUndoRefund(Guid RefundId, Guid SourceId);
/// <summary>Ownership and content of retained orphan attachments without copying their bytes into Undo.</summary>
internal sealed record HoldingUndoAttachment(Guid Id, Guid EntryId, string FileName, string ContentType,
    string Digest, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
/// <summary>The affected original post-deletion source, currencies, dependents and absent identities.</summary>
internal sealed record HoldingUndoFacts(HoldingPriceInputs Holding, List<Account> Accounts, List<Category> Categories,
    List<LedgerEntry> Refunds, List<LedgerEntry> OccupiedEntries, List<HoldingUndoAttachment> Attachments);

/// <summary>File-bound, immutable, single-use short Undo authority for one committed holding deletion.</summary>
internal sealed class HoldingDeletionReceipt
{
    private readonly string _file, _original, _before, _after;
    private readonly HoldingUndoFacts? _beforeFacts;
    private readonly Guid _typeId;
    private readonly List<Guid> _entryIds, _accountIds, _categoryIds, _groupIds;
    private readonly List<HoldingUndoRefund> _links;
    private int _state;
    private HoldingDeletionReceipt(string file, HoldingGroup group, List<LedgerEntry> refunds)
    {
        _file = file; _original = Packet(group); _typeId = group.Events.Single().AssetTypeId;
        _entryIds = group.Entries.Select(e => e.Id).ToList();
        _groupIds = group.Events.Where(e => e.GroupId is not null).Select(e => e.GroupId!.Value).Distinct().ToList();
        _accountIds = group.Entries.Concat(refunds).SelectMany(e => e.ToAccountId is { } destination
            ? new[] { e.AccountId, destination } : new[] { e.AccountId }).Distinct().ToList();
        _categoryIds = group.Entries.Concat(refunds).Where(e => e.CategoryId is not null).Select(e => e.CategoryId!.Value).Distinct().ToList();
        _links = refunds.Select(e => new HoldingUndoRefund(e.Id, e.RefundOfId!.Value)).ToList();
        _before = _after = string.Empty;
    }
    private HoldingDeletionReceipt(HoldingDeletionReceipt original, HoldingUndoFacts before, string after)
    {
        _file = original._file; _original = original._original; _typeId = original._typeId;
        _entryIds = original._entryIds; _accountIds = original._accountIds; _categoryIds = original._categoryIds;
        _groupIds = original._groupIds; _links = original._links; _beforeFacts = before; _before = FactsPacket(before); _after = after;
    }
    /// <summary>Captures original explicit links before deletion while the same writer owns them.</summary>
    internal static async Task<HoldingDeletionReceipt> BeforeAsync(ZananceDbContext db, HoldingGroup group, CancellationToken ct)
    {
        var ids = group.Entries.Select(e => e.Id).ToList();
        var refunds = await db.Entries.AsNoTracking().Where(e => e.RefundOfId != null && ids.Contains(e.RefundOfId.Value)
            && !ids.Contains(e.Id)).OrderBy(e => e.Id).ToListAsync(ct);
        var receipt = new HoldingDeletionReceipt(DatabaseFileIdentity.ConfiguredPath(db), group, refunds);
        return new(receipt, await receipt.ReadAsync(db, ct), string.Empty);
    }
    /// <summary>Seals the actual post-delete packet before this same transaction commits.</summary>
    internal async Task<HoldingDeletionReceipt> AfterAsync(ZananceDbContext db, CancellationToken ct) =>
        new(this, _beforeFacts!, FactsPacket(await ReadAsync(db, ct)));
    /// <summary>Checks source, actual file and immutable payload, claiming only this original Undo.</summary>
    internal void Begin(ZananceDbContext db, HoldingGroup group)
    {
        if (_file != DatabaseFileIdentity.ConfiguredPath(db) || _original != Packet(group)
            || Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            throw new HoldingEventReviewChangedException("Original deleted holding belongs to another file, changed or was consumed.");
    }
    /// <summary>Checks the actual post-delete packet under the claimed original writer.</summary>
    internal async Task ValidateAsync(ZananceDbContext db, CancellationToken ct)
    {
        if (_after != FactsPacket(await ReadAsync(db, ct)))
            throw new HoldingEventReviewChangedException("Original post-deletion holding, currencies, refunds or receipt ownership changed.");
    }
    /// <summary>Retains consumed authority after a known commit, including failed notification.</summary>
    internal void End(bool committed) => Volatile.Write(ref _state, committed ? 2 : 0);
    /// <summary>Returns detached original copies so auditing never mutates the retained deletion payload.</summary>
    internal HoldingUndoPayload Copy(HoldingGroup group) => new(group.Events.Select(HoldingEventReadReview.CopyEvent).ToList(),
        group.Entries.Select(e => e.Copy()).ToList());
    /// <summary>Relinks only the original explicitly retained refunds, after original source rows exist again.</summary>
    internal async Task RelinkAsync(ZananceDbContext db, CancellationToken ct)
    {
        foreach (var link in _links)
        {
            var count = await db.Entries.Where(e => e.Id == link.RefundId && e.RefundOfId == null)
                .ExecuteUpdateAsync(set => set.SetProperty(e => e.RefundOfId, (Guid?)link.SourceId), ct);
            if (count != 1) throw new HoldingEventReviewChangedException("Original refund no longer permits exact relinking.");
        }
    }
    /// <summary>Rechecks the entire original restored packet; only the restored rows' audit update time may advance.</summary>
    internal async Task ValidateRestoredAsync(ZananceDbContext db, HoldingGroup group, CancellationToken ct)
    {
        // Entity ids are constructor-owned. Keep private detached originals rather than deserialize fresh identities.
        var expected = _beforeFacts ?? throw new HoldingEventReviewChangedException("Original holding Undo packet is unavailable.");
        var actual = await ReadAsync(db, ct);
        var eventIds = group.Events.Select(e => e.Id).ToHashSet();
        foreach (var item in actual.Holding.Events.Where(e => eventIds.Contains(e.Id)))
            item.UpdatedAt = expected.Holding.Events.Single(e => e.Id == item.Id).UpdatedAt;
        foreach (var item in actual.OccupiedEntries.Where(e => _entryIds.Contains(e.Id)))
            item.UpdatedAt = expected.OccupiedEntries.Single(e => e.Id == item.Id).UpdatedAt;
        if (_before != FactsPacket(actual))
            throw new HoldingEventReviewChangedException("Restored holding or financial metadata differs from the original deletion.");
    }
    private async Task<HoldingUndoFacts> ReadAsync(ZananceDbContext db, CancellationToken ct)
    {
        var refundIds = _links.Select(link => link.RefundId).ToList();
        var attachments = await db.Attachments.AsNoTracking().Where(a => _entryIds.Contains(a.EntryId)).OrderBy(a => a.Id).ToListAsync(ct);
        return new(await HoldingPriceReview.ReadAsync(db, _typeId, ct),
            await db.Accounts.AsNoTracking().Where(a => _accountIds.Contains(a.Id)).OrderBy(a => a.Id).ToListAsync(ct),
            await db.Categories.AsNoTracking().Where(c => _categoryIds.Contains(c.Id)).OrderBy(c => c.Id).ToListAsync(ct),
            await db.Entries.AsNoTracking().Where(e => refundIds.Contains(e.Id)).OrderBy(e => e.Id).ToListAsync(ct),
            await db.Entries.AsNoTracking().Where(e => _entryIds.Contains(e.Id) || e.GroupId != null && _groupIds.Contains(e.GroupId.Value)).OrderBy(e => e.Id).ToListAsync(ct),
            attachments.Select(a => new HoldingUndoAttachment(a.Id, a.EntryId, a.FileName, a.ContentType,
                Convert.ToHexString(SHA256.HashData(a.Data)), a.CreatedAt, a.UpdatedAt)).ToList());
    }
    private static string Packet(HoldingGroup group) => JsonSerializer.Serialize(
        new HoldingUndoPayload(group.Events.ToList(), group.Entries.ToList()), HoldingUndoJson.Default.HoldingUndoPayload);
    private static string FactsPacket(HoldingUndoFacts facts) => JsonSerializer.Serialize(facts, HoldingUndoJson.Default.HoldingUndoFacts);
}

/// <summary>Explicit native trim/AOT metadata for the process-local deletion receipt; no stored schema change.</summary>
[JsonSerializable(typeof(HoldingUndoPayload))]
[JsonSerializable(typeof(HoldingUndoFacts))]
internal sealed partial class HoldingUndoJson : JsonSerializerContext;
