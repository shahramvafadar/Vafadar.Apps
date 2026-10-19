using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Coherent original display inputs; the writer independently validates the retained plan and advances.</summary>
/// <param name="DatabasePath">Actual configured file, never a newly selected profile.</param>
/// <param name="Plan">Original saved expense plan.</param>
/// <param name="Account">Original bill account and currency.</param>
/// <param name="Entries">Original history used by the complete advance/refund preview.</param>
public sealed record AdvanceSettlementDraft(string DatabasePath, Schedule Plan, Account Account, List<LedgerEntry> Entries);

public sealed partial class ZananceStore
{
    /// <summary>Reads the original bill account, plan and ledger in one actual-file snapshot before enabling input.</summary>
    public async Task<AdvanceSettlementDraft> ReadAdvanceSettlementDraftAsync(Guid planId, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        var access = new CommercialFileAccess(commercialAccess, path); access.EnsureSnapshotCurrent();
        await db.Database.OpenConnectionAsync(ct);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
        var plan = await db.Schedules.AsNoTracking().SingleAsync(p => p.Id == planId, ct);
        if (plan.Kind != EntryKind.Expense) throw new InvalidOperationException("Only an original expense plan has advance bills.");
        var account = await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == plan.AccountId, ct);
        var entries = await db.Entries.AsNoTracking().OrderBy(e => e.Id).ToListAsync(ct);
        access.EnsureSnapshotCurrent();
        return new(path, plan, account, entries);
    }
}
