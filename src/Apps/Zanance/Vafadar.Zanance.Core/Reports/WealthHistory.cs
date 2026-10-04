using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Core.Reports;

/// <summary>Net worth on one day: per currency, and converted when every currency has a rate on or before that day.</summary>
public sealed record WealthPoint(DateOnly Date, IReadOnlyDictionary<string, long> PerCurrency, long? Converted, bool IsIncomplete);

/// <summary>Why a wealth change has a remainder that the parts do not explain (04 §4.1). New values are appended.</summary>
public enum RemainderCause
{
    /// <summary>A holding had no price on one of the two dates.</summary>
    MissingValuation = 0,

    /// <summary>An account's opening balance is unknown.</summary>
    UnknownOpeningBalance = 1,

    /// <summary>Holdings were bought or sold at another price than their valuation.</summary>
    TradedAtOtherPrice = 2,

    /// <summary>A currency had no rate on one of the dates (converted view).</summary>
    MissingRate = 3,
}

/// <summary>
/// The change of wealth between two dates in one currency (04 §4.1): ΔNW = flows + price effect + FX effect + corrections
/// + remainder. Flows are the surplus (income − consumption); the remainder is always shown with its causes.
/// </summary>
public sealed record WealthChange(
    string CurrencyCode,
    DateOnly From,
    DateOnly To,
    long Start,
    long End,
    long Flows,
    long PriceEffect,
    long FxEffect,
    long Corrections,
    IReadOnlyList<RemainderCause> Causes)
{
    /// <summary>Gets ΔNW.</summary>
    public long Change => End - Start;

    /// <summary>Gets what the parts do not explain; never hidden.</summary>
    public long Remainder => Change - Flows - PriceEffect - FxEffect - Corrections;
}

/// <summary>Net worth over time and the explanation of its change (ZEX-S0801, S0802). Computed, never stored.</summary>
public static class WealthHistory
{
    /// <summary>
    /// Returns net worth at the end of each of the last <paramref name="months"/> complete financial months and today,
    /// with prices and rates dated on or before each day – a new price never changes an earlier point (AS13).
    /// </summary>
    public static IReadOnlyList<WealthPoint> MonthEnds(
        IReadOnlyCollection<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        IReadOnlyCollection<AssetType> types,
        IReadOnlyCollection<AssetEvent> events,
        IReadOnlyCollection<AssetValuation> valuations,
        RateTable rates,
        string currencyCode,
        DateOnly today,
        int months,
        PeriodCalendar calendar,
        int startDay = 1)
    {
        ArgumentNullException.ThrowIfNull(rates);
        var dates = new List<DateOnly> { today };
        var current = PeriodMath.MonthOf(today, calendar, startDay);
        var (year, month) = current;
        for (var i = 0; i < months; i++)
        {
            (year, month) = PeriodMath.Previous(year, month);
            dates.Insert(0, PeriodMath.MonthRange(year, month, calendar, startDay).Last);
        }

        var first = accounts.Count == 0 ? today : accounts.Min(a => a.OpeningDate);
        return
        [
            .. dates.Where(d => d >= first).Select(date =>
            {
                var worth = NetWorthCalculator.Compute(accounts, entries, types, events, valuations, date);
                var perCurrency = worth.Totals.ToDictionary(t => t.CurrencyCode, t => t.Total, StringComparer.OrdinalIgnoreCase);
                var combined = rates.Combine(perCurrency, currencyCode, date);
                return new WealthPoint(date, perCurrency, combined.Total, worth.IsIncomplete || !combined.IsComplete);
            }),
        ];
    }

    /// <summary>
    /// Explains the change of net worth per currency between <paramref name="from"/> and <paramref name="to"/>:
    /// flows = surplus of (from, to]; price effect = for each price change of a holding, the quantity held then × the
    /// change; corrections = adjustments, opening balances of new accounts and holdings that came or went without money.
    /// </summary>
    public static IReadOnlyList<WealthChange> Explain(
        IReadOnlyCollection<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        IReadOnlyCollection<AssetType> types,
        IReadOnlyCollection<AssetEvent> events,
        IReadOnlyCollection<AssetValuation> valuations,
        DateOnly from,
        DateOnly to)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(types);
        var start = NetWorthCalculator.Compute(accounts, entries, types, events, valuations, from);
        var end = NetWorthCalculator.Compute(accounts, entries, types, events, valuations, to);
        var flows = KpiCatalog.Surplus(accounts, entries, new LedgerFilter(from.AddDays(1), to, [.. accounts.Select(a => a.Id)]))
            .ToDictionary(s => s.CurrencyCode, s => s.Surplus, StringComparer.OrdinalIgnoreCase);
        var currencies = start.Totals.Select(t => t.CurrencyCode).Concat(end.Totals.Select(t => t.CurrencyCode)).Concat(flows.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToList();
        var result = new List<WealthChange>();
        foreach (var currency in currencies)
        {
            bool Same(string code) => string.Equals(code, currency, StringComparison.OrdinalIgnoreCase);
            var causes = new SortedSet<RemainderCause>();
            long price = 0, corrections = 0;

            // Price effect: every price change inside the range, applied to what was held on its day.
            foreach (var type in types.Where(t => !t.IsArchived && Same(t.PriceCurrencyCode)))
            {
                var days = valuations.Where(v => v.AssetTypeId == type.Id && v.Date > from && v.Date <= to).Select(v => v.Date).Distinct().Order();
                foreach (var day in days)
                {
                    var before = AssetValuationService.PriceAt(valuations, type.Id, day.AddDays(-1));
                    var after = AssetValuationService.PriceAt(valuations, type.Id, day);
                    var held = HoldingsLedger.Quantity(events, type.Id, day);
                    if (before is not null && after is not null && held > 0)
                    {
                        price += AssetValuationService.ValueOf(held, after.PricePerUnitMilli) - AssetValuationService.ValueOf(held, before.PricePerUnitMilli);
                    }
                }

                // Holdings that came or went without money (opening, gift, removal, correction) at the price of their day.
                foreach (var moved in events.Where(e => e.AssetTypeId == type.Id && e.Date > from && e.Date <= to
                             && e.Kind is AssetEventKind.Opening or AssetEventKind.GiftReceived or AssetEventKind.Outflow or AssetEventKind.Correction))
                {
                    if (AssetValuationService.PriceAt(valuations, type.Id, moved.Date) is { } atDay)
                    {
                        corrections += Math.Sign(moved.TotalEffect) * AssetValuationService.ValueOf(Math.Abs(moved.TotalEffect), atDay.PricePerUnitMilli);
                    }
                    else
                    {
                        causes.Add(RemainderCause.MissingValuation);
                    }
                }

                if (events.Any(e => e.AssetTypeId == type.Id && e.Date > from && e.Date <= to && e.Kind is AssetEventKind.Purchase or AssetEventKind.Sale))
                {
                    causes.Add(RemainderCause.TradedAtOtherPrice);
                }
            }

            if (start.Unvalued.Concat(end.Unvalued).Any(u => Same(u.AssetType.PriceCurrencyCode)))
            {
                causes.Add(RemainderCause.MissingValuation);
            }

            // Adjustments and opening balances of accounts opened inside the range are corrections, not flows.
            foreach (var account in accounts.Where(a => Same(a.CurrencyCode)))
            {
                corrections += entries.Where(e => e.Kind == EntryKind.Adjustment && e.Date > from && e.Date <= to && !LedgerCalculator.IsBeforeOpening(e, account))
                    .Sum(e => e.EffectOn(account.Id));
                if (account.OpeningDate > from && account.OpeningDate <= to)
                {
                    corrections += account.Type == AccountType.Loan ? -LoanCalculator.Outstanding(account.Type, account.OpeningBalance) : account.OpeningBalance;
                }

                if (!account.OpeningBalanceKnown)
                {
                    causes.Add(RemainderCause.UnknownOpeningBalance);
                }
            }

            var change = new WealthChange(
                currency, from, to,
                start.Totals.FirstOrDefault(t => Same(t.CurrencyCode))?.Total ?? 0,
                end.Totals.FirstOrDefault(t => Same(t.CurrencyCode))?.Total ?? 0,
                flows.GetValueOrDefault(currency), price, 0, corrections, [.. causes]);
            result.Add(change.Remainder == 0 ? change with { Causes = [] } : change);
        }

        return result;
    }

    /// <summary>
    /// Returns the change in <paramref name="currencyCode"/> (04 §4.1, converted view): each currency's parts at the rate
    /// of the end date, plus the FX effect = start balance × (rate at the end − rate at the start). Currencies without a
    /// rate on one of the dates are left out and named.
    /// </summary>
    public static (WealthChange Change, IReadOnlyList<string> MissingRates) Convert(IReadOnlyList<WealthChange> changes, RateTable rates, string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(rates);
        var missing = new List<string>();
        long start = 0, end = 0, flows = 0, price = 0, fx = 0, corrections = 0;
        var causes = new SortedSet<RemainderCause>();
        var (from, to) = changes.Count == 0 ? (default, default) : (changes[0].From, changes[0].To);
        foreach (var change in changes)
        {
            long AtEnd(long amount) => rates.TryConvert(amount, change.CurrencyCode, currencyCode, to, out var converted, out _) ? converted : 0;
            var sameCurrency = string.Equals(change.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase);
            long startThen = change.Start, startNow = change.Start;
            if (!sameCurrency
                && (!rates.TryConvert(change.Start, change.CurrencyCode, currencyCode, from, out startThen, out _)
                    || !rates.TryConvert(change.Start, change.CurrencyCode, currencyCode, to, out startNow, out _)))
            {
                missing.Add(change.CurrencyCode);
                causes.Add(RemainderCause.MissingRate);
                continue;
            }

            var startFrom = sameCurrency ? change.Start : startThen;
            start += startFrom;
            end += sameCurrency ? change.End : AtEnd(change.End);
            flows += sameCurrency ? change.Flows : AtEnd(change.Flows);
            price += sameCurrency ? change.PriceEffect : AtEnd(change.PriceEffect);
            corrections += sameCurrency ? change.Corrections : AtEnd(change.Corrections);
            fx += sameCurrency ? 0 : startNow - startThen;
            foreach (var cause in change.Causes)
            {
                causes.Add(cause);
            }
        }

        return (new WealthChange(currencyCode, from, to, start, end, flows, price, fx, corrections, [.. causes]), missing);
    }
}
