using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Finance.App.Features.Entries;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Plans;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Plans;

/// <summary>An occurrence in the plan details.</summary>
public sealed record OccurrenceRow(DateOnly OriginalDate, string DateText, string AmountText, string StatusText, Color StatusColor);

/// <summary>A plan with its next and past occurrences; pause, resume, end and delete (REC-16).</summary>
public sealed partial class PlanDetailViewModel(
    FinanceStore finance,
    PlanStore plans,
    Translator translator,
    ILocalizationService localization,
    IDateFormatter dates,
    TimeProvider time) : ViewModelBase, IQueryAttributable
{
    private Guid _id;
    private Schedule? _schedule;

    public ObservableCollection<DetailLine> Lines { get; } = [];

    public ObservableCollection<OccurrenceRow> Upcoming { get; } = [];

    public ObservableCollection<OccurrenceRow> History { get; } = [];

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial string? AmountText { get; set; }

    [ObservableProperty]
    public partial string? RuleText { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial bool IsPaused { get; set; }

    [ObservableProperty]
    public partial bool IsEnded { get; set; }

    [ObservableProperty]
    public partial bool CanDelete { get; set; }

    [ObservableProperty]
    public partial bool HasHistory { get; set; }

    [ObservableProperty]
    public partial DateOnly ResumeFrom { get; set; }

    [ObservableProperty]
    public partial bool NotFound { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && value is Guid id)
        {
            _id = id;
        }
    }

    public async Task LoadAsync()
    {
        _schedule = await plans.GetScheduleAsync(_id);
        NotFound = _schedule is null;
        if (_schedule is null)
        {
            return;
        }

        var schedule = _schedule;
        var accounts = (await finance.GetAccountsAsync()).ToDictionary(a => a.Id);
        var categories = new CategoryLookup(await finance.GetCategoriesAsync(), translator);
        var states = await plans.GetStatesAsync(schedule.Id);
        var text = new PlanText(translator, dates, localization.CurrentCulture);
        var currency = accounts.TryGetValue(schedule.AccountId, out var account) ? account.CurrencyCode : "EUR";
        var today = Today;

        Name = schedule.Name;
        AmountText = text.Amount(schedule.Amount, schedule.AmountMode, currency);
        RuleText = text.Rule(schedule.Rule);
        IsActive = schedule.State == ScheduleState.Active;
        IsPaused = schedule.State == ScheduleState.Paused;
        IsEnded = schedule.State == ScheduleState.Ended;
        ResumeFrom = today;

        Lines.Clear();
        Lines.Add(new DetailLine(translator["Entry_Kind"], translator["EntryKind_" + schedule.Kind]));
        Lines.Add(new DetailLine(translator[schedule.Kind == Core.Ledger.EntryKind.Transfer ? "Entry_FromAccount" : "Entry_Account"], account?.Name ?? "?"));
        if (schedule.ToAccountId is { } to && accounts.TryGetValue(to, out var destination))
        {
            Lines.Add(new DetailLine(translator["Entry_ToAccount"], destination.Name));
        }

        if (schedule.CategoryId is not null)
        {
            Lines.Add(new DetailLine(translator["Entry_Category"], categories.Name(schedule.CategoryId)));
        }

        Lines.Add(new DetailLine(translator["Plan_AutoPost"], translator[schedule.AutoPost ? "Common_Yes" : "Common_No"]));
        if (schedule.ReminderEnabled)
        {
            Lines.Add(new DetailLine(translator["Plan_Reminder"], translator.Format("Plan_ReminderText", schedule.ReminderDaysBefore, schedule.ReminderTime.ToString("t", localization.CurrentCulture))
                + (schedule.ReminderOnDueDate && schedule.ReminderDaysBefore > 0 ? " · " + translator["Plan_ReminderAlsoDue"] : string.Empty)));
        }

        // Contract details (F2-CON-01); the deadline is to cancel, not to pay.
        if (schedule.ContractProvider is { } provider)
        {
            Lines.Add(new DetailLine(translator["Contract_Provider"], provider));
        }

        if (schedule.ContractReference is { } reference)
        {
            Lines.Add(new DetailLine(translator["Contract_Reference"], reference));
        }

        if (schedule.ContractEnd is { } contractEnd)
        {
            Lines.Add(new DetailLine(translator["Contract_End"], dates.Format(contractEnd, DateFormatStyle.Long) + (schedule.ContractRenews ? " · " + translator["Contract_RenewsShort"] : string.Empty)));
        }

        if (schedule.CancellationDeadline is { } deadline)
        {
            Lines.Add(new DetailLine(translator["Contract_Deadline"], dates.Format(deadline, DateFormatStyle.Long)));
        }

        if (schedule.ReviewDate is { } review)
        {
            Lines.Add(new DetailLine(translator["Contract_Review"], dates.Format(review, DateFormatStyle.Long)));
        }

        if (!string.IsNullOrEmpty(schedule.Note))
        {
            Lines.Add(new DetailLine(translator["Entry_Note"], schedule.Note));
        }

        Upcoming.Clear();
        if (schedule.State == ScheduleState.Active)
        {
            foreach (var occurrence in Occurrences.Between(schedule, states, today.AddDays(-400), today.AddYears(2), today).Where(o => o.IsOpen).Take(6))
            {
                Upcoming.Add(Row(occurrence, text, currency));
            }
        }

        History.Clear();
        foreach (var occurrence in Occurrences.Between(schedule, states, DateOnly.MinValue, today.AddYears(2), today).Where(o => !o.IsOpen).Reverse().Take(12))
        {
            History.Add(Row(occurrence, text, currency));
        }

        HasHistory = History.Count > 0;
        CanDelete = !await plans.HasHistoryAsync(schedule.Id);
    }

    private OccurrenceRow Row(Occurrence occurrence, PlanText text, string currency)
    {
        var color = occurrence.Status switch
        {
            OccurrenceView.Overdue => EntryPresenter.ExpenseColor,
            OccurrenceView.Settled => EntryPresenter.IncomeColor,
            _ => EntryPresenter.NeutralColor,
        };
        return new OccurrenceRow(occurrence.OriginalDate, text.Date(occurrence.DueDate), text.Amount(occurrence.IsOpen && occurrence.Paid > 0 ? occurrence.Outstanding : occurrence.Amount, occurrence.AmountMode, currency), text.Status(occurrence.Status), color);
    }

    [RelayCommand]
    private Task OpenOccurrenceAsync(OccurrenceRow row) =>
        Shell.Current.GoToAsync(AppShell.OccurrenceRoute, new Dictionary<string, object> { ["plan"] = _id, ["date"] = row.OriginalDate });

    [RelayCommand]
    private Task EditAsync() => Shell.Current.GoToAsync(AppShell.PlanEditorRoute, new Dictionary<string, object> { ["id"] = _id });

    [RelayCommand]
    private async Task PauseAsync()
    {
        if (_schedule is null || !await Shell.Current.DisplayAlertAsync(translator["Plan_Pause"], translator["Plan_PauseMessage"], translator["Plan_Pause"], translator["Common_Cancel"]))
        {
            return;
        }

        PlanActions.Pause(_schedule, Today);
        await plans.SaveScheduleAsync(_schedule);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ResumeAsync()
    {
        if (_schedule is null || PlanActions.Resume(_schedule, ResumeFrom) is not { } continuation)
        {
            return;
        }

        await plans.SaveSchedulesAsync([_schedule, continuation]);
        _id = continuation.Id;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task EndAsync()
    {
        if (_schedule is null || !await Shell.Current.DisplayAlertAsync(translator["Plan_EndAction"], translator["Plan_EndMessage"], translator["Plan_EndAction"], translator["Common_Cancel"]))
        {
            return;
        }

        PlanActions.End(_schedule, Today);
        await plans.SaveScheduleAsync(_schedule);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_schedule is null || !await Shell.Current.DisplayAlertAsync(translator["Plan_Delete"], translator["Plan_DeleteMessage"], translator["Common_Delete"], translator["Common_Cancel"]))
        {
            return;
        }

        if (await plans.DeleteScheduleAsync(_schedule.Id))
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
