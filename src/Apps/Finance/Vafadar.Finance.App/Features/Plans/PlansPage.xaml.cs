namespace Vafadar.Finance.App.Features.Plans;

public partial class PlansPage : ContentPage
{
    private readonly PlansViewModel _viewModel;

    public PlansPage(PlansViewModel viewModel)
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
