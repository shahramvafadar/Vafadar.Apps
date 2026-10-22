#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Page = Microsoft.Maui.Controls.Page;
using ScrollView = Microsoft.Maui.Controls.ScrollView;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Reads the original selected/popup caption on the application's isolated desktop without choosing another row.</summary>
    private static async Task ObserveHouseholdCaptionWindowsAsync(App app, Page page, Picker picker, string stage, bool displayOnly,
        string folder, string priorObservations, Func<VisualElement, Task> scroll, List<object> observations, List<object> captures)
    {
        await PlanEditorNativeReview.WaitAsync(() => picker.Handler?.PlatformView is ComboBox { IsLoaded: true, ActualWidth: >= 44, ActualHeight: >= 44 }
            && picker.SelectedIndex >= 0 && picker.SelectedIndex < picker.Items.Count, CancellationToken.None);
        ScrollViewer? viewport = null;
        for (Element? parent = picker.Parent; parent is not null; parent = parent.Parent)
            if (parent is ScrollView { Handler.PlatformView: ScrollViewer found }) { viewport = found; break; }
        if (viewport is null) throw new IOException("Original household scroll viewport missing.");
        async Task<Windows.Foundation.Point> VisiblePosition()
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                viewport.UpdateLayout(); await scroll(picker); await Task.Delay(150); viewport.UpdateLayout();
                if (picker.Handler?.PlatformView is ComboBox current)
                {
                    var position = current.TransformToVisual(viewport).TransformPoint(new Windows.Foundation.Point());
                    if (position.X >= -1 && position.Y >= -1 && position.X + current.ActualWidth <= viewport.ActualWidth + 1
                        && position.Y + current.ActualHeight <= viewport.ActualHeight + 1) return position;
                }
            }
            throw new IOException("Original household caption is outside its actual visible viewport.");
        }
        await VisiblePosition();
        var index = picker.SelectedIndex; var expected = picker.Items[index];
        if (expected.Contains("\u00c2\u00b7", StringComparison.Ordinal) || !expected.Contains('\u00b7'))
            throw new IOException("Original household separator is malformed or missing.");
        var output = Path.Combine(folder, stage); Directory.CreateDirectory(output);
        var selected = DebugPickerText.Check(picker, output, stage);
        if (picker.Handler?.PlatformView is not ComboBox combo
            || FrameworkElementAutomationPeer.CreatePeerForElement(combo) is not ComboBoxAutomationPeer peer
            || peer.GetPattern(PatternInterface.ExpandCollapse) is not IExpandCollapseProvider expand)
            throw new IOException("Original household picker has no native popup pattern.");
        var retained = Path.Combine(priorObservations, stage, "observation.json");
        object popup; int? popupOriginalProcess = null;
        if (File.Exists(retained) && priorObservations != folder)
        {
            using var previous = JsonDocument.Parse(await File.ReadAllTextAsync(retained));
            if (previous.RootElement.GetProperty("Expected").GetString() != expected
                || previous.RootElement.GetProperty("DisplayPacketOnly").GetBoolean() != displayOnly)
                throw new IOException("Original household popup proof belongs to different caption data.");
            popup = previous.RootElement.GetProperty("Popup").Clone();
            popupOriginalProcess = previous.RootElement.GetProperty("ProcessId").GetInt32();
        }
        else
        {
            expand.Expand();
            try
            {
                await PlanEditorNativeReview.WaitAsync(() => combo.ContainerFromIndex(index) is ComboBoxItem
                    { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0 }, CancellationToken.None);
                var item = (ComboBoxItem)combo.ContainerFromIndex(index);
                var itemPeer = peer.GetChildren()?.OfType<ComboBoxItemDataAutomationPeer>()
                    .SingleOrDefault(child => Equals(child.Item, combo.Items[index]))
                    ?? throw new IOException("Original household popup choice has no native identity.");
                popup = DebugPickerText.CheckPopup(picker, item, itemPeer.GetName(), output, stage);
            }
            finally { expand.Collapse(); }
        }
        var visiblePosition = await VisiblePosition();
        await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "money.png"));
        if (!ReferenceEquals(Shell.Current.CurrentPage, page) || picker.SelectedIndex != index)
            throw new IOException("Original household caption observation changed page or selection.");
        var row = new { Stage = stage, ProcessId = Environment.ProcessId, Expected = expected, Selected = selected,
            Popup = popup, PopupOriginalProcess = popupOriginalProcess, VisiblePosition = visiblePosition,
            ViewportWidth = viewport.ActualWidth, ViewportHeight = viewport.ActualHeight,
            PopupImageIncluded = false, DisplayPacketOnly = displayOnly, FinancialSaves = 0 };
        await File.WriteAllTextAsync(Path.Combine(output, "observation.json"), JsonSerializer.Serialize(row));
        observations.Add(row); captures.Add(new { Language = stage, File = "money.png" });
    }
}
#endif
