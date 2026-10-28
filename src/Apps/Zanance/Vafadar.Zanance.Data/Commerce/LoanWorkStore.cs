using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Coherent original debt/history inputs and actual-file rights, separate from new loan analysis.</summary>
public sealed class LoanReadReview
{
    private readonly CommercialFileAccess _access;
    private readonly TimeProvider _time;
    private readonly string _packet;
    private readonly bool _hasSettings;
    /// <summary>Retains the exact configured file for native destination and writer validation.</summary>
    internal readonly string DatabasePath;
    /// <summary>Captures complete original inputs before any native choice or confirmation.</summary>
    internal LoanReadReview(string path, CommercialFileAccess access, TimeProvider time, DateOnly day, ZananceSettings? settings,
        List<Account> accounts, List<LedgerEntry> entries, List<Category> categories)
    {
        DatabasePath = path; _access = access; _time = time; Day = day; _hasSettings = settings is not null; Settings = settings ?? new();
        Accounts = accounts; Entries = entries; Categories = categories; _packet = Packet(settings, accounts, entries, categories);
    }
    /// <summary>Gets the reviewed local day used for original balances and a confirmed installment.</summary>
    public DateOnly Day { get; }
    /// <summary>Gets original settings, with nonpersisted defaults when no row exists.</summary>
    public ZananceSettings Settings { get; }
    /// <summary>Gets all original accounts independently of active-item financial filtering.</summary>
    public IReadOnlyList<Account> Accounts { get; }
    /// <summary>Gets complete original financial history.</summary>
    public IReadOnlyList<LedgerEntry> Entries { get; }
    /// <summary>Gets original category identities for the established interest-entry classification.</summary>
    public IReadOnlyList<Category> Categories { get; }
    /// <summary>Returns the original approved feature permission; inactive test builds remain unrestricted.</summary>
    public FeaturePermission Permission(CommercialFeature feature) => _access.Context is { } context ? PlanPolicy.Check(feature, context) : FeaturePermission.Allowed;
    /// <summary>Rejects retired access or a changed local day without silently rebasing a confirmed payment.</summary>
    public void EnsureCurrent()
    {
        _access.EnsureSnapshotCurrent();
        if (DateOnly.FromDateTime(_time.GetLocalNow().DateTime) != Day) throw new InvalidOperationException("The reviewed loan day changed.");
    }
    /// <summary>Demands the actual operation independently of UI presentation mode.</summary>
    public void Demand(CommercialFeature feature) { EnsureCurrent(); _access.DemandFeature(feature); }
    /// <summary>Returns complete recorded balance through the original calculator with retained History rights.</summary>
    public long Balance(Account account)
    {
        Demand(CommercialFeature.History);
        var original = Accounts.SingleOrDefault(item => item.Id == account.Id);
        if (original is null || JsonSerializer.Serialize(original) != JsonSerializer.Serialize(account))
            throw new InvalidOperationException("The loan account is not part of the original review.");
        return LedgerCalculator.Balance(original, Entries, Day);
    }
    /// <summary>Returns original outstanding principal; this retained financial fact is not a paid estimate.</summary>
    public long Outstanding(Account account) => LoanCalculator.Outstanding(account.Type, Balance(account));
    /// <summary>Retains the essential warning when saved installments cannot cover nominal monthly interest.</summary>
    public bool HasUnderpaymentWarning(Account account)
    {
        Demand(CommercialFeature.Warnings); var outstanding = Outstanding(account);
        return account.Type.IsDebt() && !account.IsArchived && outstanding > 0 && account.Installment is > 0
            && account.Installment.Value <= LoanCalculator.Interest(outstanding, account.InterestRate ?? 0);
    }
    /// <summary>Calls the original paid schedule calculator without persisting estimates or modifying history.</summary>
    public LoanSchedule Schedule(Account account)
    {
        Demand(CommercialFeature.LoanAnalysis); EnsureOriginalInputs();
        if (!account.Type.IsDebt() || account.IsArchived || account.Installment is not > 0)
            throw new InvalidOperationException("The original account has no usable loan terms.");
        var result = LoanCalculator.Schedule(Outstanding(account), account.InterestRate ?? 0, account.Installment.Value, Day.AddMonths(1));
        EnsureCurrent(); return result;
    }
    /// <summary>Calls the original monthly split only for authorized new analytical work.</summary>
    public (long Interest, long Principal) Split(Account account)
    { Demand(CommercialFeature.LoanAnalysis); EnsureOriginalInputs(); return LoanCalculator.Split(Outstanding(account), account.InterestRate ?? 0, account.Installment ?? 0); }
    /// <summary>Builds the original explicitly confirmed transfer/interest batch, never a caller-authored correction grant.</summary>
    internal IReadOnlyList<LedgerEntry> CreateInstallment(Guid accountId, Guid cashId, string title)
    {
        Demand(CommercialFeature.LoanAnalysis); EnsureOriginalInputs(); var account = Accounts.Single(item => item.Id == accountId);
        var cash = Accounts.Single(item => item.Id == cashId);
        if (!account.Type.IsDebt() || account.IsArchived || account.Installment is not > 0 || Outstanding(account) <= 0
            || cash.IsArchived || cash.Type.IsOutsideCash() || cash.CurrencyCode != account.CurrencyCode)
            throw new InvalidOperationException("The original confirmed loan accounts or terms are unavailable.");
        var category = account.Type == AccountType.Loan
            ? Categories.First(item => item.Kind == CategoryKind.Expense && item.SystemKey == DefaultCategories.Fees)
            : Categories.FirstOrDefault(item => item.Kind == CategoryKind.Income && item.SystemKey == "Interest")
              ?? Categories.First(item => item.Kind == CategoryKind.Income && item.SystemKey == DefaultCategories.Uncategorized);
        return LoanCalculator.CreateInstallment(account, cash.Id, Outstanding(account), account.InterestRate ?? 0,
            account.Installment.Value, Day, category.Id, title);
    }
    /// <summary>Rechecks original inputs under the real writer, excluding only this batch's own newly added ids.</summary>
    internal async Task ValidateWriterAsync(ZananceDbContext db, IReadOnlyCollection<Guid>? ownNewIds = null, CancellationToken cancellationToken = default)
    {
        if (DatabasePath != DatabaseFileIdentity.ConfiguredPath(db)) throw new InvalidOperationException("The reviewed loan belongs to another file.");
        Demand(CommercialFeature.LoanAnalysis); await ValidatePacketAsync(db, ownNewIds, cancellationToken); EnsureCurrent();
    }
    /// <summary>Rechecks all original financial packets; audit timestamps alone never authorize a stale payment.</summary>
    internal async Task ValidatePacketAsync(ZananceDbContext db, IReadOnlyCollection<Guid>? ownNewIds, CancellationToken cancellationToken)
    {
        EnsureOriginalInputs();
        var settings = await db.Settings.AsNoTracking().OrderBy(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        var accounts = await db.Accounts.AsNoTracking().ToListAsync(cancellationToken);
        var query = db.Entries.AsNoTracking();
        if (ownNewIds is not null) query = query.Where(item => !ownNewIds.Contains(item.Id));
        var entries = await query.ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        if (_packet != Packet(settings, accounts, entries, categories)) throw new InvalidOperationException("The original loan inputs changed; review them again.");
    }
    private void EnsureOriginalInputs()
    {
        if (_packet != Packet(_hasSettings ? Settings : null, Accounts, Entries, Categories))
            throw new InvalidOperationException("The original reviewed loan packet was modified.");
    }
    private static string Packet(ZananceSettings? settings, IEnumerable<Account> accounts, IEnumerable<LedgerEntry> entries, IEnumerable<Category> categories) =>
        JsonSerializer.Serialize(new { settings, accounts = accounts.OrderBy(item => item.Id), entries = entries.OrderBy(item => item.Id), categories = categories.OrderBy(item => item.Id) });
}

/// <summary>Reads coherent original loan inputs without creating Settings or changing financial data.</summary>
public sealed partial class LoanWorkStore(IDbContextFactory<ZananceDbContext> factory, ICommercialWriteAccessSource source, TimeProvider time)
{
    /// <summary>Allows explicit new reads only within the displayed original file; it grants no feature or write.</summary>
    public async Task ValidateFileAsync(LoanReadReview reviewed, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewed); await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (reviewed.DatabasePath != DatabaseFileIdentity.ConfiguredPath(db)) throw new InvalidOperationException("The original loan file changed.");
    }
    /// <summary>Captures complete original history and rights in one deferred SQLite reader.</summary>
    public async Task<LoanReadReview> ReadAsync(CancellationToken cancellationToken = default)
    {
        var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        await using var db = await factory.CreateDbContextAsync(cancellationToken); var path = DatabaseFileIdentity.ConfiguredPath(db);
        var access = new CommercialFileAccess(source, path); access.EnsureSnapshotCurrent(); access.DemandFeature(CommercialFeature.History);
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
        var settings = await db.Settings.AsNoTracking().OrderBy(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        var review = new LoanReadReview(path, access, time, day, settings, await db.Accounts.AsNoTracking().ToListAsync(cancellationToken),
            await db.Entries.AsNoTracking().ToListAsync(cancellationToken), await db.Categories.AsNoTracking().ToListAsync(cancellationToken));
        review.EnsureCurrent(); return review;
    }
    /// <summary>Rejects a retired actual file or financial packet after native choices and before destination publication.</summary>
    public async Task ValidateAsync(LoanReadReview reviewed, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewed); reviewed.EnsureCurrent(); await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (reviewed.DatabasePath != DatabaseFileIdentity.ConfiguredPath(db)) throw new InvalidOperationException("The original loan file changed.");
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
        await reviewed.ValidatePacketAsync(db, null, cancellationToken); reviewed.EnsureCurrent();
    }
}
