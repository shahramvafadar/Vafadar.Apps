using System.Security.Cryptography;
using System.Text;

namespace Vafadar.Authentication.OAuth;

/// <summary>
/// A Proof Key for Code Exchange pair (RFC 7636, method <c>S256</c>): the verifier stays in the app, only its SHA-256
/// challenge goes into the authorization request. An app that intercepts the redirect cannot redeem the code without
/// the verifier, which is why public clients need no client secret.
/// </summary>
/// <param name="Verifier">The random code verifier, 43 characters of base64url (32 random bytes).</param>
/// <param name="Challenge">The base64url-encoded SHA-256 hash of <paramref name="Verifier"/>.</param>
public sealed record PkceCodes(string Verifier, string Challenge)
{
    /// <summary>The only challenge method used: SHA-256.</summary>
    public const string Method = "S256";

    /// <summary>Creates a new pair from 32 cryptographically random bytes.</summary>
    public static PkceCodes Create()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        return new PkceCodes(verifier, ChallengeOf(verifier));
    }

    /// <summary>Returns the <c>S256</c> challenge of <paramref name="verifier"/>.</summary>
    public static string ChallengeOf(string verifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(verifier);
        return Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    }

    /// <summary>Returns a random, URL-safe value for the <c>state</c> parameter (16 random bytes).</summary>
    public static string NewState() => Base64Url(RandomNumberGenerator.GetBytes(16));

    // base64url without padding (RFC 7636 appendix A).
    internal static string Base64Url(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
