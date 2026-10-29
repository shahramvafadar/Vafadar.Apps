using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Complete detached account-list facts in their original display/history order, without initialization writes.</summary>
public sealed record AccountOverviewInputs(ZananceSettings? Settings, List<Account> Accounts, List<LedgerEntry> Entries,
    List<ExchangeRate> Rates, List<AssetType> Types, List<AssetEvent> Events);

/// <summary>Owns the original file, cached access and complete input identity of one covered account-list read.</summary>
public sealed class AccountOverviewSnapshot : IAccountEditorOrigin
{
    private readonly CommercialFileAccess _access;
    private readonly string _stamp;
    private readonly string _metadataStamp;
    /// <summary>Gets the transactionally captured ledger/rate/holding revision, independently of account metadata.</summary>
    internal string Revision { get; }

    /// <summary>Gets the original captured database file; a retry must never redirect to another profile.</summary>
    public string DatabasePath { get; }

    /// <summary>Gets all original detached facts; caller mutation refuses later validation instead of changing authority.</summary>
    public AccountOverviewInputs Inputs { get; }
    /// <summary>Gets the small original editor handoff without retaining this overview's ledger graph in the modal.</summary>
    public AccountEditorOrigin EditorOrigin { get; }

    /// <summary>Captures complete input identities only on the read worker, never while native rows are publishing.</summary>
    internal AccountOverviewSnapshot(string path, CommercialFileAccess access, string revision, AccountOverviewInputs inputs)
    {
        DatabasePath = path; _access = access; Revision = revision; Inputs = inputs; EditorOrigin = new(path, access);
        _stamp = Capture(inputs, AccountOverviewJson.Default.AccountOverviewInputs);
        _metadataStamp = Capture(new AccountOverviewMetadata(inputs.Settings, inputs.Accounts), AccountOverviewJson.Default.AccountOverviewMetadata);
    }

    /// <summary>Checks synchronous cached ownership immediately before covered native publication.</summary>
    public void EnsureCurrent() => _access.EnsureSnapshotCurrent();

    /// <summary>Refuses another opened profile even while commercial enforcement is inactive.</summary>
    internal void RequireFile(ZananceDbContext db)
    {
        EnsureCurrent();
        if (DatabasePath != DatabaseFileIdentity.ConfiguredPath(db))
            throw new InvalidOperationException("The original account overview belongs to another opened file.");
    }

    /// <summary>Checks caller mutation before classifying a persisted same-file edit as retryable.</summary>
    internal void RequireOriginal(ZananceDbContext db)
    {
        RequireFile(db);
        if (_stamp != Capture(Inputs, AccountOverviewJson.Default.AccountOverviewInputs))
            throw new InvalidOperationException("The original account overview input changed in memory.");
    }

    /// <summary>Accounts/settings are outside Home's financial revision and require their own exact persisted comparison.</summary>
    internal void CompareMetadata(AccountOverviewMetadata current)
    {
        if (_metadataStamp != Capture(current, AccountOverviewJson.Default.AccountOverviewMetadata))
            throw new AccountOverviewSupersededException();
    }

    private static string Capture<T>(T inputs, JsonTypeInfo<T> metadata)
    {
        // D-627: hash complete original fields on the worker without allocating a second full-ledger JSON string.
        using var hash = SHA256.Create();
        using var output = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        JsonSerializer.Serialize(output, inputs, metadata);
        output.FlushFinalBlock();
        return Convert.ToHexString(hash.Hash!);
    }
}

/// <summary>Only a persisted edit in the same original file admits a bounded fresh account-list read.</summary>
public sealed class AccountOverviewSupersededException() : InvalidOperationException("The original account overview source changed.");

/// <summary>Complete original small source tables not covered by the existing ledger/rate/holding revision triggers.</summary>
internal sealed record AccountOverviewMetadata(ZananceSettings? Settings, List<Account> Accounts);

/// <summary>Explicit AOT serialization of complete original overview input identities; no financial values are exported.</summary>
[JsonSerializable(typeof(AccountOverviewInputs))]
[JsonSerializable(typeof(AccountOverviewMetadata))]
internal sealed partial class AccountOverviewJson : JsonSerializerContext;

public sealed partial class ZananceStore
{
    /// <summary>Reads the complete account page from one captured lease and one deferred transaction off the UI thread.</summary>
    public async Task<AccountOverviewSnapshot> ReadAccountOverviewAsync(CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(commercialAccess, path);
        access.EnsureSnapshotCurrent();
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var revision = await ReadHomeFinancialRevisionAsync(db, ct);
            var settings = await SettingsRowReader.ReadAsync(db, ct);
            var accounts = await db.Accounts.AsNoTracking().OrderBy(a => a.IsArchived).ThenBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync(ct);
            var entries = await ReadCompleteHomeLedgerAsync(db, ct);
            var totalsCurrencies = LedgerCalculator.InScope(accounts, null).Where(a => !a.IsArchived)
                .Select(a => a.CurrencyCode).Distinct(StringComparer.Ordinal).Count();
            var rates = settings is { ValuationCurrencyEnabled: true } && settings.Shows(Feature.ConvertedTotals) && totalsCurrencies > 1
                ? await db.ExchangeRates.AsNoTracking().OrderByDescending(rate => rate.Date).ToListAsync(ct) : [];
            var types = await db.AssetTypes.AsNoTracking().OrderBy(t => t.IsArchived).ThenBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct);
            var activeTypes = types.Any(t => !t.IsArchived);
            var events = activeTypes ? (await db.AssetEvents.AsNoTracking().ToListAsync(ct)).OrderBy(e => e.Date).ThenBy(e => e.CreatedAt).ToList() : [];
            ct.ThrowIfCancellationRequested(); access.EnsureSnapshotCurrent();
            return new AccountOverviewSnapshot(path, access, revision, new(settings, accounts, entries, rates, types, events));
        }, ct);
    }

    /// <summary>Checks the original file/access for a retained row command without opening/keying another database.</summary>
    public async Task ValidateAccountOverviewAccessAsync(AccountOverviewSnapshot snapshot, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        ct.ThrowIfCancellationRequested(); snapshot.RequireFile(db);
    }

    /// <summary>Validates complete original inputs and the original transactional source revision before publication.</summary>
    public async Task ValidateAccountOverviewAsync(AccountOverviewSnapshot snapshot, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await Task.Run(async () =>
        {
            snapshot.RequireOriginal(db);
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            if (await ReadHomeFinancialRevisionAsync(db, ct) != snapshot.Revision)
                throw new AccountOverviewSupersededException();
            snapshot.CompareMetadata(new(await SettingsRowReader.ReadAsync(db, ct),
                await db.Accounts.AsNoTracking().OrderBy(a => a.IsArchived).ThenBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync(ct)));
            ct.ThrowIfCancellationRequested(); snapshot.RequireOriginal(db);
        }, ct);
    }
}
