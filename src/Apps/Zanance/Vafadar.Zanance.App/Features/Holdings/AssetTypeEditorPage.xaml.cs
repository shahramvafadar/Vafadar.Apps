namespace Vafadar.Zanance.App.Features.Holdings;

public partial class AssetTypeEditorPage : ContentPage
{
    private readonly AssetTypeEditorViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;

    public AssetTypeEditorPage(AssetTypeEditorViewModel viewModel)
    {
        InitializeComponent(); BindingContext = _viewModel = viewModel;
    }
    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing(); if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterAssetTypeEditor(this);
            var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var owner) && owner.OwnsAssetTypeEditorPublication);
        }
        _viewModel.SetViewActive(true); _viewModel.SetNumericPresentationActive(true);
        await Presentation.Failures.GuardAsync(_viewModel.EnsureLoadedAsync);
    }
    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        RetirePublication(); _viewModel.SetNumericPresentationActive(false); base.OnDisappearing();
    }
    /// <summary>Retires the actual original form's reads and native consent on tab/page departure.</summary>
    internal void RetirePublication() => _viewModel.SetViewActive(false);
    /// <summary>Permanently retires an old root and its localization subscription without losing raw input.</summary>
    internal void RetireShellPublication()
    { _shellRetired = true; RetirePublication(); _viewModel.SetNumericPresentationActive(false); }
    // Native Back uses exactly the same owner-bound discard and busy guard as the actual header command.
    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.CanLeave) return true;
        Dispatcher.Dispatch(async () => await Presentation.Failures.GuardAsync(() => _viewModel.CancelCommand.ExecuteAsync(null)));
        return true;
    }
}
