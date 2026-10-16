using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.ProtectedSync;

/// <summary>Qualifies actual incremental verifier pull/disposal and complete-before-state semantics over the original protected replica.</summary>
internal static class OrderedReplayRefusals
{
    /// <summary>Invalid first ciphertext is refused without fetching the tail; missing/excess/canceled input changes no original financial cursor or graph.</summary>
    internal static async Task RunAsync(HouseholdJournal journal, HouseholdJournalState state, Action<string> proof, CancellationToken ct)
    {
        using var replica = await journal.ReadReplicaAsync(state.Authority, ct);
        using var keys = await journal.ReadKeysAsync(state.Authority, ct);
        var membership = await journal.ReadCurrentMembershipAsync(state.Authority, ct);
        using var epoch = await journal.ReadEpochAsync(state.Authority, membership.Epoch, ct);
        var original = replica.Snapshot(); var cursor = replica.Cursor; var financial = replica.FinancialCursor; var head = replica.PublicationHead;
        var proposals = original.Values.Where(row => row.Record.Table == "Accounts" && row.Values is not null).Take(2).Select(row =>
        {
            var value = JsonNode.Parse(row.Values!.Value.GetRawText())!.AsObject(); value["Name"] = "Fictitious bounded replay candidate";
            return new SyncProposal(row.Record, row.Version, JsonSerializer.SerializeToElement(value));
        }).ToArray();
        var crypto = new HouseholdCryptography(TimeProvider.System);
        var candidate = await replica.PrepareAsync(membership, epoch, state.DeviceId, keys.Device, Guid.NewGuid(), Guid.NewGuid(), proposals, [], new Dictionary<string, byte[]>(), crypto);
        proof("ordered-replay-candidate-prepared-parts-" + candidate.Parts.Count);
        if (!candidate.Plan.Succeeded || candidate.Parts.Count != 2) throw new InvalidDataException("The exact original two-row candidate is required.");
        var parts = candidate.Parts.ToArray(); var corrupt = parts.ToArray();
        corrupt[0] = corrupt[0] with { Ciphertext = "a.b.c.d.e", Digest = HouseholdPublicationDigest.Ciphertext("a.b.c.d.e") };
        var invalid = Packet(corrupt);
        var pulledTail = false; var disposed = false;
        IEnumerable<HouseholdOpaqueEvent> InvalidFirst()
        {
            try { yield return invalid.Events[0]; pulledTail = true; yield return invalid.Events[1]; }
            finally { disposed = true; }
        }
        var refused = false;
        try { replica.ApplyOrdered(membership, epoch, invalid.Receipt, InvalidFirst(), 2, crypto, null, ct); }
        catch (CryptographicException) { refused = true; }
        Require(refused && !pulledTail && disposed); Unchanged();
        proof("ordered-replay-invalid-first-ciphertext-refused-without-prefetch-and-reader-disposed");
        var valid = Packet(parts);
        refused = false;
        try { replica.ApplyOrdered(membership, epoch, valid.Receipt, valid.Events.Take(1), 2, crypto, null, ct); }
        catch (CryptographicException) { refused = true; }
        Require(refused); Unchanged();
        refused = false;
        try { replica.ApplyOrdered(membership, epoch, valid.Receipt, valid.Events.Concat([valid.Events[1]]), 2, crypto, null, ct); }
        catch (CryptographicException) { refused = true; }
        Require(refused); Unchanged();
        using var canceled = new CancellationTokenSource();
        proof("ordered-replay-missing-and-excess-input-refused-without-financial-publication");
        IEnumerable<HouseholdOpaqueEvent> Interrupted()
        { yield return valid.Events[0]; canceled.Cancel(); yield return valid.Events[1]; }
        var stopped = false;
        try { replica.ApplyOrdered(membership, epoch, valid.Receipt, Interrupted(), 2, crypto, null, canceled.Token); }
        catch (OperationCanceledException) { stopped = true; }
        Require(stopped); Unchanged();
        proof("incremental-original-cipher-verifier-does-not-prefetch-invalid-tail-disposes-reader-and-refuses-missing-excess-or-canceled-input-before-financial-publication");

        void Require(bool condition) { if (!condition) throw new InvalidDataException("The bounded original replay invariant failed."); }
        void Unchanged() => Require(replica.Cursor == cursor && replica.FinancialCursor == financial && replica.PublicationHead == head &&
            replica.Snapshot().Count == original.Count && replica.Snapshot().All(row => Equals(row.Value, original[row.Key])));
        (HouseholdPublicationReceipt Receipt, HouseholdOpaqueEvent[] Events) Packet(HouseholdOpaquePart[] values)
        {
            var manifest = values.Select(part => new HouseholdPartCommitment(part.Index, part.RecordId, part.BaseVersion, part.Digest)).ToArray();
            var statement = new HouseholdCommitStatement(1, state.SpaceId, state.DeviceId, candidate.Open.OperationId, candidate.Open.AttemptId,
                cursor, cursor + 1, membership.Version, epoch.Epoch, head, 2, HouseholdPublicationDigest.Manifest(manifest));
            var signed = StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(statement, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement),
                keys.Device, "zanance-household-commit+jwt");
            var receipt = new HouseholdPublicationReceipt(Guid.NewGuid(), signed, manifest);
            return (receipt, values.Select(part => new HouseholdOpaqueEvent(cursor + 1, receipt.RoundId, part.RecordId, epoch.Epoch, membership.Version,
                state.DeviceId, candidate.Open.OperationId, candidate.Open.AttemptId, part.Index, part.BaseVersion, part.Ciphertext, part.Digest)).ToArray());
        }
    }
}
