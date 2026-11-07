namespace Vafadar.Zanance.App.Features.Plans;

public partial class SettlementPage : ContentPage
{
    private readonly SettlementViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;
    private bool _observingValidation;
    /// <summary>Gets the actual original appearance read so native callers never replay it for observation.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;
    private int _validationVersion;

    public SettlementPage(SettlementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Observes validation only while this form is presented.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterSettlement(this);
        }
        var window = Window; var root = window?.Page;
        // D-938: the original bill may itself be modal, but a different top modal covers its authority.
        _viewModel.SetActive(true, () => !_shellRetired && window is not null && root is not null && ReferenceEquals(Window, window)
            && ReferenceEquals(window.Page, root) && (root.Navigation.ModalStack.LastOrDefault() is { } modal
                ? ReferenceEquals(modal, this) : root is AppShell owner ? ReferenceEquals(owner.CurrentPage, this) : ReferenceEquals(root, this)));
        _observingValidation = true;
        _viewModel.ValidationFailed -= OnValidationFailed;
        _viewModel.ValidationFailed += OnValidationFailed;
        PendingPublication = _viewModel.RefreshForNavigationAsync();
        await Presentation.Failures.GuardAsync(() => PendingPublication);
    }

    /// <summary>Permanently retires original bill reads, native discard and pending writer authority after root replacement.</summary>
    internal void RetireShellPublication() { _shellRetired = true; _viewModel.SetActive(false); }

    /// <summary>Stops original read/command and scroll work when this form leaves the visible navigation lifetime.</summary>
    protected override void OnDisappearing()
    {
        _viewModel.SetActive(false);
        _viewModel.ValidationFailed -= OnValidationFailed;
        _observingValidation = false;
        _validationVersion++;
        base.OnDisappearing();
    }

    // D-102: Save remains reachable at the bottom; reveal the first problem without moving keyboard focus or typing.
    private async void OnValidationFailed(object? sender, EventArgs args) => await Presentation.Failures.GuardAsync(async () =>
    {
        var version = ++_validationVersion;
        VisualElement? target = _viewModel.PeriodError is not null ? ToInput
            : _viewModel.AmountError is not null ? ActualInput : null;
        if (target is null || Handler is null || !_observingValidation) { return; }
        // Clearing earlier errors shortens the form. A queued task alone can still read the old target coordinates.
        if (!await WaitForValidationLayoutAsync() || version != _validationVersion || !_observingValidation
            || Handler is null || target.Handler is null) { return; }
#if WINDOWS
        if (ValidationViewport.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer viewport
            && target.Handler.PlatformView is Microsoft.UI.Xaml.FrameworkElement field)
        {
            var position = field.TransformToVisual(viewport).TransformPoint(new Windows.Foundation.Point());
            var wanted = Math.Clamp(viewport.VerticalOffset + position.Y, 0, viewport.ScrollableHeight);
            // WinUI emits no ViewChanged for an already reached/clamped offset (D-81); do not await a redundant scroll.
            if (Math.Abs(wanted - viewport.VerticalOffset) > 1) { viewport.ChangeView(null, wanted, null, true); }
            return;
        }
#endif
        await ValidationViewport.ScrollToAsync(target, ScrollToPosition.Start, animated: false);
    });

    /// <summary>Waits for the actual error-row layout before resolving a field's new scroll position.</summary>
    private async Task<bool> WaitForValidationLayoutAsync()
    {
#if WINDOWS
        if (ValidationViewport.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement native)
        {
            native.UpdateLayout();
            return true;
        }
#elif ANDROID
        if (ValidationViewport.Handler?.PlatformView is Android.Views.View native)
            return await Presentation.AndroidValidationLayout.WaitAsync(native);
#elif IOS
        if (ValidationViewport.Handler?.PlatformView is UIKit.UIView native)
        {
            native.SetNeedsLayout();
            native.LayoutIfNeeded();
            return true;
        }
#endif
        await Task.Yield();
        return Handler is not null;
    }

    // The Android back button asks before input is lost, like Cancel (CR12).
    protected override bool OnBackButtonPressed() =>
        (BindingContext is Presentation.IUnsavedChanges editor && Presentation.UnsavedChanges.OnBackButton(this, editor)) || base.OnBackButtonPressed();
}
