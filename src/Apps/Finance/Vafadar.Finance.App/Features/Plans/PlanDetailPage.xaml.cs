namespace Vafadar.Finance.App.Features.Plans;

public partial class PlanDetailPage : ContentPage
{
    private readonly PlanDetailViewModel _viewModel;

    public PlanDetailPage(PlanDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
