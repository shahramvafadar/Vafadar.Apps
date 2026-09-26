using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Finance.Core.Goals;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Goals;

/// <summary>Creates or edits a savings goal (F2-GOAL-01).</summary>
public sealed partial class GoalEditorViewModel : ViewModelBase, IQueryAttributable
{
    private readonly FinanceStore _finance;
    private readonly GoalStore _goals;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private Goal? _existing;

    public GoalEditorViewModel(FinanceStore finance, GoalStore goals, Translator translator, ILocalizationService localization, TimeProvider time)
    {
        _finance = finance;
        _goals = goals;
        _translator = translator;
        _localization = localization;
        _time = time;
        Title = translator["Goal_NewTitle"];
        Name = string.Empty;
        AmountText = string.Empty;
        Note = string.Empty;
        CurrencyCode = Currencies.Euro.Code;
        TargetDate = Today.AddYears(1);
        FrequencyNames = [translator["Goal_Monthly"], translator["Goal_Weekly"]];
        PriorityNames = [translator["GoalPriority_High"], translator["GoalPriority_Normal"], translator["GoalPriority_Low"]];
        PriorityIndex = (int)GoalPriority.Normal;
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];

    public IReadOnlyList<string> FrequencyNames { get; }

    public IReadOnlyList<string> PriorityNames { get; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string AmountText { get; set; }

    [ObservableProperty]
    public partial string CurrencyCode { get; set; }

    [ObservableProperty]
    public partial bool CurrencyLocked { get; set; }

    [ObservableProperty]
    public partial bool HasTargetDate { get; set; }

    [ObservableProperty]
    public partial DateOnly TargetDate { get; set; }

    [ObservableProperty]
    public partial int FrequencyIndex { get; set; }

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

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var settings = await _finance.GetSettingsAsync();
        CurrencyCode = settings.ReportCurrencyCode;
        if (query.TryGetValue("id", out var value) && value is Guid id && await _goals.GetGoalAsync(id) is { } goal)
        {
            _existing = goal;
            Title = _translator["Goal_EditTitle"];
            Name = goal.Name;
            CurrencyCode = goal.CurrencyCode;
            AmountText = MoneyText.ForInput(goal.TargetAmount, goal.CurrencyCode, _localization.CurrentCulture);
            HasTargetDate = goal.TargetDate is not null;
            TargetDate = goal.TargetDate ?? Today.AddYears(1);
            FrequencyIndex = (int)goal.Frequency;
            PriorityIndex = (int)goal.Priority;
            IconKey = goal.Icon;
            Note = goal.Note ?? string.Empty;

            // Allocations are in the goal's currency; changing it would relabel money (ACC-07 applies the same way).
            CurrencyLocked = (await _goals.GetAllocationsAsync(goal.Id)).Count > 0;
        }

        query.Clear();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        NameError = string.IsNullOrWhiteSpace(Name) ? _translator["Goal_NameRequired"] : null;
        AmountError = MoneyAmount.TryParse(AmountText, Currencies.Get(CurrencyCode), _localization.CurrentCulture, out var amount) && amount > 0
            ? null
            : _translator["Amount_Invalid"];
        if (NameError is not null || AmountError is not null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var goal = _existing ?? new Goal { Name = Name.Trim(), CurrencyCode = CurrencyCode };
            goal.Name = Name.Trim();
            goal.TargetAmount = amount;
            goal.CurrencyCode = CurrencyCode;
            goal.TargetDate = HasTargetDate ? TargetDate : null;
            goal.Frequency = (ContributionFrequency)Math.Clamp(FrequencyIndex, 0, 1);
            goal.Priority = (GoalPriority)Math.Clamp(PriorityIndex, 0, 2);
            goal.Icon = IconKey;
            goal.Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
            await _goals.SaveGoalAsync(goal);
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
}