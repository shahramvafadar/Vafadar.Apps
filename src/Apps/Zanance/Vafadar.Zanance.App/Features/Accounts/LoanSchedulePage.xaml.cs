namespace Vafadar.Zanance.App.Features.Accounts;

public partial class LoanSchedulePage : ContentPage
{
    private readonly LoanScheduleViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;

    public LoanSchedulePage(LoanScheduleViewModel viewModel)
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
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterLoanSchedule(this);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }
    /// <summary>Retires original source publication when this native schedule leaves.</summary>
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
    /// <summary>Shares the original covered loader on an actual retained-tab return.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_shellRetired) return Task.CompletedTask;
        _viewModel.SetActive(true); return _viewModel.LoadAsync();
    }
    /// <summary>Removes the pending native page's publication authority.</summary>
    internal void RetirePublication() => _viewModel.SetActive(false);
    /// <summary>Prevents a retired root from reopening the old original schedule.</summary>
    internal void RetireShellPublication() { _shellRetired = true; RetirePublication(); }
}
