using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>Local status contains no credential, pending financial payload or source connection string.</summary>
public sealed record PersonalSyncStatus(bool Connected, bool PausedAfterRestore, string? RemoteName,
    long PendingChanges, int Conflicts, DateTimeOffset? LastSuccessAt, string? Failure);

/// <summary>An actual-file binding review, independent of a subsequently selected profile.</summary>
public sealed record PersonalSyncReview(string GenerationPath, Guid? RemoteProfile, string? IdentityId, string? PeerKey, Guid? DeviceId = null);

/// <summary>Profile-local durable sync capture; constructing the store performs no database or network work.</summary>
public sealed partial class PersonalSyncStore(IDbContextFactory<ZananceDbContext> factory, ICommercialWriteAccessSource commercialAccess,
    TimeProvider? time = null)
{
    private static readonly string[] MetadataTables = ["SyncBlobParts", "SyncBlobTransfers", "SyncConflicts", "SyncIncoming", "SyncClientRound", "SyncOutbox", "SyncKnown", "SyncBinding"];
    /// <summary>Exact owned metadata tables, shared only with the portable-copy scrubber.</summary>
    internal static IReadOnlyList<string> PortableMetadataTables => MetadataTables;

    /// <summary>Reads the captured actual generation; restored metadata cannot authorize an automatic replay.</summary>
    public async Task<PersonalSyncReview> ReviewAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var access = Access(db);
        return await Task.Run(async () =>
        {
            var binding = await db.Set<SyncBindingRow>().AsNoTracking().SingleOrDefaultAsync(ct);
            access.EnsureSnapshotCurrent();
            return new PersonalSyncReview(Generation(db), binding?.ProfileId, binding?.IdentityId, binding?.PeerKey, binding?.DeviceId);
        }, ct);
    }

    /// <summary>Reads only current profile status, not the financial outbox bodies.</summary>
    public async Task<PersonalSyncStatus> StatusAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var access = Access(db);
        return await Task.Run(async () =>
        {
            var binding = await db.Set<SyncBindingRow>().AsNoTracking().SingleOrDefaultAsync(ct);
            var changes = await db.Set<SyncOutboxRow>().LongCountAsync(ct);
            var conflicts = await db.Set<SyncConflictRow>().CountAsync(ct);
            access.EnsureSnapshotCurrent();
            var restored = binding is not null && binding.GenerationPath != Generation(db);
            return new PersonalSyncStatus(binding?.Enabled == true && !restored, restored, binding?.RemoteName,
                changes, conflicts, binding?.LastSuccessAt, binding?.LastFailure);
        }, ct);
    }

    /// <summary>Starts an explicitly reviewed connection or resumes the same generation/owner without discarding offline deletions.</summary>
    public async Task ConnectAsync(PersonalSyncReview review, LocalSyncPeer peer, string identity, Guid device,
        SyncProfile remote, CancellationToken ct = default, bool restartInitialization = false, bool receiveFirstRun = false)
    {
        if (string.IsNullOrEmpty(identity) || identity.Length > 450 || device == Guid.Empty || remote.Id == Guid.Empty ||
            remote.SchemaHash != SyncSchema.Hash || string.IsNullOrWhiteSpace(remote.Name) || remote.Name.Length > 100)
            throw new ArgumentException("A reviewed personal sync identity and compatible space are required.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var access = Access(db);
        await Task.Run(async () =>
        {
            if (Generation(db) != review.GenerationPath) throw new InvalidOperationException("The reviewed local profile changed.");
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var previous = await db.Set<SyncBindingRow>().SingleOrDefaultAsync(ct);
            if (previous?.ProfileId != review.RemoteProfile || previous?.IdentityId != review.IdentityId || previous?.PeerKey != review.PeerKey || previous?.DeviceId != review.DeviceId)
                throw new InvalidOperationException("The reviewed connection changed.");
            var key = peer.BindingKey(identity);
            var resume = previous is not null && previous.GenerationPath == review.GenerationPath && previous.ProfileId == remote.Id &&
                previous.PeerKey == key && previous.IdentityId == identity && previous.SchemaHash == SyncSchema.Hash;
            if (receiveFirstRun)
            {
                // A wizard's empty destination preferences are not edits to the selected remote
                // profile. Never apply this receive-only exception to existing financial work.
                if (await db.Settings.AnyAsync(row => row.OnboardingCompleted, ct) || !resume && await db.Set<SyncClientRound>().AnyAsync(ct))
                    throw new InvalidOperationException("First-run reception requires an empty destination and preserves only its own active round.");
                foreach (var table in SyncSchema.Manifest.Tables.Where(row => row.Name is not ("Settings" or "Categories")))
                {
                    await using var present = Command(db, "SELECT 1 FROM " + Quote(table.Name) + " LIMIT 1;");
                    if (await present.ExecuteScalarAsync(ct) is not null)
                        throw new InvalidOperationException("First-run reception cannot replace an existing financial profile.");
                }
            }
            if (!resume)
            {
                foreach (var table in MetadataTables)
                {
                    // Identifiers come exclusively from this fixed internal list, never a peer or user value.
                    var sql = "DELETE FROM \"" + table + "\";";
                    await db.Database.ExecuteSqlRawAsync(sql, ct);
                }
                await db.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name='SyncOutbox';", ct);
                db.ChangeTracker.Clear();
                previous = new() { Id = 1, ProfileId = remote.Id, IdentityId = identity, PeerKey = key,
                    GenerationPath = review.GenerationPath, SchemaHash = SyncSchema.Hash };
                db.Add(previous);
            }
            else
            {
                // A new authenticated device can recover the same local queue, including offline tombstones.
                // Its old operation may already have committed, so replay the same row versions through a fresh nonce.
                if (restartInitialization && (!remote.Initializing || remote.Cursor != 0 || remote.BootstrapDeviceId != device ||
                    await db.Set<SyncClientRound>().AnyAsync(row => row.Phase == 2, ct)))
                    throw new InvalidOperationException("Acknowledged financial work cannot be discarded for initialization recovery.");
                if (previous!.DeviceId != device || restartInitialization)
                {
                    await db.Set<SyncClientRound>().ExecuteDeleteAsync(ct);
                    await db.Set<SyncIncomingRow>().ExecuteDeleteAsync(ct);
                    await db.Set<SyncConflictRow>().ExecuteDeleteAsync(ct);
                }
            }
            previous!.DeviceId = device; previous.RemoteName = remote.Name; previous.Applying = false; previous.Enabled = false;
            previous.LastFailure = null;
            await db.SaveChangesAsync(ct);
            // A full reconciliation also captures changes made while sync was paused. Existing frozen cutoffs stay immutable.
            foreach (var table in SyncSchema.Manifest.Tables)
            {
                if (receiveFirstRun && table.Name == "Settings") continue;
                await db.Database.ExecuteSqlRawAsync(SyncTriggerSql.CaptureExisting(table), ct);
                if (resume)
                {
                    var keys = table.PrimaryKey.Length == 0 ? "json_array()" : "json_array(" + string.Join(',', table.PrimaryKey.Select(name => "source.\"" + name + "\"")) + ")";
                    var sql = "INSERT INTO \"SyncOutbox\"(\"TableName\",\"RecordKey\",\"Body\") " +
                        "SELECT \"TableName\",\"RecordKey\",NULL FROM \"SyncKnown\" AS known WHERE \"TableName\"=$table AND \"Body\" IS NOT NULL " +
                        "AND NOT EXISTS(SELECT 1 FROM \"" + table.Name + "\" AS source WHERE " + keys + "=known.\"RecordKey\");";
                    await db.Database.ExecuteSqlRawAsync(sql, [new SqliteParameter("$table", table.Name)], ct);
                }
            }
            previous.Enabled = true;
            if (receiveFirstRun && !await db.Set<SyncClientRound>().AnyAsync(ct))
                await db.Set<SyncOutboxRow>().Where(row => row.TableName == "Settings").ExecuteDeleteAsync(ct);
            access.EnsureSnapshotCurrent(); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        }, ct);
    }

    /// <summary>Stops new replay while retaining the owned queue, version history and local finances.</summary>
    public async Task PauseAsync(PersonalSyncReview review, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var access = Access(db);
        await Task.Run(async () =>
        {
            if (Generation(db) != review.GenerationPath) throw new InvalidOperationException("The reviewed local profile changed.");
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var binding = await db.Set<SyncBindingRow>().SingleOrDefaultAsync(ct);
            if (binding is not null)
            {
                if (binding.ProfileId != review.RemoteProfile || binding.IdentityId != review.IdentityId || binding.PeerKey != review.PeerKey || binding.DeviceId != review.DeviceId)
                    throw new InvalidOperationException("The reviewed connection changed.");
                binding.Enabled = false; access.EnsureSnapshotCurrent(); await db.SaveChangesAsync(ct);
            }
            access.EnsureSnapshotCurrent(); await transaction.CommitAsync(ct);
        }, ct);
    }

    private static string Generation(ZananceDbContext db) => Path.GetFullPath(DatabaseFileIdentity.ConfiguredPath(db));
    private CommercialFileAccess Access(ZananceDbContext db)
    {
        var access = new CommercialFileAccess(commercialAccess, DatabaseFileIdentity.ConfiguredPath(db));
        access.DemandFeature(CommercialFeature.PersonalSync); return access;
    }
}
