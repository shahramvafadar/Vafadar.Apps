using Vafadar.Finance.Core.Money;

namespace Vafadar.Finance.Core.Rates;

/// <summary>A value converted into the report currency, or the currencies whose rate is missing.</summary>
/// <param name="Total">The combined total; <see langword="null"/> when any rate is missing (FX-02, FX-05).</param>
/// <param name="CurrencyCode">The currency of <paramref name="Total"/>.</param>
/// <param name="MissingCurrencies">Currencies without a usable rate.</param>
/// <param name="OldestRateDate">The oldest rate date used, shown next to the total (FX-04).</param>
public sealed record CombinedTotal(long? Total, string CurrencyCode, IReadOnlyList<string> MissingCurrencies, DateOnly? OldestRateDate)
{
    /// <summary>Gets a value indicating whether every currency could be converted.</summary>
    public bool IsComplete => Total is not null;
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
    public bool TryGetRate(string from, string to, DateOnly date, out decimal rate, out DateOnly rateDate)
    {
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
            ({ } d, { } i) => d.Date >= i.Date ? (d.Rate, d.Date) : (1 / i.Rate, i.Date),
            ({ } d, null) => (d.Rate, d.Date),
            (null, { } i) => (1 / i.Rate, i.Date),
            _ => ((decimal Rate, DateOnly Date)?)null,
        };

        if (chosen is not { } found)
        {
            return false;
        }

        (rate, rateDate) = found;
        return true;
    }

    /// <summary>Converts minor units of one currency into minor units of another, rounding half away from zero.</summary>
    public bool TryConvert(long minor, string from, string to, DateOnly date, out long converted, out DateOnly rateDate)
    {
        converted = 0;
        if (!TryGetRate(from, to, date, out var rate, out rateDate))
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
    /// <paramref name="date"/>. If any rate is missing, the total is incomplete instead of a guessed number.
    /// </summary>
    public CombinedTotal Combine(IReadOnlyDictionary<string, long> perCurrency, string reportCurrency, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(perCurrency);
        long total = 0;
        var missing = new List<string>();
        DateOnly? oldest = null;

        foreach (var (currency, amount) in perCurrency)
        {
            if (TryConvert(amount, currency, reportCurrency, date, out var converted, out var rateDate))
            {
                total += converted;
                if (Normalize(currency) != Normalize(reportCurrency) && (oldest is null || rateDate < oldest))
                {
                    oldest = rateDate;
                }
            }
            else
            {
                missing.Add(currency);
            }
        }

        return new CombinedTotal(missing.Count == 0 ? total : null, reportCurrency, missing, oldest);
    }

    private ExchangeRate? Latest((string, string) key, DateOnly date) =>
        _rates.TryGetValue(key, out var list) ? list.LastOrDefault(r => r.Date <= date) : null;

    private static string Normalize(string code) => code.ToUpperInvariant();
}
