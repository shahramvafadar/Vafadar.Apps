using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Accounts;

public sealed partial class AccountEditorViewModel : ViewModelBase, IQueryAttributable
{
    private readonly ZananceStore _store;
    private readonly PlanStore _plans;
    private readonly TimeProvider _time;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private Account _account;
    private string _snapshot = string.Empty;

    public AccountEditorViewModel(ZananceStore store, PlanStore plans, Translator translator, ILocalizationService localization, TimeProvider time)
    {
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
            }
        };
        UpdateCanBeDefault();
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

    public async void ApplyQueryAttributes(IDictionary<string, object> query) => await Presentation.Failures.GuardAsync(() => ApplyQueryAsync(query));

    private async Task ApplyQueryAsync(IDictionary<string, object> query)
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
        }

        _snapshot = Snapshot();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Names tell accounts apart in pickers and in CSV files, so one cannot be used twice (CR08).
        if (IsBusy || !Form.TryApply(_account, _localization.CurrentCulture, await _store.GetAccountsAsync()))
        {
            return;
        }

        var saved = false;
        IsBusy = true;
        try
        {
            if (!await _store.SaveAccountAsync(_account))
            {
                SaveError = Form.CurrencyLockText ?? _translator["Account_CurrencyLocked"];
                return;
            }

            await UpdateDefaultAsync();
            _snapshot = Snapshot();
            saved = true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            SaveError = _translator["Common_SaveFailed"];
        }
        finally
        {
            IsBusy = false;
        }

        // Leaving the page is not part of saving: a navigation problem must not say "not saved".
        if (saved)
        {
            await Presentation.Failures.GuardAsync(() => Shell.Current.GoToAsync(".."));
        }
    }

    [RelayCommand]
    private async Task ToggleArchiveAsync()
    {
        if (!IsArchived)
        {
            // Future plans must not silently lose their account (ACC-06, AT-10): they are ended with the archive.
            var active = (await _plans.GetSchedulesAsync())
                .Where(s => s.State != Core.Plans.ScheduleState.Ended && (s.AccountId == _account.Id || s.ToAccountId == _account.Id))
                .ToList();
            var message = active.Count == 0
                ? _translator["Account_ArchiveMessage"]
                : _translator.Format("Account_ArchiveWithPlans", active.Count, string.Join(", ", active.Select(s => s.Name).Take(5)));
            var confirmed = await Shell.Current.DisplayAlertAsync(_translator["Account_Archive"], message, _translator["Account_Archive"], _translator["Common_Cancel"]);
            if (!confirmed)
            {
                return;
            }

            var today = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
            foreach (var schedule in active)
            {
                Core.Plans.PlanActions.End(schedule, today);
            }

            if (active.Count > 0)
            {
                await _plans.SaveSchedulesAsync(active);
            }
        }

        _account.IsArchived = !IsArchived;
        await _store.SaveAccountAsync(_account);
        IsArchived = _account.IsArchived;

        // An archived account cannot be the default any more: the default is cleared and the user is told, so quick add
        // asks for an account instead of silently using another one (ZEX-S0103).
        var settings = await _store.GetSettingsAsync();
        if (IsArchived && settings.DefaultAccountId == _account.Id)
        {
            settings.DefaultAccountId = null;
            await _store.SaveSettingsAsync(settings);
            await Shell.Current.DisplayAlertAsync(_translator["Account_Archive"], _translator["Account_DefaultCleared"], _translator["Common_Ok"]);
        }

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

        // An account changed into a loan, money lent or an asset stops being the default (quick add would refuse it).
        var wanted = IsDefault && EntryAccountContract.IsValidDefault(_account);
        if (wanted != isDefaultNow)
        {
            settings.DefaultAccountId = wanted ? _account.Id : null;
            await _store.SaveSettingsAsync(settings);
        }
    }
}
