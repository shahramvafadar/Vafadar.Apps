namespace Vafadar.Zanance.App.Features.Goals;

/// <summary>Keeps one original goal list covered until its complete current source is published.</summary>
public partial class GoalsPage : ContentPage
{
    private readonly GoalsViewModel _viewModel;

    /// <summary>Creates the actual list and binds its original source owner.</summary>
    public GoalsPage(GoalsViewModel viewModel)
    {
        InitializeComponent(); BindingContext = _viewModel = viewModel;
    }

    /// <summary>Gets the current original appearance/retry publication for native qualification.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); var pending = LoadCoveredAsync(); PendingPublication = pending;
        await Presentation.Failures.GuardAsync(() => pending);
    }

    private async void OnReloadClicked(object? sender, EventArgs e)
    {
        var pending = LoadCoveredAsync(); PendingPublication = pending;
        await Presentation.Failures.GuardAsync(() => pending);
    }

    private async Task LoadCoveredAsync()
    {
        try { await _viewModel.LoadAsync(); }
        catch (Exception error) when (error is not OutOfMemoryException && _viewModel.Loading.HasFailed)
        {
            // The local translated retry cover owns this failed read; do not open a second generic modal.
            System.Diagnostics.Debug.WriteLine("Original goal list load failed: " + error.GetType().FullName);
            Presentation.Failures.Observed?.Invoke(error);
        }
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _viewModel.Retire(); base.OnDisappearing();
    }
}
