namespace Vafadar.Zanance.App.Features.Forecast;

public partial class ForecastPage : ContentPage
{
    private readonly ForecastViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired, _publicationActive;
    private readonly Vafadar.Localization.Formatting.IDateFormatter _dates;

    /// <summary>Gets the actual appearance/navigation read so simultaneous native callbacks share its lifetime.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    public ForecastPage(ForecastViewModel viewModel, Vafadar.Localization.Formatting.IDateFormatter dates)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _dates = dates;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterForecastWorkspace(this); var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var owner) && owner.OwnsForecastWorkspacePublication);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <summary>Refreshes current financial facts on retained-tab activation while keeping the original what-if draft.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_shellRetired) return Task.CompletedTask;
        if (_publicationActive) return PendingPublication;
        _publicationActive = true; _viewModel.SetActive(true); return PendingPublication = PublishAfterRouteAsync();
    }
    private async Task PublishAfterRouteAsync() { await Task.Yield(); if (_publicationActive && !_shellRetired) await _viewModel.LoadAsync(); }
    /// <summary>Retires the actual tab, pending source and native scenario/name prompts together.</summary>
    internal void RetirePublication() { _publicationActive = false; _viewModel.SetActive(false); }
    /// <summary>Permanently retires original native root ownership without discarding process-local raw drafts.</summary>
    internal void RetireShellPublication() { _shellRetired = true; RetirePublication(); }
    /// <inheritdoc />
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => _viewModel.IsBusy || _viewModel.Workspace.IsBusy || base.OnBackButtonPressed();

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
            e.Label = Vafadar.Localization.Formatting.NativeDigits.Apply(_dates.Format(date, Vafadar.Localization.Formatting.DateFormatStyle.DayMonthShort)) ?? e.Label;
        }
    }
}
