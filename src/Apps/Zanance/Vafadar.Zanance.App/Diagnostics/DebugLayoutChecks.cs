#if DEBUG && WINDOWS
using System.Text.Json;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Measures actual native financial rows and persistent actions in the fictitious snapshot review.</summary>
internal static class DebugLayoutChecks
{
    /// <summary>Rejects action overlap or clipped realized financial identities after the native layout pass.</summary>
    internal static void Check(ContentPage page, string folder, string name)
    {
        var evidence = new List<object>();
        if (page.FindByName<VisualElement>("ContentViewport") is { IsVisible: true } viewport)
        {
            foreach (var actionName in new[] { "AddAction", "BulkActions", "UndoNotice" })
            {
                if (page.FindByName<VisualElement>(actionName) is not { IsVisible: true } action) { continue; }
                // D-78: both siblings are measured in their common grid, so scrolling cannot hide overlap.
                if (viewport.Width <= 0 || viewport.Height <= 0 || action.Height < 44
                    || action.Y < viewport.Y + viewport.Height - 1)
                {
                    File.WriteAllText(Path.Combine(folder, name + "-action-layout-failure.json"),
                        JsonSerializer.Serialize(new { actionName, viewport.Width, viewport.Height, viewport.Y,
                            actionTop = action.Y, actionHeight = action.Height }, new JsonSerializerOptions { WriteIndented = true }));
                    throw new InvalidOperationException("A persistent action covers its viewport or has no usable target.");
                }
                evidence.Add(new { kind = "action dock", actionName, viewport.Width, viewport.Height,
                    viewportBottom = viewport.Y + viewport.Height, actionTop = action.Y, actionHeight = action.Height });
            }
        }

        foreach (var row in Descendants(page).OfType<Grid>())
        {
            if (!IsVisibleThroughParents(row)) { continue; }
            if (row.BindingContext is not (Presentation.EntryRow or Features.Accounts.AccountItem or Features.Holdings.HoldingRow)) { continue; }
            var identity = row.Children.OfType<VerticalStackLayout>().FirstOrDefault(child => Grid.GetColumn(child) == 1 && Grid.GetRow(child) == 0);
            var title = identity?.Children.OfType<Label>().FirstOrDefault();
            if (title?.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text) { continue; }
            var amount = row.Children.OfType<VisualElement>().FirstOrDefault(child => Grid.GetColumn(child) == 1 && Grid.GetRow(child) == 1);
            if (amount is null || identity is null) { continue; }
            // A realized identity must have useful width, and its amount must start after the whole identity stack.
            if (text.IsTextTrimmed || title.Width <= 0 || identity.Width < row.Width * .4
                || amount.Y < identity.Y + identity.Height - 1)
            {
                File.WriteAllText(Path.Combine(folder, name + "-financial-layout-failure.json"),
                    JsonSerializer.Serialize(new { type = row.BindingContext.GetType().Name, title.Text, text.IsTextTrimmed,
                        row.Width, row.Height, row.IsVisible, identityWidth = identity.Width, titleWidth = title.Width,
                        identityTop = identity.Y, identityHeight = identity.Height, amountTop = amount.Y, amountHeight = amount.Height },
                        new JsonSerializerOptions { WriteIndented = true }));
                throw new InvalidOperationException("A financial row clips its identity or overlaps its amount.");
            }
            evidence.Add(new { kind = "financial row", type = row.BindingContext.GetType().Name, title.Text,
                text.FontSize, text.IsTextScaleFactorEnabled, text.IsTextTrimmed, row.Width,
                identityWidth = identity.Width, identityBottom = identity.Y + identity.Height, amountTop = amount.Y });
        }

        // D-80: check the actual caption and last native command button, never a source-text mirror.
        foreach (var action in Descendants(page).OfType<Presentation.WrappingAction>())
        {
            if (!IsVisibleThroughParents(action)) { continue; }
            if (action.Content is not Grid grid || grid.Children.FirstOrDefault() is not Border { Content: Label caption }
                || grid.Children.LastOrDefault() is not Button button
                || caption.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text)
            { throw new InvalidOperationException("A growing action lacks its actual caption/native button."); }
            if (text.IsTextTrimmed || caption.Width <= 0 || action.Width < 44 || action.Height < 44
                || text.ActualHeight + 1 < text.DesiredSize.Height
                || button.Command != action.Command || button.CommandParameter != action.CommandParameter
                || SemanticProperties.GetDescription(button) != action.Text)
            { throw new InvalidOperationException("A growing action clips its caption or loses its command/name."); }
            evidence.Add(new { kind = "wrapping action", action.Text, action.Appearance, action.Width, action.Height,
                captionWidth = caption.Width, captionHeight = caption.Height, text.IsTextTrimmed,
                text.FontSize, nativeButtonEnabled = button.IsEnabled, spokenName = SemanticProperties.GetDescription(button) });
        }
        File.WriteAllText(Path.Combine(folder, name + "-layout-checks.json"),
            JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static IEnumerable<VisualElement> Descendants(IVisualTreeElement root)
    {
        foreach (var child in root.GetVisualChildren())
        {
            if (child is VisualElement visual) { yield return visual; }
            foreach (var descendant in Descendants(child)) { yield return descendant; }
        }
    }

    private static bool IsVisibleThroughParents(VisualElement element)
    {
        // Hidden Home sections still contain handlers but have never been measured; they are not native row evidence.
        for (Element? current = element; current is not null; current = current.Parent)
        {
            if (current is VisualElement { IsVisible: false }) { return false; }
        }
        return true;
    }
}
#endif
