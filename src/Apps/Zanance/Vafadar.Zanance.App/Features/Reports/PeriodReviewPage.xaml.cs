namespace Vafadar.Zanance.App.Features.Reports;

public partial class PeriodReviewPage : ContentPage
{
    private readonly PeriodReviewViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;
    private bool _publicationActive;
    /// <summary>Shares original appearance/navigation publication for one actual page presence.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    public PeriodReviewPage(PeriodReviewViewModel viewModel)
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
            _ownerShell = shell; shell.RegisterPeriodReview(this);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <summary>Refreshes a retained actual tab without a second automatic Retry for the same presence.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_shellRetired) return Task.CompletedTask;
        if (_publicationActive) return PendingPublication;
        _publicationActive = true; _viewModel.SetActive(true);
        return PendingPublication = _viewModel.LoadAsync();
    }
    /// <summary>Retires original rows, reads and native confirmations on actual departure.</summary>
    internal void RetirePublication() { _publicationActive = false; _viewModel.SetActive(false); }
    /// <summary>Prevents an old native root from publishing or accepting original period-review commands.</summary>
    internal void RetireShellPublication() { _shellRetired = true; RetirePublication(); }
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
}
