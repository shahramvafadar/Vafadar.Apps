#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Keeps original native dialog buttons at the application's minimum touch size.</summary>
internal static class WindowsDialogTargets
{
    /// <summary>Observes only this window's transient popup while the original operation is outstanding.</summary>
    internal static async Task<T> ShowAsync<T>(Page page, Func<Task<T>> show)
    {
        if (page.Window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content: FrameworkElement content })
            return await show();
        var direction = page.Window.Page?.FlowDirection == Microsoft.Maui.FlowDirection.RightToLeft
            ? Microsoft.UI.Xaml.FlowDirection.RightToLeft : Microsoft.UI.Xaml.FlowDirection.LeftToRight;
        // D-294: MAUI action sheets are Flyouts, while prompts/confirmations use native subclasses with
        // their own styles. A single footer resource cannot enlarge all 32-unit targets. Preserve their
        // original results, templates and commands; remove this scoped observer after dismissal/failure.
        content.LayoutUpdated += Update;
        try { var operation = show(); Update(null, null!); return await operation; }
        finally { content.LayoutUpdated -= Update; }

        void Update(object? sender, object args)
        {
            if (content.XamlRoot is not { } root) return;
            foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(root))
                if (popup.Child is { } child) Grow(child);
        }
        void Grow(DependencyObject node)
        {
            // MAUI's native PromptDialog does not copy the page's direction, unlike AlertDialog.
            if (node is Microsoft.UI.Xaml.Controls.ContentDialog dialog && dialog.FlowDirection != direction)
                dialog.FlowDirection = direction;
            if (node is NativeButton button)
            {
                if (button.MinHeight < 44) button.MinHeight = 44;
                if (button.MinWidth < 44) button.MinWidth = 44;
            }
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++) Grow(VisualTreeHelper.GetChild(node, index));
        }
    }
}
#endif
