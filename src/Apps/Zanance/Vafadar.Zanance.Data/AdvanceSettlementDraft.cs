using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Complete original bill inputs and cached access from one actual-file worker transaction.</summary>
public sealed class AdvanceSettlementDraft
{
    private readonly CommercialFileAccess _access;
    private readonly string _stamp;
    /// <summary>Captures complete detached rows before they can be edited by presentation code.</summary>
    internal AdvanceSettlementDraft(string path, Schedule plan, Account account, List<LedgerEntry> entries, CommercialFileAccess access)
    { DatabasePath = path; Plan = plan; Account = account; Entries = entries; _access = access; _stamp = Capture(plan, account, entries); }
    /// <summary>Gets the original configured file, never a newly selected profile.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets the exact original expense plan.</summary>
    public Schedule Plan { get; }
    /// <summary>Gets the complete original bill account, including currency and ownership facts.</summary>
    public Account Account { get; }
    /// <summary>Gets complete original history and independent tag values used by the advance/refund preview.</summary>
    public List<LedgerEntry> Entries { get; }
    /// <summary>Checks cached access without serializing financial rows on the presentation thread.</summary>
    public void EnsureAccessCurrent() => _access.EnsureSnapshotCurrent();
    /// <summary>Rejects edited caller rows or cached access before publication and each original writer boundary.</summary>
    internal void EnsureUnchanged()
    {
        EnsureAccessCurrent();
        if (_stamp != Capture(Plan, Account, Entries)) throw new InvalidOperationException("The original bill review changed.");
    }
    /// <summary>Rejects a replacement file or a substituted plan object before acquiring correction authority.</summary>
    internal void RequireOriginal(Schedule plan, ZananceDbContext db)
    {
        if (DatabaseFileIdentity.ConfiguredPath(db) != DatabasePath || !ReferenceEquals(plan, Plan))
            throw new InvalidOperationException("The original bill source was replaced.");
        EnsureUnchanged();
    }
    /// <summary>Compares every original plan/account/history field under the same actual-file read or write transaction.</summary>
    internal void Validate(Schedule plan, Account account, List<LedgerEntry> entries)
    {
        EnsureUnchanged();
        if (_stamp != Capture(plan, account, entries)) throw new InvalidOperationException("The original bill source changed; review it again.");
    }
    private static string Capture(Schedule plan, Account account, List<LedgerEntry> entries) => JsonSerializer.Serialize(new { Plan = plan, Account = account, Entries = entries });
}

public sealed partial class ZananceStore
{
    /// <summary>Reads original plan/account/complete history on one file/worker/transaction before enabling bill input.</summary>
    public async Task<AdvanceSettlementDraft> ReadAdvanceSettlementDraftAsync(Guid planId, CancellationToken ct = default,
        Func<string, bool>? acceptsFile = null)
    {
        if (planId == Guid.Empty) throw new ArgumentException("An original bill plan is required.", nameof(planId));
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original bill cannot follow a replacement file.");
        var access = new CommercialFileAccess(commercialAccess, path); access.EnsureSnapshotCurrent();
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var plan = await db.Schedules.AsNoTracking().SingleAsync(p => p.Id == planId, ct);
            if (plan.Kind != EntryKind.Expense) throw new InvalidOperationException("Only an original expense plan has advance bills.");
            var account = await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == plan.AccountId, ct);
            var entries = await db.Entries.AsNoTracking().OrderBy(e => e.Id).ToListAsync(ct);
            var result = new AdvanceSettlementDraft(path, plan, account, entries, access);
            ct.ThrowIfCancellationRequested(); result.EnsureUnchanged(); return result;
        }, ct);
    }

    /// <summary>Revalidates the complete original bill packet on its same actual-file worker before publication.</summary>
    public async Task ValidateAdvanceSettlementDraftAsync(AdvanceSettlementDraft draft, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var current = await ReadAdvanceSettlementDraftAsync(draft.Plan.Id, ct, path => path == draft.DatabasePath);
        await Task.Run(() => { ct.ThrowIfCancellationRequested(); draft.Validate(current.Plan, current.Account, current.Entries); }, ct);
    }
}
