namespace Vafadar.Zanance.App.Features.Plans;

public partial class PlanDetailPage : ContentPage
{
    private readonly PlanDetailViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;

    public PlanDetailPage(PlanDetailViewModel viewModel)
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
            _ownerShell = shell; shell.RegisterPlanDetail(this);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    private async void OnReloadClicked(object? sender, EventArgs e) => await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
    /// <summary>Retires pending original source and native confirmation ownership on actual departure.</summary>
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
    /// <summary>Shares the original covered loader when an actual retained tab becomes visible.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_shellRetired) return Task.CompletedTask;
        _viewModel.SetActive(true); return _viewModel.LoadAsync();
    }
    /// <summary>Prevents departed native targets and confirmations from writing or navigating.</summary>
    internal void RetirePublication() => _viewModel.SetActive(false);
    /// <summary>Permanently retires this original root without following its replacement file.</summary>
    internal void RetireShellPublication() { _shellRetired = true; RetirePublication(); }
}
