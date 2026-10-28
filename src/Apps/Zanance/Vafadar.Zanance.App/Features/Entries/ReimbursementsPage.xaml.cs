namespace Vafadar.Zanance.App.Features.Entries;

public partial class ReimbursementsPage : ContentPage
{
    private readonly ReimbursementsViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;

    public ReimbursementsPage(ReimbursementsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void OnReloadClicked(object? sender, EventArgs e) => await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterReimbursements(this);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <summary>Retires source reads and retained row commands on an actual original departure.</summary>
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
    /// <summary>Shares the covered original loader on an actual retained-tab return.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_shellRetired) return Task.CompletedTask;
        _viewModel.SetActive(true); return _viewModel.LoadAsync();
    }
    /// <summary>Prevents a departed page from publishing its pending read.</summary>
    internal void RetirePublication() => _viewModel.SetActive(false);
    /// <summary>Retires the original shell permanently without redirecting its file or reopening its reader.</summary>
    internal void RetireShellPublication() { _shellRetired = true; RetirePublication(); }
}
