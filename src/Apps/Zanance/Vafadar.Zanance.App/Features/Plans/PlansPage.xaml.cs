namespace Vafadar.Zanance.App.Features.Plans;

public partial class PlansPage : ContentPage
{
    private readonly PlansViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;
    private bool _publicationActive;
    /// <summary>Exposes the original appearance read so native review never races a second load.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    public PlansPage(PlansViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterPlanCenter(this);
        }
        HeaderBack.IsVisible = Navigation.NavigationStack.Count > 1;
        var backIcon = new FluentIcons.Maui.SymbolImageSource
        {
            Symbol = FlowDirection == FlowDirection.RightToLeft ? FluentIcons.Common.Symbol.ArrowRight : FluentIcons.Common.Symbol.ArrowLeft,
            Size = 22,
        };
        backIcon.SetDynamicResource(FontImageSource.ColorProperty, "Primary");
        HeaderBack.Source = backIcon;
        PendingPublication = RefreshForNavigationAsync();
        await Presentation.Failures.GuardAsync(() => PendingPublication);
    }

    /// <summary>Shares the current original appearance/retained-tab loader instead of starting an independent publication.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_shellRetired) return Task.CompletedTask;
        // Android can finish appearance before Shell reports navigation. Both callbacks belong to the
        // same presence: a second automatic read must not erase its failure before explicit Retry (D-633).
        if (_publicationActive) return PendingPublication;
        _publicationActive = true;
        _viewModel.SetActive(true);
        return PendingPublication = _viewModel.LoadAsync();
    }
    /// <summary>Retires source, retained rows and native permission continuations on actual departure.</summary>
    internal void RetirePublication() { _publicationActive = false; _viewModel.SetActive(false); }
    /// <summary>Prevents a replaced native root from publishing or navigating through old row objects.</summary>
    internal void RetireShellPublication() { _shellRetired = true; RetirePublication(); }
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
    /// <summary>Retains original Shell Back when this same titled page is pushed.</summary>
    private void OnHeaderBackClicked(object? sender, EventArgs e) => Shell.Current?.SendBackButtonPressed();
}
