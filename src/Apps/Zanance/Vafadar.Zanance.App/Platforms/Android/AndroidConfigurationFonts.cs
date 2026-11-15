using Android.App;
using Microsoft.Maui;
using Microsoft.Maui.Platform;

namespace Vafadar.Zanance.App;

/// <summary>Refreshes existing native text sizes after Android changes the retained activity's density.</summary>
internal static class AndroidConfigurationFonts
{
    /// <summary>Reapplies current font bindings without replacing controls, text, focus or the page.</summary>
    internal static void Refresh(Activity activity)
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
                ReferenceEquals(handler.VirtualView, element) && element is ITextStyle)
            {
                handler.UpdateValue(nameof(ITextStyle.Font));
                if (!IsCurrent(activity, window, root))
                {
                    return;
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
    }

    /// <summary>Rejects a departed window or root after any handler callback.</summary>
    private static bool IsCurrent(Activity activity, IWindow window, IVisualTreeElement root) =>
        ReferenceEquals(activity.GetWindow(), window) && ReferenceEquals(window.Content, root);
}
