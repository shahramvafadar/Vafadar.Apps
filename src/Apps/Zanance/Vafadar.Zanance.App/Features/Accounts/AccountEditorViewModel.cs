using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.App.Features.Entries;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>Original explicit account/debt form with native effects separated from committed financial writes.</summary>
public sealed partial class AccountEditorViewModel : ViewModelBase
{
    private readonly ZananceStore _store;
    private readonly PlanStore _plans;
    private readonly TimeProvider _time;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private Account _account;
    private string _snapshot = string.Empty;
    private readonly IAppInteraction _interaction;
    private bool _committed;
    private EditorWorkReview? _work;
    private bool _reviewingWork;
    private bool _workFailed;
    private bool _workRetired;
    private bool _loading = true;
    private IOriginalAccountHandoff? _originalAccountHandoff;
    private bool _presented = true;
    private long _presentationRevision;
    /// <summary>Retires this nested child's pending callbacks on departure without discarding an uncommitted raw form.</summary>
    internal void SetPresentationActive(bool active)
    {
        if (_presented != active) { _presented = active; _presentationRevision++; }
        _localization.Changed -= OnDisplayChanged;
        if (active) _localization.Changed += OnDisplayChanged;
        Form.RefreshTexts();
        RefreshAccess();
    }
    private void OnDisplayChanged(object? sender, EventArgs args) => Form.RefreshTexts();
    private bool OwnsChild(long revision) => _presented && revision == _presentationRevision
        && (_originalAccountHandoff is null || _originalAccountHandoff.IsOwned);

    /// <summary>Creates the original form without starting database or native work.</summary>
    public AccountEditorViewModel(ZananceStore store, PlanStore plans, Translator translator, ILocalizationService localization, TimeProvider time, IAppInteraction interaction)
    {
        _interaction = interaction;
        PropertyChanged += (_, e) => { if (e.PropertyName == nameof(IsBusy)) RefreshAccess(); };
        _store = store;
        _plans = plans;
        _time = time;
        _translator = translator;
        _localization = localization;
        Form = new AccountFormModel(translator, time, localization);
        _account = new Account { Name = string.Empty, CurrencyCode = Form.CurrencyCode, OpeningDate = Form.OpeningDate };
        Title = translator["Account_NewTitle"];
        Form.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AccountFormModel.MoneyContextsReady)) RefreshAccess();
            if (e.PropertyName == nameof(AccountFormModel.TypeIndex))
            {
                UpdateCanBeDefault();
                Title = _translator[Form.IsDebtType ? (IsExisting ? "Debt_EditTitle" : "Debt_NewTitle") : (IsExisting ? "Account_EditTitle" : "Account_NewTitle")];
            }
        };
        UpdateCanBeDefault();
    }

    public AccountFormModel Form { get; }

    /// <summary>Gets whether user input can change this loaded, not-yet-committed form.</summary>
    public bool CanInteract => !_loading && !IsBusy && !_committed && OwnsChild(_presentationRevision);
    /// <summary>Gets whether a financial Save can run; a committed account cannot be resubmitted.</summary>
    public bool CanSave => CanInteract && Form.MoneyContextsReady && !_reviewingWork && !_workFailed && !_workRetired && _work is not null && WorkRestriction is null;
    /// <summary>Gets whether a pending writer must finish before leaving this form.</summary>
    public bool CanLeave => !IsBusy;
    /// <summary>Gets the actual Close/Cancel caption after the commit boundary.</summary>
    public string CloseText => _translator[_committed ? "Common_Close" : "Common_Cancel"];
    private Core.Commerce.WorkRestriction? WorkRestriction => _work?.AccountRestriction(_account.Id, IsArchived);
    /// <summary>Gets a captured reason before Save; no financial field validation or write occurs here.</summary>
    public string? WorkText => _workFailed ? _translator["Editor_WorkReadFailed"]
        : _workRetired ? _translator["Editor_WorkRetired"]
        : WorkRestriction is { } restriction ? Presentation.CommercialFailureMessage.Describe(restriction, _translator) : null;
    /// <summary>Gets whether the original availability can be explicitly refreshed without changing any draft.</summary>
    public bool CanReviewWork => !_loading && !IsBusy && !_committed && !_reviewingWork;
    private async Task ReadWorkAsync()
    {
        _reviewingWork = true; RefreshAccess();
        try
        {
            _originalAccountHandoff?.EnsureCurrent();
            var next = _originalAccountHandoff is { } child ? await child.ReadWorkAsync(_store) : await _store.ReadEditorWorkAsync();
            _originalAccountHandoff?.EnsureCurrent();
            next.EnsureCurrent(); _work = next; _workFailed = false; _workRetired = false;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { _work = null; _workFailed = true; }
        finally { _reviewingWork = false; RefreshAccess(); }
    }
    [RelayCommand(CanExecute = nameof(CanReviewWork))]
    private Task ReviewWorkAsync() => ReadWorkAsync();

    private void RefreshAccess()
    {
        OnPropertyChanged(nameof(WorkText)); OnPropertyChanged(nameof(CanReviewWork)); ReviewWorkCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanLeave)); OnPropertyChanged(nameof(CanInteract)); OnPropertyChanged(nameof(CanSave)); OnPropertyChanged(nameof(CloseText));
        SaveCommand.NotifyCanExecuteChanged(); ToggleArchiveCommand.NotifyCanExecuteChanged(); CancelCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial bool IsExisting { get; set; }

    [ObservableProperty]
    public partial bool IsArchived { get; set; }

    [ObservableProperty]
    public partial bool IsDefault { get; set; }

    /// <summary>
    /// Gets a value indicating whether the account can be the default of new entries: a money account that is not archived
    /// (<see cref="EntryAccountContract.IsValidDefault"/>). A loan, money lent or an asset is never offered.
    /// </summary>
    [ObservableProperty]
    public partial bool CanBeDefault { get; set; }

    partial void OnIsArchivedChanged(bool value) => UpdateCanBeDefault();

    private void UpdateCanBeDefault() => CanBeDefault = Form.Type.CanBeDefault() && !IsArchived;

    [ObservableProperty]
    public partial string? SaveError { get; set; }

    /// <summary>Gets a value indicating whether the user changed something.</summary>
    public bool IsDirty => Snapshot() != _snapshot;

    // The form and the default switch: switching the default and cancelling asks like any other change.
    private string Snapshot() => $"{Form.Snapshot()}|{IsDefault}";

    /// <summary>Loads the actual draft through the native page adapter without a write or route transition.</summary>
    public async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        _presentationRevision++;
        var originalVisit = query.TryGetValue("originalAccountHandoff", out var handoff);
        if (originalVisit || query.TryGetValue("entryAccountHandoff", out handoff))
        {
            if (handoff is not IOriginalAccountHandoff visit || query.ContainsKey("id")
                || originalVisit && query.ContainsKey("entryAccountHandoff"))
                throw new ArgumentException("The original nested account route is invalid.");
            visit.EnsureCurrent(); _originalAccountHandoff = visit;
        }
        var settings = _originalAccountHandoff?.Settings ?? (_originalAccountHandoff is null ? await _store.GetSettingsAsync() : new ZananceSettings());
        Form.ShowAdvanced = settings.Shows(Feature.AccountOptions);

        if (query.TryGetValue("id", out var value) && value is Guid id
            && (await _store.GetAccountsAsync()).FirstOrDefault(a => a.Id == id) is { } account)
        {
            _account = account;
            Form.Load(account, _localization.CurrentCulture, await _store.GetCurrencyLockAsync(id));
            IsExisting = true;
            IsArchived = account.IsArchived;
            IsDefault = settings.DefaultAccountId == id;
            Title = _translator["Account_EditTitle"];
        }
        else
        {
            Form.CurrencyCode = _originalAccountHandoff?.SuggestedCurrencyCode ?? settings.DefaultCurrencyCode;
            if (query.TryGetValue("debt", out var debt) && debt is true)
            {
                Form.TypeIndex = (int)AccountType.Loan;
            }
            else if (debt is AccountType type && type.IsDebt())
            {
                Form.TypeIndex = (int)type;
            }
        }

        Title = _translator[Form.IsDebtType ? (IsExisting ? "Debt_EditTitle" : "Debt_NewTitle") : (IsExisting ? "Account_EditTitle" : "Account_NewTitle")];

        await ReadWorkAsync();
        _snapshot = Snapshot(); _loading = false; RefreshAccess();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        Form.RefreshMoneyTexts();
        if (!CanSave) return;
        var presentation = _presentationRevision;
        var raw = Snapshot(); var moneyRevision = Form.MoneyRevision; var originalWork = _work!;
        bool CurrentDraft() => OwnsChild(presentation) && ReferenceEquals(_work, originalWork)
            && Snapshot() == raw && Form.MoneyRevision == moneyRevision && Form.MoneyContextsReady;
        IsBusy = true; SaveError = null;
        var target = _account.CopyForEditing();
        try
        {
            // Disable input before the account read, so another native Save cannot race validation.
            _originalAccountHandoff?.EnsureCurrent();
            var accounts = _originalAccountHandoff?.Accounts ?? await _store.GetAccountsAsync();
            if (!Form.TryApply(target, _localization.CurrentCulture, accounts)) return;
            if (!CurrentDraft()) throw new InvalidOperationException("The original account input changed before Save.");
            if (_originalAccountHandoff is { } child)
            {
                await child.SaveAsync(_store, target, IsDefault, CurrentDraft);
                _account = target; _committed = true; _snapshot = Snapshot(); RefreshAccess();
                if (child.NotificationFailed) { SaveError = _translator["Account_SavedNeedsReview"]; return; }
            }
            else if (!await _store.SaveAccountAsync(target, isCurrent: CurrentDraft, originalWork: originalWork))
            {
                SaveError = Form.CurrencyLockText ?? _translator["Account_CurrencyLocked"];
                return;
            }
            if (_originalAccountHandoff is null)
            {
                _account = target; _committed = true; _snapshot = Snapshot(); RefreshAccess();
                await _store.UpdateOriginalAccountDefaultAsync(_account.Id, IsDefault, originalWork, () => OwnsChild(presentation));
            }
        }
        catch (CommittedWriteNotificationException)
        {
            _account = target; _committed = true; _snapshot = Snapshot(); RefreshAccess();
            SaveError = _translator["Account_SavedNeedsReview"]; return;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (!_committed && exception is CommercialWriteRejectedException) { _workRetired = true; RefreshAccess(); }
            SaveError = _committed ? _translator["Account_SavedNeedsReview"]
                : Presentation.CommercialFailureMessage.OrFallback(exception, _translator, "Common_SaveFailed", draftKept: true);
            return;
        }
        finally { IsBusy = false; }

        try
        {
            if (!OwnsChild(presentation)) return;
            await _interaction.NavigateAsync("..");
            if (_originalAccountHandoff is null && !IsExisting && Form.IsDebtType)
                await _interaction.NavigateAsync(AppRoutes.AccountDetailRoute, new Dictionary<string, object> { ["id"] = _account.Id });
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            SaveError = _translator["Editor_SavedReturnFailed"]; await _interaction.ShowFailureAsync(exception);
        }
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private async Task ToggleArchiveAsync()
    {
        if (!CanInteract) return;
        IsBusy = true; SaveError = null; AccountArchiveResult? accepted = null;
        try
        {
            var reviewed = await _store.ReadAccountArchiveAsync(_account.Id);
            if (reviewed.IsArchived != IsArchived) throw new InvalidOperationException("The original archive state changed.");
            var archive = !IsArchived; var action = _translator[archive ? "Account_Archive" : "Account_Unarchive"];
            if (archive || IsDirty)
            {
                var message = reviewed.PlanNames.Count == 0 ? _translator["Account_ArchiveMessage"]
                    : _translator.Format("Account_ArchiveWithPlans", reviewed.PlanNames.Count, string.Join(", ", reviewed.PlanNames.Take(5)));
                if (!archive) message = _translator["Account_UnarchiveDraft"];
                if (IsDirty) message += "\n\n" + _translator["Account_ArchiveDraftDiscard"];
                if (!await _interaction.ConfirmAsync(action, message, action, _translator["Common_KeepEditing"])) return;
            }
            accepted = await _store.WriteAccountArchiveAsync(reviewed, archive, _plans);
            AcceptArchive(accepted);
            Form.Load(_account, _localization.CurrentCulture, await _store.GetCurrencyLockAsync(_account.Id));
            _snapshot = Snapshot();
            if (accepted.DefaultCleared) await _interaction.AlertAsync(_translator["Account_Archive"], _translator["Account_DefaultCleared"], _translator["Common_Ok"]);
        }
        catch (AccountArchivePublicationException exception)
        {
            accepted = exception.Result; AcceptArchive(accepted); SaveError = _translator["Account_ArchiveSavedReview"]; return;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            SaveError = accepted is not null ? _translator["Account_ArchiveSavedReview"]
                : Presentation.CommercialFailureMessage.OrFallback(exception, _translator, "Account_ArchiveNeedsReview", draftKept: true);
            return;
        }
        finally { IsBusy = false; }
        try { await _interaction.NavigateAsync(".."); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { SaveError = _translator["Editor_SavedReturnFailed"]; await _interaction.ShowFailureAsync(exception); }
    }

    private void AcceptArchive(AccountArchiveResult result)
    {
        _account = result.Account; IsArchived = _account.IsArchived;
        if (result.DefaultCleared) IsDefault = false;
        _committed = true; _snapshot = Snapshot(); RefreshAccess();
    }

    [RelayCommand(CanExecute = nameof(CanLeave))]
    private async Task CancelAsync()
    {
        var presentation = _presentationRevision;
        if (!OwnsChild(presentation)) return;
        if (_loading || _committed || !IsDirty || await ConfirmDiscardAsync())
        {
            if (!OwnsChild(presentation)) return;
            try { await _interaction.NavigateAsync(".."); }
            catch (Exception exception) when (exception is not OutOfMemoryException) { await _interaction.ShowFailureAsync(exception); }
        }
    }

    /// <summary>Asks whether unsaved changes may be discarded.</summary>
    public Task<bool> ConfirmDiscardAsync() => _interaction.ConfirmAsync(
        _translator["Common_DiscardTitle"], _translator["Common_DiscardMessage"], _translator["Common_Discard"], _translator["Common_KeepEditing"]);

}
