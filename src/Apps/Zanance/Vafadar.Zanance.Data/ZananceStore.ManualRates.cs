using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>Refuses a manual-rate command after original file, stored facts or native draft ownership changes.</summary>
public sealed class ManualRateReviewChangedException : InvalidOperationException
{
    /// <summary>Creates content-free original manual-rate refusal without financial or file details.</summary>
    public ManualRateReviewChangedException() : base("The original manual-rate review changed.") { }
}

/// <summary>Immutable original rate facts and page defaults captured from one actual financial file.</summary>
public sealed class ManualRatesReview
{
    private readonly string path;
    private readonly Fact[] facts;
    private sealed record Fact(Guid Id, DateOnly Date, string From, string To, decimal Rate, bool Estimate,
        DateTimeOffset Created, DateTimeOffset Updated);
    /// <summary>Detaches every original field from mutable caller rows and captures the original page defaults.</summary>
    internal ManualRatesReview(string path, IEnumerable<ExchangeRate> rates, string reportCurrency, IEnumerable<string> accounts)
    {
        this.path = path; facts = Capture(rates); ReportCurrencyCode = reportCurrency;
        AccountCurrencies = Array.AsReadOnly(accounts.Distinct(StringComparer.Ordinal).ToArray());
    }
    /// <summary>Original report currency is a default only; it never converts a stored rate or amount.</summary>
    public string ReportCurrencyCode { get; }
    /// <summary>Original active account currencies used solely for the same existing missing-rate hint.</summary>
    public IReadOnlyList<string> AccountCurrencies { get; }
    /// <summary>Returns detached original rate rows; mutation never renews the private writer review.</summary>
    public IReadOnlyList<ExchangeRate> Rates => facts.Select(row => new ExchangeRate(row.Id) { Date = row.Date,
        FromCurrencyCode = row.From, ToCurrencyCode = row.To, Rate = row.Rate, IsEstimate = row.Estimate,
        CreatedAt = row.Created, UpdatedAt = row.Updated }).OrderByDescending(row => row.Date).ThenBy(row => row.FromCurrencyCode).ToArray();
    /// <summary>Compares original file identity without disclosing its private path to presentation.</summary>
    public bool OwnsSameFile(ManualRatesReview other) => other is not null && path == other.path;
    /// <summary>Detects a refreshed rate version without exposing or renewing any private original facts.</summary>
    public bool OwnsSameVersion(ManualRatesReview other) => OwnsSameFile(other) && facts.SequenceEqual(other.facts);
    /// <summary>Checks actual file and complete reviewed rate facts under the eventual writer.</summary>
    internal void Require(ZananceDbContext db, IEnumerable<ExchangeRate>? current = null)
    {
        if (path != DatabaseFileIdentity.ConfiguredPath(db) || current is not null && !facts.SequenceEqual(Capture(current)))
            throw new ManualRateReviewChangedException();
    }
    private static Fact[] Capture(IEnumerable<ExchangeRate> rows) => rows.OrderBy(row => row.Id).Select(row =>
        new Fact(row.Id, row.Date, row.FromCurrencyCode, row.ToCurrencyCode, row.Rate, row.IsEstimate, row.CreatedAt, row.UpdatedAt)).ToArray();
}

/// <summary>Known original rate acceptance, independent of later change notification and page readback.</summary>
/// <param name="Id">The actual inserted/replaced/deleted rate identity.</param>
/// <param name="Deleted">Whether the explicitly reviewed command removed the rate.</param>
public sealed record ManualRateReceipt(Guid Id, bool Deleted);

public sealed partial class ZananceStore
{
    /// <summary>Reads actual rates, settings and active currency hints consistently after ordinary app startup.</summary>
    public async Task<ManualRatesReview> ReadManualRatesAsync(CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await using var read = await db.Database.BeginTransactionAsync(ct);
        var settings = await SettingsRowReader.ReadAsync(db, ct) ?? throw new ManualRateReviewChangedException();
        var rates = await db.ExchangeRates.AsNoTracking().ToListAsync(ct);
        var accounts = await db.Accounts.AsNoTracking().Where(row => !row.IsArchived).Select(row => row.CurrencyCode).ToListAsync(ct);
        var review = new ManualRatesReview(DatabaseFileIdentity.ConfiguredPath(db), rates, settings.ReportCurrencyCode, accounts);
        await read.CommitAsync(ct); return review;
    }
    /// <summary>Publishes only one frozen reviewed ratio; a same-date pair replaces its original reviewed identity.</summary>
    public Task<ManualRateReceipt> SaveManualRateAsync(ManualRatesReview original, ExchangeRate input, Func<bool> isCurrent,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        // D-437: never follow mutable view-model fields while waiting for the original writer.
        var draft = new ExchangeRate(input.Id) { Date = input.Date, FromCurrencyCode = input.FromCurrencyCode,
            ToCurrencyCode = input.ToCurrencyCode, Rate = input.Rate, IsEstimate = input.IsEstimate };
        return WriteManualRateAsync(original, draft, null, isCurrent, ct);
    }
    /// <summary>Deletes only a row in the original reviewed set after original native consent still owns it.</summary>
    public Task<ManualRateReceipt> DeleteManualRateAsync(ManualRatesReview original, Guid id, Func<bool> isCurrent,
        CancellationToken ct = default) => WriteManualRateAsync(original, null, id, isCurrent, ct);

    private async Task<ManualRateReceipt> WriteManualRateAsync(ManualRatesReview original, ExchangeRate? draft, Guid? remove,
        Func<bool> isCurrent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(original); ArgumentNullException.ThrowIfNull(isCurrent);
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        void Current() { original.Require(db); if (!isCurrent()) throw new ManualRateReviewChangedException(); }
        Current(); await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, ct, requireTransaction: true);
        Current(); var rates = await db.ExchangeRates.ToListAsync(ct); original.Require(db, rates);
        ManualRateReceipt receipt;
        if (remove is { } id)
        {
            var row = rates.SingleOrDefault(row => row.Id == id) ?? throw new ManualRateReviewChangedException();
            db.ExchangeRates.Remove(row); receipt = new(row.Id, true);
        }
        else if (draft is not null)
        {
            if (draft.Rate <= 0 || draft.Date == default || string.Equals(draft.FromCurrencyCode, draft.ToCurrencyCode, StringComparison.OrdinalIgnoreCase) ||
                !Core.Money.Currencies.TryGet(draft.FromCurrencyCode, out _) || !Core.Money.Currencies.TryGet(draft.ToCurrencyCode, out _))
                throw new ArgumentException("Only an explicit positive known-currency pair and date can be saved.", nameof(draft));
            var row = rates.SingleOrDefault(row => row.Date == draft.Date && row.FromCurrencyCode == draft.FromCurrencyCode && row.ToCurrencyCode == draft.ToCurrencyCode);
            if (row is null) { row = draft; db.ExchangeRates.Add(row); }
            else { row.Rate = draft.Rate; row.IsEstimate = draft.IsEstimate; }
            receipt = new(row.Id, false);
        }
        else throw new ArgumentException("An explicit original rate command is required.");
        Current(); write.EnsureCurrent(); await db.SaveChangesAsync(ct); Current(); await write.CommitAsync(ct);
        return CommittedWriteNotification.Publish(OnChanged, receipt);
    }
}
