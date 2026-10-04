namespace Vafadar.Zanance.App.Features.Holdings;

public partial class HoldingDetailPage : ContentPage
{
    private readonly HoldingDetailViewModel _viewModel;

    public HoldingDetailPage(HoldingDetailViewModel viewModel)
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
