using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using Vafadar.Data;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceDbContext
{
    private static readonly HashSet<string> FinancialTables = Core.Sharing.HouseholdFinancialSchema.Manifest.Tables.Select(table => table.Name).ToHashSet(StringComparer.Ordinal);
    private HouseholdRuntimeWriteScope? householdWriteScope;
    private HouseholdWriteRejectedException? householdWriteFailure;
    private HouseholdRuntimeWriteScope? householdCommitScope;
    private Guid? costGraphTransaction;
    private readonly HashSet<Guid> insertedHouseholdCosts = [];
    private readonly HashSet<Guid> insertedHouseholdSettlements = [];
    private readonly HashSet<(Guid CostId, Guid ContactId)> insertedHouseholdAllocations = [];

    /// <summary>Custom model conversions or command observers retain the full original scalar reader.</summary>
    internal bool CanUseHomeLedgerBlocks => ReferenceEquals(Model, CompiledModel.ZananceDbContextModel.Instance)
        && HasOriginalReadPipeline(interceptor => interceptor is HouseholdCommitInterceptor);

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(new HouseholdCommitInterceptor(this));
    }

    /// <summary>Uses the same exact transaction-local verified member authority for original stores and native trigger checks.</summary>
    internal IDisposable ArmHouseholdEditor(HouseholdLocalWritePermission? permission) => ArmHouseholdScope(permission, null);

    /// <summary>Only already-validated internal seed/feed code can arm bounded maintenance over its actual reviewed row keys.</summary>
    internal IDisposable ArmVerifiedHouseholdMaintenance(IReadOnlySet<SyncRecordKey> keys)
    {
        if (Database.GetDbConnection() is not SqliteConnection connection) throw new HouseholdWriteRejectedException(HouseholdWriteBlockReason.Unverified);
        ProtectedSqliteAccess.Require(connection);
        HouseholdWriteBoundary.Require(this);
        return ArmHouseholdScope(null, keys.ToHashSet());
    }

    private IDisposable ArmHouseholdScope(HouseholdLocalWritePermission? permission, IReadOnlySet<SyncRecordKey>? keys)
    {
        var transaction = Database.CurrentTransaction?.TransactionId ?? throw new InvalidOperationException("Household writes require the actual SQLite writer transaction.");
        var next = new HouseholdRuntimeWriteScope(this, transaction, permission, keys, householdWriteScope);
        householdWriteFailure = null; householdWriteScope = next; return next;
    }

    /// <summary>The native trigger callback does no SQL or key import. It checks only the prepared exact transaction/file/clock and optional reviewed key scope.</summary>
    private long DemandHouseholdFinancialWrite(string table, string operation, string key)
    {
        try
        {
            if (!FinancialTables.Contains(table) || operation is not ("insert" or "update" or "delete") || householdWriteScope is not { } scope ||
                Database.CurrentTransaction?.TransactionId != scope.Transaction) throw new HouseholdWriteRejectedException(HouseholdWriteBlockReason.Unverified);
            if (scope.Keys is { } reviewed)
            {
                if (!reviewed.Contains(new(table, key)) || Database.GetDbConnection() is not SqliteConnection connection)
                    throw new HouseholdWriteRejectedException(HouseholdWriteBlockReason.Unverified);
                ProtectedSqliteAccess.Require(connection);
            }
            else if (scope.Permission is { } permission) permission.Demand();
            else throw new HouseholdWriteRejectedException(HouseholdWriteBlockReason.Unverified);
            householdCommitScope = scope;
            TrackCostGraphWrite(scope.Transaction, table, scope.Keys is null ? operation : "maintenance", key);
            return 1;
        }
        catch (HouseholdWriteRejectedException rejected) { householdWriteFailure = rejected; throw; }
    }

    /// <summary>Even an outer borrowed transaction rechecks its actual last financial authority at publication, after the editor scope has unwound.</summary>
    private void DemandHouseholdCommit(Guid transaction)
    {
        if (householdCommitScope is { } scope && scope.Transaction == transaction)
        {
            if (scope.Permission is { } permission) permission.Demand();
            else if (scope.Keys is not null && Database.GetDbConnection() is SqliteConnection connection) ProtectedSqliteAccess.Require(connection);
            else throw new HouseholdWriteRejectedException(HouseholdWriteBlockReason.Unverified);
        }
        // This runs outside SaveChanges, after every statement in a compound cost/allocations or feed apply.
        // Checking intermediate statements would reject a valid replacement before its new shares are inserted.
        if (costGraphTransaction == transaction)
            HouseholdCostState.Read(this).DemandValid(insertedHouseholdCosts, insertedHouseholdSettlements, insertedHouseholdAllocations);
    }

    /// <summary>Retains only transaction identity/new-row ids; the native callback still performs no SQL or financial reads.</summary>
    private void TrackCostGraphWrite(Guid transaction, string table, string operation, string? key = null)
    {
        if (costGraphTransaction != transaction)
        {
            costGraphTransaction = transaction; insertedHouseholdCosts.Clear(); insertedHouseholdSettlements.Clear(); insertedHouseholdAllocations.Clear();
        }
        if (operation == "insert" && table == "HouseholdAllocations" && key is { Length: 79 })
        {
            using var values = System.Text.Json.JsonDocument.Parse(key);
            insertedHouseholdAllocations.Add((values.RootElement[0].GetGuid(), values.RootElement[1].GetGuid()));
        }
        if (operation != "insert" || key is not { Length: 40 } || !Guid.TryParse(key.AsSpan(2, 36), out var id)) return;
        if (table == "HouseholdCosts") insertedHouseholdCosts.Add(id);
        if (table == "HouseholdSettlements") insertedHouseholdSettlements.Add(id);
    }

    /// <summary>Original tracked writers also protect linked costs in detached portable history without a household binding.</summary>
    private void TrackCostGraphChanges()
    {
        if (Database.CurrentTransaction is not { } transaction) return;
        foreach (var entry in ChangeTracker.Entries().Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Metadata.GetTableName() is not { } table || !FinancialTables.Contains(table)) continue;
            TrackCostGraphWrite(transaction.TransactionId, table, "tracked");
            if (entry.State != EntityState.Added) continue;
            if (entry.Entity is Core.Sharing.HouseholdCost cost) insertedHouseholdCosts.Add(cost.Id);
            if (entry.Entity is Core.Sharing.HouseholdSettlement settlement) insertedHouseholdSettlements.Add(settlement.Id);
            if (entry.Entity is Core.Sharing.HouseholdCostAllocation share) insertedHouseholdAllocations.Add((share.CostId, share.ContactId));
        }
    }

    /// <summary>Private original-context commit barrier, including clocks that expire after the last SQL write and before an outer commit.</summary>
    private sealed class HouseholdCommitInterceptor(ZananceDbContext owner) : DbTransactionInterceptor
    {
        public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
        {
            owner.DemandHouseholdCommit(eventData.TransactionId);
            HomeLedgerReadBlocks.MaintainAsync(owner, CancellationToken.None).GetAwaiter().GetResult();
            owner.DemandHouseholdCommit(eventData.TransactionId);
            return result;
        }
        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); owner.DemandHouseholdCommit(eventData.TransactionId);
            await HomeLedgerReadBlocks.MaintainAsync(owner, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested(); owner.DemandHouseholdCommit(eventData.TransactionId);
            return result;
        }
    }

    /// <inheritdoc />
    public override int SaveChanges() => SaveChanges(true);
    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        Task.Run(() => SaveChangesAsync(acceptAllChangesOnSuccess, CancellationToken.None)).GetAwaiter().GetResult();
    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => SaveChangesAsync(true, cancellationToken);
    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        householdWriteFailure = null;
        var needsScope = householdWriteScope is null || Database.CurrentTransaction?.TransactionId != householdWriteScope.Transaction;
        if (!needsScope || !ChangeTracker.Entries().Any(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
            entry.Metadata.GetTableName() is { } table && FinancialTables.Contains(table)))
        {
            TrackCostGraphChanges();
            try { return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false); }
            catch (Exception) when (householdWriteFailure is not null) { throw householdWriteFailure; }
        }
        var owned = Database.CurrentTransaction is null ? await Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false) : null;
        try
        {
            var permission = await HouseholdJournal.CaptureLocalWritePermissionAsync(this, WorkClock, cancellationToken).ConfigureAwait(false);
            using var prepared = ArmHouseholdEditor(permission);
            TrackCostGraphChanges();
            // A denied role is not thrown here: native Settings UPDATE triggers distinguish actual
            // financial-column changes from harmless device-only preference/status writes.
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
            if (owned is not null) await owned.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (Exception) when (householdWriteFailure is not null) { throw householdWriteFailure; }
        finally { if (owned is not null) await owned.DisposeAsync().ConfigureAwait(false); }
    }

    /// <summary>Nested private scope cannot escape its original native writer or authorize another context/generation.</summary>
    private sealed class HouseholdRuntimeWriteScope(ZananceDbContext owner, Guid transaction, HouseholdLocalWritePermission? permission,
        IReadOnlySet<SyncRecordKey>? keys, HouseholdRuntimeWriteScope? previous) : IDisposable
    {
        internal Guid Transaction { get; } = transaction;
        internal HouseholdLocalWritePermission? Permission { get; } = permission;
        internal IReadOnlySet<SyncRecordKey>? Keys { get; } = keys;
        private bool disposed;
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (ReferenceEquals(owner.householdWriteScope, this)) owner.householdWriteScope = previous;
        }
    }
}
