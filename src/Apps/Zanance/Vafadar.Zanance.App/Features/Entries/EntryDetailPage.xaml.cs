namespace Vafadar.Zanance.App.Features.Entries;

public partial class EntryDetailPage : ContentPage
{
    private readonly EntryDetailViewModel _viewModel;

    public EntryDetailPage(EntryDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnDisappearing()
    {
        _viewModel.Retire();
        base.OnDisappearing();
    }

    private async void OnReloadClicked(object? sender, EventArgs args) => await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // The entry may have been edited, refunded or reviewed in the meantime.
        await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
    }
}
