using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.ProtectedSync;

/// <summary>Changed framing refusals over the retained actual protected native replica; no network publication or owner data is involved.</summary>
internal static class PackedRefusals
{
    /// <summary>Changing one authenticated small-attachment byte still fails the original complete SHA/graph check before replacing any verified state.</summary>
    internal static async Task RejectChangedSmallAttachmentAsync(HouseholdJournal journal, HouseholdJournalState state,
        HouseholdJournalAttempt attempt, Action<string> proof, CancellationToken ct)
    {
        using var replica = await journal.ReadReplicaAsync(state.Authority, ct);
        using var keys = await journal.ReadKeysAsync(state.Authority, ct);
        var membership = await journal.ReadCurrentMembershipAsync(state.Authority, ct);
        using var epoch = await journal.ReadEpochAsync(state.Authority, membership.Epoch, ct);
        var crypto = new HouseholdCryptography(TimeProvider.System);
        var parts = await Vafadar.Zanance.HouseholdQualification.RetainedCiphertext.SmallFixtureAsync(journal, state.Authority, attempt, ct); var changed = false;
        var context = new HouseholdProtectionContext(state.SpaceId, epoch.Epoch, attempt.Open.OperationId, HouseholdProtectionPurpose.Financial, "members");
        for (var index = 0; index < parts.Length; index++)
        {
            var clear = epoch.Decrypt(crypto, context, parts[index].Ciphertext, keys.Device.Public); HouseholdFinancialPayload payload;
            try { payload = StrictJsonPayload.Deserialize(clear, HouseholdFinancialJsonContext.Default.HouseholdFinancialPayload, HouseholdCryptography.MaximumPlaintextBytes); }
            finally { CryptographicOperations.ZeroMemory(clear); }
            try
            {
                if (payload.Kind != HouseholdFinancialContentKind.AttachmentBatch) continue;
                if (payload.Blobs is not { Length: > 0 } blobs || blobs[0].Bytes.Length == 0) throw new InvalidDataException("A real original attachment batch is required.");
                blobs[0].Bytes[0] ^= 1;
                var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, HouseholdFinancialJsonContext.Default.HouseholdFinancialPayload);
                try
                {
                    var ciphertext = epoch.Encrypt(crypto, context, bytes, keys.Device);
                    parts[index] = parts[index] with { Ciphertext = ciphertext, Digest = HouseholdPublicationDigest.Ciphertext(ciphertext) }; changed = true;
                }
                finally { CryptographicOperations.ZeroMemory(bytes); }
                break;
            }
            finally
            {
                if (payload.Stream?.Bytes is { } stream) CryptographicOperations.ZeroMemory(stream);
                if (payload.Blobs is { } blobs) foreach (var blob in blobs) CryptographicOperations.ZeroMemory(blob.Bytes);
            }
        }
        if (!changed) throw new InvalidDataException("The original bounded small attachment packing was not exercised.");
        var original = replica.Snapshot(); var beforeCursor = replica.Cursor; var beforeFinancial = replica.FinancialCursor; var beforeHead = replica.PublicationHead;
        var statement = StandardJoseSignatures.VerifyJson(attempt.Commit.SignedCommit, keys.Device.Public, "zanance-household-commit+jwt",
            HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement);
        var commitments = parts.Select(part => new HouseholdPartCommitment(part.Index, part.RecordId, part.BaseVersion, part.Digest)).ToArray();
        var signed = StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(statement with { ContentHash = HouseholdPublicationDigest.Manifest(commitments) },
            HouseholdMetadataJsonContext.Default.HouseholdCommitStatement), keys.Device, "zanance-household-commit+jwt");
        var receipt = new HouseholdPublicationReceipt(Guid.NewGuid(), signed, commitments);
        var events = parts.Select(part => new HouseholdOpaqueEvent(statement.Cursor, receipt.RoundId, part.RecordId, epoch.Epoch, membership.Version,
            state.DeviceId, attempt.Open.OperationId, attempt.Open.AttemptId, part.Index, part.BaseVersion, part.Ciphertext, part.Digest)).ToArray();
        var refused = false; try { replica.Apply(membership, epoch, receipt, events, crypto); } catch (CryptographicException) { refused = true; }
        if (!refused || replica.Cursor != beforeCursor || replica.FinancialCursor != beforeFinancial || replica.PublicationHead != beforeHead ||
            replica.Snapshot().Count != original.Count || replica.Snapshot().Any(row => !Equals(row.Value, original[row.Key])))
            throw new InvalidDataException("The original financial state changed after a corrupted small attachment candidate.");
        proof("approved-signer-small-attachment-batch-corruption-refused-with-original-financial-graph-head-and-cursors-unchanged");
    }

    /// <summary>Even an approved private signer cannot apply a truncated stream, dishonest final row count or oversized row allocation.</summary>
    internal static async Task RunAsync(HouseholdJournal journal, HouseholdJournalState state, Action<string> proof, CancellationToken ct, bool allocationOnly = false)
    {
        using var replica = await journal.ReadReplicaAsync(state.Authority, ct);
        using var keys = await journal.ReadKeysAsync(state.Authority, ct);
        var membership = await journal.ReadCurrentMembershipAsync(state.Authority, ct);
        using var epoch = await journal.ReadEpochAsync(state.Authority, membership.Epoch, ct);
        var original = replica.Snapshot(); var beforeCursor = replica.Cursor; var beforeRows = replica.FinancialCursor; var beforeHead = replica.PublicationHead;
        var proposals = original.Values.Where(row => row.Record.Table == "Accounts" && row.Values is not null).Select(row =>
        {
            var body = JsonNode.Parse(row.Values!.Value.GetRawText())!.AsObject(); body["Name"] = "Fictitious framing refusal";
            return new SyncProposal(row.Record, row.Version, JsonSerializer.SerializeToElement(body));
        }).ToArray();
        if (proposals.Length < 4096) throw new InvalidDataException("The original retained large fixture is required.");
        var crypto = new HouseholdCryptography(TimeProvider.System);
        var prepared = await replica.PrepareAsync(membership, epoch, state.DeviceId, keys.Device, Guid.NewGuid(), Guid.NewGuid(), proposals, [], new Dictionary<string, byte[]>(), crypto);
        if (!prepared.Plan.Succeeded || prepared.Parts.Count < 2) throw new InvalidDataException("The changed complete stream was not exercised.");
        var context = new HouseholdProtectionContext(state.SpaceId, epoch.Epoch, prepared.Open.OperationId, HouseholdProtectionPurpose.Financial, "members");

        if (!allocationOnly)
        {
            Reject(Alter(prepared.Parts.Count - 1, block => block with { Records = checked(block.Records + 1) }));
            Reject(prepared.Parts.Take(prepared.Parts.Count - 1).ToArray());
        }
        Reject(Alter(0, block =>
        {
            BinaryPrimitives.WriteInt32BigEndian(block.Bytes, HouseholdFinancialFraming.MaximumRecordBytes + 1); return block;
        }));
        proof(allocationOnly ? "approved-signer-row-above-reviewed-16MiB-local-framing-bound-refused-with-original-graph-head-and-cursors-unchanged"
            : "approved-signer-packed-final-count-truncation-and-oversized-row-refused-with-original-native-graph-head-and-cursors-unchanged");

        HouseholdOpaquePart[] Alter(int index, Func<HouseholdRecordBlock, HouseholdRecordBlock> mutate)
        {
            var parts = prepared.Parts.ToArray(); var clear = epoch.Decrypt(crypto, context, parts[index].Ciphertext, keys.Device.Public);
            HouseholdFinancialPayload payload;
            try { payload = StrictJsonPayload.Deserialize(clear, HouseholdFinancialJsonContext.Default.HouseholdFinancialPayload, HouseholdCryptography.MaximumPlaintextBytes); }
            finally { CryptographicOperations.ZeroMemory(clear); }
            var block = payload.Stream ?? throw new InvalidDataException("The original packed stream is required.");
            byte[]? changed = null;
            try
            {
                changed = JsonSerializer.SerializeToUtf8Bytes(payload with { Stream = mutate(block) }, HouseholdFinancialJsonContext.Default.HouseholdFinancialPayload);
                var ciphertext = epoch.Encrypt(crypto, context, changed, keys.Device);
                parts[index] = parts[index] with { Ciphertext = ciphertext, Digest = HouseholdPublicationDigest.Ciphertext(ciphertext) }; return parts;
            }
            finally { CryptographicOperations.ZeroMemory(block.Bytes); if (changed is not null) CryptographicOperations.ZeroMemory(changed); }
        }
        void Reject(HouseholdOpaquePart[] parts)
        {
            var commitments = parts.Select(part => new HouseholdPartCommitment(part.Index, part.RecordId, part.BaseVersion, part.Digest)).ToArray();
            var statement = new HouseholdCommitStatement(1, state.SpaceId, state.DeviceId, prepared.Open.OperationId, prepared.Open.AttemptId,
                beforeCursor, beforeCursor + 1, membership.Version, epoch.Epoch, beforeHead, parts.Length, HouseholdPublicationDigest.Manifest(commitments));
            var signed = StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(statement, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement),
                keys.Device, "zanance-household-commit+jwt");
            var receipt = new HouseholdPublicationReceipt(Guid.NewGuid(), signed, commitments);
            var events = parts.Select(part => new HouseholdOpaqueEvent(beforeCursor + 1, receipt.RoundId, part.RecordId, epoch.Epoch, membership.Version,
                state.DeviceId, prepared.Open.OperationId, prepared.Open.AttemptId, part.Index, part.BaseVersion, part.Ciphertext, part.Digest)).ToArray();
            var refused = false;
            try { replica.Apply(membership, epoch, receipt, events, crypto); }
            catch (CryptographicException) { refused = true; }
            if (!refused || replica.Cursor != beforeCursor || replica.FinancialCursor != beforeRows || replica.PublicationHead != beforeHead ||
                replica.Snapshot().Count != original.Count || replica.Snapshot().Any(row => !Equals(row.Value, original[row.Key])))
                throw new InvalidDataException("A rejected framing candidate changed the original verified financial state.");
        }
    }
}
