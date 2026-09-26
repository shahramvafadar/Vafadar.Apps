namespace Vafadar.Finance.App.Features.DataFiles;

public partial class ImportExportPage : ContentPage
{
    private readonly ImportExportViewModel _viewModel;

    public ImportExportPage(ImportExportViewModel viewModel)
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
