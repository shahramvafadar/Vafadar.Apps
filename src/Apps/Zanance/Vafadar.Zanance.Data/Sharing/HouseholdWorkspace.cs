using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Original app-facing household creation/admission/administration over protected journals, signed consent and the encrypted financial coordinator.</summary>
public sealed partial class HouseholdWorkspace(HouseholdJournal journal, PersonalSyncStore finances, HouseholdSyncCoordinator coordinator, TimeProvider time)
{
    private readonly SemaphoreSlim changing = new(1, 1);
    private readonly HouseholdCryptography cryptography = new(time);
    private const string MembershipType = "zanance-household-membership+jwt";
    private const string InvitationType = "zanance-household-invitation+jwt";
    private const string ClaimType = "zanance-household-invitation-claim+jwt";

    /// <summary>Reattaches only the same already-approved private device key to the actual newly authenticated session; never enrolls or approves a new key.</summary>
    public async Task<HouseholdJournalState> RebindCurrentDeviceAsync(HouseholdServiceTransport transport,
        HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct);
            if (state.SpaceId == Guid.Empty || state.DeviceId == Guid.Empty) throw Refusal();
            using var keys = await journal.ReadKeysAsync(authority, ct);
            var session = await transport.SessionAsync(ct); if (session.IdentityId != authority.IdentityId) throw Refusal();
            var statement = new HouseholdDeviceSessionStatement(1, state.SpaceId, state.DeviceId, Guid.NewGuid(), authority.IdentityId,
                session.SessionId, Public(keys.Device), ProposalDeadline());
            HouseholdDeviceObservation observed;
            try
            {
                // The service, not a local version hint, decides whether this exact key was approved.
                // Approval may have committed before this recipient ever downloaded its first chain.
                observed = await transport.RebindAsync(state.SpaceId, state.DeviceId,
                    new(Sign(statement, HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement, keys.Device, "zanance-household-device-session+jwt")), ct);
            }
            catch (PersonalSyncHttpException error) when (error.Status is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                if (state.MembershipVersion != 0) await journal.RecordRemoteAccessRefusalAsync(authority, ct);
                throw;
            }
            if (observed.Revoked || observed.DeviceId != state.DeviceId || observed.IdentityId != authority.IdentityId ||
                observed.Fingerprint != state.DeviceFingerprint) throw Refusal();
            if (!observed.Approved)
            {
                if (state.MembershipVersion != 0) throw Refusal();
                return state; // A real pending-device session can resume without acquiring a grant.
            }
            var lifecycle = await transport.LifecycleAsync(state.SpaceId, state.DeviceId, ct);
            if (lifecycle.SpaceId != state.SpaceId || !Enum.IsDefined(lifecycle.Retention)) throw Refusal();
            if (lifecycle.Retention is HouseholdRetentionState.Retained or HouseholdRetentionState.DeletionDue or HouseholdRetentionState.Deleted)
            {
                // Reattaching an existing key is not a financial read grant. Retained
                // members still need status/security actions without fetching finance.
                await journal.RecordRemoteAccessRefusalAsync(authority, ct); return state;
            }
            return await coordinator.RefreshAuthorityAsync(transport, authority, ct);
        }
        finally { changing.Release(); }
    }

    /// <summary>Creates only an empty independent protected draft or returns the same original retained setup.</summary>
    public async Task<HouseholdJournalState> BeginOwnerAsync(HouseholdServiceTransport transport, CancellationToken ct = default,
        HouseholdProjectionReview? selectedSource = null)
    {
        await changing.WaitAsync(ct);
        try
        {
            var captured = await finances.ReviewAsync(ct);
            var identity = await transport.IdentityAsync(ct);
            var retained = await journal.CaptureAsync(transport.Peer, identity.Id, ct);
            if (retained is not null) { if (!retained.OwnerCreation || retained.Authority.GenerationPath != captured.GenerationPath) throw Refusal(); return retained; }
            using var root = SigningRecipientIdentity.Create(); using var device = SigningRecipientIdentity.Create();
            return await journal.BeginDraftAsync(transport.Peer, identity.Id, root.Public, true, device, root, null, ct, requireEmptyFinancial: true,
                expectedGenerationPath: captured.GenerationPath, selectedSource: selectedSource);
        }
        finally { changing.Release(); }
    }

    /// <summary>Completes original remote reservation/genesis/connection; a retained proposal is replayed byte-for-byte before any new operation.</summary>
    public async Task<HouseholdJournalState> CompleteOwnerAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        string reviewedName, CancellationToken ct = default)
    {
        RequireName(reviewedName);
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct); if (!state.OwnerCreation) throw Refusal();
            using var keys = await journal.ReadKeysAsync(authority, ct); var root = keys.Owner ?? throw Refusal();
            if (state.MembershipVersion == 0)
            {
                var reservation = await transport.ReserveAsync(new(authority.SetupOperation, HouseholdFinancialSchema.Hash, Public(root), Public(keys.Device)), ct);
                if (reservation.OwnerId != authority.IdentityId) throw Refusal();
                await journal.BindRemoteAsync(authority, reservation.SpaceId, reservation.DeviceId, ct);
                state = await RequireStateAsync(transport, authority, ct);
                var pending = await journal.ReadMembershipProposalAsync(authority, ct);
                if (pending is null && !reservation.Initialized)
                {
                    using var epoch = HouseholdEpochMaterial.Create();
                    var statement = new HouseholdMembershipStatement(1, state.SpaceId, authority.SetupOperation,
                        HouseholdMembershipChange.Genesis, null, 0, 1, new('0', 64), authority.IdentityId, authority.IdentityId, Public(root),
                        epoch.Epoch, epoch.KeyCommitment, epoch.AddressKeyCommitment, ProposalDeadline(),
                        [new(authority.IdentityId, HouseholdRole.Owner, new('0', 64), [new(state.DeviceId, Public(keys.Device))])]);
                    pending = BuildPublication(state.DeviceId, statement, root, epoch, null, []);
                    // The proposal already contains the owner's encrypted epoch grant. Retaining it
                    // first makes even a cut before response/local promotion recoverable without a new key.
                    await journal.StageMembershipProposalAsync(authority, pending, ct);
                }
                if (pending is not null) _ = await transport.PublishMembershipAsync(state.SpaceId, pending, ct);
            }
            state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            await journal.CompleteMembershipProposalAsync(authority, ct);
            await ConnectAsync(transport, state, reviewedName, receiving: false, ct); return state;
        }
        finally { changing.Release(); }
    }

    /// <summary>Creates or recovers the exact retained one-use invitation; only an explicit later Finish permits another invitation.</summary>
    public async Task<HouseholdInvitationPackage> InviteAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        HouseholdRole reviewedRole, CancellationToken ct = default)
    {
        if (reviewedRole is not (HouseholdRole.Editor or HouseholdRole.Viewer)) throw Refusal();
        await changing.WaitAsync(ct);
        try
        {
            var state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            using var keys = await RequireOwnerKeysAsync(transport, state, ct); var root = keys.Owner!;
            var intent = await journal.ReadSetupIntentAsync(authority, true, ct);
            if (intent is null)
            {
                var capability = RandomNumberGenerator.GetBytes(32);
                try
                {
                    var statement = new HouseholdInvitationStatement(1, state.SpaceId, Guid.NewGuid(), Guid.NewGuid(), state.MembershipVersion,
                        state.MembershipHead, reviewedRole, Convert.ToHexStringLower(SHA256.HashData(capability)),
                        HouseholdLimits.ClientDeadline(time.GetUtcNow(), HouseholdLimits.InvitationLifetime));
                    intent = new(statement.OperationId, Sign(statement, HouseholdMetadataJsonContext.Default.HouseholdInvitationStatement, root, InvitationType), Convert.ToHexStringLower(capability));
                    await journal.RetainSetupIntentAsync(authority, true, intent, ct);
                }
                finally { CryptographicOperations.ZeroMemory(capability); }
            }
            var original = StandardJoseSignatures.VerifyJson(intent.SignedBody, root.Public, InvitationType,
                HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdInvitationStatement);
            if (original.OperationId != intent.OperationId || original.SpaceId != state.SpaceId || original.Role != reviewedRole || intent.Capability is null) throw Refusal();
            _ = await transport.InviteAsync(state.SpaceId, new(state.DeviceId, intent.SignedBody), ct);
            return new(1, Public(root), intent.SignedBody, intent.Capability);
        }
        finally { changing.Release(); }
    }

    /// <summary>Explicitly closes this exact displayed invitation intent; it does not revoke another person's already shared invitation.</summary>
    public Task FinishInvitationAsync(HouseholdJournalAuthority authority, Guid operation, CancellationToken ct = default) =>
        journal.CompleteSetupIntentAsync(authority, true, operation, ct);

    /// <summary>Finishes only a freshly reviewed exact local invitation; no old clipboard capability or signature-root re-publication is required.</summary>
    public async Task FinishPendingInvitationAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        HouseholdInvitationNotice reviewed, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct);
            using var keys = await RequireOwnerKeysAsync(transport, state, ct);
            if (await journal.ReadInvitationNoticeAsync(authority, ct) != reviewed) throw Refusal();
            await journal.CompleteSetupIntentAsync(authority, true, reviewed.OperationId, ct, reviewed.FrameDigest);
        }
        finally { changing.Release(); }
    }

    /// <summary>Explicitly clears only the exact original displayed private invitation, even after its old root was retired.</summary>
    public async Task FinishDisplayedInvitationAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        HouseholdInvitationPackage displayed, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct);
            using var keys = await RequireOwnerKeysAsync(transport, state, ct);
            var original = await journal.ReadSetupIntentAsync(authority, true, ct) ?? throw Refusal();
            if (displayed.Format != 1 || displayed.SignedInvitation != original.SignedBody || displayed.Capability != original.Capability) throw Refusal();
            // Exact protected intent, not an incoming key directory, establishes this original
            // signed body's ownership. Finishing it grants nobody keys and never re-publishes it.
            var root = new PublicSigningRecipientIdentity(displayed.OwnerKeys.SigningKey, displayed.OwnerKeys.WrappingKey);
            var statement = StandardJoseSignatures.VerifyJson(original.SignedBody, root, InvitationType,
                HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdInvitationStatement);
            if (statement.SpaceId != state.SpaceId || statement.OperationId != original.OperationId) throw Refusal();
            await journal.CompleteSetupIntentAsync(authority, true, original.OperationId, ct);
        }
        finally { changing.Release(); }
    }

    /// <summary>Checks the independently confirmed root and persists the exact original recipient claim before the first HTTP submission.</summary>
    public async Task<HouseholdJournalState> ClaimAsync(HouseholdServiceTransport transport, HouseholdInvitationPackage package,
        string independentlyConfirmedRootFingerprint, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var root = new PublicSigningRecipientIdentity(package.OwnerKeys.SigningKey, package.OwnerKeys.WrappingKey);
            if (package.Format != 1 || root.Fingerprint != independentlyConfirmedRootFingerprint || !HouseholdProtectionContext.IsFingerprint(package.Capability)) throw Refusal();
            var invitation = StandardJoseSignatures.VerifyJson(package.SignedInvitation, root, InvitationType,
                HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdInvitationStatement);
            var capability = Convert.FromHexString(package.Capability);
            try
            {
                if (invitation.Format != 1 || invitation.SpaceId == Guid.Empty || invitation.InvitationId == Guid.Empty || invitation.Role is not (HouseholdRole.Viewer or HouseholdRole.Editor) ||
                    invitation.ExpiresUnixSeconds <= time.GetUtcNow().ToUnixTimeSeconds() ||
                    invitation.CapabilityHash != Convert.ToHexStringLower(SHA256.HashData(capability))) throw Refusal();
            }
            finally { CryptographicOperations.ZeroMemory(capability); }
            var captured = await finances.ReviewAsync(ct);
            var identity = await transport.IdentityAsync(ct); var state = await journal.CaptureAsync(transport.Peer, identity.Id, ct);
            if (state is null)
            {
                using var freshDevice = SigningRecipientIdentity.Create();
                state = await journal.BeginDraftAsync(transport.Peer, identity.Id, root, false, freshDevice, null, package.SignedInvitation, ct, requireEmptyFinancial: true,
                    expectedGenerationPath: captured.GenerationPath);
            }
            if (state.Authority.GenerationPath != captured.GenerationPath || state.OwnerCreation || state.PinnedRoot != root || state.SignedInvitation != package.SignedInvitation ||
                state.SpaceId != Guid.Empty && state.SpaceId != invitation.SpaceId) throw Refusal();
            if (state.MembershipVersion != 0) return state;
            using var keys = await journal.ReadKeysAsync(state.Authority, ct);
            var intent = await journal.ReadSetupIntentAsync(state.Authority, false, ct);
            if (intent is null)
            {
                var statement = new HouseholdInvitationClaimStatement(1, invitation.SpaceId, invitation.InvitationId, state.Authority.SetupOperation,
                    identity.Id, HouseholdPublicationDigest.Ciphertext(package.SignedInvitation), root.Fingerprint, Public(keys.Device));
                intent = new(statement.OperationId, Sign(statement, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimStatement, keys.Device, ClaimType), package.Capability);
                await journal.RetainSetupIntentAsync(state.Authority, false, intent, ct);
            }
            if (intent.Capability != package.Capability || intent.OperationId != state.Authority.SetupOperation) throw Refusal();
            var claimed = await transport.ClaimAsync(invitation.SpaceId, invitation.InvitationId, new(intent.Capability, intent.SignedBody, Public(keys.Device)), ct);
            if (claimed.IdentityId != identity.Id || claimed.InvitationId != invitation.InvitationId || claimed.Fingerprint != keys.Device.Public.Fingerprint || claimed.ProposedRole != invitation.Role) throw Refusal();
            await journal.BindRemoteAsync(state.Authority, invitation.SpaceId, claimed.DeviceId, ct);
            return await RequireStateAsync(transport, state.Authority, ct);
        }
        finally { changing.Release(); }
    }

    /// <summary>Completes only actual accepted admission; pending404 never creates membership, keys or an empty financial upload.</summary>
    public async Task<bool> CompleteJoinAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        string reviewedName, CancellationToken ct = default)
    {
        RequireName(reviewedName);
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct); if (state.OwnerCreation || state.SpaceId == Guid.Empty) throw Refusal();
            try { _ = await transport.CheckpointAsync(state.SpaceId, state.DeviceId, ct); }
            catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.NotFound && state.MembershipVersion == 0) { return false; }
            state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            await ConnectAsync(transport, state, reviewedName, receiving: (await finances.ReviewAsync(ct)).RemoteProfile is null, ct);
            var outcome = await coordinator.SynchronizeAsync(transport, authority, ct);
            if (!outcome.Applied || outcome.NeedsReview) throw Refusal();
            await journal.CompleteSetupIntentAsync(authority, false, authority.SetupOperation, ct); return true;
        }
        finally { changing.Release(); }
    }

    /// <summary>Approves only the reviewed exact two-sided recipient proof and independently confirmed joint fingerprint.</summary>
    public async Task ApproveAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        HouseholdPendingClaim reviewed, string independentlyConfirmedDeviceFingerprint, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            using var keys = await RequireOwnerKeysAsync(transport, state, ct);
            if (await journal.ReadMembershipProposalAsync(authority, ct) is not null) throw Refusal();
            var actual = (await transport.PendingClaimsAsync(state.SpaceId, state.DeviceId, ct)).SingleOrDefault(row => row == reviewed) ?? throw Refusal();
            var recipient = new PublicSigningRecipientIdentity(actual.DeviceKeys.SigningKey, actual.DeviceKeys.WrappingKey);
            if (recipient.Fingerprint != independentlyConfirmedDeviceFingerprint) throw Refusal();
            var invitation = StandardJoseSignatures.VerifyJson(actual.SignedInvitation, keys.Owner!.Public, InvitationType,
                HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdInvitationStatement);
            var claim = StandardJoseSignatures.VerifyJson(actual.SignedClaim, recipient, ClaimType,
                HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimStatement);
            if (invitation.SpaceId != state.SpaceId || invitation.InvitationId != actual.InvitationId || invitation.ExpiresUnixSeconds <= time.GetUtcNow().ToUnixTimeSeconds() ||
                claim.Format != 1 || claim.SpaceId != state.SpaceId || claim.InvitationId != actual.InvitationId || claim.IdentityId != actual.IdentityId ||
                claim.DeviceKeys != actual.DeviceKeys || claim.InvitationHash != HouseholdPublicationDigest.Ciphertext(actual.SignedInvitation) || claim.OwnerFingerprint != keys.Owner.Public.Fingerprint) throw Refusal();
            var current = (await journal.ReadCurrentMembershipAsync(authority, ct)).Snapshot();
            var members = current.Members.Append(new HouseholdMemberDefinition(actual.IdentityId, invitation.Role,
                HouseholdPublicationDigest.Ciphertext(actual.SignedClaim), [new(actual.DeviceId, actual.DeviceKeys)])).ToArray();
            await PublishChangeAsync(transport, state, current, keys.Owner, HouseholdMembershipChange.AcceptMember, members, actual.InvitationId, actual.SignedClaim, actual.DeviceId, ct);
        }
        finally { changing.Release(); }
    }

    /// <summary>Changes only an original non-owner role or revokes that exact member, rotating away from its known financial key.</summary>
    public async Task ChangeMemberAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, long reviewedVersion,
        string memberIdentity, HouseholdRole? role, CancellationToken ct = default)
    {
        if (role is not (null or HouseholdRole.Editor or HouseholdRole.Viewer)) throw Refusal();
        await changing.WaitAsync(ct);
        try
        {
            var state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            using var keys = await RequireOwnerKeysAsync(transport, state, ct);
            if (await journal.ReadMembershipProposalAsync(authority, ct) is not null) throw Refusal();
            var current = (await journal.ReadCurrentMembershipAsync(authority, ct)).Snapshot();
            var selected = current.Members.SingleOrDefault(member => member.IdentityId == memberIdentity) ?? throw Refusal();
            if (current.Version != reviewedVersion || selected.Role == HouseholdRole.Owner || role == selected.Role) throw Refusal();
            var members = current.Members.Where(member => role is not null || member.IdentityId != memberIdentity)
                .Select(member => member.IdentityId == memberIdentity && role is { } next ? member with { Role = next } : member).ToArray();
            await PublishChangeAsync(transport, state, current, keys.Owner!, role is null ? HouseholdMembershipChange.RevokeMember : HouseholdMembershipChange.ChangeRole,
                members, null, null, null, ct);
        }
        finally { changing.Release(); }
    }

    private async Task PublishChangeAsync(HouseholdServiceTransport transport, HouseholdJournalState state, HouseholdMembershipStatement current,
        SigningRecipientIdentity root, HouseholdMembershipChange change, HouseholdMemberDefinition[] members, Guid? invitation,
        string? claim, Guid? admittedDevice, CancellationToken ct)
    {
        using var old = await journal.ReadEpochAsync(state.Authority, current.KeyEpoch, ct); using var next = old.Next();
        var statement = current with { OperationId = Guid.NewGuid(), ExpectedVersion = current.Version, Version = current.Version + 1,
            PreviousHash = state.MembershipHead, PreviousOwnerId = current.OwnerId, Change = change, InvitationId = invitation, KeyEpoch = next.Epoch, KeyCommitment = next.KeyCommitment,
            AddressKeyCommitment = next.AddressKeyCommitment, ValidUntilUnixSeconds = ProposalDeadline(), Members = members,
            PublicationCursor = state.Cursor, PublicationHead = state.PublicationHead };
        var historical = new List<HouseholdEncryptedGrant>();
        if (admittedDevice is { } id)
        {
            var device = members.SelectMany(member => member.Devices).Single(device => device.Id == id);
            if (current.KeyEpoch + members.Sum(member => member.Devices.Length) > 384) throw Refusal();
            // Keep verified original origins, not reinterpreted current metadata, for every old key.
            foreach (var epoch in Enumerable.Range(1, checked((int)current.KeyEpoch)))
            {
                using var material = await journal.ReadEpochAsync(state.Authority, epoch, ct);
                var origin = await journal.ReadEpochOriginAsync(state.Authority, epoch, ct);
                var body = material.GrantHistorical(cryptography, statement, origin, root, device);
                historical.Add(new(epoch, id, body, HouseholdPublicationDigest.Ciphertext(body)));
            }
        }
        var proposal = BuildPublication(state.DeviceId, statement, root, next, claim, historical);
        var before = await journal.ReadCurrentMembershipAsync(state.Authority, ct);
        var verified = HouseholdTrustChain.Advance(before, new(proposal.SignedStatement, null, claim, HouseholdPublicationDigest.Grants(proposal.Grants)));
        proposal = proposal with { OwnerRootGrants = statement.Members.Single(member => member.Role == HouseholdRole.Owner).Devices
            .Select(device => HouseholdRecoveryMaterial.GrantOwnerRoot(cryptography, verified, root, device.Id)).ToArray() };
        var recovery = await ObserveRecoveryAsync(transport, state, ct);
        if (recovery is not null)
        {
            var certificate = StandardJoseSignatures.VerifyJson(recovery.SignedEnrollment, before.Root, HouseholdRecoveryProofTypes.Enrollment,
                HouseholdRecoveryBounds.MetadataCharacters, HouseholdMetadataJsonContext.Default.HouseholdRecoveryCertificate);
            var kit = new PublicSigningRecipientIdentity(certificate.RecoveryKeys.SigningKey, certificate.RecoveryKeys.WrappingKey);
            var archive = HouseholdRecoveryMaterial.SealEpoch(cryptography, recovery.EnrollmentId, statement, statement, next, root, kit);
            proposal = proposal with { Recovery = new(HouseholdRecoveryMutationKind.Refresh, recovery.EnrollmentId, null, null, null, [archive]) };
        }
        await journal.StageMembershipProposalAsync(state.Authority, proposal, ct);
        _ = await transport.PublishMembershipAsync(state.SpaceId, proposal, ct);
        _ = await coordinator.RefreshAuthorityAsync(transport, state.Authority, ct);
        await journal.CompleteMembershipProposalAsync(state.Authority, ct);
    }

    private HouseholdPublishMembership BuildPublication(Guid device, HouseholdMembershipStatement statement, SigningRecipientIdentity root,
        HouseholdEpochMaterial epoch, string? claim, IEnumerable<HouseholdEncryptedGrant> historical)
    {
        var grants = statement.Members.SelectMany(member => member.Devices).Select(recipient =>
        { var body = epoch.Grant(cryptography, statement, root, recipient); return new HouseholdEncryptedGrant(epoch.Epoch, recipient.Id, body, HouseholdPublicationDigest.Ciphertext(body)); })
            .Concat(historical).ToArray();
        return new(device, Sign(statement, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement, root, MembershipType), null, claim, grants);
    }

    /// <summary>Explicitly resumes only the original retained administrative proposal, independently of a newly requested action.</summary>
    public async Task<HouseholdJournalState> ResumePendingAdministrationAsync(HouseholdServiceTransport transport,
        HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct);
            using var keys = await RequireOwnerKeysAsync(transport, state, ct);
            var pending = await journal.ReadMembershipProposalAsync(authority, ct) ?? throw Refusal();
            _ = await transport.PublishMembershipAsync(state.SpaceId, pending, ct);
            state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            await journal.CompleteMembershipProposalAsync(authority, ct);
            if (pending.Recovery?.Kind == HouseholdRecoveryMutationKind.Enroll) await journal.CompleteRecoveryPreparationAsync(authority, ct);
            return state;
        }
        finally { changing.Release(); }
    }

    private async Task ConnectAsync(HouseholdServiceTransport transport, HouseholdJournalState state, string name, bool receiving, CancellationToken ct)
    {
        // A crash can leave the real connection committed before its first financial apply.
        // A non-creating device must retain receive-only initialization until that atomic apply
        // records success; a connection row alone must not turn local defaults into proposals.
        receiving = !state.OwnerCreation && (receiving || (await finances.StatusAsync(ct)).LastSuccessAt is null);
        await finances.ConnectAsync(await finances.ReviewAsync(ct), transport.Peer, state.Authority.IdentityId, state.DeviceId,
            new(state.SpaceId, name, HouseholdFinancialSchema.Hash, state.Cursor, false), ct, receiveFirstRun: receiving, transportKind: ProfileSynchronizationKind.Household);
    }

    private async Task<HouseholdJournalKeys> RequireOwnerKeysAsync(HouseholdServiceTransport transport, HouseholdJournalState state, CancellationToken ct, bool allowOwnership = false)
    {
        if (!allowOwnership && await journal.ReadOwnershipAsync(state.Authority, ct) is { Stage: >= HouseholdOwnershipStage.Offered and <= HouseholdOwnershipStage.Accepted }) throw Refusal();
        var current = await journal.ReadCurrentMembershipAsync(state.Authority, ct); var keys = await journal.ReadKeysAsync(state.Authority, ct);
        if (current.Snapshot().OwnerId == state.Authority.IdentityId && keys.Owner is null)
        {
            keys.Dispose();
            await journal.RecordOwnerRootAsync(state.Authority, await transport.OwnerRootAsync(state.SpaceId, state.DeviceId, ct), ct);
            keys = await journal.ReadKeysAsync(state.Authority, ct);
        }
        try
        {
            if (current.Snapshot().OwnerId != state.Authority.IdentityId || keys.Owner?.Public != current.Root || current.Device(state.DeviceId, true) != keys.Device.Public) throw Refusal();
            return keys;
        }
        catch { keys.Dispose(); throw; }
    }

    private async Task<HouseholdJournalState> RequireStateAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, CancellationToken ct)
    {
        var identity = await transport.IdentityAsync(ct); if (identity.Id != authority.IdentityId) throw Refusal();
        var state = await journal.CaptureAsync(transport.Peer, identity.Id, ct) ?? throw Refusal(); if (state.Authority != authority) throw Refusal(); return state;
    }
    private long ProposalDeadline() => HouseholdLimits.ClientDeadline(time.GetUtcNow(), HouseholdLimits.MembershipProposalLifetime);
    private static void RequireName(string name)
    { if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || name != name.Trim()) throw new ArgumentException("A complete reviewed household profile name is required.", nameof(name)); }
    private static HouseholdPublicKeys Public(SigningRecipientIdentity identity) => new(identity.Public.SigningKey, identity.Public.WrappingKey);
    private static string Sign<T>(T value, JsonTypeInfo<T> json, SigningRecipientIdentity key, string type) => StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(value, json), key, type);
    private static InvalidOperationException Refusal() => new("The reviewed household setup or administration is no longer current.");
}
