namespace Vafadar.Zanance.App.Features.Plans;

public partial class PlanEditorPage : ContentPage
{
    private readonly PlanEditorViewModel _viewModel;
    private bool _observingValidation;
    private int _validationVersion;

    public PlanEditorPage(PlanEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Observes validation only while this form is presented.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _observingValidation = true;
        _viewModel.ValidationFailed -= OnValidationFailed;
        _viewModel.ValidationFailed += OnValidationFailed;
    }

    /// <summary>Stops scroll requests when this form leaves the visible navigation lifetime.</summary>
    protected override void OnDisappearing()
    {
        _viewModel.ValidationFailed -= OnValidationFailed;
        _observingValidation = false;
        _validationVersion++;
        base.OnDisappearing();
    }

    // D-99: Save remains reachable at the bottom; reveal the first problem without moving keyboard focus or typing.
    private async void OnValidationFailed(object? sender, EventArgs args) => await Presentation.Failures.GuardAsync(async () =>
    {
        var version = ++_validationVersion;
        VisualElement? target = _viewModel.NameError is not null ? NameInput
            : _viewModel.AmountError is not null ? AmountInput
            : _viewModel.AccountError is not null ? AccountInput
            : _viewModel.ToAccountError is not null ? ToAccountInput
            : _viewModel.ToAmountError is not null ? ToAmountInput
            : _viewModel.RuleError is not null ? RuleProblem : null;
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
        if (ValidationViewport.Handler?.PlatformView is Android.Views.View native
            && native.ViewTreeObserver is { IsAlive: true } observer)
        {
            var arranged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler completed = (_, _) => arranged.TrySetResult();
            observer.GlobalLayout += completed;
            try
            {
                native.RequestLayout();
                await Task.WhenAny(arranged.Task, Task.Delay(TimeSpan.FromSeconds(1)));
                return arranged.Task.IsCompletedSuccessfully;
            }
            finally
            {
                if (observer.IsAlive) { observer.GlobalLayout -= completed; }
            }
        }
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

    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.IsDirty)
        {
            return base.OnBackButtonPressed();
        }

        Dispatcher.Dispatch(async () =>
        {
            if (await _viewModel.ConfirmDiscardAsync())
            {
                await Shell.Current.GoToAsync("..");
            }
        });
        return true;
    }
}
