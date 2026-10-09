#if DEBUG && WINDOWS
using System.Text.Json;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Measures actual native financial rows and persistent actions in the fictitious snapshot review.</summary>
internal static class DebugLayoutChecks
{
    /// <summary>Includes actual growing actions, date inputs and large captions in the own-window review.</summary>
    internal static bool AppliesTo(ContentPage page) => Descendants(page).Any(element =>
        element is Presentation.AmountReadout or Vafadar.Maui.Controls.DateField && IsVisibleThroughParents(element));

    /// <summary>Rejects action overlap or clipped realized financial identities after the native layout pass.</summary>
    internal static void Check(ContentPage page, string folder, string name)
    {
        var evidence = new List<object>();
        if (page.FindByName<Button>("BalanceHeadingAction") is { } heading && IsVisibleThroughParents(heading))
        {
            // D-81: the account link no longer spans the amount; its own heading still needs a full touch target.
            if (heading.Width < 44 || heading.Height < 44 || heading.Command is null
                || string.IsNullOrEmpty(SemanticProperties.GetDescription(heading)))
            { throw new InvalidOperationException("The balance heading lost its account command or usable target."); }
            if (heading.Parent is Grid balanceGrid
                && balanceGrid.Children.OfType<VerticalStackLayout>().FirstOrDefault(child => Grid.GetRow(child) == 1) is { } values
                && heading.Y + heading.Height > values.Y + 1)
            { throw new InvalidOperationException("The account heading covers the amount scroll viewport."); }
            evidence.Add(new { kind = "balance heading", heading.Width, heading.Height,
                spokenName = SemanticProperties.GetDescription(heading), hasCommand = true });
        }
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
        // D-81: compare native glyph width with the real available input width, not a fixed source constant.
        foreach (var field in Descendants(page).OfType<Vafadar.Maui.Controls.DateField>())
        {
            if (!IsVisibleThroughParents(field)) { continue; }
            var boxes = Descendants(field).OfType<Entry>().ToArray();
            if (boxes.Length != 3) { throw new InvalidOperationException("A date field must retain three real native inputs."); }
            foreach (var box in boxes)
            {
                if (box.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox native) { continue; }
                var glyphs = new Microsoft.UI.Xaml.Controls.TextBlock { Text = box.MaxLength == 4 ? "8888" : "88",
                    FontFamily = native.FontFamily, FontSize = native.FontSize, FontWeight = native.FontWeight,
                    FontStyle = native.FontStyle, FontStretch = native.FontStretch, CharacterSpacing = native.CharacterSpacing,
                    IsTextScaleFactorEnabled = native.IsTextScaleFactorEnabled };
                glyphs.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                var available = native.ActualWidth - native.Padding.Left - native.Padding.Right
                    - native.BorderThickness.Left - native.BorderThickness.Right;
                if (available + 1 < glyphs.DesiredSize.Width || box.Width < 44 || box.Height < 44)
                { throw new InvalidOperationException("A date input clips its complete digit count or has an undersized target."); }
                evidence.Add(new { kind = "date input", box.Text, box.MaxLength, box.Width, box.Height,
                    native.FontSize, availableWidth = available, requiredDigitWidth = glyphs.DesiredSize.Width,
                    spokenName = SemanticProperties.GetDescription(box), field.Date });
            }
        }
        foreach (var readout in Descendants(page).OfType<Presentation.AmountReadout>())
        {
            if (!IsVisibleThroughParents(readout)) { continue; }
            var scroll = Descendants(readout).OfType<ScrollView>().Single();
            if (string.IsNullOrEmpty(readout.AmountText))
            {
                if (scroll.IsVisible || Descendants(readout).OfType<Label>().Any(label => label.IsVisible && label.Text == Vafadar.Localization.Translator.Instance["Amount_ScrollHint"]))
                { throw new InvalidOperationException("An empty amount retains a blank scroll area or stale overflow hint."); }
                evidence.Add(new { kind = "empty amount", viewportVisible = scroll.IsVisible });
                continue;
            }
            var label = scroll.Content as Label ?? throw new InvalidOperationException("The real amount caption is missing.");
            if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native) { continue; }
            if (native.IsTextTrimmed || scroll.Width <= 0 || label.Text != readout.AmountText
                || SemanticProperties.GetDescription(label) != readout.AmountText || label.LineBreakMode != LineBreakMode.NoWrap)
            { throw new InvalidOperationException("A large readout loses its full display or spoken packet."); }
            // A detached TextBlock can resolve a bundled font differently from the realized caption. Check the
            // actual rendered caret/glyph boundaries, including every direction mark and the final currency glyph.
            var rendered = new List<Windows.Foundation.Rect>();
            for (var offset = 0; offset <= native.ContentEnd.Offset - native.ContentStart.Offset; offset++)
            {
                var pointer = native.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
                if (pointer is not null) { rendered.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
            }
            if (rendered.Count == 0 || rendered.Any(rect => rect.IsEmpty || rect.Height <= 0)
                || rendered.Min(rect => rect.Left) < -1 || rendered.Max(rect => rect.Right) > native.ActualWidth + 1
                || rendered.Max(rect => rect.Bottom) > native.ActualHeight + 1
                || rendered.Max(rect => rect.Top) - rendered.Min(rect => rect.Top) > 1)
            {
                File.WriteAllText(Path.Combine(folder, name + "-amount-layout-failure.json"),
                    JsonSerializer.Serialize(new { readout.AmountText, label.Width, label.Height, viewportWidth = scroll.Width,
                        native.ActualWidth, native.ActualHeight, native.FontSize, fontFamily = native.FontFamily.Source,
                        native.FlowDirection, native.TextAlignment, native.IsTextScaleFactorEnabled, rendered },
                        new JsonSerializerOptions { WriteIndented = true }));
                throw new InvalidOperationException("A whole amount exceeds its actual rendered content or splits across lines.");
            }
            evidence.Add(new { kind = "large amount", readout.AmountText, label.Width, label.Height, viewportWidth = scroll.Width,
                native.FontSize, native.IsTextTrimmed,
                fullPacketWidth = rendered.Max(rect => rect.Right) - rendered.Min(rect => rect.Left),
                renderedBoundaries = rendered.Count, overflow = label.Width > scroll.Width + 1 });
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
