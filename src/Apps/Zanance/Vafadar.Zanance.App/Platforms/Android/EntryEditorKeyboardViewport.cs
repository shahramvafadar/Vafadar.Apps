using Android.Views;
using AndroidX.Core.View;
using NativeView = Android.Views.View;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>Keeps the original entry footer above its own modal's IME without changing window or keyboard policy.</summary>
internal sealed class EntryEditorKeyboardViewport : IDisposable
{
    private readonly NativeView _root;
    private readonly Grid _container;
    private readonly VisualElement _footer;
    private readonly Func<bool> _ownsForm;
    private readonly Thickness _padding;
    private readonly ViewTreeObserver _observer;
    private double _inset;
    private bool _disposed;

    private EntryEditorKeyboardViewport(NativeView root, Grid container, VisualElement footer, Func<bool> ownsForm)
    {
        _root = root; _container = container; _footer = footer; _ownsForm = ownsForm;
        _padding = container.Padding;
        _observer = root.ViewTreeObserver!;
        _observer.GlobalLayout += UpdateViewport;
        try { UpdateViewport(this, EventArgs.Empty); }
        catch (Exception initial)
        {
            // A failed first layout has no caller-owned lease to release its native listener.
            try { Dispose(); }
            catch (Exception cleanup) { throw new AggregateException(initial, cleanup); }
            throw;
        }
    }

    /// <summary>Captures only an attached original form's native root and current padding.</summary>
    internal static EntryEditorKeyboardViewport? TryAcquire(NativeView page, Grid container, VisualElement footer,
        Func<bool> ownsForm)
        => page.RootView is { IsAttachedToWindow: true } root ? new(root, container, footer, ownsForm) : null;

    private void UpdateViewport(object? sender, EventArgs args)
    {
        if (_disposed || !_ownsForm() || !_root.IsAttachedToWindow ||
            _footer.Handler?.PlatformView is not NativeView footer || !footer.IsAttachedToWindow ||
            !footer.IsShown || !_root.Equals(footer.RootView)) return;
        var insets = ViewCompat.GetRootWindowInsets(_root);
        var density = _root.Resources?.DisplayMetrics?.Density ?? 1f;
        if (insets is null || density <= 0) return;
        var keyboard = insets.IsVisible(WindowInsetsCompat.Type.Ime())
            ? insets.GetInsets(WindowInsetsCompat.Type.Ime())?.Bottom ?? 0 : 0;
        var rootLocation = new int[2];
        _root.GetLocationOnScreen(rootLocation);
        var keyboardTop = rootLocation[1] + _root.Height - keyboard;
        var footerLocation = new int[2];
        footer.GetLocationOnScreen(footerLocation);
        // D-1136: MAUI modal adjust=pan can leave the footer geometrically behind the IME.
        // Add back only this lease's previous compensation to avoid alternating layout padding.
        var remaining = keyboard > 0
            ? Math.Max(0, footerLocation[1] + footer.Height + _inset * density - keyboardTop) / density : 0;
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
        var applied = new Thickness(_padding.Left, _padding.Top, _padding.Right, _padding.Bottom + _inset);
        if (_container.Padding == applied) _container.Padding = _padding;
    }
}
