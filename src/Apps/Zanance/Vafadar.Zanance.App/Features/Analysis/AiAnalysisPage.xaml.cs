namespace Vafadar.Zanance.App.Features.Analysis;

/// <summary>Original aggregate disclosure lifetime; navigating away retires offers/continuations without erasing private intent.</summary>
public partial class AiAnalysisPage : ContentPage
{
    private readonly AiAnalysisViewModel model;
    /// <summary>Constructs native controls without database/network work.</summary>
    public AiAnalysisPage(AiAnalysisViewModel model) { InitializeComponent(); BindingContext = this.model = model; }
    /// <inheritdoc />
    protected override async void OnAppearing() { base.OnAppearing(); await Presentation.Failures.GuardAsync(model.LoadAsync); }
    /// <inheritdoc />
    protected override void OnDisappearing() { model.Retire(); base.OnDisappearing(); }
}
