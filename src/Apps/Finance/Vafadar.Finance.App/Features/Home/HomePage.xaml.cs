namespace Vafadar.Finance.App.Features.Home;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // The language or calendar may have changed on another tab.
        _viewModel.Refresh();
    }
}
