using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Vafadar.Zanance.Core.Sync;

/// <summary>Standard content digests for immutable protocol replay, independent of transport framing.</summary>
public static class SyncProtocolDigest
{
    /// <summary>Hashes the complete typed part including its index and proposals.</summary>
    public static string Part(SyncRoundPart part) => Convert.ToHexStringLower(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(part, PersonalSyncJsonContext.Default.SyncRoundPart)));

    /// <summary>Binds all ordered parts; no missing or reordered prefix can become a complete round.</summary>
    public static string Round(IEnumerable<string> orderedPartDigests)
    {
        var values = orderedPartDigests.ToArray();
        if (values.Any(value => !IsDigest(value))) throw new ArgumentException("Invalid immutable part digest.");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', values))));
    }

    /// <summary>Binds successful completion to its exact explicit review choices as well as financial content.</summary>
    public static string Completion(SyncCommitRound completion) => Convert.ToHexStringLower(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(completion, PersonalSyncJsonContext.Default.SyncCommitRound)));

    /// <summary>Recognizes only canonical lowercase SHA-256 text.</summary>
    public static bool IsDigest(string? value) => value is { Length: 64 } &&
        value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
}
