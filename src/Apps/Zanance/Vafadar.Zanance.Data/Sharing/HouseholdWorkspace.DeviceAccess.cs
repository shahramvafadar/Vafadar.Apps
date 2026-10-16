using System.Text.Json;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdWorkspace
{
    /// <summary>Creates only a public signed checkpoint for an existing member, without reserving a seat or approving another device.</summary>
    public async Task<HouseholdDeviceAccessPackage> CreateDeviceAccessAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        string memberIdentity, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            using var keys = await RequireOwnerKeysAsync(transport, state, ct);
            var current = (await journal.ReadCurrentMembershipAsync(authority, ct)).Snapshot();
            if (!current.Members.Any(member => member.IdentityId == memberIdentity) || await journal.ReadMembershipProposalAsync(authority, ct) is not null) throw Refusal();
            var statement = new HouseholdDeviceAccessStatement(1, state.SpaceId, Guid.NewGuid(), state.MembershipVersion, state.MembershipHead,
                memberIdentity, HouseholdLimits.ClientDeadline(time.GetUtcNow(), HouseholdLimits.InvitationLifetime));
            return new(1, HouseholdSetupAnchors.DeviceAccess, Public(keys.Owner!),
                Sign(statement, HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessStatement, keys.Owner!, HouseholdSetupAnchors.DeviceAccessProof));
        }
        finally { changing.Release(); }
    }

    /// <summary>Retains an empty captured native draft and exact private key before requesting approval for an already accepted identity.</summary>
    public async Task<HouseholdJournalState> EnrollOwnDeviceAsync(HouseholdServiceTransport transport, HouseholdDeviceAccessPackage package,
        string independentlyConfirmedRoot, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var captured = await finances.ReviewAsync(ct); var identity = await transport.IdentityAsync(ct);
            var anchor = HouseholdTrustChain.DeviceAuthorization(package, identity.Id);
            var root = new PublicSigningRecipientIdentity(package.OwnerKeys.SigningKey, package.OwnerKeys.WrappingKey);
            if (root.Fingerprint != independentlyConfirmedRoot) throw Refusal();
            var json = JsonSerializer.Serialize(package, HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessPackage);
            if (json.Length > 8192) throw Refusal();
            var state = await journal.CaptureAsync(transport.Peer, identity.Id, ct);
            if (state is null)
            {
                if (anchor.ExpiresUnixSeconds <= time.GetUtcNow().ToUnixTimeSeconds()) throw Refusal();
                using var device = SigningRecipientIdentity.Create();
                state = await journal.BeginDraftAsync(transport.Peer, identity.Id, root, false, device, null, json, ct,
                    requireEmptyFinancial: true, expectedGenerationPath: captured.GenerationPath);
            }
            if (state.OwnerCreation || state.Authority.GenerationPath != captured.GenerationPath || state.SignedInvitation != json ||
                state.PinnedRoot != root || state.SpaceId != Guid.Empty && state.SpaceId != anchor.SpaceId) throw Refusal();
            return await SubmitOwnDeviceAsync(transport, state, anchor.SpaceId, ct);
        }
        finally { changing.Release(); }
    }

    /// <summary>Explicitly resumes only the same original device draft; no pasted package or replacement key is required after process loss.</summary>
    public async Task<HouseholdJournalState> ResumeOwnDeviceAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct);
            if (HouseholdSetupAnchors.Kind(state.SignedInvitation) != HouseholdSetupAnchors.DeviceAccess) throw Refusal();
            var package = JsonSerializer.Deserialize(state.SignedInvitation!, HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessPackage) ?? throw Refusal();
            var anchor = HouseholdTrustChain.DeviceAuthorization(package, authority.IdentityId);
            return await SubmitOwnDeviceAsync(transport, state, anchor.SpaceId, ct);
        }
        finally { changing.Release(); }
    }

    /// <summary>Submits or observes the same persisted key/operation; only an actually approved key can rebind a resource session.</summary>
    private async Task<HouseholdJournalState> SubmitOwnDeviceAsync(HouseholdServiceTransport transport, HouseholdJournalState state, Guid space, CancellationToken ct)
    {
        if (state.MembershipVersion != 0) return state;
        using var keys = await journal.ReadKeysAsync(state.Authority, ct); var session = await transport.SessionAsync(ct);
        if (session.IdentityId != state.Authority.IdentityId) throw Refusal();
        var intent = await journal.ReadSetupIntentAsync(state.Authority, false, ct);
        HouseholdDeviceSessionStatement? original = intent is null ? null : StandardJoseSignatures.VerifyJson(intent.SignedBody, keys.Device.Public,
            "zanance-household-device-enrollment+jwt", HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement);
        if (original is null || original.SessionId != session.SessionId || original.ValidUntilUnixSeconds <= time.GetUtcNow().ToUnixTimeSeconds())
        {
            // A fresh actual sign-in or expired proposal proves the same retained key and setup
            // operation. The service preserves its original admission proof and never approves it.
            var proof = new HouseholdDeviceSessionStatement(1, space, Guid.Empty, state.Authority.SetupOperation, state.Authority.IdentityId,
                session.SessionId, Public(keys.Device), ProposalDeadline());
            intent = new(state.Authority.SetupOperation, Sign(proof, HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement,
                keys.Device, "zanance-household-device-enrollment+jwt"), null);
            await journal.RetainOwnDevicePossessionAsync(state.Authority, intent, ct);
        }
        var observed = await transport.EnrollDeviceAsync(space, new(Public(keys.Device), intent!.SignedBody), ct);
        if (observed.DeviceId == Guid.Empty || observed.Revoked || observed.IdentityId != state.Authority.IdentityId || observed.Fingerprint != state.DeviceFingerprint ||
            state.DeviceId != Guid.Empty && state.DeviceId != observed.DeviceId) throw Refusal();
        await journal.BindRemoteAsync(state.Authority, space, observed.DeviceId, ct);
        if (observed.Approved)
        {
            var rebound = new HouseholdDeviceSessionStatement(1, space, observed.DeviceId, Guid.NewGuid(), state.Authority.IdentityId,
                session.SessionId, Public(keys.Device), ProposalDeadline());
            var admitted = await transport.RebindAsync(space, observed.DeviceId, new(Sign(rebound,
                HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement, keys.Device, "zanance-household-device-session+jwt")), ct);
            if (!admitted.Approved || admitted.Revoked || admitted.DeviceId != observed.DeviceId || admitted.Fingerprint != state.DeviceFingerprint) throw Refusal();
        }
        return await RequireStateAsync(transport, state.Authority, ct);
    }
}
