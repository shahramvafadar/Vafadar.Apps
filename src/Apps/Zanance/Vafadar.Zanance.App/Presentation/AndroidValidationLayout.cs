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
        var arranged = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var listener = new LayoutListener(() => arranged.TrySetResult(true), () => arranged.TrySetResult(false));
        try
        {
            if (!listener.Register(native)) return false;
            native.RequestLayout();
            await Task.WhenAny(arranged.Task, Task.Delay(TimeSpan.FromSeconds(1)));
            return arranged.Task.IsCompletedSuccessfully && await arranged.Task
                && ((Java.Interop.IJavaPeerable)native).PeerReference.IsValid && native.IsAttachedToWindow;
        }
        finally
        {
            listener.Retire(native);
        }
    }

    /// <summary>Owns the callback independently of Android's observer, which can be merged during attachment.</summary>
    internal sealed class LayoutListener(Action completed, Action? detached = null) : Java.Lang.Object,
        ViewTreeObserver.IOnGlobalLayoutListener
    {
        private Action? _completed = completed;
        private Action? _detached = detached;
        private readonly List<ViewTreeObserver> _observers = [];
        private AttachmentListener? _attachment;
        private bool _retired;

        /// <summary>Tracks the initial tree and subsequent attachment merges without adding duplicate global callbacks.</summary>
        internal bool Register(View native)
        {
            if (_retired || _observers.Count != 0) throw new InvalidOperationException("Layout listener cannot be reused.");
            if (!((Java.Interop.IJavaPeerable)native).PeerReference.IsValid
                || native.ViewTreeObserver is not { IsAlive: true } observer) return false;
            Capture(observer);
            _attachment = new AttachmentListener(this);
            native.AddOnAttachStateChangeListener(_attachment);
            observer.AddOnGlobalLayoutListener(this);
            return true;
        }

        /// <summary>Keeps the observer that receives the initial floating tree's listeners during attachment.</summary>
        private void Attached(View? view)
        {
            if (!_retired && view?.ViewTreeObserver is { IsAlive: true } observer) Capture(observer);
        }

        /// <summary>Retains the outgoing live tree and ends a wait whose original native target has left its window.</summary>
        private void Detached(View? view)
        {
            if (_retired) return;
            if (view?.ViewTreeObserver is { IsAlive: true } observer) Capture(observer);
            _detached?.Invoke();
        }

        /// <summary>Retains distinct native observer identities only for this bounded registration lifetime.</summary>
        private void Capture(ViewTreeObserver observer)
        {
            if (!_observers.Any(value => ((Java.Interop.IJavaPeerable)value).PeerReference.IsValid && value.Equals(observer)))
                _observers.Add(observer);
        }

        /// <inheritdoc />
        public void OnGlobalLayout() => _completed?.Invoke();

        /// <summary>Also neutralizes a callback already captured by the current native dispatch snapshot.</summary>
        internal void Retire(View native)
        {
            if (_retired) return;
            _completed = null;
            _detached = null;
            _retired = true;
            _attachment?.Detach();
            if (((Java.Interop.IJavaPeerable)native).PeerReference.IsValid)
            {
                if (_attachment is { } attachment) native.RemoveOnAttachStateChangeListener(attachment);
                if (native.ViewTreeObserver is { IsAlive: true } current) Capture(current);
            }
            // D-482: detachment can leave the registered root alive while the view returns a new floating tree.
            // Remove from every tracked live tree before disposing this Java peer, not just the current one.
            foreach (var observer in _observers)
                if (((Java.Interop.IJavaPeerable)observer).PeerReference.IsValid && observer.IsAlive)
                    observer.RemoveOnGlobalLayoutListener(this);
            _observers.Clear();
            // A detached or disposed view may still have a native dispatch snapshot. Keep its weak,
            // neutral attachment peer valid until the platform/bridge releases it (as in D-450).
            _attachment = null;
        }

        /// <summary>Keeps attachment callbacks valid independently of the disposed global-layout listener.</summary>
        private sealed class AttachmentListener(LayoutListener owner) : Java.Lang.Object, View.IOnAttachStateChangeListener
        {
            private WeakReference<LayoutListener>? _owner = new(owner);

            /// <inheritdoc />
            public void OnViewAttachedToWindow(View? view)
            {
                if (_owner?.TryGetTarget(out var listener) == true) listener.Attached(view);
            }

            /// <inheritdoc />
            public void OnViewDetachedFromWindow(View? view)
            {
                if (_owner?.TryGetTarget(out var listener) == true) listener.Detached(view);
            }

            /// <summary>Neutralizes a callback already retained by a native attachment dispatch.</summary>
            internal void Detach() => _owner = null;
        }
    }
}
#endif
