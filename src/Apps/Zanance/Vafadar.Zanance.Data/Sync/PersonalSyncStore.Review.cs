using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>One exact review snapshot; a later local edit or remote version requires another explicit decision.</summary>
public sealed record PersonalSyncConflictReview(PersonalSyncReview Connection, Guid Operation, int Phase,
    long LocalSequence, string Digest, SyncConflict Conflict, IReadOnlyList<SyncRecord>? LocalContext = null,
    IReadOnlyList<PersonalSyncConflictReview>? GroupMembers = null);

/// <summary>Original conflict snapshots with bounded local names/currencies and authenticated retained remote counterparts.</summary>
public sealed record PersonalSyncReviewData(IReadOnlyList<PersonalSyncConflictReview> Reviews, IReadOnlyList<SyncRecord> RemoteRecords);

public sealed partial class PersonalSyncStore
{
    /// <summary>Returns complete conflict choices only for the actual currently bound profile.</summary>
    public async Task<IReadOnlyList<PersonalSyncConflictReview>> ConflictsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var access = Access(db);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var binding = await db.Set<SyncBindingRow>().AsNoTracking().SingleOrDefaultAsync(ct);
            if (binding is null || !binding.Enabled || binding.GenerationPath != Generation(db)) return [];
            var round = await db.Set<SyncClientRound>().AsNoTracking().SingleOrDefaultAsync(ct);
            if (round is null) return [];
            var connection = new PersonalSyncReview(Generation(db), binding.ProfileId, binding.IdentityId, binding.PeerKey, binding.DeviceId);
            var rows = await db.Set<SyncConflictRow>().AsNoTracking().OrderBy(row => row.TableName).ThenBy(row => row.RecordKey).ToArrayAsync(ct);
            var reviews = rows.Where(row => row.UseProposal is null).Select(row => new PersonalSyncConflictReview(connection, round.OperationId,
                round.Phase, row.LocalSequence, ReviewDigest(row.Payload), JsonSerializer.Deserialize(row.Payload, PersonalSyncJsonContext.Default.SyncConflict)
                    ?? throw new InvalidDataException("The retained personal review is unavailable."))).ToArray();
            access.EnsureSnapshotCurrent(); await transaction.CommitAsync(ct); return AttachReviewGroups(reviews);
        }, ct);
    }

    /// <summary>Records only an explicit whole-row choice for the exact reviewed local change and received version.</summary>
    public async Task ResolveAsync(PersonalSyncConflictReview review, bool keepLocalProposal, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct); var access = Access(db);
        await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var binding = await db.Set<SyncBindingRow>().SingleOrDefaultAsync(ct);
            var round = await db.Set<SyncClientRound>().SingleOrDefaultAsync(ct);
            if (binding is null || !binding.Enabled || binding.GenerationPath != Generation(db) || Generation(db) != review.Connection.GenerationPath ||
                binding.ProfileId != review.Connection.RemoteProfile || binding.IdentityId != review.Connection.IdentityId || binding.PeerKey != review.Connection.PeerKey || binding.DeviceId != review.Connection.DeviceId ||
                round is null || round.OperationId != review.Operation || round.Phase != review.Phase)
                throw new InvalidOperationException("The reviewed personal connection changed.");
            var members = new[] { review }.Concat(review.GroupMembers ?? []).ToArray();
            if (review.Conflict.LinkedGroup is { } group)
            {
                var pending = await db.Set<SyncConflictRow>().AsNoTracking().Where(row => row.UseProposal == null).ToArrayAsync(ct);
                var expectedKeys = pending.Where(row => SyncConflictGrouping.Family(new(row.TableName, row.RecordKey)) == SyncConflictGrouping.Family(review.Conflict.Proposal.Record) &&
                    JsonSerializer.Deserialize(row.Payload, PersonalSyncJsonContext.Default.SyncConflict)?.LinkedGroup == group)
                    .Select(row => new SyncRecordKey(row.TableName, row.RecordKey)).ToHashSet();
                if (members.Any(member => member.Conflict.LinkedGroup != group || SyncConflictGrouping.Family(member.Conflict.Proposal.Record) != SyncConflictGrouping.Family(review.Conflict.Proposal.Record)) || members.Length != expectedKeys.Count ||
                    !expectedKeys.SetEquals(members.Select(member => member.Conflict.Proposal.Record)))
                    throw new InvalidOperationException("The complete linked financial group must be reviewed together.");
                if (keepLocalProposal && members.SelectMany(member => member.Conflict.Context ?? []).Any(record =>
                    SyncConflictGrouping.InGroup(record.Record, record.Values, review.Conflict.Proposal.Record, group) && !expectedKeys.Contains(record.Record)))
                    throw new InvalidOperationException("A remote-only group member cannot be silently included in a local version choice.");
            }
            else if (members.Length != 1) throw new InvalidOperationException("Unrelated personal conflicts cannot share a decision.");
            foreach (var member in members)
            {
            if (member.Connection != review.Connection || member.Operation != review.Operation || member.Phase != review.Phase)
                throw new InvalidOperationException("The linked personal review authority changed.");
            var key = member.Conflict.Proposal.Record;
            var row = await db.Set<SyncConflictRow>().FindAsync([key.Table, key.Key], ct)
                ?? throw new InvalidOperationException("The personal conflict was already replaced.");
            var boundary = await db.Set<SyncOutboxRow>().Where(row => row.TableName == key.Table && row.RecordKey == key.Key)
                .MaxAsync(row => (long?)row.Sequence, ct) ?? 0;
            if (row.UseProposal is not null || row.LocalSequence != member.LocalSequence || boundary != member.LocalSequence || ReviewDigest(row.Payload) != member.Digest ||
                ReviewDigest(JsonSerializer.Serialize(member.Conflict, PersonalSyncJsonContext.Default.SyncConflict)) != member.Digest)
                throw new InvalidOperationException("The personal conflict changed after review.");
            if (member.LocalContext is { } context)
                foreach (var expected in context)
                    if (!Same(expected.Values, (await ReadReviewRecordAsync(db, expected.Record, ct)).Values))
                        throw new InvalidOperationException("A displayed personal review relationship changed.");
            row.UseProposal = keepLocalProposal;
            }
            access.EnsureSnapshotCurrent(); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        }, ct);
    }

    /// <summary>Drops staging only after the actual service confirmed abandonment, preserving all local financial changes.</summary>
    internal Task DiscardAbandonedRoundAsync(SyncRoundWork work, CancellationToken ct) =>
        InRoundAsync(work, async (db, round, _) =>
        {
            if (round.Phase != 1) throw new InvalidOperationException("An acknowledged personal round cannot be abandoned locally.");
            await db.Set<SyncConflictRow>().ExecuteDeleteAsync(ct); await db.Set<SyncIncomingRow>().ExecuteDeleteAsync(ct);
            db.Remove(round);
        }, ct);

    private static string ReviewDigest(string payload) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

    private static IReadOnlyList<PersonalSyncConflictReview> AttachReviewGroups(IReadOnlyList<PersonalSyncConflictReview> reviews) =>
        reviews.Select(review => review with { GroupMembers = review.Conflict.LinkedGroup is { } group ?
            reviews.Where(member => member.Conflict.LinkedGroup == group && SyncConflictGrouping.Family(member.Conflict.Proposal.Record) == SyncConflictGrouping.Family(review.Conflict.Proposal.Record) && member.Conflict.Proposal.Record != review.Conflict.Proposal.Record)
                .Select(member => member with { GroupMembers = null }).ToArray() : null }).ToArray();
}
