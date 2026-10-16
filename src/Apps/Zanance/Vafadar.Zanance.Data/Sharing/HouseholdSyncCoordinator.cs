using System.Net;
using System.Security.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>Persistent encrypted household orchestration over the original financial capture/review/blob/apply transactions; no readable financial HTTP fallback exists.</summary>
public sealed class HouseholdSyncCoordinator(PersonalSyncStore original, HouseholdJournal journal)
{
    private readonly SemaphoreSlim running = new(1, 1);

    /// <summary>Refreshes original signed authority and complete ciphertext horizon for a subsequent explicit administration action.</summary>
    public async Task<HouseholdJournalState> RefreshAuthorityAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, CancellationToken ct = default)
    {
        await running.WaitAsync(ct);
        try
        {
            var identity = await transport.IdentityAsync(ct);
            if (identity.Id != authority.IdentityId || transport.Peer.BindingKey(identity.Id) != authority.PeerKey) throw Refusal();
            var state = await journal.CaptureAsync(transport.Peer, identity.Id, ct) ?? throw Refusal();
            if (state.Authority != authority || state.SpaceId == Guid.Empty || state.DeviceId == Guid.Empty) throw Refusal();
            await RefreshAsync(transport, authority, state.SpaceId, state.DeviceId, ct);
            return await journal.CaptureAsync(transport.Peer, identity.Id, ct) ?? throw Refusal();
        }
        finally { running.Release(); }
    }

    /// <summary>Completes one original frozen financial cutoff, retaining literal ciphertext and partial verified downloads across interruption.</summary>
    public Task<PersonalSyncOutcome> SynchronizeAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, CancellationToken ct = default) =>
        SynchronizeCoreAsync(transport, authority, null, ct);

    /// <summary>Consumes only the exact target offer's already published financial horizon, with no own proposals, resolutions or outgoing ciphertext.</summary>
    internal Task<PersonalSyncOutcome> ReceiveOwnershipAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, Guid operation, CancellationToken ct) =>
        SynchronizeCoreAsync(transport, authority, operation, ct);

    /// <summary>Original captured-round pipeline; optional ownership reception remains a strictly incoming-only exact-operation scope.</summary>
    private async Task<PersonalSyncOutcome> SynchronizeCoreAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, Guid? ownershipReception, CancellationToken ct)
    {
        await running.WaitAsync(ct);
        try
        {
            var identity = await transport.IdentityAsync(ct);
            if (identity.Id != authority.IdentityId || transport.Peer.BindingKey(identity.Id) != authority.PeerKey) throw Refusal();
            var state = await journal.CaptureAsync(transport.Peer, identity.Id, ct) ?? throw Refusal();
            if (state.Authority != authority || state.SpaceId == Guid.Empty || state.DeviceId == Guid.Empty) throw Refusal();
            var pending = await journal.ReadAttemptAsync(authority, ct);
            if (pending is { Abandoned: true, Committed: false })
            {
                await journal.CompleteAbandonedAttemptAsync(authority, pending.Open.OperationId, pending.Open.AttemptId, ct);
                pending = null;
            }
            // Observe an uncertain original completion before asking for fresh read/write authority.
            // A subsequent revocation cannot turn a lost committed response into a new operation.
            if (pending is { Committed: false, Abandoned: false } && pending.Parts.Count != 0)
            {
                try
                {
                    var observed = await transport.ObserveAttemptAsync(state.SpaceId, state.DeviceId, pending.Open.OperationId, pending.Open.AttemptId, ct);
                    await journal.RecordAttemptRemoteAsync(authority, pending.Open.OperationId, pending.Open.AttemptId, observed.Id, ct);
                    if (observed.CommittedCursor is not null)
                        await journal.RecordAttemptAcknowledgedAsync(authority, pending.Open.OperationId, pending.Open.AttemptId, observed, ct);
                    else if (observed.Abandoned)
                    {
                        await journal.RecordAttemptAbandonedAsync(authority, pending.Open.OperationId, pending.Open.AttemptId, observed.Id, ct);
                        await journal.CompleteAbandonedAttemptAsync(authority, pending.Open.OperationId, pending.Open.AttemptId, ct);
                    }
                }
                catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.NotFound && pending.RemoteId is null)
                { /* No acknowledged staging id exists. Only the same immutable attempt may be opened below. */ }
            }
            await RefreshAsync(transport, authority, state.SpaceId, state.DeviceId, ct);
            if (ownershipReception is { } reception)
            {
                await journal.RequireOwnershipReceptionAsync(authority, reception, ct);
                var status = await original.StatusAsync(ct);
                if (status.LastSuccessAt is null || status.PendingChanges != 0 || status.Conflicts != 0 ||
                    await journal.ReadAttemptAsync(authority, ct) is { Parts.Count: > 0 }) throw Refusal();
            }
            pending = await journal.ReadAttemptAsync(authority, ct);
            if (pending is { Committed: true })
                _ = await journal.TryCompleteAppliedAttemptAsync(authority, pending.Open.OperationId, pending.Open.AttemptId, ct);
            if (ownershipReception is not null && await journal.ReadAttemptAsync(authority, ct) is null)
            {
                var horizon = await journal.CaptureAsync(transport.Peer, identity.Id, ct) ?? throw Refusal();
                if (await original.OwnershipHorizonAppliedAsync(horizon, ct)) return new(true, false);
            }
            var work = await original.CaptureRoundAsync(transport.Peer, identity.Id, ct, ProfileSynchronizationKind.Household);
            if (ownershipReception is not null && (work.Parts.Any(part => part.Rows.Count != 0) || work.Resolutions.Length != 0)) throw Refusal();
            if (work.Authority.Generation != authority.GenerationPath || work.Authority.Profile != state.SpaceId || work.Authority.Device != state.DeviceId) throw Refusal();
            if (work.NeedsReview) return new(false, true);
            if (work.Phase == 1)
            {
                var prepared = await journal.PrepareOriginalAsync(authority, transport.Peer, original, ct, ownershipReception);
                if (prepared.NeedsReview) return new(false, true);
                var attempt = prepared.Attempt ?? throw Refusal();
                if (ownershipReception is not null && attempt.Parts.Count != 0) throw Refusal();
                if (attempt.Open.OperationId != work.Operation || attempt.Cutoff != work.Cutoff || attempt.CaptureHash != work.ContentHash || attempt.Abandoned) throw Refusal();
                if (attempt.Parts.Count != 0 && !attempt.Committed) await journal.DemandLocalFinancialWriteAsync(authority, ct);
                var published = await PublishOrReplanAsync(transport, authority, state.SpaceId, state.DeviceId, attempt, ct);
                if (published.NeedsReview) return new(false, true);
                attempt = await journal.ReadAttemptAsync(authority, ct) ?? throw Refusal();
                if (!attempt.Committed || attempt.RemoteId is not { } receiptId) throw Refusal();
                using var verified = await journal.ReadReplicaAsync(authority, ct);
                if (verified.FinancialCursor < attempt.FinancialCursor) throw Refusal();
                await original.RecordRemoteAsync(work, receiptId, work.Parts.Length, ct);
                await original.RecordCommitAsync(work, new(true, verified.FinancialCursor, []), ct);
                work = work with { Remote = receiptId, Phase = 2, ThroughCursor = verified.FinancialCursor, ReadCursor = work.Cursor };
            }
            if (work.Phase != 2 || work.ThroughCursor is not { } through) throw Refusal();
            using (var verified = await journal.ReadReplicaAsync(authority, ct))
            {
                if (through > verified.FinancialCursor) throw Refusal();
                var cursor = work.ReadCursor;
                while (cursor < through)
                {
                    var page = await verified.FinancialFeedAsync(cursor, through, ct);
                    await original.StageFeedAsync(work, page, ct); cursor = page.NextCursor;
                }
                foreach (var blob in await original.IncomingBlobsAsync(work, ct))
                    foreach (var index in await original.MissingBlobPiecesAsync(work, blob, ct))
                    {
                        var bytes = await verified.ReadAttachmentPieceAsync(blob.Hash, index, ct);
                        try { await original.StageBlobAsync(work, blob, index, bytes, ct); }
                        finally { CryptographicOperations.ZeroMemory(bytes); }
                    }
            }
            var applied = await original.ApplyFeedAsync(work, ct, ownershipReception: ownershipReception);
            if (applied)
            {
                pending = await journal.ReadAttemptAsync(authority, ct) ?? throw Refusal();
                if (pending.Open.OperationId != work.Operation) throw Refusal();
                await journal.CompleteAppliedAttemptAsync(authority, work.Operation, pending.Open.AttemptId, ct);
            }
            return new(applied, !applied);
        }
        finally { running.Release(); }
    }

    /// <summary>Replans only after actual server abandonment; a lost or racing commit retains the original signed ciphertext and cutoff.</summary>
    private async Task<HouseholdPreparationOutcome> PublishOrReplanAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority,
        Guid space, Guid device, HouseholdJournalAttempt attempt, CancellationToken ct)
    {
        for (var retry = 0; retry < 2; retry++)
        {
            if (attempt.Committed) return new(attempt, false);
            if (attempt.Parts.Count == 0)
            {
                await journal.RecordNoOpAsync(authority, attempt.Open.OperationId, attempt.Open.AttemptId, ct);
                return new(await journal.ReadAttemptAsync(authority, ct), false);
            }
            try
            {
                var handle = await transport.OpenAsync(space, attempt.Open, ct);
                await journal.RecordAttemptRemoteAsync(authority, attempt.Open.OperationId, attempt.Open.AttemptId, handle.Id, ct);
                if (handle.Abandoned) throw Refusal();
                if (handle.CommittedCursor is null)
                {
                    var received = new HashSet<int>(); var offset = 0; int? total = null;
                    while (true)
                    {
                        var page = await transport.UploadedPartsAsync(space, handle.Id, device, offset, ct);
                        if (page.RoundId != handle.Id || page.Offset != offset || page.Total < 0 || page.Total > attempt.Parts.Count ||
                            page.Parts is null || page.Parts.Length > 256 || page.Offset + page.Parts.Length > page.Total ||
                            page.Complete != (page.Offset + page.Parts.Length == page.Total) || !page.Complete && page.Parts.Length == 0 ||
                            total is { } expected && expected != page.Total) throw Refusal();
                        total = page.Total;
                        foreach (var part in page.Parts)
                        {
                            if (part is null || part.Index < 0 || part.Index >= attempt.Parts.Count || !received.Add(part.Index)) throw Refusal();
                            var originalPart = attempt.Parts[part.Index];
                            if (part.RecordId != originalPart.RecordId || part.BaseVersion != originalPart.BaseVersion || part.Digest != originalPart.Digest) throw Refusal();
                        }
                        offset += page.Parts.Length; if (page.Complete) break;
                    }
                    // Each skip is backed by the exact immutable original address/version/digest,
                    // including sparse uploads. Neither a count nor highest index is a receipt.
                    for (var first = 0; first < attempt.Parts.Count; first += 8)
                    {
                        var count = Math.Min(8, attempt.Parts.Count - first);
                        if (Enumerable.Range(first, count).All(received.Contains)) continue;
                        var bodies = await journal.ReadAttemptPartsAsync(authority, attempt.Open.OperationId, attempt.Open.AttemptId, first, count, ct);
                        await transport.PutBatchAsync(space, handle.Id, device, bodies.Where(part => !received.Contains(part.Index)).ToArray(), ct);
                    }
                    handle = await transport.CommitAsync(space, handle.Id, attempt.Commit, ct);
                }
                await journal.RecordAttemptAcknowledgedAsync(authority, attempt.Open.OperationId, attempt.Open.AttemptId, handle, ct);
                await RefreshAsync(transport, authority, space, device, ct);
                return new(await journal.ReadAttemptAsync(authority, ct), false);
            }
            catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.Conflict)
            {
                var observed = await transport.AbandonAttemptAsync(space, attempt.Open, ct);
                await journal.RecordAttemptRemoteAsync(authority, attempt.Open.OperationId, attempt.Open.AttemptId, observed.Id, ct);
                if (observed.CommittedCursor is not null)
                {
                    await journal.RecordAttemptAcknowledgedAsync(authority, attempt.Open.OperationId, attempt.Open.AttemptId, observed, ct);
                    await RefreshAsync(transport, authority, space, device, ct);
                    return new(await journal.ReadAttemptAsync(authority, ct), false);
                }
                if (!observed.Abandoned) throw Refusal();
                await journal.RecordAttemptAbandonedAsync(authority, attempt.Open.OperationId, attempt.Open.AttemptId, observed.Id, ct);
                await journal.CompleteAbandonedAttemptAsync(authority, attempt.Open.OperationId, attempt.Open.AttemptId, ct);
                await RefreshAsync(transport, authority, space, device, ct);
                var prepared = await journal.PrepareOriginalAsync(authority, transport.Peer, original, ct);
                if (prepared.NeedsReview) return prepared;
                attempt = prepared.Attempt ?? throw Refusal();
            }
        }
        // A continuously changing peer is an actionable retry, not an unbounded busy loop or
        // permission to discard its last original durably retained attempt.
        throw Refusal();
    }

    /// <summary>Downloads bounded original public proofs/missing grants and individually authenticated ciphertext, accepting only complete financial packets.</summary>
    private async Task RefreshAsync(HouseholdServiceTransport transport, HouseholdJournalAuthority authority, Guid space, Guid device, CancellationToken ct)
    {
        HouseholdCheckpoint checkpoint;
        try { checkpoint = await transport.CheckpointAsync(space, device, ct); }
        catch (PersonalSyncHttpException error) when (error.Status is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            // The authenticated scoped endpoint intentionally hides removed rooms/devices behind 404.
            // Keep the exact local ledger/ciphertext, but never treat that refusal as an offline grant.
            await journal.RecordRemoteAccessRefusalAsync(authority, ct); throw;
        }
        var history = (await journal.ReadHistoryAsync(authority, ct)).ToList();
        if (checkpoint.SpaceId != space || checkpoint.MembershipVersion < history.Count || checkpoint.MembershipVersion > 4096) throw Refusal();
        while (history.Count < checkpoint.MembershipVersion)
        {
            var page = await transport.MembershipAsync(space, device, history.Count, ct);
            if (page.Length is < 1 or > 8) throw Refusal();
            history.AddRange(page.Take((int)(checkpoint.MembershipVersion - history.Count)));
        }
        var membership = await journal.RecordMembershipAsync(authority, history, ct);
        if (membership.Hash != checkpoint.MembershipHead || membership.Epoch != checkpoint.KeyEpoch || membership.Snapshot().OwnerId != checkpoint.OwnerId) throw Refusal();
        foreach (var epoch in await journal.MissingEpochsAsync(authority, ct))
            await journal.RecordEpochGrantAsync(authority, await transport.GrantAsync(space, device, epoch, ct), ct);
        await journal.RecordAccessCheckpointAsync(authority, checkpoint, ct);
        var receipts = new Dictionary<Guid, HouseholdPublicationReceipt>();
        while (true)
        {
            using (var current = await journal.ReadReplicaAsync(authority, ct))
            {
                if (current.Cursor > checkpoint.Cursor) throw Refusal();
                if (current.Cursor == checkpoint.Cursor)
                { if (current.PublicationHead != checkpoint.PublicationHead) throw Refusal(); break; }
            }
            var position = await journal.InboxPositionAsync(authority, ct);
            var page = await transport.FeedAsync(space, device, position, checkpoint.Cursor, ct);
            if (page.ThroughCursor != checkpoint.Cursor || page.Events is null || page.Events.Length is < 1 or > 8 ||
                page.Events.Any(item => item is null || item.Cursor > checkpoint.Cursor)) throw Refusal();
            foreach (var group in page.Events.GroupBy(item => item.Cursor).OrderBy(group => group.Key))
            {
                var first = group.First();
                if (!receipts.TryGetValue(first.RoundId, out var receipt))
                { receipt = await transport.ReceiptAsync(space, device, first.RoundId, ct); receipts.Add(first.RoundId, receipt); }
                await journal.StageInboxAsync(authority, receipt, group.ToArray(), ct);
                _ = await journal.AcceptInboxAsync(authority, ct);
            }
            var next = await journal.InboxPositionAsync(authority, ct);
            if (next.Cursor < position.Cursor || next.Cursor == position.Cursor && string.CompareOrdinal(next.RecordId, position.RecordId) <= 0) throw Refusal();
        }
    }

    /// <summary>No financial values, credentials, ciphertext or private key material enters diagnostics.</summary>
    private static InvalidOperationException Refusal() => new("The original protected household synchronization boundary was refused.");
}
