namespace Vafadar.Zanance.App.Features.DataFiles;

public partial class ImportExportPage : ContentPage
{
    private readonly ImportExportViewModel _viewModel;

    public ImportExportPage(ImportExportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <inheritdoc />
    protected override void OnDisappearing() { _viewModel.RetireBankReview(); base.OnDisappearing(); }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
    }
}
