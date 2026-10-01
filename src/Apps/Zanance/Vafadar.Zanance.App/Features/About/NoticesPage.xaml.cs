namespace Vafadar.Zanance.App.Features.About;

public partial class NoticesPage : ContentPage
{
    private readonly AboutViewModel _viewModel;

    public NoticesPage(AboutViewModel viewModel)
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
