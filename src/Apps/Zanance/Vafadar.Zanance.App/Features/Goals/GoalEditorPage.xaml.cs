namespace Vafadar.Zanance.App.Features.Goals;

public partial class GoalEditorPage : ContentPage
{
    private readonly GoalEditorViewModel _viewModel;
    private readonly Vafadar.Localization.ILocalizationService _localization;

    public GoalEditorPage(GoalEditorViewModel viewModel, Vafadar.Localization.ILocalizationService localization)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _localization = localization;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.SetViewActive(true);
        _localization.Changed += OnDisplayChanged;
        await Presentation.Failures.GuardAsync(_viewModel.EnsureLoadedAsync);
    }

    protected override void OnDisappearing()
    {
        _viewModel.SetViewActive(false);
        _localization.Changed -= OnDisplayChanged;
        base.OnDisappearing();
    }

    private void OnDisplayChanged(object? sender, EventArgs args) => _viewModel.RefreshDisplay();

    // Native Back shares the exact discard/busy guard; no second dialog can race a confirmed write.
    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.CanLeave) return true;
        if (!_viewModel.IsDirty) return base.OnBackButtonPressed();
        Dispatcher.Dispatch(() => _viewModel.CancelCommand.Execute(null));
        return true;
    }
}
