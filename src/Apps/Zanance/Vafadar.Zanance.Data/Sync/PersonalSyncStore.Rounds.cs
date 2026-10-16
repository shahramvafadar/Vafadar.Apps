using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>The exact local file/owner/device captured before any network work.</summary>
internal sealed record SyncLocalAuthority(string Generation, Guid Profile, Guid Device, string Identity, string PeerKey,
    ProfileSynchronizationKind TransportKind = ProfileSynchronizationKind.Personal);
/// <summary>A sealed local cutoff; all network work consumes immutable copies after the writer has ended.</summary>
internal sealed record SyncRoundWork(SyncLocalAuthority Authority, Guid Operation, Guid? Remote, long Cutoff, int Phase,
    int SentParts, long Cursor, long ReadCursor, long? ThroughCursor, SyncRoundPart[] Parts, string ContentHash,
    SyncResolution[] Resolutions, bool NeedsReview);

public sealed partial class PersonalSyncStore
{
    private const int BlobPieceBytes = 2 * 1024 * 1024;

    /// <summary>Freezes a complete committed cutoff; later local edits remain outside this round.</summary>
    internal async Task<SyncRoundWork> CaptureRoundAsync(LocalSyncPeer peer, string identity, CancellationToken ct,
        ProfileSynchronizationKind transportKind = ProfileSynchronizationKind.Personal)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var access = Access(db);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var binding = await db.Set<SyncBindingRow>().SingleOrDefaultAsync(ct);
            if (binding is null || !binding.Enabled || binding.Applying || binding.GenerationPath != Generation(db) ||
                binding.IdentityId != identity || binding.PeerKey != peer.BindingKey(identity) || binding.SchemaHash != SyncSchema.HashForScope(transportKind == ProfileSynchronizationKind.Household) || binding.TransportKind != transportKind)
                throw new InvalidOperationException("The personal sync connection is paused or no longer belongs to this actual profile.");
            var authority = new SyncLocalAuthority(Generation(db), binding.ProfileId, binding.DeviceId, binding.IdentityId, binding.PeerKey, binding.TransportKind);
            var round = await db.Set<SyncClientRound>().SingleOrDefaultAsync(ct);
            if (round is null)
            {
                await ReconcileAttachmentCaptureAsync(db, 0, ct);
                await ReconcileGroupCaptureAsync(db, 0, ct);
                round = new() { Id = 1, OperationId = Guid.NewGuid(), Cutoff = await db.Set<SyncOutboxRow>().MaxAsync(row => (long?)row.Sequence, ct) ?? 0,
                    ReadCursor = binding.Cursor };
                db.Add(round);
            }
            var captured = await OutboxThroughAsync(db, round.Cutoff, ct);
            var known = await db.Set<SyncKnownRow>().AsNoTracking().ToDictionaryAsync(row => new SyncRecordKey(row.TableName, row.RecordKey), ct);
            var allowed = SyncSchema.ForScope(transportKind == ProfileSynchronizationKind.Household).Tables.Select(table => table.Name).ToHashSet(StringComparer.Ordinal);
            if (captured.Any(row => !allowed.Contains(row.TableName)) || known.Keys.Any(key => !allowed.Contains(key.Table)))
                throw new InvalidDataException("Captured finance does not belong to this exact transport contract.");
            var blobs = new Dictionary<string, byte[]>(StringComparer.Ordinal); var blobBytes = 0L;
            try
            {
                var proposals = captured.Select(row =>
                {
                    var key = new SyncRecordKey(row.TableName, row.RecordKey);
                    var values = SyncRowCodec.ToWire(key, row.Body, (hash, bytes) =>
                    {
                        if (blobs.ContainsKey(hash)) return;
                        blobBytes += bytes.Length;
                        if (blobBytes > SyncGraphValidation.MaximumProfileBytes) throw new InvalidDataException("The personal attachment snapshot exceeds its bound.");
                        blobs.Add(hash, bytes.ToArray());
                    });
                    return new SyncProposal(key, known.GetValueOrDefault(key)?.Version ?? 0, values);
                }).ToArray();
                var parts = SyncRowCodec.Parts(proposals, transportKind); var digest = SyncProtocolDigest.Round(parts.Select(SyncProtocolDigest.Part));
                if (round.Phase == 0)
                { round.ContentHash = digest; round.PartCount = parts.Length; round.Phase = 1; }
                else if (digest != round.ContentHash || parts.Length != round.PartCount)
                    throw new InvalidDataException("The retained personal round changed after capture.");
                foreach (var (hash, bytes) in blobs)
                {
                    var transfer = await db.Set<SyncBlobTransfer>().FindAsync([hash], ct);
                    if (transfer is not null && transfer.Length != bytes.Length) throw new InvalidDataException("The retained attachment length changed.");
                    if (transfer is null)
                    {
                        db.Add(new SyncBlobTransfer { Hash = hash, Length = bytes.Length });
                        for (var offset = 0; offset < bytes.Length; offset += BlobPieceBytes)
                            db.Add(new SyncBlobPart { Hash = hash, Part = offset / BlobPieceBytes, Data = bytes.AsSpan(offset, Math.Min(BlobPieceBytes, bytes.Length - offset)).ToArray() });
                    }
                }
                var reviews = await db.Set<SyncConflictRow>().AsNoTracking().OrderBy(row => row.TableName).ThenBy(row => row.RecordKey).ToArrayAsync(ct);
                var resolutions = new List<SyncResolution>();
                foreach (var review in reviews)
                {
                    if (review.UseProposal is not { } selected) continue;
                    var conflict = JsonSerializer.Deserialize(review.Payload, PersonalSyncJsonContext.Default.SyncConflict)
                        ?? throw new InvalidDataException("The retained personal review is invalid.");
                    resolutions.Add(new(conflict.Proposal.Record, conflict.Current?.Version ?? 0, selected,
                        conflict.RelatedRecord?.Record, conflict.RelatedRecord?.Version,
                        conflict.Context?.Select(row => new SyncRecordVersion(row.Record, row.Version)).ToArray()));
                }
                access.EnsureSnapshotCurrent(); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
                return new SyncRoundWork(authority, round.OperationId, round.RemoteId, round.Cutoff, round.Phase, round.SentParts,
                    binding.Cursor, round.ReadCursor, round.ThroughCursor, parts, digest, resolutions.ToArray(), reviews.Any(row => row.UseProposal is null));
            }
            finally { foreach (var bytes in blobs.Values) CryptographicOperations.ZeroMemory(bytes); }
        }, ct);
    }

    /// <summary>Records only a confirmed network prefix for the same operation and actual local authority.</summary>
    internal Task RecordRemoteAsync(SyncRoundWork work, Guid remote, int confirmedParts, CancellationToken ct) =>
        UpdateRoundAsync(work, (round, _) =>
        {
            if (remote == Guid.Empty || round.RemoteId is { } previous && previous != remote || confirmedParts < round.SentParts || confirmedParts > round.PartCount)
                throw new InvalidDataException("The personal round observation is invalid.");
            round.RemoteId = remote; round.SentParts = confirmedParts;
        }, ct);

    /// <summary>A conflict stores no financial mutation; a committed receipt starts private feed staging.</summary>
    internal async Task RecordCommitAsync(SyncRoundWork work, SyncCommitResult result, CancellationToken ct)
    {
        await InRoundAsync(work, async (db, round, binding) =>
        {
            if (round.Phase != 1 || result.Cursor < binding.Cursor || result.Conflicts.Count > 100 || result.Committed && result.Conflicts.Count != 0)
                throw new InvalidDataException("The personal commit observation is invalid.");
            if (result.Committed)
            {
                await db.Set<SyncConflictRow>().ExecuteDeleteAsync(ct);
                round.Phase = 2; round.ThroughCursor = result.Cursor; round.ReadCursor = binding.Cursor;
            }
            else
            {
                // A stale counterpart can invalidate only one choice. Preserve other resolved choices for this
                // exact frozen operation, or successive partial conflict results can discard them forever.
                await db.Set<SyncConflictRow>().Where(row => row.UseProposal == null).ExecuteDeleteAsync(ct);
                foreach (var conflict in result.Conflicts)
                {
                    var proposal = work.Parts.SelectMany(part => part.Rows).SingleOrDefault(row => row.Record == conflict.Proposal.Record);
                    if (proposal is null || !Same(proposal.Values, conflict.Proposal.Values) || proposal.BaseVersion != conflict.Proposal.BaseVersion)
                        throw new InvalidDataException("The service review does not belong to this personal proposal.");
                    var boundary = await db.Set<SyncOutboxRow>().Where(row => row.TableName == proposal.Record.Table && row.RecordKey == proposal.Record.Key)
                        .MaxAsync(row => (long?)row.Sequence, ct) ?? 0;
                    await db.Set<SyncConflictRow>().Where(row => row.TableName == proposal.Record.Table && row.RecordKey == proposal.Record.Key).ExecuteDeleteAsync(ct);
                    db.Add(new SyncConflictRow { TableName = proposal.Record.Table, RecordKey = proposal.Record.Key,
                        Payload = JsonSerializer.Serialize(conflict, PersonalSyncJsonContext.Default.SyncConflict), LocalSequence = boundary });
                }
                if (result.Conflicts.Count == 0) throw new InvalidDataException("A refused personal round needs an actual review outcome.");
            }
        }, ct);
    }

    private Task UpdateRoundAsync(SyncRoundWork work, Action<SyncClientRound, SyncBindingRow> update, CancellationToken ct) =>
        InRoundAsync(work, (_, round, binding) => { update(round, binding); return Task.CompletedTask; }, ct);

    private async Task InRoundAsync(SyncRoundWork work, Func<ZananceDbContext, SyncClientRound, SyncBindingRow, Task> action, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var access = Access(db);
        await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var binding = await RequireBindingAsync(db, work.Authority, ct);
            var round = await db.Set<SyncClientRound>().SingleAsync(ct);
            if (round.OperationId != work.Operation || round.Cutoff != work.Cutoff || round.ContentHash != work.ContentHash)
                throw new InvalidOperationException("The retained personal operation changed.");
            await action(db, round, binding); access.EnsureSnapshotCurrent();
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        }, ct);
    }

    private static async Task<SyncBindingRow> RequireBindingAsync(ZananceDbContext db, SyncLocalAuthority authority, CancellationToken ct)
    {
        var binding = await db.Set<SyncBindingRow>().SingleOrDefaultAsync(ct);
        if (binding is null || !binding.Enabled || binding.Applying || Generation(db) != authority.Generation || binding.GenerationPath != authority.Generation ||
            binding.ProfileId != authority.Profile || binding.DeviceId != authority.Device || binding.IdentityId != authority.Identity ||
            binding.PeerKey != authority.PeerKey || binding.SchemaHash != SyncSchema.HashForScope(authority.TransportKind == ProfileSynchronizationKind.Household) || binding.TransportKind != authority.TransportKind)
            throw new InvalidOperationException("The captured personal sync authority is no longer current.");
        return binding;
    }

    private static async Task<SyncOutboxRow[]> OutboxThroughAsync(ZananceDbContext db, long cutoff, CancellationToken ct)
    {
        await using var command = Command(db, "SELECT outbox.\"Sequence\",outbox.\"TableName\",outbox.\"RecordKey\",outbox.\"Body\" FROM \"SyncOutbox\" AS outbox " +
            "JOIN (SELECT \"TableName\",\"RecordKey\",MAX(\"Sequence\") AS latest FROM \"SyncOutbox\" WHERE \"Sequence\"<=$cutoff GROUP BY \"TableName\",\"RecordKey\") AS selected " +
            "ON outbox.\"Sequence\"=selected.latest ORDER BY outbox.\"TableName\",outbox.\"RecordKey\";");
        Parameter(command, "$cutoff", cutoff); var rows = new List<SyncOutboxRow>(); long bytes = 0;
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var body = reader.IsDBNull(3) ? null : reader.GetString(3);
            bytes += body is null ? 0 : System.Text.Encoding.UTF8.GetByteCount(body);
            if (rows.Count >= SyncGraphValidation.MaximumRows || bytes > 3L * SyncGraphValidation.MaximumProfileBytes)
                throw new InvalidDataException("The retained personal snapshot exceeds its bound.");
            rows.Add(new() { Sequence = reader.GetInt64(0), TableName = reader.GetString(1), RecordKey = reader.GetString(2), Body = body });
        }
        return rows.ToArray();
    }
    private static DbCommand Command(ZananceDbContext db, string sql)
    {
        var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = sql;
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction(); return command;
    }
    private static void Parameter(DbCommand command, string name, object? value)
    { var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value ?? DBNull.Value; command.Parameters.Add(parameter); }
    private static bool Same(JsonElement? left, JsonElement? right) => left is null ? right is null : right is not null && JsonElement.DeepEquals(left.Value, right.Value);
}
