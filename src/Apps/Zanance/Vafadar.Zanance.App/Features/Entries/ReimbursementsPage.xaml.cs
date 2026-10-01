namespace Vafadar.Zanance.App.Features.Entries;

public partial class ReimbursementsPage : ContentPage
{
    private readonly ReimbursementsViewModel _viewModel;

    public ReimbursementsPage(ReimbursementsViewModel viewModel)
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
