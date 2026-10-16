namespace Vafadar.Zanance.App.Features.Sharing;

/// <summary>Native original household page; page lifetime cancels visible secrets and stale asynchronous publication.</summary>
public partial class HouseholdPage : ContentPage
{
    private readonly HouseholdViewModel viewModel;
    /// <summary>Constructs native controls only; storage and network work start after appearance.</summary>
    public HouseholdPage(HouseholdViewModel viewModel) { InitializeComponent(); BindingContext = this.viewModel = viewModel; }
    /// <summary>Attaches original first-run completion without replacing the wizard's unsaved choices.</summary>
    public void BeginOnboarding(Func<Task> close, Func<Task> finish) => viewModel.BeginOnboarding(close, finish);
    /// <inheritdoc />
    protected override async void OnAppearing() { base.OnAppearing(); await viewModel.ActivateAsync(); }
    /// <inheritdoc />
    protected override void OnDisappearing() { viewModel.Deactivate(); base.OnDisappearing(); }
    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => !viewModel.CanLeave || base.OnBackButtonPressed();
}
