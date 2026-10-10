#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using TextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Checks the realized selected caption, inherited typography and native target of actual Windows pickers.</summary>
internal static class DebugPickerText
{
    /// <summary>Measures every character against its actual native slot and the enclosing MAUI picker allocation.</summary>
    internal static object Check(Picker picker, string folder, string name)
    {
        if (picker.Handler?.PlatformView is not ComboBox combo || picker.SelectedIndex < 0
            || picker.SelectedIndex >= picker.Items.Count)
        { throw new InvalidOperationException("The selected picker has no current native choice."); }
        var expected = picker.Items[picker.SelectedIndex];
        var captions = NativeChildren(combo).OfType<TextBlock>().Where(text => text.Text == expected
            && text.ActualWidth > 0 && text.ActualHeight > 0).ToArray();
        if (captions.Length != 1)
        { throw new InvalidOperationException("A selected picker lacks its unique realized complete caption."); }
        var caption = captions[0];
        var slot = LayoutInformation.GetLayoutSlot(caption);
        var parent = VisualTreeHelper.GetParent(caption) as FrameworkElement
            ?? throw new InvalidOperationException("The selected caption has no native layout parent.");
        // WinUI caret positions use the allocated text slot, including alignment space (D-85).
        var origin = parent.TransformToVisual(combo).TransformPoint(new Windows.Foundation.Point(slot.X, slot.Y));
        var glyphs = new List<Windows.Foundation.Rect>();
        for (var offset = 0; offset <= caption.ContentEnd.Offset - caption.ContentStart.Offset; offset++)
        {
            if (caption.ContentStart.GetPositionAtOffset(offset, LogicalDirection.Forward) is { } pointer)
            { glyphs.Add(pointer.GetCharacterRect(LogicalDirection.Forward)); }
        }
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(combo)
            ?? throw new InvalidOperationException("The actual picker has no native automation peer.");
        var spoken = peer.GetName();
        var description = SemanticProperties.GetDescription(picker);
        var proof = new
        {
            kind = "native selected picker caption", expected, spoken, description, picker.SelectedIndex,
            picker.Width, picker.Height, nativeWidth = combo.ActualWidth, nativeHeight = combo.ActualHeight,
            caption.Text, caption.FontSize, caption.IsTextScaleFactorEnabled, caption.IsTextTrimmed,
            font = caption.FontFamily.Source, ownerFont = combo.FontFamily.Source,
            ownerFontSize = combo.FontSize, slot, origin, glyphs,
        };
        if (caption.IsTextTrimmed || !caption.IsTextScaleFactorEnabled
            || caption.FontFamily.Source != combo.FontFamily.Source || Math.Abs(caption.FontSize - combo.FontSize) > .01
            || picker.Width < 44 || picker.Height < 44 || combo.ActualWidth < 44 || combo.ActualHeight < 44
            || combo.ActualWidth > picker.Width + 1 || combo.ActualHeight > picker.Height + 1
            || !string.IsNullOrEmpty(description) && !spoken.Contains(description, StringComparison.Ordinal)
            || glyphs.Count == 0 || glyphs.Min(rect => rect.Left) < -1
            || glyphs.Max(rect => rect.Right) > slot.Width + 1 || glyphs.Max(rect => rect.Bottom) > slot.Height + 1
            || origin.X + glyphs.Min(rect => rect.Left) < -1 || origin.Y + glyphs.Min(rect => rect.Top) < -1
            || origin.X + glyphs.Max(rect => rect.Right) > combo.ActualWidth + 1
            || origin.Y + glyphs.Max(rect => rect.Bottom) > combo.ActualHeight + 1)
        {
            File.WriteAllText(Path.Combine(folder, name + "-picker-caption-failure.json"),
                JsonSerializer.Serialize(proof, new JsonSerializerOptions { WriteIndented = true }));
            throw new InvalidOperationException("A selected picker clips native text, changes typography or loses its complete target/name.");
        }
        return proof;
    }

    private static IEnumerable<DependencyObject> NativeChildren(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var descendant in NativeChildren(child)) { yield return descendant; }
        }
    }

    /// <summary>Checks the actual selected popup row before its native SelectionItem pattern is invoked.</summary>
    internal static object CheckPopup(Picker picker, ComboBoxItem item, string spoken, string folder, string name)
    {
        var combo = picker.Handler?.PlatformView as ComboBox
            ?? throw new InvalidOperationException("The popup picker was retired.");
        var expected = picker.Items[picker.SelectedIndex];
        var caption = NativeChildren(item).OfType<TextBlock>().Single(text => text.Text == expected);
        var slot = LayoutInformation.GetLayoutSlot(caption);
        var parent = VisualTreeHelper.GetParent(caption) as FrameworkElement
            ?? throw new InvalidOperationException("The popup caption has no native parent.");
        var origin = parent.TransformToVisual(item).TransformPoint(new Windows.Foundation.Point(slot.X, slot.Y));
        var glyphs = new List<Windows.Foundation.Rect>();
        for (var offset = 0; offset <= caption.ContentEnd.Offset - caption.ContentStart.Offset; offset++)
        {
            if (caption.ContentStart.GetPositionAtOffset(offset, LogicalDirection.Forward) is { } pointer)
            { glyphs.Add(pointer.GetCharacterRect(LogicalDirection.Forward)); }
        }
        var proof = new { kind = "native selected popup row", expected, spoken, item.ActualWidth, item.ActualHeight,
            caption.FontSize, ownerFontSize = combo.FontSize, font = caption.FontFamily.Source,
            ownerFont = combo.FontFamily.Source, caption.IsTextTrimmed, caption.IsTextScaleFactorEnabled, slot, origin, glyphs };
        if (spoken != expected || item.ActualWidth < 44 || item.ActualHeight < 44
            || caption.IsTextTrimmed || !caption.IsTextScaleFactorEnabled
            || caption.FontFamily.Source != combo.FontFamily.Source || Math.Abs(caption.FontSize - combo.FontSize) > .01
            || glyphs.Count == 0 || glyphs.Min(rect => rect.Left) < -1
            || glyphs.Max(rect => rect.Right) > slot.Width + 1 || glyphs.Max(rect => rect.Bottom) > slot.Height + 1
            || origin.X + glyphs.Min(rect => rect.Left) < -1 || origin.Y + glyphs.Min(rect => rect.Top) < -1
            || origin.X + glyphs.Max(rect => rect.Right) > item.ActualWidth + 1
            || origin.Y + glyphs.Max(rect => rect.Bottom) > item.ActualHeight + 1)
        {
            File.WriteAllText(Path.Combine(folder, name + "-picker-popup-failure.json"),
                JsonSerializer.Serialize(proof, new JsonSerializerOptions { WriteIndented = true }));
            throw new InvalidOperationException("A real picker popup row clips text, changes typography or lacks its full native name/target.");
        }
        return proof;
    }
}
#endif
