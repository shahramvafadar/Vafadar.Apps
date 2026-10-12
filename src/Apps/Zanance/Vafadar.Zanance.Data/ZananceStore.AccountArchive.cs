using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>One coherent original account/plan review for an explicit archive action; it grants no write.</summary>
public sealed class AccountArchiveReview
{
    /// <summary>Captures immutable original data for an exact file/day review.</summary>
    internal AccountArchiveReview(string path, CommercialFileAccess access, Account account, IReadOnlyList<Schedule> plans, DateOnly day)
    {
        Path = path; Access = access; AccountPacket = JsonSerializer.Serialize(account); PlanPacket = Packet(plans); Day = day;
        Id = account.Id; Name = account.Name; IsArchived = account.IsArchived;
        PlanNames = Array.AsReadOnly(plans.Select(plan => plan.Name).ToArray());
    }
    /// <summary>Gets the exact configured file boundary.</summary>
    internal string Path { get; }
    /// <summary>Gets original cached access, never a newly selected profile.</summary>
    internal CommercialFileAccess Access { get; }
    /// <summary>Gets complete original account values for writer revalidation.</summary>
    internal string AccountPacket { get; }
    /// <summary>Gets complete original affected plan values and identities.</summary>
    internal string PlanPacket { get; }
    /// <summary>Gets the explicitly reviewed local ending day.</summary>
    internal DateOnly Day { get; }
    /// <summary>Captures every original plan field independently of reader enumeration.</summary>
    internal static string Packet(IEnumerable<Schedule> plans) => JsonSerializer.Serialize(plans.OrderBy(plan => plan.Id));
    /// <summary>Gets the exact original account identity.</summary>
    public Guid Id { get; }
    /// <summary>Gets the complete original stored name, independent of unsaved form text.</summary>
    public string Name { get; }
    /// <summary>Gets the reviewed original archive state, never a guessed inverse.</summary>
    public bool IsArchived { get; }
    /// <summary>Gets complete original affected plan names; both active and paused plans are reviewed.</summary>
    public IReadOnlyList<string> PlanNames { get; }
}

/// <summary>Accepted account state and default cleanup from the same committed transaction.</summary>
public sealed class AccountArchiveResult
{
    private readonly Account _account;
    /// <summary>Captures the accepted state without exposing mutable internal fields.</summary>
    internal AccountArchiveResult(Account account, bool defaultCleared) { _account = account.CopyForEditing(); DefaultCleared = defaultCleared; }
    /// <summary>Gets a detached accepted account copy, never a writable internal result packet.</summary>
    public Account Account => _account.CopyForEditing();
    /// <summary>Gets whether this transaction cleared the exact archived default, without choosing another.</summary>
    public bool DefaultCleared { get; }
}

/// <summary>An archive transaction committed before its original change publication failed.</summary>
public sealed class AccountArchivePublicationException : Exception
{
    /// <summary>Retains the actual accepted result and original post-commit failure.</summary>
    internal AccountArchivePublicationException(AccountArchiveResult result, Exception inner)
        : base("The account archive committed before change publication completed.", inner) => Result = result;
    /// <summary>Gets the actual accepted outcome; consumers must never repeat its write.</summary>
    public AccountArchiveResult Result { get; }
}

public sealed partial class ZananceStore
{
    /// <summary>Reads exact original account/affected plans in a deferred reader without settings creation or mutation.</summary>
    public async Task<AccountArchiveReview> ReadAccountArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(commercialAccess, path);
        access.DemandFeature(CommercialFeature.History); var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var reader = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(reader, cancellationToken);
        var account = await db.Accounts.AsNoTracking().SingleAsync(row => row.Id == id, cancellationToken);
        var plans = account.IsArchived ? [] : await AffectedPlans(db, id).AsNoTracking().OrderBy(row => row.Id).ToListAsync(cancellationToken);
        access.EnsureSnapshotCurrent();
        return new(path, access, account, plans, day);
    }

    /// <summary>Rechecks reviewed rows under one actual writer, then atomically updates archive, plans, choices and exact default.</summary>
    public async Task<AccountArchiveResult> WriteAccountArchiveAsync(AccountArchiveReview reviewed, bool archive, PlanStore plans,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewed); ArgumentNullException.ThrowIfNull(plans);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (DatabaseFileIdentity.ConfiguredPath(db) != reviewed.Path || reviewed.IsArchived == archive)
            throw new InvalidOperationException("The archive review belongs to another file or action.");
        await plans.VerifyArchiveStoreAsync(this, reviewed.Path, cancellationToken);
        reviewed.Access.EnsureSnapshotCurrent();
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        reviewed.Access.EnsureSnapshotCurrent();
        if (DateOnly.FromDateTime(time.GetLocalNow().DateTime) != reviewed.Day)
            throw new InvalidOperationException("The reviewed archive day changed.");
        var original = await db.Accounts.AsNoTracking().SingleAsync(row => row.Id == reviewed.Id, cancellationToken);
        var affected = original.IsArchived ? [] : await AffectedPlans(db, reviewed.Id).AsNoTracking().OrderBy(row => row.Id).ToListAsync(cancellationToken);
        if (JsonSerializer.Serialize(original) != reviewed.AccountPacket || AccountArchiveReview.Packet(affected) != reviewed.PlanPacket)
            throw new InvalidOperationException("The original archive inputs changed; review them again.");
        var target = original.CopyForEditing(); target.IsArchived = archive;
        await CheckAccountWorkAsync(db, write, target, original, cancellationToken);
        var ending = archive ? affected.Select(row => row.CopyForEditing()).ToList() : [];
        foreach (var plan in ending) PlanActions.End(plan, reviewed.Day);
        if (ending.Count > 0) await PlanStore.CheckCommercialSaveAsync(db, write, ending, null, cancellationToken);
        db.Accounts.Update(target);
        foreach (var plan in ending) db.Schedules.Update(plan);
        await write.PrepareAccountChangeAsync(db, target, original, cancellationToken);
        var settings = archive ? await db.Settings.OrderBy(row => row.CreatedAt).FirstOrDefaultAsync(cancellationToken) : null;
        var clear = settings?.DefaultAccountId == target.Id;
        if (clear) settings!.DefaultAccountId = null;
        reviewed.Access.EnsureSnapshotCurrent(); write.EnsureCurrent();
        await db.SaveChangesAsync(cancellationToken);
        reviewed.Access.EnsureSnapshotCurrent();
        await write.CommitAsync(cancellationToken);
        var result = new AccountArchiveResult(target, clear);
        var failures = new List<Exception>();
        try { OnChanged(); } catch (Exception exception) when (exception is not OutOfMemoryException) { failures.Add(exception); }
        if (ending.Count > 0)
            try { plans.NotifyCombinedCommit(); } catch (Exception exception) when (exception is not OutOfMemoryException) { failures.Add(exception); }
        if (failures.Count > 0) throw new AccountArchivePublicationException(result, new AggregateException(failures));
        return result;
    }

    private static IQueryable<Schedule> AffectedPlans(ZananceDbContext db, Guid id) => db.Schedules.Where(row =>
        row.State != ScheduleState.Ended && (row.AccountId == id || row.ToAccountId == id));

    /// <summary>Shares the original account feature/capacity decision across ordinary Save and atomic archive.</summary>
    private static async Task CheckAccountWorkAsync(ZananceDbContext db, CommercialWriteTransaction write, Account account,
        Account? existing, CancellationToken cancellationToken)
    {
        write.DemandFeature(existing is null ? CommercialFeature.FinancialAccounts : CommercialFeature.Corrections);
        if (write.Enforced && !write.HasStoredAccountChoice && !account.IsArchived && (existing is null || existing.IsArchived))
        {
            // D-118: creation and unarchive consume the same slot. Existing corrections and archival stay possible
            // above quota; no chosen account, money, visibility or historical status is changed automatically.
            if (write.DemandCapacity(CommercialFeature.FinancialAccounts, QuotaKind.FinancialAccounts) is { } maximum)
            {
                var current = await db.Accounts.CountAsync(a => !a.IsArchived, cancellationToken);
                write.DemandCount(CommercialFeature.FinancialAccounts, QuotaKind.FinancialAccounts, maximum, current);
            }
        }
    }
}
