namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Disjoint authenticated purposes; a key grant can never be interpreted as financial content.</summary>
public enum HouseholdProtectionPurpose
{
    /// <summary>An encrypted signed financial publication.</summary>
    Financial = 1,
    /// <summary>One specifically approved device's encrypted epoch key grant.</summary>
    DeviceGrant = 2,
    /// <summary>Explicit user-held recovery material, separate from service authentication.</summary>
    Recovery = 3,
    /// <summary>An encrypted attachment piece, bound independently of financial records.</summary>
    Attachment = 4,
    /// <summary>Current owner-root private authority delegated only to another explicitly approved device of that same owning identity.</summary>
    OwnerRoot = 5,
}

/// <summary>Expected authority supplied by the trusted local operation, never inferred from an incoming token.</summary>
public sealed record HouseholdProtectionContext
{
    /// <summary>Creates an exact space, epoch, operation and recipient binding.</summary>
    public HouseholdProtectionContext(Guid spaceId, long epoch, Guid operationId,
        HouseholdProtectionPurpose purpose, string recipient)
    {
        if (spaceId == Guid.Empty || operationId == Guid.Empty || epoch < 1 ||
            !Enum.IsDefined(purpose) || recipient != "members" && !IsFingerprint(recipient))
            throw new ArgumentException("Exact household protection authority required.");
        if (purpose is HouseholdProtectionPurpose.DeviceGrant or HouseholdProtectionPurpose.OwnerRoot or HouseholdProtectionPurpose.Recovery && recipient == "members")
            throw new ArgumentException("A device grant requires one verified recipient fingerprint.");
        SpaceId = spaceId; Epoch = epoch; OperationId = operationId; Purpose = purpose; Recipient = recipient;
    }

    /// <summary>The actual shared space, never a personal profile id inferred from content.</summary>
    public Guid SpaceId { get; }
    /// <summary>The current explicitly authorized membership encryption generation.</summary>
    public long Epoch { get; }
    /// <summary>Stable operation identity retained across retries, not a newly minted encryption nonce.</summary>
    public Guid OperationId { get; }
    /// <summary>Separates financially interpreted content from grants, recovery and attachment pieces.</summary>
    public HouseholdProtectionPurpose Purpose { get; }
    /// <summary>Verified device fingerprint or the explicitly scoped current members.</summary>
    public string Recipient { get; }
    /// <summary>Exact ordinal audience for standard signed/encrypted household data messages.</summary>
    public string Audience => $"urn:vafadar:zanance:household:{SpaceId:N}:{(int)Purpose}";

    /// <summary>Accepts only the canonical lowercase SHA-256 public identity representation.</summary>
    public static bool IsFingerprint(string? value) => value is { Length: 64 }
        && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
}
