namespace Vafadar.Zanance.App.Features.Plans;

public partial class OccurrencePage : ContentPage
{
    private readonly OccurrenceViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;
    private bool _observingValidation;
    private int _validationVersion;
    /// <summary>Gets the original appearance load so native callers await its actual lifetime rather than starting another read.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    public OccurrencePage(OccurrenceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterOccurrence(this);
        }
        _observingValidation = true;
        _viewModel.ValidationFailed -= OnValidationFailed;
        _viewModel.ValidationFailed += OnValidationFailed;
        PendingPublication = RefreshForNavigationAsync();
        await Presentation.Failures.GuardAsync(() => PendingPublication);
    }

    /// <summary>Shares the original covered read after appearance or retained Shell-tab activation.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_shellRetired) return Task.CompletedTask;
        var window = Window; var root = window?.Page;
        // D-937: CurrentPage can remain this page beneath a lock or other modal.
        // Retained-tab refresh and accepted notices must not cross that visible cover.
        _viewModel.SetActive(true, () => !_shellRetired && window is not null && root is not null && ReferenceEquals(Window, window)
            && ReferenceEquals(window.Page, root) && root.Navigation.ModalStack.Count == 0
            && (root is AppShell shell ? ReferenceEquals(shell.CurrentPage, this) : ReferenceEquals(root, this)),
            () => !_shellRetired && window is not null && root is not null && ReferenceEquals(window.Page, root)
                && root.Navigation.ModalStack.Count == 0);
        return PendingPublication = _viewModel.RefreshForNavigationAsync();
    }

    /// <summary>Permanently retires this original root's read and pending native consent ownership.</summary>
    internal void RetireShellPublication() { _shellRetired = true; _viewModel.SetActive(false); }

    /// <summary>Stops scroll requests when this form leaves the visible navigation lifetime.</summary>
    protected override void OnDisappearing()
    {
        _viewModel.SetActive(false);
        _viewModel.ValidationFailed -= OnValidationFailed;
        _observingValidation = false;
        _validationVersion++;
        base.OnDisappearing();
    }

    // D-103: reveal the input of the actual attempted action without replacing the draft or moving desktop focus.
    private async void OnValidationFailed(OccurrenceInput input) => await Presentation.Failures.GuardAsync(async () =>
    {
        var version = ++_validationVersion;
        VisualElement target = input == OccurrenceInput.Payment ? PaymentInput : OverrideInput;
        if (Handler is null || !_observingValidation) { return; }
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

}
