using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.HouseholdQualification;

/// <summary>Content-free literal-byte proofs consume bounded native pages, not a materialized production ciphertext attempt.</summary>
internal static class RetainedCiphertext
{
    /// <summary>Hashes original canonical messages incrementally; the proof never includes private bytes or actual message text.</summary>
    internal static async Task<string> HashAsync(HouseholdJournal journal, HouseholdJournalAuthority authority, HouseholdJournalAttempt attempt, CancellationToken ct)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(attempt.Open, HouseholdMetadataJsonContext.Default.HouseholdOpenRound));
        hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(attempt.Commit, HouseholdMetadataJsonContext.Default.HouseholdCommitRound));
        for (var offset = 0; offset < attempt.Parts.Count; offset += 8)
            foreach (var part in await journal.ReadAttemptPartsAsync(authority, attempt.Open.OperationId, attempt.Open.AttemptId, offset, Math.Min(8, attempt.Parts.Count - offset), ct))
                hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(part, HouseholdMetadataJsonContext.Default.HouseholdOpaquePart));
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    /// <summary>Only deliberately small signed negative fixtures collect a whole body array; the production sender uses bounded pages.</summary>
    internal static async Task<HouseholdOpaquePart[]> SmallFixtureAsync(HouseholdJournal journal, HouseholdJournalAuthority authority, HouseholdJournalAttempt attempt, CancellationToken ct)
    {
        if (attempt.Parts.Count > 128) throw new InvalidDataException("The negative fixture exceeds its explicit ciphertext bound.");
        var result = new List<HouseholdOpaquePart>();
        for (var offset = 0; offset < attempt.Parts.Count; offset += 8)
            result.AddRange(await journal.ReadAttemptPartsAsync(authority, attempt.Open.OperationId, attempt.Open.AttemptId, offset, Math.Min(8, attempt.Parts.Count - offset), ct));
        return result.ToArray();
    }
}
