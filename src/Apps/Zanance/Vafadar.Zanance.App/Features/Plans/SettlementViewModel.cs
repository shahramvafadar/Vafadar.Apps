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

    /// <summary>Explains the original bill's numeric format after a display-only change.</summary>
    public string? InputFormatNote => _actual.OriginalExample(localization.CurrentCulture) is { } example
        ? translator.Format("Plan_OriginalInputFormat", example) : null;

    /// <summary>Gets the original coherent loading/failed/retry lifetime; no half-read form is interactive.</summary>
    public SnapshotLoadState Loading { get; } = new();
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
    public partial DateOnly From { get; set; } = DateOnly.FromDateTime(time.GetLocalNow().DateTime).AddYears(-1).AddDays(1);

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
        if (HasCommitted || IsBusy) return;
        if (_id != Guid.Empty && _id != id) throw new InvalidOperationException("A retained bill form cannot target another plan.");
        _id = id; CanEdit = false; Error = null;
        try
        {
            await Loading.RunAsync(async () =>
            {
                var draft = await Task.Run(() => store.ReadAdvanceSettlementDraftAsync(id));
                _plan = draft.Plan; _entries = draft.Entries; _file = draft.DatabasePath; _currency = draft.Account.CurrencyCode;
                if (string.IsNullOrWhiteSpace(ActualText)) _actual.Initialize(ActualText, _currency, localization.CurrentCulture);
                PlanName = _plan.Name;
                Update(); _snapshot ??= Snapshot();
            }, () => { });
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Error = CommercialFailureMessage.OrFallback(exception, translator, "Settlement_ReadFailed", draftKept: true); }
        finally { RefreshInteraction(); }
    }

    /// <summary>Retries only the original failed/read target; accepted bills are never reloaded as new drafts.</summary>
    [RelayCommand]
    private Task ReloadAsync() => LoadAsync(_id);

    Task IThemeAware.RefreshThemeAsync()
    {
        if (Loading.IsReady) Update();
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

    private void Update()
    {
        if (HasCommitted) { CanSave = false; return; }
        if (_plan is null || From == default || To == default)
        {
            return;
        }

        var culture = localization.CurrentCulture;
        UnitNote = DisplayUnitNote.ForInput(translator, _actual, _currency);
        OnPropertyChanged(nameof(InputFormatNote));
        var validation = SettlementDraftValidation.Validate(From, To, ActualText, _currency, culture, _actual.Context);
        Error = null;
        AmountError = validation.AmountErrorKey is { } amountKey && (_validationRequested || !string.IsNullOrWhiteSpace(ActualText))
            ? translator[amountKey] : null;
        if (_actual.NeedsReview(_currency)) AmountError = translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(_currency));
        PeriodError = validation.PeriodErrorKey is { } periodKey ? translator[periodKey] : null;
        // D-102: an inverted period must not look like a legitimate period with no payments.
        _result = validation.PeriodErrorKey is null ? AdvanceSettlement.Compute(_plan, _entries, From, To, validation.Actual ?? 0) : null;
        if (_result is null)
        {
            AdvancesText = null;
            ResultText = null;
            CanSave = false;
            return;
        }

        AdvancesText = translator.Format("Settlement_Advances", _result.Advances.Count, MoneyText.Format(_result.Paid, _currency, culture));
        if (_result.Advances.Count == 0) { PeriodError = translator["Settlement_NoAdvances"]; }
        if (validation.Actual is null || PeriodError is not null)
        {
            ResultText = null;
            CanSave = false;
            return;
        }

        ResultText = _result.Difference switch
        {
            > 0 => translator.Format("Settlement_Extra", MoneyText.Format(_result.Difference, _currency, culture)),
            < 0 => translator.Format("Settlement_Back", MoneyText.Format(-_result.Difference, _currency, culture)),
            _ => translator["Settlement_Even"],
        };
        CanSave = _result.Difference != 0 && _result.Advances.Count > 0 && (_result.Difference > 0 || -_result.Difference <= _result.Paid);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;
        if (HasCommitted) { await LeaveCommittedAsync(); return; }
        if (!Loading.IsReady || _plan is null || _file is null) return;
        _validationRequested = true; Update();
        if (_result is null || !CanSave) { ValidationFailed?.Invoke(this, EventArgs.Empty); return; }
        IsBusy = true; RefreshInteraction(); Error = null;
        var version = _draftVersion; var plan = _plan; var result = _result; var from = From; var to = To;
        try
        {
            var title = translator.Format(result.Difference > 0 ? "Settlement_ExtraTitle" : "Settlement_BackTitle", plan.Name);
            SaveResult saved; var notificationFailed = false;
            try
            {
                saved = await store.SaveAdvanceSettlementAsync(plan, from, to, result, title,
                    expectedPath: _file, expectedCurrency: _currency,
                    isCurrent: () => version == _draftVersion && !HasCommitted && !_actual.NeedsReview(_currency));
            }
            catch (CommittedWriteNotificationException<SaveResult> accepted)
            { saved = accepted.Result; notificationFailed = true; }
            if (!saved.Succeeded)
            { Error = string.Join(Environment.NewLine, saved.Errors.Distinct().Select(e => translator[$"LedgerError_{e}"])); return; }

            // Acceptance retires this form before any notification/error display or native navigation can fail.
            HasCommitted = true; CanSave = false; _snapshot = Snapshot(); SaveFeedback = translator["Settlement_Recorded"];
            RefreshInteraction();
            if (notificationFailed) { Error = translator["Settlement_RecordedFollowUpFailed"]; return; }
            await ReturnAfterCommitAsync();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Error = HasCommitted ? translator["Settlement_RecordedFollowUpFailed"]
            : CommercialFailureMessage.OrFallback(exception, translator, "Common_SaveFailed", draftKept: true); }
        finally { IsBusy = false; RefreshInteraction(); }
    }

    private void RefreshInteraction() => CanEdit = Loading.IsReady && !IsBusy && !HasCommitted;

    /// <summary>Retries only returning from an already accepted form, keeping the execution guard across native effects.</summary>
    private async Task LeaveCommittedAsync()
    {
        if (IsBusy) return;
        IsBusy = true; RefreshInteraction();
        try { await ReturnAfterCommitAsync(); }
        finally { IsBusy = false; RefreshInteraction(); }
    }

    private async Task ReturnAfterCommitAsync()
    {
        try { await interaction.NavigateAsync(".."); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Error = translator["Editor_SavedReturnFailed"]; }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (IsBusy) return;
        if (HasCommitted) { await LeaveCommittedAsync(); return; }
        IsBusy = true; RefreshInteraction();
        try
        {
            if (!IsDirty || await interaction.ConfirmAsync(translator["Common_DiscardTitle"], translator["Common_DiscardMessage"],
                translator["Common_Discard"], translator["Common_KeepEditing"])) await interaction.NavigateAsync("..");
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
