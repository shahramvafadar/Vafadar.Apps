using System.Text.Json;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.Performance;

/// <summary>Compares the original public Home read sequence with the coherent production operation on the same retained data.</summary>
internal static class HomeReadWorkload
{
    /// <summary>Executes the prior public read sequence, with no copied financial or resource-policy algorithms.</summary>
    internal static async Task<HomeReadSnapshot> ReadOriginalAsync(ZananceStore store, PlanStore plans, GoalStore goals,
        HoldingStore holdings, DateOnly day, CancellationToken ct)
    {
        var quick = await store.GetQuickResourceReviewAsync(cancellationToken: ct);
        var entries = await store.GetEntriesAsync(cancellationToken: ct);
        var plan = await plans.GetWorkSnapshotAsync(ct);
        var settings = quick.Settings ?? new ZananceSettings();
        // Match Home's actual currency-read condition without including unrelated UI calculations in the timer.
        var currencies = LedgerCalculator.InScope(quick.Accounts, null).Where(account => !account.IsArchived)
            .Select(account => account.CurrencyCode).Distinct(StringComparer.Ordinal).Count();
        var rates = settings.ValuationCurrencyEnabled && currencies > 1 ? await store.GetRatesAsync(ct) : new List<ExchangeRate>();
        var (year, month) = PeriodMath.MonthOf(day, settings.BudgetCalendar, settings.MonthStartDay);
        var budget = await store.GetBudgetAsync(year, month, settings.BudgetCalendar, BudgetCurrency.Resolve(settings, quick.Accounts), ct);
        var carry = budget is null ? BudgetCarry.None : await store.GetBudgetCarryAsync(budget, ct);
        var budgets = await store.GetBudgetsAsync(ct);
        var goal = await goals.GetWorkSnapshotAsync(ct);
        var types = await holdings.GetTypesAsync(ct); var events = await holdings.GetEventsAsync(cancellationToken: ct);
        await goals.ValidateWorkSnapshotAsync(goal, ct);
        _ = await holdings.GetTypesAsync(ct);
        if (types.Any(type => !type.IsArchived)) _ = await holdings.GetEventsAsync(cancellationToken: ct);
        await store.ValidateQuickResourceReviewAsync(quick, ct);
        return new(quick, plan, goal, entries, budgets, rates, types, events, budget, carry, 0);
    }

    /// <summary>Requires all original financial packets and each original activity projection, not only counts or sums.</summary>
    internal static string Capture(HomeReadSnapshot packet) => JsonSerializer.Serialize(new
    {
        packet.QuickResources.Settings, packet.QuickResources.Accounts, packet.QuickResources.Categories,
        packet.QuickResources.Templates, packet.QuickResources.Filters, packet.Entries, packet.Budgets, packet.Rates,
        packet.AssetTypes, packet.AssetEvents, packet.MonthBudget, packet.Carry, packet.Plans.Schedules, packet.Plans.States,
        packet.Goals.Goals, packet.Goals.Allocations, packet.Goals.ContributionPlans,
        TemplateActivity = packet.QuickResources.Templates.Select(item => packet.QuickResources.Work.CanApplyTemplate(item.Id)),
        PlanActivity = packet.Plans.Schedules.Select(item => packet.Plans.Work.CanGenerate(item.Id)),
        GoalActivity = packet.Goals.Goals.Select(item => packet.Goals.Work.CanContribute(item.Id)),
    });
}
