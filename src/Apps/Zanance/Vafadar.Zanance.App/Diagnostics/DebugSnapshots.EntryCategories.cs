#if DEBUG && WINDOWS
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // Only fictitious presentation choices are replaced; native selection never invokes Save.
    private static async Task ReviewEntryCategoriesAsync(App app, IServiceProvider services, EntryEditorPage page,
        string folder, string language)
    {
        var vm = (EntryEditorViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(),
            Categories = await store.GetCategoriesAsync(), Schedules = await services.GetRequiredService<PlanStore>().GetSchedulesAsync() });
        string Draft() => (string)typeof(EntryEditorViewModel).GetMethod("Snapshot", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null)!;
        var originals = vm.Categories.ToArray();
        var chosenField = typeof(EntryEditorViewModel).GetField("_categoryChosen", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var originalChosen = chosenField.GetValue(vm);
        var originalHint = vm.RuleHint; var originalDirty = vm.IsDirty; var originalDraft = Draft(); var stored = await StoredAsync();
        var example = originals.First();
        var caption = services.GetRequiredService<Translator>()["Entry_Category"];
        var longCaption = string.Join(" ", Enumerable.Repeat(caption, 20));
        longCaption = longCaption[..Math.Min(98, longCaption.Length)];
        var fixtures = new[] { new CategoryChoice(Guid.NewGuid(), caption, example.Icon, example.Color),
            new CategoryChoice(Guid.NewGuid(), longCaption, example.Icon, example.Color),
            new CategoryChoice(Guid.NewGuid(), new string('W', 98), example.Icon, example.Color) };
        var checks = new List<object>(); var failed = false;
        try
        {
            vm.Categories.Clear(); foreach (var choice in fixtures) { vm.Categories.Add(choice); }
            await Task.Delay(300);
            var group = VisualDescendants(page).OfType<FlexLayout>().Single(layout => ReferenceEquals(BindableLayout.GetItemsSource(layout), vm.Categories));
            var scroll = FindScrollView(page)!;
            if (group.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement nativeGroup
                || scroll.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer viewport)
            { throw new InvalidOperationException("The actual category group/viewport is unavailable."); }
            var scrollPeer = new ScrollViewerAutomationPeer(viewport);
            var provider = scrollPeer.GetPattern(PatternInterface.Scroll) as IScrollProvider
                ?? throw new InvalidOperationException("The actual category viewport lacks native Scroll.");
            foreach (var choice in fixtures)
            {
                var row = group.Children.OfType<Grid>().Single(item => ReferenceEquals(item.BindingContext, choice));
                var button = row.Children.OfType<Button>().Single();
                var label = VisualDescendants(row).OfType<Label>().Single(item => item.Text == choice.Name);
                if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                    || label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text)
                { throw new InvalidOperationException("The actual category caption/button is unavailable."); }
                Windows.Foundation.Rect Bounds(Microsoft.UI.Xaml.FrameworkElement item, Microsoft.UI.Xaml.UIElement parent) =>
                    item.TransformToVisual(parent).TransformBounds(new Windows.Foundation.Rect(0, 0, item.ActualWidth, item.ActualHeight));
                var initial = Bounds(native, viewport);
                if (viewport.ScrollableHeight > 0)
                {
                    var offset = viewport.VerticalOffset + initial.Top - (viewport.ActualHeight - initial.Height)/2;
                    provider.SetScrollPercent(-1, Math.Clamp(offset, 0, viewport.ScrollableHeight)/viewport.ScrollableHeight*100);
                }
                await Task.Delay(250); await CaptureAsync(app, folder, language + "-entry-category-" + checks.Count);
                var target = Bounds(native, nativeGroup); var visible = Bounds(native, viewport);
                var slot = LayoutInformation.GetLayoutSlot(text);
                var parent = (Microsoft.UI.Xaml.FrameworkElement)Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(text);
                var origin = parent.TransformToVisual(nativeGroup).TransformPoint(new Windows.Foundation.Point(slot.X, slot.Y));
                var glyphs = new List<Windows.Foundation.Rect>();
                for (var offset = 0; offset <= text.ContentEnd.Offset - text.ContentStart.Offset; offset++)
                {
                    if (text.ContentStart.GetPositionAtOffset(offset, LogicalDirection.Forward) is { } pointer)
                    { glyphs.Add(pointer.GetCharacterRect(LogicalDirection.Forward)); }
                }
                var peer = new ButtonAutomationPeer(native);
                var valid = target.Width >= 44 && target.Height >= 44 && target.Left >= -1 && target.Right <= nativeGroup.ActualWidth + 1
                    && (choice.Name != caption || target.Height <= Math.Max(44, text.ActualHeight) + 24)
                    && visible.Left >= -1 && visible.Right <= viewport.ActualWidth + 1 && visible.Top >= -1 && visible.Bottom <= viewport.ActualHeight + 1
                    && text.Text == choice.Name && !text.IsTextTrimmed && text.IsTextScaleFactorEnabled && glyphs.Count > 0
                    && glyphs.Min(box => box.Left) >= -1 && glyphs.Max(box => box.Right) <= slot.Width + 1
                    && glyphs.Max(box => box.Bottom) <= slot.Height + 1
                    && origin.X + glyphs.Min(box => box.Left) >= -1 && origin.X + glyphs.Max(box => box.Right) <= nativeGroup.ActualWidth + 1
                    && button.Command == vm.SelectCategoryCommand && ReferenceEquals(button.CommandParameter, choice)
                    && peer.GetName() == choice.Name && peer.IsEnabled();
                if (valid)
                {
                    var invoke = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider
                        ?? throw new InvalidOperationException("The actual category target lacks native Invoke.");
                    invoke.Invoke(); await Task.Delay(150);
                    if (!choice.IsSelected || vm.Categories.Count(item => item.IsSelected) != 1 || stored != await StoredAsync())
                    { throw new InvalidOperationException("Category selection loses its original identity or writes stored data."); }
                }
                else { failed = true; }
                checks.Add(new { choice.Name, choice.Id, valid, target, visible, GroupWidth = nativeGroup.ActualWidth,
                    ViewportWidth = viewport.ActualWidth, ViewportHeight = viewport.ActualHeight, text.FontSize,
                    text.IsTextTrimmed, text.IsTextScaleFactorEnabled, slot, origin, glyphs, NativeSelection = valid });
            }
        }
        finally
        {
            vm.Categories.Clear(); foreach (var choice in originals) { vm.Categories.Add(choice); }
            chosenField.SetValue(vm, originalChosen); vm.RuleHint = originalHint;
            if (Draft() != originalDraft || vm.IsDirty != originalDirty || stored != await StoredAsync())
            { throw new InvalidOperationException("The category review changed the complete draft or stored rows."); }
        }
        File.WriteAllText(Path.Combine(folder, language + "-entry-categories.json"), JsonSerializer.Serialize(new
        { Pass = !failed, Cases = checks, NoSave = true, CompleteDraftRestored = true, CompleteStoredRowsUnchanged = true }));
        if (failed) { throw new InvalidOperationException("A native category target/caption extends outside its current group or viewport."); }
    }
}
#endif
