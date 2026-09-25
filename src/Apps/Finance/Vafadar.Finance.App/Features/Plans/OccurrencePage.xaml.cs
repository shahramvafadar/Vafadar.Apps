namespace Vafadar.Finance.App.Features.Plans;

public partial class OccurrencePage : ContentPage
{
    private readonly OccurrenceViewModel _viewModel;

    public OccurrencePage(OccurrenceViewModel viewModel)
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
