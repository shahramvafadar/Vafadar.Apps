using System.Security.Cryptography;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>The outcome of one whole personal round; newer local changes deliberately remain queued.</summary>
public sealed record PersonalSyncOutcome(bool Applied, bool NeedsReview);

/// <summary>Connects actual pinned service requests with short local transactions and resumable immutable boundaries.</summary>
public sealed class PersonalSyncCoordinator(PersonalSyncStore store)
{
    private readonly SemaphoreSlim running = new(1, 1);

    /// <summary>Explicitly replaces an uncommitted review with the latest local edits; a lost committed acknowledgement stays recoverable.</summary>
    public async Task<bool> RestartReviewAsync(IProfileSynchronizationTransport transport, CancellationToken ct = default)
    {
        await running.WaitAsync(ct);
        try
        {
            var identity = await transport.IdentityAsync(ct); var work = await store.CaptureRoundAsync(transport.Peer, identity.Id, ct, transport.Kind);
            if (work.Phase == 2)
            {
                // A service acknowledgement is immutable. Recompute only local consent against
                // its complete retained feed; refreshing a review must never write financial rows.
                await store.ApplyFeedAsync(work, ct, refreshReviewOnly: true); return true;
            }
            if (work.Phase != 1) throw new InvalidDataException("The retained personal phase is invalid.");
            var authority = work.Authority;
            var handle = await transport.OpenAsync(authority.Profile, new(authority.Device, work.Operation, SyncSchema.Hash), ct);
            if (handle.Committed)
            { await store.RecordCommitAsync(work, new(true, handle.Cursor, []), ct); return false; }
            try { await transport.AbandonAsync(authority.Profile, authority.Device, handle.Id, ct); }
            catch (PersonalSyncHttpException error) when (error.Status == System.Net.HttpStatusCode.Conflict)
            {
                // Another still-authorized caller can have committed after observation. Never erase its local cutoff.
                handle = await transport.OpenAsync(authority.Profile, new(authority.Device, work.Operation, SyncSchema.Hash), ct);
                if (!handle.Committed) throw;
                await store.RecordCommitAsync(work, new(true, handle.Cursor, []), ct); return false;
            }
            await store.DiscardAbandonedRoundAsync(work, ct); return true;
        }
        finally { running.Release(); }
    }

    /// <summary>Completes one captured round without holding a local financial connection over network waits.</summary>
    public async Task<PersonalSyncOutcome> SynchronizeAsync(IProfileSynchronizationTransport transport, CancellationToken ct = default)
    {
        await running.WaitAsync(ct);
        try
        {
            var identity = await transport.IdentityAsync(ct);
            var work = await store.CaptureRoundAsync(transport.Peer, identity.Id, ct, transport.Kind);
            if (work.NeedsReview)
            { await DownloadReviewBlobsAsync(store, transport, work, ct); return new(false, true); }
            var authority = work.Authority;
            if (work.Phase == 1)
            {
                var handle = await transport.OpenAsync(authority.Profile, new(authority.Device, work.Operation, SyncSchema.Hash), ct);
                if (handle.Id == Guid.Empty || work.Remote is { } previous && previous != handle.Id || handle.Parts < 0 || handle.Parts > work.Parts.Length || handle.Rows < 0)
                    throw new InvalidDataException("The personal staging observation is invalid.");
                if (handle.Committed)
                {
                    await store.RecordRemoteAsync(work, handle.Id, work.Parts.Length, ct);
                    await store.RecordCommitAsync(work, new(true, handle.Cursor, []), ct);
                    work = work with { Remote = handle.Id, Phase = 2, ThroughCursor = handle.Cursor, ReadCursor = work.Cursor };
                }
                else
                {
                    await UploadBlobsAsync(store, transport, work, ct);
                    // Retrying an unacknowledged part is safe because the service compares its complete typed digest.
                    // Do not infer a prefix from a count alone; a client can die before saving its local receipt.
                    await store.RecordRemoteAsync(work, handle.Id, work.SentParts, ct);
                    for (var index = work.SentParts; index < work.Parts.Length; index++)
                    {
                        await transport.PutPartAsync(authority.Profile, authority.Device, handle.Id, work.Parts[index], ct);
                        await store.RecordRemoteAsync(work, handle.Id, index + 1, ct);
                    }
                    var result = await transport.CommitAsync(authority.Profile, authority.Device, handle.Id,
                        new(work.Parts.Length, work.Parts.Sum(part => part.Rows.Count), work.ContentHash, work.Resolutions), ct);
                    await store.RecordCommitAsync(work, result, ct);
                    if (!result.Committed)
                    { await DownloadReviewBlobsAsync(store, transport, work, ct); return new(false, true); }
                    work = work with { Remote = handle.Id, Phase = 2, ThroughCursor = result.Cursor, ReadCursor = work.Cursor };
                }
            }
            if (work.Phase != 2 || work.ThroughCursor is not { } through) throw new InvalidDataException("The retained personal phase is invalid.");
            var cursor = work.ReadCursor;
            while (cursor < through)
            {
                var page = await transport.FeedAsync(authority.Profile, authority.Device, cursor, through, ct);
                await store.StageFeedAsync(work, page, ct); cursor = page.NextCursor;
            }
            foreach (var blob in await store.IncomingBlobsAsync(work, ct))
                foreach (var index in await store.MissingBlobPiecesAsync(work, blob, ct))
                {
                    var bytes = await transport.ReadBlobAsync(authority.Profile, authority.Device, blob.Hash, index, ct);
                    try { await store.StageBlobAsync(work, blob, index, bytes, ct); }
                    finally { CryptographicOperations.ZeroMemory(bytes); }
                }
            var applied = await store.ApplyFeedAsync(work, ct); return new(applied, !applied);
        }
        finally { running.Release(); }
    }

    private static async Task UploadBlobsAsync(PersonalSyncStore store, IProfileSynchronizationTransport transport, SyncRoundWork work, CancellationToken ct)
    {
        var authority = work.Authority;
        var references = PersonalSyncStore.BlobNeeds(work.Parts.SelectMany(part => part.Rows).Select(row => new SyncRecord(row.Record, row.BaseVersion, row.Values)));
        foreach (var blob in references)
        {
            var status = await transport.DeclareBlobAsync(authority.Profile, authority.Device, blob.Hash, blob.Length, ct);
            var count = (int)((blob.Length + 2 * 1024 * 1024 - 1) / (2 * 1024 * 1024));
            if (status.Sha256 != blob.Hash || status.Length != blob.Length || status.Parts != count || status.ReceivedParts is null ||
                status.ReceivedParts.Any(index => index < 0 || index >= count) || status.ReceivedParts.Distinct().Count() != status.ReceivedParts.Count)
                throw new InvalidDataException("The retained personal attachment observation is invalid.");
            foreach (var index in Enumerable.Range(0, count).Except(status.ReceivedParts))
            {
                var bytes = await store.OutgoingPieceAsync(work, blob.Hash, index, ct);
                try { await transport.PutBlobAsync(authority.Profile, authority.Device, blob.Hash, index, bytes, ct); }
                finally { CryptographicOperations.ZeroMemory(bytes); }
            }
            var manifest = await transport.CompleteBlobAsync(authority.Profile, authority.Device, blob.Hash, ct);
            await store.ConfirmBlobAsync(work, manifest, ct);
        }
    }

    private static async Task DownloadReviewBlobsAsync(PersonalSyncStore store, IProfileSynchronizationTransport transport, SyncRoundWork work, CancellationToken ct)
    {
        foreach (var blob in await store.ReviewBlobsAsync(work, ct))
            foreach (var index in await store.MissingBlobPiecesAsync(work, blob, ct))
            {
                var bytes = await transport.ReadBlobAsync(work.Authority.Profile, work.Authority.Device, blob.Hash, index, ct);
                try { await store.StageBlobAsync(work, blob, index, bytes, ct); }
                finally { CryptographicOperations.ZeroMemory(bytes); }
            }
    }
}
