using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Complete independently anchored original membership history, including public historical authority needed to verify old financial packets.</summary>
public sealed class HouseholdVerifiedHistory
{
    internal HouseholdVerifiedHistory(IEnumerable<HouseholdVerifiedMembership> states, long admissionVersion)
    { States = Array.AsReadOnly(states.ToArray()); AdmissionVersion = admissionVersion; }
    /// <summary>Exact verified increasing original generations, never unverified advertised directory keys.</summary>
    public IReadOnlyList<HouseholdVerifiedMembership> States { get; }
    /// <summary>Latest verified roster; a historical member may be absent/revoked here.</summary>
    public HouseholdVerifiedMembership Current => States[^1];
    /// <summary>Original independently confirmed explicit recipient admission.</summary>
    public long AdmissionVersion { get; }
}

/// <summary>Locally verified immutable membership checkpoint, created only from a pinned root and exact signed continuity.</summary>
public sealed class HouseholdVerifiedMembership
{
    private readonly HouseholdMembershipStatement statement;

    internal HouseholdVerifiedMembership(HouseholdMembershipStatement value, string hash)
    { statement = Copy(value); Hash = hash; }
    /// <summary>Digest pinned alongside this exact signed generation in protected local persistence.</summary>
    public string Hash { get; }
    /// <summary>Exact shared resource; a server cannot replace it with another identity's room.</summary>
    public Guid SpaceId => statement.SpaceId;
    /// <summary>Verified strictly increasing roster version.</summary>
    public long Version => statement.Version;
    /// <summary>Verified current key generation; membership effects always rotate it.</summary>
    public long Epoch => statement.KeyEpoch;
    /// <summary>Public current root selected by the prior trusted signature and independent transfer consent.</summary>
    public PublicSigningRecipientIdentity Root => new(statement.OwnerKeys.SigningKey, statement.OwnerKeys.WrappingKey);
    /// <summary>Returns a detached metadata snapshot; modifying its arrays never mutates trusted local authority.</summary>
    public HouseholdMembershipStatement Snapshot() => Copy(statement);

    /// <summary>Resolves only an explicitly approved public device inside this verified historical/current generation.</summary>
    public PublicSigningRecipientIdentity Device(Guid deviceId, bool requireWrite)
    {
        var member = statement.Members.SingleOrDefault(member => member.Devices.Any(device => device.Id == deviceId));
        if (member is null || requireWrite && member.Role == HouseholdRole.Viewer) throw HouseholdTrustChain.Refusal();
        var device = member.Devices.Single(device => device.Id == deviceId);
        return new(device.Keys.SigningKey, device.Keys.WrappingKey);
    }

    /// <summary>Opens a grant only for a device contained in this verified generation and checks both key commitments.</summary>
    public HouseholdEpochMaterial OpenGrant(HouseholdCryptography cryptography, string ciphertext, Guid deviceId, SigningRecipientIdentity recipient)
        => HouseholdEpochMaterial.OpenGrant(cryptography, statement, ciphertext, deviceId, recipient);

    /// <summary>Opens an explicitly rewrapped old key only against both this verified granting roster and its verified original epoch commitment.</summary>
    public HouseholdEpochMaterial OpenHistoricalGrant(HouseholdCryptography cryptography, HouseholdVerifiedMembership originalEpoch,
        string ciphertext, Guid deviceId, SigningRecipientIdentity recipient) =>
        HouseholdEpochMaterial.OpenHistoricalGrant(cryptography, statement, originalEpoch.Snapshot(), ciphertext, deviceId, recipient);

    /// <summary>Public immutable records still contain mutable arrays; copy them at both sides of the trust boundary.</summary>
    private static HouseholdMembershipStatement Copy(HouseholdMembershipStatement value) => value with
    { Members = value.Members.Select(member => member with { Devices = member.Devices.ToArray() }).ToArray() };
}

/// <summary>Recipient-side public proof verification; no signing key is selected from an incoming server directory.</summary>
public static partial class HouseholdTrustChain
{
    private const string MembershipType = "zanance-household-membership+jwt";
    private const string TransferType = "zanance-household-owner-acceptance+jwt";
    private const string ClaimType = "zanance-household-invitation-claim+jwt";
    private const string CommitType = "zanance-household-commit+jwt";
    private const string InvitationType = "zanance-household-invitation+jwt";

    /// <summary>Reopens the original recovered device from its independently imported kit anchor and exact public recovery consent; the kit need not remain enabled later.</summary>
    public static HouseholdVerifiedHistory RecoveredHistory(HouseholdRecoveryAnchor anchor, IReadOnlyList<HouseholdMembershipProof> history,
        string actualIdentity, Guid ownDeviceId, PublicSigningRecipientIdentity ownDevice)
    {
        if (anchor is null || anchor.Format != 1 || anchor.Kind != "zanance.household.recovered-anchor.v1" || anchor.SpaceId == Guid.Empty ||
            anchor.EnrollmentId == Guid.Empty || anchor.OwnerId != actualIdentity || anchor.InitialOwnerRoot is null || anchor.KitKeys is null ||
            ownDeviceId == Guid.Empty || history.Count is < 1 or > 4096 || history.Any(item => item is null || item.SignedStatement is null ||
                item.SignedStatement.Length > HouseholdLimits.MembershipBytes * 2) || history.Sum(item => (long)item.SignedStatement.Length) > 64L * 1024 * 1024) throw Refusal();
        var pinned = new PublicSigningRecipientIdentity(anchor.InitialOwnerRoot.SigningKey, anchor.InitialOwnerRoot.WrappingKey);
        using var signing = pinned.OpenSigningKey(); using var wrapping = pinned.OpenWrappingKey();
        var genesis = ReadUntrustedGenesis(history[0].SignedStatement);
        var state = Genesis(anchor.SpaceId, new(genesis.OwnerKeys.SigningKey, genesis.OwnerKeys.WrappingKey), history[0]);
        var states = new List<HouseholdVerifiedMembership>(); var anchored = false; long admission = 0;
        for (var index = 0; index < history.Count; index++)
        {
            if (index != 0) state = Advance(state, history[index]);
            states.Add(state);
            if (state.Root == pinned && state.Snapshot().OwnerId == actualIdentity) anchored = true;
            var roster = state.Snapshot();
            if (!anchored || roster.Change != HouseholdMembershipChange.RecoverOwner || roster.OwnerId != actualIdentity ||
                !roster.Members.Single(member => member.Role == HouseholdRole.Owner).Devices.Any(device => device.Id == ownDeviceId &&
                    device.Keys == new HouseholdPublicKeys(ownDevice.SigningKey, ownDevice.WrappingKey))) continue;
            var evidence = history[index].RecoveryEvidence ?? throw Refusal();
            var certificate = Read(evidence.SignedEnrollment, states[index - 1].Root, HouseholdRecoveryProofTypes.Enrollment,
                HouseholdMetadataJsonContext.Default.HouseholdRecoveryCertificate);
            if (certificate.EnrollmentId != anchor.EnrollmentId || certificate.RecoveryKeys != anchor.KitKeys) throw Refusal();
            admission = state.Version;
        }
        if (!anchored || admission == 0) throw Refusal();
        return new(states, admission);
    }

    /// <summary>Verifies an all-devices-lost history from the original password-protected kit anchor, without inventing an approved device.</summary>
    public static HouseholdVerifiedHistory RecoveryHistory(HouseholdRecoveryKit kit, IReadOnlyList<HouseholdMembershipProof> history,
        HouseholdRecoveryObservation observation, string actualIdentity)
    {
        ArgumentNullException.ThrowIfNull(kit); ArgumentNullException.ThrowIfNull(history);
        if (actualIdentity != kit.OwnerId || observation.EnrollmentId != kit.EnrollmentId || history.Count is < 1 or > 4096 ||
            history.Any(item => item is null || item.SignedStatement is null || item.SignedStatement.Length > HouseholdLimits.MembershipBytes * 2) ||
            history.Sum(item => (long)item.SignedStatement.Length) > 64L * 1024 * 1024) throw Refusal();
        var genesis = ReadUntrustedGenesis(history[0].SignedStatement);
        var state = Genesis(kit.SpaceId, new(genesis.OwnerKeys.SigningKey, genesis.OwnerKeys.WrappingKey), history[0]);
        var states = new List<HouseholdVerifiedMembership>(); long anchor = 0;
        var pinned = new PublicSigningRecipientIdentity(kit.InitialOwnerAnchor.SigningKey, kit.InitialOwnerAnchor.WrappingKey);
        for (var index = 0; index < history.Count; index++)
        {
            if (index != 0) state = Advance(state, history[index]);
            states.Add(state);
            if (anchor == 0 && state.Root == pinned && state.Snapshot().OwnerId == kit.OwnerId) anchor = state.Version;
        }
        if (anchor == 0 || state.Snapshot().OwnerId != actualIdentity || observation.ThroughEpoch != state.Epoch ||
            observation.PublicationCursor < state.Snapshot().PublicationCursor || !HouseholdProtectionContext.IsFingerprint(observation.PublicationHead) ||
            observation.PublicationCursor == state.Snapshot().PublicationCursor && observation.PublicationHead != state.Snapshot().PublicationHead ||
            observation.MembershipVersion < anchor || observation.MembershipVersion > state.Version ||
            observation.RootCiphertext is not { Length: > 0 and <= HouseholdRecoveryBounds.RootCiphertextCharacters } ||
            observation.RootDigest != HouseholdPublicationDigest.Ciphertext(observation.RootCiphertext)) throw Refusal();
        var certificate = Read(observation.SignedEnrollment, state.Root, HouseholdRecoveryProofTypes.Enrollment,
            HouseholdMetadataJsonContext.Default.HouseholdRecoveryCertificate);
        var atCertificate = states[checked((int)observation.MembershipVersion - 1)];
        if (certificate.Format != 1 || certificate.SpaceId != kit.SpaceId || certificate.EnrollmentId != kit.EnrollmentId ||
            certificate.OwnerId != actualIdentity || certificate.OwnerKeys != new HouseholdPublicKeys(state.Root.SigningKey, state.Root.WrappingKey) ||
            certificate.RecoveryKeys != new HouseholdPublicKeys(kit.Public.SigningKey, kit.Public.WrappingKey) ||
            certificate.MembershipVersion != observation.MembershipVersion || certificate.MembershipHead != atCertificate.Hash ||
            atCertificate.Root != state.Root || certificate.RootCiphertextDigest != observation.RootDigest) throw Refusal();
        return new(states, anchor);
    }

    /// <summary>Bootstraps a recipient from the independently confirmed invitation root/head, including legitimate roots transferred before that invitation.</summary>
    public static HouseholdVerifiedMembership Join(PublicSigningRecipientIdentity independentlyConfirmedRoot, string signedInvitation,
        IReadOnlyList<HouseholdMembershipProof> history, string actualIdentity, Guid claimedDevice, PublicSigningRecipientIdentity ownDevice)
        => JoinHistory(independentlyConfirmedRoot, signedInvitation, history, actualIdentity, claimedDevice, ownDevice).Current;

    /// <summary>Verifies the complete public history once. Historical-only verification preserves explicit past consent but never manufactures current membership or device authority.</summary>
    public static HouseholdVerifiedHistory JoinHistory(PublicSigningRecipientIdentity independentlyConfirmedRoot, string signedInvitation,
        IReadOnlyList<HouseholdMembershipProof> history, string actualIdentity, Guid claimedDevice, PublicSigningRecipientIdentity ownDevice,
        bool requireCurrentMembership = true)
    {
        ArgumentNullException.ThrowIfNull(history);
        if (history.Count is < 1 or > 4096 || string.IsNullOrWhiteSpace(actualIdentity) || actualIdentity.Length > 128 || claimedDevice == Guid.Empty ||
            history.Any(item => item is null || item.SignedStatement is null || item.SignedStatement.Length > HouseholdLimits.MembershipBytes * 2) ||
            history.Sum(item => (long)item.SignedStatement.Length) > 64L * 1024 * 1024) throw Refusal();
        var invitation = Read(signedInvitation, independentlyConfirmedRoot, InvitationType, HouseholdMetadataJsonContext.Default.HouseholdInvitationStatement);
        if (invitation.Format != 1 || invitation.SpaceId == Guid.Empty || invitation.InvitationId == Guid.Empty || invitation.OperationId == Guid.Empty ||
            invitation.MembershipVersion < 1 || !HouseholdProtectionContext.IsFingerprint(invitation.MembershipHead) ||
            !HouseholdProtectionContext.IsFingerprint(invitation.CapabilityHash) || invitation.ExpiresUnixSeconds < 1 ||
            invitation.Role is not (HouseholdRole.Editor or HouseholdRole.Viewer)) throw Refusal();
        // Only the original public root is read before authentication. It is NOT trusted merely because
        // genesis self-signs: the entire prefix must later reach the independently pinned signed invitation
        // head/root. A pre-invitation root transfer also requires that new root's exact acceptance signature.
        var genesis = ReadUntrustedGenesis(history[0].SignedStatement);
        var state = Genesis(invitation.SpaceId, new(genesis.OwnerKeys.SigningKey, genesis.OwnerKeys.WrappingKey), history[0]);
        var anchored = false; var accepted = false; long admissionVersion = 0; var states = new List<HouseholdVerifiedMembership>();
        for (var index = 0; index < history.Count; index++)
        {
            if (index != 0) state = Advance(state, history[index]);
            states.Add(state);
            if (state.Version == invitation.MembershipVersion)
            {
                if (state.Hash != invitation.MembershipHead || state.Root != independentlyConfirmedRoot) throw Refusal();
                anchored = true;
            }
            if (state.Version <= invitation.MembershipVersion) continue;
            var roster = state.Snapshot();
            if (roster.Change == HouseholdMembershipChange.AcceptMember && roster.InvitationId == invitation.InvitationId)
            {
                var member = roster.Members.SingleOrDefault(member => member.IdentityId == actualIdentity);
                if (member is null || member.Role != invitation.Role || member.Devices.Length != 1 || member.Devices[0].Id != claimedDevice ||
                    member.Devices[0].Keys != new HouseholdPublicKeys(ownDevice.SigningKey, ownDevice.WrappingKey) || history[index].SignedAcceptanceClaim is not { } claim)
                    throw Refusal();
                var consent = Read(claim, ownDevice, ClaimType, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimStatement);
                if (consent.InvitationHash != Digest(signedInvitation)) throw Refusal();
                accepted = true; admissionVersion = state.Version;
            }
        }
        if (!anchored || !accepted) throw Refusal();
        if (requireCurrentMembership && (state.Snapshot().Members.SingleOrDefault(member => member.IdentityId == actualIdentity) is not { } current ||
            !current.Devices.Any(device => device.Id == claimedDevice && device.Keys == new HouseholdPublicKeys(ownDevice.SigningKey, ownDevice.WrappingKey)))) throw Refusal();
        return new(states, admissionVersion);
    }

    /// <summary>Establishes only the original exact independently pinned owner root/room, never an incoming advertised key.</summary>
    public static HouseholdVerifiedMembership Genesis(Guid expectedSpace, PublicSigningRecipientIdentity pinnedRoot, HouseholdMembershipProof proof)
    {
        var statement = Read(proof.SignedStatement, pinnedRoot, MembershipType, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement);
        RequireKeys(statement);
        if (statement.SpaceId != expectedSpace || statement.OwnerKeys != new HouseholdPublicKeys(pinnedRoot.SigningKey, pinnedRoot.WrappingKey) ||
            HouseholdMembershipRules.ValidateContinuity(statement, null, new('0', 64)).Count != 0 ||
            HouseholdMembershipRules.ValidateRosterTransition(statement, null).Count != 0 || proof.TransferAcceptance is not null ||
            proof.SignedAcceptanceClaim is not null || !HouseholdProtectionContext.IsFingerprint(proof.GrantsHash)) throw Refusal();
        return new(statement, Digest(proof.SignedStatement));
    }

    /// <summary>Advances an established checkpoint by one exact prior-root-signed generation; silent rollback, hidden effects and directory substitution fail closed.</summary>
    public static HouseholdVerifiedMembership Advance(HouseholdVerifiedMembership previous, HouseholdMembershipProof proof)
    {
        ArgumentNullException.ThrowIfNull(previous);
        var before = previous.Snapshot();
        var next = Read(proof.SignedStatement, previous.Root, MembershipType, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement);
        RequireKeys(next);
        if (HouseholdMembershipRules.ValidateContinuity(next, before, previous.Hash).Count != 0 ||
            HouseholdMembershipRules.ValidateRosterTransition(next, before).Count != 0 || !HouseholdProtectionContext.IsFingerprint(proof.GrantsHash)) throw Refusal();
        if (next.Change == HouseholdMembershipChange.AcceptMember)
        {
            if (proof.SignedAcceptanceClaim is null) throw Refusal();
            var accepted = next.Members.Single(member => !before.Members.Any(old => old.IdentityId == member.IdentityId));
            var device = accepted.Devices.Single();
            var claim = Read(proof.SignedAcceptanceClaim, new(device.Keys.SigningKey, device.Keys.WrappingKey), ClaimType,
                HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimStatement);
            if (accepted.AcceptanceHash != Digest(proof.SignedAcceptanceClaim) || claim.Format != 1 || claim.SpaceId != next.SpaceId ||
                claim.InvitationId != next.InvitationId || claim.OperationId == Guid.Empty || claim.IdentityId != accepted.IdentityId ||
                claim.DeviceKeys != device.Keys || claim.OwnerFingerprint != previous.Root.Fingerprint ||
                !HouseholdProtectionContext.IsFingerprint(claim.InvitationHash)) throw Refusal();
        }
        else if (proof.SignedAcceptanceClaim is not null) throw Refusal();
        if (next.OwnerKeys != before.OwnerKeys)
        {
            if (proof.TransferAcceptance is null) throw Refusal();
            var consent = Read(proof.TransferAcceptance, new(next.OwnerKeys.SigningKey, next.OwnerKeys.WrappingKey), TransferType,
                HouseholdMetadataJsonContext.Default.HouseholdOwnerAcceptanceStatement);
            if (consent.Format != 1 || consent.SpaceId != next.SpaceId || consent.OperationId != next.OperationId ||
                consent.MembershipStatementHash != Digest(proof.SignedStatement) || consent.GrantsHash != proof.GrantsHash) throw Refusal();
        }
        else if (proof.TransferAcceptance is not null) throw Refusal();
        if (next.Change == HouseholdMembershipChange.RecoverOwner)
        {
            if (proof.RecoveryEvidence is not { } evidence) throw Refusal();
            var certificate = Read(evidence.SignedEnrollment, previous.Root, HouseholdRecoveryProofTypes.Enrollment,
                HouseholdMetadataJsonContext.Default.HouseholdRecoveryCertificate);
            if (certificate.Format != 1 || certificate.SpaceId != before.SpaceId || certificate.OwnerId != before.OwnerId ||
                certificate.OwnerKeys != before.OwnerKeys || certificate.EnrollmentId == Guid.Empty || certificate.MembershipVersion < 1 ||
                certificate.MembershipVersion > before.Version || !HouseholdProtectionContext.IsFingerprint(certificate.MembershipHead) ||
                !HouseholdProtectionContext.IsFingerprint(certificate.RootCiphertextDigest) || certificate.RecoveryKeys is null) throw Refusal();
            var kit = new PublicSigningRecipientIdentity(certificate.RecoveryKeys.SigningKey, certificate.RecoveryKeys.WrappingKey);
            using var signer = kit.OpenSigningKey(); using var recipient = kit.OpenWrappingKey();
            if (kit.SigningKey == previous.Root.SigningKey || kit.WrappingKey == previous.Root.WrappingKey) throw Refusal();
            var consent = Read(evidence.SignedConsent, kit, HouseholdRecoveryProofTypes.Consent,
                HouseholdMetadataJsonContext.Default.HouseholdRecoveryConsent);
            var newDevice = next.Members.Single(member => member.Role == HouseholdRole.Owner).Devices.Single();
            if (consent.Format != 1 || consent.SpaceId != next.SpaceId || consent.EnrollmentId != certificate.EnrollmentId ||
                consent.OperationId != next.OperationId || consent.IdentityId != next.OwnerId || consent.SessionId == Guid.Empty ||
                consent.DeviceId != newDevice.Id || consent.MembershipStatementHash != Digest(proof.SignedStatement) || consent.GrantsHash != proof.GrantsHash) throw Refusal();
        }
        else if (proof.RecoveryEvidence is not null) throw Refusal();
        return new(next, Digest(proof.SignedStatement));
    }

    /// <summary>Verifies one complete signed publication before any decrypted record can be applied to the financial graph.</summary>
    public static HouseholdCommitStatement Publication(HouseholdVerifiedMembership membershipAtPublication, HouseholdPublicationReceipt receipt,
        Guid expectedDevice, Guid expectedOperation, Guid expectedAttempt, long expectedCursor, string previousHead)
    {
        if (receipt is null || receipt.RoundId == Guid.Empty || receipt.Parts is null) throw Refusal();
        var author = membershipAtPublication.Device(expectedDevice, requireWrite: true);
        var value = Read(receipt.SignedCommit, author, CommitType, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement);
        string manifest;
        try { manifest = HouseholdPublicationDigest.Manifest(receipt.Parts); }
        catch (ArgumentException) { throw Refusal(); }
        if (value.Format != 1 || value.SpaceId != membershipAtPublication.SpaceId || value.DeviceId != expectedDevice ||
            value.OperationId != expectedOperation || value.AttemptId != expectedAttempt || expectedCursor is < 0 or long.MaxValue ||
            value.ExpectedCursor != expectedCursor || value.Cursor != expectedCursor + 1 || value.PreviousHead != previousHead ||
            !HouseholdProtectionContext.IsFingerprint(previousHead) || value.KeyEpoch != membershipAtPublication.Epoch ||
            value.MembershipVersion != membershipAtPublication.Version || value.Parts != receipt.Parts.Length || value.ContentHash != manifest) throw Refusal();
        var boundary = membershipAtPublication.Snapshot();
        if (expectedCursor < boundary.PublicationCursor || expectedCursor == boundary.PublicationCursor && previousHead != boundary.PublicationHead)
            throw Refusal();
        return value;
    }

    /// <summary>A known subsequent root transition ends the old epoch exactly at its signed financial cutoff, even for otherwise valid old private signatures.</summary>
    public static void RequirePublicationHorizon(HouseholdVerifiedMembership atPublication, HouseholdVerifiedMembership nextMembership,
        HouseholdPublicationReceipt receipt, long cursor)
    {
        var after = nextMembership.Snapshot();
        if (nextMembership.SpaceId != atPublication.SpaceId || nextMembership.Version != atPublication.Version + 1 || after.PreviousHash != atPublication.Hash ||
            cursor > after.PublicationCursor || cursor == after.PublicationCursor && Digest(receipt.SignedCommit) != after.PublicationHead) throw Refusal();
    }

    /// <summary>Exact public signed-byte identity; callers persist this checkpoint, never a service-supplied unverified counter.</summary>
    public static string Digest(string signed)
    {
        if (string.IsNullOrEmpty(signed) || signed.Length > HouseholdLimits.MembershipBytes * 2) throw Refusal();
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(signed)));
    }

    /// <summary>All public keys are imported with exact standard algorithms/canonical DER before becoming trusted device/root identities.</summary>
    private static void RequireKeys(HouseholdMembershipStatement value)
    {
        if (HouseholdMembershipRules.ValidateShape(value).Count != 0) throw Refusal();
        var root = new PublicSigningRecipientIdentity(value.OwnerKeys.SigningKey, value.OwnerKeys.WrappingKey);
        using var signing = root.OpenSigningKey(); using var wrapping = root.OpenWrappingKey();
        var seen = new HashSet<string>(StringComparer.Ordinal) { root.Fingerprint };
        foreach (var device in value.Members.SelectMany(member => member.Devices))
        {
            var key = new PublicSigningRecipientIdentity(device.Keys.SigningKey, device.Keys.WrappingKey);
            using var signer = key.OpenSigningKey(); using var recipient = key.OpenWrappingKey();
            if (!seen.Add(key.Fingerprint)) throw Refusal();
        }
    }

    /// <summary>Bounds the unauthenticated public genesis key hint; this result can only enter the complete independently anchored Join path.</summary>
    private static HouseholdMembershipStatement ReadUntrustedGenesis(string token)
    {
        byte[]? payload = null;
        try
        {
            if (token.Count(character => character == '.') != 2) throw Refusal();
            var segment = token.AsSpan(); var start = segment.IndexOf('.') + 1; var end = segment[start..].IndexOf('.');
            var encoded = segment.Slice(start, end);
            if (encoded.Length > (HouseholdLimits.MembershipBytes + 2) / 3 * 4 || encoded.Length % 4 == 1 ||
                encoded.ContainsAnyExcept("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_")) throw Refusal();
            var base64 = encoded.ToString().Replace('-', '+').Replace('_', '/'); base64 = base64.PadRight((base64.Length + 3) / 4 * 4, '=');
            payload = Convert.FromBase64String(base64);
            using (JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 16 })) { }
            var value = JsonSerializer.Deserialize(payload, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement) ?? throw Refusal();
            RequireKeys(value); return value;
        }
        catch (Exception error) when (error is ArgumentException or FormatException or JsonException or InvalidOperationException)
        { throw Refusal(); }
        finally { if (payload is not null) CryptographicOperations.ZeroMemory(payload); }
    }

    /// <summary>The maintained standard verifier plus one strict duplicate-free source-generated parser is shared with service metadata verification.</summary>
    private static T Read<T>(string signed, PublicSigningRecipientIdentity trusted, string type, JsonTypeInfo<T> json) where T : class =>
        StandardJoseSignatures.VerifyJson(signed, trusted, type, HouseholdLimits.MembershipBytes, json);
    /// <summary>Content-free failure without signed body, key material or raw financial data.</summary>
    internal static CryptographicException Refusal() => new("Household trusted authority was refused.");
}
