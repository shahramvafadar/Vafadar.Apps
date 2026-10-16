using System.Net;
using System.Security.Cryptography;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Explicit password-encrypted export bytes; never display or log the file contents or retain its password.</summary>
public sealed class HouseholdRecoveryExport(byte[] content, string fingerprint) : IDisposable
{
    /// <summary>Owned encrypted file bytes for the platform's explicit Save action.</summary>
    public byte[] Content { get; } = content;
    /// <summary>Exact original kit identity to confirm only after the file was actually saved.</summary>
    public string Fingerprint { get; } = fingerprint;
    /// <inheritdoc />
    public void Dispose() => CryptographicOperations.ZeroMemory(Content);
    /// <inheritdoc />
    public override string ToString() => "Protected household recovery export";
}

public sealed partial class HouseholdWorkspace
{
    private sealed record SavedRecoveryFile(HouseholdJournalAuthority Authority, string Fingerprint);
    private SavedRecoveryFile? savedRecoveryFile;
    private sealed record PickedRecoveryFile(string Generation, string Peer, string Identity, byte[] Content);
    private readonly object recoveryFileState = new();
    private PickedRecoveryFile? pickedRecoveryFile;

    /// <summary>Retains only a bounded encrypted picker result in process memory for its original profile/peer/identity; app lock may retire the selecting page.</summary>
    public void RememberPickedRecoveryFile(string generationPath, LocalSyncPeer peer, string identity, byte[] encrypted)
    {
        if (generationPath.Length is < 1 or > 4096 || string.IsNullOrWhiteSpace(identity) || identity.Length > 128 ||
            encrypted.Length is < 64 or > 32768 || !Vafadar.Backup.Security.BackupEncryption.IsEncrypted(encrypted))
            throw new ArgumentException("A captured encrypted recovery selection is required.");
        var value = new PickedRecoveryFile(Path.GetFullPath(generationPath), peer.BindingKey(identity), identity, encrypted.ToArray());
        lock (recoveryFileState)
        {
            if (pickedRecoveryFile is not null) CryptographicOperations.ZeroMemory(pickedRecoveryFile.Content);
            pickedRecoveryFile = value;
        }
    }

    /// <summary>Transfers the encrypted selection only to a fresh same-profile/peer/identity flow; another generation cannot receive it.</summary>
    public byte[]? TakePickedRecoveryFile(string generationPath, LocalSyncPeer peer, string identity)
    {
        lock (recoveryFileState)
        {
            if (pickedRecoveryFile is not { } value || value.Generation != Path.GetFullPath(generationPath) || value.Peer != peer.BindingKey(identity) ||
                value.Identity != identity) return null;
            pickedRecoveryFile = null; return value.Content;
        }
    }

    /// <summary>Remembers only an actual completed native encrypted-file handoff in process memory, even if the native picker retired its page; this grants no storage/network authority.</summary>
    public void RememberSavedRecoveryFile(HouseholdJournalAuthority authority, string fingerprint)
    {
        ArgumentNullException.ThrowIfNull(authority);
        if (!HouseholdProtectionContext.IsFingerprint(fingerprint)) throw new ArgumentException("An exact kit identity is required.", nameof(fingerprint));
        Interlocked.Exchange(ref savedRecoveryFile, new(authority, fingerprint));
    }

    /// <summary>Returns only the same original captured profile's actual native handoff; another profile/generation cannot adopt it.</summary>
    public string? SavedRecoveryFileFingerprint(HouseholdJournalAuthority authority)
    {
        var saved = Volatile.Read(ref savedRecoveryFile); return saved?.Authority == authority ? saved.Fingerprint : null;
    }

    /// <summary>Creates or reexports only the original protected enrollment intent. Saving this file does not yet rotate roots or change membership.</summary>
    public async Task<HouseholdRecoveryExport> ExportRecoveryKitAsync(HouseholdServiceTransport transport,
        HouseholdJournalAuthority authority, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8) throw new ArgumentException("An explicit recovery-file password is required.", nameof(password));
        await changing.WaitAsync(ct);
        try
        {
            var state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            using var keys = await RequireOwnerKeysAsync(transport, state, ct);
            if (await journal.ReadMembershipProposalAsync(authority, ct) is not null) throw Refusal();
            using var existing = await journal.ReadRecoveryPreparationAsync(authority, ct);
            if (existing is null)
            {
                var current = await journal.ReadCurrentMembershipAsync(authority, ct);
                var previous = await ObserveRecoveryAsync(transport, state, ct);
                using var root = SigningRecipientIdentity.Create();
                using var kit = HouseholdRecoveryKit.Create(state.SpaceId, Guid.NewGuid(), authority.IdentityId, transport.Peer, Public(root));
                using var old = await journal.ReadEpochAsync(authority, current.Epoch, ct); using var epoch = old.Next();
                var ciphertext = HouseholdRecoveryMaterial.SealRoot(cryptography, state.SpaceId, kit.EnrollmentId, epoch.Epoch, root, kit.Public);
                var declaration = new HouseholdRecoverySeedDeclaration(1, state.SpaceId, Guid.NewGuid(), state.DeviceId, kit.EnrollmentId,
                    authority.IdentityId, current.Version, current.Hash, epoch.Epoch, Public(root), Public(kit.Public),
                    HouseholdPublicationDigest.Ciphertext(ciphertext), HouseholdLimits.ClientDeadline(time.GetUtcNow(), HouseholdRecoveryBounds.SeedLifetime));
                var seed = new HouseholdOpenRecoverySeed(state.DeviceId, Sign(declaration, HouseholdMetadataJsonContext.Default.HouseholdRecoverySeedDeclaration,
                    keys.Owner!, HouseholdRecoveryProofTypes.Seed), ciphertext);
                await journal.RetainRecoveryPreparationAsync(authority, seed, previous?.EnrollmentId, kit, root, epoch, ct);
                return new(kit.ExportEncrypted(password), kit.Public.Fingerprint);
            }
            using var original = HouseholdRecoveryKit.ImportFromProtectedStorage(existing.Kit);
            return new(original.ExportEncrypted(password), original.Public.Fingerprint);
        }
        finally { changing.Release(); }
    }

    /// <summary>Confirms only the exact protected file that the platform actually saved; it never stores the password or publishes automatically.</summary>
    public async Task ConfirmRecoveryExportAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        string fingerprint, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            _ = await RequireStateAsync(transport, authority, ct);
            await journal.ConfirmRecoveryExportAsync(authority, fingerprint, ct);
            var saved = Volatile.Read(ref savedRecoveryFile);
            if (saved?.Authority == authority && saved.Fingerprint == fingerprint) Interlocked.CompareExchange(ref savedRecoveryFile, null, saved);
        }
        finally { changing.Release(); }
    }

    /// <summary>Completes one original independently exported recovery enrollment, with persisted ciphertext pages and an atomic new-root publication.</summary>
    public async Task<HouseholdJournalState> CompleteRecoveryEnrollmentAsync(HouseholdServiceTransport transport,
        HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await RequireStateAsync(transport, authority, ct);
            var retained = await journal.ReadMembershipProposalAsync(authority, ct);
            if (retained is not null)
            {
                if (retained.Recovery?.Kind != HouseholdRecoveryMutationKind.Enroll) throw Refusal();
                _ = await transport.PublishMembershipAsync(state.SpaceId, retained, ct);
                state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
                await journal.CompleteMembershipProposalAsync(authority, ct);
                await journal.CompleteRecoveryPreparationAsync(authority, ct); return state;
            }
            state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            using var keys = await RequireOwnerKeysAsync(transport, state, ct);
            using var frame = await journal.ReadRecoveryPreparationAsync(authority, ct) ?? throw Refusal();
            if (!frame.ExportConfirmed) throw Refusal();
            using var root = SigningRecipientIdentity.ImportFromProtectedStorage(frame.Root);
            using var kit = HouseholdRecoveryKit.ImportFromProtectedStorage(frame.Kit);
            using var next = HouseholdEpochMaterial.ImportFromProtectedStorage(frame.Epoch);
            var before = await journal.ReadCurrentMembershipAsync(authority, ct);
            var declaration = StandardJoseSignatures.VerifyJson(frame.Seed.SignedDeclaration, before.Root, HouseholdRecoveryProofTypes.Seed,
                HouseholdRecoveryBounds.MetadataCharacters, HouseholdMetadataJsonContext.Default.HouseholdRecoverySeedDeclaration);
            if (declaration.ExpectedVersion != before.Version || declaration.ExpectedHead != before.Hash || declaration.TargetOwnerKeys != Public(root) ||
                declaration.TargetEpoch != next.Epoch || declaration.ValidUntilUnixSeconds <= time.GetUtcNow().ToUnixTimeSeconds()) throw Refusal();
            var progress = await transport.OpenRecoverySeedAsync(state.SpaceId, frame.Seed, ct);
            while (!progress.Complete)
            {
                if (progress.OperationId != declaration.OperationId || progress.ThroughEpoch != next.Epoch || progress.UploadedThroughEpoch < 0 ||
                    progress.UploadedThroughEpoch >= next.Epoch) throw Refusal();
                var page = await journal.RecoveryPreparationPageAsync(authority, frame, progress.UploadedThroughEpoch, ct);
                var result = await transport.UploadRecoveryEpochsAsync(state.SpaceId, declaration.OperationId, new(state.DeviceId, page), ct);
                if (result.UploadedThroughEpoch != page[^1].Epoch) throw Refusal(); progress = result;
            }
            if (progress.OperationId != declaration.OperationId || progress.ThroughEpoch != next.Epoch || progress.UploadedThroughEpoch != next.Epoch) throw Refusal();
            var statement = RotateStatement(state, before.Snapshot(), next, root, declaration.OperationId);
            var proposal = RootPublication(state.DeviceId, before, statement, keys.Owner!, root, next);
            var certificate = new HouseholdRecoveryCertificate(1, state.SpaceId, kit.EnrollmentId, authority.IdentityId, Public(root), Public(kit.Public),
                statement.Version, HouseholdTrustChain.Digest(proposal.SignedStatement), HouseholdPublicationDigest.Ciphertext(frame.Seed.RootCiphertext));
            proposal = proposal with { Recovery = new(HouseholdRecoveryMutationKind.Enroll, frame.ExistingEnrollmentId,
                Sign(certificate, HouseholdMetadataJsonContext.Default.HouseholdRecoveryCertificate, root, HouseholdRecoveryProofTypes.Enrollment),
                frame.Seed.RootCiphertext, certificate.RootCiphertextDigest, [], declaration.OperationId) };
            await journal.StageMembershipProposalAsync(authority, proposal, ct);
            _ = await transport.PublishMembershipAsync(state.SpaceId, proposal, ct);
            state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            await journal.CompleteMembershipProposalAsync(authority, ct); await journal.CompleteRecoveryPreparationAsync(authority, ct); return state;
        }
        finally { changing.Release(); }
    }

    /// <summary>Explicitly retires the current kit by replacing both owner key roles and the financial epoch; no financial row is posted or changed.</summary>
    public async Task<HouseholdJournalState> RevokeRecoveryAsync(HouseholdServiceTransport transport,
        HouseholdJournalAuthority authority, Guid reviewedEnrollment, long reviewedVersion, CancellationToken ct = default)
    {
        await changing.WaitAsync(ct);
        try
        {
            var state = await coordinator.RefreshAuthorityAsync(transport, authority, ct);
            using var keys = await RequireOwnerKeysAsync(transport, state, ct);
            if (state.MembershipVersion != reviewedVersion || await journal.ReadMembershipProposalAsync(authority, ct) is not null) throw Refusal();
            using var pending = await journal.ReadRecoveryPreparationAsync(authority, ct); if (pending is not null) throw Refusal();
            var observation = await ObserveRecoveryAsync(transport, state, ct) ?? throw Refusal();
            if (observation.EnrollmentId != reviewedEnrollment) throw Refusal();
            var before = await journal.ReadCurrentMembershipAsync(authority, ct);
            using var old = await journal.ReadEpochAsync(authority, before.Epoch, ct); using var next = old.Next(); using var root = SigningRecipientIdentity.Create();
            var operation = Guid.NewGuid(); var statement = RotateStatement(state, before.Snapshot(), next, root, operation);
            var proposal = RootPublication(state.DeviceId, before, statement, keys.Owner!, root, next) with
            { Recovery = new(HouseholdRecoveryMutationKind.Revoke, reviewedEnrollment, null, null, null, []) };
            await journal.StageOwnerRootAsync(authority, root, ct); await journal.StageEpochAsync(authority, operation, next, ct);
            await journal.StageMembershipProposalAsync(authority, proposal, ct);
            _ = await transport.PublishMembershipAsync(state.SpaceId, proposal, ct);
            state = await coordinator.RefreshAuthorityAsync(transport, authority, ct); await journal.CompleteMembershipProposalAsync(authority, ct); return state;
        }
        finally { changing.Release(); }
    }

    private HouseholdPublishMembership RootPublication(Guid device, HouseholdVerifiedMembership before, HouseholdMembershipStatement statement,
        SigningRecipientIdentity oldRoot, SigningRecipientIdentity root, HouseholdEpochMaterial epoch,
        Func<string, string, HouseholdRecoveryEvidence>? recoveryEvidence = null)
    {
        var grants = statement.Members.SelectMany(member => member.Devices).Select(recipient =>
        { var body = epoch.Grant(cryptography, statement, root, recipient); return new HouseholdEncryptedGrant(epoch.Epoch, recipient.Id, body, HouseholdPublicationDigest.Ciphertext(body)); }).ToArray();
        var signed = Sign(statement, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement, oldRoot, MembershipType);
        var grantsHash = HouseholdPublicationDigest.Grants(grants);
        var acceptance = Sign(new HouseholdOwnerAcceptanceStatement(1, statement.SpaceId, statement.OperationId, HouseholdTrustChain.Digest(signed), grantsHash),
            HouseholdMetadataJsonContext.Default.HouseholdOwnerAcceptanceStatement, root, "zanance-household-owner-acceptance+jwt");
        var evidence = recoveryEvidence?.Invoke(signed, grantsHash);
        var verified = HouseholdTrustChain.Advance(before, new(signed, acceptance, null, grantsHash, evidence));
        var rootGrants = statement.Members.Single(member => member.Role == HouseholdRole.Owner).Devices.Select(recipient =>
            HouseholdRecoveryMaterial.GrantOwnerRoot(cryptography, verified, root, recipient.Id)).ToArray();
        return new(device, signed, acceptance, null, grants, RecoveryEvidence: evidence, OwnerRootGrants: rootGrants);
    }

    private HouseholdMembershipStatement RotateStatement(HouseholdJournalState state, HouseholdMembershipStatement current,
        HouseholdEpochMaterial next, SigningRecipientIdentity root, Guid operation) => current with
    { OperationId = operation, ExpectedVersion = current.Version, Version = current.Version + 1, PreviousHash = state.MembershipHead,
        PreviousOwnerId = current.OwnerId, Change = HouseholdMembershipChange.RotateOwnerRoot, InvitationId = null, OwnerKeys = Public(root), KeyEpoch = next.Epoch,
        KeyCommitment = next.KeyCommitment, AddressKeyCommitment = next.AddressKeyCommitment, ValidUntilUnixSeconds = ProposalDeadline(),
        PublicationCursor = state.Cursor, PublicationHead = state.PublicationHead };

    private async Task<HouseholdRecoveryObservation?> ObserveRecoveryAsync(HouseholdServiceTransport transport, HouseholdJournalState state, CancellationToken ct)
    {
        HouseholdRecoveryObservation observation;
        try { observation = await transport.RecoveryAsync(state.SpaceId, ct); }
        catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.NotFound) { return null; }
        var current = await journal.ReadCurrentMembershipAsync(state.Authority, ct);
        var certificate = StandardJoseSignatures.VerifyJson(observation.SignedEnrollment, current.Root, HouseholdRecoveryProofTypes.Enrollment,
            HouseholdRecoveryBounds.MetadataCharacters, HouseholdMetadataJsonContext.Default.HouseholdRecoveryCertificate);
        var history = await journal.ReadHistoryAsync(state.Authority, ct);
        if (certificate.Format != 1 || certificate.SpaceId != state.SpaceId || certificate.OwnerId != state.Authority.IdentityId ||
            certificate.OwnerKeys != new HouseholdPublicKeys(current.Root.SigningKey, current.Root.WrappingKey) ||
            certificate.EnrollmentId != observation.EnrollmentId || certificate.MembershipVersion != observation.MembershipVersion ||
            certificate.MembershipVersion < 1 || certificate.MembershipVersion > current.Version ||
            certificate.MembershipHead != HouseholdTrustChain.Digest(history[checked((int)certificate.MembershipVersion - 1)].SignedStatement) ||
            certificate.RootCiphertextDigest != observation.RootDigest || observation.RootDigest != HouseholdPublicationDigest.Ciphertext(observation.RootCiphertext) ||
            observation.ThroughEpoch != current.Epoch) throw Refusal();
        return observation;
    }

    private static HouseholdPublicKeys Public(PublicSigningRecipientIdentity identity) => new(identity.SigningKey, identity.WrappingKey);
}
