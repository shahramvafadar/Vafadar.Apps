namespace Vafadar.Zanance.App.Features.Forecast;

public partial class SnapshotPage : ContentPage
{
    private readonly SnapshotViewModel _viewModel;

    /// <summary>Gets the original appearance/navigation read without starting a substitute comparison.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    public SnapshotPage(SnapshotViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <summary>Shares the original covered comparison read after appearance or retained Shell-tab activation.</summary>
    internal Task RefreshForNavigationAsync() => !PendingPublication.IsCompleted
        ? PendingPublication : PendingPublication = _viewModel.LoadAsync();
}
