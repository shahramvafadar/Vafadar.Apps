using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Forecasts;

/// <summary>One day of a saved forecast next to the balance that really came.</summary>
public sealed record SnapshotDay(DateOnly Date, long Saved, long Actual)
{
    /// <summary>Gets actual minus saved.</summary>
    public long Difference => Actual - Saved;
}

/// <summary>
/// A saved forecast compared with reality (ZEX-S0804, 04 §4.2): the saved path, the actual balances of the same accounts
/// from today's ledger, and the difference on the last day split into parts that add up to it – recorded later
/// (entries dated up to the base date but entered after saving), unplanned spending and income (entries without a plan
/// entered after saving), other account movements, and remaining changes to plans or existing data.
/// </summary>
public sealed record SnapshotComparison(IReadOnlyList<SnapshotDay> Days, long RecordedLater, long UnplannedSpending, long UnplannedIncome, long OtherMovements = 0)
{
    /// <summary>Gets the difference on the last compared day.</summary>
    public long Difference => Days.Count == 0 ? 0 : Days[^1].Difference;

    /// <summary>Gets the residual difference from changed planned transactions or earlier data, after the known movement categories.</summary>
    public long PlansChanged => Difference - RecordedLater - UnplannedSpending - UnplannedIncome - OtherMovements;
}

/// <summary>Compares a forecast snapshot with what happened (ZEX-S0804).</summary>
public static class SnapshotComparer
{
    /// <summary>Compares <paramref name="snapshot"/> from its base date up to today or its horizon, whichever comes first.</summary>
    public static SnapshotComparison Compare(ForecastSnapshot snapshot, IReadOnlyCollection<Account> accounts, IReadOnlyCollection<LedgerEntry> entries, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(entries);
        var scope = snapshot.Scope().ToHashSet();
        // Earlier multi-currency saves could carry every account id in each currency's scope (D-169).
        // Retain that immutable saved metadata, but never add another currency's minor units to this comparison.
        var scoped = accounts.Where(a => scope.Contains(a.Id)
            && string.Equals(a.CurrencyCode, snapshot.CurrencyCode, StringComparison.OrdinalIgnoreCase)).ToList();
        var end = today < snapshot.Horizon ? today : snapshot.Horizon;
        var saved = snapshot.Points().Where(p => p.Date <= end).ToList();

        // The effect of an entry on the whole scope: a transfer between two accounts of the scope moves nothing.
        long Effect(LedgerEntry entry) => scoped.Where(a => !LedgerCalculator.IsBeforeOpening(entry, a)).Sum(a => entry.EffectOn(a.Id));

        // The actual balances: the first day from the ledger, every later day by adding what happened on it – entries and
        // the opening balance of an account that opens that day – instead of reading the whole ledger again per day.
        var days = new List<SnapshotDay>(saved.Count);
        if (saved.Count > 0)
        {
            var first = saved[0].Date;
            var balance = scoped.Sum(a => LedgerCalculator.Balance(a, entries, first));
            var deltas = new Dictionary<DateOnly, long>();
            foreach (var entry in entries.Where(e => e.Date > first && e.Date <= end))
            {
                deltas[entry.Date] = deltas.GetValueOrDefault(entry.Date) + Effect(entry);
            }

            foreach (var account in scoped.Where(a => a.OpeningDate > first && a.OpeningDate <= end))
            {
                deltas[account.OpeningDate] = deltas.GetValueOrDefault(account.OpeningDate) + account.OpeningBalance;
            }

            var day = first;
            foreach (var point in saved)
            {
                for (; day < point.Date; day = day.AddDays(1))
                {
                    balance += deltas.GetValueOrDefault(day.AddDays(1));
                }

                days.Add(new SnapshotDay(point.Date, point.Balance, balance));
            }
        }
        var later = entries.Where(e => e.CreatedAt > snapshot.CreatedAt).ToList();
        var recordedLater = later.Where(e => e.Date <= snapshot.BaseDate).Sum(Effect);
        var unplanned = later.Where(e => e.Date > snapshot.BaseDate && e.Date <= end && e.ScheduleId is null).ToList();
        // D-487: classify original ledger kinds, not cash signs. Refunds reduce spending and
        // reversals reduce income; transfers, capital trades and corrections are neither.
        var spending = unplanned.Where(e => e.Kind is EntryKind.Expense or EntryKind.Refund).Sum(Effect);
        var income = unplanned.Where(e => e.Kind is EntryKind.Income or EntryKind.IncomeReversal).Sum(Effect);
        var other = unplanned.Where(e => e.Kind is not (EntryKind.Expense or EntryKind.Refund or EntryKind.Income or EntryKind.IncomeReversal)).Sum(Effect);
        return new SnapshotComparison(days, recordedLater, spending, income, other);
    }
}
