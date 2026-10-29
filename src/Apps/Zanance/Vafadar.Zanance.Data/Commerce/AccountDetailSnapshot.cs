using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Every original account-detail source from one actual file/transaction; optional settings stay absent.</summary>
public sealed record AccountDetailInputs(ZananceSettings? Settings, List<Account> Accounts, List<LedgerEntry> Entries,
    List<Category> Categories, List<Goal> Goals, List<GoalAllocation> Allocations);

/// <summary>Owns complete account, loan, movement and earmark facts without granting another financial operation.</summary>
public sealed class AccountDetailSnapshot : IAccountEditorOrigin
{
    private readonly string _stamp;
    private readonly string? _defaultSettingsStamp;

    /// <summary>Captures immutable original identities on the worker before native presentation.</summary>
    internal AccountDetailSnapshot(string path, CommercialFileAccess access, TimeProvider time, DateOnly day, AccountDetailInputs inputs)
    {
        DatabasePath = path; Inputs = inputs; EditorOrigin = new(path, access, time, day);
        Loan = new LoanReadReview(path, access, time, day, inputs.Settings, inputs.Accounts, inputs.Entries, inputs.Categories);
        _stamp = CaptureInputs(inputs);
        // The original nonpersisted Settings object has its own generated identity: compare only that same captured object for caller mutation.
        _defaultSettingsStamp = inputs.Settings is null ? Capture(Loan.Settings, AccountDetailJson.Default.ZananceSettings) : null;
    }

    /// <summary>Gets the original configured file; a page's refresh cannot follow a later profile selection.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets complete detached original facts; any caller edit invalidates the publication review.</summary>
    public AccountDetailInputs Inputs { get; }
    /// <summary>Gets only the original editor file/access/day, without extending this detail's full-ledger lifetime.</summary>
    public AccountEditorOrigin EditorOrigin { get; }
    /// <summary>Gets the original loan review created from the very same transaction and original permission snapshot.</summary>
    public LoanReadReview Loan { get; }

    /// <summary>Checks cached original access and day before every synchronous publication assignment.</summary>
    public void EnsureCurrent() => Loan.EnsureCurrent();

    /// <summary>Rejects another selected file before it can be opened for this original page.</summary>
    internal void RequireFile(ZananceDbContext db)
    {
        if (DatabasePath != DatabaseFileIdentity.ConfiguredPath(db))
            throw new InvalidOperationException("The original account detail belongs to another opened file.");
        EnsureCurrent();
    }

    /// <summary>Classifies caller edits separately from a persisted same-file refresh.</summary>
    internal void RequireOriginal()
    {
        EnsureCurrent();
        if (_stamp != CaptureInputs(Inputs)
            || _defaultSettingsStamp is not null && _defaultSettingsStamp != Capture(Loan.Settings, AccountDetailJson.Default.ZananceSettings))
            throw new InvalidOperationException("The original account detail inputs were modified in memory.");
    }

    /// <summary>Admits a bounded reread only when actual persisted original source differs.</summary>
    internal void ComparePersisted(AccountDetailInputs current)
    {
        if (_stamp != CaptureInputs(current))
            throw new AccountDetailSupersededException();
    }

    private static string CaptureInputs(AccountDetailInputs input)
        => Capture(new AccountDetailIdentity(input.Settings, input.Accounts.OrderBy(item => item.Id),
            input.Entries.OrderBy(item => item.Id), input.Categories.OrderBy(item => item.Id),
            input.Goals.OrderBy(item => item.Id), input.Allocations.OrderBy(item => item.Id)), AccountDetailJson.Default.AccountDetailIdentity);

    private static string Capture<T>(T input, JsonTypeInfo<T> metadata)
    {
        using var hash = SHA256.Create();
        using var output = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        JsonSerializer.Serialize(output, input, metadata); output.FlushFinalBlock();
        return Convert.ToHexString(hash.Hash!);
    }
}

/// <summary>Only changed persisted source in the same original file admits a bounded complete account-detail retry.</summary>
public sealed class AccountDetailSupersededException() : InvalidOperationException("The original account detail source changed.");

/// <summary>Canonical comparison order only; calculators retain every original read row and its original order.</summary>
internal sealed record AccountDetailIdentity(ZananceSettings? Settings, IEnumerable<Account> Accounts, IEnumerable<LedgerEntry> Entries,
    IEnumerable<Category> Categories, IEnumerable<Goal> Goals, IEnumerable<GoalAllocation> Allocations);

/// <summary>Explicit complete original source identities for trimmed/AOT account-detail reads; no values are exported.</summary>
[JsonSerializable(typeof(AccountDetailIdentity))]
[JsonSerializable(typeof(ZananceSettings))]
internal sealed partial class AccountDetailJson : JsonSerializerContext;

public sealed partial class LoanWorkStore
{
    /// <summary>Reads every original displayed source coherently off UI, without creating settings, goals or ledger rows.</summary>
    public async Task<AccountDetailSnapshot> ReadAccountDetailAsync(CancellationToken cancellationToken = default)
    {
        var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(source, path);
        access.EnsureSnapshotCurrent(); access.DemandFeature(CommercialFeature.History);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var inputs = await ReadAccountDetailInputsAsync(db, cancellationToken);
            var result = new AccountDetailSnapshot(path, access, time, day, inputs);
            cancellationToken.ThrowIfCancellationRequested(); result.EnsureCurrent(); return result;
        }, cancellationToken);
    }

    /// <summary>Validates complete original source on a fresh worker snapshot, classifying only persisted edits as retryable.</summary>
    public async Task ValidateAccountDetailAsync(AccountDetailSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        snapshot.RequireFile(db);
        await Task.Run(async () =>
        {
            snapshot.RequireOriginal();
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var current = await ReadAccountDetailInputsAsync(db, cancellationToken);
            snapshot.ComparePersisted(current); snapshot.RequireOriginal();
        }, cancellationToken);
    }

    private static async Task<AccountDetailInputs> ReadAccountDetailInputsAsync(ZananceDbContext db, CancellationToken ct)
        => new(await db.Settings.AsNoTracking().OrderBy(item => item.CreatedAt).FirstOrDefaultAsync(ct),
            await db.Accounts.AsNoTracking().ToListAsync(ct), await db.Entries.AsNoTracking().ToListAsync(ct),
            await db.Categories.AsNoTracking().ToListAsync(ct), GoalStore.Ordered(await db.Goals.AsNoTracking().ToListAsync(ct)),
            GoalStore.OrderedAllocations(await db.GoalAllocations.AsNoTracking().ToListAsync(ct)));
}
