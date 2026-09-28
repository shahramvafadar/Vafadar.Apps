using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>
/// Creates and edits plans (UI-08) with a live preview of the next six dates (REC-04). Changing a plan that already
/// has settled or skipped occurrences applies "from a date on" by splitting it, so history is never rewritten (REC-15).
/// </summary>
public sealed partial class PlanEditorViewModel : ViewModelBase, IQueryAttributable
{
    private static readonly EntryKind[] Kinds = [EntryKind.Expense, EntryKind.Income, EntryKind.Transfer];

    // Repeat presets: Once, Weekly, Every 2 weeks, Monthly, Yearly, Custom.
    private const int PresetCustom = 5;

    private readonly ZananceStore _store;
    private readonly PlanStore _plans;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly IDateFormatter _dates;
    private readonly TimeProvider _time;
    private readonly ReminderService _reminders;
    private readonly IReadOnlyList<string> _allPresets;
    private Schedule? _existing;
    private bool _hasHistory;
    private string _snapshot = string.Empty;
    private bool _loading = true;
    private CategoryLookup _categories;
    private Dictionary<Guid, Account> _accounts = [];

    public PlanEditorViewModel(ZananceStore store, PlanStore plans, Translator translator, ILocalizationService localization, IDateFormatter dates, TimeProvider time, ReminderService reminders)
    {
        _reminders = reminders;
        _store = store;
        _plans = plans;
        _translator = translator;
        _localization = localization;
        _dates = dates;
        _time = time;
        _categories = new CategoryLookup([], translator);

        KindNames = [.. Kinds.Select(k => translator[$"EntryKind_{k}"])];
        AmountModeNames = [translator["AmountMode_Fixed"], translator["AmountMode_Estimated"], translator["AmountMode_Unknown"]];
        _allPresets = [translator["Repeat_Once"], translator["Repeat_Weekly"], translator["Repeat_TwoWeeks"], translator["Repeat_Monthly"], translator["Repeat_Yearly"], translator["Repeat_Custom"]];
        PresetNames = _allPresets;
        UnitNames = [translator["Unit_Days"], translator["Unit_Weeks"], translator["Unit_Months"], translator["Unit_Years"]];
        CalendarNames = [translator["Calendar_Gregorian"], translator["Calendar_Persian"]];
        MissingDayNames = [translator["MissingDay_LastValid"], translator["MissingDay_Skip"]];
        WeekendNames = [translator["Weekend_Keep"], translator["Weekend_Before"], translator["Weekend_After"]];
        _weekend = Regions.WeekendDays(localization.CurrentRegion, System.Globalization.CultureInfo.GetCultureInfo(localization.CurrentLanguage.CultureName));
        WeekendHint = translator.Format("Weekend_Hint", string.Join(translator["Reminder_ListSeparator"], _weekend.Select(d => localization.CurrentCulture.DateTimeFormat.GetDayName(d))));
        EndNames = [translator["End_Never"], translator["End_OnDate"], translator["End_AfterCount"]];
        PastNames = [translator["Past_FromToday"], translator["Past_Include"]];
        DayRuleNames = [];

        Title = translator["Plan_NewTitle"];
        Name = string.Empty;
        AmountText = string.Empty;
        ToAmountText = string.Empty;
        IntervalText = "1";
        CountText = "12";
        Note = string.Empty;
        CurrencyCode = Currencies.Euro.Code;
        Accounts = [];
        Start = Today;
        ContractEnd = Today.AddYears(1);
        CancellationDeadline = Today.AddMonths(11);
        ReviewDate = Today.AddMonths(6);
        EndDate = Today.AddYears(1);
        ApplyFrom = Today;
        ReminderDaysText = "3";
        ReminderTime = new TimeSpan(9, 0, 0);
        PresetIndex = 3;
        CalendarIndex = localization.CurrentCalendar == CalendarSystem.Persian ? 1 : 0;
    }

    public IReadOnlyList<string> KindNames { get; }

    public IReadOnlyList<string> AmountModeNames { get; }

    [ObservableProperty]
    public partial IReadOnlyList<string> PresetNames { get; set; }

    [ObservableProperty]
    public partial bool IsAdvanced { get; set; }

    [ObservableProperty]
    public partial string? AdvancedSummary { get; set; }

    public IReadOnlyList<string> UnitNames { get; }

    public IReadOnlyList<string> CalendarNames { get; }

    public IReadOnlyList<string> MissingDayNames { get; }

    // Weekend rule (F2-CON-05): weekends only, from the region; no holiday or bank-day calendar is claimed.
    private readonly IReadOnlyList<DayOfWeek> _weekend;

    public IReadOnlyList<string> WeekendNames { get; }

    public string WeekendHint { get; }

    [ObservableProperty]
    public partial int WeekendIndex { get; set; }

    // A second day each month, e.g. the 1st and the 15th (Phase 2A, monthly plans in Advanced mode).
    [ObservableProperty]
    public partial bool HasSecondDay { get; set; }

    [ObservableProperty]
    public partial string SecondDayText { get; set; } = "15";

    partial void OnHasSecondDayChanged(bool value) => Update();

    partial void OnSecondDayTextChanged(string value) => Update();

    public bool ShowSecondDay => ShowMonthOptions && Frequency == Frequency.Monthly && !SelectedDayRule.IsWeekday();

    partial void OnWeekendIndexChanged(int value) => Update();

    public IReadOnlyList<string> EndNames { get; }

    public IReadOnlyList<string> PastNames { get; }

    public ObservableCollection<CategoryChoice> Categories { get; } = [];

    public ObservableCollection<string> Preview { get; } = [];

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial int KindIndex { get; set; }

    [ObservableProperty]
    public partial bool CanChangeKind { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string? NameError { get; set; }

    [ObservableProperty]
    public partial int AmountModeIndex { get; set; }

    [ObservableProperty]
    public partial string AmountText { get; set; }

    [ObservableProperty]
    public partial string CurrencyCode { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<AccountChoice> Accounts { get; set; }

    [ObservableProperty]
    public partial AccountChoice? Account { get; set; }

    [ObservableProperty]
    public partial AccountChoice? ToAccount { get; set; }

    [ObservableProperty]
    public partial string ToAmountText { get; set; }

    [ObservableProperty]
    public partial bool ShowToAmount { get; set; }

    [ObservableProperty]
    public partial int PresetIndex { get; set; }

    [ObservableProperty]
    public partial string IntervalText { get; set; }

    [ObservableProperty]
    public partial int UnitIndex { get; set; }

    [ObservableProperty]
    public partial DateOnly Start { get; set; }

    [ObservableProperty]
    public partial int CalendarIndex { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> DayRuleNames { get; set; }

    [ObservableProperty]
    public partial int DayRuleIndex { get; set; }

    // The day rules offered for the start date: a start in the fifth week has no "n-th weekday" apart from the last (REC-12).
    private List<MonthDayRule> _dayRules = DayRulesFor(1);

    private MonthDayRule SelectedDayRule => DayRuleIndex >= 0 && DayRuleIndex < _dayRules.Count ? _dayRules[DayRuleIndex] : MonthDayRule.SpecificDay;

    private static List<MonthDayRule> DayRulesFor(int anchorDay) => MonthDayRules.WeekOf(anchorDay) <= 4
        ? [MonthDayRule.SpecificDay, MonthDayRule.LastDayOfMonth, MonthDayRule.NthWeekday, MonthDayRule.LastWeekday]
        : [MonthDayRule.SpecificDay, MonthDayRule.LastDayOfMonth, MonthDayRule.LastWeekday];

    private int IndexOfDayRule(MonthDayRule rule) =>
        _dayRules.IndexOf(rule == MonthDayRule.NthWeekday && !_dayRules.Contains(rule) ? MonthDayRule.LastWeekday : rule) is var index and >= 0 ? index : 0;

    [ObservableProperty]
    public partial int MissingDayIndex { get; set; }

    [ObservableProperty]
    public partial int EndIndex { get; set; }

    [ObservableProperty]
    public partial DateOnly EndDate { get; set; }

    [ObservableProperty]
    public partial string CountText { get; set; }

    [ObservableProperty]
    public partial int PastIndex { get; set; }

    [ObservableProperty]
    public partial bool AutoPost { get; set; }

    [ObservableProperty]
    public partial bool ReminderEnabled { get; set; }

    [ObservableProperty]
    public partial string ReminderDaysText { get; set; }

    [ObservableProperty]
    public partial TimeSpan? ReminderTime { get; set; }

    // A second reminder on the due date (REM-01); editable in Advanced mode, kept as it is in Simple mode.
    [ObservableProperty]
    public partial bool ReminderOnDueDate { get; set; }

    [ObservableProperty]
    public partial string Note { get; set; }

    // Contract details (F2-CON-01), Advanced mode; kept as they are in Simple mode.
    [ObservableProperty]
    public partial bool ShowContract { get; set; }

    [ObservableProperty]
    public partial string ContractProvider { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ContractReference { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasContractEnd { get; set; }

    [ObservableProperty]
    public partial DateOnly ContractEnd { get; set; }

    [ObservableProperty]
    public partial bool ContractRenews { get; set; }

    [ObservableProperty]
    public partial bool HasCancellationDeadline { get; set; }

    [ObservableProperty]
    public partial DateOnly CancellationDeadline { get; set; }

    [ObservableProperty]
    public partial bool HasReviewDate { get; set; }

    [ObservableProperty]
    public partial DateOnly ReviewDate { get; set; }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleContract() => ShowContract = !ShowContract;

    [ObservableProperty]
    public partial DateOnly ApplyFrom { get; set; }

    [ObservableProperty]
    public partial bool ShowApplyFrom { get; set; }

    [ObservableProperty]
    public partial string? SaveError { get; set; }

    [ObservableProperty]
    public partial bool HasNoAccounts { get; set; }

    [ObservableProperty]
    public partial string? ReminderNote { get; set; }

    public bool IsTransfer => Kind == EntryKind.Transfer;

    public bool ShowAmount => AmountModeIndex != 2;

    public bool CanAutoPost => AmountModeIndex == 0;

    public bool ShowCustom => PresetIndex == PresetCustom;

    public bool ShowMonthOptions => IsAdvanced && Frequency is Frequency.Monthly or Frequency.Yearly;

    public bool ShowMissingDay => ShowMonthOptions && DayRuleIndex == 0 && AnchorDay > 28;

    public bool ShowEnd => IsAdvanced && Frequency != Frequency.Once;

    public bool ShowEndDate => EndIndex == 1;

    public bool ShowCount => EndIndex == 2;

    public bool ShowPastChoice => _existing is null && Start < Today;

    public bool IsDirty => Snapshot() != _snapshot;

    private EntryKind Kind => Kinds[Math.Clamp(KindIndex, 0, Kinds.Length - 1)];

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    private PeriodCalendar Calendar => CalendarIndex == 1 ? PeriodCalendar.Persian : PeriodCalendar.Gregorian;

    private int AnchorDay => Calendar == PeriodCalendar.Persian
        ? new PersianCalendar().GetDayOfMonth(Start.ToDateTime(TimeOnly.MinValue))
        : Start.Day;

    private Frequency Frequency => PresetIndex switch
    {
        0 => Frequency.Once,
        1 or 2 => Frequency.Weekly,
        3 => Frequency.Monthly,
        4 => Frequency.Yearly,
        _ => UnitIndex switch { 0 => Frequency.Daily, 1 => Frequency.Weekly, 2 => Frequency.Monthly, _ => Frequency.Yearly },
    };

    private int Interval => PresetIndex switch
    {
        2 => 2,
        PresetCustom => int.TryParse(Vafadar.Core.Text.Digits.ToAscii(IntervalText), NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : 0,
        _ => 1,
    };

    /// <summary>Query: <c>id</c> of an existing plan.</summary>
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        _loading = true;
        try
        {
            var accounts = await _store.GetAccountsAsync();
            _accounts = accounts.ToDictionary(a => a.Id);
            _categories = new CategoryLookup(await _store.GetCategoriesAsync(), _translator);
            Accounts = [.. accounts.Where(a => !a.IsArchived).Select(a => new AccountChoice(a.Id, a.Name, a.CurrencyCode))];
            HasNoAccounts = Accounts.Count == 0;
            var settings = await _store.GetSettingsAsync();
            IsAdvanced = settings.Mode == Core.Settings.ExperienceMode.Advanced;

            if (query.TryGetValue("id", out var value) && value is Guid id && await _plans.GetScheduleAsync(id) is { } schedule)
            {
                _existing = schedule;
                _hasHistory = await _plans.HasHistoryAsync(id);
                Title = _translator["Plan_EditTitle"];
                Load(schedule);
                var states = await _plans.GetStatesAsync(id);
                ApplyFrom = Occurrences.NextOpen(schedule, states, Today, Today)?.OriginalDate ?? Today;
            }
            else if (query.TryGetValue("fromEntry", out var entryValue) && entryValue is Guid entryId && await _store.GetEntryAsync(entryId) is { } entry)
            {
                // A plan from an existing entry (TX-04): same values, first due date one month later.
                CanChangeKind = true;
                KindIndex = Math.Max(0, Array.IndexOf(Kinds, entry.Kind));
                Name = entry.Title ?? _categories.Name(entry.CategoryId);
                Accounts = EnsureChoice(Accounts, entry.AccountId);
                Account = Accounts.FirstOrDefault(a => a.Id == entry.AccountId);
                ToAccount = entry.ToAccountId is { } to ? Accounts.FirstOrDefault(a => a.Id == to) : Accounts.FirstOrDefault(a => a.Id != entry.AccountId);
                AmountText = MoneyText.ForInput(entry.Amount, CurrencyOf(entry.AccountId), _localization.CurrentCulture);
                Start = entry.Date.AddMonths(1);
                Note = entry.Note ?? string.Empty;
                BuildCategories(entry.CategoryId);
            }
            else
            {
                CanChangeKind = true;
                Account = Accounts.FirstOrDefault(a => a.Id == settings.DefaultAccountId) ?? Accounts.FirstOrDefault();
                ToAccount = Accounts.FirstOrDefault(a => a.Id != Account?.Id);
                ReminderDaysText = settings.ReminderDaysBefore.ToString(CultureInfo.InvariantCulture);
                ReminderTime = settings.ReminderTime.ToTimeSpan();
                BuildCategories(null);
            }
        }
        finally
        {
            _loading = false;
        }

        ShowApplyFrom = _existing is not null && _hasHistory;

        // Simple offers the common repeats; a custom repeat that is already set stays visible (UX-02).
        PresetNames = IsAdvanced || PresetIndex == PresetCustom ? _allPresets : [.. _allPresets.Take(PresetCustom)];
        Update();
        _snapshot = Snapshot();
        query.Clear();
    }

    private void Load(Schedule schedule)
    {
        var culture = _localization.CurrentCulture;
        CanChangeKind = !_hasHistory;
        KindIndex = Math.Max(0, Array.IndexOf(Kinds, schedule.Kind));
        Name = schedule.Name;
        AmountModeIndex = (int)schedule.AmountMode;
        Accounts = EnsureChoice(Accounts, schedule.AccountId);
        Account = Accounts.FirstOrDefault(a => a.Id == schedule.AccountId);
        if (schedule.ToAccountId is { } to)
        {
            Accounts = EnsureChoice(Accounts, to);
            ToAccount = Accounts.FirstOrDefault(a => a.Id == to);
        }

        AmountText = schedule.Amount is { } amount ? MoneyText.ForInput(amount, CurrencyOf(schedule.AccountId), culture) : string.Empty;
        ToAmountText = schedule.ToAmount is { } toAmount && ToAccount is not null ? MoneyText.ForInput(toAmount, ToAccount.CurrencyCode, culture) : string.Empty;

        var rule = schedule.Rule;
        (PresetIndex, UnitIndex) = (rule.Frequency, rule.Interval) switch
        {
            (Frequency.Once, _) => (0, 0),
            (Frequency.Weekly, 1) => (1, 1),
            (Frequency.Weekly, 2) => (2, 1),
            (Frequency.Monthly, 1) => (3, 2),
            (Frequency.Yearly, 1) => (4, 3),
            (Frequency.Daily, _) => (PresetCustom, 0),
            (Frequency.Weekly, _) => (PresetCustom, 1),
            (Frequency.Monthly, _) => (PresetCustom, 2),
            _ => (PresetCustom, 3),
        };
        IntervalText = rule.Interval.ToString(CultureInfo.InvariantCulture);
        Start = rule.Start;
        CalendarIndex = rule.Calendar == PeriodCalendar.Persian ? 1 : 0;
        _dayRules = DayRulesFor(AnchorDay);
        DayRuleIndex = IndexOfDayRule(rule.DayRule);
        MissingDayIndex = (int)rule.MissingDay;
        WeekendIndex = (int)rule.WeekendShift;
        HasSecondDay = rule.SecondDay is not null;
        SecondDayText = (rule.SecondDay ?? 15).ToString(CultureInfo.InvariantCulture);
        EndIndex = (int)rule.End;
        EndDate = rule.EndDate ?? Today.AddYears(1);
        CountText = (rule.Count ?? 12).ToString(CultureInfo.InvariantCulture);
        AutoPost = schedule.AutoPost;
        ReminderEnabled = schedule.ReminderEnabled;
        ReminderDaysText = schedule.ReminderDaysBefore.ToString(CultureInfo.InvariantCulture);
        ReminderTime = schedule.ReminderTime.ToTimeSpan();
        ReminderOnDueDate = schedule.ReminderOnDueDate;
        Note = schedule.Note ?? string.Empty;
        ContractProvider = schedule.ContractProvider ?? string.Empty;
        ContractReference = schedule.ContractReference ?? string.Empty;
        HasContractEnd = schedule.ContractEnd is not null;
        ContractEnd = schedule.ContractEnd ?? Today.AddYears(1);
        ContractRenews = schedule.ContractRenews;
        HasCancellationDeadline = schedule.CancellationDeadline is not null;
        CancellationDeadline = schedule.CancellationDeadline ?? Today.AddMonths(11);
        HasReviewDate = schedule.ReviewDate is not null;
        ReviewDate = schedule.ReviewDate ?? Today.AddMonths(6);
        ShowContract = schedule.HasContract;
        BuildCategories(schedule.CategoryId);
    }

    private IReadOnlyList<AccountChoice> EnsureChoice(IReadOnlyList<AccountChoice> choices, Guid id) =>
        choices.Any(a => a.Id == id) || !_accounts.TryGetValue(id, out var account)
            ? choices
            : [.. choices, new AccountChoice(account.Id, account.Name, account.CurrencyCode)];

    private string CurrencyOf(Guid id) => _accounts.TryGetValue(id, out var account) ? account.CurrencyCode : Currencies.Euro.Code;

    partial void OnKindIndexChanged(int value)
    {
        if (!_loading)
        {
            BuildCategories(null);
            Update();
        }
    }

    partial void OnAmountModeIndexChanged(int value)
    {
        if (value != 0)
        {
            AutoPost = false;
        }

        Update();
    }

    partial void OnAccountChanged(AccountChoice? value) => Update();

    partial void OnToAccountChanged(AccountChoice? value) => Update();

    partial void OnPresetIndexChanged(int value) => Update();

    partial void OnIntervalTextChanged(string value) => Update();

    partial void OnUnitIndexChanged(int value) => Update();

    partial void OnStartChanged(DateOnly value) => Update();

    partial void OnCalendarIndexChanged(int value) => Update();

    partial void OnDayRuleIndexChanged(int value) => Update();

    partial void OnMissingDayIndexChanged(int value) => Update();

    partial void OnEndIndexChanged(int value) => Update();

    partial void OnEndDateChanged(DateOnly value) => Update();

    partial void OnCountTextChanged(string value) => Update();

    partial void OnPastIndexChanged(int value) => Update();

    // Permission is asked for only when the user turns a reminder on (REM-03); without it the plan still works.
    async partial void OnReminderEnabledChanged(bool value)
    {
        ReminderNote = null;
        if (!value || _loading)
        {
            return;
        }

        if (!_reminders.Scheduler.IsSupported)
        {
            ReminderNote = _translator["Reminder_NotOnThisDevice"];
        }
        else if (!await _reminders.EnsurePermissionAsync())
        {
            ReminderNote = _translator["Reminder_PermissionDenied"];
        }
    }

    // Refreshes every dependent visibility and the preview of the next dates.
    private void Update()
    {
        if (_loading)
        {
            return;
        }

        CurrencyCode = Account?.CurrencyCode ?? Currencies.Euro.Code;
        ShowToAmount = IsTransfer && Account is not null && ToAccount is not null && Account.CurrencyCode != ToAccount.CurrencyCode;
        var selected = SelectedDayRule;
        _dayRules = DayRulesFor(AnchorDay);
        var weekday = _localization.CurrentCulture.DateTimeFormat.GetDayName(Start.DayOfWeek);
        DayRuleNames = [.. _dayRules.Select(r => r switch
        {
            MonthDayRule.LastDayOfMonth => _translator["DayRule_LastDay"],
            MonthDayRule.NthWeekday => _translator.Format("DayRule_NthWeekday", _translator[$"Ordinal_{MonthDayRules.WeekOf(AnchorDay)}"], weekday),
            MonthDayRule.LastWeekday => _translator.Format("DayRule_LastWeekday", weekday),
            _ => _translator.Format("DayRule_OnDay", AnchorDay),
        })];
        if (IndexOfDayRule(selected) is var dayIndex && dayIndex != DayRuleIndex)
        {
            DayRuleIndex = dayIndex;
        }

        OnPropertyChanged(nameof(IsTransfer));
        OnPropertyChanged(nameof(ShowAmount));
        OnPropertyChanged(nameof(CanAutoPost));
        OnPropertyChanged(nameof(ShowCustom));
        OnPropertyChanged(nameof(ShowMonthOptions));
        OnPropertyChanged(nameof(ShowSecondDay));
        OnPropertyChanged(nameof(ShowMissingDay));
        OnPropertyChanged(nameof(ShowEnd));
        OnPropertyChanged(nameof(ShowEndDate));
        OnPropertyChanged(nameof(ShowCount));
        OnPropertyChanged(nameof(ShowPastChoice));

        // Hidden settings that change the dates are summarised in Simple mode (UX-02).
        var current = BuildRule();
        AdvancedSummary = !IsAdvanced && (current.End != EndKind.Never || current.DayRule != MonthDayRule.SpecificDay || current.MissingDay != MissingDayPolicy.LastValidDay)
            ? _translator.Format("Plan_AdvancedSummary", new PlanText(_translator, _dates, _localization.CurrentCulture).Rule(current))
            : null;

        Preview.Clear();
        var rule = current;
        if (Recurrence.Validate(rule) is not null)
        {
            Preview.Add(_translator["Plan_RuleInvalid"]);
            return;
        }

        // Existing plans preview from today; new plans from the start (or today when past dates are left out).
        var from = _existing is not null || (ShowPastChoice && PastIndex == 0) ? Today : rule.Start;
        foreach (var date in Recurrence.Next(rule, from, 6))
        {
            Preview.Add(_dates.Format(rule.ApplyWeekend(date.Date), DateFormatStyle.Long));
        }

        if (Preview.Count == 0)
        {
            Preview.Add(_translator["Plan_NoDates"]);
        }
    }

    private RecurrenceRule BuildRule() => new()
    {
        Frequency = Frequency,
        Interval = Interval,
        Start = Start,
        Calendar = Calendar,
        DayRule = SelectedDayRule,
        MissingDay = (MissingDayPolicy)MissingDayIndex,
        WeekendShift = (WeekendShift)Math.Clamp(WeekendIndex, 0, 2),
        SecondDay = HasSecondDay && Frequency == Frequency.Monthly && !SelectedDayRule.IsWeekday() && int.TryParse(Vafadar.Core.Text.Digits.ToAscii(SecondDayText), NumberStyles.None, CultureInfo.InvariantCulture, out var second) ? second : null,
        WeekendDays = WeekendIndex == 0 ? 0 : RecurrenceRule.MaskOf(_weekend),
        End = Frequency == Frequency.Once ? EndKind.Never : (EndKind)EndIndex,
        EndDate = EndIndex == 1 ? EndDate : null,
        Count = EndIndex == 2 && int.TryParse(Vafadar.Core.Text.Digits.ToAscii(CountText), NumberStyles.None, CultureInfo.InvariantCulture, out var count) ? count : null,
    };

    private void BuildCategories(Guid? selectedId)
    {
        Categories.Clear();
        if (IsTransfer)
        {
            return;
        }

        var kind = Kind == EntryKind.Income ? CategoryKind.Income : CategoryKind.Expense;
        foreach (var category in _categories.All
                     .Where(c => c.Kind == kind && (!c.IsArchived || c.Id == selectedId))
                     .OrderBy(c => c.SystemKey == DefaultCategories.Uncategorized)
                     .ThenBy(c => c.SortOrder))
        {
            Categories.Add(new CategoryChoice(category.Id, CategoryLookup.NameOf(category, _translator), Icons.Parse(category.Icon, Symbol.Tag), CategoryLookup.ParseColor(category.Color))
            {
                IsSelected = category.Id == selectedId,
            });
        }
    }

    [RelayCommand]
    private void SelectCategory(CategoryChoice choice)
    {
        foreach (var category in Categories)
        {
            category.IsSelected = ReferenceEquals(category, choice) && !category.IsSelected;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        SaveError = null;
        NameError = string.IsNullOrWhiteSpace(Name) ? _translator["Plan_NameRequired"] : null;
        if (NameError is not null || Account is null)
        {
            SaveError ??= Account is null ? _translator["Entry_NoAccounts"] : null;
            return;
        }

        var culture = _localization.CurrentCulture;
        long? amount = null;
        if (ShowAmount)
        {
            if (!MoneyText.TryParse(AmountText, Currencies.Get(Account.CurrencyCode), culture, out var parsed) || parsed <= 0)
            {
                SaveError = _translator["LedgerError_AmountMustBePositive"];
                return;
            }

            amount = parsed;
        }

        long? toAmount = null;
        if (IsTransfer)
        {
            if (ToAccount is null || ToAccount.Id == Account.Id)
            {
                SaveError = _translator[ToAccount is null ? "LedgerError_DestinationRequired" : "LedgerError_SameAccountTransfer"];
                return;
            }

            if (ShowToAmount)
            {
                if (!MoneyText.TryParse(ToAmountText, Currencies.Get(ToAccount.CurrencyCode), culture, out var parsedTo) || parsedTo <= 0)
                {
                    SaveError = _translator["LedgerError_DestinationAmountRequired"];
                    return;
                }

                toAmount = parsedTo;
            }
        }

        var rule = BuildRule();
        if (Recurrence.Validate(rule) is { } problem)
        {
            SaveError = _translator[$"Plan_{problem}"];
            return;
        }

        IsBusy = true;
        try
        {
            var target = _existing is not null && _hasHistory ? PlanActions.SplitFrom(_existing, ApplyFrom) : _existing ?? new Schedule { Name = Name.Trim() };
            target.Name = Name.Trim();
            target.Kind = Kind;
            target.AccountId = Account.Id;
            target.ToAccountId = IsTransfer ? ToAccount?.Id : null;
            target.ToAmount = toAmount;
            target.CategoryId = IsTransfer ? null : Categories.FirstOrDefault(c => c.IsSelected)?.Id ?? _categories.Uncategorized(Kind == EntryKind.Income ? CategoryKind.Income : CategoryKind.Expense);
            target.AmountMode = (AmountMode)AmountModeIndex;
            target.Amount = amount;
            target.Rule = rule;
            target.Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
            target.ContractProvider = string.IsNullOrWhiteSpace(ContractProvider) ? null : ContractProvider.Trim();
            target.ContractReference = string.IsNullOrWhiteSpace(ContractReference) ? null : ContractReference.Trim();
            target.ContractEnd = HasContractEnd ? ContractEnd : null;
            target.ContractRenews = HasContractEnd && ContractRenews;
            target.CancellationDeadline = HasCancellationDeadline ? CancellationDeadline : null;
            target.ReviewDate = HasReviewDate ? ReviewDate : null;
            target.ReminderEnabled = ReminderEnabled;
            target.ReminderDaysBefore = int.TryParse(Vafadar.Core.Text.Digits.ToAscii(ReminderDaysText), NumberStyles.None, CultureInfo.InvariantCulture, out var days) ? Math.Clamp(days, 0, 60) : 3;
            target.ReminderTime = TimeOnly.FromTimeSpan(ReminderTime ?? new TimeSpan(9, 0, 0));
            target.ReminderOnDueDate = ReminderOnDueDate;

            if (_existing is null && ShowPastChoice && PastIndex == 0)
            {
                // Past start: occurrences before today are not part of the plan (REC-07).
                target.ActiveFrom = Today;
            }

            // Enabling automatic posting never records occurrences from before that moment (REC-07).
            var autoPost = AutoPost && CanAutoPost;
            if (autoPost && (!target.AutoPost || target.AutoPostFrom is null))
            {
                target.AutoPostFrom = Today;
            }

            target.AutoPost = autoPost;

            if (_existing is not null && _hasHistory)
            {
                if (!await _plans.SaveSplitAsync(_existing, target))
                {
                    // Recorded occurrences after the change date do not fit the new dates (e.g. another day of the month).
                    SaveError = _translator["Plan_SplitConflict"];
                    return;
                }
            }
            else
            {
                await _plans.SaveScheduleAsync(target);
            }

            _snapshot = Snapshot();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception)
        {
            SaveError = _translator["Common_SaveFailed"];
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (!IsDirty || await ConfirmDiscardAsync())
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    private Task AddAccountAsync() => Shell.Current.GoToAsync($"../{AppShell.AccountEditorRoute}");

    public Task<bool> ConfirmDiscardAsync() => Shell.Current.DisplayAlertAsync(
        _translator["Common_DiscardTitle"], _translator["Common_DiscardMessage"], _translator["Common_Discard"], _translator["Common_KeepEditing"]);

    private string Snapshot() => string.Join('|',
        KindIndex, Name, AmountModeIndex, AmountText, Account?.Id, ToAccount?.Id, ToAmountText, PresetIndex, IntervalText, UnitIndex, Start,
        CalendarIndex, DayRuleIndex, MissingDayIndex, WeekendIndex, HasSecondDay, SecondDayText, EndIndex, EndDate, CountText, PastIndex, AutoPost, ReminderEnabled, ReminderDaysText,
        ReminderTime, ReminderOnDueDate, Note, ContractProvider, ContractReference, HasContractEnd, ContractEnd, ContractRenews,
        HasCancellationDeadline, CancellationDeadline, HasReviewDate, ReviewDate, Categories.FirstOrDefault(c => c.IsSelected)?.Id);
}
