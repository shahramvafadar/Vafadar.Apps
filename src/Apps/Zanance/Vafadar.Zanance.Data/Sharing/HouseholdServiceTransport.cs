using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Original pinned authenticated service channel for public household metadata/ciphertext; it exposes no readable financial publication method.</summary>
public sealed partial class HouseholdServiceTransport : IDisposable
{
    private readonly PersonalSyncTransport authentication;

    /// <summary>Constructs no database/session/network work; actual app access is rechecked by the original authenticated transport on every request.</summary>
    public HouseholdServiceTransport(LocalSyncPeer peer, IPersonalSyncTokenStore tokens, TimeProvider? time = null, Action? verifyAccess = null)
        => authentication = new(peer, tokens, time, verifyAccess);

    /// <summary>Exact reviewed endpoint/certificate identity, shared with original session rotation and secure credential storage.</summary>
    public LocalSyncPeer Peer => authentication.Peer;
    /// <summary>Explicit optional identity sign-in, never a financial encryption password or grant.</summary>
    public Task<SyncIdentity> SignInAsync(string email, string password, CancellationToken ct = default) => authentication.SignInAsync(email, password, ct);
    /// <summary>Forgets only this reviewed peer's actual maintained identity session.</summary>
    public Task ForgetAsync(CancellationToken ct = default) => authentication.ForgetAsync(ct);
    /// <summary>Reads actual registered identity without parsing opaque bearer credentials.</summary>
    public Task<SyncIdentity> IdentityAsync(CancellationToken ct = default) => authentication.IdentityAsync(ct);
    /// <summary>Reads actual public session id for a private-key proof; a session id is not a bearer credential.</summary>
    public Task<HouseholdSessionIdentity> SessionAsync(CancellationToken ct = default) => authentication.HouseholdGetAsync("households/session", HouseholdMetadataJsonContext.Default.HouseholdSessionIdentity, ct);
    /// <summary>Idempotently reserves only the original room/device public keys, never clear financial data.</summary>
    public Task<HouseholdSpaceReservation> ReserveAsync(HouseholdReserveSpace input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync("households/", input, HouseholdMetadataJsonContext.Default.HouseholdReserveSpace, HouseholdMetadataJsonContext.Default.HouseholdSpaceReservation, ct);
    /// <summary>Publishes exact standard-signed membership and encrypted recipient grants, including independent transfer consent.</summary>
    public Task<HouseholdCheckpoint> PublishMembershipAsync(Guid space, HouseholdPublishMembership input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Space(space) + "/membership", input, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership, HouseholdMetadataJsonContext.Default.HouseholdCheckpoint, ct);
    /// <summary>Reserves an expiring seat with public root proof; the independent capability never enters the address.</summary>
    public Task<HouseholdInvitationReservation> InviteAsync(Guid space, HouseholdCreateInvitation input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Space(space) + "/invitations", input, HouseholdMetadataJsonContext.Default.HouseholdCreateInvitation, HouseholdMetadataJsonContext.Default.HouseholdInvitationReservation, ct);
    /// <summary>Reads only owner-scoped pending public proofs, never an invitation capability or a financial row.</summary>
    public Task<HouseholdPendingClaim[]> PendingClaimsAsync(Guid space, Guid device, CancellationToken ct = default) =>
        authentication.HouseholdGetAsync(Space(space) + "/invitations/pending?device=" + Id(device), HouseholdMetadataJsonContext.Default.HouseholdPendingClaimArray, ct);
    /// <summary>Submits the independent one-use capability and recipient-signed consent in an HTTPS body only.</summary>
    public Task<HouseholdInvitationClaimResult> ClaimAsync(Guid space, Guid invitation, HouseholdClaimInvitation input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Space(space) + "/invitations/" + Id(invitation) + "/claim", input, HouseholdMetadataJsonContext.Default.HouseholdClaimInvitation, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimResult, ct);
    /// <summary>Registers a self-proven pending key without silently approving a new device from login alone.</summary>
    public Task<HouseholdDeviceObservation> EnrollDeviceAsync(Guid space, HouseholdEnrollDevice input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Space(space) + "/devices", input, HouseholdMetadataJsonContext.Default.HouseholdEnrollDevice, HouseholdMetadataJsonContext.Default.HouseholdDeviceObservation, ct);

    /// <summary>Only the actual current owner can review original self-enrolled keys of accepted members.</summary>
    public Task<HouseholdPendingDevice[]> PendingDevicesAsync(Guid space, Guid device, CancellationToken ct = default) =>
        authentication.HouseholdGetAsync(Space(space) + "/devices/pending?device=" + Id(device), HouseholdMetadataJsonContext.Default.HouseholdPendingDeviceArray, ct);
    /// <summary>Rebinds an existing accepted-member device only with private possession bound to the actual session; pending status grants no resource access.</summary>
    public Task<HouseholdDeviceObservation> RebindAsync(Guid space, Guid device, HouseholdRebindDevice input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Space(space) + "/devices/" + Id(device) + "/rebind", input, HouseholdMetadataJsonContext.Default.HouseholdRebindDevice, HouseholdMetadataJsonContext.Default.HouseholdDeviceObservation, ct);
    /// <summary>Observes actual current service metadata; callers must still verify the independently pinned signed chain.</summary>
    public Task<HouseholdCheckpoint> CheckpointAsync(Guid space, Guid device, CancellationToken ct = default) =>
        authentication.HouseholdGetAsync(Space(space) + "/checkpoint?device=" + Id(device), HouseholdMetadataJsonContext.Default.HouseholdCheckpoint, ct);
    /// <summary>Reads bounded immutable public proofs after an exact local verified sequence, not a mutable advertised key directory.</summary>
    public Task<HouseholdMembershipProof[]> MembershipAsync(Guid space, Guid device, long after, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(after);
        return authentication.HouseholdGetAsync(FormattableString.Invariant($"{Space(space)}/membership?device={Id(device)}&after={after}"), HouseholdMetadataJsonContext.Default.HouseholdMembershipProofArray, ct);
    }
    /// <summary>Reads only this actual approved device's encrypted grant; callers check context/signature/commitments before using it.</summary>
    public Task<HouseholdDeviceGrant> GrantAsync(Guid space, Guid device, long epoch, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(epoch, 1);
        return authentication.HouseholdGetAsync(FormattableString.Invariant($"{Space(space)}/grants/{epoch}?device={Id(device)}"), HouseholdMetadataJsonContext.Default.HouseholdDeviceGrant, ct);
    }
    /// <summary>Opens one exact immutable ciphertext attempt against the actual observed room cursor/membership/epoch.</summary>
    public Task<HouseholdRoundHandle> OpenAsync(Guid space, HouseholdOpenRound input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Space(space) + "/rounds", input, HouseholdMetadataJsonContext.Default.HouseholdOpenRound, HouseholdMetadataJsonContext.Default.HouseholdRoundHandle, ct);
    /// <summary>Observes only this actual session's original immutable attempt, including a committed response lost before later revocation.</summary>
    public Task<HouseholdRoundHandle> ObserveAttemptAsync(Guid space, Guid device, Guid operation, Guid attempt, CancellationToken ct = default) =>
        authentication.HouseholdGetAsync($"{Space(space)}/rounds/attempt?device={Id(device)}&operation={Id(operation)}&attempt={Id(attempt)}",
            HouseholdMetadataJsonContext.Default.HouseholdRoundHandle, ct);
    /// <summary>Confirms a durable tombstone for the exact unpublished original attempt before any new ciphertext can replace it.</summary>
    public Task<HouseholdRoundHandle> AbandonAttemptAsync(Guid space, HouseholdOpenRound input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Space(space) + "/rounds/abandon", input, HouseholdMetadataJsonContext.Default.HouseholdOpenRound,
            HouseholdMetadataJsonContext.Default.HouseholdRoundHandle, ct);
    /// <summary>Uploads only bounded standard-envelope-shaped ciphertext with its exact digest, never original SyncProposal/attachment plaintext.</summary>
    public Task PutAsync(Guid space, Guid round, HouseholdOpaquePart input, CancellationToken ct = default)
    {
        if (input.Ciphertext is null || input.Ciphertext.Count(character => character == '.') != 4 ||
            HouseholdPublicationDigest.Ciphertext(input.Ciphertext) != input.Digest) throw new ArgumentException("An exact protected household part is required.", nameof(input));
        return authentication.HouseholdPutAsync(Round(space, round) + "/parts", input, ct);
    }
    /// <summary>Stages at most eight exact original ciphertext parts per request; request protection/size bounds remain unchanged.</summary>
    public async Task PutBatchAsync(Guid space, Guid round, Guid device, HouseholdOpaquePart[] parts, CancellationToken ct = default)
    {
        if (parts is null || parts.Length is < 1 or > 8 || parts.Any(part => part is null || part.Ciphertext is null ||
            part.Ciphertext.Count(character => character == '.') != 4 || HouseholdPublicationDigest.Ciphertext(part.Ciphertext) != part.Digest) ||
            parts.Sum(part => (long)part.Ciphertext.Length) > 7L * 1024 * 1024) throw new ArgumentException("Exact bounded protected household parts are required.", nameof(parts));
        _ = await authentication.HouseholdPostAsync(Round(space, round) + "/parts", new HouseholdUploadParts(device, parts),
            HouseholdMetadataJsonContext.Default.HouseholdUploadParts, HouseholdMetadataJsonContext.Default.Boolean, ct);
    }
    /// <summary>Reads exact uploaded routing/digests, which callers match against every original stored ciphertext before skipping any part.</summary>
    public Task<HouseholdUploadedParts> UploadedPartsAsync(Guid space, Guid round, Guid device, int offset, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        return authentication.HouseholdGetAsync(FormattableString.Invariant($"{Round(space, round)}/parts?device={Id(device)}&offset={offset}"),
            HouseholdMetadataJsonContext.Default.HouseholdUploadedParts, ct);
    }
    /// <summary>Requests one exact author-signed complete manifest publication; a lost response is replayed with the same original bytes.</summary>
    public Task<HouseholdRoundHandle> CommitAsync(Guid space, Guid round, HouseholdCommitRound input, CancellationToken ct = default) =>
        authentication.HouseholdPostAsync(Round(space, round) + "/commit", input, HouseholdMetadataJsonContext.Default.HouseholdCommitRound, HouseholdMetadataJsonContext.Default.HouseholdRoundHandle, ct);
    /// <summary>Abandons only uncommitted staging, without reverting any live financial publication.</summary>
    public Task AbandonAsync(Guid space, Guid round, CancellationToken ct = default) => authentication.HouseholdEmptyAsync(HttpMethod.Delete, Round(space, round), ct);
    /// <summary>Explicit departure immediately stops remote access and requires fresh recipient-key rotation before more writes.</summary>
    public Task LeaveAsync(Guid space, Guid device, CancellationToken ct = default) =>
        authentication.HouseholdEmptyAsync(HttpMethod.Post, Space(space) + "/leave?device=" + Id(device), ct);
    /// <summary>Reads only a bounded exact encrypted prefix frozen at an observed publication cursor.</summary>
    public Task<HouseholdFeedPage> FeedAsync(Guid space, Guid device, HouseholdFeedPosition after, long through, CancellationToken ct = default)
    {
        if (after is null || after.Cursor < 0 || through < after.Cursor || after.RecordId is null ||
            after.RecordId.Length != 0 && !HouseholdProtectionContext.IsFingerprint(after.RecordId)) throw new ArgumentException("An exact household feed position is required.", nameof(after));
        return authentication.HouseholdGetAsync(FormattableString.Invariant($"{Space(space)}/feed?device={Id(device)}&afterCursor={after.Cursor}&afterRecord={after.RecordId}&through={through}"),
            HouseholdMetadataJsonContext.Default.HouseholdFeedPage, ct);
    }
    /// <summary>Obtains the complete original author-signed manifest before recipient-side verification/application.</summary>
    public async Task<HouseholdPublicationReceipt> ReceiptAsync(Guid space, Guid device, Guid round, CancellationToken ct = default)
    {
        var parts = new List<HouseholdPartCommitment>(); string? signed = null; int? total = null;
        while (true)
        {
            var page = await authentication.HouseholdGetAsync(FormattableString.Invariant($"{Space(space)}/receipts/{Id(round)}/parts?device={Id(device)}&offset={parts.Count}"),
                HouseholdMetadataJsonContext.Default.HouseholdReceiptPage, ct);
            if (page.RoundId != round || page.Offset != parts.Count || page.Total is < 1 or > HouseholdPublicationDigest.MaximumParts ||
                page.Parts is null || page.Parts.Length is < 1 or > 256 || page.Offset + page.Parts.Length > page.Total ||
                page.Complete != (page.Offset + page.Parts.Length == page.Total) || total is { } expected && expected != page.Total ||
                signed is not null && signed != page.SignedCommit || page.Parts.Where((part, index) => part is null || part.Index != page.Offset + index).Any())
                throw new InvalidDataException("The complete original household manifest page was refused.");
            signed = page.SignedCommit; total = page.Total; parts.AddRange(page.Parts);
            if (page.Complete) return new(round, signed, parts.ToArray());
        }
    }
    /// <inheritdoc />
    public void Dispose() => authentication.Dispose();
    /// <summary>Validated exact shared resource id, never a mutable profile label or email.</summary>
    private static string Space(Guid space) => "households/" + Id(space);
    /// <summary>Validated exact original staging resource.</summary>
    private static string Round(Guid space, Guid round) => Space(space) + "/rounds/" + Id(round);
    /// <summary>No untrusted route, address, capability or credential is accepted as an identifier.</summary>
    private static string Id(Guid value) => value != Guid.Empty ? value.ToString("D") : throw new ArgumentException("An exact household resource id is required.");
}
