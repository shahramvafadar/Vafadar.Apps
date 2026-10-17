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
public sealed record PersonalSyncReview(string GenerationPath, Guid? RemoteProfile, string? IdentityId, string? PeerKey, Guid? DeviceId = null,
    ProfileSynchronizationKind TransportKind = ProfileSynchronizationKind.Personal);

/// <summary>Profile-local durable sync capture; constructing the store performs no database or network work.</summary>
public sealed partial class PersonalSyncStore(IDbContextFactory<ZananceDbContext> factory, ICommercialWriteAccessSource commercialAccess,
    TimeProvider? time = null)
{
    private static readonly string[] MetadataTables = ["SyncMarketSourcesSent", "SyncBlobParts", "SyncBlobTransfers", "SyncConflicts", "SyncIncoming", "SyncClientRound", "SyncOutbox", "SyncKnown", "SyncBinding"];
    /// <summary>Exact owned metadata tables, shared only with the portable-copy scrubber.</summary>
    internal static IReadOnlyList<string> PortableMetadataTables => MetadataTables.Concat(Sharing.HouseholdLocalModel.PortableTables).ToArray();

    /// <summary>Reads the captured actual generation; restored metadata cannot authorize an automatic replay.</summary>
    public async Task<PersonalSyncReview> ReviewAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var access = Access(db);
        return await Task.Run(async () =>
        {
            var binding = await db.Set<SyncBindingRow>().AsNoTracking().SingleOrDefaultAsync(ct);
            access.EnsureSnapshotCurrent();
            return new PersonalSyncReview(Generation(db), binding?.ProfileId, binding?.IdentityId, binding?.PeerKey, binding?.DeviceId,
                binding?.TransportKind ?? ProfileSynchronizationKind.Personal);
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
            if (binding?.Enabled == true && !restored)
                changes += await db.MarketReferenceReceipts.LongCountAsync(row => !db.Set<SyncMarketSourceSend>().Any(sent => sent.DocumentId == row.Id && sent.Acknowledged), ct);
            access.EnsureSnapshotCurrent();
            return new PersonalSyncStatus(binding?.Enabled == true && !restored, restored, binding?.RemoteName,
                changes, conflicts, binding?.LastSuccessAt, binding?.LastFailure);
        }, ct);
    }

    /// <summary>Starts an explicitly reviewed connection or resumes the same generation/owner without discarding offline deletions.</summary>
    public async Task ConnectAsync(PersonalSyncReview review, LocalSyncPeer peer, string identity, Guid device,
        SyncProfile remote, CancellationToken ct = default, bool restartInitialization = false, bool receiveFirstRun = false,
        ProfileSynchronizationKind transportKind = ProfileSynchronizationKind.Personal)
    {
        if (string.IsNullOrEmpty(identity) || identity.Length > 450 || device == Guid.Empty || remote.Id == Guid.Empty ||
            remote.SchemaHash != SyncSchema.HashForScope(transportKind == ProfileSynchronizationKind.Household) || string.IsNullOrWhiteSpace(remote.Name) || remote.Name.Length > 100 || !Enum.IsDefined(transportKind))
            throw new ArgumentException("A reviewed personal sync identity and compatible space are required.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var access = Access(db);
        await Task.Run(async () =>
        {
            if (Generation(db) != review.GenerationPath) throw new InvalidOperationException("The reviewed local profile changed.");
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var previous = await db.Set<SyncBindingRow>().SingleOrDefaultAsync(ct);
            if (transportKind != ProfileSynchronizationKind.Household && await db.Set<Sharing.HouseholdLocalBinding>().AnyAsync(ct))
                throw new InvalidOperationException("A prepared household draft cannot use readable personal publication.");
            if (transportKind != ProfileSynchronizationKind.Household && (await db.HouseholdContacts.AnyAsync(ct) || await db.HouseholdCosts.AnyAsync(ct) || await db.HouseholdSettlements.AnyAsync(ct)))
                throw new InvalidOperationException("Household financial history requires its encrypted contract; a personal connection cannot silently omit it.");
            if (previous?.ProfileId != review.RemoteProfile || previous?.IdentityId != review.IdentityId || previous?.PeerKey != review.PeerKey || previous?.DeviceId != review.DeviceId ||
                previous is not null && previous.TransportKind != review.TransportKind)
                throw new InvalidOperationException("The reviewed connection changed.");
            if (previous?.TransportKind == ProfileSynchronizationKind.Household && transportKind != ProfileSynchronizationKind.Household)
                throw new InvalidOperationException("A household profile cannot use readable personal publication; explicitly export a detached portable copy first.");
            var key = peer.BindingKey(identity);
            if (transportKind == ProfileSynchronizationKind.Household)
                await Sharing.HouseholdJournal.RequireLocalHouseholdConnectionAsync(db, review.GenerationPath, identity, key, remote.Id, device, receiveFirstRun, ct);
            var resume = previous is not null && previous.GenerationPath == review.GenerationPath && previous.ProfileId == remote.Id &&
                previous.PeerKey == key && previous.IdentityId == identity && previous.SchemaHash == SyncSchema.HashForScope(transportKind == ProfileSynchronizationKind.Household) && previous.TransportKind == transportKind;
            if (receiveFirstRun)
            {
                // A wizard's empty destination preferences are not edits to the selected remote
                // profile. Never apply this receive-only exception to existing financial work.
                if (await db.Settings.AnyAsync(row => row.OnboardingCompleted, ct) || !resume && await db.Set<SyncClientRound>().AnyAsync(ct))
                    throw new InvalidOperationException("First-run reception requires an empty destination and preserves only its own active round.");
                foreach (var table in SyncSchema.ForScope(transportKind == ProfileSynchronizationKind.Household).Tables.Where(row => row.Name is not ("Settings" or "Categories")))
                {
                    await using var present = Command(db, "SELECT 1 FROM " + Quote(table.Name) + " LIMIT 1;");
                    if (await present.ExecuteScalarAsync(ct) is not null)
                        throw new InvalidOperationException("First-run reception cannot replace an existing financial profile.");
                }
                if (transportKind == ProfileSynchronizationKind.Household)
                {
                    if (await db.Categories.AnyAsync(row => row.SystemKey == null || row.Name != null || row.IsArchived, ct))
                        throw new InvalidOperationException("A fresh household recipient cannot silently discard custom local categories.");
                    var initial = await ReadFinancialAsync(db, ct);
                    using var receiveDefaults = db.ArmVerifiedHouseholdMaintenance(initial.Keys.Where(row => row.Table == "Categories").ToHashSet());
                    await db.Database.ExecuteSqlRawAsync("PRAGMA defer_foreign_keys=ON;", ct);
                    await db.Categories.ExecuteDeleteAsync(ct);
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
                    GenerationPath = review.GenerationPath, SchemaHash = SyncSchema.HashForScope(transportKind == ProfileSynchronizationKind.Household), TransportKind = transportKind };
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
            foreach (var table in SyncSchema.ForScope(transportKind == ProfileSynchronizationKind.Household).Tables)
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
        access.DemandFeature(access.Context?.Scope.Kind == EntitlementScopeKind.SharedSpace
            ? CommercialFeature.SharedSpaceSync : CommercialFeature.PersonalSync); return access;
    }
}
