using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Separate standard recipient-encrypted private frames; importing bytes never grants membership or service authority.</summary>
public static class HouseholdRecoveryMaterial
{
    /// <summary>Wraps only the proposed exact root for an independently confirmed recovery kit, before its archive is published.</summary>
    public static string SealRoot(HouseholdCryptography crypto, Guid space, Guid enrollment, long epoch,
        SigningRecipientIdentity root, PublicSigningRecipientIdentity kit)
    {
        if (root.Public.SigningKey == kit.SigningKey || root.Public.WrappingKey == kit.WrappingKey) throw Refusal();
        return SealPrivateRoot(crypto, new(space, epoch, enrollment, HouseholdProtectionPurpose.Recovery, kit.Fingerprint), root, kit);
    }

    /// <summary>Opens only a root whose public identity was independently verified through the kit anchor and signed membership chain.</summary>
    public static SigningRecipientIdentity OpenRoot(HouseholdCryptography crypto, Guid space, Guid enrollment, long epoch,
        string ciphertext, PublicSigningRecipientIdentity verifiedRoot, HouseholdRecoveryKit kit)
    {
        if (kit.SpaceId != space || kit.EnrollmentId != enrollment) throw Refusal();
        return OpenPrivateRoot(crypto, new(space, epoch, enrollment, HouseholdProtectionPurpose.Recovery, kit.Public.Fingerprint),
            ciphertext, verifiedRoot, kit.Secret);
    }

    /// <summary>Delegates current owner authority only to an exact device of the current owning identity, never to an editor or viewer.</summary>
    public static HouseholdOwnerRootGrant GrantOwnerRoot(HouseholdCryptography crypto, HouseholdVerifiedMembership membership,
        SigningRecipientIdentity root, Guid deviceId)
    {
        var state = membership.Snapshot();
        var device = state.Members.Single(member => member.Role == HouseholdRole.Owner).Devices.SingleOrDefault(device => device.Id == deviceId);
        if (device is null || membership.Root != root.Public) throw Refusal();
        var recipient = new PublicSigningRecipientIdentity(device.Keys.SigningKey, device.Keys.WrappingKey);
        var ciphertext = SealPrivateRoot(crypto, new(state.SpaceId, state.KeyEpoch, state.OperationId,
            HouseholdProtectionPurpose.OwnerRoot, recipient.Fingerprint), root, recipient);
        return new(deviceId, state.Version, root.Public.Fingerprint, ciphertext, HouseholdPublicationDigest.Ciphertext(ciphertext));
    }

    /// <summary>Checks original granting generation, current same-owner membership and exact digest before opening a delegated root.</summary>
    public static SigningRecipientIdentity OpenOwnerRoot(HouseholdCryptography crypto, HouseholdVerifiedMembership granting,
        HouseholdVerifiedMembership current, HouseholdOwnerRootGrant grant, Guid ownDeviceId, SigningRecipientIdentity ownDevice)
    {
        var state = granting.Snapshot(); var now = current.Snapshot();
        if (grant.MembershipVersion != state.Version || grant.DeviceId != ownDeviceId || state.SpaceId != now.SpaceId ||
            state.Version > now.Version || granting.Root != current.Root || state.OwnerId != now.OwnerId ||
            grant.RootFingerprint != current.Root.Fingerprint || grant.Ciphertext is not { Length: > 0 and <= HouseholdRecoveryBounds.RootCiphertextCharacters } ||
            grant.Digest != HouseholdPublicationDigest.Ciphertext(grant.Ciphertext)) throw Refusal();
        foreach (var roster in new[] { state, now })
        {
            var owner = roster.Members.Single(member => member.Role == HouseholdRole.Owner);
            if (!owner.Devices.Any(device => device.Id == ownDeviceId && device.Keys ==
                new HouseholdPublicKeys(ownDevice.Public.SigningKey, ownDevice.Public.WrappingKey))) throw Refusal();
        }
        return OpenPrivateRoot(crypto, new(state.SpaceId, state.KeyEpoch, state.OperationId, HouseholdProtectionPurpose.OwnerRoot,
            ownDevice.Public.Fingerprint), grant.Ciphertext, granting.Root, ownDevice);
    }

    /// <summary>Archives an original epoch only after its independent signed commitments have been checked; a recovery frame cannot be used as a device grant.</summary>
    public static HouseholdRecoveryEpochGrant SealEpoch(HouseholdCryptography crypto, Guid enrollment,
        HouseholdMembershipStatement granting, HouseholdMembershipStatement origin, HouseholdEpochMaterial material,
        SigningRecipientIdentity root, PublicSigningRecipientIdentity kit)
    {
        RequireEpoch(granting, origin, material);
        if (granting.OwnerKeys != new HouseholdPublicKeys(root.Public.SigningKey, root.Public.WrappingKey) ||
            root.Public.SigningKey == kit.SigningKey || root.Public.WrappingKey == kit.WrappingKey) throw Refusal();
        var clear = material.ExportForProtectedStorage();
        try
        {
            var ciphertext = crypto.EncryptForDevice(new(granting.SpaceId, material.Epoch, enrollment,
                HouseholdProtectionPurpose.Recovery, kit.Fingerprint), clear, root, kit);
            return new(material.Epoch, granting.Version, ciphertext, HouseholdPublicationDigest.Ciphertext(ciphertext));
        }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    /// <summary>Opens one bounded archived key against trusted original commitments, independently of the service's advertised epoch numbers.</summary>
    public static HouseholdEpochMaterial OpenEpoch(HouseholdCryptography crypto, HouseholdMembershipStatement granting,
        HouseholdMembershipStatement origin, HouseholdRecoveryEpochGrant grant, HouseholdRecoveryKit kit)
    {
        if (kit.SpaceId != granting.SpaceId || grant.Epoch != origin.KeyEpoch || grant.MembershipVersion != granting.Version ||
            grant.Ciphertext is not { Length: > 0 and <= HouseholdRecoveryBounds.EpochCiphertextCharacters } ||
            grant.Digest != HouseholdPublicationDigest.Ciphertext(grant.Ciphertext)) throw Refusal();
        var root = new PublicSigningRecipientIdentity(granting.OwnerKeys.SigningKey, granting.OwnerKeys.WrappingKey);
        var clear = crypto.DecryptForDevice(new(granting.SpaceId, grant.Epoch, kit.EnrollmentId, HouseholdProtectionPurpose.Recovery,
            kit.Public.Fingerprint), grant.Ciphertext, root, kit.Secret);
        HouseholdEpochMaterial? material = null;
        try { material = HouseholdEpochMaterial.ImportFromProtectedStorage(clear); RequireEpoch(granting, origin, material); return material; }
        catch { material?.Dispose(); throw; }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    private static void RequireEpoch(HouseholdMembershipStatement granting, HouseholdMembershipStatement origin, HouseholdEpochMaterial material)
    {
        if (granting.SpaceId != origin.SpaceId || granting.Version < origin.Version || granting.KeyEpoch < origin.KeyEpoch ||
            material.Epoch != origin.KeyEpoch || material.KeyCommitment != origin.KeyCommitment ||
            material.AddressKeyCommitment != origin.AddressKeyCommitment || granting.AddressKeyCommitment != origin.AddressKeyCommitment) throw Refusal();
    }

    private static string SealPrivateRoot(HouseholdCryptography crypto, HouseholdProtectionContext context,
        SigningRecipientIdentity root, PublicSigningRecipientIdentity recipient)
    {
        var secret = root.ExportForProtectedStorage(); byte[]? clear = null;
        try
        {
            clear = JsonSerializer.SerializeToUtf8Bytes(new HouseholdPrivateRootFrame(1, "zanance.household.owner-root.v1",
                context.SpaceId, context.OperationId, context.Epoch, new(root.Public.SigningKey, root.Public.WrappingKey), secret),
                HouseholdRecoveryPrivateJsonContext.Default.HouseholdPrivateRootFrame);
            return crypto.EncryptForDevice(context, clear, root, recipient);
        }
        finally { CryptographicOperations.ZeroMemory(secret); if (clear is not null) CryptographicOperations.ZeroMemory(clear); }
    }

    private static SigningRecipientIdentity OpenPrivateRoot(HouseholdCryptography crypto, HouseholdProtectionContext context,
        string ciphertext, PublicSigningRecipientIdentity verifiedRoot, SigningRecipientIdentity recipient)
    {
        if (ciphertext is not { Length: > 0 and <= HouseholdRecoveryBounds.RootCiphertextCharacters }) throw Refusal();
        var clear = crypto.DecryptForDevice(context, ciphertext, verifiedRoot, recipient); HouseholdPrivateRootFrame? frame = null;
        SigningRecipientIdentity? root = null;
        try
        {
            frame = StrictJsonPayload.Deserialize(clear, HouseholdRecoveryPrivateJsonContext.Default.HouseholdPrivateRootFrame, 16384);
            if (frame.Format != 1 || frame.Kind != "zanance.household.owner-root.v1" || frame.SpaceId != context.SpaceId ||
                frame.OperationId != context.OperationId || frame.Epoch != context.Epoch || frame.Root !=
                new HouseholdPublicKeys(verifiedRoot.SigningKey, verifiedRoot.WrappingKey) || frame.PrivateRoot is not { Length: > 0 and <= 8192 }) throw Refusal();
            root = SigningRecipientIdentity.ImportFromProtectedStorage(frame.PrivateRoot);
            if (root.Public != verifiedRoot) throw Refusal();
            return root;
        }
        catch { root?.Dispose(); throw; }
        finally { CryptographicOperations.ZeroMemory(clear); if (frame?.PrivateRoot is { } bytes) CryptographicOperations.ZeroMemory(bytes); }
    }

    private static CryptographicException Refusal() => new("Household recovery material was refused.");
}

/// <summary>Private client-only root frame; service DTOs contain only its standard recipient-encrypted envelope.</summary>
internal sealed record HouseholdPrivateRootFrame([property: JsonRequired] int Format, [property: JsonRequired] string Kind,
    [property: JsonRequired] Guid SpaceId, [property: JsonRequired] Guid OperationId, [property: JsonRequired] long Epoch,
    [property: JsonRequired] HouseholdPublicKeys Root, [property: JsonRequired] byte[] PrivateRoot)
{
    /// <inheritdoc />
    public override string ToString() => "Private household owner frame";
}
