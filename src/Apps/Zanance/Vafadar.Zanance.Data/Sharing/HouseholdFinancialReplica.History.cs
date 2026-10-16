using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Bounded historical reads reopen the captured protected journal without retaining a native connection or private keys.</summary>
internal interface IHouseholdFinancialArchive
{
    /// <summary>Reads at most one original128-row/2MiB page, authenticating exact archived sources again.</summary>
    Task<SyncFeedPage> ReadFeedAsync(long after, long through, CancellationToken ct);
    /// <summary>Reads only the requested original baseline, or no baseline when that row/version never existed.</summary>
    Task<IReadOnlyDictionary<SyncRecordKey, SyncRecord>> ReadBaselinesAsync(IEnumerable<SyncProposal> proposals, CancellationToken ct);
    /// <summary>Holds one captured native transaction only during an in-memory qualification seal, never across HTTP.</summary>
    Task<IHouseholdOpaqueVersionSource> OpenVersionsAsync(CancellationToken ct);
}

/// <summary>Original predecessor lookup under a captured native scope, without retaining every historical opaque address in memory.</summary>
internal interface IHouseholdOpaqueVersionSource : IAsyncDisposable
{
    /// <summary>Returns the exact latest preceding accepted publication for this opaque address.</summary>
    long Read(string address);
}

public sealed partial class HouseholdFinancialReplica
{
    private readonly IHouseholdFinancialArchive? financialArchive;
    /// <summary>Prepares a standalone in-memory qualification packet from a protected replica through bounded historical reads; production uses the durable journal sealer.</summary>
    public async Task<HouseholdPreparedPublication> PrepareAsync(HouseholdVerifiedMembership membership, HouseholdEpochMaterial epoch, Guid authorDevice,
        SigningRecipientIdentity author, Guid operation, Guid attempt, IReadOnlyList<SyncProposal> proposals,
        IReadOnlyList<SyncResolution> resolutions, IReadOnlyDictionary<string, byte[]> outgoingAttachments, HouseholdCryptography cryptography,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this); ct.ThrowIfCancellationRequested();
        if (financialArchive is null) return Prepare(membership, epoch, authorDevice, author, operation, attempt, proposals, resolutions, outgoingAttachments, cryptography);
        var baselines = await ReadBaselinesAsync(ValidateProposals(proposals).Values, ct);
        await using var versions = await financialArchive.OpenVersionsAsync(ct);
        var parts = new List<HouseholdOpaquePart>();
        var sealedMetadata = SealTo(membership, epoch, authorDevice, author, operation, attempt, proposals, resolutions,
            new MemoryAttachmentSource(outgoingAttachments), cryptography, parts.Add, ct, baselines, versions.Read);
        return new(sealedMetadata.Open, parts.ToArray(), sealedMetadata.Commit, sealedMetadata.Plan, sealedMetadata.FinancialCursor);
    }
    /// <summary>Uses bounded authenticated historical pages for a protected replica; standalone protocol callers retain their in-memory feed.</summary>
    public Task<SyncFeedPage> FinancialFeedAsync(long after, long through, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this); ct.ThrowIfCancellationRequested();
        if (after < 0 || through < after || through > FinancialCursor) throw Refusal();
        return financialArchive is null ? Task.FromResult(FinancialFeed(after, through)) : financialArchive.ReadFeedAsync(after, through, ct);
    }
    /// <summary>Loads only the captured proposal baselines before the subsequent protected write transaction begins.</summary>
    internal async Task<IReadOnlyDictionary<SyncRecordKey, SyncRecord>> ReadBaselinesAsync(IEnumerable<SyncProposal> proposals, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (financialArchive is not null) return await financialArchive.ReadBaselinesAsync(proposals, ct);
        var result = new Dictionary<SyncRecordKey, SyncRecord>();
        foreach (var proposal in proposals.Where(row => row.BaseVersion > 0))
        {
            ct.ThrowIfCancellationRequested();
            var row = history.GetValueOrDefault((proposal.Record, proposal.BaseVersion));
            if (row is not null) result.Add(proposal.Record, row);
        }
        return result;
    }
    /// <summary>Scoped qualification observes retained historical bodies, independent of current graph or process-wide memory.</summary>
    internal int ResidentHistoricalRows => history.Count;
}
