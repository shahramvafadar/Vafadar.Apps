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
/// entered after saving), and plans that changed (the rest).
/// </summary>
public sealed record SnapshotComparison(IReadOnlyList<SnapshotDay> Days, long RecordedLater, long UnplannedSpending, long UnplannedIncome)
{
    /// <summary>Gets the difference on the last compared day.</summary>
    public long Difference => Days.Count == 0 ? 0 : Days[^1].Difference;

    /// <summary>Gets the part of the difference that comes from plans paid with other amounts, on other days or not at all.</summary>
    public long PlansChanged => Difference - RecordedLater - UnplannedSpending - UnplannedIncome;
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
        var scoped = accounts.Where(a => scope.Contains(a.Id)).ToList();
        var end = today < snapshot.Horizon ? today : snapshot.Horizon;
        var saved = snapshot.Points().Where(p => p.Date <= end).ToList();
        var days = saved.Select(p => new SnapshotDay(p.Date, p.Balance, scoped.Sum(a => LedgerCalculator.Balance(a, entries, p.Date)))).ToList();

        // The effect of an entry on the whole scope: a transfer between two accounts of the scope moves nothing.
        long Effect(LedgerEntry entry) => scoped.Where(a => !LedgerCalculator.IsBeforeOpening(entry, a)).Sum(a => entry.EffectOn(a.Id));
        var later = entries.Where(e => e.CreatedAt > snapshot.CreatedAt).ToList();
        var recordedLater = later.Where(e => e.Date <= snapshot.BaseDate).Sum(Effect);
        var unplanned = later.Where(e => e.Date > snapshot.BaseDate && e.Date <= end && e.ScheduleId is null).Select(Effect).ToList();
        return new SnapshotComparison(days, recordedLater, unplanned.Where(v => v < 0).Sum(), unplanned.Where(v => v > 0).Sum());
    }
}
