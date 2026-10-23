#if ANDROID
using View = Android.Views.View;
using ViewTreeObserver = Android.Views.ViewTreeObserver;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Owns the toolbar layout callback separately from the native Shell tracker.</summary>
internal sealed class AndroidToolbarLayoutObserver<TOwner>(TOwner owner, Action<TOwner> layout) : Java.Lang.Object,
    ViewTreeObserver.IOnGlobalLayoutListener, View.IOnAttachStateChangeListener where TOwner : class
{
    private WeakReference<TOwner>? _owner = new(owner);
    private readonly Action<TOwner> _layout = layout;
    private ViewTreeObserver? _observer;
    private bool _registered;

    /// <summary>Observes the first tree and subsequent native attachment changes exactly once.</summary>
    internal void Register(View view)
    {
        if (_registered || _owner is null) throw new InvalidOperationException("Toolbar observation cannot be reused.");
        if (view.ViewTreeObserver is not { IsAlive: true } observer)
            throw new InvalidOperationException("Toolbar layout observer is unavailable.");
        _registered = true;
        _observer = observer;
        view.AddOnAttachStateChangeListener(this);
        observer.AddOnGlobalLayoutListener(this);
    }

    /// <inheritdoc />
    public void OnGlobalLayout()
    {
        if (_observer is not null && _owner?.TryGetTarget(out var value) == true) _layout(value);
    }

    /// <inheritdoc />
    public void OnViewAttachedToWindow(View? view)
    {
        if (_owner is null || view?.ViewTreeObserver is not { IsAlive: true } current) return;
        if (_observer is { } previous)
        {
            if (previous.Equals(current)) return;
            if (!previous.IsAlive)
            {
                // Android already merged a pre-attachment floating observer's listener into this root.
                _observer = current;
                return;
            }
            RemoveFromObservedTree();
        }
        _observer = current;
        current.AddOnGlobalLayoutListener(this);
    }

    /// <inheritdoc />
    public void OnViewDetachedFromWindow(View? view) => RemoveFromObservedTree();

    /// <summary>Stops the exact outgoing registration, even while the view exposes another live tree.</summary>
    private void RemoveFromObservedTree()
    {
        var observer = _observer;
        _observer = null;
        if (observer is { IsAlive: true }) observer.RemoveOnGlobalLayoutListener(this);
    }

    /// <summary>Neutralizes in-flight callbacks and releases both native registrations.</summary>
    internal void Retire(View view)
    {
        _owner = null;
        RemoveFromObservedTree();
        if (((Java.Interop.IJavaPeerable)view).PeerReference.IsValid)
            view.RemoveOnAttachStateChangeListener(this);
        // D-485: leave this neutral Java peer valid for native dispatch snapshots; the bridge
        // releases it after both registrations disappear. Never dispose it inside a callback.
    }
}
#endif
