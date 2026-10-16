using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Shared domain limits without a purchase grant, infrastructure key handle or alternative commercial quota.</summary>
public static class HouseholdLimits
{
    /// <summary>Finite independent device enrollment bound per identity; revocation frees a device slot, never a member seat.</summary>
    public const int DevicesPerIdentity = 8;
    /// <summary>Maximum standard-signed public membership metadata; financial content has its own separate bounds.</summary>
    public const int MembershipBytes = 64 * 1024;
    /// <summary>One explicit invitation reserves its seat for at most48 elapsed operational hours.</summary>
    public static readonly TimeSpan InvitationLifetime = TimeSpan.FromHours(48);
    /// <summary>Bounded validity for a newly committed membership/transfer proposal, not its historical signed chain.</summary>
    public static readonly TimeSpan MembershipProposalLifetime = TimeSpan.FromMinutes(15);

    /// <summary>Client-only headroom below the service's unchanged maximum; small positive clock skew must not create an excessive future proposal.</summary>
    public static readonly TimeSpan ClientProposalHeadroom = TimeSpan.FromMinutes(1);

    /// <summary>Creates a shorter client deadline without weakening the service clock, expiry checks or maximum authority lifetime.</summary>
    public static long ClientDeadline(DateTimeOffset clientNow, TimeSpan maximumLifetime)
    {
        if (maximumLifetime <= ClientProposalHeadroom) throw new ArgumentOutOfRangeException(nameof(maximumLifetime));
        // Android's independently observed clock was two seconds ahead of the service.
        // Using the entire server maximum rejected fresh requests as future-dated (D-294).
        return clientNow.Add(maximumLifetime - ClientProposalHeadroom).ToUnixTimeSeconds();
    }
}

/// <summary>Content-free structural/continuity rules shared by actual client and service; valid signatures alone grant no role.</summary>
public static class HouseholdMembershipRules
{
    /// <summary>Checks complete bounded public metadata before importing keys or interpreting a role transition.</summary>
    public static IReadOnlyList<string> ValidateShape(HouseholdMembershipStatement? statement)
    {
        if (statement is null || statement.Format != 1 || statement.SpaceId == Guid.Empty || statement.OperationId == Guid.Empty ||
            !Enum.IsDefined(statement.Change) || statement.ExpectedVersion < 0 || statement.ExpectedVersion == long.MaxValue ||
            statement.Version != statement.ExpectedVersion + 1 || statement.KeyEpoch < 1 || statement.ValidUntilUnixSeconds < 0 ||
            statement.PublicationCursor < 0 || !HouseholdProtectionContext.IsFingerprint(statement.PublicationHead) ||
            !HouseholdProtectionContext.IsFingerprint(statement.KeyCommitment) || !HouseholdProtectionContext.IsFingerprint(statement.AddressKeyCommitment) ||
            !Identity(statement.PreviousOwnerId) || !Identity(statement.OwnerId) || !Keys(statement.OwnerKeys) ||
            !HouseholdProtectionContext.IsFingerprint(statement.PreviousHash) || statement.Members is null ||
            statement.Members.Length is < 1 or > QuotaPolicy.SharedSpaceSeatLimit)
            return ["InvalidHouseholdMembership"];
        if (statement.Members.Any(member => member is null || !Identity(member.IdentityId) || !Enum.IsDefined(member.Role) ||
            !HouseholdProtectionContext.IsFingerprint(member.AcceptanceHash) ||
            member.Devices is null || member.Devices.Length > HouseholdLimits.DevicesPerIdentity ||
            member.Devices.Any(device => device is null || device.Id == Guid.Empty || !Keys(device.Keys))))
            return ["InvalidHouseholdMember"];
        if (statement.Members.Select(member => member.IdentityId).Distinct(StringComparer.Ordinal).Count() != statement.Members.Length ||
            statement.Members.Count(member => member.Role == HouseholdRole.Owner) != 1 ||
            statement.Members.Single(member => member.Role == HouseholdRole.Owner) is not { } owner ||
            owner.IdentityId != statement.OwnerId || owner.Devices.Length == 0)
            return ["InvalidHouseholdOwner"];
        var devices = statement.Members.SelectMany(member => member.Devices).ToArray();
        if (devices.Select(device => device.Id).Distinct().Count() != devices.Length ||
            devices.Any(device => device.Keys.SigningKey == statement.OwnerKeys.SigningKey || device.Keys.WrappingKey == statement.OwnerKeys.WrappingKey) ||
            devices.Select(device => device.Keys.SigningKey).Distinct(StringComparer.Ordinal).Count() != devices.Length ||
            devices.Select(device => device.Keys.WrappingKey).Distinct(StringComparer.Ordinal).Count() != devices.Length)
            return ["DuplicateHouseholdDevice"];
        if ((statement.Change == HouseholdMembershipChange.AcceptMember) != (statement.InvitationId is { } invitation && invitation != Guid.Empty))
            return ["UnexpectedHouseholdInvitation"];
        if (statement.Change == HouseholdMembershipChange.Genesis &&
            (statement.ExpectedVersion != 0 || statement.KeyEpoch != 1 || statement.PreviousHash != new string('0', 64) ||
             statement.PublicationCursor != 0 || statement.PublicationHead != new string('0', 64) ||
             statement.PreviousOwnerId != statement.OwnerId || statement.Members.Length != 1 || owner.Devices.Length != 1))
            return ["InvalidHouseholdGenesis"];
        return [];
    }

    /// <summary>Requires the exact prior pinned head and generation; current-root verification and permitted roster effects remain mandatory.</summary>
    public static IReadOnlyList<string> ValidateContinuity(HouseholdMembershipStatement next,
        HouseholdMembershipStatement? previous, string previousHash)
    {
        var shape = ValidateShape(next); if (shape.Count != 0) return shape;
        if (previous is null)
            return next.Change == HouseholdMembershipChange.Genesis && previousHash == new string('0', 64) ? [] : ["HouseholdGenesisRequired"];
        if (ValidateShape(previous).Count != 0 || next.Change == HouseholdMembershipChange.Genesis ||
            next.SpaceId != previous.SpaceId || next.ExpectedVersion != previous.Version || next.PreviousHash != previousHash ||
            next.PreviousOwnerId != previous.OwnerId || previous.KeyEpoch == long.MaxValue || next.KeyEpoch != previous.KeyEpoch + 1)
            return ["HouseholdMembershipCheckpointMismatch"];
        if (next.KeyCommitment == previous.KeyCommitment || next.AddressKeyCommitment != previous.AddressKeyCommitment)
            return ["HouseholdKeyRotationOrAddressContinuityRequired"];
        if (next.PublicationCursor < previous.PublicationCursor ||
            next.PublicationCursor == previous.PublicationCursor && next.PublicationHead != previous.PublicationHead)
            return ["HouseholdPublicationBoundaryRollback"];
        var ownerDeviceRevocation = next.Change == HouseholdMembershipChange.RevokeDevice &&
            previous.Members.Single(member => member.Role == HouseholdRole.Owner).Devices.Any(device =>
                !next.Members.Single(member => member.Role == HouseholdRole.Owner).Devices.Any(retained => retained.Id == device.Id));
        if (next.Change is not (HouseholdMembershipChange.TransferOwner or HouseholdMembershipChange.RecoverOwner or HouseholdMembershipChange.RotateOwnerRoot) && !ownerDeviceRevocation &&
            (next.OwnerId != previous.OwnerId || next.OwnerKeys != previous.OwnerKeys))
            return ["HouseholdOwnerTransitionProofRequired"];
        // Changing only the joint fingerprint does not retire an old kit's signing ability.
        // A root transition must replace both independent private-key roles, not only its RSA public half.
        if ((next.Change is HouseholdMembershipChange.TransferOwner or HouseholdMembershipChange.RecoverOwner or HouseholdMembershipChange.RotateOwnerRoot || ownerDeviceRevocation) &&
            (next.OwnerKeys.SigningKey == previous.OwnerKeys.SigningKey || next.OwnerKeys.WrappingKey == previous.OwnerKeys.WrappingKey))
            return ["HouseholdOwnerKeysMustBothRotate"];
        if (ownerDeviceRevocation && next.OwnerId != previous.OwnerId) return ["InvalidHouseholdOwnerRecovery"];
        if (next.Change == HouseholdMembershipChange.TransferOwner &&
            (next.OwnerId == previous.OwnerId || next.OwnerKeys == previous.OwnerKeys ||
             !previous.Members.Any(member => member.IdentityId == next.OwnerId && member.Role is HouseholdRole.Editor or HouseholdRole.Viewer)))
            return ["InvalidHouseholdOwnerTransfer"];
        if (next.Change is HouseholdMembershipChange.RecoverOwner or HouseholdMembershipChange.RotateOwnerRoot &&
            (next.OwnerId != previous.OwnerId || next.OwnerKeys == previous.OwnerKeys))
            return ["InvalidHouseholdOwnerRecovery"];
        return [];
    }

    /// <summary>Limits each named operation to its exact roster effect; signatures never authorize unrelated hidden role/device changes.</summary>
    public static IReadOnlyList<string> ValidateRosterTransition(HouseholdMembershipStatement next, HouseholdMembershipStatement? previous)
    {
        if (ValidateShape(next).Count != 0 || previous is not null && ValidateShape(previous).Count != 0)
            return ["InvalidHouseholdMembership"];
        if (previous is null)
            return next.Change == HouseholdMembershipChange.Genesis && next.Members[0].AcceptanceHash == new string('0', 64)
                ? [] : ["InvalidHouseholdGenesis"];
        var before = previous.Members.ToDictionary(member => member.IdentityId, StringComparer.Ordinal);
        var after = next.Members.ToDictionary(member => member.IdentityId, StringComparer.Ordinal);
        var added = after.Keys.Except(before.Keys, StringComparer.Ordinal).ToArray();
        var removed = before.Keys.Except(after.Keys, StringComparer.Ordinal).ToArray();
        var changed = before.Keys.Intersect(after.Keys, StringComparer.Ordinal).Where(id => !SameMember(before[id], after[id])).ToArray();
        var valid = next.Change switch
        {
            HouseholdMembershipChange.AcceptMember => added.Length == 1 && removed.Length == 0 && changed.Length == 0 &&
                after[added[0]].Role is HouseholdRole.Editor or HouseholdRole.Viewer && after[added[0]].Devices.Length == 1 &&
                after[added[0]].AcceptanceHash != new string('0', 64),
            HouseholdMembershipChange.RevokeMember or HouseholdMembershipChange.ConfirmDeparture => added.Length == 0 &&
                removed.Length == 1 && removed[0] != previous.OwnerId && changed.Length == 0,
            HouseholdMembershipChange.ApproveDevice or HouseholdMembershipChange.RevokeDevice => added.Length == 0 && removed.Length == 0 &&
                changed.Length == 1 && SameMemberExceptDevices(before[changed[0]], after[changed[0]]) &&
                OneDeviceChange(before[changed[0]], after[changed[0]], next.Change == HouseholdMembershipChange.ApproveDevice),
            HouseholdMembershipChange.ChangeRole => added.Length == 0 && removed.Length == 0 && changed.Length == 1 &&
                changed[0] != previous.OwnerId && before[changed[0]].AcceptanceHash == after[changed[0]].AcceptanceHash &&
                SameDevices(before[changed[0]].Devices, after[changed[0]].Devices) &&
                after[changed[0]].Role is HouseholdRole.Editor or HouseholdRole.Viewer,
            HouseholdMembershipChange.TransferOwner => added.Length == 0 && removed.All(id => id == previous.OwnerId) &&
                changed.All(id => id == previous.OwnerId || id == next.OwnerId) && before.ContainsKey(next.OwnerId) &&
                after.Keys.All(id => before[id].AcceptanceHash == after[id].AcceptanceHash && SameDevices(before[id].Devices, after[id].Devices)) &&
                (!after.TryGetValue(previous.OwnerId, out var oldOwner) || oldOwner.Role is HouseholdRole.Editor or HouseholdRole.Viewer),
            HouseholdMembershipChange.RecoverOwner => added.Length == 0 && removed.Length == 0 && changed.Length == 1 &&
                changed[0] == previous.OwnerId && SameMemberExceptDevices(before[changed[0]], after[changed[0]]) &&
                after[changed[0]].Devices.Length == 1 && !before[changed[0]].Devices.Any(device => device.Id == after[changed[0]].Devices[0].Id),
            HouseholdMembershipChange.RotateOwnerRoot => added.Length == 0 && removed.Length == 0 && changed.Length == 0,
            _ => false,
        };
        return valid ? [] : ["UnexpectedHouseholdRosterEffect"];
    }

    /// <summary>Compares exact member consent/role and canonical device identities, independently of wire array order.</summary>
    private static bool SameMember(HouseholdMemberDefinition left, HouseholdMemberDefinition right) =>
        SameMemberExceptDevices(left, right) && SameDevices(left.Devices, right.Devices);
    /// <summary>A device approval/removal cannot silently replace the identity's role or original consent.</summary>
    private static bool SameMemberExceptDevices(HouseholdMemberDefinition left, HouseholdMemberDefinition right) =>
        left.IdentityId == right.IdentityId && left.Role == right.Role && left.AcceptanceHash == right.AcceptanceHash;
    /// <summary>Checks exact device-id/key pairs without making array ordering an authorization decision.</summary>
    private static bool SameDevices(HouseholdDeviceDefinition[] left, HouseholdDeviceDefinition[] right) =>
        left.Length == right.Length && left.OrderBy(device => device.Id).SequenceEqual(right.OrderBy(device => device.Id));
    /// <summary>One explicit addition/removal preserves every previously retained public key.</summary>
    private static bool OneDeviceChange(HouseholdMemberDefinition before, HouseholdMemberDefinition after, bool addition)
    {
        var smaller = addition ? before.Devices : after.Devices;
        var larger = addition ? after.Devices : before.Devices;
        return larger.Length == smaller.Length + 1 && smaller.All(device => larger.Contains(device));
    }

    /// <summary>Bounds registered identity references without treating a display name/email as authorization.</summary>
    private static bool Identity(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128;
    /// <summary>Bounds only public SPKI before platform import; the cryptographic layer still checks exact algorithms/canonical DER.</summary>
    private static bool Keys(HouseholdPublicKeys? value) => value is { SigningKey.Length: > 0 and <= 512, WrappingKey.Length: > 0 and <= 2048 };
}
