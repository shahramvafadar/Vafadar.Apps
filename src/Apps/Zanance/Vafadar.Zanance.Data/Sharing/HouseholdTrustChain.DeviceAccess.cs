using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Data.Sharing;

public static partial class HouseholdTrustChain
{
    /// <summary>Verifies the original owner-signed checkpoint and later explicit approval of this exact existing member's independent key.</summary>
    public static HouseholdVerifiedHistory DeviceHistory(HouseholdDeviceAccessPackage package, IReadOnlyList<HouseholdMembershipProof> history,
        string actualIdentity, Guid ownDeviceId, PublicSigningRecipientIdentity ownDevice, bool requireCurrentMembership = true)
    {
        var anchor = DeviceAuthorization(package, actualIdentity);
        if (ownDeviceId == Guid.Empty || history.Count is < 1 or > 4096 || history.Any(item => item is null || item.SignedStatement is null ||
            item.SignedStatement.Length > HouseholdLimits.MembershipBytes * 2) || history.Sum(item => (long)item.SignedStatement.Length) > 64L * 1024 * 1024)
            throw Refusal();
        var pinned = new PublicSigningRecipientIdentity(package.OwnerKeys.SigningKey, package.OwnerKeys.WrappingKey);
        var genesis = ReadUntrustedGenesis(history[0].SignedStatement);
        var state = Genesis(anchor.SpaceId, new(genesis.OwnerKeys.SigningKey, genesis.OwnerKeys.WrappingKey), history[0]);
        var states = new List<HouseholdVerifiedMembership>(); var anchored = false; long admission = 0;
        for (var index = 0; index < history.Count; index++)
        {
            if (index != 0) state = Advance(state, history[index]);
            states.Add(state); var roster = state.Snapshot();
            if (state.Version == anchor.MembershipVersion)
            {
                if (state.Hash != anchor.MembershipHead || state.Root != pinned || !roster.Members.Any(member => member.IdentityId == actualIdentity) ||
                    roster.Members.SelectMany(member => member.Devices).Any(device => device.Id == ownDeviceId || device.Keys == new HouseholdPublicKeys(ownDevice.SigningKey, ownDevice.WrappingKey)))
                    throw Refusal();
                anchored = true;
            }
            if (!anchored || admission != 0 || state.Version <= anchor.MembershipVersion) continue;
            var own = roster.Members.SingleOrDefault(member => member.IdentityId == actualIdentity)?.Devices.SingleOrDefault(device => device.Id == ownDeviceId);
            if (own is null) continue;
            if (roster.Change != HouseholdMembershipChange.ApproveDevice || own.Keys != new HouseholdPublicKeys(ownDevice.SigningKey, ownDevice.WrappingKey)) throw Refusal();
            admission = state.Version;
        }
        if (!anchored || admission == 0 || requireCurrentMembership && !state.Snapshot().Members.Any(member => member.IdentityId == actualIdentity &&
            member.Devices.Any(device => device.Id == ownDeviceId && device.Keys == new HouseholdPublicKeys(ownDevice.SigningKey, ownDevice.WrappingKey)))) throw Refusal();
        return new(states, admission);
    }

    /// <summary>Checks the typed original root proof; expiry is enforced when starting setup, not when reopening historical already accepted consent.</summary>
    internal static HouseholdDeviceAccessStatement DeviceAuthorization(HouseholdDeviceAccessPackage package, string actualIdentity)
    {
        if (package is null || package.Format != 1 || package.Kind != HouseholdSetupAnchors.DeviceAccess || package.OwnerKeys is null ||
            package.SignedAuthorization is null || package.SignedAuthorization.Length > 8192) throw Refusal();
        var root = new PublicSigningRecipientIdentity(package.OwnerKeys.SigningKey, package.OwnerKeys.WrappingKey);
        var anchor = Read(package.SignedAuthorization, root, HouseholdSetupAnchors.DeviceAccessProof, HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessStatement);
        if (anchor.Format != 1 || anchor.SpaceId == Guid.Empty || anchor.OperationId == Guid.Empty || anchor.MembershipVersion < 1 ||
            !HouseholdProtectionContext.IsFingerprint(anchor.MembershipHead) || anchor.IdentityId != actualIdentity || anchor.ExpiresUnixSeconds < 1) throw Refusal();
        return anchor;
    }
}
