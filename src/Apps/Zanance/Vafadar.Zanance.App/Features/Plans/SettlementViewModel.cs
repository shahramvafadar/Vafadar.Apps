using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Plans;

/// <summary>
/// Final settlement of advance payments (F2-CON-04): the actual bill is compared with the advances of a period; only
/// the extra payment is recorded as an expense, or the money back as refunds of the advances.
/// </summary>
public sealed partial class SettlementViewModel(ZananceStore store, Translator translator, ILocalizationService localization, TimeProvider time, IAppInteraction interaction)
    : ViewModelBase, Presentation.IUnsavedChanges, IThemeAware
{
    private Schedule? _plan;
    private List<LedgerEntry> _entries = [];
    private string _currency = Currencies.Euro.Code;
    private SettlementResult? _result;
    private bool _validationRequested;
    private Guid _id;
    private string? _file;
    private long _draftVersion;
    private readonly MoneyDraftInput _actual = new();
    private AdvanceSettlementDraft? _review;
    private long _readRevision;
    private bool _active = true;
    private bool _closed;
    private Func<bool>? _isOwnerCurrent;
    private CancellationTokenSource _lifetime = new();
    private DateOnly _reviewDay;
    private (Guid Account, string Currency)? _originalTarget;
    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);
    // D-939: preserve the inclusive trailing year; a supported editor day must not
    // send an earlier default into DateField, whose original policy substitutes today.
    private static DateOnly InitialPeriodStart(DateOnly today)
    {
        var from = today.Year == 1 ? DateOnly.MinValue : today.AddYears(-1).AddDays(1);
        return today >= CalendarDates.MinDate && from < CalendarDates.MinDate ? CalendarDates.MinDate : from;
    }
    private bool IsOwnerCurrent => !_closed && _active && (_isOwnerCurrent?.Invoke() ?? true);
    private string DisplayIdentity() => $"{localization.CurrentLanguage.CultureName}|{localization.CurrentCulture.Name}|{localization.CurrentCalendar}|{localization.CurrentDigits}|{localization.FormattingCultureName}";

    /// <summary>Retires original read/confirmation/writer authority while retaining raw unsaved bill input.</summary>
    public void SetActive(bool active, Func<bool>? isCurrent = null)
    {
        if (active && isCurrent is not null) _isOwnerCurrent = isCurrent;
        if (_active == active || _closed) return;
        _active = active;
        if (!active) RetireRead();
    }

    private void RetireRead()
    {
        _readRevision++; _draftVersion++; _review = null; _plan = null; _entries = []; _result = null;
        CanEdit = false; CanSave = false; Loading = new();
        var retired = _lifetime; _lifetime = new(); retired.Cancel(); retired.Dispose();
    }

    /// <summary>Captures an original native route without beginning work before actual page appearance.</summary>
    internal void SetOriginalQuery(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("An original bill plan is required.", nameof(id));
        if (_id != Guid.Empty && _id != id) throw new InvalidOperationException("A retained bill form cannot target another plan.");
        _id = id;
    }

    /// <summary>Shares the actual appearance load; an already prepared native caller does not replay its read.</summary>
    internal Task RefreshForNavigationAsync() => Loading.IsReady && _review is not null ? Task.CompletedTask : LoadAsync(_id);

    /// <summary>Explains the original bill's numeric format after a display-only change.</summary>
    public string? InputFormatNote => _actual.OriginalExample(localization.CurrentCulture) is { } example
        ? translator.Format("Plan_OriginalInputFormat", example) : null;

    /// <summary>Gets the original coherent loading/failed/retry lifetime; no half-read form is interactive.</summary>
    [ObservableProperty] public partial SnapshotLoadState Loading { get; private set; } = new();
    /// <summary>Gets whether this exact form already committed, even if notification or navigation failed.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SaveText)), NotifyPropertyChangedFor(nameof(CancelText))]
    public partial bool HasCommitted { get; private set; }
    /// <summary>Gets whether original unsaved bill input can be edited.</summary>
    [ObservableProperty] public partial bool CanEdit { get; private set; }
    /// <summary>Gets the terminal accepted result independently of preview and follow-through errors.</summary>
    [ObservableProperty] public partial string? SaveFeedback { get; private set; }
    /// <summary>Gets Back after acceptance; retrying return can never post the bill again.</summary>
    public string SaveText => translator[HasCommitted ? "Common_Back" : "Settlement_Save"];
    /// <summary>Gets the unchanged unsaved Cancel action or a truthful accepted Back caption.</summary>
    public string CancelText => translator[HasCommitted ? "Common_Back" : "Common_Cancel"];


    /// <summary>Requests visibility of the first affected field after an invalid Save attempt.</summary>
    public event EventHandler? ValidationFailed;

    [ObservableProperty]
    public partial string? PlanName { get; set; }

    [ObservableProperty]
    public partial DateOnly From { get; set; } = InitialPeriodStart(DateOnly.FromDateTime(time.GetLocalNow().DateTime));

    [ObservableProperty]
    public partial DateOnly To { get; set; } = DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    [ObservableProperty]
    public partial string ActualText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? AdvancesText { get; set; }

    // Amounts are typed in the currency's display unit when one is defined (FX-07).
    [ObservableProperty]
    public partial string? UnitNote { get; set; }

    [ObservableProperty]
    public partial string? ResultText { get; set; }

    [ObservableProperty]
    public partial string? Error { get; set; }

    /// <summary>Gets or sets the explanation next to the inclusive period fields.</summary>
    [ObservableProperty]
    public partial string? PeriodError { get; set; }

    /// <summary>Gets or sets the explanation next to the actual bill input.</summary>
    [ObservableProperty]
    public partial string? AmountError { get; set; }

    [ObservableProperty]
    public partial bool CanSave { get; set; }

    /// <summary>Loads only this original form target, preserving drafts on explicit retry.</summary>
    public async Task LoadAsync(Guid id)
    {
        if (HasCommitted || IsBusy || !IsOwnerCurrent) return;
        SetOriginalQuery(id);
        var owner = Loading; var revision = _readRevision; var token = _lifetime.Token;
        var day = Today; var display = DisplayIdentity();
        bool Current() => IsOwnerCurrent && ReferenceEquals(Loading, owner) && revision == _readRevision
            && _id == id && Today == day && DisplayIdentity() == display;
        void RequireCurrent() { token.ThrowIfCancellationRequested(); if (!Current()) throw new OperationCanceledException("The original bill read retired.", token); }
        CanEdit = false; CanSave = false; Error = null;
        try
        {
            await owner.RunAsync(async () =>
            {
                RequireCurrent(); _review = null; _plan = null; _entries = []; _result = null;
                var draft = await store.ReadAdvanceSettlementDraftAsync(id, token, path =>
                { RequireCurrent(); _file ??= path; return path == _file; });
                RequireCurrent(); await store.ValidateAdvanceSettlementDraftAsync(draft, token); RequireCurrent(); draft.EnsureAccessCurrent();
                var target = (draft.Plan.AccountId, draft.Account.CurrencyCode);
                if (_originalTarget is { } originalTarget && originalTarget != target)
                    throw new InvalidOperationException("The original bill account or currency changed; close and review it again.");
                _plan = draft.Plan; _entries = draft.Entries; _currency = draft.Account.CurrencyCode;
                if (string.IsNullOrWhiteSpace(ActualText)) _actual.Initialize(ActualText, _currency, localization.CurrentCulture);
                PlanName = _plan.Name; RequireCurrent(); draft.EnsureAccessCurrent();
                Update(RequireCurrent); RequireCurrent(); draft.EnsureAccessCurrent(); _snapshot ??= Snapshot();
                _reviewDay = day; _originalTarget ??= target; _review = draft;
            }, RequireCurrent);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (Current())
            {
                _review = null; CanSave = false;
                Error = CommercialFailureMessage.OrFallback(exception, translator, "Settlement_ReadFailed", draftKept: true);
            }
        }
        finally { if (Current()) RefreshInteraction(); }
    }

    /// <summary>Retries only the original failed/read target; accepted bills are never reloaded as new drafts.</summary>
    [RelayCommand]
    private Task ReloadAsync() => LoadAsync(_id);

    Task IThemeAware.RefreshThemeAsync()
    {
        if (IsOwnerCurrent && Loading.IsReady) Update();
        OnPropertyChanged(nameof(SaveText)); OnPropertyChanged(nameof(CancelText));
        if (HasCommitted) SaveFeedback = translator["Settlement_Recorded"];
        return Task.CompletedTask;
    }

    partial void OnFromChanged(DateOnly value) { _draftVersion++; Update(); }

    partial void OnToChanged(DateOnly value) { _draftVersion++; Update(); }

    partial void OnActualTextChanged(string value)
    {
        _actual.ChangeText(value, _currency, localization.CurrentCulture);
        _draftVersion++; Update();
    }

    private void Update(Action? requireCurrent = null)
    {
        void Publish(Action action) { requireCurrent?.Invoke(); action(); requireCurrent?.Invoke(); }
        requireCurrent?.Invoke();
        if (HasCommitted) { CanSave = false; return; }
        if (_plan is null)
        {
            return;
        }

        var culture = localization.CurrentCulture;
        Publish(() => UnitNote = DisplayUnitNote.ForInput(translator, _actual, _currency));
        Publish(() => OnPropertyChanged(nameof(InputFormatNote)));
        var validation = SettlementDraftValidation.Validate(From, To, ActualText, _currency, culture, _actual.Context);
        Publish(() => Error = null);
        Publish(() => AmountError = validation.AmountErrorKey is { } amountKey && (_validationRequested || !string.IsNullOrWhiteSpace(ActualText))
            ? translator[amountKey] : null);
        if (_actual.NeedsReview(_currency)) Publish(() => AmountError = translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(_currency)));
        Publish(() => PeriodError = validation.PeriodErrorKey is { } periodKey ? translator[periodKey] : null);
        // D-102: an inverted period must not look like a legitimate period with no payments.
        _result = validation.PeriodErrorKey is null ? AdvanceSettlement.Compute(_plan, _entries, From, To, validation.Actual ?? 0) : null;
        if (_result is null)
        {
            Publish(() => AdvancesText = null);
            Publish(() => ResultText = null);
            Publish(() => CanSave = false);
            return;
        }

        Publish(() => AdvancesText = translator.Format("Settlement_Advances", _result.Advances.Count, MoneyText.Format(_result.Paid, _currency, culture)));
        if (_result.Advances.Count == 0) { Publish(() => PeriodError = translator["Settlement_NoAdvances"]); }
        if (validation.Actual is null || PeriodError is not null)
        {
            Publish(() => ResultText = null);
            Publish(() => CanSave = false);
            return;
        }

        Publish(() => ResultText = _result.Difference switch
        {
            > 0 => translator.Format("Settlement_Extra", MoneyText.Format(_result.Difference, _currency, culture)),
            < 0 => translator.Format("Settlement_Back", MoneyText.Format(-_result.Difference, _currency, culture)),
            _ => translator["Settlement_Even"],
        });
        Publish(() => CanSave = _result.Difference != 0 && _result.Advances.Count > 0 && (_result.Difference > 0 || -_result.Difference <= _result.Paid));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy || !IsOwnerCurrent) return;
        if (HasCommitted) { await LeaveCommittedAsync(); return; }
        if (!Loading.IsReady || _plan is null || _file is null || _review is not { } source) return;
        var attemptedVersion = _draftVersion; var attemptedRead = _readRevision; var attemptedDisplay = DisplayIdentity();
        _validationRequested = true; Update();
        if (!IsOwnerCurrent || attemptedVersion != _draftVersion || attemptedRead != _readRevision || attemptedDisplay != DisplayIdentity() || !ReferenceEquals(_review, source)) return;
        if (_result is null || !CanSave) { ValidationFailed?.Invoke(this, EventArgs.Empty); return; }
        // D-938: disabling the form can publish synchronous input/owner callbacks too.
        // Keep the attempted values from before those callbacks, never a newly edited bill.
        var version = attemptedVersion; var read = attemptedRead; var display = attemptedDisplay;
        var plan = _plan; var result = _result; var from = From; var to = To;
        var file = source.DatabasePath; var currency = source.Account.CurrencyCode;
        bool Current() => IsOwnerCurrent && Loading.IsReady && read == _readRevision && version == _draftVersion && ReferenceEquals(_review, source)
            && _reviewDay == Today && display == DisplayIdentity() && !HasCommitted && !_actual.NeedsReview(currency);
        IsBusy = true; RefreshInteraction(); Error = null;
        try
        {
            if (!Current()) return;
            var title = translator.Format(result.Difference > 0 ? "Settlement_ExtraTitle" : "Settlement_BackTitle", plan.Name);
            SaveResult saved; var notificationFailed = false;
            try
            {
                saved = await store.SaveAdvanceSettlementAsync(plan, from, to, result, title,
                    expectedPath: file, expectedCurrency: currency,
                    isCurrent: Current, original: source);
            }
            catch (CommittedWriteNotificationException<SaveResult> accepted)
            { saved = accepted.Result; notificationFailed = true; }
            if (!saved.Succeeded)
            { if (IsOwnerCurrent) Error = string.Join(Environment.NewLine, saved.Errors.Distinct().Select(e => translator[$"LedgerError_{e}"])); return; }

            // Acceptance retires this form before any notification/error display or native navigation can fail.
            HasCommitted = true; _review = null; CanSave = false; _snapshot = Snapshot(); SaveFeedback = translator["Settlement_Recorded"];
            RefreshInteraction();
            if (!IsOwnerCurrent) return;
            if (notificationFailed) { Error = translator["Settlement_RecordedFollowUpFailed"]; return; }
            await ReturnAfterCommitAsync();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (IsOwnerCurrent)
            {
                if (!HasCommitted) { _review = null; CanSave = false; if (Loading.IsReady) Loading.CoverFailedPresentation(); }
                Error = HasCommitted ? translator["Settlement_RecordedFollowUpFailed"]
                    : CommercialFailureMessage.OrFallback(exception, translator, "Common_SaveFailed", draftKept: true);
            }
        }
        finally { IsBusy = false; RefreshInteraction(); }
    }

    private void RefreshInteraction() => CanEdit = IsOwnerCurrent && Loading.IsReady && !IsBusy && !HasCommitted && _review is not null;

    /// <summary>Retries only returning from an already accepted form, keeping the execution guard across native effects.</summary>
    private async Task LeaveCommittedAsync()
    {
        if (IsBusy || !IsOwnerCurrent) return;
        IsBusy = true; RefreshInteraction();
        try { await ReturnAfterCommitAsync(); }
        finally { IsBusy = false; RefreshInteraction(); }
    }

    private async Task ReturnAfterCommitAsync()
    {
        if (!IsOwnerCurrent) return;
        try { await interaction.NavigateAsync(".."); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { if (IsOwnerCurrent) Error = translator["Editor_SavedReturnFailed"]; }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (IsBusy || !IsOwnerCurrent) return;
        if (HasCommitted) { await LeaveCommittedAsync(); return; }
        var revision = _readRevision; var draft = _draftVersion;
        IsBusy = true; RefreshInteraction();
        try
        {
            if (!IsOwnerCurrent || revision != _readRevision || draft != _draftVersion) return;
            var discard = !IsDirty || await interaction.ConfirmAsync(translator["Common_DiscardTitle"], translator["Common_DiscardMessage"],
                translator["Common_Discard"], translator["Common_KeepEditing"]);
            if (!discard || !IsOwnerCurrent || revision != _readRevision || draft != _draftVersion) return;
            // D-938: end the old read before fallible navigation, so it cannot reactivate a departed form.
            RetireRead(); _closed = true;
            try { await interaction.NavigateAsync(".."); }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                if (_active && (_isOwnerCurrent?.Invoke() ?? true))
                {
                    _closed = false;
                    try { await Loading.RunAsync(() => Task.FromException(exception), () => { }); }
                    catch (Exception failure) when (failure is not OutOfMemoryException)
                    { Error = CommercialFailureMessage.OrFallback(failure, translator, "Settlement_ReadFailed", draftKept: true); }
                }
            }
        }
        finally { IsBusy = false; RefreshInteraction(); }
    }

    // The input as it was loaded or saved; leaving with a change asks first (CR12).
    private string? _snapshot;

    /// <inheritdoc />
    public bool IsDirty => !HasCommitted && _snapshot is not null && Snapshot() != _snapshot;

    private string Snapshot() => DraftFingerprint.Create(
        From, To, ActualText);
}
