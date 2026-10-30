using Vafadar.Zanance.App.Presentation;
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
    private AccountantWorkspaceSnapshot? _workspace;
    private int _epoch, _readVersion, _operation;
    private bool _active = true, _rootRetired;
    private Func<bool>? _rootCurrent;
    private string? _file;
    private CancellationTokenSource _frame = new();
    private bool Current => _active && !_rootRetired && !_frame.IsCancellationRequested && _rootCurrent?.Invoke() != false;
    /// <summary>Gets complete current original account choices, published together under source cover.</summary>
    public SnapshotCollection<AccountantAccountChoice> Accounts { get; } = [];
    /// <summary>Gets or sets the literal Gregorian year independently of display calendar and source Retry.</summary>
    [ObservableProperty] public partial string Year { get; set; } = time.GetLocalNow().Year.ToString(CultureInfo.InvariantCulture);
    /// <summary>Gets or sets explicit optional payee/note disclosure; never inferred by a source read.</summary>
    [ObservableProperty] public partial bool IncludeNotes { get; set; }
    /// <summary>Gets or sets explicit optional stored-copy disclosure, initially false.</summary>
    [ObservableProperty] public partial bool IncludeDocuments { get; set; }
    /// <summary>Gets the translated summary of exactly the current reviewed packet.</summary>
    [ObservableProperty] public partial string? Summary { get; private set; }
    /// <summary>Gets source/operation feedback without private exception details.</summary>
    [ObservableProperty] public partial string? Error { get; private set; }
    /// <summary>Gets the original year-specific validation feedback.</summary>
    [ObservableProperty] public partial string? YearError { get; private set; }
    /// <summary>Gets the original account-selection feedback.</summary>
    [ObservableProperty] public partial string? AccountsError { get; private set; }
    /// <summary>Gets whether source input is fully published for this original native frame.</summary>
    [ObservableProperty] public partial bool Loaded { get; private set; }
    /// <summary>Gets whether the actual frame may explicitly reload its original source.</summary>
    public bool CanReload => Current && !IsBusy;
    /// <summary>Gets whether original ready disclosure inputs may be reviewed or selected.</summary>
    public bool CanReview => Current && Loaded && !IsBusy && _workspace is not null;
    /// <summary>Gets whether the exact reviewed packet still has original native sharing authority.</summary>
    public bool CanShare => CanReview && _review is not null;
    partial void OnLoadedChanged(bool value) => Refresh();
    partial void OnYearChanged(string value) => Invalidate();
    partial void OnIncludeNotesChanged(bool value) => Invalidate();
    partial void OnIncludeDocumentsChanged(bool value) => Invalidate();
    /// <summary>Binds only the actual original native application root.</summary>
    public void BindRootOwner(Func<bool> current) => _rootCurrent = current;
    /// <summary>Allows only actual page appearance/return to activate a departed frame.</summary>
    public void SetPresentationActive(bool value)
    {
        if (!value) { Retire(); return; }
        if (_rootRetired || _rootCurrent?.Invoke() == false) return;
        if (!_active) { _frame.Dispose(); _frame = new(); _active = true; }
        Refresh();
    }
    /// <summary>Permanently retires this original form when its actual root is replaced.</summary>
    public void RetireRoot() { _rootRetired = true; Retire(); }
    /// <summary>Cancels original source/output continuations while retaining unsubmitted raw disclosure choices.</summary>
    public void Retire()
    { _active = false; _readVersion++; _operation++; _frame.Cancel(); Invalidate(); Loaded = false; IsBusy = false; Refresh(); }
    private void Refresh()
    { OnPropertyChanged(nameof(CanReload)); OnPropertyChanged(nameof(CanReview)); OnPropertyChanged(nameof(CanShare)); }
    private void Invalidate() { Interlocked.Increment(ref _epoch); _review = null; Summary = null; Refresh(); }
    private void EnsureFrame()
    { if (!Current) throw new OperationCanceledException("The original disclosure frame retired."); _frame.Token.ThrowIfCancellationRequested(); }
    private bool AcceptFile(string file)
    { EnsureFrame(); _file ??= file; return _file == file; }
    /// <summary>Loads only the original actual file, preserving unchanged explicit account choices and raw year/options through Retry.</summary>
    [RelayCommand] public async Task LoadAsync()
    {
        if (!CanReload) return;
        Invalidate(); var readVersion = ++_readVersion; var operation = ++_operation; var ct = _frame.Token;
        IsBusy = true; Loaded = false; Error = null; Refresh();
        try
        {
            var workspace = await store.ReadWorkspaceAsync(ct, AcceptFile);
            EnsureFrame(); if (readVersion != _readVersion) return; workspace.EnsureCurrent();
            var selected = _workspace is null ? [] : _workspace.Accounts.Where(old => Accounts.Any(row => row.Id == old.Id && row.Selected)).ToArray();
            var rows = workspace.Accounts.Select(account =>
            {
                AccountantAccountChoice? row = null;
                row = new(account, () => { if (Current && Accounts.Any(current => ReferenceEquals(current, row))) Invalidate(); });
                // Selection follows unchanged original captions only; a changed/restored account requires explicit new disclosure.
                row.Selected = selected.Contains(account); return row;
            }).ToArray();
            _workspace = workspace; Accounts.ReplaceAll(rows); Loaded = true;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (Current && readVersion == _readVersion) { Loaded = false; Error = translator["Tax_Failed"]; } }
        finally { if (operation == _operation) { IsBusy = false; Refresh(); } }
    }
    /// <summary>Selects only actual current displayed account identities.</summary>
    [RelayCommand] private void SelectAll() { if (CanReview) foreach (var account in Accounts) account.Selected = true; }
    /// <summary>Removes current disclosure choices without changing stored accounts.</summary>
    [RelayCommand] private void SelectNone() { if (CanReview) foreach (var account in Accounts) account.Selected = false; }
    /// <summary>Reviews exactly the original current captions/year/opt-ins without a financial write or output file.</summary>
    [RelayCommand] public async Task ReviewAsync()
    {
        if (!CanReview || _workspace is not { } workspace) return;
        Invalidate(); Error = null;
        var validYear = int.TryParse(Digits.ToAscii(Year), NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            && year >= 1900 && year <= time.GetLocalNow().Year;
        YearError = validYear ? null : translator["Tax_YearError"];
        var ids = Accounts.Where(a => a.Selected).Select(a => a.Id).ToArray();
        AccountsError = ids.Length > 0 ? null : translator["Tax_AccountsError"];
        if (!validYear || ids.Length == 0) return;
        var epoch = _epoch; var operation = ++_operation; var ct = _frame.Token;
        var request = new AccountantExportRequest(year, ids, IncludeNotes, IncludeDocuments); IsBusy = true; Refresh();
        try
        {
            var review = await Task.Run(() => store.ReviewAsync(request, () => Validate(epoch), ct, workspace), ct);
            Validate(epoch); _review = review;
            Summary = string.Format(CultureInfo.CurrentCulture, translator["Tax_Summary"], review.Entries, review.Documents,
                review.DocumentBytes, review.LinkedAccounts);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (Current && epoch == _epoch) Error = translator["Tax_Failed"]; }
        finally { if (operation == _operation) { IsBusy = false; Refresh(); } }
    }
    /// <summary>Retains exact original disclosure and native authentication before bounded preparation and sharing.</summary>
    [RelayCommand] public async Task ShareAsync()
    {
        if (!CanShare || _review is not { } review) return;
        var epoch = _epoch; var operation = ++_operation; var ct = _frame.Token; IsBusy = true; Error = null; Refresh();
        string? path = null; var offered = false;
        try
        {
            Validate(epoch); runtime.EnsureAvailable();
            if (!await interaction.ConfirmAsync(translator["Tax_Title"], translator["Tax_Confirm"], translator["Tax_Share"], translator["Common_Cancel"])) return;
            Validate(epoch);
            if (!await runtime.ConfirmAsync()) return;
            Validate(epoch); runtime.EnsureAvailable();
            path = await runtime.WriteAsync(stream => Task.Run(() => store.BuildAsync(review, stream, () => Validate(epoch), ct), ct));
            await Task.Run(() => store.ValidateAsync(review, () => Validate(epoch), ct), ct);
            Validate(epoch); runtime.EnsureAvailable(); offered = true; await runtime.ShareAsync(path);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (Current && epoch == _epoch) { Invalidate(); Error = translator["Tax_Failed"]; } }
        finally
        {
            try { if (path is not null && !offered) runtime.Remove(path); }
            finally { if (operation == _operation) { IsBusy = false; Refresh(); } }
        }
    }
    private void Validate(int epoch)
    {
        EnsureFrame();
        if (epoch != Volatile.Read(ref _epoch)) throw new InvalidOperationException("The disclosure review was retired.");
        _workspace?.EnsureCurrent();
    }
}
