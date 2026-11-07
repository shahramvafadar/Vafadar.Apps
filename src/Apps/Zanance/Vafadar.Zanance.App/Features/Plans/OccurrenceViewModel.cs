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
    private long _readRevision;
    private bool _active = true;
    private Func<bool>? _isOwnerCurrent;
    private Func<bool>? _isRootCurrent;
    private bool _hasOriginalDate;
    private CancellationTokenSource _lifetime = new();
    private OccurrenceReadSnapshot? _review;
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
    [ObservableProperty] public partial SnapshotLoadState Loading { get; private set; } = new();

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
        var id = _scheduleId; var original = _originalDate; var hasDate = _hasOriginalDate;
        if (query.TryGetValue("plan", out var plan))
            id = plan is Guid value && value != Guid.Empty ? value : throw new ArgumentException("An original plan identity is required.", nameof(query));
        if (query.TryGetValue("date", out var date))
        {
            original = date is DateOnly value ? value : throw new ArgumentException("An original occurrence date is required.", nameof(query));
            hasDate = true;
        }
        if (id == _scheduleId && original == _originalDate && hasDate == _hasOriginalDate) return;
        _scheduleId = id; _originalDate = original; _hasOriginalDate = hasDate;
        RetireRead();
    }

    /// <summary>Retires actual page/tab/root work and native consent while preserving the unsaved raw draft.</summary>
    public void SetActive(bool active, Func<bool>? isCurrent = null, Func<bool>? isRootCurrent = null)
    {
        if (active && isCurrent is not null) _isOwnerCurrent = isCurrent;
        if (active && isRootCurrent is not null) _isRootCurrent = isRootCurrent;
        if (_active == active) return;
        _active = active;
        if (!active) RetireRead();
    }

    private bool IsOwnerCurrent => _active && (_isOwnerCurrent?.Invoke() ?? true);

    private void RetireRead()
    {
        _readRevision++; _draftRevision++; _occurrence = null; _review = null;
        var retired = _lifetime; _lifetime = new(); Loading = new();
        retired.Cancel(); retired.Dispose();
    }

    private string DisplayIdentity() => $"{localization.CurrentLanguage.CultureName}|{localization.CurrentCulture.Name}|{localization.CurrentCalendar}|{localization.CurrentDigits}|{localization.FormattingCultureName}";

    Task Presentation.IThemeAware.RefreshThemeAsync() => RefreshForNavigationAsync();

    /// <summary>Loads canonical defaults initially or after a known accepted command.</summary>
    public Task LoadAsync() => ReadAndPublishAsync(preserveDraft: false);

    /// <summary>Refreshes retained suggestions without replacing typed fields or interrupting native consent.</summary>
    internal async Task RefreshForNavigationAsync()
    {
        if (IsBusy || !IsOwnerCurrent) { return; }
        var revision = _readRevision;
        try { await ReadAndPublishAsync(preserveDraft: true); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // D-401: the failed cover owns retry feedback. A rejected read must never expose the old writable packet.
            if (_active && revision == _readRevision)
                Error = CommercialFailureMessage.OrFallback(exception, translator, "Occurrence_ReadFailed");
        }
    }

    [RelayCommand]
    private Task ReloadAsync() => RefreshForNavigationAsync();

    private Task ReadAndPublishAsync(bool preserveDraft)
    {
        if (!IsOwnerCurrent) return Task.CompletedTask;
        var owner = Loading; var revision = _readRevision; var token = _lifetime.Token;
        var query = (Schedule: _scheduleId, Original: _originalDate); var day = Today; var display = DisplayIdentity();
        bool Current() => IsOwnerCurrent && ReferenceEquals(owner, Loading) && revision == _readRevision
            && query == (_scheduleId, _originalDate) && day == Today && display == DisplayIdentity();
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("The original occurrence read retired.", token); }
        return owner.RunAsync(async () =>
        {
            RequireCurrent(); _draftRevision++;
            var keepInputs = preserveDraft && _publishedQuery is { } published && published == query;
            try
            {
                await LoadCoreAsync(query.Schedule, query.Original, day, keepInputs, RequireCurrent, token);
                RequireCurrent();
                if (_occurrence is { } current)
                {
                    var target = (current.Schedule.Kind, current.Schedule.AccountId, current.Schedule.ToAccountId, _currency);
                    if (keepInputs && _publishedFinancialTarget is { } previous && target != previous)
                        throw new InvalidOperationException("The original payment target changed; close and review it again.");
                    _publishedFinancialTarget = target; _publishedQuery = query;
                }
                Error = null; RequireCurrent();
            }
            catch (Exception exception)
            {
                if (Current())
                {
                    _occurrence = null; _review = null;
                    if (exception is not OutOfMemoryException)
                        Error = CommercialFailureMessage.OrFallback(exception, translator, "Occurrence_ReadFailed");
                }
                throw;
            }
        }, RequireCurrent);
    }

    private async Task LoadCoreAsync(Guid scheduleId, DateOnly originalDate, DateOnly today, bool keepInputs,
        Action requireCurrent, CancellationToken ct)
    {
        requireCurrent(); _occurrence = null; _review = null;
        if (scheduleId == Guid.Empty || !_hasOriginalDate) { NotFound = true; return; }
        var source = await plans.ReadOccurrenceSnapshotAsync(scheduleId, originalDate, today, path =>
        { requireCurrent(); _file ??= path; return _file == path; }, ct);
        requireCurrent();
        await plans.ValidateOccurrenceSnapshotAsync(source, ct); requireCurrent(); source.EnsureAccessCurrent();
        var schedule = source.Source.Inputs.Schedule; var loaded = source.Occurrence;
        void Publish(Action action)
        {
            requireCurrent(); source.EnsureAccessCurrent(); action();
            requireCurrent(); source.EnsureAccessCurrent();
        }
        Publish(() => NotFound = loaded is null);
        if (loaded is null || schedule is null) return;
        if (!source.Source.IsAvailable) throw new InvalidOperationException("The original occurrence references are unavailable.");

        var occurrence = loaded;
        var culture = localization.CurrentCulture;
        var accounts = source.Source.Inputs.Accounts.ToDictionary(a => a.Id);
        var categories = new CategoryLookup(source.Source.Inputs.Categories, translator);
        var text = new PlanText(translator, dates, localization);
        _currency = accounts.TryGetValue(schedule.AccountId, out var account) ? account.CurrencyCode : Currencies.Euro.Code;
        _money.SelectCurrency(_currency);

        Publish(() => Name = schedule.Name);
        Publish(() => CurrencyCode = _currency);
        Publish(() => DueText = translator.Format("Occurrence_DueOn", text.Date(occurrence.DueDate)));
        Publish(() => PlannedAmountText = text.Amount(occurrence.Amount, occurrence.AmountMode, _currency));
        Publish(() => StatusText = text.Status(occurrence.Status));
        Publish(() => IsOpen = occurrence.IsOpen);
        Publish(() => IsSettled = occurrence.Status == OccurrenceView.Settled);
        Publish(() => IsSkipped = occurrence.Status == OccurrenceView.Skipped);

        // Paying early or late keeps the real date (FIN-07); the default is today, or the due date when it is past.
        if (!keepInputs)
        {
            Publish(() => ActualDate = occurrence.DueDate < Today ? occurrence.DueDate : Today);
            var expected = occurrence.Paid > 0 ? occurrence.Outstanding : occurrence.Amount;
            _money.Initialize(expected, occurrence.State?.Amount);
            Publish(() => ActualAmountText = _money.Payment.Text);
            Publish(() => DueDate = occurrence.DueDate);
            Publish(() => OverrideAmountText = _money.Override.Text);
            Publish(() => OccurrenceNote = occurrence.State?.Note ?? string.Empty);
        }
        // Preserving a draft must not temporarily reseed DateField or its partial native input segments.
        // Re-translate requested feedback independently of the retained raw amount/date/note values.
        else
        {
            if (_paymentValidationRequested || _money.Payment.NeedsReview(_currency)) { ValidateAmount(OccurrenceInput.Payment); }
            if (_overrideValidationRequested || _money.Override.NeedsReview(_currency)) { ValidateAmount(OccurrenceInput.Override); }
        }
        RefreshMoneyCaptions();

        Publish(Candidates.Clear);
        Publish(() => SettledText = null);
        var presenter = new EntryPresenter(accounts, categories, translator, culture);
        if (IsOpen)
        {
            foreach (var candidate in PlanActions.LinkCandidates(occurrence, source.Candidates).Take(5))
            {
                Publish(() => Candidates.Add(presenter.Row(candidate) with { Subtitle = dates.Format(candidate.Date, DateFormatStyle.Long) }));
            }
        }
        else if (occurrence.State?.EntryId is { } entryId && source.Source.Inputs.Settlements.SingleOrDefault(e => e.Id == entryId) is { } entry)
        {
            Publish(() => SettledText = translator.Format("Occurrence_SettledWith", presenter.Amount(entry), dates.Format(entry.Date, DateFormatStyle.Long)));
        }

        Publish(() => HasCandidates = Candidates.Count > 0);

        Publish(Payments.Clear);
        foreach (var payment in source.Source.Inputs.Settlements.Where(e => e.OccurrenceDate == originalDate && e.IsPartialPayment).OrderBy(e => e.Date).ThenBy(e => e.CreatedAt))
        {
            Publish(() => Payments.Add(presenter.Row(payment) with { Subtitle = dates.Format(payment.Date, DateFormatStyle.Long) }));
        }

        Publish(() => HasPayments = Payments.Count > 0);
        Publish(() => PaidText = occurrence.Paid <= 0 ? null
            : occurrence.Outstanding is { } outstanding
                ? translator.Format("Occurrence_PaidSoFar", MoneyText.Format(occurrence.Paid, _currency, culture), MoneyText.Format(outstanding, _currency, culture))
                : translator.Format("Occurrence_PaidSoFarUnknown", MoneyText.Format(occurrence.Paid, _currency, culture)));
        requireCurrent(); source.EnsureAccessCurrent(); _review = source; _occurrence = occurrence;
    }

    private bool Current(OccurrenceReadSnapshot source, Occurrence occurrence, long revision, string display) =>
        IsOwnerCurrent && revision == _readRevision && ReferenceEquals(_review, source) && ReferenceEquals(_occurrence, occurrence)
        && Loading.IsReady && source.Source.Request.Today == Today && display == DisplayIdentity();

    // D-136: one visible busy gate spans every write and confirmation. Rejected writes never reload the draft.
    [RelayCommand]
    private Task PayPartAsync() => PayAsync(partial: true);

    [RelayCommand]
    private Task ConfirmAsync() => PayAsync(partial: false);

    /// <summary>Validates the current input before sending its captured actual amount/date to the writer.</summary>
    private async Task PayAsync(bool partial)
    {
        if (_review is not { } source || _occurrence is not { } occurrence || !IsOwnerCurrent || IsBusy || !Loading.IsReady) { return; }
        Error = null;
        if (ValidatePayment() is not { } amount) { return; }
        if (_file is not { } file) return;
        var revision = _draftRevision; var read = _readRevision; var display = DisplayIdentity(); var root = Shell.Current; var receiptRoot = _isRootCurrent; var date = ActualDate; var currency = _currency;
        await RunCommandAsync(() => commands.PayAsync(occurrence, amount, date, partial,
            error => Error = error, () => CompleteCommandAsync(navigate: !partial), file, currency,
            () => revision == _draftRevision && Current(source, occurrence, read, display)
                && !_money.Payment.NeedsReview(currency), source, accepted => accepted ? receiptRoot?.Invoke() ?? ReferenceEquals(Shell.Current, root) : IsOwnerCurrent && read == _readRevision,
            exception => RejectCommand(exception, read)));
    }

    [RelayCommand]
    private Task OpenPaymentAsync(EntryRow row) => !IsOwnerCurrent || IsBusy || !Loading.IsReady || !Payments.Contains(row)
        ? Task.CompletedTask : Shell.Current.GoToAsync(AppShell.EntryDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });

    [RelayCommand]
    private Task LinkAsync(EntryRow row)
    {
        if (_review is not { } source || _occurrence is not { IsOpen: true } occurrence || !Candidates.Contains(row)) return Task.CompletedTask;
        var read = _readRevision; var display = DisplayIdentity(); var root = Shell.Current; var receiptRoot = _isRootCurrent;
        return RunCommandAsync(() => commands.LinkAsync(occurrence, row.Id, row.Title, row.AmountText,
            () => CompleteCommandAsync(navigate: true), source, () => Current(source, occurrence, read, display),
            accepted => accepted ? receiptRoot?.Invoke() ?? ReferenceEquals(Shell.Current, root) : IsOwnerCurrent && read == _readRevision,
            exception => RejectCommand(exception, read)));
    }

    [RelayCommand]
    private Task SkipAsync()
    {
        if (_review is not { } source || _occurrence is not { IsOpen: true } occurrence) return Task.CompletedTask;
        var read = _readRevision; var display = DisplayIdentity(); var root = Shell.Current; var receiptRoot = _isRootCurrent;
        return RunCommandAsync(() => commands.SkipAsync(occurrence, () => CompleteCommandAsync(navigate: true),
            source, () => Current(source, occurrence, read, display), accepted => accepted ? receiptRoot?.Invoke() ?? ReferenceEquals(Shell.Current, root) : IsOwnerCurrent && read == _readRevision,
            exception => RejectCommand(exception, read)));
    }

    [RelayCommand]
    private void ToggleChange() { if (!IsBusy && Loading.IsReady) { ShowChange = !ShowChange; } }

    [RelayCommand]
    private async Task SaveChangeAsync()
    {
        if (_review is not { } source || _occurrence is not { } occurrence || !IsOwnerCurrent || IsBusy || !Loading.IsReady) { return; }
        Error = null;
        _overrideValidationRequested = true;
        var validation = ValidateAmount(OccurrenceInput.Override);
        if (validation.ErrorKey is not null)
        {
            ValidationFailed?.Invoke(OccurrenceInput.Override);
            return;
        }
        if (_file is not { } file) return;
        var revision = _draftRevision; var read = _readRevision; var display = DisplayIdentity(); var root = Shell.Current; var receiptRoot = _isRootCurrent; var due = DueDate; var note = OccurrenceNote; var currency = _currency;
        await RunCommandAsync(() => commands.ChangeAsync(occurrence, due, validation.Amount, note,
            () => CompleteCommandAsync(navigate: false, hideChange: true), file, currency,
            () => revision == _draftRevision && Current(source, occurrence, read, display)
                && !_money.Override.NeedsReview(currency), source, accepted => accepted ? receiptRoot?.Invoke() ?? ReferenceEquals(Shell.Current, root) : IsOwnerCurrent && read == _readRevision,
            exception => RejectCommand(exception, read)));
    }

    [RelayCommand]
    private Task UndoAsync()
    {
        if (_review is not { } source || _occurrence is not { } occurrence) return Task.CompletedTask;
        var read = _readRevision; var display = DisplayIdentity(); var root = Shell.Current; var receiptRoot = _isRootCurrent;
        return RunCommandAsync(() => commands.UndoAsync(occurrence, () => CompleteCommandAsync(navigate: false),
            source, () => Current(source, occurrence, read, display), accepted => accepted ? receiptRoot?.Invoke() ?? ReferenceEquals(Shell.Current, root) : IsOwnerCurrent && read == _readRevision,
            exception => RejectCommand(exception, read)));
    }

    private void RejectCommand(Exception exception, long revision)
    {
        if (!IsOwnerCurrent || revision != _readRevision || !Loading.HasLoaded || Loading.IsLoading) return;
        _occurrence = null; _review = null;
        Error = CommercialFailureMessage.OrFallback(exception, translator, "Occurrence_ReadFailed");
        Loading.CoverFailedPresentation();
    }

    /// <summary>Keeps all commands mutually exclusive, including while the native consent/failure dialog is open.</summary>
    private async Task RunCommandAsync(Func<Task<bool>> command)
    {
        if (!IsOwnerCurrent || IsBusy || !Loading.IsReady) { return; }
        IsBusy = true;
        try { await command(); }
        finally { IsBusy = false; }
    }

    /// <summary>Retires the previous payment identity before any fallible post-commit navigation or reload.</summary>
    private async Task CompleteCommandAsync(bool navigate, bool hideChange = false)
    {
        _occurrence = null; _review = null;
        if (!IsOwnerCurrent) return;
        if (navigate) { await Shell.Current.GoToAsync(".."); }
        else
        {
            await LoadAsync();
            if (hideChange) { ShowChange = false; }
        }
    }

    [RelayCommand]
    private Task OpenPlanAsync() => !IsOwnerCurrent || IsBusy || !Loading.IsReady ? Task.CompletedTask
        : Shell.Current.GoToAsync(AppShell.PlanDetailRoute, new Dictionary<string, object> { ["id"] = _scheduleId });
}
