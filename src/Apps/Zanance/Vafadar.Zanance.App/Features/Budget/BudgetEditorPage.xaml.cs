namespace Vafadar.Zanance.App.Features.Budget;

public partial class BudgetEditorPage : ContentPage
{
    private readonly BudgetEditorViewModel _viewModel;
    private readonly Vafadar.Localization.ILocalizationService _localization;
    private AppShell? _ownerShell;
    private bool _shellRetired;

    public BudgetEditorPage(BudgetEditorViewModel viewModel, Vafadar.Localization.ILocalizationService localization)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _localization = localization;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterBudgetEditor(this);
            var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var owner) && owner.OwnsBudgetEditorPublication);
        }
        _viewModel.SetViewActive(true);
        _localization.Changed -= OnDisplayChanged;
        _localization.Changed += OnDisplayChanged;
        await Presentation.Failures.GuardAsync(_viewModel.EnsureLoadedAsync);
    }

    protected override void OnDisappearing()
    {
        RetirePublication();
        _localization.Changed -= OnDisplayChanged;
        base.OnDisappearing();
    }

    private void OnDisplayChanged(object? sender, EventArgs args) => _viewModel.RefreshDisplay();

    /// <summary>Retires the actual form's pending reads and consent on native tab departure.</summary>
    internal void RetirePublication() => _viewModel.SetViewActive(false);
    /// <summary>Permanently retires an old root without reopening or rebinding its financial draft.</summary>
    internal void RetireShellPublication()
    { _shellRetired = true; _localization.Changed -= OnDisplayChanged; RetirePublication(); }

    // Native Back shares the exact discard/busy guard; no second dialog can race a confirmed write.
    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.CanLeave) return true;
        Dispatcher.Dispatch(async () => await Presentation.Failures.GuardAsync(() => _viewModel.CancelCommand.ExecuteAsync(null)));
        return true;
    }
}
