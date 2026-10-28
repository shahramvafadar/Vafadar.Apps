using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Complete original account metadata and original ordered ledger used by the conversion preview.</summary>
public sealed record AccountConversionInputs(Account Account, List<LedgerEntry> Entries);

/// <summary>Owns the exact original-file conversion preview and one atomic writer, never a caller-authored basis.</summary>
public sealed class AccountConversionReview
{
    private readonly CommercialFileAccess _access;
    private readonly string _stamp;
    private readonly Guid _accountId;
    private int _state;

    /// <summary>Captures the complete detached source on the read worker before any native confirmation.</summary>
    internal AccountConversionReview(string path, CommercialFileAccess access, DateOnly date, AccountConversionInputs inputs)
    {
        DatabasePath = path; _access = access; Date = date; Inputs = inputs; _accountId = inputs.Account.Id;
        _stamp = Stamp(inputs); Balance = LedgerCalculator.Balance(inputs.Account, inputs.Entries, date);
    }

    /// <summary>Gets the original configured file; a preview cannot follow a subsequently selected profile.</summary>
    public string DatabasePath { get; }
    /// <summary>Gets the original reviewed account identity independently of later caller mutation.</summary>
    public Guid AccountId => _accountId;
    /// <summary>Gets the original explicit conversion date.</summary>
    public DateOnly Date { get; }
    /// <summary>Gets the original calculator's recorded balance, preserving the existing nonpositive/unknown-value rule.</summary>
    public long Balance { get; }
    /// <summary>Gets original detached source facts; changing any field invalidates conversion authority.</summary>
    public AccountConversionInputs Inputs { get; }

    /// <summary>Claims only one unconsumed writer after verifying original cached access and all caller facts.</summary>
    internal void Begin()
    {
        RequireOriginal();
        if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            throw new AccountConversionChangedException("The original conversion review is busy or already consumed.");
    }

    /// <summary>A known commit consumes the review even when a later notification or navigation fails.</summary>
    internal void End(bool committed) => Volatile.Write(ref _state, committed ? 2 : 0);

    /// <summary>Rejects a different actual file without opening or keying it.</summary>
    internal void RequireFile(ZananceDbContext db)
    {
        RequireOriginal();
        if (DatabasePath != DatabaseFileIdentity.ConfiguredPath(db))
            throw new AccountConversionChangedException("The original conversion belongs to another file.");
    }

    private void RequireOriginal()
    {
        _access.EnsureSnapshotCurrent();
        if (_stamp != Stamp(Inputs)) throw new AccountConversionChangedException("The original conversion inputs were modified.");
    }

    /// <summary>Compares complete original facts inside the actual writer; only its own archive transition may differ.</summary>
    internal async Task ValidateAsync(ZananceDbContext db, CancellationToken ct, DateTimeOffset? ownArchiveUpdatedAt = null)
    {
        RequireFile(db);
        var current = await ReadInputsAsync(db, _accountId, ct)
            ?? throw new AccountConversionChangedException("The original asset account no longer exists.");
        if (ownArchiveUpdatedAt is { } expectedAudit)
        {
            if (!current.Account.IsArchived || Inputs.Account.IsArchived || current.Account.UpdatedAt != expectedAudit)
                throw new AccountConversionChangedException("The conversion's own archive transition is missing.");
            // D-628: normalize only the proved own archive flag/audit on a detached copy; other fields must stay exact.
            current.Account.IsArchived = false; current.Account.UpdatedAt = Inputs.Account.UpdatedAt;
        }
        if (_stamp != Stamp(current)) throw new AccountConversionChangedException("The original preview source changed; review it again.");
        RequireOriginal();
    }

    /// <summary>Reads all original mapped ledger fields and its qualified original order, without cache repair or initialization.</summary>
    internal static async Task<AccountConversionInputs?> ReadInputsAsync(ZananceDbContext db, Guid id, CancellationToken ct)
    {
        var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, ct);
        return account is null ? null : new(account, await ZananceStore.ReadCompleteHomeLedgerAsync(db, ct));
    }

    private static string Stamp(AccountConversionInputs inputs)
    {
        using var hash = SHA256.Create();
        using var output = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        JsonSerializer.Serialize(output, inputs, AccountConversionJson.Default.AccountConversionInputs);
        output.FlushFinalBlock(); return Convert.ToHexString(hash.Hash!);
    }
}

/// <summary>The original account conversion lost source, file, access or page authority; explicit review is required.</summary>
public sealed class AccountConversionChangedException(string message) : InvalidOperationException(message);

/// <summary>Explicit complete original conversion identities for trimmed/AOT applications.</summary>
[JsonSerializable(typeof(AccountConversionInputs))]
internal sealed partial class AccountConversionJson : JsonSerializerContext;

public sealed partial class HoldingStore
{
    /// <summary>Checks a retained accepted receipt's original file/access without replaying its write or rereading an archived source.</summary>
    public async Task ValidateAccountConversionFileAsync(AccountConversionReview review, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(review);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await Task.Run(() => { ct.ThrowIfCancellationRequested(); review.RequireFile(db); }, ct);
    }

    /// <summary>Captures one coherent original asset preview off UI; ineligible accounts do not create settings or holdings.</summary>
    public async Task<AccountConversionReview?> ReadAccountConversionAsync(Guid id, DateOnly date, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var path = DatabaseFileIdentity.ConfiguredPath(db); var access = new CommercialFileAccess(commercialAccess, path);
        access.EnsureSnapshotCurrent();
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            var inputs = await AccountConversionReview.ReadInputsAsync(db, id, ct);
            if (inputs is null || inputs.Account.Type != AccountType.Asset || inputs.Account.IsArchived) return null;
            access.DemandFeature(CommercialFeature.ManageHoldings); ct.ThrowIfCancellationRequested(); access.EnsureSnapshotCurrent();
            return new AccountConversionReview(path, access, date, inputs);
        }, ct);
    }

    /// <summary>Revalidates the exact displayed source without acquiring financial write authority.</summary>
    public async Task ValidateAccountConversionAsync(AccountConversionReview review, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(review);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await Task.Run(async () =>
        {
            review.RequireFile(db);
            await db.Database.OpenConnectionAsync(ct);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, ct);
            await review.ValidateAsync(db, ct);
        }, ct);
    }
}
