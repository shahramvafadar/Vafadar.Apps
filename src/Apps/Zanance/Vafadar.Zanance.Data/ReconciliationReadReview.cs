using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Complete original account and related history, retained privately for an explicit balance comparison.</summary>
internal sealed record ReconciliationInputs(Account Account, List<LedgerEntry> Entries);

/// <summary>A single-use comparison bound to its original database, account, date, amount and access facts.</summary>
public sealed class ReconciliationReadReview
{
    private readonly string _path, _packet;
    private readonly CommercialFileAccess _access;
    private int _state;

    /// <summary>Captures coherent original inputs; callers cannot manufacture or alter writer authority.</summary>
    internal ReconciliationReadReview(string path, CommercialFileAccess access, ReconciliationInputs inputs, long observed, DateOnly date)
    {
        _path = path; _access = access; AccountId = inputs.Account.Id; CurrencyCode = inputs.Account.CurrencyCode; Date = date;
        _packet = Packet(inputs);
        Result = Reconciliation.Analyze(inputs.Account, inputs.Entries, observed, date);
        // A correction must be representable as a positive minor-unit amount before it is offered for consent.
        _ = Math.Abs(checked(Result.Observed - Result.Recorded));
    }

    /// <summary>Gets the actual original file so an account page cannot compare a same-ID account in another profile.</summary>
    public string DatabasePath => _path;

    /// <summary>Gets the original account identity.</summary>
    public Guid AccountId { get; }
    /// <summary>Gets the original currency; presentation cannot replace the reviewed financial unit.</summary>
    public string CurrencyCode { get; }
    /// <summary>Gets the exact compared date, never a subsequently edited form value.</summary>
    public DateOnly Date { get; }
    /// <summary>Gets the immutable recorded/observed comparison and original review hints.</summary>
    public ReconciliationResult Result { get; }

    /// <summary>Rejects retired access before publishing a comparison or starting its writer.</summary>
    internal void EnsureCurrent() => _access.EnsureSnapshotCurrent();
    /// <summary>Prevents concurrent or repeated acceptance of this exact reviewed operation.</summary>
    internal void BeginWrite()
    {
        EnsureCurrent();
        if (Interlocked.CompareExchange(ref _state, 1, 0) != 0) throw new InvalidOperationException("The balance comparison is already being saved or was consumed.");
    }
    /// <summary>Retains a terminal accepted outcome; failed transactions may be explicitly retried.</summary>
    internal void EndWrite(bool committed) => Volatile.Write(ref _state, committed ? 2 : 0);
    /// <summary>Rechecks complete original facts under the actual file's existing financial writer.</summary>
    internal void ValidateWriter(ZananceDbContext db, ReconciliationInputs current)
    {
        EnsureCurrent();
        if (_path != DatabaseFileIdentity.ConfiguredPath(db) || _packet != Packet(current))
            throw new InvalidOperationException("The account or history changed; compare the balance again.");
    }
    /// <summary>Reads the original account and all its source/destination rows, with deterministic packet order.</summary>
    internal static async Task<ReconciliationInputs> ReadAsync(ZananceDbContext db, Guid id, CancellationToken ct) => new(
        await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == id, ct),
        await db.Entries.AsNoTracking().Where(e => e.AccountId == id || e.ToAccountId == id).OrderBy(e => e.Id).ToListAsync(ct));
    private static string Packet(ReconciliationInputs inputs) => JsonSerializer.Serialize(inputs, ReconciliationReviewJson.Default.ReconciliationInputs);
}

/// <summary>Explicit trim/AOT metadata for original balance review inputs; no stored schema changes.</summary>
[JsonSerializable(typeof(ReconciliationInputs))]
internal sealed partial class ReconciliationReviewJson : JsonSerializerContext;
