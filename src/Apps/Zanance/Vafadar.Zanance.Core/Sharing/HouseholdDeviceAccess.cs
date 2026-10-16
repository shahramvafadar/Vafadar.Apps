using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vafadar.Zanance.Core.Sharing;

/// <summary>Owner-signed public checkpoint for another device of one already accepted identity; it reserves no seat and grants no keys.</summary>
public sealed record HouseholdDeviceAccessStatement([property: JsonRequired] int Format, [property: JsonRequired] Guid SpaceId,
    [property: JsonRequired] Guid OperationId, [property: JsonRequired] long MembershipVersion, [property: JsonRequired] string MembershipHead,
    [property: JsonRequired] string IdentityId, [property: JsonRequired] long ExpiresUnixSeconds);

/// <summary>Explicit public setup package; the root fingerprint must still be compared independently before a private device key is created.</summary>
public sealed record HouseholdDeviceAccessPackage([property: JsonRequired] int Format, [property: JsonRequired] string Kind,
    [property: JsonRequired] HouseholdPublicKeys OwnerKeys, [property: JsonRequired] string SignedAuthorization);

/// <summary>Bounded setup discrimination only; typed signature/history verification remains mandatory at the actual trust boundary.</summary>
public static class HouseholdSetupAnchors
{
    /// <summary>Public existing-member device request, distinct from invitation capabilities and recovery kits.</summary>
    public const string DeviceAccess = "zanance.household.device-access.v1";
    /// <summary>Original private-kit recovery anchor already retained by the delivered recovery flow.</summary>
    public const string OwnerRecovery = "zanance.household.recovered-anchor.v1";
    /// <summary>Standard JWS purpose; another signed metadata type cannot be reinterpreted as device authorization.</summary>
    public const string DeviceAccessProof = "zanance-household-device-access+jwt";

    /// <summary>Reads exactly one explicit kind from a finite JSON object, without interpreting a JWS invitation as JSON or granting authority.</summary>
    public static string? Kind(string? value)
    {
        if (value is null || value.Length is < 2 or > 8192 || !value.StartsWith('{')) return null;
        try
        {
            using var document = JsonDocument.Parse(value, new JsonDocumentOptions { MaxDepth = 8 });
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
            var kinds = document.RootElement.EnumerateObject().Where(property => property.NameEquals("kind")).ToArray();
            return kinds.Length == 1 && kinds[0].Value.ValueKind == JsonValueKind.String ? kinds[0].Value.GetString() : null;
        }
        catch (JsonException) { return null; }
    }
}
