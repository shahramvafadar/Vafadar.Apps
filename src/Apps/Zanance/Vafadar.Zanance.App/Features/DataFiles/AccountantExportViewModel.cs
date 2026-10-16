using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Core.Text;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Data.Tax;

namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>Native cache/authentication/share effects; production sources remain in the real store.</summary>
public interface IAccountantExportRuntime
{
    /// <summary>Rejects a covered or locked application before publishing sensitive data.</summary>
    void EnsureAvailable();
    /// <summary>Requests the existing app-lock export confirmation.</summary>
    Task<bool> ConfirmAsync();
    /// <summary>Creates an owned temporary ZIP, writes it once and removes incomplete unpublished output.</summary>
    Task<string> WriteAsync(Func<Stream, Task> write);
    /// <summary>Offers only the prepared owned ZIP to the native share sheet.</summary>
    Task ShareAsync(string path);
    /// <summary>Removes a failed file which was never offered to another application.</summary>
    void Remove(string path);
}

/// <summary>Explicit account disclosure choice; changing it invalidates a previously displayed review.</summary>
public sealed partial class AccountantAccountChoice(AccountantAccount account, Action changed) : ObservableObject
{
    /// <summary>Gets the exact original account id.</summary>
    public Guid Id => account.Id;
    /// <summary>Gets the original account name and currency, retained even for archived accounts.</summary>
    public string Caption => account.Name + " · " + account.Currency;
    /// <summary>Gets or sets explicit disclosure consent, initially false.</summary>
    [ObservableProperty] public partial bool Selected { get; set; }
    partial void OnSelectedChanged(bool value) => changed();
}

/// <summary>One complete read-only Germany preparation flow; no tax assumptions, financial writes or automatic share.</summary>
public sealed partial class AccountantExportViewModel(AccountantExportStore store, Translator translator,
    TimeProvider time, IAppInteraction interaction, IAccountantExportRuntime runtime) : ViewModelBase
{
    private AccountantExportReview? _review;
    private int _epoch;
    private bool _active;
    /// <summary>Gets actual account choices including archived accounts.</summary>
    public ObservableCollection<AccountantAccountChoice> Accounts { get; } = [];
    /// <summary>Gets or sets the Gregorian calendar year, independently of display calendar.</summary>
    [ObservableProperty] public partial string Year { get; set; } = time.GetLocalNow().Year.ToString(CultureInfo.InvariantCulture);
    /// <summary>Gets or sets optional disclosure of payees and notes.</summary>
    [ObservableProperty] public partial bool IncludeNotes { get; set; }
    /// <summary>Gets or sets optional disclosure of stored copies, initially false.</summary>
    [ObservableProperty] public partial bool IncludeDocuments { get; set; }
    /// <summary>Gets the complete translated reviewed disclosure summary.</summary>
    [ObservableProperty] public partial string? Summary { get; private set; }
    /// <summary>Gets translated field and operation feedback without exposing private exception details.</summary>
    [ObservableProperty] public partial string? Error { get; private set; }
    /// <summary>Gets the year-specific validation error.</summary>
    [ObservableProperty] public partial string? YearError { get; private set; }
    /// <summary>Gets the account-specific validation error.</summary>
    [ObservableProperty] public partial string? AccountsError { get; private set; }
    /// <summary>Gets whether an original review may be explicitly shared.</summary>
    public bool CanShare => _review is not null && !IsBusy && _active;
    /// <summary>Gets whether the first read finished successfully.</summary>
    [ObservableProperty] public partial bool Loaded { get; private set; }
    partial void OnYearChanged(string value) => Invalidate();
    partial void OnIncludeNotesChanged(bool value) => Invalidate();
    partial void OnIncludeDocumentsChanged(bool value) => Invalidate();
    /// <summary>Retires pending continuations when the form leaves its original profile/window.</summary>
    public void Retire() { _active = false; Invalidate(); }
    /// <summary>Invalidates only transient disclosure review; never saves settings or money.</summary>
    private void Invalidate() { Interlocked.Increment(ref _epoch); _review = null; Summary = null; OnPropertyChanged(nameof(CanShare)); }

    /// <summary>Loads a covered form without retaining a previous profile's financial selection.</summary>
    [RelayCommand] public async Task LoadAsync()
    {
        if (IsBusy) return;
        _active = true; Invalidate(); var epoch = _epoch; IsBusy = true; Loaded = false; Error = null;
        try
        {
            var accounts = await Task.Run(() => store.AccountsAsync());
            if (!_active || epoch != _epoch) return;
            Accounts.Clear(); foreach (var account in accounts) Accounts.Add(new(account, Invalidate)); Loaded = true;
        }
        catch (Exception) { if (_active) Error = translator["Tax_Failed"]; }
        finally { IsBusy = false; OnPropertyChanged(nameof(CanShare)); }
    }
    /// <summary>Explicitly selects all displayed account identities.</summary>
    [RelayCommand] private void SelectAll() { if (!IsBusy) foreach (var account in Accounts) account.Selected = true; }
    /// <summary>Removes all disclosure choices without changing accounts.</summary>
    [RelayCommand] private void SelectNone() { if (!IsBusy) foreach (var account in Accounts) account.Selected = false; }

    /// <summary>Reviews exactly this year's selected ledger packet on a worker before native effects.</summary>
    [RelayCommand] public async Task ReviewAsync()
    {
        if (IsBusy || !Loaded || !_active) return;
        Invalidate(); Error = null;
        var validYear = int.TryParse(Digits.ToAscii(Year), NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            && year >= 1900 && year <= time.GetLocalNow().Year;
        YearError = validYear ? null : translator["Tax_YearError"];
        var ids = Accounts.Where(a => a.Selected).Select(a => a.Id).ToArray();
        AccountsError = ids.Length > 0 ? null : translator["Tax_AccountsError"];
        if (!validYear || ids.Length == 0) return;
        var epoch = _epoch; var request = new AccountantExportRequest(year, ids, IncludeNotes, IncludeDocuments); IsBusy = true;
        try
        {
            var review = await Task.Run(() => store.ReviewAsync(request, () => Validate(epoch)));
            Validate(epoch); _review = review;
            Summary = string.Format(CultureInfo.CurrentCulture, translator["Tax_Summary"], review.Entries, review.Documents,
                review.DocumentBytes, review.LinkedAccounts);
        }
        catch (Exception) { if (_active && epoch == _epoch) Error = translator["Tax_Failed"]; }
        finally { IsBusy = false; OnPropertyChanged(nameof(CanShare)); }
    }

    /// <summary>Rechecks original consent after confirmation, file preparation and before the native handoff.</summary>
    [RelayCommand] public async Task ShareAsync()
    {
        if (!CanShare || _review is not { } review) return;
        var epoch = _epoch; IsBusy = true; Error = null; OnPropertyChanged(nameof(CanShare));
        string? path = null; var offered = false;
        try
        {
            Validate(epoch); runtime.EnsureAvailable();
            if (!await interaction.ConfirmAsync(translator["Tax_Title"], translator["Tax_Confirm"], translator["Tax_Share"], translator["Common_Cancel"])) return;
            Validate(epoch);
            if (!await runtime.ConfirmAsync()) return;
            Validate(epoch); runtime.EnsureAvailable();
            path = await runtime.WriteAsync(stream => Task.Run(() => store.BuildAsync(review, stream, () => Validate(epoch))));
            await Task.Run(() => store.ValidateAsync(review, () => Validate(epoch)));
            Validate(epoch); runtime.EnsureAvailable(); offered = true; await runtime.ShareAsync(path);
        }
        catch (Exception) { if (_active && epoch == _epoch) { Invalidate(); Error = translator["Tax_Failed"]; } }
        finally
        {
            try { if (path is not null && !offered) runtime.Remove(path); }
            finally { IsBusy = false; OnPropertyChanged(nameof(CanShare)); }
        }
    }
    private void Validate(int epoch)
    {
        if (!_active || epoch != Volatile.Read(ref _epoch)) throw new InvalidOperationException("The disclosure review was retired.");
    }
}
