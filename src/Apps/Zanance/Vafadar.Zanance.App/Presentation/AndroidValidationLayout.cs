#if ANDROID
using View = Android.Views.View;
using ViewTreeObserver = Android.Views.ViewTreeObserver;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Waits for one native validation layout without retaining a retired observer or form.</summary>
internal static class AndroidValidationLayout
{
    /// <summary>Requests a bounded native layout and removes the exact listener from its current live tree.</summary>
    internal static async Task<bool> WaitAsync(View native)
    {
        if (native.ViewTreeObserver is not { IsAlive: true } observer) return false;
        var arranged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var listener = new LayoutListener(() => arranged.TrySetResult());
        observer.AddOnGlobalLayoutListener(listener);
        try
        {
            native.RequestLayout();
            await Task.WhenAny(arranged.Task, Task.Delay(TimeSpan.FromSeconds(1)));
            return arranged.Task.IsCompletedSuccessfully;
        }
        finally
        {
            listener.Retire(native);
        }
    }

    /// <summary>Owns the callback independently of Android's observer, which can be merged during attachment.</summary>
    internal sealed class LayoutListener(Action completed) : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private Action? _completed = completed;

        /// <inheritdoc />
        public void OnGlobalLayout() => _completed?.Invoke();

        /// <summary>Also neutralizes a callback already captured by the current native dispatch snapshot.</summary>
        internal void Retire(View native)
        {
            _completed = null;
            // D-480: attachment merges listeners into another tree and kills the initial observer.
            // Removing an event wrapper from the old observer either fails or leaves the listener alive.
            if (native.ViewTreeObserver is { IsAlive: true } current)
                current.RemoveOnGlobalLayoutListener(this);
        }
    }
}
#endif
