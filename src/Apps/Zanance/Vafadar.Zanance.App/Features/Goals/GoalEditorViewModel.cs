using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>A category choice of a spending cut.</summary>
public sealed record CutCategory(Guid Id, string Name)
{
    public override string ToString() => Name;
}

/// <summary>
/// Creates or edits a goal (F2-GOAL-01, ZEX-S0302, S0305, S0306): a balance on one account or money set aside, a target,
/// an optional date, the contribution dates and the user's plan. A preview shows progress, the contribution needed per
/// date and the estimated date before anything is saved (ZEX-GO14). A plan is a plan for the user: Zanance moves no money
/// and changes no budget unless the user taps "Apply to budget".
/// </summary>
public sealed partial class GoalEditorViewModel : ViewModelBase, IQueryAttributable
{
    // The schedule choices: monthly, every two weeks, weekly (ZEX-P14).
    private static readonly (Frequency Frequency, int Interval)[] Schedules = [(Frequency.Monthly, 1), (Frequency.Weekly, 2), (Frequency.Weekly, 1)];

    private readonly ZananceStore _store;
    private readonly GoalStore _goals;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly IDateFormatter _dates;
    private readonly TimeProvider _time;
    private Goal? _existing;
    private ContributionPlan? _existingPlan;
    private List<Account> _accounts = [];
    private List<LedgerEntry> _entries = [];
    private long _currentEarmarked;
    private bool _loading;

    public GoalEditorViewModel(ZananceStore store, GoalStore goals, Translator translator, ILocalizationService localization, IDateFormatter dates, TimeProvider time)
    {
        _store = store;
        _goals = goals;
        _translator = translator;
        _localization = localization;
        _dates = dates;
        _time = time;
        Title = translator["Goal_NewTitle"];
        Name = string.Empty;
        AmountText = string.Empty;
        Note = string.Empty;
        CurrencyCode = Currencies.Euro.Code;
        TargetDate = Today.AddYears(1);
        FirstDate = Today;
        TypeNames = [translator["GoalType_AccountBalance"], translator["GoalType_Earmark"]];
        ScheduleNames = [translator["Goal_Monthly"], translator["Goal_EveryTwoWeeks"], translator["Goal_Weekly"]];
        MethodNames = [translator["Contribution_Fixed"], translator["Contribution_ShareOfIncome"], translator["Contribution_SpendingCut"]];
        PriorityNames = [translator["GoalPriority_High"], translator["GoalPriority_Normal"], translator["GoalPriority_Low"]];
        PriorityIndex = (int)GoalPriority.Normal;
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];

    public IReadOnlyList<string> TypeNames { get; }

    public IReadOnlyList<string> ScheduleNames { get; }

    public IReadOnlyList<string> MethodNames { get; }

    public IReadOnlyList<string> PriorityNames { get; }

    /// <summary>Gets the money accounts a balance goal can follow.</summary>
    public ObservableCollection<AccountChoice> Accounts { get; } = [];

    /// <summary>Gets the expense categories a spending cut can use.</summary>
    public ObservableCollection<CutCategory> CutCategories { get; } = [];

    [ObservableProperty]
    public partial string Title { get; set; }

    /// <summary>Gets or sets the goal type: 0 = balance on one account, 1 = money set aside.</summary>
    [ObservableProperty]
    public partial int TypeIndex { get; set; }

    [ObservableProperty]
    public partial bool CanChangeType { get; set; } = true;

    [ObservableProperty]
    public partial bool IsBalanceGoal { get; set; } = true;

    /// <summary>Gets a value indicating whether the type can be chosen: new goals in Advanced (Simple creates balance goals).</summary>
    [ObservableProperty]
    public partial bool ShowTypeChooser { get; set; }

    [ObservableProperty]
    public partial bool IsAdvanced { get; set; }

    [ObservableProperty]
    public partial AccountChoice? Account { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string AmountText { get; set; }

    [ObservableProperty]
    public partial string CurrencyCode { get; set; }

    // Amounts are typed in the currency's display unit when one is defined (FX-07).
    [ObservableProperty]
    public partial string? UnitNote { get; set; }

    [ObservableProperty]
    public partial bool CurrencyLocked { get; set; }

    [ObservableProperty]
    public partial bool HasTargetDate { get; set; }

    [ObservableProperty]
    public partial DateOnly TargetDate { get; set; }

    [ObservableProperty]
    public partial int ScheduleIndex { get; set; }

    /// <summary>Gets or sets the first contribution date, e.g. the next pay day.</summary>
    [ObservableProperty]
    public partial DateOnly FirstDate { get; set; }

    [ObservableProperty]
    public partial int MethodIndex { get; set; }

    [ObservableProperty]
    public partial string ContributionText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PercentText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CutCategory? CutCategory { get; set; }

    [ObservableProperty]
    public partial bool IsFixedMethod { get; set; } = true;

    [ObservableProperty]
    public partial bool IsShareMethod { get; set; }

    [ObservableProperty]
    public partial bool IsCutMethod { get; set; }

    [ObservableProperty]
    public partial bool ShowOnHome { get; set; }

    [ObservableProperty]
    public partial bool Protect { get; set; }

    [ObservableProperty]
    public partial int PriorityIndex { get; set; }

    [ObservableProperty]
    public partial string? IconKey { get; set; }

    [ObservableProperty]
    public partial string Note { get; set; }

    [ObservableProperty]
    public partial string? NameError { get; set; }

    [ObservableProperty]
    public partial string? AmountError { get; set; }

    [ObservableProperty]
    public partial string? SaveError { get; set; }

    /// <summary>Gets the preview lines: progress, the user's plan and the amount needed by the date (ZEX-GO14).</summary>
    [ObservableProperty]
    public partial string? PreviewProgress { get; set; }

    [ObservableProperty]
    public partial string? PreviewPlan { get; set; }

    [ObservableProperty]
    public partial string? PreviewRequired { get; set; }

    [ObservableProperty]
    public partial string? PreviewMethod { get; set; }

    /// <summary>Gets a value indicating whether "Apply to budget" is offered (a spending cut with a valid amount).</summary>
    [ObservableProperty]
    public partial bool CanApplyCut { get; set; }

    public async void ApplyQueryAttributes(IDictionary<string, object> query) => await Presentation.Failures.GuardAsync(() => ApplyQueryAsync(query));

    private async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        _loading = true;
        try
        {
            var settings = await _store.GetSettingsAsync();
            IsAdvanced = settings.Mode == Core.Settings.ExperienceMode.Advanced;
            CurrencyCode = settings.DefaultCurrencyCode;
            _accounts = await _store.GetAccountsAsync();
            _entries = await _store.GetEntriesAsync();
            Accounts.Clear();
            foreach (var account in _accounts.Where(a => !a.IsArchived && a.Type is AccountType.Cash or AccountType.Checking or AccountType.Savings))
            {
                Accounts.Add(new AccountChoice(account.Id, account.Name, account.CurrencyCode));
            }

            var categories = new CategoryLookup(await _store.GetCategoriesAsync(), _translator);
            CutCategories.Clear();
            foreach (var category in categories.All.Where(c => c.Kind == CategoryKind.Expense && c.ParentId is null && !c.IsArchived).OrderBy(c => c.SortOrder))
            {
                CutCategories.Add(new CutCategory(category.Id, categories.Name(category.Id)));
            }

            // New goals follow an account by default; Simple creates only those (ZEX-SA).
            TypeIndex = 0;
            Account = Accounts.FirstOrDefault(a => a.Id == settings.DefaultAccountId && a.CurrencyCode == settings.DefaultCurrencyCode)
                ?? Accounts.FirstOrDefault(a => _accounts.First(x => x.Id == a.Id).Type == AccountType.Savings)
                ?? Accounts.FirstOrDefault();

            if (query.TryGetValue("id", out var value) && value is Guid id && await _goals.GetGoalAsync(id) is { } goal)
            {
                await LoadGoalAsync(goal);
            }
        }
        finally
        {
            _loading = false;
        }

        ShowTypeChooser = CanChangeType && IsAdvanced;
        UpdateType();
        query.Clear();
    }

    private async Task LoadGoalAsync(Goal goal)
    {
        _existing = goal;
        Title = _translator["Goal_EditTitle"];
        TypeIndex = goal.Type == GoalType.AccountBalance ? 0 : 1;
        CanChangeType = false;
        Name = goal.Name;
        CurrencyCode = goal.CurrencyCode;
        Account = Accounts.FirstOrDefault(a => a.Id == goal.AccountId) ?? (goal.Type == GoalType.AccountBalance ? null : Account);
        AmountText = MoneyText.ForInput(goal.TargetAmount, goal.CurrencyCode, _localization.CurrentCulture);
        HasTargetDate = goal.TargetDate is not null;
        TargetDate = goal.TargetDate ?? Today.AddYears(1);
        PriorityIndex = (int)goal.Priority;
        IconKey = goal.Icon;
        Note = goal.Note ?? string.Empty;
        ShowOnHome = goal.HomePin is not null;
        Protect = goal.Protect;

        var allocations = await _goals.GetAllocationsAsync(goal.Id);
        _currentEarmarked = Math.Max(0, allocations.Sum(a => a.Amount));

        // Allocations are in the goal's currency; changing it would relabel money (ACC-07 applies the same way).
        CurrencyLocked = allocations.Count > 0;

        _existingPlan = (await _goals.GetContributionPlansAsync()).FirstOrDefault(p => p.GoalId == goal.Id);
        var rule = ContributionSchedule.RuleFor(goal, _existingPlan, Today);
        ScheduleIndex = Math.Max(0, Array.IndexOf(Schedules, (rule.Frequency, rule.Interval)));
        FirstDate = rule.Start;
        if (_existingPlan is { } plan)
        {
            MethodIndex = (int)plan.Method;
            ContributionText = plan.Amount is { } amount ? MoneyText.ForInput(amount, goal.CurrencyCode, _localization.CurrentCulture) : string.Empty;
            PercentText = plan.Percent is { } percent ? percent.ToString("0.##", _localization.CurrentCulture) : string.Empty;
            CutCategory = CutCategories.FirstOrDefault(c => plan.CategoryIds.Contains(c.Id));
        }
    }

    partial void OnTypeIndexChanged(int value) => UpdateType();

    partial void OnAccountChanged(AccountChoice? value)
    {
        if (IsBalanceGoal && value is not null)
        {
            CurrencyCode = value.CurrencyCode;
        }

        UpdatePreview();
    }

    partial void OnCurrencyCodeChanged(string value)
    {
        UnitNote = DisplayUnitNote.For(_translator, value);
        UpdatePreview();
    }

    partial void OnMethodIndexChanged(int value)
    {
        IsFixedMethod = value == (int)ContributionMethod.FixedAmount;
        IsShareMethod = value == (int)ContributionMethod.ShareOfIncome;
        IsCutMethod = value == (int)ContributionMethod.SpendingCut;
        UpdatePreview();
    }

    partial void OnAmountTextChanged(string value) => UpdatePreview();

    partial void OnHasTargetDateChanged(bool value) => UpdatePreview();

    partial void OnTargetDateChanged(DateOnly value) => UpdatePreview();

    partial void OnScheduleIndexChanged(int value) => UpdatePreview();

    partial void OnFirstDateChanged(DateOnly value) => UpdatePreview();

    partial void OnContributionTextChanged(string value) => UpdatePreview();

    partial void OnPercentTextChanged(string value) => UpdatePreview();

    partial void OnCutCategoryChanged(CutCategory? value) => UpdatePreview();

    private void UpdateType()
    {
        IsBalanceGoal = TypeIndex == 0;
        if (IsBalanceGoal && Account is not null)
        {
            CurrencyCode = Account.CurrencyCode;
        }

        UpdatePreview();
    }

    // The live preview (ZEX-GO14): nothing is saved; the numbers use the same service as the goal list and Home.
    private void UpdatePreview()
    {
        if (_loading)
        {
            return;
        }

        PreviewProgress = PreviewPlan = PreviewRequired = PreviewMethod = null;
        CanApplyCut = false;
        var culture = _localization.CurrentCulture;
        if (!Currencies.TryGet(CurrencyCode, out var currency) || !MoneyText.TryParse(AmountText, currency, culture, out var target) || target <= 0)
        {
            return;
        }

        var goal = BuildGoal(target);
        var current = IsBalanceGoal
            ? (Account is not null && _accounts.FirstOrDefault(a => a.Id == Account.Id) is { } account ? LedgerCalculator.Balance(account, _entries, Today) : 0)
            : _currentEarmarked;
        var plan = BuildPlan(culture, currency);
        var progress = GoalProgressService.Scenario(goal, current, 0, plan, Today);
        string Money(long amount) => MoneyText.Format(amount, CurrencyCode, culture);

        PreviewProgress = _translator.Format("Goal_PreviewNow", Money(Math.Max(0, current)), Math.Round(progress.Progress * 100).ToString("0", culture), Money(progress.Remaining));
        if (progress.Eta is { } eta && progress.PlannedContribution is { } contribution)
        {
            PreviewPlan = _translator.Format("Goal_PreviewPlan", Money(contribution), ScheduleNames[ScheduleIndex], ContributionSchedule.PeriodsNeeded(progress.Remaining, contribution), _dates.Format(eta, DateFormatStyle.Short));
        }

        if (progress.Required is { } required && goal.TargetDate is { } due)
        {
            PreviewRequired = progress.Opportunities == 0
                ? _translator.Format("Goal_PreviewNeededNow", Money(progress.Remaining))
                : _translator.Format("Goal_PreviewRequired", _dates.Format(due, DateFormatStyle.Short), Money(required), progress.Opportunities);
        }

        // Share of income: what the rule would suggest from the last closed financial month (ZEX-GO09).
        if (IsShareMethod && plan?.Percent is { } percent)
        {
            var (from, to) = LastClosedMonth();
            var income = EligibleIncome.Of(_accounts, _entries, CurrencyCode, from, to);
            PreviewMethod = _translator.Format("Goal_PreviewShare", Money(income.Total), income.Entries.Count, percent.ToString("0.##", culture), Money(EligibleIncome.Suggestion(income.Total, percent)));
        }

        // Spending cut: only a preview; the budget changes when the user taps "Apply to budget" (ZEX-GO10).
        if (IsCutMethod && plan is { Amount: > 0 } && CutCategory is not null)
        {
            PreviewMethod = _translator.Format("Goal_PreviewCut", CutCategory.Name, Money(plan.Amount.Value));
            CanApplyCut = true;
        }
    }

    private (DateOnly From, DateOnly To) LastClosedMonth()
    {
        var settings = _store.GetSettings();
        var calendar = _localization.CurrentCalendar == CalendarSystem.Persian ? PeriodCalendar.Persian : PeriodCalendar.Gregorian;
        var (currentYear, currentMonth) = PeriodMath.MonthOf(Today, calendar, settings.MonthStartDay);
        var (year, month) = PeriodMath.Previous(currentYear, currentMonth);
        return PeriodMath.MonthRange(year, month, calendar, settings.MonthStartDay);
    }

    private Goal BuildGoal(long target)
    {
        var goal = _existing is null ? new Goal { Name = Name.Trim(), CurrencyCode = CurrencyCode } : CloneOf(_existing);
        goal.Name = Name.Trim();
        goal.Type = IsBalanceGoal ? GoalType.AccountBalance : GoalType.Earmark;
        goal.AccountId = IsBalanceGoal ? Account?.Id : null;
        goal.TargetAmount = target;
        goal.CurrencyCode = CurrencyCode;
        goal.TargetDate = HasTargetDate ? TargetDate : null;
        var (frequency, interval) = Schedules[Math.Clamp(ScheduleIndex, 0, Schedules.Length - 1)];
        goal.Frequency = frequency == Frequency.Monthly ? ContributionFrequency.Monthly : ContributionFrequency.Weekly;
        goal.Priority = (GoalPriority)Math.Clamp(PriorityIndex, 0, 2);
        goal.Icon = IconKey;
        goal.Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
        goal.Protect = !IsBalanceGoal && Protect;
        return goal;
    }

    private static Goal CloneOf(Goal goal) => new()
    {
        Name = goal.Name,
        CurrencyCode = goal.CurrencyCode,
        State = goal.State,
        HomePin = goal.HomePin,
        PausedAt = goal.PausedAt,
        CompletedAt = goal.CompletedAt,
    };

    // The plan always carries the contribution dates; the amount, percentage or category depend on the method.
    private ContributionPlan? BuildPlan(System.Globalization.CultureInfo culture, Currency currency)
    {
        var (frequency, interval) = Schedules[Math.Clamp(ScheduleIndex, 0, Schedules.Length - 1)];
        var calendar = frequency == Frequency.Monthly && _localization.CurrentCalendar == CalendarSystem.Persian ? PeriodCalendar.Persian : PeriodCalendar.Gregorian;
        var method = IsAdvanced ? (ContributionMethod)Math.Clamp(MethodIndex, 0, 2) : ContributionMethod.FixedAmount;
        var plan = new ContributionPlan
        {
            Method = method,
            Rule = new RecurrenceRule { Frequency = frequency, Interval = interval, Start = FirstDate, Calendar = calendar },
        };

        if (method is ContributionMethod.FixedAmount or ContributionMethod.SpendingCut
            && MoneyText.TryParse(ContributionText, currency, culture, out var amount) && amount > 0)
        {
            plan.Amount = amount;
        }

        if (method == ContributionMethod.ShareOfIncome
            && decimal.TryParse(Vafadar.Core.Text.Digits.ToAscii(PercentText.Trim().TrimEnd('%')), System.Globalization.NumberStyles.Number, culture, out var percent)
            && percent is > 0 and <= 100)
        {
            plan.Percent = percent;
        }

        if (method == ContributionMethod.SpendingCut && CutCategory is not null)
        {
            plan.CategoryIds = [CutCategory.Id];
        }

        return plan;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        SaveError = null;
        NameError = string.IsNullOrWhiteSpace(Name) ? _translator["Goal_NameRequired"] : null;
        var culture = _localization.CurrentCulture;
        long amount = 0;
        AmountError = Currencies.TryGet(CurrencyCode, out var currency) && MoneyText.TryParse(AmountText, currency, culture, out amount) && amount > 0
            ? null
            : _translator["Amount_Invalid"];
        if (IsBalanceGoal && Account is null)
        {
            SaveError = _translator["Goal_ChooseAccount"];
        }

        if (NameError is not null || AmountError is not null || SaveError is not null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var built = BuildGoal(amount);
            var goal = _existing ?? built;
            if (_existing is not null)
            {
                goal.Name = built.Name;
                goal.Type = built.Type;
                goal.AccountId = built.AccountId;
                goal.TargetAmount = built.TargetAmount;
                goal.CurrencyCode = built.CurrencyCode;
                goal.TargetDate = built.TargetDate;
                goal.Frequency = built.Frequency;
                goal.Priority = built.Priority;
                goal.Icon = built.Icon;
                goal.Note = built.Note;
                goal.Protect = built.Protect;
            }

            // Pinned goals keep their place; a newly pinned goal goes after the others (ZEX-GO06).
            goal.HomePin = ShowOnHome ? goal.HomePin ?? ((await _goals.GetGoalsAsync()).Max(g => g.HomePin) ?? 0) + 1 : null;
            await _goals.SaveGoalAsync(goal);
            await _goals.SaveContributionPlanAsync(goal.Id, BuildPlan(culture, currency!));
            await Shell.Current.GoToAsync("..");
        }
        catch (InvalidOperationException)
        {
            SaveError = _translator["Goal_AccountHasGoal"];
        }
        finally
        {
            IsBusy = false;
        }
    }

    // A spending cut lowers the category limit of this financial month's budget, after a confirmation (ZEX-GO10).
    [RelayCommand]
    private async Task ApplyCutAsync()
    {
        var culture = _localization.CurrentCulture;
        if (CutCategory is null || !Currencies.TryGet(CurrencyCode, out var currency) || !MoneyText.TryParse(ContributionText, currency, culture, out var cut) || cut <= 0)
        {
            return;
        }

        var settings = await _store.GetSettingsAsync();
        var (year, month) = PeriodMath.MonthOf(Today, settings.BudgetCalendar, settings.MonthStartDay);
        var budget = await _store.GetBudgetAsync(year, month, settings.BudgetCalendar, CurrencyCode);
        var limit = budget?.CategoryLimits.FirstOrDefault(l => l.CategoryId == CutCategory.Id);
        if (budget is null || limit is null)
        {
            await Shell.Current.DisplayAlertAsync(_translator["Contribution_SpendingCut"], _translator.Format("Goal_CutNoLimit", CutCategory.Name), _translator["Common_Ok"]);
            return;
        }

        var newLimit = Math.Max(0, limit.Limit - cut);
        if (!await Shell.Current.DisplayAlertAsync(
                _translator["Goal_ApplyCut"],
                _translator.Format("Goal_ApplyCutMessage", CutCategory.Name, MoneyText.Format(limit.Limit, CurrencyCode, culture), MoneyText.Format(newLimit, CurrencyCode, culture)),
                _translator["Goal_ApplyCut"],
                _translator["Common_Cancel"]))
        {
            return;
        }

        limit.Limit = newLimit;
        await _store.SaveBudgetAsync(budget);
        CanApplyCut = false;
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
}
