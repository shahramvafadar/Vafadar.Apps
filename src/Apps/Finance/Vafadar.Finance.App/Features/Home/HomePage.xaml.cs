namespace Vafadar.Finance.App.Features.Home;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Language, calendar or data may have changed elsewhere.
        await _viewModel.LoadAsync();
    }
}
