namespace Vafadar.Zanance.App.Features.Entries;

public partial class EntryEditorPage : ContentPage
{
    private readonly EntryEditorViewModel _viewModel;
    private bool _observingValidation;
    private int _validationVersion;

    public EntryEditorPage(EntryEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _observingValidation = true;
        _viewModel.ValidationFailed -= OnValidationFailed;
        _viewModel.ValidationFailed += OnValidationFailed;

        // Quick entry (UX-04): the amount is the first thing to type.
        if (string.IsNullOrEmpty(_viewModel.AmountText))
        {
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(350), () => AmountEntry.Focus());
        }
    }

    /// <summary>Stops scroll requests when this form leaves the visible navigation lifetime.</summary>
    protected override void OnDisappearing()
    {
        _viewModel.ValidationFailed -= OnValidationFailed;
        _observingValidation = false;
        _validationVersion++;
        base.OnDisappearing();
    }

    // D-100: Save remains reachable at the bottom; reveal the first problem without moving keyboard focus or typing.
    private async void OnValidationFailed(object? sender, EventArgs args) => await Presentation.Failures.GuardAsync(async () =>
    {
        var version = ++_validationVersion;
        VisualElement? target = _viewModel.AmountError is not null ? AmountEntry
            : _viewModel.AccountError is not null ? AccountInput
            : _viewModel.ToAccountError is not null ? ToAccountInput
            : _viewModel.ToAmountError is not null ? ToAmountInput
            : _viewModel.FeeError is not null ? FeeInput
            : _viewModel.DestinationFeeError is not null ? DestinationFeeInput
            : _viewModel.ReimbursableError is not null ? ReimbursableInput
            : _viewModel.ForeignAmountError is not null || _viewModel.ForeignCurrencyError is not null ? ForeignInput : null;
        if (target is null || Handler is null || !_observingValidation) { return; }
        // Clearing earlier errors shortens the form. A queued task alone can still read the old target coordinates.
        if (!await WaitForValidationLayoutAsync() || version != _validationVersion || !_observingValidation
            || Handler is null || target.Handler is null) { return; }
#if WINDOWS
        if (ValidationViewport.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer viewport
            && target.Handler.PlatformView is Microsoft.UI.Xaml.FrameworkElement field)
        {
            var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            // D-100: a focused text caret can enqueue its own bring-into-view during error/Save layout changes.
            // Queue the validation request after those native updates; let WinUI resolve the complete target itself.
            // No focus replacement or persistent keyboard/scroll policy change is needed.
            if (!viewport.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                try
                {
                    if (version == _validationVersion && _observingValidation && Handler is not null && target.Handler is not null)
                    {
                        viewport.UpdateLayout();
                        field.StartBringIntoView(new Microsoft.UI.Xaml.BringIntoViewOptions
                            { AnimationDesired = false, VerticalAlignmentRatio = 0 });
                    }
                    requested.TrySetResult();
                }
                // The native callback runs outside GuardAsync's stack; deliver a failure through its awaited task.
                catch (Exception error) when (error is not OutOfMemoryException) { requested.TrySetException(error); }
            })) { return; }
            if (await Task.WhenAny(requested.Task, Task.Delay(TimeSpan.FromSeconds(1))) == requested.Task)
            { await requested.Task; }
            else if (version == _validationVersion) { _validationVersion++; }
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
            await Task.Yield();
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
