namespace Vafadar.Zanance.App.Features.Reports;

public partial class PeriodReviewPage : ContentPage
{
    private readonly PeriodReviewViewModel _viewModel;

    public PeriodReviewPage(PeriodReviewViewModel viewModel)
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
