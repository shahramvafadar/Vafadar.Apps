using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Original recipient-side trust/key lifecycle, independent of a server directory or a paid entitlement.</summary>
[Trait("AT", "AT-294")]
public sealed class HouseholdTrustChainTests
{
    /// <summary>Original source consent fixes the former role, selected approved device and financial checkpoint independently of valid new-root signatures.</summary>
    [Theory]
    [InlineData(HouseholdFormerOwnerRole.Leave)]
    [InlineData(HouseholdFormerOwnerRole.Viewer)]
    [InlineData(HouseholdFormerOwnerRole.Editor)]
    public void Ownership_offer_cannot_change_former_role_selected_device_or_financial_checkpoint(HouseholdFormerOwnerRole disposition)
    {
        using var oldRoot = SigningRecipientIdentity.Create(); using var newRoot = SigningRecipientIdentity.Create();
        using var oldDevice = SigningRecipientIdentity.Create(); using var newDevice = SigningRecipientIdentity.Create();
        using var epoch = HouseholdEpochMaterial.Create(); using var nextEpoch = epoch.Next();
        var oldId = Guid.NewGuid(); var newId = Guid.NewGuid(); var room = Guid.NewGuid(); var zero = new string('0', 64);
        var owner = new HouseholdMemberDefinition("fictitious-owner", HouseholdRole.Owner, zero, [new(oldId, Public(oldDevice))]);
        var target = new HouseholdMemberDefinition("fictitious-target", HouseholdRole.Editor, zero, [new(newId, Public(newDevice))]);
        var before = Statement(room, oldRoot, epoch, 2, new('a', 64), HouseholdMembershipChange.ChangeRole, [owner, target]);
        var operation = Guid.NewGuid(); var head = new string('b', 64); var publication = new string('c', 64);
        var offer = new HouseholdOwnershipOffer(1, room, operation, oldId, owner.IdentityId, newId, target.IdentityId,
            disposition, before.Version, head, 17, publication, before.ValidUntilUnixSeconds);
        var roster = disposition == HouseholdFormerOwnerRole.Leave ? new[] { target with { Role = HouseholdRole.Owner } } :
            new[] { owner with { Role = disposition == HouseholdFormerOwnerRole.Editor ? HouseholdRole.Editor : HouseholdRole.Viewer }, target with { Role = HouseholdRole.Owner } };
        var next = before with { OperationId = operation, Change = HouseholdMembershipChange.TransferOwner, ExpectedVersion = before.Version,
            Version = before.Version + 1, PreviousHash = head, OwnerId = target.IdentityId, OwnerKeys = Public(newRoot), Members = roster,
            KeyEpoch = nextEpoch.Epoch, KeyCommitment = nextEpoch.KeyCommitment, AddressKeyCommitment = nextEpoch.AddressKeyCommitment,
            PublicationCursor = 17, PublicationHead = publication };
        Assert.Empty(HouseholdMembershipRules.ValidateShape(next));
        Assert.Empty(HouseholdMembershipRules.ValidateRosterTransition(next, before));
        Assert.True(HouseholdOwnershipProtocol.Matches(offer, before, next));
        Assert.False(HouseholdOwnershipProtocol.Matches(offer with { TargetDeviceId = Guid.NewGuid() }, before, next));
        Assert.False(HouseholdOwnershipProtocol.Matches(offer with { OwnerDeviceId = Guid.NewGuid() }, before, next));
        Assert.False(HouseholdOwnershipProtocol.Matches(offer, before, next with { PublicationCursor = 18 }));
        Assert.False(HouseholdOwnershipProtocol.Matches(offer, before, next with { PublicationHead = new('d', 64) }));
        Assert.False(HouseholdOwnershipProtocol.Matches(offer, before, next with { PreviousOwnerId = target.IdentityId }));
        var altered = offer with { FormerRole = disposition == HouseholdFormerOwnerRole.Viewer ? HouseholdFormerOwnerRole.Editor : HouseholdFormerOwnerRole.Viewer };
        Assert.False(HouseholdOwnershipProtocol.Matches(altered, before, next));
        var signed = Sign(offer, HouseholdMetadataJsonContext.Default.HouseholdOwnershipOffer, oldRoot, HouseholdOwnershipProtocol.OfferType);
        Assert.Throws<CryptographicException>(() => StandardJoseSignatures.VerifyJson(signed, oldRoot.Public, HouseholdOwnershipProtocol.DeviceProofType,
            8192, HouseholdMetadataJsonContext.Default.HouseholdOwnershipOffer));
    }

    /// <summary>A public existing-member package anchors history but only the later exact original owner approval admits this independent device.</summary>
    [Fact]
    public void Existing_member_device_anchor_requires_exact_identity_checkpoint_key_and_later_approval_and_retains_revoked_history_without_current_access()
    {
        using var root = SigningRecipientIdentity.Create(); using var owner = SigningRecipientIdentity.Create(); using var device = SigningRecipientIdentity.Create();
        using var different = SigningRecipientIdentity.Create(); using var epoch1 = HouseholdEpochMaterial.Create(); using var epoch2 = epoch1.Next(); using var epoch3 = epoch2.Next();
        var room = Guid.NewGuid(); var ownerId = Guid.NewGuid(); var newId = Guid.NewGuid(); var zero = new string('0', 64);
        var member = new HouseholdMemberDefinition("fictitious-owner", HouseholdRole.Owner, zero, [new(ownerId, Public(owner))]);
        var first = Proof(Statement(room, root, epoch1, 0, zero, HouseholdMembershipChange.Genesis, [member]), root);
        var genesis = HouseholdTrustChain.Genesis(room, root.Public, first);
        var authorization = new HouseholdDeviceAccessStatement(1, room, Guid.NewGuid(), 1, genesis.Hash, member.IdentityId, 1);
        HouseholdDeviceAccessPackage Package(HouseholdDeviceAccessStatement value) => new(1, HouseholdSetupAnchors.DeviceAccess, Public(root),
            Sign(value, HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessStatement, root, HouseholdSetupAnchors.DeviceAccessProof));
        var package = Package(authorization); var approved = member with { Devices = [..member.Devices, new(newId, Public(device))] };
        var second = Proof(Statement(room, root, epoch2, 1, genesis.Hash, HouseholdMembershipChange.ApproveDevice, [approved]), root);
        var current = HouseholdTrustChain.Advance(genesis, second); HouseholdMembershipProof[] history = [first, second];
        Assert.Equal(2, HouseholdTrustChain.DeviceHistory(package, history, member.IdentityId, newId, device.Public).AdmissionVersion);
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.DeviceHistory(package, [first], member.IdentityId, newId, device.Public));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.DeviceHistory(package, history, "another-identity", newId, device.Public));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.DeviceHistory(package, history, member.IdentityId, newId, different.Public));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.DeviceHistory(Package(authorization with { MembershipHead = new('f', 64) }), history, member.IdentityId, newId, device.Public));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.DeviceHistory(package with { Kind = HouseholdSetupAnchors.OwnerRecovery }, history, member.IdentityId, newId, device.Public));
        using var rotatedRoot = SigningRecipientIdentity.Create();
        var thirdStatement = Statement(room, root, epoch3, 2, current.Hash, HouseholdMembershipChange.RevokeDevice, [member]) with { OwnerKeys = Public(rotatedRoot) };
        var third = Proof(thirdStatement, root);
        third = third with { TransferAcceptance = Sign(new HouseholdOwnerAcceptanceStatement(1, room, thirdStatement.OperationId,
            HouseholdTrustChain.Digest(third.SignedStatement), third.GrantsHash), HouseholdMetadataJsonContext.Default.HouseholdOwnerAcceptanceStatement,
            rotatedRoot, "zanance-household-owner-acceptance+jwt") };
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.DeviceHistory(package, [..history, third], member.IdentityId, newId, device.Public));
        Assert.Equal(2, HouseholdTrustChain.DeviceHistory(package, [..history, third], member.IdentityId, newId, device.Public, requireCurrentMembership: false).AdmissionVersion);
        Assert.NotEqual(HouseholdSetupAnchors.OwnerRecovery, HouseholdSetupAnchors.Kind(JsonSerializer.Serialize(package, HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessPackage)));
        Assert.Null(HouseholdSetupAnchors.Kind("{\"kind\":\"one\",\"kind\":\"two\"}"));
    }

    /// <summary>An old approved private signature cannot create new financial history beyond a known later root-signed epoch cutoff.</summary>
    [Fact]
    public void Known_membership_transition_rejects_old_epoch_forgery_and_same_cursor_fork_before_any_financial_state_change()
    {
        using var root = SigningRecipientIdentity.Create(); using var author = SigningRecipientIdentity.Create(); using var another = SigningRecipientIdentity.Create();
        using var epoch1 = HouseholdEpochMaterial.Create(); using var epoch2 = epoch1.Next();
        var room = Guid.NewGuid(); var device = Guid.NewGuid(); var zero = new string('0', 64); var operation = Guid.NewGuid(); var attempt = Guid.NewGuid();
        var definition = new HouseholdMemberDefinition("fictitious-owner", HouseholdRole.Owner, zero, [new(device, Public(author))]);
        var initial = Statement(room, root, epoch1, 0, zero, HouseholdMembershipChange.Genesis, [definition]);
        var before = HouseholdTrustChain.Genesis(room, root.Public, Proof(initial, root));
        var context = new HouseholdProtectionContext(room, 1, operation, HouseholdProtectionPurpose.Financial, "members");
        var crypto = new HouseholdCryptography(TimeProvider.System);
        var ciphertext = epoch1.Encrypt(crypto, context, "Fictitious old-key candidate"u8, author);
        var part = new HouseholdPartCommitment(0, epoch1.RecordAddress("fictitious", "candidate"), 0, HouseholdPublicationDigest.Ciphertext(ciphertext));
        var statement = new HouseholdCommitStatement(1, room, device, operation, attempt, 0, 1, 1, 1, zero, 1, HouseholdPublicationDigest.Manifest([part]));
        var signed = Sign(statement, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement, author, "zanance-household-commit+jwt");
        var receipt = new HouseholdPublicationReceipt(Guid.NewGuid(), signed, [part]);
        // The original key/signature itself remains cryptographically valid: only the known later signed
        // financial horizon can distinguish this prohibited old-epoch publication from authorized history.
        Assert.Equal(1, HouseholdTrustChain.Publication(before, receipt, device, operation, attempt, 0, zero).Cursor);
        var nextRoster = definition with { Devices = [..definition.Devices, new(Guid.NewGuid(), Public(another))] };
        var rotation = Statement(room, root, epoch2, 1, before.Hash, HouseholdMembershipChange.ApproveDevice, [nextRoster]);
        var immediatelyRotated = HouseholdTrustChain.Advance(before, Proof(rotation, root));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.RequirePublicationHorizon(before, immediatelyRotated, receipt, 1));
        using var replica = new HouseholdFinancialReplica(room);
        var events = new[] { new HouseholdOpaqueEvent(1, receipt.RoundId, part.RecordId, 1, 1, device, operation, attempt, 0, 0, ciphertext, part.Digest) };
        Assert.Throws<CryptographicException>(() => replica.Apply(before, epoch1, receipt, events, crypto, immediatelyRotated));
        Assert.Equal(0, replica.Cursor); Assert.Equal(0, replica.FinancialCursor); Assert.Empty(replica.Snapshot());
        var afterCommitted = HouseholdTrustChain.Advance(before, Proof(rotation with { PublicationCursor = 1, PublicationHead = HouseholdTrustChain.Digest(signed) }, root));
        HouseholdTrustChain.RequirePublicationHorizon(before, afterCommitted, receipt, 1);
        var fork = receipt with { SignedCommit = Sign(statement with { OperationId = Guid.NewGuid() }, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement,
            author, "zanance-household-commit+jwt") };
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.RequirePublicationHorizon(before, afterCommitted, fork, 1));
        Assert.Throws<CryptographicException>(() => replica.Prepare(afterCommitted, epoch2, device, author, Guid.NewGuid(), Guid.NewGuid(), [], [],
            new Dictionary<string, byte[]>(), crypto));
    }

    /// <summary>A new recipient anchors the complete historical chain at its independently confirmed invitation, even after a legitimate previous owner transfer.</summary>
    [Fact]
    public void Invitation_bootstrap_accepts_prior_root_transfer_but_refuses_unanchored_or_unapproved_membership()
    {
        using var originalRoot = SigningRecipientIdentity.Create(); using var nextRoot = SigningRecipientIdentity.Create();
        using var firstDevice = SigningRecipientIdentity.Create(); using var ownerDevice = SigningRecipientIdentity.Create(); using var joiningDevice = SigningRecipientIdentity.Create();
        using var epoch1 = HouseholdEpochMaterial.Create(); using var epoch2 = epoch1.Next(); using var epoch3 = epoch2.Next(); using var epoch4 = epoch3.Next();
        var room = Guid.NewGuid(); var firstId = Guid.NewGuid(); var ownerId = Guid.NewGuid(); var joiningId = Guid.NewGuid(); var zero = new string('0', 64);
        var oldOwner = new HouseholdMemberDefinition("fictitious-owner", HouseholdRole.Owner, zero, [new(firstId, Public(firstDevice))]);
        var genesis = Statement(room, originalRoot, epoch1, 0, zero, HouseholdMembershipChange.Genesis, [oldOwner]);
        var first = Proof(genesis, originalRoot); var trusted = HouseholdTrustChain.Genesis(room, originalRoot.Public, first);
        var previousInvitation = Guid.NewGuid();
        var oldClaim = new HouseholdInvitationClaimStatement(1, room, previousInvitation, Guid.NewGuid(), "fictitious-next-owner", new('b', 64), originalRoot.Public.Fingerprint, Public(ownerDevice));
        var oldSignedClaim = Sign(oldClaim, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimStatement, ownerDevice, "zanance-household-invitation-claim+jwt");
        var proposedOwner = new HouseholdMemberDefinition("fictitious-next-owner", HouseholdRole.Editor, HouseholdTrustChain.Digest(oldSignedClaim), [new(ownerId, Public(ownerDevice))]);
        var accepted = Statement(room, originalRoot, epoch2, 1, trusted.Hash, HouseholdMembershipChange.AcceptMember, [oldOwner, proposedOwner], previousInvitation);
        var second = Proof(accepted, originalRoot, oldSignedClaim); trusted = HouseholdTrustChain.Advance(trusted, second);
        var newOwner = proposedOwner with { Role = HouseholdRole.Owner };
        var transfer = Statement(room, originalRoot, epoch3, 2, trusted.Hash, HouseholdMembershipChange.TransferOwner, [newOwner]) with
        { OwnerId = newOwner.IdentityId, OwnerKeys = Public(nextRoot) };
        var signedTransfer = Sign(transfer, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement, originalRoot, "zanance-household-membership+jwt");
        var consent = new HouseholdOwnerAcceptanceStatement(1, room, transfer.OperationId, HouseholdTrustChain.Digest(signedTransfer), new('e', 64));
        var transferAcceptance = Sign(consent, HouseholdMetadataJsonContext.Default.HouseholdOwnerAcceptanceStatement, nextRoot, "zanance-household-owner-acceptance+jwt");
        var third = new HouseholdMembershipProof(signedTransfer, transferAcceptance, null, consent.GrantsHash); trusted = HouseholdTrustChain.Advance(trusted, third);
        var invitation = new HouseholdInvitationStatement(1, room, Guid.NewGuid(), Guid.NewGuid(), 3, trusted.Hash, HouseholdRole.Viewer,
            new('a', 64), TimeProvider.System.GetUtcNow().AddHours(1).ToUnixTimeSeconds());
        var signedInvitation = Sign(invitation, HouseholdMetadataJsonContext.Default.HouseholdInvitationStatement, nextRoot, "zanance-household-invitation+jwt");
        var claim = new HouseholdInvitationClaimStatement(1, room, invitation.InvitationId, Guid.NewGuid(), "fictitious-recipient",
            HouseholdTrustChain.Digest(signedInvitation), nextRoot.Public.Fingerprint, Public(joiningDevice));
        var signedClaim = Sign(claim, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimStatement, joiningDevice, "zanance-household-invitation-claim+jwt");
        var newMember = new HouseholdMemberDefinition(claim.IdentityId, HouseholdRole.Viewer, HouseholdTrustChain.Digest(signedClaim), [new(joiningId, Public(joiningDevice))]);
        var final = Statement(room, nextRoot, epoch4, 3, trusted.Hash, HouseholdMembershipChange.AcceptMember, [newOwner, newMember], invitation.InvitationId) with
        { OwnerId = newOwner.IdentityId, PreviousOwnerId = newOwner.IdentityId };
        var fourth = Proof(final, nextRoot, signedClaim);
        HouseholdMembershipProof[] history = [first, second, third, fourth];
        var joined = HouseholdTrustChain.Join(nextRoot.Public, signedInvitation, history, claim.IdentityId, joiningId, joiningDevice.Public);
        Assert.Equal(4, joined.Version); Assert.Equal(nextRoot.Public, joined.Root); Assert.Equal(HouseholdTrustChain.Digest(fourth.SignedStatement), joined.Hash);
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Join(originalRoot.Public, signedInvitation, history, claim.IdentityId, joiningId, joiningDevice.Public));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Join(nextRoot.Public, signedInvitation, history[..3], claim.IdentityId, joiningId, joiningDevice.Public));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Join(nextRoot.Public, signedInvitation, history[2..], claim.IdentityId, joiningId, joiningDevice.Public));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Join(nextRoot.Public, signedInvitation, history, "fictitious-other", joiningId, joiningDevice.Public));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Join(nextRoot.Public, signedInvitation, history, claim.IdentityId, Guid.NewGuid(), joiningDevice.Public));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Join(nextRoot.Public, signedInvitation, history, claim.IdentityId, joiningId, ownerDevice.Public));
        var replacedFirst = first with { SignedStatement = Sign(genesis with { OwnerKeys = Public(nextRoot) }, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement,
            nextRoot, "zanance-household-membership+jwt") };
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Join(nextRoot.Public, signedInvitation, [replacedFirst, second, third, fourth], claim.IdentityId, joiningId, joiningDevice.Public));
    }

    /// <summary>Only pinned continuity plus recipient consent enrolls a key; real rotation excludes old material while preserving opaque addresses.</summary>
    [Fact]
    public void Pinned_chain_requires_recipient_consent_and_rotation_keeps_addresses_but_excludes_removed_keys()
    {
        using var root = SigningRecipientIdentity.Create(); using var owner = SigningRecipientIdentity.Create();
        using var member = SigningRecipientIdentity.Create(); using var attacker = SigningRecipientIdentity.Create();
        using var epoch1 = HouseholdEpochMaterial.Create(); using var epoch2 = epoch1.Next(); using var epoch3 = epoch2.Next();
        var room = Guid.NewGuid(); var ownerDevice = Guid.NewGuid(); var memberDevice = Guid.NewGuid();
        var zero = new string('0', 64); var crypto = new HouseholdCryptography(TimeProvider.System);
        var ownerDefinition = new HouseholdMemberDefinition("fictitious-owner", HouseholdRole.Owner, zero, [new(ownerDevice, Public(owner))]);
        var genesis = Statement(room, root, epoch1, 0, zero, HouseholdMembershipChange.Genesis, [ownerDefinition]);
        var genesisProof = Proof(genesis, root);
        var initial = HouseholdTrustChain.Genesis(room, root.Public, genesisProof);
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Genesis(room, attacker.Public, genesisProof));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Genesis(Guid.NewGuid(), root.Public, genesisProof));
        var detached = initial.Snapshot(); detached.Members[0] = detached.Members[0] with { Role = HouseholdRole.Viewer };
        Assert.Equal(HouseholdRole.Owner, initial.Snapshot().Members[0].Role);
        var invitation = Guid.NewGuid();
        var claim = new HouseholdInvitationClaimStatement(1, room, invitation, Guid.NewGuid(), "fictitious-member", new('b', 64), root.Public.Fingerprint, Public(member));
        var signedClaim = Sign(claim, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimStatement, member, "zanance-household-invitation-claim+jwt");
        var accepted = new HouseholdMemberDefinition("fictitious-member", HouseholdRole.Viewer, HouseholdTrustChain.Digest(signedClaim), [new(memberDevice, Public(member))]);
        var joined = Statement(room, root, epoch2, 1, initial.Hash, HouseholdMembershipChange.AcceptMember, [ownerDefinition, accepted], invitation);
        var joinedProof = Proof(joined, root, signedClaim);
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Advance(initial, joinedProof with { SignedAcceptanceClaim = null }));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Advance(initial, Proof(joined with { PreviousHash = new('c', 64) }, root, signedClaim)));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Advance(initial, Proof(joined with { OwnerKeys = Public(attacker) }, attacker, signedClaim)));
        var current = HouseholdTrustChain.Advance(initial, joinedProof);
        Assert.Equal(2, current.Version); Assert.Equal(member.Public, current.Device(memberDevice, requireWrite: false));
        Assert.Throws<CryptographicException>(() => current.Device(memberDevice, requireWrite: true));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Advance(current, joinedProof));
        var memberGrant = epoch2.Grant(crypto, joined, root, accepted.Devices[0]);
        using var memberKeys = current.OpenGrant(crypto, memberGrant, memberDevice, member);
        Assert.Equal(epoch2.KeyCommitment, memberKeys.KeyCommitment);
        Assert.Throws<CryptographicException>(() => current.OpenGrant(crypto, memberGrant, memberDevice, attacker));
        Assert.Throws<CryptographicException>(() => HouseholdEpochMaterial.OpenGrant(crypto, joined with { KeyCommitment = new('d', 64) }, memberGrant, memberDevice, member));
        var revoked = Statement(room, root, epoch3, 2, current.Hash, HouseholdMembershipChange.RevokeMember, [ownerDefinition]);
        var after = HouseholdTrustChain.Advance(current, Proof(revoked, root));
        Assert.Throws<CryptographicException>(() => after.Device(memberDevice, requireWrite: false));
        Assert.Equal(epoch2.RecordAddress("Entries", "fictitious-key"), epoch3.RecordAddress("Entries", "fictitious-key"));
        Assert.NotEqual(epoch2.KeyCommitment, epoch3.KeyCommitment);
        Assert.Equal(epoch2.AddressKeyCommitment, epoch3.AddressKeyCommitment);
        Assert.NotEqual(epoch3.RecordAddress("ab", "c"), epoch3.RecordAddress("a", "bc"));
        var operation = Guid.NewGuid(); var context = new HouseholdProtectionContext(room, 3, operation, HouseholdProtectionPurpose.Financial, "members");
        var ciphertext = epoch3.Encrypt(crypto, context, "Fictitious new financial data"u8, owner);
        // Even an adversary changing the old frame's generation label cannot make its old key open the new GCM ciphertext.
        var oldFrame = memberKeys.ExportForProtectedStorage();
        try
        {
            BinaryPrimitives.WriteInt64BigEndian(oldFrame.AsSpan(4), 3);
            using var relabeled = HouseholdEpochMaterial.ImportFromProtectedStorage(oldFrame);
            Assert.Throws<CryptographicException>(() => relabeled.Decrypt(crypto, context, ciphertext, owner.Public));
        }
        finally { CryptographicOperations.ZeroMemory(oldFrame); }
        using var available = after.OpenGrant(crypto, epoch3.Grant(crypto, revoked, root, ownerDefinition.Devices[0]), ownerDevice, owner);
        var clear = available.Decrypt(crypto, context, ciphertext, owner.Public);
        try { Assert.True(clear.AsSpan().SequenceEqual("Fictitious new financial data"u8)); }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    /// <summary>All ordered opaque parts and original predecessor/attempt are verified before any decrypted financial row is accepted.</summary>
    [Fact]
    public void Publication_rejects_missing_reordered_or_altered_parts_and_replayed_attempt_or_predecessor()
    {
        using var root = SigningRecipientIdentity.Create(); using var author = SigningRecipientIdentity.Create();
        using var epoch = HouseholdEpochMaterial.Create(); var room = Guid.NewGuid(); var device = Guid.NewGuid(); var zero = new string('0', 64);
        var membership = Statement(room, root, epoch, 0, zero, HouseholdMembershipChange.Genesis,
            [new("fictitious-owner", HouseholdRole.Owner, zero, [new(device, Public(author))])]);
        var trusted = HouseholdTrustChain.Genesis(room, root.Public, Proof(membership, root));
        var parts = new[] { new HouseholdPartCommitment(0, new('a', 64), 0, new('b', 64)), new HouseholdPartCommitment(1, new('c', 64), 1, new('d', 64)) };
        var operation = Guid.NewGuid(); var attempt = Guid.NewGuid();
        var value = new HouseholdCommitStatement(1, room, device, operation, attempt, 1, 2, 1, 1, zero, 2, HouseholdPublicationDigest.Manifest(parts));
        var receipt = new HouseholdPublicationReceipt(Guid.NewGuid(), Sign(value, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement, author,
            "zanance-household-commit+jwt"), parts);
        Assert.Equal(value, HouseholdTrustChain.Publication(trusted, receipt, device, operation, attempt, 1, zero));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Publication(trusted, receipt with { Parts = parts[..1] }, device, operation, attempt, 1, zero));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Publication(trusted, receipt with { Parts = [parts[1], parts[0]] }, device, operation, attempt, 1, zero));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Publication(trusted, receipt with { Parts = [parts[0], parts[1] with { BaseVersion = 0 }] }, device, operation, attempt, 1, zero));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Publication(trusted, receipt, device, operation, Guid.NewGuid(), 1, zero));
        Assert.Throws<CryptographicException>(() => HouseholdTrustChain.Publication(trusted, receipt, device, operation, attempt, 0, zero));
    }

    /// <summary>Strict standard signature parsing rejects duplicate authority claims even when signed by an otherwise trusted key.</summary>
    [Fact]
    public void Signed_duplicate_metadata_is_not_reinterpreted_by_another_JSON_parser()
    {
        using var root = SigningRecipientIdentity.Create();
        var payload = Encoding.UTF8.GetBytes("{\"format\":1,\"format\":2}");
        var token = StandardJoseSignatures.Sign(payload, root, "zanance-household-membership+jwt");
        Assert.Throws<CryptographicException>(() => StandardJoseSignatures.VerifyJson(token, root.Public, "zanance-household-membership+jwt",
            HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement));
    }

    /// <summary>Exact fixture roster metadata; original domain and cryptographic verifiers still enforce all transitions.</summary>
    private static HouseholdMembershipStatement Statement(Guid room, SigningRecipientIdentity root, HouseholdEpochMaterial epoch,
        long previous, string head, HouseholdMembershipChange change, HouseholdMemberDefinition[] members, Guid? invitation = null) =>
        new(1, room, Guid.NewGuid(), change, invitation, previous, previous + 1, head, "fictitious-owner", "fictitious-owner", Public(root), epoch.Epoch,
            epoch.KeyCommitment, epoch.AddressKeyCommitment, TimeProvider.System.GetUtcNow().AddMinutes(5).ToUnixTimeSeconds(), members);
    /// <summary>Standard signed fixture authority, not an alternative crypto implementation.</summary>
    private static HouseholdMembershipProof Proof(HouseholdMembershipStatement statement, SigningRecipientIdentity root, string? claim = null) =>
        new(Sign(statement, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement, root, "zanance-household-membership+jwt"), null, claim, new('e', 64));
    /// <summary>Signs exact original source-generated metadata with maintained standard ES256.</summary>
    private static string Sign<T>(T value, JsonTypeInfo<T> json, SigningRecipientIdentity key, string type) =>
        StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(value, json), key, type);
    /// <summary>Public-only fixture identity.</summary>
    private static HouseholdPublicKeys Public(SigningRecipientIdentity identity) => new(identity.Public.SigningKey, identity.Public.WrappingKey);
}
