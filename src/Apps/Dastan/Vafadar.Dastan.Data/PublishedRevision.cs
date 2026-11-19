using System.Security.Cryptography;
using System.Text;

namespace Vafadar.Dastan.Data;

/// <summary>
/// What the editors' store and the site agree on about a published revision as the database keeps it
/// (ADR 0015): the schema it lies in, and the hash that says a revision is still the text it was published as.
/// </summary>
public static class PublishedRevision
{
    /// <summary>The schema of the database that holds the published revisions; the site reads this one and no other of the editors.</summary>
    public const string Schema = "published";

    /// <summary>
    /// Returns the hash of a revision: SHA-256 over the file of the revision and the translations published
    /// with it, as lower-case hexadecimal. Who publishes writes it; who reads compares it.
    /// </summary>
    /// <param name="json">The file of the revision.</param>
    /// <param name="translations">The translations published with it, as one JSON object by language.</param>
    public static string HashOf(string json, string translations)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(translations);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes(json));
        hash.AppendData([0]);
        hash.AppendData(Encoding.UTF8.GetBytes(translations));
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
