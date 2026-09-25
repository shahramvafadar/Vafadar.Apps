namespace Vafadar.Finance.App.Features.Entries;

public partial class EntryDetailPage : ContentPage
{
    private readonly EntryDetailViewModel _viewModel;

    public EntryDetailPage(EntryDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // The entry may have been edited, refunded or reviewed in the meantime.
        await _viewModel.LoadAsync();
    }
}
