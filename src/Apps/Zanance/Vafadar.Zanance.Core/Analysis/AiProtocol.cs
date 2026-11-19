using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Analysis;

/// <summary>Fixed read-only questions; arbitrary receipt/note/model instructions never select application work.</summary>
public enum AiPurpose
{
    /// <summary>Explain only the selected recorded period totals.</summary>
    PeriodExplanation,
    /// <summary>Compare two explicitly disclosed recorded periods, without investment/tax/payment recommendations.</summary>
    PeriodComparison,
}

/// <summary>Only aggregate figures and their exact scope; no account/transaction identifiers, names, notes or documents.</summary>
public sealed record AiPeriod(DateOnly From, DateOnly To, int AccountCount, bool ConfirmedOnly,
    IReadOnlyList<PeriodTotals> Totals, int UnreviewedCount);

/// <summary>The exact minimal payload previewed before any service/provider financial request.</summary>
public sealed record AiPayload(int Version, AiPurpose Purpose, string Language, AiPeriod Current, AiPeriod? Previous,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] AiReportDetails? Report = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] AiFundingFacts? Funding = null);

/// <summary>An anonymous top-level category's net spending in two explicitly shown periods.</summary>
public sealed record AiCategoryChange(long Current, long Previous);

/// <summary>An anonymous plan row. Variance compares only settled payments with their known planned amounts.</summary>
public sealed record AiPlanDifference(string Currency, EntryKind Kind, long Planned, long Actual,
    int Settled, int Open, int Unknown, long? Variance);

/// <summary>Rows referenced by one-based position. Names, identifiers and notes stay on the device.</summary>
public sealed record AiReportDetails(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<AiCategoryChange>? Categories,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<AiPlanDifference>? Plans);

/// <summary>Identifies the fixed source of the disclosed funding amounts.</summary>
public enum AiFundingKind { BudgetLimit, BudgetSuggestion, Goal }

/// <summary>Locally computed facts. Baseline and observed mean limit/spent, suggestion/average, or target/funded.</summary>
public sealed record AiFundingFacts(AiFundingKind Kind, string Currency, long Baseline, long Observed, long Difference,
    long? Existing, long? Carry, int? PeriodCount, long? PlannedContribution, long? RequiredContribution, long? Unfunded)
{
    /// <summary>Explains the effective limit, including carry, and the draft base limit that would cover recorded spending.</summary>
    public static AiFundingFacts Limit(string currency, long ownLimit, long effectiveLimit, long spent, long carry) =>
        new(AiFundingKind.BudgetLimit, currency, effectiveLimit, spent, checked(spent - effectiveLimit),
            ownLimit, carry, null, null, null, null);

    /// <summary>Explains the current locally rounded suggestion against recorded history and any original saved limit.</summary>
    public static AiFundingFacts Suggestion(string currency, long average, long suggested, int count, long? saved) =>
        new(AiFundingKind.BudgetSuggestion, currency, suggested, average, checked(suggested - average),
            saved, null, count, null, null, null);

    /// <summary>Explains recorded monetary progress and the original contribution scenario, where available.</summary>
    public static AiFundingFacts Goal(string currency, long target, long funded, long remaining, int opportunities,
        long? planned, long? required, long unfunded) =>
        new(AiFundingKind.Goal, currency, target, funded, remaining, null, null, opportunities, planned, required, unfunded);

    /// <summary>Returns a local comparison option, which has no authority to update any original amount.</summary>
    public long? Alternative() => Kind switch
    {
        AiFundingKind.BudgetLimit when Existing is { } original && Difference > 0 && (Int128)original + Difference <= long.MaxValue
            => original + Difference,
        AiFundingKind.BudgetSuggestion => Baseline,
        AiFundingKind.Goal => RequiredContribution,
        _ => null,
    };
}

/// <summary>Provider metadata and maximum technical credit units, never a client-supplied price or implied subscription benefit.</summary>
public sealed record AiOffer(Guid Id, string Provider, string Model, AiPurpose Purpose, long MaximumUnits, DateTimeOffset ExpiresAt);

/// <summary>Current split wallet; reserved units remain unavailable while provider outcome is uncertain.</summary>
public sealed record AiBalance(long PurchasedAvailable, long GiftedAvailable, long PurchasedReserved, long GiftedReserved);

/// <summary>Explicitly unavailable until a separately approved actual provider and credit pricing are configured.</summary>
public sealed record AiAvailability(bool Configured, string? Provider, string? Model, AiBalance Balance,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? ComparisonConfigured = null);

/// <summary>Metadata-only request; no financial content is sent to obtain a cost/destination offer.</summary>
public sealed record AiOfferRequest(AiPurpose Purpose);

/// <summary>Metadata-only close of an original unaccepted intent; it cannot refund dispatched work or disclose financial content.</summary>
public sealed record AiCloseRequest(AiOffer Offer, string PayloadDigest);

/// <summary>One immutable consent request; retries retain its original id, offer and aggregate payload.</summary>
public sealed record AiSubmission(Guid RequestId, AiOffer Offer, AiPayload Payload);

/// <summary>Persisted service outcome; pending never means safe to resend under a different id.</summary>
public enum AiRequestState
{
    /// <summary>Original reservation/dispatch was committed; only provider status may resolve uncertainty.</summary>
    Pending,
    /// <summary>Confirmed original result settled once; output remains untrusted read-only text.</summary>
    Completed,
    /// <summary>Provider definitively confirmed no charge/work; the exact original reservation was refunded.</summary>
    Refunded,
    /// <summary>The original request was durably closed before reservation/dispatch; late retries cannot execute it.</summary>
    NotAccepted,
}

/// <summary>One owned request's result; no model output carries a financial command or application authority.</summary>
public sealed record AiRequestResult(Guid RequestId, string PayloadDigest, AiRequestState State, long ChargedUnits,
    string? Text, AiBalance Balance);

/// <summary>Strict bounded aggregate contract and standard digest; no custom financial or cryptographic algorithm.</summary>
public static class AiProtocol
{
    /// <summary>Finite serialized financial disclosure limit, independent of commercial credit package sizes.</summary>
    public const int MaximumPayloadBytes = 16384;
    /// <summary>Finite untrusted plain-text result limit.</summary>
    public const int MaximumResultCharacters = 4000;
    /// <summary>Validates only supported explicit periods/currencies; it cannot authorize a network request.</summary>
    public static void Validate(AiPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Version != (payload.Funding is not null ? 3 : payload.Report is null ? 1 : 2) || !Enum.IsDefined(payload.Purpose) || payload.Language is not ("en" or "fa" or "de" or "es" or "fr" or "it"))
            throw new ArgumentException("Unsupported analysis contract.");
        Period(payload.Current);
        if (payload.Purpose == AiPurpose.PeriodComparison)
        {
            if (payload.Previous is null) throw new ArgumentException("Explicit comparison period required.");
            Period(payload.Previous);
            if (payload.Previous.To >= payload.Current.From || payload.Previous.AccountCount != payload.Current.AccountCount ||
                payload.Previous.ConfirmedOnly != payload.Current.ConfirmedOnly ||
                !payload.Previous.Totals.Select(t => t.CurrencyCode).SequenceEqual(payload.Current.Totals.Select(t => t.CurrencyCode)))
                throw new ArgumentException("Comparison scope differs.");
        }
        else if (payload.Previous is not null) throw new ArgumentException("Unrequested previous period refused.");
        if (payload.Report is { } report) ValidateReport(payload, report);
        if (payload.Funding is { } funding) ValidateFunding(payload, funding);
        if (JsonSerializer.SerializeToUtf8Bytes(payload, AiJson.Default.AiPayload).Length > MaximumPayloadBytes)
            throw new ArgumentException("Analysis payload exceeds its technical bound.");
    }
    /// <summary>Hashes the exact canonical disclosed payload for consent/idempotence, never as financial authority.</summary>
    public static string Digest(AiPayload payload)
    {
        Validate(payload);
        return Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(payload, AiJson.Default.AiPayload)));
    }
    /// <summary>Rejects unsafe/oversized provider text; it is always rendered as text, never markup or commands.</summary>
    public static void ValidateText(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > MaximumResultCharacters || new UTF8Encoding(false, true).GetByteCount(text) > 32768 ||
            text.Any(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t' || c is >= '\u202A' and <= '\u202E' or >= '\u2066' and <= '\u2069'))
            throw new ArgumentException("Provider text is invalid.");
    }
    private static void ValidateFunding(AiPayload payload, AiFundingFacts facts)
    {
        if (payload.Purpose != AiPurpose.PeriodExplanation || payload.Report is not null || payload.Current.Totals.Count != 1 ||
            payload.Current.Totals[0].CurrencyCode != facts.Currency)
            throw new ArgumentException("Invalid funding scope.");

        var valid = facts.Kind switch
        {
            AiFundingKind.BudgetLimit => facts.Existing is >= 0 && facts.Carry is not null && facts.PeriodCount is null &&
                facts.PlannedContribution is null && facts.RequiredContribution is null && facts.Unfunded is null &&
                facts.Difference > 0 && (Int128)facts.Observed - facts.Baseline == facts.Difference,
            AiFundingKind.BudgetSuggestion => facts.Observed > 0 && facts.Baseline >= facts.Observed &&
                (Int128)facts.Baseline - facts.Observed == facts.Difference && facts.Existing is null or >= 0 &&
                facts.PeriodCount is >= 1 and <= 4 && facts.Carry is null && facts.PlannedContribution is null &&
                facts.RequiredContribution is null && facts.Unfunded is null,
            AiFundingKind.Goal => facts.Baseline > 0 && facts.Difference > 0 && facts.Unfunded is >= 0 &&
                (Int128)facts.Baseline - Math.Max(0, facts.Observed) == facts.Difference && facts.PeriodCount is >= 0 &&
                facts.Existing is null && facts.Carry is null && facts.PlannedContribution is null or >= 0 &&
                (facts.RequiredContribution is null || facts.RequiredContribution > 0 && facts.PeriodCount > 0),
            _ => false,
        };
        if (!valid) throw new ArgumentException("Invalid funding facts.");
    }

    private static void ValidateReport(AiPayload payload, AiReportDetails report)
    {
        if (payload.Purpose == AiPurpose.PeriodComparison && report.Plans is null &&
            report.Categories is { Count: >= 1 and <= MaximumReportRows } categories && payload.Current.Totals.Count == 1)
        {
            foreach (var row in categories)
                if (row is null || checked(row.Current - row.Previous) == 0)
                    throw new ArgumentException("Invalid spending change.");
            return;
        }

        if (payload.Purpose == AiPurpose.PeriodExplanation && report.Categories is null &&
            report.Plans is { Count: >= 1 and <= MaximumReportRows } plans &&
            plans.All(row => row is not null && Enum.IsDefined(row.Kind) && row.Planned >= 0 && row.Actual >= 0
                && row.Settled >= 0 && row.Open >= 0 && row.Unknown >= 0
                && (row.Settled > 0 || (row.Variance is null && row.Actual == 0))
                && payload.Current.Totals.Any(total => total.CurrencyCode == row.Currency)))
            return;

        throw new ArgumentException("Invalid report details.");
    }

    /// <summary>Maximum number of anonymous report rows included in one disclosure.</summary>
    public const int MaximumReportRows = 12;

    private static void Period(AiPeriod period)
    {
        ArgumentNullException.ThrowIfNull(period);
        if (period.From == DateOnly.MinValue || period.To < period.From || period.To.DayNumber - period.From.DayNumber > 365 ||
            period.AccountCount is < 1 or > 10000 || period.UnreviewedCount < 0 || period.Totals is null || period.Totals.Count is < 1 or > 64 || period.Totals.Any(t => t is null))
            throw new ArgumentException("Analysis period exceeds its explicit scope.");
        var currencies = period.Totals.Select(t => t.CurrencyCode).ToArray();
        if (!currencies.SequenceEqual(currencies.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))) throw new ArgumentException("Canonical currencies required.");
        foreach (var total in period.Totals)
        {
            if (!Currencies.TryGet(total.CurrencyCode, out _) || total.Income < 0 || total.IncomeReversals < 0 || total.GrossExpense < 0 || total.Refunds < 0)
                throw new ArgumentException("Invalid recorded totals.");
            _ = checked(total.NetIncome - total.NetExpense);
        }
    }
}

/// <summary>Explicit AOT/trim metadata for the entire limited contract; unrecognized request fields are refused.</summary>
[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(AiPayload))]
[JsonSerializable(typeof(AiOffer))]
[JsonSerializable(typeof(AiOfferRequest))]
[JsonSerializable(typeof(AiCloseRequest))]
[JsonSerializable(typeof(AiSubmission))]
[JsonSerializable(typeof(AiBalance))]
[JsonSerializable(typeof(AiAvailability))]
[JsonSerializable(typeof(AiRequestResult))]
public sealed partial class AiJson : JsonSerializerContext;
