namespace Vafadar.Zanance.App.Features.Forecast;

public partial class SnapshotPage : ContentPage
{
    private readonly SnapshotViewModel _viewModel;

    public SnapshotPage(SnapshotViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
    }
}
