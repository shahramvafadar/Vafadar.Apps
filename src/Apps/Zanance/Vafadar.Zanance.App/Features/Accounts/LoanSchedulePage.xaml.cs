namespace Vafadar.Zanance.App.Features.Accounts;

public partial class LoanSchedulePage : ContentPage
{
    private readonly LoanScheduleViewModel _viewModel;

    public LoanSchedulePage(LoanScheduleViewModel viewModel)
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