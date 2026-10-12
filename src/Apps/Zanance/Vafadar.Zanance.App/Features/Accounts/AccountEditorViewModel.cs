using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

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
    private bool _loading = true;

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
        Form = new AccountFormModel(translator, time);
        _account = new Account { Name = string.Empty, CurrencyCode = Form.CurrencyCode, OpeningDate = Form.OpeningDate };
        Title = translator["Account_NewTitle"];
        Form.PropertyChanged += (_, e) =>
        {
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
    public bool CanInteract => !_loading && !IsBusy && !_committed;
    /// <summary>Gets whether a financial Save can run; a committed account cannot be resubmitted.</summary>
    public bool CanSave => CanInteract;
    /// <summary>Gets whether a pending writer must finish before leaving this form.</summary>
    public bool CanLeave => !IsBusy;
    /// <summary>Gets the actual Close/Cancel caption after the commit boundary.</summary>
    public string CloseText => _translator[_committed ? "Common_Close" : "Common_Cancel"];
    private void RefreshAccess()
    {
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
        var settings = await _store.GetSettingsAsync();
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
            Form.CurrencyCode = settings.DefaultCurrencyCode;
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

        _snapshot = Snapshot(); _loading = false; RefreshAccess();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (!CanSave) return;
        IsBusy = true; SaveError = null;
        var target = _account.CopyForEditing();
        try
        {
            // Disable input before the account read, so another native Save cannot race validation.
            if (!Form.TryApply(target, _localization.CurrentCulture, await _store.GetAccountsAsync())) return;
            if (!await _store.SaveAccountAsync(target))
            {
                SaveError = Form.CurrencyLockText ?? _translator["Account_CurrencyLocked"];
                return;
            }
            _account = target; _committed = true; _snapshot = Snapshot(); RefreshAccess();
            await UpdateDefaultAsync();
        }
        catch (CommittedWriteNotificationException)
        {
            _account = target; _committed = true; _snapshot = Snapshot(); RefreshAccess();
            SaveError = _translator["Account_SavedNeedsReview"]; return;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            SaveError = _committed ? _translator["Account_SavedNeedsReview"]
                : Presentation.CommercialFailureMessage.OrFallback(exception, _translator, "Common_SaveFailed", draftKept: true);
            return;
        }
        finally { IsBusy = false; }

        try
        {
            await _interaction.NavigateAsync("..");
            if (!IsExisting && Form.IsDebtType)
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
        if (_loading || _committed || !IsDirty || await ConfirmDiscardAsync())
        {
            try { await _interaction.NavigateAsync(".."); }
            catch (Exception exception) when (exception is not OutOfMemoryException) { await _interaction.ShowFailureAsync(exception); }
        }
    }

    /// <summary>Asks whether unsaved changes may be discarded.</summary>
    public Task<bool> ConfirmDiscardAsync() => _interaction.ConfirmAsync(
        _translator["Common_DiscardTitle"], _translator["Common_DiscardMessage"], _translator["Common_Discard"], _translator["Common_KeepEditing"]);

    private async Task UpdateDefaultAsync()
    {
        var settings = await _store.GetSettingsAsync();
        var isDefaultNow = settings.DefaultAccountId == _account.Id;

        // An account changed into a loan, money lent or an asset stops being the default (quick add would refuse it).
        var wanted = IsDefault && EntryAccountContract.IsValidDefault(_account);
        if (wanted != isDefaultNow)
        {
            settings.DefaultAccountId = wanted ? _account.Id : null;
            await _store.SaveSettingsAsync(settings);
        }
    }
}
