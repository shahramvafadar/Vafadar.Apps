using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Api.Sync;

/// <summary>A content-free protocol refusal; financial input and native exception details never enter its message.</summary>
internal sealed class SyncProtocolRefusal(int status, string code) : Exception(code)
{
    /// <summary>Bounded HTTP outcome.</summary>
    internal int Status { get; } = status;
}

/// <summary>Actual PostgreSQL staging/history and atomic publication under the caller's owner transaction.</summary>
internal sealed class SyncRoundStore(ServiceDbContext db, IDataProtectionProvider protection, ServiceStorage storage)
{
    /// <summary>Bounds one immutable part independently of Kestrel's larger general request limit.</summary>
    internal const int MaximumPartBytes = 1024 * 1024;
    /// <summary>Bounds all staged parts and feed response cardinality.</summary>
    internal const int MaximumPartRows = 128;
    private const int MaximumParts = 16384;

    /// <summary>Returns the same operation after lost acknowledgement; changed schema or abandoned work is refused.</summary>
    internal async Task<SyncRoundRow> OpenAsync(SyncRequestScope scope, SyncSpaceRow space, SyncOpenRound input, CancellationToken ct)
    {
        if (input.OperationId == Guid.Empty || input.SchemaHash != SyncSchema.Hash || space.SchemaHash != SyncSchema.Hash)
            throw new SyncProtocolRefusal(409, "SchemaOrOperation");
        RequireAuthority(scope, space, input.DeviceId);
        var round = await db.Set<SyncRoundRow>().SingleOrDefaultAsync(row => row.SpaceId == space.Id && row.DeviceId == input.DeviceId && row.OperationId == input.OperationId, ct);
        if (round is not null)
        {
            if (round.Abandoned) throw new SyncProtocolRefusal(410, "AbandonedOperation");
            if (round.SchemaHash != input.SchemaHash) throw new SyncProtocolRefusal(409, "AlteredOperation");
            return round;
        }
        if (await db.Set<SyncRoundRow>().CountAsync(row => row.SpaceId == space.Id && !row.Committed && !row.Abandoned, ct) >= 16)
            throw new SyncProtocolRefusal(413, "PendingRoundBound");
        if (await (from pending in db.Set<SyncRoundRow>() join owned in db.Set<SyncSpaceRow>() on pending.SpaceId equals owned.Id
            where owned.UserId == scope.Owner && !pending.Committed && !pending.Abandoned select pending.Id).CountAsync(ct) >= storage.SyncLimits.PendingRoundsPerOwner)
            throw new SyncProtocolRefusal(413, "OwnerPendingRoundBound");
        round = new() { Id = Guid.NewGuid(), SpaceId = space.Id, DeviceId = input.DeviceId, OperationId = input.OperationId,
            SchemaHash = input.SchemaHash, CreatedAt = scope.Now };
        db.Add(round); return round;
    }

    /// <summary>Persists only complete immutable bounded parts; no proposal is yet a readable financial record.</summary>
    internal async Task PutPartAsync(SyncRequestScope scope, SyncSpaceRow space, SyncRoundRow round, SyncRoundPart input, CancellationToken ct)
    {
        RequireRound(scope, space, round);
        if (input.Index is < 0 or >= MaximumParts || input.Rows is null || input.Rows.Count is < 1 or > MaximumPartRows)
            throw new SyncProtocolRefusal(400, "InvalidPart");
        foreach (var proposal in input.Rows) ValidateProposal(proposal);
        var serialized = JsonSerializer.Serialize(input, PersonalSyncJsonContext.Default.SyncRoundPart);
        var bytes = Encoding.UTF8.GetByteCount(serialized);
        if (bytes > MaximumPartBytes) throw new SyncProtocolRefusal(413, "PartByteBound");
        var hash = SyncProtocolDigest.Part(input);
        var existing = await db.Set<SyncPartRow>().SingleOrDefaultAsync(row => row.RoundId == round.Id && row.Index == input.Index, ct);
        if (existing is not null)
        {
            if (existing.Hash != hash) throw new SyncProtocolRefusal(409, "AlteredPart");
            return;
        }
        if (round.Committed) throw new SyncProtocolRefusal(409, "CommittedOperation");
        var sizes = await db.Set<SyncPartRow>().Where(row => row.RoundId == round.Id)
            .GroupBy(row => row.RoundId).Select(group => new { Bytes = group.Sum(row => (long)row.Bytes), Rows = group.Sum(row => (long)row.Rows) }).SingleOrDefaultAsync(ct);
        if ((sizes?.Bytes ?? 0) + bytes > SyncGraphValidation.MaximumProfileBytes || (sizes?.Rows ?? 0) + input.Rows.Count > SyncGraphValidation.MaximumRows)
            throw new SyncProtocolRefusal(413, "RoundBound");
        // All devices already share the actual owner transaction lock. Another round or space cannot race this budget.
        var staged = from part in db.Set<SyncPartRow>() join pending in db.Set<SyncRoundRow>() on part.RoundId equals pending.Id
            join owned in db.Set<SyncSpaceRow>() on pending.SpaceId equals owned.Id
            where owned.UserId == scope.Owner && !pending.Committed && !pending.Abandoned
            select new { pending.SpaceId, part.Bytes };
        if (await staged.Where(part => part.SpaceId == space.Id).SumAsync(part => (long)part.Bytes, ct) + bytes > SyncGraphValidation.MaximumProfileBytes ||
            await staged.SumAsync(part => (long)part.Bytes, ct) + bytes > storage.SyncLimits.StagedBytesPerOwner)
            throw new SyncProtocolRefusal(413, "OwnerStagingBound");
        db.Add(new SyncPartRow { RoundId = round.Id, Index = input.Index, Bytes = bytes, Rows = input.Rows.Count, Hash = hash,
            ProtectedBody = PartProtector(scope, space.Id, round.Id, input.Index).Protect(serialized) });
    }

    /// <summary>Authentic baselines, complete graph checks and all row/events share the caller's single transaction.</summary>
    internal async Task<SyncCommitResult> CommitAsync(SyncRequestScope scope, SyncSpaceRow space, SyncRoundRow round, SyncCommitRound input, CancellationToken ct)
    {
        RequireRound(scope, space, round);
        if (input.Parts is < 0 or > MaximumParts || input.Rows is < 0 or > SyncGraphValidation.MaximumRows ||
            !SyncProtocolDigest.IsDigest(input.ContentHash) || input.Resolutions is null || input.Resolutions.Count > SyncGraphValidation.MaximumRows)
            throw new SyncProtocolRefusal(400, "InvalidCompletion");
        var completionHash = SyncProtocolDigest.Completion(input);
        if (round.Committed)
        {
            if (round.ContentHash != input.ContentHash || round.CompletionHash != completionHash)
                throw new SyncProtocolRefusal(409, "AlteredCompletion");
            return new(true, round.CommitCursor, []);
        }
        var parts = await db.Set<SyncPartRow>().AsNoTracking().Where(row => row.RoundId == round.Id).OrderBy(row => row.Index).ToListAsync(ct);
        if (parts.Count != input.Parts || parts.Where((part, index) => part.Index != index).Any() ||
            parts.Sum(part => (long)part.Rows) != input.Rows || SyncProtocolDigest.Round(parts.Select(part => part.Hash)) != input.ContentHash)
            throw new SyncProtocolRefusal(409, "IncompleteRound");
        var proposals = new Dictionary<SyncRecordKey, SyncProposal>();
        foreach (var part in parts)
        {
            var body = PartProtector(scope, space.Id, round.Id, part.Index).Unprotect(part.ProtectedBody);
            var decoded = JsonSerializer.Deserialize(body, PersonalSyncJsonContext.Default.SyncRoundPart)
                ?? throw new SyncProtocolRefusal(503, "StagingUnavailable");
            if (decoded.Index != part.Index || decoded.Rows.Count != part.Rows || SyncProtocolDigest.Part(decoded) != part.Hash)
                throw new SyncProtocolRefusal(503, "StagingUnavailable");
            foreach (var proposal in decoded.Rows)
            {
                ValidateProposal(proposal);
                if (!proposals.TryAdd(proposal.Record, proposal)) throw new SyncProtocolRefusal(400, "DuplicateProposedIdentity");
            }
        }
        var resolutions = new Dictionary<SyncRecordKey, SyncResolution>();
        foreach (var resolution in input.Resolutions)
            if (resolution is null || resolution.Record is null || !proposals.ContainsKey(resolution.Record) ||
                resolution.ReviewedVersion < 0 || resolution.RelatedVersion < 0 ||
                (resolution.RelatedRecord is null) != (resolution.RelatedVersion is null) ||
                resolution.RelatedRecord is { } relatedKey && !ValidReviewKey(relatedKey) ||
                resolution.ContextVersions is { } contexts && (contexts.Count > 128 || contexts.Any(item => item is null || item.Record is null || item.Version < 0 || !ValidReviewKey(item.Record)) ||
                    contexts.Select(item => item.Record).Distinct().Count() != contexts.Count) ||
                !resolutions.TryAdd(resolution.Record, resolution))
                throw new SyncProtocolRefusal(400, "InvalidReview");
        var stored = await db.Set<SyncRecordRow>().Where(row => row.SpaceId == space.Id).ToListAsync(ct);
        if (stored.Count > SyncGraphValidation.MaximumRows) throw new SyncProtocolRefusal(413, "ProfileRowBound");
        var current = stored.ToDictionary(row => new SyncRecordKey(row.TableName, row.RecordKey), row => Decode(scope, row));
        var needed = proposals.Values.Where(proposal => proposal.BaseVersion > 0).Select(proposal => proposal.BaseVersion).Distinct().ToArray();
        var history = await db.Set<SyncEventRow>().AsNoTracking().Where(row => row.SpaceId == space.Id && needed.Contains(row.Sequence)).ToDictionaryAsync(row => row.Sequence, ct);
        var baselines = new Dictionary<SyncRecordKey, SyncRecord>();
        foreach (var proposal in proposals.Values)
            if (proposal.BaseVersion > 0 && history.TryGetValue(proposal.BaseVersion, out var prior) &&
                prior.TableName == proposal.Record.Table && prior.RecordKey == proposal.Record.Key)
                baselines.Add(proposal.Record, Decode(scope, prior));
        SyncPublicationPlan plan;
        try { plan = SyncPublicationPlanner.Plan(current, proposals, baselines, resolutions, space.Cursor, space.Initializing); }
        catch (SyncPublicationException error)
        {
            throw error.Failure == SyncPublicationFailure.ReviewBound
                ? new SyncProtocolRefusal(413, "ReviewContextBound")
                : new SyncProtocolRefusal(503, "StoredFinancialGraphUnavailable");
        }
        if (!plan.Succeeded) return new(false, space.Cursor, plan.Conflicts);
        var projected = plan.Projected;
        await RequireVerifiedBlobsAsync(scope, space, current, projected.Values, ct);
        var storedByKey = stored.ToDictionary(row => new SyncRecordKey(row.TableName, row.RecordKey));
        foreach (var (key, selected) in projected.OrderBy(row => row.Key.Table, StringComparer.Ordinal).ThenBy(row => row.Key.Key, StringComparer.Ordinal))
        {
            current.TryGetValue(key, out var before);
            if (Same(before?.Values, selected.Values)) continue;
            space.Cursor = checked(space.Cursor + 1);
            var encrypted = selected.Values is { } value ? RecordProtector(scope, space.Id, key, space.Cursor).Protect(value.GetRawText()) : null;
            if (!storedByKey.TryGetValue(key, out var target))
            { target = new() { SpaceId = space.Id, TableName = key.Table, RecordKey = key.Key }; db.Add(target); }
            target.Version = space.Cursor; target.ProtectedBody = encrypted;
            db.Add(new SyncEventRow { SpaceId = space.Id, Sequence = space.Cursor, TableName = key.Table, RecordKey = key.Key, ProtectedBody = encrypted });
        }
        space.Initializing = false; round.Committed = true; round.CommitCursor = space.Cursor;
        round.ContentHash = input.ContentHash; round.CompletionHash = completionHash;
        // Retain immutable digests/counts for replay, but not a second stored copy of already committed proposals.
        await db.Set<SyncPartRow>().Where(row => row.RoundId == round.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.ProtectedBody, ""), ct);
        // Caller commits this result with every current row and immutable event; no acknowledgement precedes it.
        return new(true, space.Cursor, []);
    }

    /// <summary>Reads an immutable bounded history page, not mutable latest rows that can skip concurrent edits.</summary>
    internal async Task<SyncFeedPage> FeedAsync(SyncRequestScope scope, SyncSpaceRow space, long after, long through, CancellationToken ct)
    {
        if (scope.AuthorizedDeviceId is not { } device) throw new SyncProtocolRefusal(404, "DeviceUnavailable");
        RequireAuthority(scope, space, device);
        if (space.Initializing || after < 0 || through < after || through > space.Cursor)
            throw new SyncProtocolRefusal(409, "InvalidFeedBoundary");
        if (space.SchemaHash != SyncSchema.Hash) throw new SyncProtocolRefusal(409, "SchemaOrOperation");
        var rows = new List<SyncRecord>(); long bytes = 0; var limitedByBytes = false;
        var events = db.Set<SyncEventRow>().AsNoTracking().Where(row => row.SpaceId == space.Id && row.Sequence > after && row.Sequence <= through)
            .OrderBy(row => row.Sequence).Take(MaximumPartRows).AsAsyncEnumerable();
        await foreach (var item in events.WithCancellation(ct))
        {
            var record = Decode(scope, item);
            if (record.Version != checked(after + rows.Count + 1)) throw new SyncProtocolRefusal(503, "FeedHistoryUnavailable");
            var length = JsonSerializer.SerializeToUtf8Bytes(record, PersonalSyncJsonContext.Default.SyncRecord).Length;
            if (length > 2 * MaximumPartBytes) throw new SyncProtocolRefusal(503, "FeedRecordBound");
            if (rows.Count != 0 && bytes + length > 2 * MaximumPartBytes) { limitedByBytes = true; break; }
            rows.Add(record); bytes += length;
        }
        var next = rows.Count == 0 ? after : rows[^1].Version;
        if (!limitedByBytes && rows.Count < MaximumPartRows && next != through)
            throw new SyncProtocolRefusal(503, "FeedHistoryUnavailable");
        return new(rows, next, through, next == through);
    }

    private static void ValidateProposal(SyncProposal? proposal)
    {
        if (proposal?.Record is not { } key || key.Table is null || key.Key is null || proposal.BaseVersion < 0)
            throw new SyncProtocolRefusal(400, "InvalidProposal");
        var table = SyncSchema.Manifest.Tables.SingleOrDefault(table => table.Name == key.Table);
        if (table is null || !SyncGraphValidation.ValidIdentity(table, key) || proposal.Values is { } row && SyncGraphValidation.ValidateRow(table, key, row).Count != 0)
            throw new SyncProtocolRefusal(400, "InvalidProposal");
    }

    private static bool ValidReviewKey(SyncRecordKey key)
    {
        var table = SyncSchema.Manifest.Tables.SingleOrDefault(row => row.Name == key.Table);
        return key.Key is { Length: <= 2048 } && table is not null && SyncGraphValidation.ValidIdentity(table, key);
    }

    private async Task RequireVerifiedBlobsAsync(SyncRequestScope scope, SyncSpaceRow space,
        IReadOnlyDictionary<SyncRecordKey, SyncRecord> current, IEnumerable<SyncRecord> records, CancellationToken ct)
    {
        var required = new Dictionary<string, long>(StringComparer.Ordinal);
        var changed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in records.Where(record => record.Values is not null))
            foreach (var column in SyncSchema.Manifest.Tables.Single(table => table.Name == record.Record.Table).Columns.Where(column => column.Kind == "blob"))
            {
                var cell = record.Values!.Value.GetProperty(column.Name);
                if (cell.ValueKind == JsonValueKind.Null) continue;
                var hash = cell.GetProperty("sha256").GetString()!; var length = cell.GetProperty("length").GetInt64();
                if (required.TryGetValue(hash, out var previous) && previous != length) throw new SyncProtocolRefusal(400, "BlobLengthMismatch");
                required[hash] = length;
                // Reopen newly introduced content before accepting its financial reference. An unrelated
                // text edit must not decrypt every previously verified image in the personal space.
                if (!current.TryGetValue(record.Record, out var before) || before.Values is not { } old ||
                    !JsonElement.DeepEquals(old.GetProperty(column.Name), cell)) changed.Add(hash);
            }
        var hashes = required.Keys.ToArray();
        var stored = await db.Set<SyncBlobRow>().Where(row => row.SpaceId == space.Id && hashes.Contains(row.Hash) && row.Complete)
            .ToDictionaryAsync(row => row.Hash, ct);
        if (required.Any(item => !stored.TryGetValue(item.Key, out var blob) || blob.Length != item.Value))
            throw new SyncProtocolRefusal(409, "BlobNotVerified");
        var verifier = new SyncBlobStore(db, storage, protection);
        foreach (var hash in changed) await verifier.CompleteAsync(scope, space, stored[hash], ct);
    }

    private static void RequireRound(SyncRequestScope scope, SyncSpaceRow space, SyncRoundRow round)
    {
        RequireAuthority(scope, space, round.DeviceId);
        if (round.SpaceId != space.Id || round.Abandoned || round.SchemaHash != space.SchemaHash || round.SchemaHash != SyncSchema.Hash)
            throw new SyncProtocolRefusal(409, "RoundUnavailable");
    }

    private static void RequireAuthority(SyncRequestScope scope, SyncSpaceRow space, Guid device)
    {
        if (space.UserId != scope.Owner || scope.AuthorizedDeviceId != device || space.Initializing && space.BootstrapDeviceId != device)
            throw new SyncProtocolRefusal(404, "SpaceUnavailable");
    }

    private SyncRecord Decode(SyncRequestScope scope, SyncRecordRow row) => Decode(scope, row.SpaceId, row.TableName, row.RecordKey, row.Version, row.ProtectedBody);
    private SyncRecord Decode(SyncRequestScope scope, SyncEventRow row) => Decode(scope, row.SpaceId, row.TableName, row.RecordKey, row.Sequence, row.ProtectedBody);
    private SyncRecord Decode(SyncRequestScope scope, Guid space, string table, string key, long version, string? encrypted)
    {
        var identity = new SyncRecordKey(table, key);
        if (encrypted is null) return new(identity, version, null);
        using var body = JsonDocument.Parse(RecordProtector(scope, space, identity, version).Unprotect(encrypted));
        return new(identity, version, body.RootElement.Clone());
    }

    private IDataProtector RecordProtector(SyncRequestScope scope, Guid space, SyncRecordKey record, long version) => protection.CreateProtector(
        "personal-financial-record-v1", scope.Owner, space.ToString("N"), record.Table, record.Key, version.ToString(System.Globalization.CultureInfo.InvariantCulture));
    private IDataProtector PartProtector(SyncRequestScope scope, Guid space, Guid round, int index) => protection.CreateProtector(
        "personal-financial-part-v1", scope.Owner, space.ToString("N"), round.ToString("N"), index.ToString(System.Globalization.CultureInfo.InvariantCulture));
    private static bool Same(JsonElement? left, JsonElement? right) => left is null ? right is null : right is not null && JsonElement.DeepEquals(left.Value, right.Value);
}
