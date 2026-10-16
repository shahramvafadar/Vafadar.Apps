using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public sealed partial class HouseholdWorkspace
{
    /// <summary>Checks the finite audit prefix against independently verified historical membership; service timestamps and unsigned operational actions remain service-reported.</summary>
    public async Task<HouseholdAuditPage> ReadAuditAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        long after, long? through, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct); var page = await transport.AuditAsync(state.SpaceId, state.DeviceId, after, through, ct);
            if (page.SpaceId != state.SpaceId || page.Items is null || page.Items.Length > 16 || page.Through < after || through is { } horizon && page.Through != horizon ||
                page.NextAfter != after + page.Items.Length || page.NextAfter > page.Through || page.Complete != (page.NextAfter == page.Through)) throw Refusal();
            var history = await journal.ReadHistoryAsync(authority, ct); var next = after;
            foreach (var item in page.Items)
            {
                if (item is null || item.Sequence != ++next || !Enum.IsDefined(item.Action) || item.DeviceId == Guid.Empty || item.OperationId == Guid.Empty ||
                    item.OccurredAtUnixSeconds <= 0 || item.Evidence is null) throw Refusal();
                var anchor = await journal.ReadMembershipVersionAsync(authority, item.MembershipVersion, ct);
                if (anchor.Hash != item.MembershipHead) throw Refusal();
                var actor = anchor.Snapshot().Members.SingleOrDefault(member => member.IdentityId == item.ActorId);
                if (item.Action == HouseholdAuditAction.MembershipPublished)
                {
                    if (item.Evidence.Membership is null || item.Evidence.SignedPublication is not null || item.MembershipVersion > history.Count ||
                        System.Text.Json.JsonSerializer.Serialize(item.Evidence.Membership, HouseholdMetadataJsonContext.Default.HouseholdMembershipProof) !=
                        System.Text.Json.JsonSerializer.Serialize(history[checked((int)item.MembershipVersion - 1)], HouseholdMetadataJsonContext.Default.HouseholdMembershipProof) ||
                        anchor.Snapshot().OperationId != item.OperationId) throw Refusal();
                    // Transfer/revocation may remove the signing source from the resulting
                    // roster; recovery may introduce its replacement. The verified original
                    // proof carries cryptographic consent; actor/time remain service metadata.
                    var preceding = item.MembershipVersion == 1 ? anchor : await journal.ReadMembershipVersionAsync(authority, item.MembershipVersion - 1, ct);
                    if (!anchor.Snapshot().Members.Concat(preceding.Snapshot().Members).Any(member => member.Role == HouseholdRole.Owner &&
                        member.IdentityId == item.ActorId && member.Devices.Any(device => device.Id == item.DeviceId))) throw Refusal();
                }
                else if (item.Action == HouseholdAuditAction.EncryptedUpdate)
                {
                    if (actor is null || !actor.Devices.Any(device => device.Id == item.DeviceId)) throw Refusal();
                    if (item.Evidence.Membership is not null || item.Evidence.SignedPublication is null) throw Refusal();
                    var statement = Vafadar.Cryptography.StandardJoseSignatures.VerifyJson(item.Evidence.SignedPublication, anchor.Device(item.DeviceId, true), "zanance-household-commit+jwt",
                        HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement);
                    if (statement.Format != 1 || statement.SpaceId != state.SpaceId || statement.DeviceId != item.DeviceId || statement.OperationId != item.OperationId ||
                        statement.MembershipVersion != item.MembershipVersion || statement.Cursor != item.PublicationCursor ||
                        HouseholdTrustChain.Digest(item.Evidence.SignedPublication) != item.PublicationHead) throw Refusal();
                }
                else if (actor is null || !actor.Devices.Any(device => device.Id == item.DeviceId) || item.Evidence.Membership is not null || item.Evidence.SignedPublication is not null) throw Refusal();
            }
            return page;
        }
        finally { changing.Release(); }
    }

    /// <summary>Refreshes service-reported lifecycle metadata without granting financial/key access in retained time.</summary>
    public async Task<HouseholdLifecycleStatus> ReadLifecycleAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct); var status = await transport.LifecycleAsync(state.SpaceId, state.DeviceId, ct);
            if (status.SpaceId != state.SpaceId || !Enum.IsDefined(status.Retention) || status.ObservedAtUnixSeconds <= 0 ||
                status.Retention != HouseholdAccessPolicy.Retention(DateTimeOffset.FromUnixTimeSeconds(status.ObservedAtUnixSeconds),
                    status.HostExpiresAtUnixSeconds is { } expiry ? DateTimeOffset.FromUnixTimeSeconds(expiry) : null, status.Retention == HouseholdRetentionState.Deleted)) throw Refusal();
            if (status.Retention is HouseholdRetentionState.Retained or HouseholdRetentionState.DeletionDue or HouseholdRetentionState.Deleted)
                await journal.RecordRemoteAccessRefusalAsync(authority, ct);
            return status;
        }
        finally { changing.Release(); }
    }

    /// <summary>Captures only owner-scoped opaque privacy metadata before confirmation, even when retention disallows financial reading.</summary>
    public async Task<HouseholdDeletionCheckpoint> ReviewDeletionCheckpointAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try { return await DeletionCheckpointCoreAsync(transport, await RequireStateAsync(transport, authority, ct), ct); }
        finally { changing.Release(); }
    }

    /// <summary>Retains the existing caller's reviewed position without requiring financial access or manufacturing a financial grant.</summary>
    public async Task<HouseholdDeletionIntent> BeginDeletionAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        long reviewedVersion, long reviewedCursor, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct);
            var checkpoint = await DeletionCheckpointCoreAsync(transport, state, ct);
            if (checkpoint.MembershipVersion != reviewedVersion || checkpoint.PublicationCursor != reviewedCursor) throw Refusal();
            return await BeginDeletionCoreAsync(transport, state, checkpoint, ct);
        }
        finally { changing.Release(); }
    }

    /// <summary>Rechecks the exact pre-confirmation metadata and seals original consent, without fetching financial content or deleting a local profile.</summary>
    public async Task<HouseholdDeletionIntent> BeginDeletionAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        HouseholdDeletionCheckpoint reviewed, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct); var checkpoint = await DeletionCheckpointCoreAsync(transport, state, ct);
            if (checkpoint with { ObservedAtUnixSeconds = reviewed.ObservedAtUnixSeconds } != reviewed) throw Refusal();
            return await BeginDeletionCoreAsync(transport, state, checkpoint, ct);
        }
        finally { changing.Release(); }
    }

    /// <summary>Only the current actual owner can read this metadata; its fingerprint must match an independently trusted existing root, never a new directory key.</summary>
    private async Task<HouseholdDeletionCheckpoint> DeletionCheckpointCoreAsync(HouseholdServiceTransport transport, HouseholdJournalState state, CancellationToken ct)
    {
        var checkpoint = await transport.DeletionCheckpointAsync(state.SpaceId, state.DeviceId, ct);
        var trusted = await journal.ReadCurrentMembershipAsync(state.Authority, ct);
        if (checkpoint.SpaceId != state.SpaceId || checkpoint.DeviceId != state.DeviceId || checkpoint.OwnerId != state.Authority.IdentityId ||
            trusted.Snapshot().OwnerId != state.Authority.IdentityId || checkpoint.RootFingerprint != trusted.Root.Fingerprint ||
            checkpoint.MembershipVersion < trusted.Version || !HouseholdProtectionContext.IsFingerprint(checkpoint.MembershipHead) ||
            checkpoint.MembershipVersion == trusted.Version && checkpoint.MembershipHead != trusted.Hash || checkpoint.PublicationCursor < 0 ||
            !HouseholdProtectionContext.IsFingerprint(checkpoint.PublicationHead) || checkpoint.ObservedAtUnixSeconds <= 0) throw Refusal();
        return checkpoint;
    }

    /// <summary>Privacy-only consent may use an unchanged historical root over a newer opaque checkpoint; it never adopts a roster, epoch or financial write authority.</summary>
    private async Task<HouseholdDeletionIntent> BeginDeletionCoreAsync(HouseholdServiceTransport transport, HouseholdJournalState state,
        HouseholdDeletionCheckpoint checkpoint, CancellationToken ct)
    {
        var authority = state.Authority;
        if (await journal.ReadDeletionAsync(authority, ct) is not null || await journal.ReadMembershipProposalAsync(authority, ct) is not null ||
            await journal.ReadOwnershipAsync(authority, ct) is not null) throw Refusal();
        using var preparation = await journal.ReadRecoveryPreparationAsync(authority, ct); if (preparation is not null) throw Refusal();
        var anchor = await journal.ReadCurrentMembershipAsync(authority, ct); using var keys = await journal.ReadKeysAsync(authority, ct);
        if (keys.Owner?.Public != anchor.Root || checkpoint.RootFingerprint != anchor.Root.Fingerprint || anchor.Device(state.DeviceId, true) != keys.Device.Public)
            throw Refusal();
        var declaration = new HouseholdDeleteStatement(1, state.SpaceId, Guid.NewGuid(), authority.IdentityId, state.DeviceId,
            checkpoint.MembershipVersion, checkpoint.MembershipHead, checkpoint.PublicationCursor, checkpoint.PublicationHead, ProposalDeadline());
        var signed = Sign(declaration, HouseholdMetadataJsonContext.Default.HouseholdDeleteStatement, keys.Owner, HouseholdLifecycleProtocol.DeleteType);
        var original = new HouseholdDeletionIntent(new(state.DeviceId, signed), anchor.Version);
        await journal.RetainDeletionAsync(authority, original, ct); return await SubmitDeletionAsync(transport, state, original, ct);
    }

    /// <summary>Resubmits exactly the original declaration or refreshes its minimal receipt; expired/uncertain work never generates another deletion.</summary>
    public async Task<HouseholdDeletionIntent> ResumeDeletionAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct); var original = await journal.ReadDeletionAsync(authority, ct) ?? throw Refusal();
            if (original.Rejected) return original;
            return await SubmitDeletionAsync(transport, state, original, ct);
        }
        finally { changing.Release(); }
    }

    /// <summary>Observes the original accepted receipt or locked service proof that its unaccepted UTC deadline passed; a mere HTTP failure never releases intent.</summary>
    public async Task<HouseholdDeletionIntent> ReviewDeletionAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct); var original = await journal.ReadDeletionAsync(authority, ct) ?? throw Refusal();
            var review = await transport.ReviewDeletionAsync(state.SpaceId, original.Request, ct);
            var anchor = await journal.ReadMembershipVersionAsync(authority, original.AnchorVersion, ct);
            var declaration = Vafadar.Cryptography.StandardJoseSignatures.VerifyJson(original.Request.SignedDeclaration, anchor.Root, HouseholdLifecycleProtocol.DeleteType,
                HouseholdLifecycleProtocol.DeclarationBytes, HouseholdMetadataJsonContext.Default.HouseholdDeleteStatement);
            if (review.SpaceId != state.SpaceId || review.OperationId != declaration.OperationId || review.RequestHash != HouseholdJournal.HouseholdMetadataProofHash(original.Request.SignedDeclaration) ||
                review.Rejected && review.Receipt is not null || original.Receipt is not null && review.Receipt is null) throw Refusal();
            var result = original with { Receipt = review.Receipt, Rejected = review.Rejected };
            await journal.RetainDeletionAsync(authority, result, ct); return result;
        }
        finally { changing.Release(); }
    }

    /// <summary>Explicitly finishes only an authenticated unaccepted expired operation, preserving local money, original keys and user pause state.</summary>
    public async Task FinishRejectedDeletionAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, Guid operation, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try { _ = await RequireStateAsync(transport, authority, ct); await journal.FinishRejectedDeletionAsync(authority, operation, ct); }
        finally { changing.Release(); }
    }

    /// <summary>Accepted deletion remains a permanent local write block. Retirement is a current service observation: an old database restore can require physical cleanup again.</summary>
    private async Task<HouseholdDeletionIntent> SubmitDeletionAsync(HouseholdServiceTransport transport, HouseholdJournalState state, HouseholdDeletionIntent original, CancellationToken ct)
    {
        var receipt = original.Receipt is { } known ? await transport.DeletionReceiptAsync(state.SpaceId, known.OperationId, ct)
            : await transport.DeleteSpaceAsync(state.SpaceId, original.Request, ct);
        var next = original with { Receipt = receipt }; await journal.RetainDeletionAsync(state.Authority, next, ct); return next;
    }
}
