namespace Vafadar.Zanance.App.Features.Budget;

public partial class BudgetEditorPage : ContentPage
{
    private readonly BudgetEditorViewModel _viewModel;
    private readonly Vafadar.Localization.ILocalizationService _localization;

    public BudgetEditorPage(BudgetEditorViewModel viewModel, Vafadar.Localization.ILocalizationService localization)
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

    // Native Back shares the explicit discard/busy guard and cannot interrupt publication or a financial write.
    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.CanLeave) return true;
        if (!_viewModel.IsDirty) return base.OnBackButtonPressed();
        Dispatcher.Dispatch(() => _viewModel.CancelCommand.Execute(null));
        return true;
    }
}
