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
    TimeProvider time,
    OccurrenceCommands commands) : ViewModelBase, IQueryAttributable, Presentation.IThemeAware
{
    private Guid _scheduleId;
    private DateOnly _originalDate;
    private Occurrence? _occurrence;
    private string _currency = Currencies.Euro.Code;
    private bool _paymentValidationRequested;
    private bool _overrideValidationRequested;
    private readonly OccurrenceMoneyInputs _money = new(localization);
    private string? _file;
    private long _draftRevision;
    /// <summary>Gets the payment's original visible unit independently of later display preferences.</summary>
    public string PaymentUnitName => _money.Payment.UnitName(_currency);
    /// <summary>Explains the payment's original display-unit factor.</summary>
    public string? PaymentUnitNote => DisplayUnitNote.ForInput(translator, _money.Payment, _currency);
    /// <summary>Explains the payment's original numeric format.</summary>
    public string? PaymentFormatNote => OriginalFormat(_money.Payment);
    /// <summary>Explains the optional override's independent original display-unit factor.</summary>
    public string? OverrideUnitNote => DisplayUnitNote.ForInput(translator, _money.Override, _currency);
    /// <summary>Gets the override's original unit even when it has no custom factor.</summary>
    public string OverrideUnitName => _money.Override.UnitName(_currency);
    /// <summary>Explains the optional override's independent original numeric format.</summary>
    public string? OverrideFormatNote => OriginalFormat(_money.Override);
    private string? OriginalFormat(MoneyDraftInput input) => input.OriginalExample(localization.CurrentCulture) is { } example
        ? translator.Format("Plan_OriginalInputFormat", example) : null;
    private void RefreshMoneyCaptions()
    {
        OnPropertyChanged(nameof(PaymentUnitName)); OnPropertyChanged(nameof(PaymentUnitNote)); OnPropertyChanged(nameof(PaymentFormatNote));
        OnPropertyChanged(nameof(OverrideUnitName)); OnPropertyChanged(nameof(OverrideUnitNote)); OnPropertyChanged(nameof(OverrideFormatNote));
    }
    private (Guid Schedule, DateOnly Original)? _publishedQuery;
    private (EntryKind Kind, Guid Account, Guid? Destination, string Currency)? _publishedFinancialTarget;

    /// <summary>Keeps original fields covered until one complete appearance or retained-tab read is published.</summary>
    public SnapshotLoadState Loading { get; } = new();

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
        _money.ChangePayment(value);
        _draftRevision++;
        if (_paymentValidationRequested || _money.Payment.NeedsReview(_currency)) { ValidateAmount(OccurrenceInput.Payment); }
        RefreshMoneyCaptions();
    }

    partial void OnOverrideAmountTextChanged(string value)
    {
        _money.ChangeOverride(value);
        _draftRevision++;
        if (_overrideValidationRequested || _money.Override.NeedsReview(_currency)) { ValidateAmount(OccurrenceInput.Override); }
        RefreshMoneyCaptions();
    }

    // Keep each correction attached to its original input; changing an override must not replace payment feedback.
    private OccurrenceAmountResult ValidateAmount(OccurrenceInput field)
    {
        var payment = field == OccurrenceInput.Payment;
        var result = _money.Validate(payment);
        var explanation = result.ErrorKey == "Plan_InputUnitChanged" ? translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(_currency))
            : result.ErrorKey is { } key ? translator[key] : null;
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

    partial void OnActualDateChanged(DateOnly value) => _draftRevision++;
    partial void OnDueDateChanged(DateOnly value) => _draftRevision++;
    partial void OnOccurrenceNoteChanged(string value) => _draftRevision++;

    /// <summary>Query: <c>plan</c> (id) and <c>date</c> (original date).</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("plan", out var plan) && plan is Guid id)
        {
            if (_scheduleId != id) _draftRevision++;
            _scheduleId = id;
        }

        if (query.TryGetValue("date", out var date) && date is DateOnly original)
        {
            if (_originalDate != original) _draftRevision++;
            _originalDate = original;
        }
    }

    Task Presentation.IThemeAware.RefreshThemeAsync() => RefreshForNavigationAsync();

    /// <summary>Loads canonical defaults initially or after a known accepted command.</summary>
    public Task LoadAsync() => ReadAndPublishAsync(preserveDraft: false);

    /// <summary>Refreshes retained suggestions without replacing typed fields or interrupting native consent.</summary>
    internal async Task RefreshForNavigationAsync()
    {
        if (IsBusy) { return; }
        try { await ReadAndPublishAsync(preserveDraft: true); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // D-401: the failed cover owns retry feedback. A rejected read must never expose the old writable packet.
            Error = CommercialFailureMessage.OrFallback(exception, translator, "Occurrence_ReadFailed");
        }
    }

    [RelayCommand]
    private Task ReloadAsync() => RefreshForNavigationAsync();

    private Task ReadAndPublishAsync(bool preserveDraft) => Loading.RunAsync(async () =>
    {
        _draftRevision++;
        var query = (Schedule: _scheduleId, Original: _originalDate);
        var keepInputs = preserveDraft && _publishedQuery is { } published && published == query;
        try
        {
            await LoadCoreAsync(query.Schedule, query.Original, keepInputs);
            if (query != (_scheduleId, _originalDate)) throw new InvalidOperationException("The original occurrence route changed during its read.");
            if (_occurrence is { } current)
            {
                var target = (current.Schedule.Kind, current.Schedule.AccountId, current.Schedule.ToAccountId, _currency);
                if (keepInputs && _publishedFinancialTarget is { } previous && target != previous)
                    throw new InvalidOperationException("The original payment target changed; close and review it again.");
                _publishedFinancialTarget = target;
                _publishedQuery = query;
            }
            Error = null;
        }
        catch (Exception exception)
        {
            _occurrence = null;
            if (exception is not OutOfMemoryException)
                Error = CommercialFailureMessage.OrFallback(exception, translator, "Occurrence_ReadFailed");
            throw;
        }
    }, () => { });

    private async Task LoadCoreAsync(Guid scheduleId, DateOnly originalDate, bool keepInputs)
    {
        _occurrence = null;
        var original = await plans.ReadOriginalScheduleAsync(scheduleId);
        var schedule = original.Schedule; _file = original.DatabasePath;
        var states = await plans.GetStatesAsync(scheduleId);

        // The due date may have been moved away from the original date; search around it.
        var loaded = schedule is null ? null : Occurrences.Between(schedule, states, originalDate.AddDays(-400), originalDate.AddDays(400), Today)
            .FirstOrDefault(o => o.OriginalDate == originalDate);
        NotFound = loaded is null;
        if (loaded is null || schedule is null)
        {
            return;
        }

        var occurrence = loaded;
        var culture = localization.CurrentCulture;
        var accounts = (await store.GetAccountsAsync()).ToDictionary(a => a.Id);
        var categories = new CategoryLookup(await store.GetCategoriesAsync(), translator);
        var text = new PlanText(translator, dates, localization);
        _currency = accounts.TryGetValue(schedule.AccountId, out var account) ? account.CurrencyCode : Currencies.Euro.Code;
        _money.SelectCurrency(_currency);

        Name = schedule.Name;
        CurrencyCode = _currency;
        DueText = translator.Format("Occurrence_DueOn", text.Date(occurrence.DueDate));
        PlannedAmountText = text.Amount(occurrence.Amount, occurrence.AmountMode, _currency);
        StatusText = text.Status(occurrence.Status);
        IsOpen = occurrence.IsOpen;
        IsSettled = occurrence.Status == OccurrenceView.Settled;
        IsSkipped = occurrence.Status == OccurrenceView.Skipped;

        // Paying early or late keeps the real date (FIN-07); the default is today, or the due date when it is past.
        if (!keepInputs)
        {
            ActualDate = occurrence.DueDate < Today ? occurrence.DueDate : Today;
            var expected = occurrence.Paid > 0 ? occurrence.Outstanding : occurrence.Amount;
            _money.Initialize(expected, occurrence.State?.Amount);
            ActualAmountText = _money.Payment.Text;
            DueDate = occurrence.DueDate;
            OverrideAmountText = _money.Override.Text;
            OccurrenceNote = occurrence.State?.Note ?? string.Empty;
        }
        // Preserving a draft must not temporarily reseed DateField or its partial native input segments.
        // Re-translate requested feedback independently of the retained raw amount/date/note values.
        else
        {
            if (_paymentValidationRequested || _money.Payment.NeedsReview(_currency)) { ValidateAmount(OccurrenceInput.Payment); }
            if (_overrideValidationRequested || _money.Override.NeedsReview(_currency)) { ValidateAmount(OccurrenceInput.Override); }
        }
        RefreshMoneyCaptions();

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
        _occurrence = occurrence;
    }

    // D-136: one visible busy gate spans every write and confirmation. Rejected writes never reload the draft.
    [RelayCommand]
    private Task PayPartAsync() => PayAsync(partial: true);

    [RelayCommand]
    private Task ConfirmAsync() => PayAsync(partial: false);

    /// <summary>Validates the current input before sending its captured actual amount/date to the writer.</summary>
    private async Task PayAsync(bool partial)
    {
        if (_occurrence is not { } occurrence || IsBusy || !Loading.IsReady) { return; }
        Error = null;
        if (ValidatePayment() is not { } amount) { return; }
        if (_file is not { } file) return;
        var revision = _draftRevision; var date = ActualDate; var currency = _currency;
        await RunCommandAsync(() => commands.PayAsync(occurrence, amount, date, partial,
            error => Error = error, () => CompleteCommandAsync(navigate: !partial), file, currency,
            () => revision == _draftRevision && ReferenceEquals(_occurrence, occurrence) && Loading.IsReady
                && !_money.Payment.NeedsReview(currency)));
    }

    [RelayCommand]
    private Task OpenPaymentAsync(EntryRow row) => Shell.Current.GoToAsync(AppShell.EntryDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });

    [RelayCommand]
    private Task LinkAsync(EntryRow row) => _occurrence is { } occurrence
        ? RunCommandAsync(() => commands.LinkAsync(occurrence, row.Id, row.Title, row.AmountText,
            () => CompleteCommandAsync(navigate: true))) : Task.CompletedTask;

    [RelayCommand]
    private Task SkipAsync() => _occurrence is { } occurrence
        ? RunCommandAsync(() => commands.SkipAsync(occurrence, () => CompleteCommandAsync(navigate: true))) : Task.CompletedTask;

    [RelayCommand]
    private void ToggleChange() { if (!IsBusy && Loading.IsReady) { ShowChange = !ShowChange; } }

    [RelayCommand]
    private async Task SaveChangeAsync()
    {
        if (_occurrence is not { } occurrence || IsBusy || !Loading.IsReady) { return; }
        Error = null;
        _overrideValidationRequested = true;
        var validation = ValidateAmount(OccurrenceInput.Override);
        if (validation.ErrorKey is not null)
        {
            ValidationFailed?.Invoke(OccurrenceInput.Override);
            return;
        }
        if (_file is not { } file) return;
        var revision = _draftRevision; var due = DueDate; var note = OccurrenceNote; var currency = _currency;
        await RunCommandAsync(() => commands.ChangeAsync(occurrence, due, validation.Amount, note,
            () => CompleteCommandAsync(navigate: false, hideChange: true), file, currency,
            () => revision == _draftRevision && ReferenceEquals(_occurrence, occurrence) && Loading.IsReady
                && !_money.Override.NeedsReview(currency)));
    }

    [RelayCommand]
    private Task UndoAsync() => _occurrence is { } occurrence
        ? RunCommandAsync(() => commands.UndoAsync(occurrence, () => CompleteCommandAsync(navigate: false))) : Task.CompletedTask;

    /// <summary>Keeps all commands mutually exclusive, including while the native consent/failure dialog is open.</summary>
    private async Task RunCommandAsync(Func<Task<bool>> command)
    {
        if (IsBusy || !Loading.IsReady) { return; }
        IsBusy = true;
        try { await command(); }
        finally { IsBusy = false; }
    }

    /// <summary>Retires the previous payment identity before any fallible post-commit navigation or reload.</summary>
    private async Task CompleteCommandAsync(bool navigate, bool hideChange = false)
    {
        _occurrence = null;
        if (navigate) { await Shell.Current.GoToAsync(".."); }
        else
        {
            await LoadAsync();
            if (hideChange) { ShowChange = false; }
        }
    }

    [RelayCommand]
    private Task OpenPlanAsync() => IsBusy || !Loading.IsReady ? Task.CompletedTask
        : Shell.Current.GoToAsync(AppShell.PlanDetailRoute, new Dictionary<string, object> { ["id"] = _scheduleId });
}
