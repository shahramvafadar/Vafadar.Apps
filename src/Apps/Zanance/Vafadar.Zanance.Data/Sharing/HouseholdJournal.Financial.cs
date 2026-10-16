using System.Security.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Original financial review or exact retained ciphertext, never a replacement readable publication.</summary>
public sealed record HouseholdPreparationOutcome(HouseholdJournalAttempt? Attempt, bool NeedsReview);

public sealed partial class HouseholdJournal
{
    /// <summary>Captures the original actual outbox, applies the original planner and durably retains its exact ciphertext before network work.</summary>
    public async Task<HouseholdPreparationOutcome> PrepareOriginalAsync(HouseholdJournalAuthority authority, LocalSyncPeer peer,
        PersonalSyncStore original, CancellationToken ct = default, Guid? ownershipReception = null)
    {
        if (peer.BindingKey(authority.IdentityId) != authority.PeerKey) throw Refusal();
        // Resume must read the immutable attempt before considering new signatures or nonces.
        // New edits remain after the original frozen cutoff in the ordinary financial outbox.
        var retained = await ReadAttemptAsync(authority, ct);
        if (retained is not null)
        {
            if (!await DiscardStaleUnpublishedNoOpAsync(authority, retained, ct)) return new(retained, false);
        }
        var work = await original.CaptureRoundAsync(peer, authority.IdentityId, ct, ProfileSynchronizationKind.Household);
        if (work.Authority.Generation != authority.GenerationPath) throw Refusal();
        if (work.NeedsReview) return new(null, true);
        using var keys = await ReadKeysAsync(authority, ct);
        var membership = await ReadCurrentMembershipAsync(authority, ct);
        using var epoch = await ReadEpochAsync(authority, membership.Epoch, ct);
        using var replica = await ReadReplicaAsync(authority, ct);
        var baselines = await replica.ReadBaselinesAsync(work.Parts.SelectMany(part => part.Rows), ct);
        var prepared = await SealOriginalAsync(authority, work, membership, epoch, keys, replica, baselines, ct, ownershipReception);
        if (!prepared.Plan.Succeeded)
        {
            await original.RecordCommitAsync(work, new(false, replica.FinancialCursor, prepared.Plan.Conflicts), ct);
            return new(null, true);
        }
        return new(await ReadAttemptAsync(authority, ct) ?? throw Refusal(), false);
    }

    /// <summary>Returns only the current locally independently verified original membership, never a service directory hint.</summary>
    public async Task<HouseholdVerifiedMembership> ReadCurrentMembershipAsync(HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await using var context = await OpenAsync(ct); var binding = await RequireBindingAsync(context, authority, ct);
        using var device = await ReadIdentityAsync(context.Db, "device", ct);
        var state = await ReadMembershipAsync(context.Db, binding, device, ct); context.Check(); return state.Current;
    }
}
