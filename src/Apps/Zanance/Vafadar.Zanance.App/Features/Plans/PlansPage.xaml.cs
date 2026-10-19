namespace Vafadar.Zanance.App.Features.Plans;

public partial class PlansPage : ContentPage
{
    private readonly PlansViewModel _viewModel;
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
        HeaderBack.IsVisible = Navigation.NavigationStack.Count > 1;
        var backIcon = new FluentIcons.Maui.SymbolImageSource
        {
            Symbol = FlowDirection == FlowDirection.RightToLeft ? FluentIcons.Common.Symbol.ArrowRight : FluentIcons.Common.Symbol.ArrowLeft,
            Size = 22,
        };
        backIcon.SetDynamicResource(FontImageSource.ColorProperty, "Primary");
        HeaderBack.Source = backIcon;
        PendingPublication = _viewModel.LoadAsync();
        await Presentation.Failures.GuardAsync(() => PendingPublication);
    }

    /// <summary>Retains original Shell Back when this same titled page is pushed.</summary>
    private void OnHeaderBackClicked(object? sender, EventArgs e) => Shell.Current?.SendBackButtonPressed();
}
