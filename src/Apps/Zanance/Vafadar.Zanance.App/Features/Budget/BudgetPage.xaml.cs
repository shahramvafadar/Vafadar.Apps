namespace Vafadar.Zanance.App.Features.Budget;

public partial class BudgetPage : ContentPage
{
    private readonly BudgetViewModel _viewModel;

    public BudgetPage(BudgetViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnShown();
        await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
    }
}
