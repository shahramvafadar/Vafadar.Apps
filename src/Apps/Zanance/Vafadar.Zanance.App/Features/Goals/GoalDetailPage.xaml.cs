namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>Keeps original complete goal details covered until their actual source is ready.</summary>
public partial class GoalDetailPage : ContentPage
{
    private readonly GoalDetailViewModel _viewModel;

    /// <summary>Creates the original detail page and binds its retained source owner.</summary>
    public GoalDetailPage(GoalDetailViewModel viewModel)
    {
        InitializeComponent(); BindingContext = _viewModel = viewModel;
    }

    /// <summary>Gets the current original appearance publication for native qualification.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); var pending = LoadCoveredAsync(); PendingPublication = pending;
        await Presentation.Failures.GuardAsync(() => pending);
    }

    private async Task LoadCoveredAsync()
    {
        try { await _viewModel.LoadAsync(); }
        catch (Exception error) when (error is not OutOfMemoryException && _viewModel.HasReadFailed)
        {
            // The actual local retry cover owns this read failure, without a second generic modal.
            System.Diagnostics.Debug.WriteLine("Original goal detail load failed: " + error.GetType().FullName);
            Presentation.Failures.Observed?.Invoke(error);
        }
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _viewModel.Retire(); base.OnDisappearing();
    }
}
