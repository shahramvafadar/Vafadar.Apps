using System.Security.Cryptography;
using System.Text;

namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Public store account bindings; these identifiers are not credentials or purchase proof.</summary>
public static class BillingAccountBinding
{
    /// <summary>Provider-chain commitment shared with signed current grants; the original persisted id format stays unchanged.</summary>
    public static string PurchaseId(BillingIssuer issuer, BillingEnvironment environment, string reference)
    {
        if (!Enum.IsDefined(issuer) || !Enum.IsDefined(environment) || reference.Length is < 1 or > 16384 || reference.Any(char.IsControl))
            throw new ArgumentException("Bounded original provider reference required.");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("zanance.billing.purchase.v1:" + issuer + ":" + environment + "\0" + reference)));
    }
    /// <summary>Private receipt commitment for distinguishing a new native payment from an already owned purchase.</summary>
    public static string ReceiptHash(string reference)
    {
        if (reference.Length is < 1 or > 16384 || reference.Any(char.IsControl)) throw new ArgumentException("Bounded receipt required.");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("zanance.billing.native.receipt.v1\0" + reference)));
    }
    /// <summary>Exact signed original-command bytes, not semantic JSON or a replaceable operation id.</summary>
    public static string CommandHash(string signed) => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes("zanance.billing.command.bytes.v1\0" + signed)));
    /// <summary>Domain-separated service-confirmed identity commitment, never an email, profile id or unverified token subject.</summary>
    public static string Identity(string user)
    {
        if (string.IsNullOrWhiteSpace(user) || user.Length > 256 || user.Any(char.IsControl)) throw new ArgumentException("Actual bounded identity required.");
        return Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("zanance.billing.identity.v1\0" + user)));
    }
    /// <summary>Domain-separated Google opaque account id; installation and identity tokens cannot alias each other.</summary>
    public static string Google(Guid serverAccountToken, bool identity)
    {
        if (serverAccountToken == Guid.Empty) throw new ArgumentException("Original server account token required.");
        var purpose = identity ? "zanance.billing.google.identity.v1" : "zanance.billing.google.installation.v1";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(purpose + "\0" + serverAccountToken.ToString("N"))));
    }
}
