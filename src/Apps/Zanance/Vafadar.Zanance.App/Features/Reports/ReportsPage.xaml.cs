namespace Vafadar.Zanance.App.Features.Reports;

public partial class ReportsPage : ContentPage
{
    private readonly ReportsViewModel _viewModel;

    public ReportsPage(ReportsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    // Chart axes draw their own text, so Persian digits (D-27) are applied to each label.
    private void OnAxisLabelCreated(object? sender, Syncfusion.Maui.Charts.ChartAxisLabelEventArgs e) =>
        e.Label = Vafadar.Localization.Formatting.NativeDigits.Apply(e.Label) ?? e.Label;
}
