using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Data;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Sharing;

/// <summary>One existing cash movement available for explicit linkage; no original counterpart identity is inferred.</summary>
public sealed record HouseholdCashMovement(Guid Id, DateOnly Date, EntryKind Kind, long Amount, string CurrencyCode,
    long? DestinationAmount, string? DestinationCurrencyCode, string? Title)
{
    /// <summary>Opaque complete original cash/account identity; it grants no private access or write authority.</summary>
    internal string? OriginalCashToken { get; init; }
}

/// <summary>Captured cost review; its immutable private token is rechecked in the actual financial writer before any edit.</summary>
public sealed class HouseholdCostReview
{
    internal HouseholdCostReview(string path, HouseholdCostState state, bool canWrite)
    {
        Path = path; Token = state.Fingerprint(); CanWrite = canWrite; Contacts = Array.AsReadOnly(state.Contacts);
        Costs = Array.AsReadOnly(state.Costs); Allocations = Array.AsReadOnly(state.Allocations);
        Settlements = Array.AsReadOnly(state.Settlements); Totals = HouseholdCostLedger.Totals(state.Costs);
        Members = HouseholdCostLedger.Members(state.Contacts, state.Costs, state.Allocations, state.Settlements);
        LinkedMovements = state.Entries.Values.Select(row => HouseholdCostStore.CaptureMovement(row, state.Accounts[row.AccountId],
            row.ToAccountId is { } destination && state.Accounts.TryGetValue(destination, out var account) ? account : null)).ToArray();
    }
    internal string Path { get; }
    internal string Token { get; }
    /// <summary>Current display hint only; every later Save captures its own member/file/clock permission under the writer.</summary>
    public bool CanWrite { get; }
    /// <summary>Explicit financial contacts; these rows never grant membership or authenticate an identity.</summary>
    public IReadOnlyList<HouseholdContact> Contacts { get; }
    /// <summary>Actual expense/refund facts; shared cash movements are linked, never added again.</summary>
    public IReadOnlyList<HouseholdCost> Costs { get; }
    /// <summary>Exact saved shares, with no automatic equal split or inferred ownership.</summary>
    public IReadOnlyList<HouseholdCostAllocation> Allocations { get; }
    /// <summary>Confirmed private repayments, separate from shared cash-account ownership.</summary>
    public IReadOnlyList<HouseholdSettlement> Settlements { get; }
    /// <summary>One total per actual currency without inferred exchange rates.</summary>
    public IReadOnlyList<HouseholdCostTotal> Totals { get; }
    /// <summary>Cost attribution and private advances remain separate display facts.</summary>
    public IReadOnlyList<HouseholdMemberCostSummary> Members { get; }
    /// <summary>Original linked cash facts from the same read snapshot, never an automatically chosen replacement.</summary>
    public IReadOnlyList<HouseholdCashMovement> LinkedMovements { get; }
    /// <inheritdoc />
    public override string ToString() => "Household cost review";
}

/// <summary>Atomic, reviewed household cost edits; no operation here creates, changes or duplicates a cash ledger entry.</summary>
public sealed class HouseholdCostStore(IDbContextFactory<ZananceDbContext> factory, ICommercialWriteAccessSource commercial)
{
    /// <summary>Captures the original protected generation before worker work and reads one coherent native snapshot.</summary>
    public async Task<HouseholdCostReview> ReadAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var sourceAccess = new CommercialFileAccess(commercial, DatabaseFileIdentity.ConfiguredPath(db));
        return await Task.Run(async () =>
        {
            if (db.Database.GetDbConnection() is not SqliteConnection connection) throw new InvalidOperationException("Native household storage is required.");
            ProtectedSqliteAccess.Require(connection); await db.Database.OpenConnectionAsync(ct);
            using var transaction = connection.BeginTransaction(deferred: true);
            await using var borrowed = await db.Database.UseTransactionAsync(transaction, ct);
            var state = HouseholdCostState.Read(db); state.DemandValid(); ProtectedSqliteAccess.Require(connection);
            var permission = await HouseholdJournal.CaptureLocalWritePermissionAsync(db, db.WorkClock, ct); var canWrite = false;
            try { permission?.Demand(); canWrite = permission is not null; } catch (HouseholdWriteRejectedException) { }
            sourceAccess.RequireFile(DatabaseFileIdentity.ConfiguredPath(db)); ProtectedSqliteAccess.Require(connection); ct.ThrowIfCancellationRequested();
            return new HouseholdCostReview(DatabaseFileIdentity.ConfiguredPath(db), state, canWrite);
        }, ct);
    }

    /// <summary>Finds existing actual movements on the explicit draft date; an overfull result requires a narrower title search.</summary>
    public async Task<IReadOnlyList<HouseholdCashMovement>> FindMovementsAsync(DateOnly date, EntryKind kind, string? search, CancellationToken ct = default,
        Action? validateOriginal = null)
    {
        if (kind is not (EntryKind.Expense or EntryKind.Refund or EntryKind.Transfer) || search?.Length > 200)
            throw new ArgumentException("An explicit bounded household movement search is required.");
        validateOriginal?.Invoke();
        await using var db = await factory.CreateDbContextAsync(ct);
        var sourceAccess = new CommercialFileAccess(commercial, DatabaseFileIdentity.ConfiguredPath(db));
        return await Task.Run(async () =>
        {
            if (db.Database.GetDbConnection() is not SqliteConnection connection) throw new InvalidOperationException("Native household storage is required.");
            validateOriginal?.Invoke(); ProtectedSqliteAccess.Require(connection); await db.Database.OpenConnectionAsync(ct);
            using var transaction = connection.BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var rows = await ReadMovementsInContextAsync(db, date, kind, search, ct);
            sourceAccess.RequireFile(DatabaseFileIdentity.ConfiguredPath(db)); validateOriginal?.Invoke();
            ProtectedSqliteAccess.Require(connection); ct.ThrowIfCancellationRequested(); return rows;
        }, ct);
    }

    /// <summary>Materializes complete original rows under the caller's single source transaction; no private grant is inferred.</summary>
    internal static async Task<IReadOnlyList<HouseholdCashMovement>> ReadMovementsInContextAsync(ZananceDbContext db,
        DateOnly date, EntryKind kind, string? search, CancellationToken ct)
    {
        if (kind is not (EntryKind.Expense or EntryKind.Refund or EntryKind.Transfer) || search?.Length > 200)
            throw new ArgumentException("An explicit bounded household movement search is required.");
        var query = db.Entries.AsNoTracking().Where(row => row.Date == date && row.Kind == kind);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(row => row.Title != null && row.Title.Contains(search));
        var rows = await (from row in query
            join account in db.Accounts.AsNoTracking() on row.AccountId equals account.Id
            join destination in db.Accounts.AsNoTracking() on row.ToAccountId equals destination.Id into destinations
            from destination in destinations.DefaultIfEmpty()
            orderby row.Id
            select new { Entry = row, Account = account, Destination = destination }).Take(101).ToArrayAsync(ct);
        if (rows.Length > 100) throw new InvalidOperationException("Narrow the household movement title search.");
        return Array.AsReadOnly(rows.Select(row => CaptureMovement(row.Entry, row.Account, row.Destination)).ToArray());
    }

    /// <summary>Retains the existing public movement caption with a complete immutable source token.</summary>
    internal static HouseholdCashMovement CaptureMovement(LedgerEntry row, Account account, Account? destination) => new(row.Id, row.Date,
        row.Kind, row.Amount, account.CurrencyCode, row.ToAmount, destination?.CurrencyCode, row.Title)
        { OriginalCashToken = HouseholdCostState.CashFingerprint([row], destination is null ? [account] : [account, destination]) };

    /// <summary>Rechecks the exact selected original cash facts inside the writer, independently of graph validity.</summary>
    internal static async Task RequireMovementInContextAsync(ZananceDbContext db, HouseholdCashMovement selected, Guid? expectedId, CancellationToken ct)
    {
        if (expectedId != selected.Id || selected.OriginalCashToken is null)
            throw new InvalidOperationException("The original household cash selection is unavailable.");
        var row = await db.Entries.AsNoTracking().SingleOrDefaultAsync(row => row.Id == selected.Id, ct)
            ?? throw new InvalidOperationException("The selected original cash movement is unavailable.");
        var account = await db.Accounts.AsNoTracking().SingleAsync(account => account.Id == row.AccountId, ct);
        var destination = row.ToAccountId is { } id ? await db.Accounts.AsNoTracking().SingleOrDefaultAsync(account => account.Id == id, ct) : null;
        if (CaptureMovement(row, account, destination).OriginalCashToken != selected.OriginalCashToken)
            throw new InvalidOperationException("The selected original cash facts changed after review.");
    }

    /// <summary>Saves an explicit contact name/archive choice without changing household service access.</summary>
    public Task SaveContactAsync(HouseholdCostReview review, HouseholdContact contact, CancellationToken ct = default,
        Func<bool>? isCurrent = null, Action? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(contact);
        var draft = new HouseholdContact(contact.Id) { Name = contact.Name, IdentityId = contact.IdentityId, IsArchived = contact.IsArchived };
        return WriteAsync(review, async db =>
        {
            var original = await db.HouseholdContacts.SingleOrDefaultAsync(row => row.Id == draft.Id, ct);
            if (original is null) db.HouseholdContacts.Add(draft);
            else { original.Name = draft.Name; original.IdentityId = draft.IdentityId; original.IsArchived = draft.IsArchived; }
        }, ct, isCurrent, accepted);
    }

    /// <summary>Publishes the complete cost and exact allocations together; a stale review or invalid sum rolls everything back.</summary>
    public Task SaveCostAsync(HouseholdCostReview review, HouseholdCost cost, IReadOnlyList<HouseholdCostAllocation> allocations,
        CancellationToken ct = default, Func<bool>? isCurrent = null, Action? accepted = null, HouseholdCashMovement? reviewedMovement = null)
    {
        ArgumentNullException.ThrowIfNull(cost); ArgumentNullException.ThrowIfNull(allocations);
        var draft = new HouseholdCost(cost.Id) { Kind = cost.Kind, Date = cost.Date, Amount = cost.Amount, CurrencyCode = cost.CurrencyCode,
            PaidByContactId = cost.PaidByContactId, PaymentKind = cost.PaymentKind, EntryId = cost.EntryId,
            RefundOfCostId = cost.RefundOfCostId, Title = cost.Title, Note = cost.Note };
        var shares = allocations.Select(row => new HouseholdCostAllocation { CostId = row.CostId, ContactId = row.ContactId, Amount = row.Amount }).ToArray();
        if (shares.Any(row => row.CostId != draft.Id) || shares.Select(row => row.ContactId).Distinct().Count() != shares.Length)
            throw new HouseholdCostRejectedException([new(draft.Id, "InvalidHouseholdAllocation")]);
        return WriteAsync(review, async db =>
        {
            var original = await db.HouseholdCosts.SingleOrDefaultAsync(row => row.Id == draft.Id, ct);
            if (original is null) db.HouseholdCosts.Add(draft);
            else
            {
                original.Kind = draft.Kind; original.Date = draft.Date; original.Amount = draft.Amount; original.CurrencyCode = draft.CurrencyCode;
                original.PaidByContactId = draft.PaidByContactId; original.PaymentKind = draft.PaymentKind; original.EntryId = draft.EntryId;
                original.RefundOfCostId = draft.RefundOfCostId; original.Title = draft.Title; original.Note = draft.Note;
            }
            var oldShares = await db.HouseholdAllocations.Where(row => row.CostId == draft.Id).ToListAsync(ct);
            db.HouseholdAllocations.RemoveRange(oldShares.Where(row => !shares.Any(next => next.ContactId == row.ContactId)));
            foreach (var row in shares)
            {
                var old = oldShares.SingleOrDefault(item => item.ContactId == row.ContactId);
                if (old is null) db.HouseholdAllocations.Add(row); else old.Amount = row.Amount;
            }
        }, ct, isCurrent, accepted, reviewedMovement, draft.EntryId);
    }

    /// <summary>Records only a confirmed person-to-person repayment, optionally linked to one existing matching transfer side.</summary>
    public Task SaveSettlementAsync(HouseholdCostReview review, HouseholdSettlement settlement, CancellationToken ct = default,
        Func<bool>? isCurrent = null, Action? accepted = null, HouseholdCashMovement? reviewedMovement = null)
    {
        ArgumentNullException.ThrowIfNull(settlement);
        var draft = new HouseholdSettlement(settlement.Id) { FromContactId = settlement.FromContactId, ToContactId = settlement.ToContactId,
            Date = settlement.Date, Amount = settlement.Amount, CurrencyCode = settlement.CurrencyCode, EntryId = settlement.EntryId,
            UsesDestinationAmount = settlement.UsesDestinationAmount, Note = settlement.Note };
        return WriteAsync(review, async db =>
        {
            var original = await db.HouseholdSettlements.SingleOrDefaultAsync(row => row.Id == draft.Id, ct);
            if (original is null) db.HouseholdSettlements.Add(draft);
            else
            {
                original.FromContactId = draft.FromContactId; original.ToContactId = draft.ToContactId; original.Date = draft.Date;
                original.Amount = draft.Amount; original.CurrencyCode = draft.CurrencyCode; original.EntryId = draft.EntryId;
                original.UsesDestinationAmount = draft.UsesDestinationAmount; original.Note = draft.Note;
            }
        }, ct, isCurrent, accepted, reviewedMovement, draft.EntryId);
    }

    /// <summary>Removes only the explicitly reviewed cost/shares; retained refunds prevent removing their original.</summary>
    public Task DeleteCostAsync(HouseholdCostReview review, Guid id, CancellationToken ct = default,
        Func<bool>? isCurrent = null, Action? accepted = null) => WriteAsync(review, async db =>
    {
        var row = await db.HouseholdCosts.SingleAsync(row => row.Id == id, ct); db.HouseholdCosts.Remove(row);
    }, ct, isCurrent, accepted);

    /// <summary>Removes the explicit repayment record without deleting or changing its linked cash movement.</summary>
    public Task DeleteSettlementAsync(HouseholdCostReview review, Guid id, CancellationToken ct = default,
        Func<bool>? isCurrent = null, Action? accepted = null) => WriteAsync(review, async db =>
    {
        var row = await db.HouseholdSettlements.SingleAsync(row => row.Id == id, ct); db.HouseholdSettlements.Remove(row);
    }, ct, isCurrent, accepted);

    private async Task WriteAsync(HouseholdCostReview review, Func<ZananceDbContext, Task> edit, CancellationToken ct,
        Func<bool>? isCurrent = null, Action? accepted = null, HouseholdCashMovement? reviewedMovement = null, Guid? expectedEntryId = null)
    {
        ArgumentNullException.ThrowIfNull(review);
        void Current()
        { ct.ThrowIfCancellationRequested(); if (isCurrent?.Invoke() == false) throw new InvalidOperationException("The original household financial draft retired."); }
        Current();
        await using var db = await factory.CreateDbContextAsync(ct);
        await Task.Run(async () =>
        {
            if (DatabaseFileIdentity.ConfiguredPath(db) != review.Path) throw new InvalidOperationException("The household profile changed after review.");
            Current();
            await using var writer = await CommercialWriteTransaction.OpenAsync(db, commercial, ct, requireTransaction: true);
            writer.DemandHousehold(); var current = HouseholdCostState.Read(db);
            if (current.Fingerprint() != review.Token) throw new InvalidOperationException("The household costs changed after review.");
            Current();
            if (reviewedMovement is not null) { await RequireMovementInContextAsync(db, reviewedMovement, expectedEntryId, ct); Current(); }
            await edit(db); await db.SaveChangesAsync(ct); Current();
            if (reviewedMovement is not null) { await RequireMovementInContextAsync(db, reviewedMovement, expectedEntryId, ct); Current(); }
            // Original context commit interceptor rechecks graph/link and exact member/clock authority after every compound write.
            await writer.CommitAsync(ct);
            // Record only the returned original commit, before disposal or other fallible
            // follow-through. This callback carries no key, role or reusable write grant.
            accepted?.Invoke();
        }, ct);
    }
}
