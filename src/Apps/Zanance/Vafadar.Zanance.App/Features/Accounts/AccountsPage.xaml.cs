namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>Original account groups with a complete loading/retry cover and retired native target ownership.</summary>
public partial class AccountsPage : ContentPage
{
    private readonly AccountsViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;

    /// <summary>Shares the original read when appearance and retained-tab navigation arrive together.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    /// <summary>Binds the actual original overview; no database is prepared during construction.</summary>
    public AccountsPage(AccountsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell))
            { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterAccountOverview(this);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <summary>Refreshes the original balances after another tab changed the ledger, without posting money.</summary>
    internal Task RefreshForNavigationAsync() => _shellRetired ? Task.CompletedTask : !PendingPublication.IsCompleted
        ? PendingPublication : PendingPublication = LoadCoveredAsync();

    private async void OnReloadClicked(object? sender, EventArgs e) => await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);

    private async Task LoadCoveredAsync()
    {
        try { await _viewModel.LoadAsync(); }
        catch (Exception ex) when (ex is not OutOfMemoryException && _viewModel.Loading.HasFailed)
        {
            System.Diagnostics.Debug.WriteLine($"Original account overview failed: {ex}");
            Presentation.Failures.Observed?.Invoke(ex);
        }
    }

    /// <summary>Routes only the current page's published row through its original ownership guard.</summary>
    internal Task OpenAccountAsync(AccountItem row) => _viewModel.OpenAsync(row);

    /// <summary>Retires a retained Android tab even when its actual page does not receive OnDisappearing.</summary>
    internal void RetirePublication()
    {
        _viewModel.Retire(); PendingPublication = Task.CompletedTask;
    }

    /// <summary>A disconnected original shell can never acquire a new publication lifetime for this old page.</summary>
    internal void RetireShellPublication()
    {
        _shellRetired = true; RetirePublication();
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        RetirePublication();
        base.OnDisappearing();
    }
}
