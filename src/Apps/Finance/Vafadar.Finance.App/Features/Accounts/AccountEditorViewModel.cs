using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Accounts;

public sealed partial class AccountEditorViewModel : ViewModelBase, IQueryAttributable
{
    private readonly FinanceStore _store;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private Account _account;
    private string _snapshot = string.Empty;

    public AccountEditorViewModel(FinanceStore store, Translator translator, ILocalizationService localization, TimeProvider time)
    {
        _store = store;
        _translator = translator;
        _localization = localization;
        Form = new AccountFormModel(translator, time);
        _account = new Account { Name = string.Empty, CurrencyCode = Form.CurrencyCode, OpeningDate = Form.OpeningDate };
        Title = translator["Account_NewTitle"];
    }

    public AccountFormModel Form { get; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial bool IsExisting { get; set; }

    [ObservableProperty]
    public partial bool IsArchived { get; set; }

    [ObservableProperty]
    public partial bool IsDefault { get; set; }

    [ObservableProperty]
    public partial string? SaveError { get; set; }

    /// <summary>Gets a value indicating whether the user changed something.</summary>
    public bool IsDirty => Form.Snapshot() != _snapshot;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var settings = await _store.GetSettingsAsync();

        if (query.TryGetValue("id", out var value) && value is Guid id
            && (await _store.GetAccountsAsync()).FirstOrDefault(a => a.Id == id) is { } account)
        {
            _account = account;
            Form.Load(account, _localization.CurrentCulture, await _store.HasEntriesAsync(id));
            IsExisting = true;
            IsArchived = account.IsArchived;
            IsDefault = settings.DefaultAccountId == id;
            Title = _translator["Account_EditTitle"];
        }
        else
        {
            Form.CurrencyCode = settings.ReportCurrencyCode;
        }

        _snapshot = Form.Snapshot();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy || !Form.TryApply(_account, _localization.CurrentCulture))
        {
            return;
        }

        IsBusy = true;
        try
        {
            if (!await _store.SaveAccountAsync(_account))
            {
                SaveError = _translator["Account_CurrencyLocked"];
                return;
            }

            await UpdateDefaultAsync();
            _snapshot = Form.Snapshot();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception)
        {
            SaveError = _translator["Common_SaveFailed"];
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleArchiveAsync()
    {
        if (!IsArchived)
        {
            var confirmed = await Shell.Current.DisplayAlertAsync(
                _translator["Account_Archive"], _translator["Account_ArchiveMessage"], _translator["Account_Archive"], _translator["Common_Cancel"]);
            if (!confirmed)
            {
                return;
            }
        }

        _account.IsArchived = !IsArchived;
        await _store.SaveAccountAsync(_account);
        IsArchived = _account.IsArchived;
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (!IsDirty || await ConfirmDiscardAsync())
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    /// <summary>Asks whether unsaved changes may be discarded.</summary>
    public Task<bool> ConfirmDiscardAsync() => Shell.Current.DisplayAlertAsync(
        _translator["Common_DiscardTitle"], _translator["Common_DiscardMessage"], _translator["Common_Discard"], _translator["Common_KeepEditing"]);

    private async Task UpdateDefaultAsync()
    {
        var settings = await _store.GetSettingsAsync();
        var isDefaultNow = settings.DefaultAccountId == _account.Id;
        if (IsDefault != isDefaultNow)
        {
            settings.DefaultAccountId = IsDefault ? _account.Id : null;
            await _store.SaveSettingsAsync(settings);
        }
    }
}
