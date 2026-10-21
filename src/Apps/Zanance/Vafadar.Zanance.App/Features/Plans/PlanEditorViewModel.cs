using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Data.Commerce;
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
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>
/// Creates and edits plans (UI-08) with a live preview of the next six dates (REC-04). Changing a plan that already
/// has settled or skipped occurrences applies "from a date on" by splitting it, so history is never rewritten (REC-15).
/// </summary>
public sealed partial class PlanEditorViewModel : ViewModelBase, Presentation.IThemeAware
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
    private readonly IAppInteraction _interaction;
    private bool _committed;
    private EditorWorkReview? _work;
    private bool _reviewingWork;
    private bool _workFailed;
    private bool _workRetired;
    private bool _ready;
    private bool _loading = true;
    private CategoryLookup _categories;
    private Dictionary<Guid, Account> _accounts = [];

    public PlanEditorViewModel(ZananceStore store, PlanStore plans, Translator translator, ILocalizationService localization, IDateFormatter dates, TimeProvider time, ReminderService reminders, IAppInteraction interaction)
    {
        _interaction = interaction;
        ResetFormCommands();
        PropertyChanged += (_, e) =>
        {
            _publicationGuard?.Invoke();
            if (e.PropertyName is nameof(IsBusy) or nameof(ContractProvider) or nameof(ContractReference) or nameof(HasContractEnd)
                or nameof(ContractEnd) or nameof(ContractRenews) or nameof(HasCancellationDeadline) or nameof(CancellationDeadline)
                or nameof(HasReviewDate) or nameof(ReviewDate) or nameof(AutoPost)) RefreshAccess();
        };
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
        CalendarNames = Presentation.Calendars.Names(translator);
        MissingDayNames = [translator["MissingDay_LastValid"], translator["MissingDay_Skip"]];
        WeekendNames = [translator["Weekend_Keep"], translator["Weekend_Before"], translator["Weekend_After"]];
        _weekend = Regions.WeekendDays(localization.CurrentRegion, System.Globalization.CultureInfo.GetCultureInfo(localization.CurrentLanguage.CultureName));
        _holidayRegion = PublicHolidays.IsSupported(localization.CurrentRegion) ? localization.CurrentRegion!.ToUpperInvariant() : null;
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
        // A blank plan is one-off; choosing Monthly anchors it to the first due date (D-65).
        PresetIndex = 0;
        CalendarIndex = (int)Presentation.Calendars.ToPeriod(localization.CurrentCalendar);
    }

    /// <summary>Gets whether the original loaded, not-yet-committed draft can accept user input.</summary>
    public bool CanInteract => _ready && FormVisible && !IsBusy && !_committed && _saveSession?.Accepted != true && _entryOrigin?.IsOwned != false;
    /// <summary>Gets whether an explicit Save may run; committed split continuations cannot be resubmitted.</summary>
    public bool CanSave => CanInteract && !HasNoAccounts && !_reviewingWork && !_workFailed && !_workRetired && _work is not null && WorkRestriction is null;
    /// <summary>Gets whether a pending writer must finish before the form leaves.</summary>
    public bool CanLeave => !IsBusy;
    /// <summary>Gets the actual Close/Cancel caption after a successful write.</summary>
    public string CloseText => _translator[_committed ? "Common_Close" : "Common_Cancel"];
    private Core.Commerce.WorkRestriction? WorkRestriction => _work?.PlanRestriction(BuildWriteCandidate(BuildRule()).Target, _hasHistory ? _existing?.Id : null);
    /// <summary>Gets a captured reason before Save; no financial field validation or write occurs here.</summary>
    public string? WorkText => _workFailed ? _translator["Editor_WorkReadFailed"] : _workRetired ? _translator["Editor_WorkRetired"]
        : WorkRestriction is { } restriction ? Presentation.CommercialFailureMessage.Describe(restriction, _translator) : null;
    /// <summary>Gets whether the original availability can be explicitly refreshed without changing any draft.</summary>
    public bool CanReviewWork => CanInteract && !_reviewingWork;
    private async Task ReadWorkAsync()
    {
        if (!CanReviewWork || _loadSession is not { HasPublished: true } owner) return;
        var epoch = _formEpoch;
        bool Current() => epoch == _formEpoch && !_formDeparted && ReferenceEquals(owner, _loadSession) && _entryOrigin?.IsOwned != false;
        _reviewingWork = true; ResetFormCommands(); RefreshAccess();
        try
        {
            if (_saveSession is { } previous) await previous.WaitForIdleAsync();
            var next = await owner.ReadAsync(reviewAccess: true);
            if (!Current()) return;
            next.EnsureCurrent(); owner.Publish(next);
            _saveSession = new(_plans, next); _work = next.Inputs.Work; _workFailed = _workRetired = false;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (!Current()) return;
            if (error is PlanEditorSourceChangedException or EntryEditorSourceChangedException)
            { _originalSourceChanged = true; Loading = new(); await FailOriginalReadAsync(Loading, error); }
            else { _work = null; _workFailed = true; }
        }
        finally { if (Current()) { _reviewingWork = false; ResetFormCommands(); PublishFormAvailability(); } }
    }
    private Task ReviewWorkAsync() => ReadWorkAsync();

    private void RefreshAccess()
    {
        OnPropertyChanged(nameof(WorkText)); OnPropertyChanged(nameof(CanReviewWork)); ReviewWorkCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanLeave)); OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanSave)); OnPropertyChanged(nameof(CloseText));
        SaveCommand.NotifyCanExecuteChanged(); CancelCommand.NotifyCanExecuteChanged(); AddAccountCommand.NotifyCanExecuteChanged();
        ToggleRuleOptionsCommand.NotifyCanExecuteChanged(); ToggleContractCommand.NotifyCanExecuteChanged(); SelectCategoryCommand.NotifyCanExecuteChanged();
    }

    public IReadOnlyList<string> KindNames { get; }

    public IReadOnlyList<string> AmountModeNames { get; }

    [ObservableProperty]
    public partial IReadOnlyList<string> PresetNames { get; set; }

    [ObservableProperty]
    public partial bool IsAdvanced { get; set; }

    /// <summary>Gets or sets whether the less common date rules are expanded.</summary>
    [ObservableProperty]
    public partial bool ShowRuleOptions { get; set; }

    /// <summary>Gets or sets the schedule sentence beside the live preview.</summary>
    [ObservableProperty]
    public partial string RuleSummary { get; set; } = string.Empty;

    /// <summary>Gets or sets the short-month explanation when a monthly anchor may be missing.</summary>
    [ObservableProperty]
    public partial string? ShortMonthHint { get; set; }

    /// <summary>Gets or sets a date-rule error beside the affected schedule controls.</summary>
    [ObservableProperty]
    public partial string? RuleError { get; set; }

    /// <summary>Gets or sets a principal-only explanation for debt transfers.</summary>
    [ObservableProperty]
    public partial bool IsDebtTransfer { get; set; }

    /// <summary>Gets or sets the category shown by the compact picker.</summary>
    [ObservableProperty]
    public partial PlanCategoryChoice? SelectedCategory { get; set; }

    /// <summary>Gets the recurrence calendar used by this form's date fields.</summary>
    public CalendarSystem RuleCalendar => Calendars.ToDisplay(Calendar);

    /// <summary>Gets the named calendar beside the first due date, including for one-off plans.</summary>
    public string RuleCalendarName => _translator[Calendars.NameKey(Calendar)];

    private void ToggleRuleOptions() => ShowRuleOptions = !ShowRuleOptions;

    public IReadOnlyList<string> UnitNames { get; }

    public IReadOnlyList<string> CalendarNames { get; }

    public IReadOnlyList<string> MissingDayNames { get; }

    // Weekend rule (F2-CON-05): weekends only, from the region; no holiday or bank-day calendar is claimed.
    private readonly IReadOnlyList<DayOfWeek> _weekend;
    private readonly string? _holidayRegion;

    public IReadOnlyList<string> WeekendNames { get; }

    public string WeekendHint => _translator.Format("Weekend_Hint", string.Join(_translator["Reminder_ListSeparator"],
        Enum.GetValues<DayOfWeek>().Where(day => (HolidayCalendarText.WeekendMaskForDraft(_existing?.Rule, _weekend) & (1 << (int)day)) != 0)
            .Select(day => _localization.CurrentCulture.DateTimeFormat.GetDayName(day))));

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

    partial void OnWeekendIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CanSkipHolidays));
        Update();
    }

    // Public holidays (F2-CON-05, D-29): only for a region with a holiday calendar, and only with a weekend rule.
    [ObservableProperty]
    public partial bool SkipHolidays { get; set; }

    partial void OnSkipHolidaysChanged(bool value) => Update();

    /// <summary>Gets a value indicating whether holidays can be skipped: the region has a calendar and a weekend rule is chosen.</summary>
    public bool CanSkipHolidays => EffectiveHolidayRegion is not null && WeekendIndex != 0;

    /// <summary>Gets the explanation of the holiday calendar, or <see langword="null"/> without a supported region.</summary>
    public string? HolidayHint => EffectiveHolidayRegion is { } region
        ? _translator.Format("Holiday_Hint", Regions.DisplayName(region)) + " " + HolidayCalendarText.Description(region, Start.Year, _translator) : null;

    // D-273: display preferences cannot silently replace the calendar captured by an existing rule.
    private string? EffectiveHolidayRegion => HolidayCalendarText.RegionForDraft(_existing?.Rule, _holidayRegion);

    public IReadOnlyList<string> EndNames { get; }

    public IReadOnlyList<string> PastNames { get; }

    public ObservableCollection<PlanCategoryChoice> Categories { get; } = [];

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
    public partial string? AmountError { get; set; }

    /// <summary>Gets or sets the source account problem beside its picker.</summary>
    [ObservableProperty]
    public partial string? AccountError { get; set; }

    /// <summary>Gets or sets the destination account problem beside its picker.</summary>
    [ObservableProperty]
    public partial string? ToAccountError { get; set; }

    /// <summary>Gets or sets the cross-currency destination amount problem beside its input.</summary>
    [ObservableProperty]
    public partial string? ToAmountError { get; set; }

    /// <summary>Requests that the visible form reveal its first invalid field after publishing all problems.</summary>
    public event EventHandler? ValidationFailed;

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

    // The second reminder and the contract are created in Advanced; once they exist, Simple shows them too, so they can
    // be seen and corrected (ZEX-S0502, SA14, SA15).
    [ObservableProperty]
    public partial bool ShowReminderOnDueDate { get; set; }

    [ObservableProperty]
    public partial bool ShowContractCard { get; set; }

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

    public bool ShowMonthOptions => Frequency is Frequency.Monthly or Frequency.Yearly;

    public bool ShowMissingDay => ShowMonthOptions && DayRuleIndex == 0 && AnchorDay > 28;

    public bool ShowEnd => Frequency != Frequency.Once;

    public bool ShowEndDate => EndIndex == 1;

    public bool ShowCount => EndIndex == 2;

    public bool ShowPastChoice => _existing is null && Start < Today;

    public bool IsDirty => _hasPublishedForm && Snapshot() != _snapshot;

    private EntryKind Kind => Kinds[Math.Clamp(KindIndex, 0, Kinds.Length - 1)];

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    private PeriodCalendar Calendar => Presentation.Calendars.All[Math.Clamp(CalendarIndex, 0, Presentation.Calendars.All.Count - 1)];

    private int AnchorDay => PeriodMath.DayOf(Start, Calendar);

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

    /// <summary>Loads one complete original plan/debt/entry route without a write or native transition.</summary>
    public Task ApplyQueryAsync(IDictionary<string, object> query) => ApplyOriginalQueryAsync(query);

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
        CalendarIndex = (int)rule.Calendar;
        _dayRules = DayRulesFor(AnchorDay);
        DayRuleIndex = IndexOfDayRule(rule.DayRule);
        MissingDayIndex = (int)rule.MissingDay;
        WeekendIndex = (int)rule.WeekendShift;
        SkipHolidays = rule.HolidayRegion is not null;
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
        ShowReminderOnDueDate |= schedule.ReminderOnDueDate;
        ShowContractCard |= schedule.HasContract;
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

    partial void OnPresetIndexChanged(int oldValue, int newValue)
    {
        // Moving from Monthly to Custom must start with "every 1 month", rather than quietly becoming daily.
        if (!_loading && newValue == PresetCustom && oldValue < PresetCustom)
        {
            UnitIndex = oldValue switch { 1 or 2 => 1, 3 => 2, 4 => 3, _ => 0 };
            IntervalText = oldValue == 2 ? "2" : "1";
        }

        Update();
    }

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

    partial void OnSelectedCategoryChanged(PlanCategoryChoice? value)
    {
        foreach (var category in Categories)
        {
            category.IsSelected = ReferenceEquals(category, value);
        }
    }

    // An explicit reminder toggle owns its native permission response; old queries/departed forms cannot receive it.
    private long _reminderRevision;
    async partial void OnReminderEnabledChanged(bool value)
    {
        var revision = ++_reminderRevision;
        if (_loading || !_hasPublishedForm || _formDeparted) return;
        ReminderNote = null;
        if (value) await GuardOriginalReminderAsync(revision);
    }
    private async Task GuardOriginalReminderAsync(long revision)
    {
        var epoch = _formEpoch; var owner = _loadSession;
        try { await CheckOriginalReminderAsync(revision); }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (epoch == _formEpoch && !_formDeparted && ReferenceEquals(owner, _loadSession) && revision == _reminderRevision)
                await _interaction.ShowFailureAsync(error);
        }
    }
    private async Task CheckOriginalReminderAsync(long revision)
    {
        var epoch = _formEpoch; var owner = _loadSession;
        bool Current() => epoch == _formEpoch && !_formDeparted && ReferenceEquals(owner, _loadSession)
            && revision == _reminderRevision && ReminderEnabled && _entryOrigin?.IsOwned != false;
        if (owner is null || !Current()) return;
        await owner.ValidateAsync(); if (!Current()) return;
        if (!_reminders.Scheduler.IsSupported) ReminderNote = _translator["Reminder_NotOnThisDevice"];
        else
        {
            var allowed = await _reminders.EnsurePermissionAsync();
            if (Current() && !allowed) ReminderNote = _translator["Reminder_PermissionDenied"];
        }
    }

    // Refreshes every dependent visibility and the preview of the next dates.
    private void Update()
    {
        OnPropertyChanged(nameof(HolidayHint));
        OnPropertyChanged(nameof(WeekendHint));
        OnPropertyChanged(nameof(CanSkipHolidays));
        if (_loading)
        {
            return;
        }

        CurrencyCode = Account?.CurrencyCode ?? Currencies.Euro.Code;
        ShowToAmount = ShowAmount && IsTransfer && Account is not null && ToAccount is not null && Account.CurrencyCode != ToAccount.CurrencyCode;
        var selected = SelectedDayRule;
        _dayRules = DayRulesFor(AnchorDay);
        var culture = _localization.CurrentCulture;
        var weekday = culture.DateTimeFormat.GetDayName(Start.DayOfWeek);
        // The ordinal and "last" agree with the weekday's gender (Italian "Ultima domenica", D-57).
        DayRuleNames = [.. _dayRules.Select(r => r switch
        {
            MonthDayRule.LastDayOfMonth => _translator["DayRule_LastDay"],
            MonthDayRule.NthWeekday => _translator.Format("DayRule_NthWeekday", _translator[WeekdayGrammar.OrdinalKey(MonthDayRules.WeekOf(AnchorDay), culture, Start.DayOfWeek)], weekday),
            MonthDayRule.LastWeekday => _translator.Format(WeekdayGrammar.TemplateKey("DayRule_LastWeekday", culture, Start.DayOfWeek), weekday),
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
        OnPropertyChanged(nameof(RuleCalendar));
        OnPropertyChanged(nameof(RuleCalendarName));
        IsDebtTransfer = IsTransfer && ((_accounts.GetValueOrDefault(Account?.Id ?? Guid.Empty)?.Type.IsDebt() ?? false)
            || (_accounts.GetValueOrDefault(ToAccount?.Id ?? Guid.Empty)?.Type.IsDebt() ?? false));

        var rule = BuildRule();
        RuleError = Recurrence.Validate(rule) is { } problem ? _translator[$"Plan_{problem}"] : null;
        var text = new PlanText(_translator, _dates, _localization);
        RefreshAccess();
        RuleSummary = text.EditorSummary(rule);
        ShortMonthHint = text.ShortMonthHint(rule);
        Preview.Clear();
        // Existing plans preview from today; new plans from the first due date unless past dates are excluded.
        var from = _existing is not null || (ShowPastChoice && PastIndex == 0) ? Today : rule.Start;
        foreach (var date in text.UpcomingDates(rule, from))
        {
            Preview.Add(date);
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
        WeekendDays = WeekendIndex == 0 ? 0 : HolidayCalendarText.WeekendMaskForDraft(_existing?.Rule, _weekend),

        // Public holidays move a date like the weekend (F2-CON-05). The switch was read but never saved, so the option was
        // lost on every save; a plan keeps its holiday region when the device's region has no calendar.
        HolidayRegion = SkipHolidays && WeekendIndex != 0 ? EffectiveHolidayRegion : null,
        End = Frequency == Frequency.Once ? EndKind.Never : (EndKind)EndIndex,
        EndDate = EndIndex == 1 ? EndDate : null,
        Count = EndIndex == 2 && int.TryParse(Vafadar.Core.Text.Digits.ToAscii(CountText), NumberStyles.None, CultureInfo.InvariantCulture, out var count) ? count : null,
    };

    // Refresh category choices with their selection; theme changes must not replace the typed plan draft.
    Task Presentation.IThemeAware.RefreshThemeAsync()
    {
        BuildCategories(Categories.FirstOrDefault(c => c.IsSelected)?.Id);
        return Task.CompletedTask;
    }

    private void BuildCategories(Guid? selectedId)
    {
        SelectedCategory = null;
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
            Categories.Add(new PlanCategoryChoice(category.Id, CategoryLookup.NameOf(category, _translator))
            {
                IsSelected = category.Id == selectedId,
            });
        }

        SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedId);
    }

    private void SelectCategory(PlanCategoryChoice choice)
    {
        foreach (var category in Categories)
        {
            category.IsSelected = ReferenceEquals(category, choice) && !category.IsSelected;
        }
    }

    /// <summary>Builds the same detached original candidate for Save and readiness, without parsing or mutating input.</summary>
    private (Schedule? Previous, Schedule Target) BuildWriteCandidate(RecurrenceRule rule)
    {
        var previous = _existing?.CopyForEditing();
        Schedule target;
        if (previous is not null && _hasHistory)
        {
            var next = PlanActions.SplitFrom(previous, ApplyFrom);
            // Retain one distinct continuation identity across uncommitted retries without changing entity identity rules.
            _splitDraft ??= next.CopyForEditing(); target = _splitDraft.CopyForEditing(); target.ActiveFrom = next.ActiveFrom;
        }
        else target = previous ?? _draftIdentity.CopyForEditing();
        target.Name = Name.Trim();
        target.Kind = Kind;
        target.AccountId = Account?.Id ?? Guid.Empty;
        target.ToAccountId = IsTransfer ? ToAccount?.Id : null;

        target.CategoryId = IsTransfer ? null : Categories.FirstOrDefault(c => c.IsSelected)?.Id ?? _categories.Uncategorized(Kind == EntryKind.Income ? CategoryKind.Income : CategoryKind.Expense);
        target.AmountMode = (AmountMode)AmountModeIndex;

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

        return (previous, target);
    }

    private async Task SaveAsync()
    {
        if (!CanSave) return;

        SaveError = null;
        var culture = _localization.CurrentCulture;

        // D-99: a missing name/amount must not postpone destination feedback until a second Save attempt.
        var rule = BuildRule();
        var validation = PlanDraftValidation.Validate(new PlanValidationInput
        {
            Name = Name, AmountText = AmountText, AmountRequired = ShowAmount,
            AccountId = Account?.Id, CurrencyCode = CurrencyCode,
            IsTransfer = IsTransfer, DestinationId = ToAccount?.Id,
            DestinationCurrencyCode = ToAccount?.CurrencyCode ?? CurrencyCode,
            DestinationAmountText = ToAmountText, DestinationAmountRequired = ShowToAmount, Rule = rule,
        }, culture);
        string? Text(string? key) => key is null ? null : _translator[key];
        NameError = Text(validation.NameErrorKey);
        AmountError = Text(validation.AmountErrorKey);
        AccountError = Text(validation.AccountErrorKey);
        ToAccountError = Text(validation.DestinationErrorKey);
        ToAmountError = Text(validation.DestinationAmountErrorKey);
        RuleError = Text(validation.RuleErrorKey);
        if (validation.HasErrors || Account is null)
        {
            ValidationFailed?.Invoke(this, EventArgs.Empty);
            return;
        }
        var amount = validation.Amount;
        var toAmount = validation.DestinationAmount;

        var session = _saveSession; var owner = _loadSession;
        if (session is null || owner is null) return;
        var epoch = _formEpoch; var raw = Snapshot();
        bool Current() => epoch == _formEpoch && !_formDeparted && ReferenceEquals(owner, _loadSession)
            && ReferenceEquals(session, _saveSession) && _entryOrigin?.IsOwned != false;
        var (previous, target) = BuildWriteCandidate(rule); target.Amount = amount; target.ToAmount = toAmount;
        var saved = false; IsBusy = true;
        try
        {
            await owner.ValidateAsync();
            if (!Current() || raw != Snapshot()) return;
            if (!await session.SaveAsync(_hasHistory ? previous : null, target, () => Current() && raw == Snapshot()))
            { if (Current()) SaveError = _translator["Plan_SplitConflict"]; return; }
            if (!Current()) return;
            saved = session.Accepted; _committed = saved; _snapshot = Snapshot(); RefreshAccess();
            if (session.NotificationFailed) { SaveError = _translator["Plan_SavedNeedsReview"]; return; }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (!Current()) return;
            if (session.Accepted)
            { _committed = true; _snapshot = Snapshot(); SaveError = _translator["Plan_SavedNeedsReview"]; RefreshAccess(); return; }
            if (exception is PlanEditorAccessChangedException)
            {
                // Explain current denial without silently granting a new writer to the old raw form.
                try
                {
                    var review = await _plans.RefreshEditorSnapshotAsync(session.Source);
                    if (!Current()) return;
                    var reason = review.Inputs.Work.PlanRestriction(target, _hasHistory ? _existing?.Id : null);
                    _workRetired = true;
                    SaveError = reason is null ? _translator["Editor_WorkRetired"] : CommercialFailureMessage.Describe(reason, _translator);
                }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                    if (!Current()) return;
                    if (error is PlanEditorSourceChangedException or EntryEditorSourceChangedException)
                    { _originalSourceChanged = true; owner.Retire(); Loading = new(); await FailOriginalReadAsync(Loading, error); }
                    else { _work = null; _workFailed = true; SaveError = _translator["Editor_WorkReadFailed"]; }
                }
                RefreshAccess();
            }
            else if (exception is PlanEditorSourceChangedException or EntryEditorSourceChangedException)
            {
                _originalSourceChanged = true; owner.Retire(); Loading = new(); await FailOriginalReadAsync(Loading, exception);
            }
            else
            {
                if (exception is CommercialWriteRejectedException) { _workRetired = true; RefreshAccess(); }
                SaveError = CommercialFailureMessage.OrFallback(exception, _translator, "Common_SaveFailed", draftKept: true);
            }
        }
        finally { if (Current()) { IsBusy = false; PublishFormAvailability(); } }
        // A return belongs to the same accepted form. It cannot close a new routed query after an await.
        if (saved && Current())
        {
            try { await _interaction.NavigateAsync(".."); }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            { if (Current()) { SaveError = _translator["Editor_SavedReturnFailed"]; await _interaction.ShowFailureAsync(exception); } }
        }
    }

    private async Task CancelAsync()
    {
        var epoch = _formEpoch; var source = _loadSession; var raw = Snapshot();
        if (_committed || !IsDirty || await ConfirmDiscardAsync())
        {
            if (epoch != _formEpoch || _formDeparted || !ReferenceEquals(source, _loadSession) || raw != Snapshot()) return;
            try { await _interaction.NavigateAsync(".."); }
            catch (Exception exception) when (exception is not OutOfMemoryException) { await _interaction.ShowFailureAsync(exception); }
        }
    }

    private async Task AddAccountAsync()
    {
        if (!CanInteract || !HasNoAccounts || _loadSession is not { } owner) return;
        var epoch = _formEpoch;
        try
        {
            await owner.ValidateAsync();
            if (epoch != _formEpoch || _formDeparted || !ReferenceEquals(owner, _loadSession) || owner.Review is not { } source) return;
            PlanAccountHandoff? visit = null;
            visit = new(_plans, source, () => ReferenceEquals(owner, _loadSession) && ReferenceEquals(_accountVisit, visit));
            _accountVisit = visit;
            await _interaction.NavigateAsync(AppRoutes.AccountEditorRoute, new Dictionary<string, object> { ["originalAccountHandoff"] = visit });
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (epoch == _formEpoch && !_formDeparted && ReferenceEquals(owner, _loadSession))
            { owner.Retire(); Loading = new(); await FailOriginalReadAsync(Loading, error); }
        }
    }

    public Task<bool> ConfirmDiscardAsync() => _interaction.ConfirmAsync(
        _translator["Common_DiscardTitle"], _translator["Common_DiscardMessage"], _translator["Common_Discard"], _translator["Common_KeepEditing"]);

    private string Snapshot() => Presentation.DraftFingerprint.Create(
        KindIndex, Name, AmountModeIndex, AmountText, Account?.Id, ToAccount?.Id, ToAmountText, PresetIndex, IntervalText, UnitIndex, Start,
        CalendarIndex, DayRuleIndex, MissingDayIndex, WeekendIndex, SkipHolidays, HasSecondDay, SecondDayText, EndIndex, EndDate, CountText, PastIndex, AutoPost, ReminderEnabled, ReminderDaysText,
        ReminderTime, ReminderOnDueDate, Note, ContractProvider, ContractReference, HasContractEnd, ContractEnd, ContractRenews,
        HasCancellationDeadline, CancellationDeadline, HasReviewDate, ReviewDate, Categories.FirstOrDefault(c => c.IsSelected)?.Id);
}
