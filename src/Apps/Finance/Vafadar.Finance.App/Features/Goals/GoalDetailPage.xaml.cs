namespace Vafadar.Finance.App.Features.Goals;

public partial class GoalDetailPage : ContentPage
{
    private readonly GoalDetailViewModel _viewModel;

    public GoalDetailPage(GoalDetailViewModel viewModel)
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