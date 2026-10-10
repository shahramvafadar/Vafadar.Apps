using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>
/// Review of one occurrence (UI-09): confirm with the actual amount and date, link an existing entry, move the due date,
/// change the amount of this occurrence, skip – and undo each of them (REC-14, REC-17, REC-18).
/// </summary>
public sealed partial class OccurrenceViewModel(
    ZananceStore store,
    PlanStore plans,
    Translator translator,
    ILocalizationService localization,
    IDateFormatter dates,
    TimeProvider time) : ViewModelBase, IQueryAttributable, Presentation.IThemeAware
{
    private Guid _scheduleId;
    private DateOnly _originalDate;
    private Occurrence? _occurrence;
    private string _currency = Currencies.Euro.Code;
    private bool _paymentValidationRequested;
    private bool _overrideValidationRequested;

    /// <summary>Requests visibility of the input belonging to the attempted action without changing focus.</summary>
    public event Action<OccurrenceInput>? ValidationFailed;

    /// <summary>Gets or sets the explanation beside the actual payment input.</summary>
    [ObservableProperty]
    public partial string? PaymentAmountError { get; set; }

    /// <summary>Gets or sets the explanation beside the optional occurrence override.</summary>
    [ObservableProperty]
    public partial string? OverrideAmountError { get; set; }

    public ObservableCollection<EntryRow> Candidates { get; } = [];

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial string? DueText { get; set; }

    [ObservableProperty]
    public partial string? PlannedAmountText { get; set; }

    [ObservableProperty]
    public partial string? StatusText { get; set; }

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    [ObservableProperty]
    public partial bool IsSettled { get; set; }

    [ObservableProperty]
    public partial bool IsSkipped { get; set; }

    [ObservableProperty]
    public partial string ActualAmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateOnly ActualDate { get; set; } = DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    [ObservableProperty]
    public partial string? CurrencyCode { get; set; }

    [ObservableProperty]
    public partial DateOnly DueDate { get; set; } = DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    [ObservableProperty]
    public partial string OverrideAmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OccurrenceNote { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowChange { get; set; }

    [ObservableProperty]
    public partial bool HasCandidates { get; set; }

    [ObservableProperty]
    public partial string? SettledText { get; set; }

    // Partial payments (F2-TX-02): what was paid so far and what is still outstanding.
    public ObservableCollection<EntryRow> Payments { get; } = [];

    [ObservableProperty]
    public partial string? PaidText { get; set; }

    [ObservableProperty]
    public partial bool HasPayments { get; set; }

    [ObservableProperty]
    public partial string? Error { get; set; }

    [ObservableProperty]
    public partial bool NotFound { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    partial void OnActualAmountTextChanged(string value)
    {
        if (_paymentValidationRequested) { ValidateAmount(OccurrenceInput.Payment); }
    }

    partial void OnOverrideAmountTextChanged(string value)
    {
        if (_overrideValidationRequested) { ValidateAmount(OccurrenceInput.Override); }
    }

    // Keep each correction attached to its original input; changing an override must not replace payment feedback.
    private OccurrenceAmountResult ValidateAmount(OccurrenceInput field)
    {
        var payment = field == OccurrenceInput.Payment;
        var result = OccurrenceAmountValidation.Validate(payment ? ActualAmountText : OverrideAmountText,
            _currency, localization.CurrentCulture, payment);
        var explanation = result.ErrorKey is { } key ? translator[key] : null;
        if (payment) { PaymentAmountError = explanation; }
        else { OverrideAmountError = explanation; }
        return result;
    }

    private long? ValidatePayment()
    {
        _paymentValidationRequested = true;
        var result = ValidateAmount(OccurrenceInput.Payment);
        if (result.ErrorKey is not null) { ValidationFailed?.Invoke(OccurrenceInput.Payment); }
        return result.Amount;
    }

    /// <summary>Query: <c>plan</c> (id) and <c>date</c> (original date).</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("plan", out var plan) && plan is Guid id)
        {
            _scheduleId = id;
        }

        if (query.TryGetValue("date", out var date) && date is DateOnly original)
        {
            _originalDate = original;
        }
    }

    // Loading fills the payment fields with their defaults; what the user typed or picked stays.
    async Task Presentation.IThemeAware.RefreshThemeAsync()
    {
        var (actualDate, actualAmount, dueDate, overrideAmount, note) = (ActualDate, ActualAmountText, DueDate, OverrideAmountText, OccurrenceNote);
        await LoadAsync();
        (ActualDate, ActualAmountText, DueDate, OverrideAmountText, OccurrenceNote) = (actualDate, actualAmount, dueDate, overrideAmount, note);
    }

    public async Task LoadAsync()
    {
        var schedule = await plans.GetScheduleAsync(_scheduleId);
        var states = await plans.GetStatesAsync(_scheduleId);

        // The due date may have been moved away from the original date; search around it.
        _occurrence = schedule is null ? null : Occurrences.Between(schedule, states, _originalDate.AddDays(-400), _originalDate.AddDays(400), Today)
            .FirstOrDefault(o => o.OriginalDate == _originalDate);
        NotFound = _occurrence is null;
        if (_occurrence is null || schedule is null)
        {
            return;
        }

        var occurrence = _occurrence;
        var culture = localization.CurrentCulture;
        var accounts = (await store.GetAccountsAsync()).ToDictionary(a => a.Id);
        var categories = new CategoryLookup(await store.GetCategoriesAsync(), translator);
        var text = new PlanText(translator, dates, localization);
        _currency = accounts.TryGetValue(schedule.AccountId, out var account) ? account.CurrencyCode : Currencies.Euro.Code;

        Name = schedule.Name;
        CurrencyCode = _currency;
        DueText = translator.Format("Occurrence_DueOn", text.Date(occurrence.DueDate));
        PlannedAmountText = text.Amount(occurrence.Amount, occurrence.AmountMode, _currency);
        StatusText = text.Status(occurrence.Status);
        IsOpen = occurrence.IsOpen;
        IsSettled = occurrence.Status == OccurrenceView.Settled;
        IsSkipped = occurrence.Status == OccurrenceView.Skipped;

        // Paying early or late keeps the real date (FIN-07); the default is today, or the due date when it is past.
        ActualDate = occurrence.DueDate < Today ? occurrence.DueDate : Today;
        var expected = occurrence.Paid > 0 ? occurrence.Outstanding : occurrence.Amount;
        ActualAmountText = expected is { } amount and > 0 ? MoneyText.ForInput(amount, _currency, culture) : string.Empty;
        DueDate = occurrence.DueDate;
        OverrideAmountText = occurrence.State?.Amount is { } overridden ? MoneyText.ForInput(overridden, _currency, culture) : string.Empty;
        OccurrenceNote = occurrence.State?.Note ?? string.Empty;

        Candidates.Clear();
        SettledText = null;
        var presenter = new EntryPresenter(accounts, categories, translator, culture);
        if (IsOpen)
        {
            var entries = await store.GetEntriesAsync(occurrence.DueDate.AddDays(-10), occurrence.DueDate.AddDays(10));
            foreach (var candidate in PlanActions.LinkCandidates(occurrence, entries).Take(5))
            {
                Candidates.Add(presenter.Row(candidate) with { Subtitle = dates.Format(candidate.Date, DateFormatStyle.Long) });
            }
        }
        else if (occurrence.State?.EntryId is { } entryId && await store.GetEntryAsync(entryId) is { } entry)
        {
            SettledText = translator.Format("Occurrence_SettledWith", presenter.Amount(entry), dates.Format(entry.Date, DateFormatStyle.Long));
        }

        HasCandidates = Candidates.Count > 0;

        Payments.Clear();
        foreach (var payment in await plans.GetPartialPaymentsAsync(occurrence))
        {
            Payments.Add(presenter.Row(payment) with { Subtitle = dates.Format(payment.Date, DateFormatStyle.Long) });
        }

        HasPayments = Payments.Count > 0;
        PaidText = occurrence.Paid <= 0 ? null
            : occurrence.Outstanding is { } outstanding
                ? translator.Format("Occurrence_PaidSoFar", MoneyText.Format(occurrence.Paid, _currency, culture), MoneyText.Format(outstanding, _currency, culture))
                : translator.Format("Occurrence_PaidSoFarUnknown", MoneyText.Format(occurrence.Paid, _currency, culture));
    }

    // F2-TX-02: records part of the amount; the occurrence stays open (and reminded) until the rest is paid.
    [RelayCommand]
    private async Task PayPartAsync()
    {
        if (_occurrence is null || IsBusy)
        {
            return;
        }

        Error = null;
        if (ValidatePayment() is not { } amount)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var entry = Occurrences.CreateEntry(_occurrence, amount, ActualDate, ReviewState.Confirmed);
            var result = await plans.PayPartAsync(_occurrence, entry);
            if (!result.Succeeded)
            {
                Error = string.Join(Environment.NewLine, result.Errors.Select(e => translator[$"LedgerError_{e}"]));
                return;
            }

            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenPaymentAsync(EntryRow row) => Shell.Current.GoToAsync(AppShell.EntryDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (_occurrence is null || IsBusy)
        {
            return;
        }

        Error = null;
        if (ValidatePayment() is not { } amount)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var entry = Occurrences.CreateEntry(_occurrence, amount, ActualDate, ReviewState.Confirmed);
            var result = await plans.SettleAsync(_occurrence, entry);
            if (!result.Succeeded)
            {
                Error = string.Join(Environment.NewLine, result.Errors.Select(e => translator[$"LedgerError_{e}"]));
                return;
            }

            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LinkAsync(EntryRow row)
    {
        if (_occurrence is null || !await Shell.Current.DisplayAlertAsync(
                translator["Occurrence_Link"], translator.Format("Occurrence_LinkMessage", row.Title, row.AmountText), translator["Occurrence_Link"], translator["Common_Cancel"]))
        {
            return;
        }

        await plans.LinkAsync(_occurrence, row.Id);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task SkipAsync()
    {
        if (_occurrence is null)
        {
            return;
        }

        await plans.SkipAsync(_occurrence);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private void ToggleChange() => ShowChange = !ShowChange;

    [RelayCommand]
    private async Task SaveChangeAsync()
    {
        if (_occurrence is null || IsBusy)
        {
            return;
        }

        Error = null;
        _overrideValidationRequested = true;
        var validation = ValidateAmount(OccurrenceInput.Override);
        if (validation.ErrorKey is not null)
        {
            ValidationFailed?.Invoke(OccurrenceInput.Override);
            return;
        }

        IsBusy = true;
        try
        {
            await plans.ChangeOccurrenceAsync(_occurrence, DueDate, validation.Amount, OccurrenceNote);
            ShowChange = false;
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task UndoAsync()
    {
        if (_occurrence is null)
        {
            return;
        }

        if (IsSkipped)
        {
            await plans.UnskipAsync(_occurrence);
        }
        else if (IsSettled)
        {
            // Explain the effect first (REC-18): a plan-created entry is deleted, a linked own entry is only unlinked.
            if (!await Shell.Current.DisplayAlertAsync(translator["Occurrence_Undo"], translator["Occurrence_UndoMessage"], translator["Occurrence_Undo"], translator["Common_Cancel"]))
            {
                return;
            }

            await plans.UnsettleAsync(_occurrence);
        }

        await LoadAsync();
    }

    [RelayCommand]
    private Task OpenPlanAsync() => Shell.Current.GoToAsync(AppShell.PlanDetailRoute, new Dictionary<string, object> { ["id"] = _scheduleId });
}
