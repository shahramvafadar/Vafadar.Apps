using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Backup;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Actual separate kit/root/epoch boundaries; no service directory or file possession alone establishes membership.</summary>
[Trait("AT", "AT-294")]
public sealed class HouseholdRecoveryTests
{
    /// <summary>Only an explicit protected file retains the same independent kit; wrong password/tamper fail before producing a key handle.</summary>
    [Fact]
    public void Encrypted_kit_preserves_original_identity_and_anchor_but_refuses_plain_or_altered_files()
    {
        using var root = SigningRecipientIdentity.Create();
        using var kit = HouseholdRecoveryKit.Create(Guid.NewGuid(), Guid.NewGuid(), "fictitious-owner", Peer(), Public(root));
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var file = kit.ExportEncrypted(password);
        try
        {
            using var restored = HouseholdRecoveryKit.ImportEncrypted(file, password);
            Assert.Equal(kit.Public, restored.Public); Assert.Equal(kit.SpaceId, restored.SpaceId);
            Assert.Equal(kit.InitialOwnerAnchor, restored.InitialOwnerAnchor); Assert.Equal(kit.Peer.Address, restored.Peer.Address);
            Assert.Throws<BackupException>(() => HouseholdRecoveryKit.ImportEncrypted(file, "fictitious-wrong-password"));
            file[^1] ^= 1;
            Assert.Throws<BackupException>(() => HouseholdRecoveryKit.ImportEncrypted(file, password));
            Assert.Throws<CryptographicException>(() => HouseholdRecoveryKit.ImportEncrypted("readable kit refused"u8, password));
            Assert.Throws<ArgumentException>(() => kit.ExportEncrypted("short"));
        }
        finally { CryptographicOperations.ZeroMemory(file); }
    }

    /// <summary>Root and epoch frames cannot substitute for each other or another kit/enrollment/commitment.</summary>
    [Fact]
    public void Recovery_root_and_epoch_are_separate_exact_recipient_frames_with_original_signed_commitments()
    {
        using var root = SigningRecipientIdentity.Create(); using var device = SigningRecipientIdentity.Create();
        using var epoch = HouseholdEpochMaterial.Create(); using var nextEpoch = epoch.Next();
        var space = Guid.NewGuid(); var enrollment = Guid.NewGuid();
        using var kit = HouseholdRecoveryKit.Create(space, enrollment, "fictitious-owner", Peer(), Public(root));
        using var stranger = HouseholdRecoveryKit.Create(space, Guid.NewGuid(), "fictitious-owner", Peer(), Public(root));
        var crypto = new HouseholdCryptography(TimeProvider.System);
        var origin = Genesis(space, root, device, epoch); var granting = origin with { Version = 2, KeyEpoch = 2, KeyCommitment = nextEpoch.KeyCommitment };
        var rootCipher = HouseholdRecoveryMaterial.SealRoot(crypto, space, enrollment, 2, root, kit.Public);
        using var opened = HouseholdRecoveryMaterial.OpenRoot(crypto, space, enrollment, 2, rootCipher, root.Public, kit);
        Assert.Equal(root.Public, opened.Public);
        Assert.Throws<CryptographicException>(() => HouseholdRecoveryMaterial.OpenRoot(crypto, space, enrollment, 2, rootCipher, root.Public, stranger));
        Assert.Throws<CryptographicException>(() => HouseholdRecoveryMaterial.OpenRoot(crypto, space, enrollment, 1, rootCipher, root.Public, kit));
        var archive = HouseholdRecoveryMaterial.SealEpoch(crypto, enrollment, granting, origin, epoch, root, kit.Public);
        using var restored = HouseholdRecoveryMaterial.OpenEpoch(crypto, granting, origin, archive, kit);
        Assert.Equal(epoch.KeyCommitment, restored.KeyCommitment); Assert.Equal(epoch.RecordAddress("Entries", "synthetic"), restored.RecordAddress("Entries", "synthetic"));
        Assert.Throws<CryptographicException>(() => HouseholdRecoveryMaterial.OpenEpoch(crypto, granting,
            origin with { KeyCommitment = nextEpoch.KeyCommitment }, archive, kit));
        Assert.Throws<CryptographicException>(() => HouseholdRecoveryMaterial.OpenEpoch(crypto, granting, origin,
            archive with { Ciphertext = rootCipher, Digest = HouseholdPublicationDigest.Ciphertext(rootCipher) }, kit));
    }

    /// <summary>A delegated root is for the same current owner device; changed purpose, root generation and current roster fail closed.</summary>
    [Fact]
    public void Owner_root_delegation_requires_current_owning_roster_and_original_granting_generation()
    {
        using var root = SigningRecipientIdentity.Create(); using var device = SigningRecipientIdentity.Create();
        using var epoch = HouseholdEpochMaterial.Create(); using var other = SigningRecipientIdentity.Create();
        var space = Guid.NewGuid(); var state = Genesis(space, root, device, epoch);
        var signed = StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(state,
            HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement), root, "zanance-household-membership+jwt");
        var membership = HouseholdTrustChain.Genesis(space, root.Public, new(signed, null, null, new('e', 64)));
        var id = state.Members[0].Devices[0].Id; var crypto = new HouseholdCryptography(TimeProvider.System);
        var grant = HouseholdRecoveryMaterial.GrantOwnerRoot(crypto, membership, root, id);
        using var reopened = HouseholdRecoveryMaterial.OpenOwnerRoot(crypto, membership, membership, grant, id, device);
        Assert.Equal(root.Public, reopened.Public);
        Assert.Throws<CryptographicException>(() => HouseholdRecoveryMaterial.OpenOwnerRoot(crypto, membership, membership, grant, id, other));
        Assert.Throws<CryptographicException>(() => HouseholdRecoveryMaterial.OpenOwnerRoot(crypto, membership, membership,
            grant with { MembershipVersion = 2 }, id, device));
        Assert.Throws<CryptographicException>(() => crypto.DecryptForDevice(new(space, epoch.Epoch, state.OperationId,
            HouseholdProtectionPurpose.DeviceGrant, device.Public.Fingerprint), grant.Ciphertext, root.Public, device));
        using var kit = HouseholdRecoveryKit.Create(space, state.OperationId, "fictitious-owner", Peer(), Public(root));
        Assert.Throws<CryptographicException>(() => HouseholdRecoveryMaterial.OpenRoot(crypto, space, state.OperationId, 1, grant.Ciphertext,
            root.Public, kit));
    }

    private static LocalSyncPeer Peer() => new("https://localhost:55475", new('a', 64));
    private static HouseholdPublicKeys Public(SigningRecipientIdentity identity) => new(identity.Public.SigningKey, identity.Public.WrappingKey);
    private static HouseholdMembershipStatement Genesis(Guid space, SigningRecipientIdentity root, SigningRecipientIdentity device, HouseholdEpochMaterial epoch) =>
        new(1, space, Guid.NewGuid(), HouseholdMembershipChange.Genesis, null, 0, 1, new('0', 64), "fictitious-owner", "fictitious-owner",
            Public(root), 1, epoch.KeyCommitment, epoch.AddressKeyCommitment, TimeProvider.System.GetUtcNow().AddMinutes(5).ToUnixTimeSeconds(),
            [new("fictitious-owner", HouseholdRole.Owner, new('0', 64), [new(Guid.NewGuid(), Public(device))])]);
}
