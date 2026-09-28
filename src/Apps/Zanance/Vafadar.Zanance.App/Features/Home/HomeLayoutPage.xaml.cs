namespace Vafadar.Zanance.App.Features.Home;

public partial class HomeLayoutPage : ContentPage
{
    private readonly HomeLayoutViewModel _viewModel;

    public HomeLayoutPage(HomeLayoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Load();
    }
}