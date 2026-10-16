#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Keeps original native dialog buttons readable and at the application's minimum touch size.</summary>
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
        void Grow(DependencyObject node, bool buttonCaption = false)
        {
            // MAUI's native PromptDialog does not copy the page's direction, unlike AlertDialog.
            if (node is Microsoft.UI.Xaml.Controls.ContentDialog dialog && dialog.FlowDirection != direction)
                dialog.FlowDirection = direction;
            if (node is NativeButton button)
            {
                buttonCaption = true;
                if (button.MinHeight < 44) button.MinHeight = 44;
                if (button.MinWidth < 44) button.MinWidth = 44;
            }
            if (buttonCaption && node is Microsoft.UI.Xaml.Controls.TextBlock text)
            {
                // Native ContentDialog footer columns remain narrow even when the body grows. D-295's
                // German Restore caption exposed clipping without IsTextTrimmed being set. Keep the original
                // button/name/result and let its actual text plus footer row grow instead of shortening it.
                if (text.TextWrapping != TextWrapping.Wrap) text.TextWrapping = TextWrapping.Wrap;
                if (text.TextTrimming != TextTrimming.None) text.TextTrimming = TextTrimming.None;
                if (text.MaxLines != 0) text.MaxLines = 0;
            }
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++) Grow(VisualTreeHelper.GetChild(node, index), buttonCaption);
        }
    }
}
#endif
