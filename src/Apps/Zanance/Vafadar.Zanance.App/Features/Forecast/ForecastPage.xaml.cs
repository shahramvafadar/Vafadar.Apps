namespace Vafadar.Zanance.App.Features.Forecast;

public partial class ForecastPage : ContentPage
{
    private readonly ForecastViewModel _viewModel;
    private readonly Vafadar.Localization.Formatting.IDateFormatter _dates;

    public ForecastPage(ForecastViewModel viewModel, Vafadar.Localization.Formatting.IDateFormatter dates)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _dates = dates;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Presentation.Failures.GuardAsync(_viewModel.LoadAsync);
    }

    // Chart axes draw their own text, so Persian digits (D-27) are applied to each label.
    private void OnAxisLabelCreated(object? sender, Syncfusion.Maui.Charts.ChartAxisLabelEventArgs e) =>
        e.Label = Vafadar.Localization.Formatting.NativeDigits.Apply(e.Label) ?? e.Label;

    // The chart formats dates with its own culture, which can differ from the app language and ignores the chosen
    // calendar; the app's formatter writes "3 Mehr" or "October 25" as everywhere else.
    private void OnDateLabelCreated(object? sender, Syncfusion.Maui.Charts.ChartAxisLabelEventArgs e)
    {
        // A position outside the date range keeps the chart's own label instead of breaking the chart.
        if (e.Position is >= -657435.0 and <= 2958465.99999999)
        {
            var date = DateOnly.FromDateTime(DateTime.FromOADate(e.Position));
            e.Label = Vafadar.Localization.Formatting.NativeDigits.Apply(_dates.Format(date, Vafadar.Localization.Formatting.DateFormatStyle.DayMonth)) ?? e.Label;
        }
    }
}
