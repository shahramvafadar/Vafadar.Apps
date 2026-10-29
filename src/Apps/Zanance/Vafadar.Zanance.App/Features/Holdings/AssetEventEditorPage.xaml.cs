namespace Vafadar.Zanance.App.Features.Holdings;

public partial class AssetEventEditorPage : ContentPage
{
    private readonly AssetEventEditorViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;
    public AssetEventEditorPage(AssetEventEditorViewModel viewModel)
    { InitializeComponent(); BindingContext = _viewModel = viewModel; }
    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing(); if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterAssetEventEditor(this); var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var owner) && owner.OwnsAssetEventEditorPublication);
        }
        _viewModel.SetViewActive(true); _viewModel.SetMoneyPresentationActive(true);
    }
    /// <inheritdoc />
    protected override void OnDisappearing()
    { RetirePublication(); _viewModel.SetMoneyPresentationActive(false); base.OnDisappearing(); }
    /// <summary>Retires only the actual original event frame and its outstanding native consent.</summary>
    internal void RetirePublication() => _viewModel.SetViewActive(false);
    /// <summary>Permanently retires an old native root without retaining its source or localization handler.</summary>
    internal void RetireShellPublication()
    { _shellRetired = true; RetirePublication(); _viewModel.SetMoneyPresentationActive(false); }
    // D-643: native Back follows exactly the same frame/draft/accepted-result consent as the header.
    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.CanLeave) return true;
        Dispatcher.Dispatch(async () => await Presentation.Failures.GuardAsync(() => _viewModel.CancelCommand.ExecuteAsync(null)));
        return true;
    }
}
