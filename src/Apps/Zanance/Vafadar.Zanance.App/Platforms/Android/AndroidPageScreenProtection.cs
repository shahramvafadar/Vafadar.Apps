using System.Runtime.CompilerServices;
using Android.Content;
using Android.Views;
using NativeView = Android.Views.View;

namespace Vafadar.Zanance.App.Security;

/// <summary>Applies the existing screenshot choice to the actual owned MAUI page windows (D-726).</summary>
internal static class AndroidPageScreenProtection
{
    private static readonly ConditionalWeakTable<NativeView, PageObserver> Pages = new();
    private static readonly List<WeakReference<NativeView>> AttachedPages = [];
    private static int _registered;
    private static bool _block = true;

    /// <summary>Registers once before page handlers are created, without changing any system setting.</summary>
    internal static void Register(bool block)
    {
        Volatile.Write(ref _block, block);
        if (Interlocked.Exchange(ref _registered, 1) != 0) return;
        AndroidDialogScreenProtection.Register();
        Microsoft.Maui.Handlers.PageHandler.Mapper.AppendToMapping("ZanancePageScreenProtection", (handler, _) =>
        {
            if (handler.PlatformView is { } page) Pages.GetValue(page, static view => new PageObserver(view));
        });
    }

    /// <summary>Includes the actual native dialog root without replacing its SDK controls or commands.</summary>
    internal static void TrackNativeRoot(NativeView root)
    {
        if (root.Context?.PackageName != Android.App.Application.Context.PackageName ||
            !root.Equals(root.RootView)) return;
        Pages.GetValue(root, static view => new PageObserver(view));
    }

    /// <summary>Updates attached roots on the UI thread when the original preference or background state changes.</summary>
    internal static void Apply(bool block)
    {
        Volatile.Write(ref _block, block);
        AttachedPages.RemoveAll(item => !item.TryGetTarget(out var page) || !page.IsAttachedToWindow);
        var visited = new List<NativeView>();
        foreach (var item in AttachedPages.ToArray())
        {
            // An attached handler keeps its page alive; a decor's managed wrapper can be
            // collected independently. Resolve the actual root again for a policy change.
            if (!item.TryGetTarget(out var page) || RememberRoot(page) is not { } root ||
                visited.Any(previous => previous.Equals(root))) continue;
            visited.Add(root);
            ApplyRoot(root);
        }
    }

    private static NativeView? RememberRoot(NativeView page)
    {
        var root = page.RootView;
        if (root is not { IsAttachedToWindow: true } || root.LayoutParameters is not WindowManagerLayoutParams ||
            root.Context?.PackageName != Android.App.Application.Context.PackageName) return null;
        AttachedPages.RemoveAll(item => !item.TryGetTarget(out var existing) || !existing.IsAttachedToWindow);
        if (!AttachedPages.Any(item => item.TryGetTarget(out var existing) && existing.Equals(page))) AttachedPages.Add(new(page));
        return root;
    }

    private enum ApplicationResult { Current, Updated, Unavailable }

    private static ApplicationResult ApplyRoot(NativeView root)
    {
        if (!root.IsAttachedToWindow) return ApplicationResult.Current;
        var block = Volatile.Read(ref _block);
        if (root.LayoutParameters is not WindowManagerLayoutParams attributes) return ApplicationResult.Unavailable;
        var secure = (attributes.Flags & WindowManagerFlags.Secure) != 0;
        if (secure == block) return ApplicationResult.Current;
        if (root.Context?.GetSystemService(Context.WindowService) is not IWindowManager manager)
            return ApplicationResult.Unavailable;

        // A modal has its own decor/window: the Activity flag does not protect it.
        // Preserve every unrelated flag and the keyboard adapter's existing input mode.
        attributes.Flags = block ? attributes.Flags | WindowManagerFlags.Secure : attributes.Flags & ~WindowManagerFlags.Secure;
        manager.UpdateViewLayout(root, attributes);
        root.RequestLayout();
        return ApplicationResult.Updated;
    }

    /// <summary>Observes only one page's attachment and native pre-draw; native peers stay valid during dispatch.</summary>
    private sealed class PageObserver : Java.Lang.Object, NativeView.IOnAttachStateChangeListener,
        ViewTreeObserver.IOnPreDrawListener
    {
        private readonly WeakReference<NativeView> _page;
        private WeakReference<NativeView>? _root;
        private ViewTreeObserver? _observer;
        private bool _deferFirstDraw;

        /// <summary>Tracks this handler's native page without retaining the managed owner or disposing native peers.</summary>
        internal PageObserver(NativeView page)
        {
            _page = new(page);
            page.AddOnAttachStateChangeListener(this);
            if (page.IsAttachedToWindow) OnViewAttachedToWindow(page);
        }

        /// <inheritdoc />
        public void OnViewAttachedToWindow(NativeView? view)
        {
            if (view?.ViewTreeObserver is not { IsAlive: true } current) return;
            RemoveObserver();
            _observer = current;
            current.AddOnPreDrawListener(this);
            if (RememberRoot(view) is { } root)
            {
                _root = new(root);
                _deferFirstDraw = ApplyRoot(root) == ApplicationResult.Updated;
            }
        }

        /// <inheritdoc />
        public bool OnPreDraw()
        {
            if (!_page.TryGetTarget(out var page) || !page.IsAttachedToWindow) return true;
            var root = _root?.TryGetTarget(out var captured) == true ? captured : RememberRoot(page);
            if (root is null) return !Volatile.Read(ref _block);
            _root ??= new(root);
            var result = ApplyRoot(root);
            // Cancel this draw after a flag change so the next window traversal applies it
            // before content is painted. Missing required protection never enables drawing.
            if (result == ApplicationResult.Updated) return false;
            if (result == ApplicationResult.Unavailable) return !Volatile.Read(ref _block);
            if (!_deferFirstDraw) return true;
            _deferFirstDraw = false;
            root.RequestLayout();
            return false;
        }

        /// <inheritdoc />
        public void OnViewDetachedFromWindow(NativeView? view)
        {
            _root = null;
            RemoveObserver();
        }

        private void RemoveObserver()
        {
            var observer = _observer;
            _observer = null;
            if (observer is { IsAlive: true }) observer.RemoveOnPreDrawListener(this);
            // D-485: do not dispose a Java peer inside a native dispatch snapshot.
            // The weak page registration and native attachment release it together.
        }
    }
}
