using System.Text.Json;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>One original editor proposal and every new aggregate overlap, including an explicitly empty preview.</summary>
public sealed class EntryAggregateReview
{
    private readonly string _stamp;
    /// <summary>Detaches the original proposal and stamps the complete new-coverage review before publication.</summary>
    internal EntryAggregateReview(EntryEditorReadSnapshot source, IReadOnlyList<LedgerEntry> proposal,
        IReadOnlyCollection<Guid> deleted, IReadOnlyList<AggregateOverlap> overlaps, IReadOnlySet<Guid> unavailable)
    {
        Source = source; Proposal = proposal.Select(e => e.Copy()).ToArray(); Deleted = deleted.ToArray();
        Overlaps = overlaps; Unavailable = unavailable; _stamp = Stamp(Proposal, Deleted, Overlaps, Unavailable);
    }
    /// <summary>Gets the exact original editor packet which may submit this proposal.</summary>
    internal EntryEditorReadSnapshot Source { get; }
    /// <summary>Gets the detached complete proposed group, before any aggregate reduction.</summary>
    internal IReadOnlyList<LedgerEntry> Proposal { get; }
    /// <summary>Gets only this reviewed proposal's requested removals.</summary>
    internal IReadOnlyCollection<Guid> Deleted { get; }
    /// <summary>Gets aggregate identities whose relationship, document or detail history refuses a link.</summary>
    internal IReadOnlySet<Guid> Unavailable { get; }
    /// <summary>Gets each complete original overlap in stable identity order.</summary>
    public IReadOnlyList<AggregateOverlap> Overlaps { get; }
    /// <summary>Whether this exact aggregate can be linked without reusing a detail or moving a document/financial relationship.</summary>
    public bool CanLink(Guid aggregateId) => !Unavailable.Contains(aggregateId);
    /// <summary>Rejects another owner, in-memory mutation or changed persisted coverage under the original writer.</summary>
    internal void Validate(EntryEditorReadSnapshot? source, IReadOnlyList<LedgerEntry> proposal,
        IReadOnlyCollection<Guid> deleted, IReadOnlyList<AggregateOverlap> current, IReadOnlySet<Guid> unavailable)
    {
        Source.EnsureCurrent();
        if (!ReferenceEquals(source, Source) || _stamp != Stamp(Proposal, Deleted, Overlaps, Unavailable)
            || _stamp != Stamp(proposal, deleted, current, unavailable))
            throw new EntryEditorSourceChangedException();
    }
    private static string Stamp(IReadOnlyList<LedgerEntry> proposal, IReadOnlyCollection<Guid> deleted,
        IReadOnlyList<AggregateOverlap> overlaps, IReadOnlySet<Guid> unavailable) => JsonSerializer.Serialize(new
        {
            Proposal = proposal.OrderBy(e => e.Id), Deleted = deleted.Order(),
            Overlaps = overlaps.OrderBy(o => o.Aggregate.Id).Select(o => new { o.Aggregate, Detailed = o.Detailed.OrderBy(e => e.Id) }),
            Unavailable = unavailable.Order(),
        });
}

/// <summary>Original-file, one-use short Undo for accepted editor aggregate adjustments; details remain saved.</summary>
public sealed class EntryAggregateUndo
{
    private readonly CommercialFileAccess _access;
    private int _state;
    /// <summary>Captures the accepted journal identity and cached original-file rights before publication.</summary>
    internal EntryAggregateUndo(Guid journalId, string path, CommercialFileAccess access, DateTimeOffset createdAt)
    { JournalId = journalId; DatabasePath = path; _access = access; CreatedAt = createdAt; }
    /// <summary>Gets the durable adjustment record consumed only by this original short Undo.</summary>
    internal Guid JournalId { get; }
    /// <summary>Gets the original actual file; an offer never follows a subsequently selected profile.</summary>
    internal string DatabasePath { get; }
    /// <summary>Gets the original financial commit's deadline origin; later publication never renews it.</summary>
    public DateTimeOffset CreatedAt { get; }
    /// <summary>Gets known Undo acceptance before a fallible change notification.</summary>
    public bool Accepted => Volatile.Read(ref _state) == 2;
    /// <summary>Admits one pending Undo without rearming an accepted adjustment.</summary>
    internal void Begin() { if (Interlocked.CompareExchange(ref _state, 1, 0) != 0) throw new InvalidOperationException("The original aggregate Undo is pending or accepted."); }
    /// <summary>Consumes known acceptance or releases only an uncommitted attempt.</summary>
    internal void End(bool accepted) => Volatile.Write(ref _state, accepted ? 2 : 0);
    /// <summary>Rechecks actual-file identity, cached rights and the original deadline before committing.</summary>
    internal void Current(ZananceDbContext db, DateTimeOffset now)
    {
        _access.EnsureSnapshotCurrent();
        if (DatabaseFileIdentity.ConfiguredPath(db) != DatabasePath || now < CreatedAt || now - CreatedAt >= TimeSpan.FromSeconds(8))
            throw new InvalidOperationException("The original aggregate Undo file or deadline changed.");
    }
}
