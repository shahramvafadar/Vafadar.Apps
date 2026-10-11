using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Complete immutable original period data for a reviewed definition choice, never a filtered financial input.</summary>
public sealed record BudgetChoiceItem(Guid Id, int Year, int Month, PeriodCalendar Calendar, BudgetPeriod Period,
    DateOnly PeriodStart, string CurrencyCode, BudgetMethod Method, BudgetRollover Rollover, long? TotalLimit,
    bool AlertsEnabled, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string Accounts, string Limits)
{
    /// <summary>Returns canonical financial identity without the row id, dates, calendar, method or limits.</summary>
    public BudgetDefinitionKey Definition(QuotaScope scope) => new(scope, CurrencyCode, Period,
        Accounts.Length == 0 ? [] : Accounts.Split(';').Select(Guid.Parse));

    /// <summary>Captures all original scalar values and the complete ordered owned account/limit packets.</summary>
    internal static BudgetChoiceItem From(Budget budget) => new(budget.Id, budget.Year, budget.Month, budget.Calendar,
        budget.Period, budget.PeriodStart, budget.CurrencyCode, budget.Method, budget.Rollover, budget.TotalLimit,
        budget.AlertsEnabled, budget.CreatedAt, budget.UpdatedAt, string.Join(';', budget.AccountIds.Select(id => id.ToString("N"))),
        string.Join(';', budget.CategoryLimits.Select(limit => FormattableString.Invariant($"{limit.CategoryId:N}:{limit.Limit}"))));
}

/// <summary>Actual-file review retaining all periods and independent current new-work eligibility.</summary>
public sealed class BudgetChoiceSnapshot
{
    /// <summary>Retains the reviewed actual-file inputs independently of later entity/caller mutations.</summary>
    internal BudgetChoiceSnapshot(IReadOnlyList<BudgetChoiceItem> items, string path, CommercialFileAccess access,
        StoredResourceSelection? stored, DateOnly today, int monthStartDay, BudgetAvailability? availability, BudgetWorkPolicy work)
    {
        Items = Array.AsReadOnly(items.ToArray()); DatabasePath = path; Access = access; Revision = stored?.Revision; StoredValue = stored?.IdentitySet;
        Today = today; MonthStartDay = monthStartDay; Maximum = availability?.Maximum;
        SelectedDefinitions = availability?.SelectedDefinitions ?? Array.AsReadOnly(Array.Empty<BudgetDefinitionKey>());
        RequiresSelection = availability?.RequiresSelection == true || Maximum is > 0 && stored is not null && !StoredBudgetSelection.IsValid(stored);
        Work = work;
    }
    /// <summary>Gets every complete original period, including unselected budgets and historical paid methods.</summary>
    public IReadOnlyList<BudgetChoiceItem> Items { get; }
    /// <summary>Gets the actual local date used for the reviewed financial periods.</summary>
    public DateOnly Today { get; }
    /// <summary>Gets the original financial month-start preference, without creating Settings.</summary>
    public int MonthStartDay { get; }
    /// <summary>Gets current capacity; null means unrestricted.</summary>
    public int? Maximum { get; }
    /// <summary>Gets whether an explicit bounded choice can be saved.</summary>
    public bool CanChoose => Maximum is > 0;
    /// <summary>Gets whether stale, malformed or excessive choices require explicit review.</summary>
    public bool RequiresSelection { get; }
    /// <summary>Gets currently selected definitions, never guessed row identities.</summary>
    public IReadOnlyList<BudgetDefinitionKey> SelectedDefinitions { get; }
    /// <summary>Gets separate current new-work rights; original calculations continue using every original budget.</summary>
    public BudgetWorkPolicy Work { get; }
    /// <summary>Gets the exact captured file, private to the data boundary.</summary>
    internal string DatabasePath { get; }
    /// <summary>Gets the immutable synchronous rights guard, without exposing credentials or portable grants.</summary>
    internal CommercialFileAccess Access { get; }
    /// <summary>Gets the original choice revision, including the distinct missing-row state.</summary>
    internal Guid? Revision { get; }
    /// <summary>Gets the complete original canonical value, also detecting edits without a revision change.</summary>
    internal string? StoredValue { get; }
}
