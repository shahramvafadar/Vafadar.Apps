using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Serial PostgreSQL opaque publication. The service never decrypts, merges or guesses household financial content.</summary>
internal sealed class HouseholdRoundStore(ServiceDbContext db)
{
    /// <summary>Creates one immutable attempt or returns its exact original result after a lost transport acknowledgement.</summary>
    internal async Task<HouseholdRoundRow> OpenAsync(HouseholdRequestScope scope, HouseholdOpenRound input, CancellationToken ct)
    {
        _ = await scope.RequireAsync(input.DeviceId, HouseholdRemoteOperation.WriteFinancial, ct);
        if (input.OperationId == Guid.Empty || input.AttemptId == Guid.Empty || input.ExpectedCursor < 0 ||
            !HouseholdProtectionContext.IsFingerprint(input.SchemaHash) || input.SchemaHash != scope.Space.SchemaHash)
            throw Refuse(400, "InvalidHouseholdPublication");
        var prior = await db.Set<HouseholdRoundRow>().SingleOrDefaultAsync(row => row.SpaceId == scope.Space.Id &&
            row.DeviceId == input.DeviceId && row.OperationId == input.OperationId && row.AttemptId == input.AttemptId, ct);
        if (prior is not null)
        {
            if (prior.ExpectedCursor != input.ExpectedCursor || prior.MembershipVersion != input.MembershipVersion || prior.KeyEpoch != input.KeyEpoch)
                throw Refuse(409, "AlteredHouseholdAttempt");
            if (prior.Abandoned) throw Refuse(410, "AbandonedHouseholdAttempt");
            prior.LastSeenAt = scope.Now; return prior;
        }
        if (await db.Set<HouseholdRoundRow>().AnyAsync(row => row.SpaceId == scope.Space.Id && row.DeviceId == input.DeviceId &&
            row.OperationId == input.OperationId && row.CommittedCursor != null, ct))
            throw Refuse(409, "HouseholdOperationAlreadyCommitted");
        RequireCurrent(scope, input.ExpectedCursor, input.MembershipVersion, input.KeyEpoch);
        // Every participant holds the same room lock: independent editor sessions cannot race a pending-work bound.
        if (await db.Set<HouseholdRoundRow>().CountAsync(row => row.SpaceId == scope.Space.Id &&
            row.CommittedCursor == null && !row.Abandoned, ct) >= 16)
            throw Refuse(413, "HouseholdPendingBound");
        var result = new HouseholdRoundRow { Id = Guid.NewGuid(), SpaceId = scope.Space.Id, DeviceId = input.DeviceId,
            OperationId = input.OperationId, AttemptId = input.AttemptId, ExpectedCursor = input.ExpectedCursor,
            MembershipVersion = input.MembershipVersion, KeyEpoch = input.KeyEpoch, CreatedAt = scope.Now, LastSeenAt = scope.Now };
        db.Add(result); return result;
    }

    /// <summary>Atomically tombstones an unpublished exact attempt, including one whose open lost a CAS race before staging existed; a late old request cannot resurrect it.</summary>
    internal async Task<HouseholdRoundHandle> AbandonAttemptAsync(HouseholdRequestScope scope, HouseholdOpenRound input, CancellationToken ct)
    {
        _ = await scope.RequireAsync(input.DeviceId, HouseholdRemoteOperation.WriteFinancial, ct);
        if (input.OperationId == Guid.Empty || input.AttemptId == Guid.Empty || input.ExpectedCursor < 0 || input.MembershipVersion < 1 ||
            input.KeyEpoch < 1 || input.SchemaHash != scope.Space.SchemaHash) throw Refuse(400, "InvalidHouseholdPublication");
        var row = await db.Set<HouseholdRoundRow>().SingleOrDefaultAsync(value => value.SpaceId == scope.Space.Id && value.DeviceId == input.DeviceId &&
            value.OperationId == input.OperationId && value.AttemptId == input.AttemptId, ct);
        if (row is null)
        {
            if (await db.Set<HouseholdRoundRow>().CountAsync(value => value.SpaceId == scope.Space.Id && value.CommittedCursor == null, ct) >= 4096)
                throw Refuse(413, "HouseholdRetainedAttemptBound");
            if (await db.Set<HouseholdRoundRow>().AnyAsync(value => value.SpaceId == scope.Space.Id && value.DeviceId == input.DeviceId &&
                value.OperationId == input.OperationId && value.CommittedCursor != null, ct)) throw Refuse(409, "HouseholdOperationAlreadyCommitted");
            row = new HouseholdRoundRow { Id = Guid.NewGuid(), SpaceId = scope.Space.Id, DeviceId = input.DeviceId,
                OperationId = input.OperationId, AttemptId = input.AttemptId, ExpectedCursor = input.ExpectedCursor,
                MembershipVersion = input.MembershipVersion, KeyEpoch = input.KeyEpoch, CreatedAt = scope.Now, LastSeenAt = scope.Now };
            db.Add(row);
        }
        if (row.ExpectedCursor != input.ExpectedCursor || row.MembershipVersion != input.MembershipVersion || row.KeyEpoch != input.KeyEpoch)
            throw Refuse(409, "AlteredHouseholdAttempt");
        if (row.CommittedCursor is not null) return await HandleAsync(row, ct);
        row.Abandoned = true; row.LastSeenAt = scope.Now;
        await db.Set<HouseholdPartRow>().Where(part => part.RoundId == row.Id).ExecuteDeleteAsync(ct);
        return await HandleAsync(row, ct);
    }

    /// <summary>Persists exact immutable parts. Neither an incomplete transaction nor a newer retry changes live records.</summary>
    internal async Task PutAsync(HouseholdRequestScope scope, HouseholdRoundRow round, HouseholdOpaquePart input, CancellationToken ct)
    {
        _ = await RequireRoundAsync(scope, round, HouseholdRemoteOperation.WriteFinancial, ct);
        if (input.Index is < 0 or >= HouseholdPublicationDigest.MaximumParts || input.BaseVersion < 0 ||
            !HouseholdProtectionContext.IsFingerprint(input.RecordId) || !HouseholdProtectionContext.IsFingerprint(input.Digest))
            throw Refuse(400, "InvalidHouseholdPart");
        string digest;
        try { digest = HouseholdPublicationDigest.Ciphertext(input.Ciphertext); }
        catch (ArgumentException) { throw Refuse(413, "HouseholdCiphertextBound"); }
        if (digest != input.Digest) throw Refuse(400, "HouseholdCiphertextDigestMismatch");
        var prior = await db.Set<HouseholdPartRow>().SingleOrDefaultAsync(row => row.RoundId == round.Id && row.Index == input.Index, ct);
        if (prior is not null)
        {
            if (prior.RecordId != input.RecordId || prior.BaseVersion != input.BaseVersion || prior.Digest != input.Digest || prior.Ciphertext != input.Ciphertext)
                throw Refuse(409, "AlteredHouseholdPart");
            return;
        }
        if (round.CommittedCursor is not null) throw Refuse(409, "CommittedHouseholdAttempt");
        RequireCurrent(scope, round.ExpectedCursor, round.MembershipVersion, round.KeyEpoch);
        if (await db.Set<HouseholdPartRow>().AnyAsync(row => row.RoundId == round.Id && row.RecordId == input.RecordId, ct))
            throw Refuse(409, "DuplicateHouseholdRecord");
        var pendingBytes = await (from part in db.Set<HouseholdPartRow>() join pending in db.Set<HouseholdRoundRow>() on part.RoundId equals pending.Id
            where pending.SpaceId == scope.Space.Id && pending.CommittedCursor == null && !pending.Abandoned
            select (long)part.Bytes).SumAsync(ct);
        if (pendingBytes + input.Ciphertext.Length > HouseholdPublicationDigest.MaximumBytes)
            throw Refuse(413, "HouseholdStagingBound");
        db.Add(new HouseholdPartRow { RoundId = round.Id, Index = input.Index, RecordId = input.RecordId,
            BaseVersion = input.BaseVersion, Ciphertext = input.Ciphertext, Digest = digest, Bytes = input.Ciphertext.Length });
        round.LastSeenAt = scope.Now;
    }

    /// <summary>Returns only this actual session's immutable original ciphertext metadata, including sparse uploads; no count alone proves a prefix.</summary>
    internal async Task<HouseholdUploadedParts> UploadedPartsAsync(HouseholdRequestScope scope, HouseholdRoundRow round, Guid device, int offset, CancellationToken ct)
    {
        await scope.RequireOwnAttemptObservationAsync(device, ct);
        if (round.DeviceId != device || round.SpaceId != scope.Space.Id || offset < 0) throw Refuse(404, "HouseholdResourceUnavailable");
        var total = await db.Set<HouseholdPartRow>().CountAsync(row => row.RoundId == round.Id, ct);
        if (offset > total || total > HouseholdPublicationDigest.MaximumParts) throw Refuse(400, "InvalidHouseholdManifestPosition");
        var parts = await db.Set<HouseholdPartRow>().AsNoTracking().Where(row => row.RoundId == round.Id).OrderBy(row => row.Index).Skip(offset).Take(256)
            .Select(row => new HouseholdPartCommitment(row.Index, row.RecordId, row.BaseVersion, row.Digest)).ToArrayAsync(ct);
        return new(round.Id, offset, total, parts, offset + parts.Length == total);
    }

    /// <summary>Stages a small exact ciphertext batch under one actual room transaction, without weakening the request, total staging or per-part bounds.</summary>
    internal async Task PutBatchAsync(HouseholdRequestScope scope, HouseholdRoundRow round, HouseholdUploadParts input, CancellationToken ct)
    {
        _ = await RequireRoundAsync(scope, round, HouseholdRemoteOperation.WriteFinancial, ct);
        if (input.DeviceId != round.DeviceId || input.Parts is null || input.Parts.Length is < 1 or > 8 || input.Parts.Any(part => part is null || part.Ciphertext is null) ||
            input.Parts.Select(part => part.Index).Distinct().Count() != input.Parts.Length || input.Parts.Select(part => part.RecordId).Distinct().Count() != input.Parts.Length)
            throw Refuse(400, "InvalidHouseholdPart");
        if (input.Parts.Sum(part => (long)part.Ciphertext.Length) > 7L * 1024 * 1024) throw Refuse(413, "HouseholdCiphertextBound");
        var pending = await (from part in db.Set<HouseholdPartRow>() join candidate in db.Set<HouseholdRoundRow>() on part.RoundId equals candidate.Id
            where candidate.SpaceId == scope.Space.Id && candidate.CommittedCursor == null && !candidate.Abandoned select (long)part.Bytes).SumAsync(ct);
        var indices = input.Parts.Select(part => part.Index).ToArray();
        var stored = await db.Set<HouseholdPartRow>().Where(part => part.RoundId == round.Id && indices.Contains(part.Index)).Select(part => part.Index).ToArrayAsync(ct);
        if (pending + input.Parts.Where(part => !stored.Contains(part.Index)).Sum(part => (long)part.Ciphertext.Length) > HouseholdPublicationDigest.MaximumBytes)
            throw Refuse(413, "HouseholdStagingBound");
        foreach (var part in input.Parts) await PutAsync(scope, round, part, ct);
    }

    /// <summary>Verifies actual author proof and complete ciphertext manifest, then publishes every record/feed row under the one room transaction.</summary>
    internal async Task<HouseholdRoundHandle> CommitAsync(HouseholdRequestScope scope, HouseholdRoundRow round,
        HouseholdCommitRound input, CancellationToken ct)
    {
        var device = await RequireRoundAsync(scope, round, HouseholdRemoteOperation.WriteFinancial, ct);
        if (input.DeviceId != round.DeviceId) throw Refuse(404, "HouseholdResourceUnavailable");
        var proof = HouseholdMetadataProofs.Read(input.SignedCommit,
            HouseholdMetadataProofs.Public(new(device.SigningKey, device.WrappingKey)), HouseholdMetadataProofs.CommitType,
            HouseholdMetadataJsonContext.Default.HouseholdCommitStatement);
        if (proof.Format != 1 || proof.SpaceId != scope.Space.Id || proof.DeviceId != device.Id || proof.OperationId != round.OperationId ||
            proof.AttemptId != round.AttemptId || proof.ExpectedCursor != round.ExpectedCursor || round.ExpectedCursor == long.MaxValue ||
            proof.Cursor != round.ExpectedCursor + 1 || proof.MembershipVersion != round.MembershipVersion || proof.KeyEpoch != round.KeyEpoch ||
            !HouseholdProtectionContext.IsFingerprint(proof.PreviousHead) || !HouseholdProtectionContext.IsFingerprint(proof.ContentHash) ||
            proof.Parts is < 1 or > HouseholdPublicationDigest.MaximumParts)
            throw Refuse(400, "InvalidHouseholdCommit");
        if (round.CommittedCursor is not null)
        {
            if (round.SignedCommit != input.SignedCommit) throw Refuse(409, "AlteredHouseholdCompletion");
            return await HandleAsync(round, ct);
        }
        RequireCurrent(scope, round.ExpectedCursor, round.MembershipVersion, round.KeyEpoch);
        if (proof.PreviousHead != scope.Space.PublicationHead) throw Refuse(409, "HouseholdPublicationHeadMismatch");
        if (await db.Set<HouseholdRoundRow>().AnyAsync(row => row.SpaceId == scope.Space.Id && row.DeviceId == device.Id &&
            row.OperationId == round.OperationId && row.CommittedCursor != null, ct))
            throw Refuse(409, "HouseholdOperationAlreadyCommitted");
        var parts = await db.Set<HouseholdPartRow>().AsNoTracking().Where(row => row.RoundId == round.Id).OrderBy(row => row.Index).ToArrayAsync(ct);
        if (parts.Length != proof.Parts || parts.Sum(row => (long)row.Bytes) > HouseholdPublicationDigest.MaximumBytes)
            throw Refuse(409, "IncompleteHouseholdPublication");
        string manifest;
        try { manifest = HouseholdPublicationDigest.Manifest(parts.Select(row => new HouseholdPartCommitment(row.Index, row.RecordId, row.BaseVersion, row.Digest)).ToArray()); }
        catch (ArgumentException) { throw Refuse(409, "IncompleteHouseholdPublication"); }
        if (manifest != proof.ContentHash || parts.Any(row => HouseholdPublicationDigest.Ciphertext(row.Ciphertext) != row.Digest))
            throw Refuse(409, "HouseholdManifestMismatch");
        var ids = parts.Select(row => row.RecordId).ToArray();
        var existing = await db.Set<HouseholdRecordRow>().Where(row => row.SpaceId == scope.Space.Id && ids.Contains(row.RecordId)).ToDictionaryAsync(row => row.RecordId, ct);
        if (parts.Any(part => part.BaseVersion != (existing.GetValueOrDefault(part.RecordId)?.Version ?? 0)))
            throw Refuse(409, "HouseholdRecordPredecessorMismatch");
        var liveBytes = await db.Set<HouseholdRecordRow>().Where(row => row.SpaceId == scope.Space.Id).SumAsync(row => (long)row.Bytes, ct);
        var projectedBytes = liveBytes - existing.Values.Sum(row => (long)row.Bytes) + parts.Sum(row => (long)row.Bytes);
        var historyBytes = await db.Set<HouseholdEventRow>().Where(row => row.SpaceId == scope.Space.Id).SumAsync(row => (long)row.Bytes, ct);
        if (projectedBytes > HouseholdPublicationDigest.MaximumBytes || historyBytes + parts.Sum(row => (long)row.Bytes) > HouseholdPublicationDigest.MaximumBytes * 4)
            throw Refuse(413, "HouseholdStoredContentBound");
        foreach (var part in parts)
        {
            if (!existing.TryGetValue(part.RecordId, out var record))
            { record = new() { SpaceId = scope.Space.Id, RecordId = part.RecordId }; db.Add(record); }
            record.Version = proof.Cursor; record.KeyEpoch = round.KeyEpoch; record.MembershipVersion = round.MembershipVersion;
            record.DeviceId = device.Id; record.OperationId = round.OperationId; record.Ciphertext = part.Ciphertext; record.Digest = part.Digest; record.Bytes = part.Bytes;
            db.Add(new HouseholdEventRow { SpaceId = scope.Space.Id, Cursor = proof.Cursor, RecordId = part.RecordId,
                RoundId = round.Id, Index = part.Index, BaseVersion = part.BaseVersion, Ciphertext = part.Ciphertext, Digest = part.Digest, Bytes = part.Bytes });
        }
        round.CommittedCursor = proof.Cursor; round.SignedCommit = input.SignedCommit; round.ContentHash = manifest; round.LastSeenAt = scope.Now;
        scope.Space.Cursor = proof.Cursor;
        scope.Space.PublicationHead = HouseholdMetadataProofs.Digest(input.SignedCommit, HouseholdLimits.MembershipBytes * 2);
        await new HouseholdAuditStore(db).AppendAsync(scope, HouseholdAuditAction.EncryptedUpdate, device.Id, round.OperationId,
            new(SignedPublication: input.SignedCommit), ct);
        // Caller persists once with authority observations, room cursor, live records and immutable feed rows.
        return await HandleAsync(round, ct);
    }

    /// <summary>Reads an exact finite ciphertext prefix frozen at the caller's declared checkpoint; never silently skips records within a cursor.</summary>
    internal async Task<HouseholdFeedPage> FeedAsync(HouseholdRequestScope scope, Guid device, HouseholdFeedPosition after,
        long through, CancellationToken ct)
    {
        _ = await scope.RequireAsync(device, HouseholdRemoteOperation.Read, ct);
        if (after.Cursor < 0 || through < after.Cursor || through > scope.Space.Cursor || after.RecordId is null ||
            (after.RecordId.Length != 0 && !HouseholdProtectionContext.IsFingerprint(after.RecordId)))
            throw Refuse(400, "InvalidHouseholdFeedPosition");
        var rows = await (from entry in db.Set<HouseholdEventRow>().AsNoTracking()
            join round in db.Set<HouseholdRoundRow>().AsNoTracking() on entry.RoundId equals round.Id
            where entry.SpaceId == scope.Space.Id && entry.Cursor <= through &&
                (entry.Cursor > after.Cursor || entry.Cursor == after.Cursor && string.Compare(entry.RecordId, after.RecordId) > 0)
            orderby entry.Cursor, entry.RecordId
            select new { Entry = entry, Round = round }).Take(9).ToArrayAsync(ct);
        var result = new List<HouseholdOpaqueEvent>(); long bytes = 0;
        foreach (var item in rows.Take(8))
        {
            if (result.Count > 0 && bytes + item.Entry.Bytes > 2 * 1024 * 1024) break;
            bytes += item.Entry.Bytes;
            result.Add(new(item.Entry.Cursor, item.Round.Id, item.Entry.RecordId, item.Round.KeyEpoch, item.Round.MembershipVersion,
                item.Round.DeviceId, item.Round.OperationId, item.Round.AttemptId, item.Entry.Index, item.Entry.BaseVersion, item.Entry.Ciphertext, item.Entry.Digest));
        }
        var complete = result.Count == rows.Length;
        var position = complete ? new HouseholdFeedPosition(through, new string('f', 64)) : new(result[^1].Cursor, result[^1].RecordId);
        return new(through, result.ToArray(), position, complete);
    }

    /// <summary>Returns full original signed public commitment for any accepted member, never just an unauthenticated row count.</summary>
    internal async Task<HouseholdPublicationReceipt> ReceiptAsync(HouseholdRequestScope scope, Guid device, Guid roundId, CancellationToken ct)
    {
        _ = await scope.RequireAsync(device, HouseholdRemoteOperation.Read, ct);
        var round = await db.Set<HouseholdRoundRow>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == roundId && row.SpaceId == scope.Space.Id && row.CommittedCursor != null, ct)
            ?? throw Refuse(404, "HouseholdResourceUnavailable");
        var parts = await db.Set<HouseholdPartRow>().AsNoTracking().Where(row => row.RoundId == round.Id).OrderBy(row => row.Index)
            .Select(row => new HouseholdPartCommitment(row.Index, row.RecordId, row.BaseVersion, row.Digest)).ToArrayAsync(ct);
        return new(round.Id, round.SignedCommit!, parts);
    }

    /// <summary>Returns only a bounded exact manifest prefix; its final authority remains the complete original author-signed manifest hash.</summary>
    internal async Task<HouseholdReceiptPage> ReceiptPageAsync(HouseholdRequestScope scope, Guid device, Guid roundId, int offset, CancellationToken ct)
    {
        _ = await scope.RequireAsync(device, HouseholdRemoteOperation.Read, ct);
        var round = await db.Set<HouseholdRoundRow>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == roundId && row.SpaceId == scope.Space.Id && row.CommittedCursor != null, ct)
            ?? throw Refuse(404, "HouseholdResourceUnavailable");
        var total = await db.Set<HouseholdPartRow>().CountAsync(row => row.RoundId == roundId, ct);
        if (offset < 0 || offset >= total || total > HouseholdPublicationDigest.MaximumParts) throw Refuse(400, "InvalidHouseholdManifestPosition");
        var parts = await db.Set<HouseholdPartRow>().AsNoTracking().Where(row => row.RoundId == roundId && row.Index >= offset).OrderBy(row => row.Index).Take(256)
            .Select(row => new HouseholdPartCommitment(row.Index, row.RecordId, row.BaseVersion, row.Digest)).ToArrayAsync(ct);
        return new(roundId, round.SignedCommit!, offset, total, parts, offset + parts.Length == total);
    }

    /// <summary>Explicitly abandons only the actual author's unpublished attempt; a successful publication cannot be undone through staging.</summary>
    internal async Task AbandonAsync(HouseholdRequestScope scope, HouseholdRoundRow round, CancellationToken ct)
    {
        _ = await RequireRoundAsync(scope, round, HouseholdRemoteOperation.WriteFinancial, ct);
        if (round.CommittedCursor is not null) throw Refuse(409, "CommittedHouseholdAttempt");
        round.Abandoned = true; round.LastSeenAt = scope.Now;
        await db.Set<HouseholdPartRow>().Where(row => row.RoundId == round.Id).ExecuteDeleteAsync(ct);
    }

    /// <summary>Exact retained result, independent of whether HTTP observed the original successful commit.</summary>
    internal async Task<HouseholdRoundHandle> HandleAsync(HouseholdRoundRow round, CancellationToken ct)
    {
        var parts = await db.Set<HouseholdPartRow>().Where(row => row.RoundId == round.Id).GroupBy(row => row.RoundId)
            .Select(group => new { Count = group.Count(), Bytes = group.Sum(row => (long)row.Bytes) }).SingleOrDefaultAsync(ct);
        return new(round.Id, parts?.Count ?? 0, parts?.Bytes ?? 0, round.CommittedCursor, round.Abandoned, round.Acknowledged, round.SignedCommit);
    }

    /// <summary>Resolves current real session/device authority again for every staged/committed resource operation.</summary>
    private async Task<HouseholdDeviceRow> RequireRoundAsync(HouseholdRequestScope scope, HouseholdRoundRow round,
        HouseholdRemoteOperation operation, CancellationToken ct)
    {
        if (round.SpaceId != scope.Space.Id || round.Abandoned) throw Refuse(404, "HouseholdResourceUnavailable");
        return await scope.RequireAsync(round.DeviceId, operation, ct);
    }

    /// <summary>Financial writes never pass an observed predecessor, signed membership or epoch mismatch.</summary>
    private static void RequireCurrent(HouseholdRequestScope scope, long cursor, long membership, long epoch)
    {
        if (cursor != scope.Space.Cursor || membership != scope.Space.MembershipVersion || epoch != scope.Space.KeyEpoch)
            throw Refuse(409, "HouseholdCheckpointChanged");
    }

    /// <summary>Content-free refusal; no encrypted content, private state or native error enters the response.</summary>
    private static HouseholdProtocolRefusal Refuse(int status, string code) => new(status, code);
}
