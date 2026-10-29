using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Narrow original-file authority for a budget form, without retaining a parent financial graph.</summary>
public sealed class BudgetEditorOrigin
{
    private readonly CommercialFileAccess _access;
    /// <summary>Retains only the parent's original actual file and immutable cached-access guard.</summary>
    internal BudgetEditorOrigin(string path, CommercialFileAccess access) { DatabasePath = path; _access = access; }
    /// <summary>Gets the original actual file, independently of later profile selection.</summary>
    public string DatabasePath { get; }
    /// <summary>Refuses retired cached rights before acquiring the original form.</summary>
    public void EnsureCurrent() => _access.EnsureSnapshotCurrent();
}

/// <summary>Complete original budget references and full-field history acquired in one worker transaction.</summary>
public sealed class BudgetEditorSnapshot
{
    private readonly string _historyStamp;
    private readonly string? _defaultStamp;
    /// <summary>Captures the complete detached review/history and guarded nonpersisted preferences.</summary>
    internal BudgetEditorSnapshot(BudgetEditorReview review, List<LedgerEntry> entries)
    {
        Review = review; Entries = entries; Settings = review.Settings ?? new ZananceSettings();
        _historyStamp = Capture(entries);
        _defaultStamp = review.Settings is null ? JsonSerializer.Serialize(Settings, BudgetEditorJson.Default.ZananceSettings) : null;
    }
    /// <summary>Gets original periods, references, preferences and separate current work rights.</summary>
    public BudgetEditorReview Review { get; }
    /// <summary>Gets complete original history; the form applies its original period filter without a second file read.</summary>
    public List<LedgerEntry> Entries { get; }
    /// <summary>Gets stored preferences or guarded nonpersisted defaults; reading never initializes storage.</summary>
    public ZananceSettings Settings { get; }
    /// <summary>Gets the original preview day and quota period.</summary>
    public DateOnly Day => Review.Choices.Today;
    /// <summary>Gets the actual original file.</summary>
    public string DatabasePath => Review.Choices.DatabasePath;
    /// <summary>Checks only cached rights for repeated synchronous native ownership checks.</summary>
    public void EnsureAccessCurrent() => Review.Choices.Access.EnsureSnapshotCurrent();
    /// <summary>Refuses changed detached data before publication, merge or writing.</summary>
    public void EnsureCurrent() { EnsureAccessCurrent(); RequireUnchangedInputs(); }
    private void RequireUnchangedInputs()
    {
        if (_historyStamp != Capture(Entries)
            || Review.InputStamp != BudgetEditorReview.Capture(Review.Accounts, Review.Categories, Review.Settings)
            || !Review.Choices.Items.SequenceEqual(Review.Choices.Budgets.Select(BudgetChoiceItem.From))
            || _defaultStamp is not null && _defaultStamp != JsonSerializer.Serialize(Settings, BudgetEditorJson.Default.ZananceSettings))
            throw new InvalidOperationException("The original budget editor inputs changed in memory.");
    }
    /// <summary>Compares complete inputs for explicit draft-preserving reload, allowing fresh rights to replace expired rights.</summary>
    public bool HasSameInputs(BudgetEditorSnapshot previous)
    {
        ArgumentNullException.ThrowIfNull(previous); EnsureCurrent(); previous.RequireUnchangedInputs();
        return _historyStamp == previous._historyStamp && Review.MatchesInputs(previous.Review);
    }
    /// <summary>Requires the captured file and its original cached access in a reader or writer context.</summary>
    internal void RequireFile(ZananceDbContext db) => Review.Choices.Access.RequireFile(DatabaseFileIdentity.ConfiguredPath(db));
    /// <summary>Refuses changed stored or detached inputs before the original form is published or committed.</summary>
    internal void Compare(BudgetEditorSnapshot fresh)
    {
        EnsureCurrent();
        if (!HasSameInputs(fresh)) throw new InvalidOperationException("The complete original budget editor source changed.");
    }
    private static string Capture(List<LedgerEntry> entries)
    {
        // D-641: stream every mapped ledger field instead of allocating a second full JSON history buffer.
        using var hash = SHA256.Create();
        using var output = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        JsonSerializer.Serialize(output, entries, BudgetEditorJson.Default.ListLedgerEntry); output.FlushFinalBlock();
        return Convert.ToHexString(hash.Hash!);
    }
}

/// <summary>Explicit AOT metadata for the complete original history and nonpersisted default preferences.</summary>
[JsonSerializable(typeof(List<LedgerEntry>))]
[JsonSerializable(typeof(ZananceSettings))]
internal sealed partial class BudgetEditorJson : JsonSerializerContext;

public sealed partial class ZananceStore
{
    /// <summary>Captures the requested actual file before dispatching complete budget form reads onto one worker.</summary>
    public async Task<BudgetEditorSnapshot> GetBudgetEditorSnapshotAsync(Func<string, bool>? acceptsFile = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var path = DatabaseFileIdentity.ConfiguredPath(db);
        if (acceptsFile?.Invoke(path) == false) throw new InvalidOperationException("The original budget editor file changed.");
        return await Task.Run(async () =>
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            return await ReadOriginalBudgetEditorAsync(db, cancellationToken);
        }, cancellationToken);
    }
    /// <summary>Revalidates the complete original form and history in a fresh deferred worker transaction.</summary>
    public async Task ValidateBudgetEditorSnapshotAsync(BudgetEditorSnapshot source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source); source.EnsureAccessCurrent();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await Task.Run(async () =>
        {
            source.RequireFile(db); await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true);
            await using var joined = await db.Database.UseTransactionAsync(transaction, cancellationToken);
            await ValidateOriginalBudgetEditorAsync(source, db, cancellationToken);
        }, cancellationToken);
    }
    private async Task ValidateOriginalBudgetEditorAsync(BudgetEditorSnapshot source, ZananceDbContext db, CancellationToken ct)
    {
        source.RequireFile(db); source.EnsureCurrent();
        source.Compare(await ReadOriginalBudgetEditorAsync(db, ct));
        ct.ThrowIfCancellationRequested(); source.RequireFile(db);
    }
    private async Task<BudgetEditorSnapshot> ReadOriginalBudgetEditorAsync(ZananceDbContext db, CancellationToken ct)
    {
        var review = await GetBudgetEditorReviewAsync(db, ct);
        var entries = await ReadCompleteHomeLedgerAsync(db, ct);
        var result = new BudgetEditorSnapshot(review, entries); ct.ThrowIfCancellationRequested(); result.EnsureCurrent(); return result;
    }
}
