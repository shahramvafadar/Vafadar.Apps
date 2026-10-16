using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.ProtectedSync;

/// <summary>Actual process loss in a newly staged ciphertext packet qualifies protected header-prefix resume independently of historic fixture layouts.</summary>
internal static class HeaderContinuation
{
    /// <summary>Prepares once before actual network publication, or continues exactly the original retained attempt/partial prefix after process exit.</summary>
    internal static async Task RunAsync(ProtectedClient client, HouseholdJournal journal, HouseholdSyncCoordinator coordinator,
        HouseholdServiceTransport transport, HouseholdJournalState state, string root, bool resume, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("The exact original protected header continuation invariant failed."); }
        var evidencePath = Path.Combine(root, "header-continuation-proof.json");
        if (!resume)
        {
            Require(state.Cursor == 8 && !File.Exists(evidencePath) && await journal.ReadAttemptAsync(state.Authority, ct) is null);
            var expense = (await client.Store.GetEntriesAsync(cancellationToken: ct)).Single();
            var attachment = new EntryAttachment { EntryId = expense.Id, FileName = "fictitious-header-continuation.bin", ContentType = "application/octet-stream",
                Data = RandomNumberGenerator.GetBytes(2 * 1024 * 1024 + 32771) };
            try
            {
                await client.Store.AddAttachmentAsync(attachment, ct);
                await File.WriteAllTextAsync(evidencePath, JsonSerializer.Serialize(new HeaderProof(attachment.Id, attachment.Data.Length,
                    Convert.ToHexStringLower(SHA256.HashData(attachment.Data)))), ct);
            }
            finally { CryptographicOperations.ZeroMemory(attachment.Data); }
        }
        else
        {
            Require(state.Cursor == 8);
            var expected = JsonSerializer.Deserialize(await File.ReadAllTextAsync(Path.Combine(root, "header-continuation-prefix.json"), ct),
                HouseholdMetadataJsonContext.Default.HouseholdFeedPosition) ?? throw new InvalidDataException();
            Require(expected.Cursor == 9 && expected.RecordId.Length == 64 && expected.RecordId != new string('f', 64));
            Require(await journal.InboxPositionAsync(state.Authority, ct) == expected);
            proof("cold-original-pending-prefix-matches-its-signed-manifest-with-bounded-protected-header-reads");
        }
        Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
        if (!resume) throw new InvalidDataException("The actual process-loss boundary was not exercised.");
        var expectedContent = JsonSerializer.Deserialize<HeaderProof>(await File.ReadAllTextAsync(evidencePath, ct)) ?? throw new InvalidDataException();
        var content = await client.Store.GetAttachmentAsync(expectedContent.Id, ct) ?? throw new InvalidDataException();
        try { Require(content.Data.Length == expectedContent.Length && Convert.ToHexStringLower(SHA256.HashData(content.Data)) == expectedContent.Hash); }
        finally { CryptographicOperations.ZeroMemory(content.Data); }
        using var verified = await journal.ReadReplicaAsync(state.Authority, ct);
        Require(verified.Cursor == 9 && verified.ResidentAttachmentBytes == expectedContent.Length + 1800);
        Require(await journal.ReadAttemptAsync(state.Authority, ct) is null);
        proof("same-original-ciphertext-prefix-continues-after-process-loss-with-complete-attachment-and-original-financial-application");
    }

    /// <summary>Public synthetic content identity only; original attachment bytes and keys remain protected.</summary>
    private sealed record HeaderProof(Guid Id, int Length, string Hash);
}
