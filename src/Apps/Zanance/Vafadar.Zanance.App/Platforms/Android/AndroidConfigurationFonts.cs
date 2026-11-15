using Android.App;
using Microsoft.Maui;
using Microsoft.Maui.Platform;

namespace Vafadar.Zanance.App;

/// <summary>Refreshes retained native fonts on text-scale changes and dimensions on density changes.</summary>
internal static class AndroidConfigurationFonts
{
    /// <summary>Reapplies density-dependent bindings without replacing controls, text, focus or the page.</summary>
    internal static void Refresh(Activity activity, bool densityChanged = true, bool revealFocused = false)
    {
        var window = activity.GetWindow();
        if (window?.Content is not IVisualTreeElement root)
        {
            return;
        }

        // Android TextView stores the converted pixel size. A configuration callback does not reapply its original
        // SP/DIP request. Use the handler's public font mapping rather than inferring units from the old pixel size.
        var pending = new Stack<IVisualTreeElement>();
        var visited = new HashSet<IVisualTreeElement>(ReferenceEqualityComparer.Instance);
        pending.Push(root);
        while (pending.TryPop(out var visual))
        {
            if (!IsCurrent(activity, window, root))
            {
                return;
            }

            if (!visited.Add(visual))
            {
                continue;
            }

            if (visual is IElement element && element.Handler is { } handler &&
                ReferenceEquals(handler.VirtualView, element))
            {
                if (element is ITextStyle)
                {
                    handler.UpdateValue(nameof(ITextStyle.Font));
                    if (!IsCurrent(activity, window, root))
                    {
                        return;
                    }
                }

                // Android stores minimum dimensions as pixels too. D1188 retains116px for a44DIP minimum
                // after420->160dpi. Remap explicit positive requests; leave unspecified native defaults alone.
                if (densityChanged && element is IView sizedView && ReferenceEquals(element.Handler, handler) &&
                    ReferenceEquals(handler.VirtualView, element))
                {
                    if (sizedView.MinimumHeight > 0)
                    {
                        handler.UpdateValue(nameof(IView.MinimumHeight));
                        if (!IsCurrent(activity, window, root))
                        {
                            return;
                        }
                    }

                    if (sizedView.MinimumWidth > 0 && ReferenceEquals(element.Handler, handler) &&
                        ReferenceEquals(handler.VirtualView, element))
                    {
                        handler.UpdateValue(nameof(IView.MinimumWidth));
                        if (!IsCurrent(activity, window, root))
                        {
                            return;
                        }
                    }
                }

                if (densityChanged && AndroidEntryConfigurationPadding.NeedsRefresh(element, handler))
                {
                    // Refresh the owned drawable too; its density-dependent insets otherwise remain stale.
                    handler.UpdateValue(nameof(IView.Background));
                    if (!IsCurrent(activity, window, root))
                    {
                        return;
                    }

                    AndroidEntryConfigurationPadding.Refresh(element, handler);
                    if (!IsCurrent(activity, window, root))
                    {
                        return;
                    }
                }
            }

            if (visual is IView view)
            {
                view.InvalidateMeasure();
                if (!IsCurrent(activity, window, root))
                {
                    return;
                }
            }

            // A custom mapping may change the tree. Visit the current children only after it has finished.
            foreach (var child in visual.GetVisualChildren().ToArray())
            {
                pending.Push(child);
            }
        }

        if (revealFocused)
        {
            RevealFocusedAfterLayout(activity, window, root);
        }
    }

    /// <summary>Reveals the same focused control only after the new font layout, without moving focus or changing text.</summary>
    private static void RevealFocusedAfterLayout(Activity activity, IWindow window, IVisualTreeElement root)
    {
        if (!IsCurrent(activity, window, root) || activity.CurrentFocus is not { IsAttachedToWindow: true } focused ||
            activity.Window?.DecorView is not { IsAttachedToWindow: true, HasWindowFocus: true } decor ||
            !decor.Equals(focused.RootView))
        {
            return;
        }

        // D1192: retained ScrollView offsets can clip the focused field after text grows. A pre-draw callback
        // sees its new native dimensions. The listener removes itself on draw/detach; keep only weak owners.
        var activityOwner = new WeakReference<Activity>(activity);
        var windowOwner = new WeakReference<IWindow>(window);
        var rootOwner = new WeakReference<IVisualTreeElement>(root);
        var focusOwner = new WeakReference<Android.Views.View>(focused);
        AndroidX.Core.View.OneShotPreDrawListener.Add(decor, new Java.Lang.Runnable(() =>
        {
            if (!activityOwner.TryGetTarget(out var currentActivity) || currentActivity.Handle == IntPtr.Zero ||
                !windowOwner.TryGetTarget(out var currentWindow) || !rootOwner.TryGetTarget(out var currentRoot) ||
                !focusOwner.TryGetTarget(out var currentFocus) || currentFocus.Handle == IntPtr.Zero ||
                !IsCurrent(currentActivity, currentWindow, currentRoot) ||
                !ReferenceEquals(currentActivity.CurrentFocus, currentFocus) || !currentFocus.IsAttachedToWindow ||
                !currentFocus.IsShown || currentActivity.Window?.DecorView is not { HasWindowFocus: true } currentDecor ||
                !currentDecor.Equals(currentFocus.RootView))
            {
                return;
            }

            using var rectangle = new Android.Graphics.Rect(0, 0, currentFocus.Width, currentFocus.Height);
            currentFocus.RequestRectangleOnScreen(rectangle, true);
        }));
    }

    /// <summary>Rejects a departed window or root after any handler callback.</summary>
    private static bool IsCurrent(Activity activity, IWindow window, IVisualTreeElement root) =>
        ReferenceEquals(activity.GetWindow(), window) && ReferenceEquals(window.Content, root);
}
