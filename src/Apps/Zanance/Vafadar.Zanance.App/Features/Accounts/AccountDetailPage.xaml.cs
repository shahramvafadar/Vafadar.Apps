namespace Vafadar.Zanance.App.Features.Accounts;

public partial class AccountDetailPage : ContentPage
{
    private readonly AccountDetailViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;

    public AccountDetailPage(AccountDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell))
            { RetireShellCommands(); return; }
            _ownerShell = shell; shell.RegisterAccountDetail(this);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <summary>Retires pending original balance consent before another native page receives input.</summary>
    protected override void OnDisappearing()
    {
        RetireCommands(); base.OnDisappearing();
    }

    /// <summary>Shares the covered read when the original page or its retained Shell tab becomes visible.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_shellRetired) return Task.CompletedTask;
        _viewModel.SetReconciliationActive(true); return _viewModel.LoadAsync();
    }

    /// <summary>A retained tab departure retires original reconciliation and conversion consent even without OnDisappearing.</summary>
    internal void RetireCommands() => _viewModel.SetReconciliationActive(false);

    /// <summary>An old shell's account page cannot acquire native command authority under a replacement root.</summary>
    internal void RetireShellCommands() { _shellRetired = true; RetireCommands(); }
}
