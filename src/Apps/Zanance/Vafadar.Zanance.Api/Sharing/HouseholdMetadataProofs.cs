using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Public signed metadata only; the service never invokes financial/epoch decryption.</summary>
internal static class HouseholdMetadataProofs
{
    /// <summary>Owner-root membership authority cannot be interpreted as a financial publication.</summary>
    internal const string MembershipType = "zanance-household-membership+jwt";
    /// <summary>Independent new-root consent is distinct from an old-root transfer offer.</summary>
    internal const string TransferType = "zanance-household-owner-acceptance+jwt";
    /// <summary>One-use reserved-seat authority.</summary>
    internal const string InvitationType = "zanance-household-invitation+jwt";
    /// <summary>Recipient's exact acceptance/key-possession proof.</summary>
    internal const string ClaimType = "zanance-household-invitation-claim+jwt";
    /// <summary>Device author proof of an atomic opaque publication.</summary>
    internal const string CommitType = "zanance-household-commit+jwt";

    /// <summary>Imports exactly the selected public algorithms before using a key as an asserted directory identity.</summary>
    internal static PublicSigningRecipientIdentity Public(HouseholdPublicKeys? keys)
    {
        if (keys is null) throw new HouseholdProtocolRefusal(400, "InvalidHouseholdPublicKey");
        try
        {
            var result = new PublicSigningRecipientIdentity(keys.SigningKey, keys.WrappingKey);
            using var signing = result.OpenSigningKey(); using var wrapping = result.OpenWrappingKey();
            return result;
        }
        catch (Exception error) when (error is CryptographicException or ArgumentException or FormatException)
        { throw new HouseholdProtocolRefusal(400, "InvalidHouseholdPublicKey"); }
    }

    /// <summary>Verifies against an already established root, never a key advertised inside the incoming statement.</summary>
    internal static HouseholdMembershipStatement Membership(string token, PublicSigningRecipientIdentity trustedRoot)
    {
        var value = Read(token, trustedRoot, MembershipType, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement);
        if (HouseholdMembershipRules.ValidateShape(value).Count != 0)
            throw new HouseholdProtocolRefusal(400, "InvalidHouseholdMembership");
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        _ = Public(value.OwnerKeys);
        foreach (var device in value.Members.SelectMany(member => member.Devices))
            if (!fingerprints.Add(Public(device.Keys).Fingerprint))
                throw new HouseholdProtocolRefusal(400, "DuplicateHouseholdDevice");
        return value;
    }

    /// <summary>Historical signatures remain verifiable; only a newly committed proposal must be inside its bounded validity.</summary>
    internal static void RequireCurrentProposal(HouseholdMembershipStatement statement, DateTimeOffset now)
    {
        var end = statement.ValidUntilUnixSeconds;
        if (end <= now.ToUnixTimeSeconds() || end > now.Add(HouseholdLimits.MembershipProposalLifetime).ToUnixTimeSeconds())
            throw new HouseholdProtocolRefusal(409, "HouseholdProposalExpiredOrFuture");
    }

    /// <summary>Uses a fixed caller-selected type/schema with no case-insensitive or dynamic CLR deserialization.</summary>
    internal static T Read<T>(string token, PublicSigningRecipientIdentity trusted, string type, JsonTypeInfo<T> json) where T : class
    {
        try
        {
            return StandardJoseSignatures.VerifyJson(token, trusted, type, HouseholdLimits.MembershipBytes, json);
        }
        catch (Exception error) when (error is CryptographicException or JsonException or ArgumentException)
        { throw new HouseholdProtocolRefusal(400, "InvalidHouseholdProof"); }
    }

    /// <summary>Digest of bounded signed metadata/ciphertext, never a credential, clear financial value or client decryption key.</summary>
    internal static string Digest(string value, int maximumCharacters)
    {
        if (string.IsNullOrEmpty(value) || value.Length > maximumCharacters)
            throw new HouseholdProtocolRefusal(413, "HouseholdProtectedContentBound");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
