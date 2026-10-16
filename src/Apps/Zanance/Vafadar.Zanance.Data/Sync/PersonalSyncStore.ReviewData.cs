using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

public sealed partial class PersonalSyncStore
{
    /// <summary>Reads only bounded original relationship records, preserving their exact values for later consent validation.</summary>
    public async Task<PersonalSyncReviewData> PrepareReviewsAsync(IReadOnlyList<PersonalSyncConflictReview> reviews, CancellationToken ct = default)
    {
        if (reviews.Count > 100) throw new ArgumentException("The personal review batch exceeds its bound.");
        if (reviews.Count == 0) return new([], []);
        await using var db = await factory.CreateDbContextAsync(ct); var access = Access(db);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct); await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (reviews.Any(review => review.Connection.GenerationPath != Generation(db)))
                throw new InvalidOperationException("The personal review profile changed.");
            var round = await db.Set<SyncClientRound>().AsNoTracking().SingleOrDefaultAsync(ct);
            var binding = await db.Set<SyncBindingRow>().AsNoTracking().SingleOrDefaultAsync(ct);
            if (round is null || binding is null || !binding.Enabled || binding.GenerationPath != Generation(db) || reviews.Any(review =>
                review.Operation != round.OperationId || review.Phase != round.Phase || review.Connection.RemoteProfile != binding.ProfileId ||
                review.Connection.IdentityId != binding.IdentityId || review.Connection.PeerKey != binding.PeerKey || review.Connection.DeviceId != binding.DeviceId))
                throw new InvalidOperationException("The personal review authority changed.");
            var conflicts = reviews.Select(review => review.Conflict).ToArray();
            var remote = conflicts.SelectMany(conflict => new[] { conflict.Current, conflict.RelatedRecord }.OfType<SyncRecord>().Concat(conflict.Context ?? []))
                .OfType<SyncRecord>().Where(record => record.Values is not null).ToList();
            var sources = conflicts.Select(conflict => new SyncRecord(conflict.Proposal.Record, 0, conflict.Proposal.Values)).Concat(remote).ToArray();
            var keys = sources.Select(row => row.Record).ToHashSet();
            foreach (var source in sources.Where(source => source.Values is not null))
                foreach (var key in ReferenceKeys(source.Record.Table, source.Values!.Value)) keys.Add(key);
            if (keys.Count > 1024) throw new InvalidDataException("The personal review relationships exceed their bound.");
            var local = new Dictionary<SyncRecordKey, SyncRecord>(); long bytes = 0;
            var pendingKeys = new Queue<SyncRecordKey>(keys);
            while (pendingKeys.TryDequeue(out var key))
            {
                var record = await ReadReviewRecordAsync(db, key, ct);
                if (record.Values is { } value) bytes += System.Text.Encoding.UTF8.GetByteCount(value.GetRawText());
                if (bytes > 1024 * 1024) throw new InvalidDataException("The personal review context exceeds its byte bound.");
                local.Add(key, record);
                if (record.Values is { } referenced)
                    foreach (var dependency in ReferenceKeys(record.Record.Table, referenced))
                        if (keys.Add(dependency))
                        {
                            if (keys.Count > 1024) throw new InvalidDataException("The personal review relationships exceed their bound.");
                            pendingKeys.Enqueue(dependency);
                        }
                if (record.Values is null && remote.All(row => row.Record != key))
                {
                    // Read at most one relevant retained choice, not every resolved financial payload in a large round.
                    await using var lookup = Command(db, "SELECT Payload FROM SyncConflicts WHERE UseProposal IS NOT NULL AND " +
                        "((json_extract(Payload,'$.relatedRecord.record.table')=$table AND json_extract(Payload,'$.relatedRecord.record.key')=$key) OR " +
                        "(json_extract(Payload,'$.current.record.table')=$table AND json_extract(Payload,'$.current.record.key')=$key)) LIMIT 1;");
                    Parameter(lookup, "$table", key.Table); Parameter(lookup, "$key", key.Key);
                    if (await lookup.ExecuteScalarAsync(ct) is string payload)
                    {
                        var retained = JsonSerializer.Deserialize(payload, PersonalSyncJsonContext.Default.SyncConflict)
                            ?? throw new InvalidDataException("The retained review relationship is invalid.");
                        var related = new[] { retained.Current, retained.RelatedRecord }.OfType<SyncRecord>().FirstOrDefault(row => row.Record == key);
                        if (related is not null) remote.Add(related);
                    }
                }
            }
            if (local.Count > 1024) throw new InvalidDataException("The personal review relationships exceed their bound.");
            var context = local.Values.ToArray(); access.EnsureSnapshotCurrent(); await transaction.CommitAsync(ct);
            return new PersonalSyncReviewData(AttachReviewGroups(reviews.Select(review => review with { LocalContext = context, GroupMembers = null }).ToArray()), remote);
        }, ct);
    }

    private static IEnumerable<SyncRecordKey> ReferenceKeys(string tableName, JsonElement values) =>
        SyncReviewRelationships.Keys(tableName, values);

    private static async Task<SyncRecord> ReadReviewRecordAsync(ZananceDbContext db, SyncRecordKey key, CancellationToken ct)
    {
        var table = SyncSchema.Describe(key.Table);
        if (!SyncGraphValidation.ValidIdentity(table, key)) throw new InvalidDataException("The personal review relationship identity is invalid.");
        // Hash the actual logical live attachment under this same snapshot; its content is not a missing row.
        if (table.Name == "Attachments") return await ReadReviewAttachmentRecordAsync(db, key, ct);
        using var identity = JsonDocument.Parse(key.Key); var cells = identity.RootElement.EnumerateArray().ToArray();
        var conditions = table.PrimaryKey.Select((name, index) => "source.\"" + name + "\"=$k" + index).ToArray();
        await using var command = Command(db, SyncTriggerSql.ReadExistingAliased(table) +
            (conditions.Length == 0 ? "" : " WHERE " + string.Join(" AND ", conditions)) + ";");
        for (var index = 0; index < cells.Length; index++)
            Parameter(command, "$k" + index, cells[index].ValueKind == JsonValueKind.String ? cells[index].GetString() : cells[index].GetInt64());
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return new(key, 0, null);
        var row = new SyncRecord(key, 0, SyncRowCodec.ToWire(key, reader.GetString(1)));
        if (await reader.ReadAsync(ct)) throw new InvalidDataException("The personal review singleton is ambiguous.");
        return row;
    }
}
