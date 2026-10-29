namespace Vafadar.Zanance.App.Features.Accounts;

public partial class AccountEditorPage : ContentPage, IQueryAttributable
{
    private readonly AccountEditorViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;

    public AccountEditorPage(AccountEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Forwards the native route into the actual platform-independent draft loader.</summary>
    public async void ApplyQueryAttributes(IDictionary<string, object> query) =>
        await Presentation.Failures.GuardAsync(() => _viewModel.ApplyQueryAsync(query));

    /// <summary>Activates only this actual nested form's current native presentation.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing(); if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterAccountEditor(this);
            var root = new WeakReference<AppShell>(shell);
            _viewModel.BindRootOwner(() => root.TryGetTarget(out var owner) && owner.OwnsAccountEditorPublication);
        }
        if (!_shellRetired) _viewModel.SetPresentationActive(true);
    }
    /// <summary>Retires this actual form before accepted tab departure can give another page input.</summary>
    internal void RetirePublication() => _viewModel.SetPresentationActive(false);
    /// <summary>Prevents an old native root from reopening a captured original account form.</summary>
    internal void RetireShellPublication() { _shellRetired = true; RetirePublication(); }
    /// <summary>Retires pending original nested metadata/confirmation callbacks before another page receives input.</summary>
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }

    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.CanLeave) return true;
        // Hardware Back shares the same original-child revision checks as the visible Close/Cancel action.
        Dispatcher.Dispatch(async () => await Presentation.Failures.GuardAsync(() => _viewModel.CancelCommand.ExecuteAsync(null)));
        return true;
    }
}
