using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Core.Reports;

/// <summary>The groups of net worth (ZEX-K10). New values are appended.</summary>
public enum WealthGroup
{
    /// <summary>Cash, checking, savings and positive card balances.</summary>
    Money = 0,

    /// <summary>Money lent and open reimbursements.</summary>
    Receivables = 1,

    /// <summary>Accounts of valued assets (a car, a flat).</summary>
    ValuedAssets = 2,

    /// <summary>Holdings at their latest price.</summary>
    Holdings = 3,

    /// <summary>Loans and negative card balances (subtracted).</summary>
    Debts = 4,
}

/// <summary>One part of net worth: an account, an open reimbursement or a valued holding; debts are positive amounts.</summary>
public sealed record WealthPart(WealthGroup Group, Guid SourceId, string Name, string CurrencyCode, long Amount);

/// <summary>A holding without a price on the date: listed with its quantity, never valued as zero.</summary>
public sealed record UnvaluedHolding(AssetType AssetType, long Quantity);

/// <summary>Net worth of one currency (ZEX-K10): money + receivables + valued assets + holdings − debts.</summary>
public sealed record WealthTotal(string CurrencyCode, long Money, long Receivables, long ValuedAssets, long Holdings, long Debts)
{
    /// <summary>Gets the net worth of the currency.</summary>
    public long Total => Money + Receivables + ValuedAssets + Holdings - Debts;
}

/// <summary>Net worth per currency with its parts and the holdings that have no price (ZEX-K10).</summary>
public sealed record NetWorthResult(DateOnly Date, IReadOnlyList<WealthTotal> Totals, IReadOnlyList<WealthPart> Parts, IReadOnlyList<UnvaluedHolding> Unvalued)
{
    /// <summary>Gets a value indicating whether holdings without a price make the result incomplete.</summary>
    public bool IsIncomplete => Unvalued.Count > 0;
}

/// <summary>A slice of the composition: one currency of money, receivables and valued assets, or one holding type.</summary>
/// <param name="Label">The currency code or the holding type name.</param>
/// <param name="IsHolding">Whether the slice is a holding.</param>
/// <param name="Value">Its value in the valuation currency.</param>
/// <param name="SharePercent">Its share of the known part in percent (one decimal).</param>
public sealed record CompositionSlice(string Label, bool IsHolding, long Value, decimal SharePercent);

/// <summary>
/// The composition of wealth in the valuation currency (ZEX-K13): shares of the known part only; currencies without a
/// rate and holdings without a price are listed, debts are a separate bar.
/// </summary>
public sealed record Composition(string CurrencyCode, IReadOnlyList<CompositionSlice> Slices, long Debts, IReadOnlyList<string> MissingRates, IReadOnlyList<UnvaluedHolding> Unvalued, IReadOnlyList<RateInfo> Rates)
{
    /// <summary>Gets the known total the shares are taken of.</summary>
    public long Known => Slices.Sum(s => s.Value);
}

/// <summary>Net worth and its composition (ZEX-S0605).</summary>
public static class NetWorthCalculator
{
    /// <summary>
    /// Returns net worth per currency at <paramref name="date"/>: every account that is not archived, archived ones only
    /// with a non-zero balance, open reimbursements as receivables, and holdings at their latest price on or before the
    /// date (in their price currency). Credit limits and earmarks are never part of it.
    /// </summary>
    public static NetWorthResult Compute(
        IEnumerable<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        IEnumerable<AssetType> types,
        IReadOnlyCollection<AssetEvent> events,
        IReadOnlyCollection<AssetValuation> valuations,
        DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(entries);
        var parts = new List<WealthPart>();
        var accountList = accounts.ToList();
        foreach (var account in accountList)
        {
            var balance = LedgerCalculator.Balance(account, entries, date);
            if (account.IsArchived && balance == 0)
            {
                continue;
            }

            var (group, amount) = account.Type switch
            {
                AccountType.Loan => (WealthGroup.Debts, LoanCalculator.Outstanding(account.Type, balance)),
                AccountType.Lent => (WealthGroup.Receivables, balance),
                AccountType.Asset => (WealthGroup.ValuedAssets, balance),
                _ when balance < 0 && account.Type == AccountType.CreditCard => (WealthGroup.Debts, -balance),
                _ => (WealthGroup.Money, balance),
            };
            parts.Add(new WealthPart(group, account.Id, account.Name, account.CurrencyCode, amount));
        }

        // An expense another person pays back is still money owed to the user (ZEX-K12).
        var byId = accountList.ToDictionary(a => a.Id);
        foreach (var (expense, open) in EntryActions.OpenReimbursements([.. entries.Where(e => e.Date <= date)]))
        {
            if (byId.TryGetValue(expense.AccountId, out var account))
            {
                parts.Add(new WealthPart(WealthGroup.Receivables, expense.Id, expense.ReimbursedBy ?? expense.Title ?? account.Name, account.CurrencyCode, open));
            }
        }

        var unvalued = new List<UnvaluedHolding>();
        foreach (var value in AssetValuationService.Values(types.Where(t => !t.IsArchived), events, valuations, date))
        {
            if (value.Value is { } known)
            {
                parts.Add(new WealthPart(WealthGroup.Holdings, value.AssetType.Id, value.AssetType.Name, value.AssetType.PriceCurrencyCode, known));
            }
            else if (value.Quantity > 0)
            {
                unvalued.Add(new UnvaluedHolding(value.AssetType, value.Quantity));
            }
        }

        var totals = parts.GroupBy(p => p.CurrencyCode, StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new WealthTotal(
                g.Key,
                g.Where(p => p.Group == WealthGroup.Money).Sum(p => p.Amount),
                g.Where(p => p.Group == WealthGroup.Receivables).Sum(p => p.Amount),
                g.Where(p => p.Group == WealthGroup.ValuedAssets).Sum(p => p.Amount),
                g.Where(p => p.Group == WealthGroup.Holdings).Sum(p => p.Amount),
                g.Where(p => p.Group == WealthGroup.Debts).Sum(p => p.Amount)))
            .ToList();
        return new NetWorthResult(date, totals, parts, unvalued);
    }

    /// <summary>
    /// Returns the composition in <paramref name="currencyCode"/> (ZEX-K13): positive money, receivables and valued
    /// assets per currency, and each valued holding type, converted with rates dated on or before the date. Shares are of
    /// the known part; what cannot be converted or valued is listed, never counted as zero.
    /// </summary>
    public static Composition Compose(NetWorthResult netWorth, RateTable rates, string currencyCode, RateFreshness? freshness = null)
    {
        ArgumentNullException.ThrowIfNull(netWorth);
        ArgumentNullException.ThrowIfNull(rates);
        var values = new Dictionary<(string Label, bool IsHolding), long>();
        var missing = new SortedSet<string>(StringComparer.Ordinal);
        var used = new Dictionary<string, RateInfo>(StringComparer.OrdinalIgnoreCase);
        long debts = 0;
        bool TryConvert(long amount, string from, out long converted)
        {
            if (string.Equals(from, currencyCode, StringComparison.OrdinalIgnoreCase))
            {
                converted = amount;
                return true;
            }

            if (rates.TryConvert(amount, from, currencyCode, netWorth.Date, out converted, out var rateDate, out var estimate))
            {
                used[from] = new RateInfo(from, rateDate, estimate, (freshness ?? RateFreshness.Default).IsOutdated(rateDate, netWorth.Date));
                return true;
            }

            missing.Add(from);
            return false;
        }

        foreach (var part in netWorth.Parts.Where(p => p.Amount != 0))
        {
            if (!TryConvert(part.Amount, part.CurrencyCode, out var converted))
            {
                continue;
            }

            if (part.Group == WealthGroup.Debts)
            {
                debts += converted;
            }
            else if (converted > 0)
            {
                var key = part.Group == WealthGroup.Holdings ? (part.Name, true) : (part.CurrencyCode.ToUpperInvariant(), false);
                values[key] = values.GetValueOrDefault(key) + converted;
            }
        }

        var known = values.Values.Sum();
        var slices = values.Where(v => v.Value > 0)
            .Select(v => new CompositionSlice(v.Key.Label, v.Key.IsHolding, v.Value, known > 0 ? Math.Round(v.Value * 100m / known, 1, MidpointRounding.AwayFromZero) : 0))
            .OrderByDescending(s => s.Value)
            .ToList();
        return new Composition(currencyCode, slices, debts, [.. missing], netWorth.Unvalued, [.. used.Values]);
    }
}
