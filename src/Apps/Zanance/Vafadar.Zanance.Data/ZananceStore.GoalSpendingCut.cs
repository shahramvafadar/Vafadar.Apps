using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

/// <summary>The actual monthly category limit reviewed before a goal draft's explicit budget confirmation.</summary>
public sealed class GoalBudgetCutReview
{
    internal GoalBudgetCutReview(GoalWorkSnapshot goals, Guid? goalId, GoalType type, Budget budget,
        Category category, string settingsStamp)
    {
        Goals = goals; GoalId = goalId; Type = type; BudgetId = budget.Id; CategoryId = category.Id;
        Category = category; CategoryStamp = CaptureCategory(category);
        CurrencyCode = budget.CurrencyCode; OriginalLimit = budget.CategoryLimits.Single(limit => limit.CategoryId == category.Id).Limit;
        BudgetStamp = Capture(budget); SettingsStamp = settingsStamp;
    }

    /// <summary>Gets the original category limit, never an account balance or transaction amount.</summary>
    public long OriginalLimit { get; }
    /// <summary>Gets the original budget currency used in the native confirmation.</summary>
    public string CurrencyCode { get; }
    /// <summary>Gets the current reviewed category for localized native confirmation wording.</summary>
    public Category Category { get; }
    internal Guid BudgetId { get; }
    internal Guid CategoryId { get; }
    internal Guid? GoalId { get; }
    internal GoalType Type { get; }
    internal GoalWorkSnapshot Goals { get; }
    internal string BudgetStamp { get; }
    internal string SettingsStamp { get; }
    internal string CategoryStamp { get; }

    // All original limits and metadata participate, including equal-time concurrent changes.
    internal static string Capture(Budget budget) => FormattableString.Invariant(
        $"{budget.Id:N}|{budget.Period}|{budget.PeriodStart.DayNumber}|{budget.Year}|{budget.Month}|{budget.Calendar}|{budget.CurrencyCode}|{budget.TotalLimit}|{budget.Method}|{budget.Rollover}|{budget.AlertsEnabled}|{budget.CreatedAt.UtcTicks}|{budget.UpdatedAt.UtcTicks}")
        + "|" + string.Join(',', budget.AccountIds)
        + "|" + string.Join(';', budget.CategoryLimits.OrderBy(limit => limit.CategoryId)
            .Select(limit => FormattableString.Invariant($"{limit.CategoryId:N}:{limit.Limit}")));

    internal static string CaptureSettings(ZananceSettings? settings, DateOnly today) => FormattableString.Invariant(
        $"{settings?.Id:N}|{settings?.BudgetCalendar ?? PeriodCalendar.Gregorian}|{settings?.MonthStartDay ?? 1}|{settings?.UpdatedAt.UtcTicks}|{today.DayNumber}");

    internal static string CaptureCategory(Category category) => FormattableString.Invariant(
        $"{category.Id:N}|{category.Kind}|{category.ParentId:N}|{category.IsArchived}|{category.Name}|{category.SystemKey}|{category.UpdatedAt.UtcTicks}");
}

public sealed partial class ZananceStore
{
    /// <summary>Reads the actual current monthly limit without changing the goal, contribution, budget or ledger.</summary>
    public async Task<GoalBudgetCutReview?> ReviewGoalBudgetCutAsync(GoalWorkSnapshot review, Guid? goalId,
        GoalType type, Guid categoryId, string currencyCode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(review);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await GoalStore.ValidateWorkSnapshotAsync(review, db, cancellationToken);
        DemandGoalCutWork(review, goalId, type);
        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
        if (category is null || category.Kind != CategoryKind.Expense || category.IsArchived) return null;
        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var calendar = settings?.BudgetCalendar ?? PeriodCalendar.Gregorian;
        var (year, month) = PeriodMath.MonthOf(today, calendar, settings?.MonthStartDay ?? 1);
        var budget = await db.Budgets.AsNoTracking().FirstOrDefaultAsync(b => b.Period == BudgetPeriod.Month
            && b.Year == year && b.Month == month && b.Calendar == calendar && b.CurrencyCode == currencyCode, cancellationToken);
        if (budget is null || !budget.CategoryLimits.Any(limit => limit.CategoryId == categoryId)) return null;
        await GoalStore.ValidateWorkSnapshotAsync(review, db, cancellationToken);
        return new(review, goalId, type, budget, category, GoalBudgetCutReview.CaptureSettings(settings, today));
    }

    /// <summary>Applies only the explicitly confirmed category cut under the same reviewed actual-file writer.</summary>
    public async Task ApplyGoalBudgetCutAsync(GoalBudgetCutReview review, long cut, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(review);
        if (cut <= 0) throw new ArgumentOutOfRangeException(nameof(cut));
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        await GoalStore.ValidateWorkSnapshotAsync(review.Goals, db, cancellationToken);
        DemandGoalCutWork(review.Goals, review.GoalId, review.Type);
        write.DemandFeature(CommercialFeature.AdvancedGoals);
        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == review.CategoryId, cancellationToken);
        if (category is null || category.Kind != CategoryKind.Expense || category.IsArchived
            || GoalBudgetCutReview.CaptureCategory(category) != review.CategoryStamp)
            throw new InvalidOperationException("The category changed after the spending cut was reviewed.");
        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (GoalBudgetCutReview.CaptureSettings(settings, DateOnly.FromDateTime(time.GetLocalNow().DateTime)) != review.SettingsStamp)
            throw new InvalidOperationException("The financial period changed after the category cut was reviewed.");
        var budget = await db.Budgets.FirstOrDefaultAsync(b => b.Id == review.BudgetId, cancellationToken);
        if (budget is null || GoalBudgetCutReview.Capture(budget) != review.BudgetStamp || review.OriginalLimit < 0)
            throw new InvalidOperationException("The budget changed after the category cut was reviewed.");
        await CheckCommercialBudgetSaveAsync(db, write, budget, budget, cancellationToken);
        budget.CategoryLimits.Single(limit => limit.CategoryId == review.CategoryId).Limit = Math.Max(0, review.OriginalLimit - cut);
        budget.UpdatedAt = time.GetUtcNow();
        write.EnsureCurrent(); await db.SaveChangesAsync(cancellationToken); await write.CommitAsync(cancellationToken);
        OnChanged();
    }

    private static void DemandGoalCutWork(GoalWorkSnapshot review, Guid? goalId, GoalType type)
    {
        review.Access?.DemandFeature(CommercialFeature.AdvancedGoals);
        if (type is not (GoalType.Earmark or GoalType.AccountBalance))
            throw new ArgumentException("A spending cut requires a monetary goal draft.", nameof(type));
        var allowed = goalId is { } id
            ? review.Goals.Any(goal => goal.Id == id && goal.State == GoalState.Active && goal.Type == type) && review.Work.CanContribute(id)
            : review.Work.CanCreateContribution(type);
        if (!allowed) throw new InvalidOperationException("New goal contribution work is unavailable for this reviewed draft.");
    }
}
