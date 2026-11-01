using Android.Content;
using Android.Views;
using AndroidX.Core.View;
using NativeView = Android.Views.View;

namespace Vafadar.Zanance.App.Security;

/// <summary>Temporarily enables keyboard insets only on the actual owned modal window (D-724).</summary>
internal sealed class LockKeyboardViewport : IDisposable
{
    private readonly NativeView _root;
    private readonly IWindowManager _manager;
    private readonly SoftInput _previous;
    private readonly SoftInput _applied;
    private readonly Grid _container;
    private readonly ScrollView _viewport;
    private readonly Thickness _padding;
    private readonly ViewTreeObserver _observer;
    private double _inset;
    private bool _disposed;

    private LockKeyboardViewport(NativeView root, IWindowManager manager, SoftInput previous, SoftInput applied,
        Grid container, ScrollView viewport)
    {
        _root = root;
        _manager = manager;
        _previous = previous;
        _applied = applied;
        _container = container;
        _viewport = viewport;
        _padding = container.Padding;
        _observer = root.ViewTreeObserver!;
        _observer.GlobalLayout += UpdateViewport;
        UpdateViewport(this, EventArgs.Empty);
    }

    /// <summary>Captures the attached page's own window mode without changing the Activity or system settings.</summary>
    internal static LockKeyboardViewport? TryAcquire(NativeView? page, Grid container, ScrollView viewport)
    {
        var root = page?.RootView;
        if (root is not { IsAttachedToWindow: true } || root.LayoutParameters is not WindowManagerLayoutParams attributes ||
            root.Context?.GetSystemService(Context.WindowService) is not IWindowManager manager)
            return null;

        // MAUI's Android modal defaults to adjust=pan. SafeAreaEdges alone then receives
        // no usable IME viewport. Update the actual modal decor, not the hidden Activity.
        var previous = attributes.SoftInputMode;
        var applied = (previous & ~SoftInput.MaskAdjust) | SoftInput.AdjustResize;
        attributes.SoftInputMode = applied;
        manager.UpdateViewLayout(root, attributes);
        root.RequestApplyInsets();
        return new(root, manager, previous, applied, container, viewport);
    }

    private void UpdateViewport(object? sender, EventArgs args)
    {
        if (_disposed || !_root.IsAttachedToWindow || _viewport.Handler?.PlatformView is not NativeView viewport ||
            !viewport.IsAttachedToWindow || !_root.Equals(viewport.RootView)) return;
        var insets = ViewCompat.GetRootWindowInsets(_root);
        if (insets is null) return;
        var density = _root.Resources?.DisplayMetrics?.Density ?? 1f;
        if (density <= 0) return;
        var keyboard = insets.IsVisible(WindowInsetsCompat.Type.Ime())
            ? insets.GetInsets(WindowInsetsCompat.Type.Ime())?.Bottom ?? 0 : 0;
        var location = new int[2];
        _root.GetLocationOnScreen(location);
        var keyboardTop = location[1] + _root.Height - keyboard;
        viewport.GetLocationOnScreen(location);
        // D-724: the modal's MAUI safe area can still omit IME insets with adjust=resize.
        // Compensate only actual remaining occlusion, adding back our last adjustment so
        // a subsequent layout cannot oscillate between padded and unpadded viewports.
        var remaining = keyboard > 0
            ? Math.Max(0, location[1] + viewport.Height + _inset * density - keyboardTop) / density : 0;
        if (Math.Abs(remaining - _inset) < 0.5 / density) return;
        _inset = remaining;
        _container.Padding = new(_padding.Left, _padding.Top, _padding.Right, _padding.Bottom + remaining);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_observer.IsAlive) _observer.GlobalLayout -= UpdateViewport;
        var appliedPadding = new Thickness(_padding.Left, _padding.Top, _padding.Right, _padding.Bottom + _inset);
        if (_container.Padding == appliedPadding) _container.Padding = _padding;
        // Do not overwrite a later owner's mode or attempt to update an already retired window.
        if (!_root.IsAttachedToWindow || _root.LayoutParameters is not WindowManagerLayoutParams attributes ||
            attributes.SoftInputMode != _applied) return;
        attributes.SoftInputMode = _previous;
        _manager.UpdateViewLayout(_root, attributes);
        _root.RequestApplyInsets();
    }
}
