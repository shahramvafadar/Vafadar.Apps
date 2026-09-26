namespace Vafadar.Finance.App.Features.Goals;

public partial class GoalsPage : ContentPage
{
    private readonly GoalsViewModel _viewModel;

    public GoalsPage(GoalsViewModel viewModel)
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