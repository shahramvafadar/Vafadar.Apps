using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.HouseholdQualification;

namespace Vafadar.Zanance.ProtectedSync;

/// <summary>Original native outbound transaction interruption, source refusal and exact-byte cold publication; no production test hook is installed.</summary>
internal static class OutboundContinuation
{
    /// <summary>A retained local empty decision is replanned after a concurrent real signed publication, without another financial upload or frozen-cutoff replacement.</summary>
    internal static async Task NoOpRaceAsync(ProtectedClient client, HouseholdJournal journal, HouseholdSyncCoordinator coordinator,
        HouseholdServiceTransport transport, HouseholdJournalState state, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("The original unpublished no-op continuation invariant failed."); }
        Require(state.Cursor == 11 && await journal.ReadAttemptAsync(state.Authority, ct) is null);
        var local = (await journal.PrepareOriginalAsync(state.Authority, transport.Peer, client.Sync, ct)).Attempt ?? throw new InvalidDataException();
        Require(local.Parts.Count == 0 && !local.Committed && local.RemoteId is null);
        using var replica = await journal.ReadReplicaAsync(state.Authority, ct);
        var row = replica.Snapshot().Values.Single(item => item.Record.Table == "Entries" && item.Values is not null);
        var value = System.Text.Json.Nodes.JsonNode.Parse(row.Values!.Value.GetRawText())!.AsObject(); value["Amount"] = 1800;
        using var keys = await journal.ReadKeysAsync(state.Authority, ct);
        var membership = await journal.ReadCurrentMembershipAsync(state.Authority, ct); using var epoch = await journal.ReadEpochAsync(state.Authority, membership.Epoch, ct);
        var concurrent = await replica.PrepareAsync(membership, epoch, state.DeviceId, keys.Device, Guid.NewGuid(), Guid.NewGuid(),
            [new(row.Record, row.Version, JsonSerializer.SerializeToElement(value))], [], new Dictionary<string, byte[]>(), new(TimeProvider.System));
        Require(concurrent.Plan.Succeeded && concurrent.Parts.Count == 1);
        var handle = await transport.OpenAsync(state.SpaceId, concurrent.Open, ct);
        await transport.PutAsync(state.SpaceId, handle.Id, concurrent.Parts[0], ct);
        Require((await transport.CommitAsync(state.SpaceId, handle.Id, concurrent.Commit, ct)).CommittedCursor == 12);
        Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
        Require((await client.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1800 && await journal.ReadAttemptAsync(state.Authority, ct) is null);
        using var verified = await journal.ReadReplicaAsync(state.Authority, ct); Require(verified.Cursor == 12);
        proof("unacknowledged-local-zero-part-plan-replans-same-frozen-cutoff-after-real-concurrent-head-change-without-extra-publication");
    }

    /// <summary>Each explicit phase continues the same original file/key/cutoff; deliberate process exits leave all bytes for the next process.</summary>
    internal static async Task RunAsync(string mode, ProtectedClient client, HouseholdJournal journal, HouseholdSyncCoordinator coordinator,
        HouseholdServiceTransport transport, HouseholdJournalState state, string root, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("The captured native outbound continuation invariant failed."); }
        var factory = client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var path = Path.Combine(root, "outbound-stream-proof.json");
        if (mode == "stream-seal-cut")
        {
            Require(state.Cursor == 10 && !File.Exists(path) && await journal.ReadAttemptAsync(state.Authority, ct) is null);
            var entry = (await client.Store.GetEntriesAsync(cancellationToken: ct)).Single(); entry.Amount = 1700;
            Require((await client.Store.SaveEntryAsync(entry, ct)).Succeeded);
            var attachment = new EntryAttachment { EntryId = entry.Id, FileName = "fictitious-outbound-stream.bin", ContentType = "application/octet-stream",
                Data = RandomNumberGenerator.GetBytes(4 * 1024 * 1024 + 32773) };
            var hash = Convert.ToHexStringLower(SHA256.HashData(attachment.Data)); var length = attachment.Data.Length;
            try { await client.Store.AddAttachmentAsync(attachment, ct); }
            finally { CryptographicOperations.ZeroMemory(attachment.Data); }
            var work = await client.Sync.CaptureRoundAsync(transport.Peer, state.Authority.IdentityId, ct, ProfileSynchronizationKind.Household);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new StreamProof(work.Operation, work.Cutoff, work.ContentHash, attachment.Id, length, hash, Guid.Empty, null)), ct);
            using var writes = new CipherWrites(() =>
            {
                proof("eight-original-ciphertext-inserts-executed-inside-uncommitted-native-transaction-before-real-process-exit");
                Environment.Exit(32);
            });
            using var observed = DiagnosticListener.AllListeners.Subscribe(writes);
            _ = await journal.PrepareOriginalAsync(state.Authority, transport.Peer, client.Sync, ct);
            throw new InvalidDataException("The actual transaction-loss boundary was not exercised.");
        }
        var expected = JsonSerializer.Deserialize<StreamProof>(await File.ReadAllTextAsync(path, ct)) ?? throw new InvalidDataException();
        Require(state.Cursor == 10);
        var frozen = await client.Sync.CaptureRoundAsync(transport.Peer, state.Authority.IdentityId, ct, ProfileSynchronizationKind.Household);
        Require(frozen.Operation == expected.Operation && frozen.Cutoff == expected.Cutoff && frozen.ContentHash == expected.Capture);
        if (mode == "stream-seal-resume")
        {
            Require(await journal.ReadAttemptAsync(state.Authority, ct) is null);
            await RequireEmptyPartsAsync();
            proof("cold-native-rollback-removes-half-sealed-ciphertext-and-preserves-original-operation-cutoff-and-source");
            // Alter and restore only this captured synthetic source cell. The late whole-source
            // hash failure must roll back even already-flushed encrypted parts.
            byte[] saved;
            await using (var db = await factory.CreateDbContextAsync(ct))
            {
                var part = await db.Set<SyncBlobPart>().SingleAsync(row => row.Hash == expected.Hash && row.Part == 2, ct);
                saved = part.Data.ToArray(); part.Data = part.Data.ToArray(); part.Data[0] ^= 1; await db.SaveChangesAsync(ct);
                CryptographicOperations.ZeroMemory(part.Data);
            }
            var refused = false;
            try { _ = await journal.PrepareOriginalAsync(state.Authority, transport.Peer, client.Sync, ct); }
            catch (CryptographicException) { refused = true; }
            finally
            {
                await using var db = await factory.CreateDbContextAsync(ct);
                var part = await db.Set<SyncBlobPart>().SingleAsync(row => row.Hash == expected.Hash && row.Part == 2, ct);
                part.Data = saved; await db.SaveChangesAsync(ct); CryptographicOperations.ZeroMemory(saved);
            }
            Require(refused); await RequireEmptyPartsAsync();
            proof("late-original-attachment-sha-refusal-rolls-back-all-flushed-ciphertext-with-no-network-publication");
            using (var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct))
            using (var writes = new CipherWrites(cancellation.Cancel))
            using (var observed = DiagnosticListener.AllListeners.Subscribe(writes))
            {
                var canceled = false;
                try { _ = await journal.PrepareOriginalAsync(state.Authority, transport.Peer, client.Sync, cancellation.Token); }
                catch (OperationCanceledException) { canceled = cancellation.IsCancellationRequested; }
                Require(canceled);
            }
            await RequireEmptyPartsAsync();
            proof("cancellation-after-real-native-ciphertext-writes-rolls-back-with-original-frozen-cutoff-unchanged");
            var prepared = (await journal.PrepareOriginalAsync(state.Authority, transport.Peer, client.Sync, ct)).Attempt ?? throw new InvalidDataException();
            Require(prepared.Parts.Count > 32 && prepared.Open.OperationId == expected.Operation && prepared.Cutoff == expected.Cutoff && prepared.CaptureHash == expected.Capture);
            var digest = await RetainedCiphertext.HashAsync(journal, state.Authority, prepared, ct);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(expected with { Attempt = prepared.Open.AttemptId, Ciphertext = digest }), ct);
            proof("complete-original-streamed-seal-durably-retained-before-first-network-request-and-process-exit");
            Environment.Exit(33);
        }
        Require(mode == "stream-seal-publish" && expected.Attempt != Guid.Empty && expected.Ciphertext is not null);
        var retained = await journal.ReadAttemptAsync(state.Authority, ct) ?? throw new InvalidDataException();
        Require(retained.Open.OperationId == expected.Operation && retained.Open.AttemptId == expected.Attempt && retained.Cutoff == expected.Cutoff &&
            retained.CaptureHash == expected.Capture && await RetainedCiphertext.HashAsync(journal, state.Authority, retained, ct) == expected.Ciphertext);
        string originalJson;
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            var row = await db.Set<HouseholdLocalAttemptPart>().SingleAsync(part => part.Index == 0, ct); originalJson = row.PartJson;
            var part = JsonSerializer.Deserialize(originalJson, Vafadar.Zanance.Core.Sharing.HouseholdMetadataJsonContext.Default.HouseholdOpaquePart) ?? throw new InvalidDataException();
            var corrupted = part with { Ciphertext = (part.Ciphertext[0] == 'A' ? "B" : "A") + part.Ciphertext[1..] };
            row.PartJson = JsonSerializer.Serialize(corrupted, Vafadar.Zanance.Core.Sharing.HouseholdMetadataJsonContext.Default.HouseholdOpaquePart);
            await db.SaveChangesAsync(ct);
        }
        var bodyRefused = false;
        try { _ = await journal.ReadAttemptPartsAsync(state.Authority, retained.Open.OperationId, retained.Open.AttemptId, 0, 1, ct); }
        catch (CryptographicException) { bodyRefused = true; }
        finally
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var row = await db.Set<HouseholdLocalAttemptPart>().SingleAsync(part => part.Index == 0, ct); row.PartJson = originalJson; await db.SaveChangesAsync(ct);
        }
        Require(bodyRefused && await RetainedCiphertext.HashAsync(journal, state.Authority, retained, ct) == expected.Ciphertext);
        proof("bounded-original-outbound-body-reader-refuses-ciphertext-disagreement-with-signed-manifest-before-http");
        Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
        Require(await journal.ReadAttemptAsync(state.Authority, ct) is null && (await client.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1700);
        var content = await client.Store.GetAttachmentAsync(expected.Attachment, ct) ?? throw new InvalidDataException();
        try { Require(content.Data.Length == expected.Length && Convert.ToHexStringLower(SHA256.HashData(content.Data)) == expected.Hash); }
        finally { CryptographicOperations.ZeroMemory(content.Data); }
        using var replica = await journal.ReadReplicaAsync(state.Authority, ct); Require(replica.Cursor == 11);
        proof("cold-original-streamed-ciphertext-publishes-once-through-bounded-pages-and-preserves-complete-source-hash-and-financial-amount");

        async Task RequireEmptyPartsAsync()
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            Require(!await db.Set<HouseholdLocalAttempt>().AnyAsync(ct) && !await db.Set<HouseholdLocalAttemptPart>().AnyAsync(ct));
        }
    }

    /// <summary>Observes real EF native command completion, confined to this process and exact private table; disposal removes every subscription.</summary>
    private sealed class CipherWrites(Action afterEight) : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private readonly List<IDisposable> subscriptions = [];
        private int writes;
        /// <inheritdoc />
        public void OnNext(DiagnosticListener value)
        { if (value.Name == "Microsoft.EntityFrameworkCore") subscriptions.Add(value.Subscribe(this)); }
        /// <inheritdoc />
        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == RelationalEventId.CommandExecuted.Name && value.Value is CommandExecutedEventData data &&
                data.Command.CommandText.StartsWith("INSERT INTO \"HouseholdLocalAttemptParts\"", StringComparison.Ordinal) && ++writes == 8) afterEight();
        }
        /// <inheritdoc />
        public void OnError(Exception error) { }
        /// <inheritdoc />
        public void OnCompleted() { }
        /// <inheritdoc />
        public void Dispose() { foreach (var value in subscriptions) value.Dispose(); subscriptions.Clear(); }
    }
    /// <summary>Synthetic source/operation commitments only; ciphertext and key bytes remain in original protected storage.</summary>
    private sealed record StreamProof(Guid Operation, long Cutoff, string Capture, Guid Attachment, int Length, string Hash, Guid Attempt, string? Ciphertext);
}
