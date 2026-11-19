using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Reports;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Analysis;

public sealed partial class AiSnapshotStore
{
    /// <summary>Resolves the next financial month for the original file, without interpreting a disclosure.</summary>
    public Task<(DateOnly From, DateOnly To)> UpcomingMonthAsync(string file, DateOnly asOf,
        PeriodCalendar calendar, CancellationToken ct = default) => Task.Run(async () =>
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (DatabaseFileIdentity.ConfiguredPath(db) != file) throw new InvalidOperationException("The original question file changed.");
        return await NextMonthAsync(db, asOf, calendar, ct);
    }, ct);

    private static async Task<(DateOnly From, DateOnly To)> NextMonthAsync(ZananceDbContext db, DateOnly day,
        PeriodCalendar calendar, CancellationToken ct)
    {
        var start = await db.Settings.AsNoTracking().Select(row => (int?)row.MonthStartDay).FirstOrDefaultAsync(ct) ?? 1;
        var current = PeriodMath.MonthOf(day, calendar, start);
        var next = PeriodMath.Next(current.Year, current.Month);
        return PeriodMath.MonthRange(next.Year, next.Month, calendar, start);
    }

    private static async Task<AiQuestionSource> ReadQuestionAsync(ZananceDbContext db, AiSelection selection, CommercialFileAccess rights,
        IReadOnlyList<AiSourceAccount> selected, CancellationToken ct)
    {
        var question = selection.Question!;
        var currency = selected[0].Currency;
        if (selected.Any(item => item.Currency != currency) || question.Currency is { } explicitCurrency && explicitCurrency != currency)
            throw new InvalidOperationException("Select accounts in the requested single currency.");
        return question.Intent == AiQuestionIntent.SpendingChange
            ? await ReadSpendingAsync(db, selection, rights, currency, ct)
            : await ReadPlanShortfallAsync(db, selection, rights, currency, ct);
    }

    private static async Task<AiQuestionSource> ReadSpendingAsync(ZananceDbContext db, AiSelection selection,
        CommercialFileAccess rights, string currency, CancellationToken ct)
    {
        rights.DemandFeature(CommercialFeature.AdvancedReports);
        var accounts = await db.Accounts.AsNoTracking().Where(account => selection.AccountIds.Contains(account.Id)).ToListAsync(ct);
        var categories = await db.Categories.AsNoTracking().ToDictionaryAsync(item => item.Id, ct);
        var start = PreviousFrom(selection);
        var entries = await db.Entries.AsNoTracking().Where(entry => selection.AccountIds.Contains(entry.AccountId) &&
                entry.Date >= start && entry.Date <= selection.To &&
                (entry.Kind == EntryKind.Expense || entry.Kind == EntryKind.Refund))
            .OrderBy(entry => entry.Id).Select(entry => new LedgerEntry(entry.Id) { AccountId = entry.AccountId, Date = entry.Date, Kind = entry.Kind,
                Amount = entry.Amount, CategoryId = entry.CategoryId, Review = entry.Review }).Take(100001).ToListAsync(ct);
        if (entries.Count > 100000) throw new InvalidOperationException("The spending question exceeds its row bound.");
        var range = new ComparisonRange(selection.From, selection.To, start, selection.CompareTo ?? selection.From.AddDays(-1), false);
        var changes = KpiCatalog.SpendingChanges(accounts, entries, range, currency,
            id => id is { } value && categories.TryGetValue(value, out var category) ? category.ParentId ?? id : id,
            selection.AccountIds, selection.ConfirmedOnly).Where(row => row.Delta != 0).Take(AiProtocol.MaximumReportRows).ToArray();
        return new(changes.Length == 0 ? null : new(changes.Select(row => new AiCategoryChange(row.Current, row.Comparison)).ToArray(), null),
            null, changes.Select(row => row.CategoryId is { } id && categories.TryGetValue(id, out var category) ? category.Name ?? "" : "").ToList());
    }

    private static async Task<AiQuestionSource> ReadPlanShortfallAsync(ZananceDbContext db, AiSelection selection,
        CommercialFileAccess rights, string currency, CancellationToken ct)
    {
        rights.DemandFeature(CommercialFeature.AdvancedForecast);
        var day = selection.AsOf!.Value;
        if (selection.Question!.NextMonth &&
            await NextMonthAsync(db, day, selection.QuestionCalendar!.Value, ct) != (selection.From, selection.To))
            throw new InvalidOperationException("The original next-month window changed.");

        var accounts = await db.Accounts.AsNoTracking().ToListAsync(ct);
        if (accounts.Any(item => selection.AccountIds.Contains(item.Id) && (item.IsArchived || !item.UsableForPayments)))
            throw new InvalidOperationException("Select available payment accounts.");
        var entries = await db.Entries.AsNoTracking().Where(entry => entry.Date <= selection.To &&
                (selection.AccountIds.Contains(entry.AccountId) || entry.ToAccountId != null && selection.AccountIds.Contains(entry.ToAccountId.Value)))
            .OrderBy(entry => entry.Id).Select(entry => new LedgerEntry(entry.Id) { AccountId = entry.AccountId, ToAccountId = entry.ToAccountId,
                Date = entry.Date, Kind = entry.Kind, Amount = entry.Amount, ToAmount = entry.ToAmount, Direction = entry.Direction })
            .Take(100001).ToListAsync(ct);
        if (entries.Count > 100000) throw new InvalidOperationException("The forecast question exceeds its row bound.");
        var schedules = await db.Schedules.AsNoTracking().Where(plan => selection.AccountIds.Contains(plan.AccountId) ||
            plan.ToAccountId != null && selection.AccountIds.Contains(plan.ToAccountId.Value)).OrderBy(plan => plan.Id).Take(10001).ToListAsync(ct);
        if (schedules.Count > 10000) throw new InvalidOperationException("The forecast question exceeds its plan bound.");
        var ids = schedules.Select(plan => plan.Id).ToArray();
        var states = await db.OccurrenceStates.AsNoTracking().Where(state => ids.Contains(state.ScheduleId)).Take(100001).ToListAsync(ct);
        if (states.Count > 100000) throw new InvalidOperationException("The forecast question exceeds its occurrence bound.");

        var full = ForecastCalculator.Compute(accounts, entries, schedules, states, day, selection.To, selection.AccountIds).Single();
        // Compute all earlier commitments first so the selected month's opening is the actual preceding daily balance.
        var path = full.Path.Where(point => point.Date >= selection.From).ToArray();
        var minimum = path.MinBy(point => point.Balance);
        var opening = selection.From == day ? full.StartBalance : full.Path.Last(point => point.Date < selection.From).Balance;
        var window = full with { StartBalance = opening, Minimum = minimum.Balance, MinimumDate = minimum.Date, Path = path,
            Items = full.Items.Where(item => item.Date >= selection.From).ToArray() };
        var (facts, labels) = AiForecastEvidence.Project(window);
        var earlierUnknown = full.Items.Count(item => item.Date < selection.From && item.Effect is null && !item.IsExcluded);
        return new(null, new(currency, facts, null, earlierUnknown), labels.ToList());
    }
}
