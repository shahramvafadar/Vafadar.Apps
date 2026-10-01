namespace Vafadar.Zanance.App.Features.Categories;

public partial class RulesPage : ContentPage
{
    private readonly RulesViewModel _viewModel;

    public RulesPage(RulesViewModel viewModel)
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
