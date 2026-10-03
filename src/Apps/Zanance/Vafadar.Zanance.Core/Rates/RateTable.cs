using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.Rates;

/// <summary>
/// How a converted number was obtained for one source currency (ZEX-S0106): the date of the rate, whether it is an
/// estimate, whether it may be outdated, or that it is missing.
/// </summary>
/// <param name="CurrencyCode">The source currency.</param>
/// <param name="Date">The date of the rate used; <see langword="null"/> when no rate exists.</param>
/// <param name="IsEstimate">Whether the rate was entered as an estimate.</param>
/// <param name="IsOutdated">Whether the rate may be outdated (see <see cref="RateFreshness"/>).</param>
public sealed record RateInfo(string CurrencyCode, DateOnly? Date, bool IsEstimate, bool IsOutdated)
{
    /// <summary>Gets a value indicating whether no rate exists.</summary>
    public bool IsMissing => Date is null;
}

/// <summary>
/// When a rate counts as possibly outdated (ZEX-P06): it is older than <paramref name="Days"/> days before the valuation
/// date and it also lies before the start of the current financial month, so a rate entered for this month never
/// counts as outdated. It is a reminder to update, not an accuracy claim.
/// </summary>
/// <param name="Days">The age in days (7, 30 or 90; default 30); <see langword="null"/> = never outdated.</param>
/// <param name="FinancialMonthStart">The first day of the financial month of the valuation date, when known.</param>
public sealed record RateFreshness(int? Days = 30, DateOnly? FinancialMonthStart = null)
{
    /// <summary>The default: 30 days, without a month start.</summary>
    public static RateFreshness Default { get; } = new();

    /// <summary>Returns whether a rate dated <paramref name="rateDate"/> may be outdated on <paramref name="valuationDate"/>.</summary>
    public bool IsOutdated(DateOnly rateDate, DateOnly valuationDate)
    {
        if (Days is not { } days || days <= 0)
        {
            return false;
        }

        var tooOld = rateDate < valuationDate.AddDays(-days);
        return tooOld && (FinancialMonthStart is not { } start || rateDate < start);
    }
}

/// <summary>A value converted into the valuation currency, or the currencies whose rate is missing.</summary>
/// <param name="Total">The combined total; <see langword="null"/> when any rate is missing (FX-02, FX-05).</param>
/// <param name="CurrencyCode">The currency of <paramref name="Total"/>.</param>
/// <param name="MissingCurrencies">Currencies without a usable rate.</param>
/// <param name="OldestRateDate">The oldest rate date used, shown next to the total (FX-04).</param>
public sealed record CombinedTotal(long? Total, string CurrencyCode, IReadOnlyList<string> MissingCurrencies, DateOnly? OldestRateDate)
{
    /// <summary>Gets the rate information of every converted currency (not the valuation currency itself).</summary>
    public IReadOnlyList<RateInfo> Rates { get; init; } = [];

    /// <summary>Gets a value indicating whether every currency could be converted.</summary>
    public bool IsComplete => Total is not null;

    /// <summary>Gets a value indicating whether a rate used may be outdated.</summary>
    public bool IsOutdated => Rates.Any(r => r.IsOutdated);

    /// <summary>Gets a value indicating whether a rate used is an estimate.</summary>
    public bool HasEstimate => Rates.Any(r => r.IsEstimate);
}

/// <summary>
/// Converts amounts with manually entered rates (FX-02). The rate of a date is the latest rate on or before it; a rate
/// entered in the other direction is used inverted. There is never an implicit 1:1 rate (FX-05, AT-43).
/// </summary>
public sealed class RateTable
{
    private readonly Dictionary<(string From, string To), List<ExchangeRate>> _rates = [];

    /// <summary>Creates the table from all stored rates.</summary>
    public RateTable(IEnumerable<ExchangeRate> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);
        foreach (var rate in rates.Where(r => r.Rate > 0))
        {
            var key = (Normalize(rate.FromCurrencyCode), Normalize(rate.ToCurrencyCode));
            if (!_rates.TryGetValue(key, out var list))
            {
                _rates[key] = list = [];
            }

            list.Add(rate);
        }

        foreach (var list in _rates.Values)
        {
            list.Sort((a, b) => a.Date.CompareTo(b.Date));
        }
    }

    /// <summary>Returns the rate from <paramref name="from"/> to <paramref name="to"/> valid on <paramref name="date"/>.</summary>
    /// <param name="from">Source currency.</param>
    /// <param name="to">Target currency.</param>
    /// <param name="date">The date.</param>
    /// <param name="rate">1 unit of <paramref name="from"/> in <paramref name="to"/>.</param>
    /// <param name="rateDate">The date of the rate used.</param>
    public bool TryGetRate(string from, string to, DateOnly date, out decimal rate, out DateOnly rateDate) =>
        TryGetRate(from, to, date, out rate, out rateDate, out _);

    /// <summary>Returns the rate valid on <paramref name="date"/> and whether it was entered as an estimate.</summary>
    public bool TryGetRate(string from, string to, DateOnly date, out decimal rate, out DateOnly rateDate, out bool isEstimate)
    {
        isEstimate = false;
        from = Normalize(from);
        to = Normalize(to);
        rate = 1;
        rateDate = date;
        if (from == to)
        {
            return true;
        }

        var direct = Latest((from, to), date);
        var inverse = Latest((to, from), date);
        var chosen = (direct, inverse) switch
        {
            ({ } d, { } i) => d.Date >= i.Date ? (d.Rate, d.Date, d.IsEstimate) : (1 / i.Rate, i.Date, i.IsEstimate),
            ({ } d, null) => (d.Rate, d.Date, d.IsEstimate),
            (null, { } i) => (1 / i.Rate, i.Date, i.IsEstimate),
            _ => ((decimal Rate, DateOnly Date, bool IsEstimate)?)null,
        };

        if (chosen is not { } found)
        {
            return false;
        }

        (rate, rateDate, isEstimate) = found;
        return true;
    }

    /// <summary>Converts minor units of one currency into minor units of another, rounding half away from zero.</summary>
    public bool TryConvert(long minor, string from, string to, DateOnly date, out long converted, out DateOnly rateDate) =>
        TryConvert(minor, from, to, date, out converted, out rateDate, out _);

    /// <summary>Converts like <see cref="TryConvert(long, string, string, DateOnly, out long, out DateOnly)"/> and reports an estimated rate.</summary>
    public bool TryConvert(long minor, string from, string to, DateOnly date, out long converted, out DateOnly rateDate, out bool isEstimate)
    {
        converted = 0;
        if (!TryGetRate(from, to, date, out var rate, out rateDate, out isEstimate))
        {
            return false;
        }

        var source = Currencies.TryGet(from, out var s) ? s : new Currency(from, 2);
        var target = Currencies.TryGet(to, out var t) ? t : new Currency(to, 2);
        converted = MoneyAmount.ToMinor(MoneyAmount.ToDecimal(minor, source) * rate, target);
        return true;
    }

    /// <summary>
    /// Combines amounts per currency into <paramref name="reportCurrency"/> with the rates valid on
    /// <paramref name="date"/>. If any rate is missing, the total is incomplete instead of a guessed number; every
    /// converted currency carries its <see cref="RateInfo"/> (ZEX-S0106). A rate dated after <paramref name="date"/> is
    /// never used.
    /// </summary>
    public CombinedTotal Combine(IReadOnlyDictionary<string, long> perCurrency, string reportCurrency, DateOnly date, RateFreshness? freshness = null)
    {
        ArgumentNullException.ThrowIfNull(perCurrency);
        freshness ??= RateFreshness.Default;
        long total = 0;
        var missing = new List<string>();
        var rates = new List<RateInfo>();
        DateOnly? oldest = null;

        foreach (var (currency, amount) in perCurrency)
        {
            var same = Normalize(currency) == Normalize(reportCurrency);
            if (TryConvert(amount, currency, reportCurrency, date, out var converted, out var rateDate, out var estimate))
            {
                total = checked(total + converted);
                if (!same)
                {
                    rates.Add(new RateInfo(currency, rateDate, estimate, freshness.IsOutdated(rateDate, date)));
                    if (oldest is null || rateDate < oldest)
                    {
                        oldest = rateDate;
                    }
                }
            }
            else
            {
                missing.Add(currency);
                rates.Add(new RateInfo(currency, null, false, false));
            }
        }

        return new CombinedTotal(missing.Count == 0 ? total : null, reportCurrency, missing, oldest) { Rates = rates };
    }

    private ExchangeRate? Latest((string, string) key, DateOnly date) =>
        _rates.TryGetValue(key, out var list) ? list.LastOrDefault(r => r.Date <= date) : null;

    private static string Normalize(string code) => code.ToUpperInvariant();
}
