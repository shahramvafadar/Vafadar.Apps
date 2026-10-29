using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Captures the actual parent publication's file before the first native account-editor read can await.</summary>
public interface IAccountEditorOrigin
{
    /// <summary>Gets the original configured source file, never a subsequently selected profile.</summary>
    string DatabasePath { get; }
    /// <summary>Requires the original cached access and publication prerequisites before opening the form.</summary>
    void EnsureCurrent();
}

/// <summary>Retains only the original parent's file/access/day, never its potentially large ledger graph.</summary>
public sealed class AccountEditorOrigin : IAccountEditorOrigin
{
    private readonly CommercialFileAccess _access;
    private readonly TimeProvider? _time;
    private readonly DateOnly? _day;
    /// <summary>Captures the exact original lease identity without retaining financial row collections.</summary>
    internal AccountEditorOrigin(string path, CommercialFileAccess access, TimeProvider? time = null, DateOnly? day = null)
    { DatabasePath = path; _access = access; _time = time; _day = day; }
    /// <summary>Gets the original actual file before the editor's first await.</summary>
    public string DatabasePath { get; }
    /// <summary>Rechecks only the captured parent's original cached rights and optional day boundary.</summary>
    public void EnsureCurrent()
    {
        _access.EnsureSnapshotCurrent();
        if (_time is not null && _day != DateOnly.FromDateTime(_time.GetLocalNow().DateTime))
            throw new AccountEditorUnavailableException();
    }
}

/// <summary>Complete original account definitions, optional settings and the requested account's currency constraints.</summary>
public sealed record AccountEditorInputs(ZananceSettings? Settings, List<Account> Accounts, AccountCurrencyLock CurrencyLock);

/// <summary>Owns one original account form's actual file, complete read inputs and coherent quota explanation.</summary>
public sealed class AccountEditorSnapshot
{
    private readonly CommercialFileAccess _access;
    private readonly string _stamp;
    private readonly string? _defaultStamp;

    /// <summary>Captures immutable small-input identities on the original reader worker.</summary>
    internal AccountEditorSnapshot(string path, CommercialFileAccess access, Guid? requestedId,
        AccountEditorInputs inputs, EditorWorkReview work)
    {
        DatabasePath = path; _access = access; RequestedId = requestedId; Inputs = inputs; Work = work;
        Settings = inputs.Settings ?? new ZananceSettings(); _stamp = Capture(inputs);
        _defaultStamp = inputs.Settings is null ? JsonSerializer.Serialize(Settings, AccountEditorJson.Default.ZananceSettings) : null;
    }

    /// <summary>Gets the original actual file; a later retry cannot follow another selected profile.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets the requested original edit identity, or null only for an explicit new-account route.</summary>
    public Guid? RequestedId { get; }
    /// <summary>Gets detached complete original facts; caller mutation invalidates later publication and writes.</summary>
    public AccountEditorInputs Inputs { get; }
    /// <summary>Gets original settings or the same nonpersisted defaults; reads never create a settings row.</summary>
    public ZananceSettings Settings { get; }
    /// <summary>Gets the original readiness explanation captured within the same read transaction.</summary>
    public EditorWorkReview Work { get; }
    /// <summary>Checks original cached ownership immediately before a synchronous form publication.</summary>
    public void EnsureCurrent() => _access.EnsureSnapshotCurrent();

    /// <summary>Requires an unchanged original stored account before refreshing a retained unsaved form.</summary>
    public bool MatchesAccount(Account account) => RequestedId is null || Inputs.Accounts.SingleOrDefault(row => row.Id == RequestedId) is { } original
        && JsonSerializer.Serialize(original, AccountEditorJson.Default.Account) == JsonSerializer.Serialize(account, AccountEditorJson.Default.Account);

    /// <summary>Admits explicit refreshed rights only with the same file and every unchanged detached/persisted input.</summary>
    public bool HasSameInputs(AccountEditorSnapshot previous)
    {
        ArgumentNullException.ThrowIfNull(previous); EnsureCurrent();
        // An explicit readiness review may replace expired quota rights, but cannot replace original source facts.
        return DatabasePath == previous.DatabasePath && RequestedId == previous.RequestedId
            && _stamp == Capture(Inputs) && previous._stamp == Capture(previous.Inputs) && _stamp == previous._stamp
            && (_defaultStamp is null || _defaultStamp == JsonSerializer.Serialize(Settings, AccountEditorJson.Default.ZananceSettings))
            && (previous._defaultStamp is null || previous._defaultStamp == JsonSerializer.Serialize(previous.Settings, AccountEditorJson.Default.ZananceSettings));
    }

    /// <summary>Refuses another file and any changed detached source without granting a write.</summary>
    internal void RequireOriginal(ZananceDbContext db)
    {
        RequireFile(db);
        if (_stamp != Capture(Inputs) || _defaultStamp is not null
            && _defaultStamp != JsonSerializer.Serialize(Settings, AccountEditorJson.Default.ZananceSettings))
            throw new InvalidOperationException("The original account editor input was changed in memory.");
    }

    /// <summary>Checks original file/access without comparing metadata changed by this already accepted account write.</summary>
    internal void RequireFile(ZananceDbContext db) => _access.RequireFile(DatabaseFileIdentity.ConfiguredPath(db));

    /// <summary>Requires every original persisted field and currency constraint to remain unchanged.</summary>
    internal void Compare(AccountEditorInputs fresh)
    {
        if (_stamp != Capture(fresh)) throw new InvalidOperationException("The original account editor source changed.");
    }

    private static string Capture(AccountEditorInputs inputs) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(inputs, AccountEditorJson.Default.AccountEditorInputs)));
}

/// <summary>Refuses an unavailable requested edit instead of exposing an unrelated new-account form.</summary>
public sealed class AccountEditorUnavailableException() : InvalidOperationException("The requested original account is unavailable.");

/// <summary>Explicit AOT metadata for the complete original small account-editing packet.</summary>
[JsonSerializable(typeof(AccountEditorInputs))]
[JsonSerializable(typeof(ZananceSettings))]
[JsonSerializable(typeof(Account))]
internal sealed partial class AccountEditorJson : JsonSerializerContext;

public sealed partial class ZananceStore
{
    /// <summary>Checks the original accepted form's file/access for its normal return without querying or changing rows.</summary>
    public async Task ValidateAccountEditorAccessAsync(AccountEditorSnapshot source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested(); source.RequireFile(db);
    }

    /// <summary>Reads original account fields, settings, locks and work on one captured lease and deferred worker transaction.</summary>
    public async Task<AccountEditorSnapshot> ReadAccountEditorAsync(Guid? requestedId,
        Func<string, bool>? acceptsFile = null, CancellationToken cancellationToken = default)
    {
        if (requestedId == Guid.Empty) throw new AccountEditorUnavailableException();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original account editor file changed.");
        var access = new CommercialFileAccess(commercialAccess, path); access.DemandFeature(CommercialFeature.History);
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var reader = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(reader, cancellationToken);
            var inputs = await ReadAccountEditorInputsAsync(db, requestedId, cancellationToken);
            var work = await EditorWorkStore.ReadContextAsync(db, access, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested(); access.EnsureSnapshotCurrent();
            return new AccountEditorSnapshot(path, access, requestedId, inputs, work);
        }, cancellationToken);
    }

    /// <summary>Revalidates complete original inputs under one fresh same-file read before uncovering or continuing a form.</summary>
    public async Task ValidateAccountEditorAsync(AccountEditorSnapshot source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await Task.Run(async () =>
        {
            source.RequireOriginal(db); await db.Database.OpenConnectionAsync(cancellationToken);
            await using var reader = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(reader, cancellationToken);
            await ValidateOriginalAccountEditorAsync(source, db, cancellationToken);
        }, cancellationToken);
    }

    private static async Task<AccountEditorInputs> ReadAccountEditorInputsAsync(ZananceDbContext db, Guid? requestedId, CancellationToken ct)
    {
        var settings = await SettingsRowReader.ReadAsync(db, ct);
        var accounts = await db.Accounts.AsNoTracking().OrderBy(row => row.IsArchived).ThenBy(row => row.SortOrder)
            .ThenBy(row => row.Name).ThenBy(row => row.Id).ToListAsync(ct);
        var account = requestedId is { } id ? accounts.SingleOrDefault(row => row.Id == id) : null;
        if (requestedId is not null && account is null) throw new AccountEditorUnavailableException();
        var currencyLock = account is null ? AccountCurrencyLock.None : await CurrencyLockAsync(db, account, ct);
        return new(settings, accounts, currencyLock);
    }

    private static async Task ValidateOriginalAccountEditorAsync(AccountEditorSnapshot source, ZananceDbContext db, CancellationToken ct)
    {
        source.RequireOriginal(db); source.Compare(await ReadAccountEditorInputsAsync(db, source.RequestedId, ct));
        ct.ThrowIfCancellationRequested(); source.RequireOriginal(db);
    }
}
