namespace Vafadar.Zanance.App.Features.DataFiles;

public partial class ImportExportPage : ContentPage
{
    private readonly ImportExportViewModel _viewModel;
    private bool _appearing;

    public ImportExportPage(ImportExportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _appearing = false;
        _viewModel.LedgerPreviewPageChanged -= OnLedgerPreviewPageChanged;
        _viewModel.RetireBankReview(); base.OnDisappearing();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _appearing = true;
        _viewModel.LedgerPreviewPageChanged -= OnLedgerPreviewPageChanged;
        _viewModel.LedgerPreviewPageChanged += OnLedgerPreviewPageChanged;
        await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
    }

    // D-332: Android retains the old bottom offset after replacing equally tall source cards.
    // Dispatch after publication so the first interpreted row is shown, without moving ordinary status updates.
    private void OnLedgerPreviewPageChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(async () =>
    {
        if (!_appearing || !_viewModel.HasLedgerPreview) return;
        await Presentation.Failures.GuardAsync(() => ImportViewport.ScrollToAsync(LedgerPreviewSection, ScrollToPosition.Start, animated: false));
    });
}
