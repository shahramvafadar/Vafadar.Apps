using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Complete original transaction history and its original references, saved queries and file authority.</summary>
public sealed class TransactionsReadSnapshot
{
    private readonly string _ledgerStamp;
    /// <summary>Gets the complete original resource packet from the same SQLite snapshot as the ledger.</summary>
    public QuickResourceReview Resources { get; }
    /// <summary>Gets every original ledger field in the original newest-first order, without query filtering.</summary>
    public List<LedgerEntry> Entries { get; }
    /// <summary>Gets the captured source file; explicit Retry cannot silently adopt another profile.</summary>
    public string DatabasePath => Resources.TemplateChoice.DatabasePath;
    /// <summary>Gets the actual persisted financial revision captured before materialization.</summary>
    internal string Revision { get; }

    /// <summary>Captures complete input identity on the read worker, without exporting financial values.</summary>
    internal TransactionsReadSnapshot(QuickResourceReview resources, List<LedgerEntry> entries, string revision)
    {
        Resources = resources; Entries = entries; Revision = revision; _ledgerStamp = Capture(entries);
    }

    /// <summary>Rejects another file, retired access or mutation of the complete original detached ledger.</summary>
    internal void RequireOriginal(ZananceDbContext db)
    {
        Resources.EnsureCurrent();
        if (DatabaseFileIdentity.ConfiguredPath(db) != DatabasePath || _ledgerStamp != Capture(Entries))
            throw new InvalidOperationException("The original transaction history or file changed.");
    }

    /// <summary>Checks complete original row inputs supplied by a retained bulk presenter, independently of display order.</summary>
    public bool MatchesEntries(IReadOnlyList<LedgerEntry> entries) =>
        Capture(entries.OrderBy(entry => entry.Id).ToList()) == Capture(Entries.OrderBy(entry => entry.Id).ToList());

    private static string Capture(List<LedgerEntry> entries)
    {
        using var hash = SHA256.Create();
        using var output = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        JsonSerializer.Serialize(output, entries, TransactionsReadJson.Default.ListLedgerEntry);
        output.FlushFinalBlock(); return Convert.ToHexString(hash.Hash!);
    }
}

/// <summary>Explicit AOT metadata preserves every original ledger field, including independent tag lists.</summary>
[JsonSerializable(typeof(List<LedgerEntry>))]
internal sealed partial class TransactionsReadJson : JsonSerializerContext;

public sealed partial class ZananceStore
{
    /// <summary>Reads complete Transactions inputs on one worker, keyed connection and deferred SQLite transaction.</summary>
    public async Task<TransactionsReadSnapshot> ReadTransactionsAsync(Func<string, bool>? acceptsFile = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (acceptsFile?.Invoke(DatabaseFileIdentity.ConfiguredPath(db)) == false)
            throw new InvalidOperationException("The original transaction page cannot follow another file.");
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            var revision = await ReadHomeFinancialRevisionAsync(db, cancellationToken);
            var resources = await GetQuickResourceReviewAsync(db, null, cancellationToken);
            var entries = await ReadCompleteHomeLedgerAsync(db, cancellationToken);
            await ValidateQuickResourceReviewAsync(resources, db, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested(); return new TransactionsReadSnapshot(resources, entries, revision);
        }, cancellationToken);
    }

    /// <summary>Revalidates the complete original history and references before publication or a native command.</summary>
    public async Task ValidateTransactionsAsync(TransactionsReadSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await Task.Run(async () =>
        {
            snapshot.RequireOriginal(db);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            await ValidateQuickResourceReviewAsync(snapshot.Resources, db, cancellationToken);
            if (snapshot.Revision != await ReadHomeFinancialRevisionAsync(db, cancellationToken))
                throw new InvalidOperationException("The original transaction history changed.");
            cancellationToken.ThrowIfCancellationRequested(); snapshot.RequireOriginal(db);
        }, cancellationToken);
    }

    /// <summary>Validates original references and financial revision inside the same writer that will change selected rows.</summary>
    private async Task ValidateTransactionsWriterAsync(TransactionsReadSnapshot snapshot, ZananceDbContext db, CancellationToken ct)
    {
        snapshot.RequireOriginal(db);
        await ValidateQuickResourceReviewAsync(snapshot.Resources, db, ct);
        if (snapshot.Revision != await ReadHomeFinancialRevisionAsync(db, ct))
            throw new InvalidOperationException("The original transaction history changed before its writer.");
    }

    /// <summary>Corrects selected original rows on a worker with unchanged financial rules; acceptance callbacks record state without UI effects.</summary>
    public async Task<SaveResult> SaveTransactionsAsync(TransactionsReadSnapshot snapshot, IReadOnlyList<LedgerEntry> entries,
        Func<bool> isCurrent, CancellationToken cancellationToken = default, Action? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot); ArgumentNullException.ThrowIfNull(entries); ArgumentNullException.ThrowIfNull(isCurrent);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Task.Run(async () =>
        {
            var known = snapshot.Entries.Select(entry => entry.Id).ToHashSet();
            if (entries.Any(entry => !known.Contains(entry.Id)) || entries.Select(entry => entry.Id).Distinct().Count() != entries.Count)
                throw new InvalidOperationException("A bulk correction must retain its original row identities.");
            void Current()
            {
                if (!isCurrent()) throw new OperationCanceledException("The original bulk correction retired.");
                snapshot.RequireOriginal(db);
            }
            Current();
            await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
            await ValidateTransactionsWriterAsync(snapshot, db, cancellationToken); Current();
            write.DemandFeature(CommercialFeature.Corrections);
            return await SaveEntriesUnderWriterAsync(db, write, entries, [], null, new HashSet<Guid>(), cancellationToken,
                committedPublication: true, validateReview: Current, accepted: accepted);
        }, cancellationToken);
    }

    /// <summary>Deletes selected original rows on a worker with original groups and Undo receipt; acceptance callbacks record state without UI effects.</summary>
    public async Task<IReadOnlyList<LedgerEntry>> DeleteTransactionsAsync(TransactionsReadSnapshot snapshot,
        IReadOnlyCollection<Guid> ids, Func<bool> isCurrent, CancellationToken cancellationToken = default, Action? accepted = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot); ArgumentNullException.ThrowIfNull(ids); ArgumentNullException.ThrowIfNull(isCurrent);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Task.Run(async () =>
        {
            var known = snapshot.Entries.Select(entry => entry.Id).ToHashSet();
            if (ids.Any(id => !known.Contains(id))) throw new InvalidOperationException("A bulk deletion must retain its original row identities.");
            void Current()
            {
                if (!isCurrent()) throw new OperationCanceledException("The original bulk deletion retired.");
                snapshot.RequireOriginal(db);
            }
            Current();
            await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
            await ValidateTransactionsWriterAsync(snapshot, db, cancellationToken); Current();
            return await DeleteEntriesUnderWriterAsync(db, write, ids, cancellationToken, committedPublication: true,
                validateReview: Current, accepted: accepted);
        }, cancellationToken);
    }
}
