namespace Vafadar.Zanance.App.Features.Profiles;

public partial class ProfilesPage : ContentPage
{
    private readonly ProfilesViewModel _viewModel;

    public ProfilesPage(ProfilesViewModel viewModel)
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
