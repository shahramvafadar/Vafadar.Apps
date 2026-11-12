using Android.Views;
using AndroidX.Core.View;
using NativeView = Android.Views.View;

namespace Vafadar.Zanance.App.Features.Transactions;

/// <summary>Bounds the original transaction results by actual remaining IME occlusion, without changing window policy.</summary>
internal sealed class TransactionsKeyboardViewport : IDisposable
{
    private readonly NativeView _root;
    private readonly Grid _container;
    private readonly CollectionView _results;
    private readonly Thickness _padding;
    private readonly Action _changed;
    private readonly ViewTreeObserver _observer;
    private double _inset;
    private bool _disposed;

    private TransactionsKeyboardViewport(NativeView root, Grid container, CollectionView results, Action changed)
    {
        _root = root; _container = container; _results = results; _changed = changed;
        _padding = container.Padding;
        _observer = root.ViewTreeObserver!;
        _observer.GlobalLayout += UpdateViewport;
        UpdateViewport(this, EventArgs.Empty);
    }

    /// <summary>Observes only the attached original page's root; no global inset listener or Activity setting is replaced.</summary>
    internal static TransactionsKeyboardViewport? TryAcquire(NativeView page, Grid container, CollectionView results, Action changed)
        => page.RootView is { IsAttachedToWindow: true } root ? new(root, container, results, changed) : null;

    private void UpdateViewport(object? sender, EventArgs args)
    {
        if (_disposed || !_root.IsAttachedToWindow || _results.Handler?.PlatformView is not NativeView results ||
            !results.IsAttachedToWindow || !_root.Equals(results.RootView)) return;
        var insets = ViewCompat.GetRootWindowInsets(_root);
        var density = _root.Resources?.DisplayMetrics?.Density ?? 1f;
        if (insets is null || density <= 0) return;
        var keyboard = insets.IsVisible(WindowInsetsCompat.Type.Ime())
            ? insets.GetInsets(WindowInsetsCompat.Type.Ime())?.Bottom ?? 0 : 0;
        var location = new int[2];
        _root.GetLocationOnScreen(location);
        var keyboardTop = location[1] + _root.Height - keyboard;
        results.GetLocationOnScreen(location);
        // D-1124: edge-to-edge layout can retain a full-height result viewport behind the real IME.
        // Add back only our previous compensation so repeated layouts do not alternate padding.
        var remaining = keyboard > 0
            ? Math.Max(0, location[1] + results.Height + _inset * density - keyboardTop) / density : 0;
        if (Math.Abs(remaining - _inset) < 0.5 / density) return;
        _inset = remaining;
        _container.Padding = new(_padding.Left, _padding.Top, _padding.Right, _padding.Bottom + remaining);
        _changed();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_observer.IsAlive) _observer.GlobalLayout -= UpdateViewport;
        var applied = new Thickness(_padding.Left, _padding.Top, _padding.Right, _padding.Bottom + _inset);
        if (_container.Padding != applied) return;
        _container.Padding = _padding;
        _changed();
    }
}
