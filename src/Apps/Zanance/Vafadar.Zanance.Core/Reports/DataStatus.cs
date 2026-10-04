using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Core.Reports;

/// <summary>A reason a number may not be reliable (ZEX-K14). Listed in this order of importance; new values are appended.</summary>
public enum DataIssueKind
{
    /// <summary>The last backup is older than 30 days, or there is none.</summary>
    BackupOld = 0,

    /// <summary>Entries in scope are not reviewed yet.</summary>
    Unreviewed = 1,

    /// <summary>Plan amounts in the horizon are unknown.</summary>
    UnknownAmounts = 2,

    /// <summary>Rates are missing for a converted number.</summary>
    MissingRates = 3,

    /// <summary>Rates used are outdated or estimates.</summary>
    OutdatedRates = 4,

    /// <summary>Holdings have no price.</summary>
    HoldingsWithoutPrice = 5,

    /// <summary>Accounts have an unknown opening balance.</summary>
    OpeningUnknown = 6,

    /// <summary>Accounts were not compared with the bank for more than 60 days.</summary>
    NotReconciled = 7,

    /// <summary>Entries have no category.</summary>
    WithoutCategory = 8,

    /// <summary>Entries look like duplicates (same account, day, kind and amount).</summary>
    PossibleDuplicates = 9,

    /// <summary>Aggregated entries are in the period; their timing is unknown.</summary>
    Aggregated = 10,

    /// <summary>Entries are dated before their account's opening and are not in its balance.</summary>
    BeforeOpening = 11,
}

/// <summary>One item of the data status: what, how many, and the oldest date involved (a rate or a reconciliation).</summary>
public sealed record DataIssue(DataIssueKind Kind, int Count, DateOnly? Date = null, IReadOnlyList<string>? Details = null, IReadOnlyList<Guid>? Ids = null);

/// <summary>
/// The data status of a view (ZEX-K14): a list, never a score. Each calculator reports what makes its number
/// incomplete; this gathers the rest from the ledger. An empty list means "No open issues for this view".
/// </summary>
public static class DataStatus
{
    /// <summary>Days after which an account counts as not reconciled.</summary>
    public const int ReconcileDays = 60;

    /// <summary>Days after which a backup counts as old.</summary>
    public const int BackupDays = 30;

    /// <summary>Returns the issues of the scope and period, most important first.</summary>
    /// <param name="accounts">All accounts.</param>
    /// <param name="entries">All entries.</param>
    /// <param name="categories">All categories.</param>
    /// <param name="filter">Period and account scope of the view.</param>
    /// <param name="today">Today.</param>
    /// <param name="lastBackup">The day of the last successful backup, or <see langword="null"/> when there is none.</param>
    /// <param name="unknownAmounts">Unknown plan amounts reported by the view's calculator.</param>
    /// <param name="rates">Rates used by converted numbers of the view.</param>
    /// <param name="missingRates">Currencies a converted number could not include.</param>
    /// <param name="holdingsWithoutPrice">Holdings in the view without a price.</param>
    public static IReadOnlyList<DataIssue> Check(
        IEnumerable<Account> accounts,
        IReadOnlyCollection<LedgerEntry> entries,
        IEnumerable<Category> categories,
        LedgerFilter filter,
        DateOnly today,
        DateOnly? lastBackup,
        int unknownAmounts = 0,
        IReadOnlyCollection<RateInfo>? rates = null,
        IReadOnlyCollection<string>? missingRates = null,
        int holdingsWithoutPrice = 0)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(filter);
        var scope = LedgerCalculator.InScope(accounts, filter.AccountIds).Where(a => !a.IsArchived).ToDictionary(a => a.Id);
        var uncategorized = categories.Where(c => c.SystemKey == DefaultCategories.Uncategorized).Select(c => c.Id).ToHashSet();
        var inPeriod = entries.Where(e => e.Date >= filter.From && e.Date <= filter.To && (scope.ContainsKey(e.AccountId) || (e.ToAccountId is { } to && scope.ContainsKey(to)))).ToList();
        var issues = new List<DataIssue>();
        void Add(DataIssueKind kind, int count, DateOnly? date = null, IReadOnlyList<string>? details = null, IReadOnlyList<Guid>? ids = null)
        {
            if (count > 0)
            {
                issues.Add(new DataIssue(kind, count, date, details, ids));
            }
        }

        if (lastBackup is not { } backup || today.DayNumber - backup.DayNumber > BackupDays)
        {
            Add(DataIssueKind.BackupOld, entries.Count > 0 ? 1 : 0, lastBackup);
        }

        var unreviewed = inPeriod.Where(e => e.Review == ReviewState.Unreviewed).ToList();
        Add(DataIssueKind.Unreviewed, unreviewed.Count, ids: [.. unreviewed.Select(e => e.Id)]);
        Add(DataIssueKind.UnknownAmounts, unknownAmounts);
        Add(DataIssueKind.MissingRates, missingRates?.Count ?? 0, details: missingRates?.ToList());
        var stale = rates?.Where(r => r.IsOutdated || r.IsEstimate).ToList() ?? [];
        Add(DataIssueKind.OutdatedRates, stale.Count, stale.Where(r => r.Date is not null).Select(r => r.Date).Min(), [.. stale.Select(r => r.CurrencyCode)]);
        Add(DataIssueKind.HoldingsWithoutPrice, holdingsWithoutPrice);
        var opening = scope.Values.Where(a => !a.OpeningBalanceKnown).ToList();
        Add(DataIssueKind.OpeningUnknown, opening.Count, details: [.. opening.Select(a => a.Name)], ids: [.. opening.Select(a => a.Id)]);

        // Never compared, or compared long ago – only accounts open longer than the limit.
        var notReconciled = scope.Values
            .Where(a => (a.LastReconciledOn ?? a.OpeningDate).DayNumber < today.DayNumber - ReconcileDays)
            .ToList();
        Add(DataIssueKind.NotReconciled, notReconciled.Count, notReconciled.Select(a => a.LastReconciledOn).Where(d => d is not null).Min(), [.. notReconciled.Select(a => a.Name)], [.. notReconciled.Select(a => a.Id)]);

        var withoutCategory = inPeriod.Where(e => e.Kind is EntryKind.Income or EntryKind.Expense && (e.CategoryId is null || uncategorized.Contains(e.CategoryId.Value))).ToList();
        Add(DataIssueKind.WithoutCategory, withoutCategory.Count, ids: [.. withoutCategory.Select(e => e.Id)]);
        Add(DataIssueKind.PossibleDuplicates, Duplicates(inPeriod).Count);
        Add(DataIssueKind.Aggregated, inPeriod.Count(e => e.IsAggregated));
        Add(DataIssueKind.BeforeOpening, inPeriod.Count(e => scope.TryGetValue(e.AccountId, out var account) && LedgerCalculator.IsBeforeOpening(e, account)));
        return [.. issues.OrderBy(i => i.Kind)];
    }

    /// <summary>
    /// Returns entries that look like a second copy of another one: same account, day, kind and amount, not part of the
    /// same group (a split or a transfer with its fee). Only a hint – two real purchases can look the same (AT-53).
    /// </summary>
    public static IReadOnlyList<LedgerEntry> Duplicates(IEnumerable<LedgerEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return
        [
            .. entries.GroupBy(e => (e.AccountId, e.Date, e.Kind, e.Amount))
                .Where(g => g.Count() > 1 && g.Select(e => e.GroupId ?? e.Id).Distinct().Count() > 1)
                .SelectMany(g => g.OrderBy(e => e.CreatedAt).Skip(1)),
        ];
    }
}
