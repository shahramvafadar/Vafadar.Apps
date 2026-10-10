#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // A fictitious report query uses the existing scope path; native Clear changes only the view, never stored money.
    private static async Task ReviewTransactionScopeAsync(App app, IServiceProvider services,
        TransactionsPage page, string folder, string language, Dictionary<string, object> query)
    {
        var vm = (TransactionsViewModel)page.BindingContext;
        var root = page.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement
            ?? throw new InvalidOperationException("The transaction page has no native root.");
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new
        {
            Settings = await store.GetSettingsAsync(), Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), SavedFilters = await store.GetSavedFiltersAsync(),
        });
        var before = await StoredAsync();
        var scope = (vm.ScopeNote, vm.CategoryFilterName, vm.CustomPeriodText);
        string Results() => JsonSerializer.Serialize(vm.Days.Select(day => new { day.Header, day.NetText, Rows = day.ToArray() }));
        var results = Results();
        var viewport = page.FindByName<ScrollView>("FiltersViewport")
            ?? throw new InvalidOperationException("The real filter viewport is missing.");
        var controls = VisualDescendants(page).OfType<Button>()
            .Where(button => ReferenceEquals(button.Command, vm.ClearCategoryFilterCommand))
            .Where(button => button.Width > 0 && button.Height > 0).ToArray();
        var targets = new List<object>();
        var captions = new List<object>();
        var valid = controls.Length == 3;
        for (var index = 0; index < controls.Length; index++)
        {
            var button = controls[index];
            await ScrollToViewIfNeededAsync(viewport, button);
            if (!ReferenceEquals(Shell.Current.CurrentPage, page))
            { throw new InvalidOperationException("The scope review page was retired."); }
            var native = button.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.Button
                ?? throw new InvalidOperationException("The scope clear target has no native button.");
            var origin = native.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point());
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(native)
                ?? throw new InvalidOperationException("The scope clear target has no native peer.");
            var scroller = viewport.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.ScrollViewer
                ?? throw new InvalidOperationException("The filter viewport has no native scroll control.");
            var local = native.TransformToVisual(scroller).TransformPoint(new Windows.Foundation.Point());
            var name = peer.GetName();
            var targetValid = origin.X >= -1 && origin.X + native.ActualWidth <= root.ActualWidth + 1
                && local.X >= -1 && local.X + native.ActualWidth <= scroller.ViewportWidth + 1
                && local.Y >= -1 && local.Y + native.ActualHeight <= scroller.ViewportHeight + 1
                && native.ActualWidth >= 44 && native.ActualHeight >= 44
                && peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider && !string.IsNullOrWhiteSpace(name);
            valid &= targetValid;
            targets.Add(new { Name = name, native.ActualWidth, native.ActualHeight, origin, local,
                scroller.ViewportHeight, scroller.VerticalOffset, TargetPass = targetValid });
            var label = VisualDescendants((View)button.Parent).OfType<Label>().Single();
            if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text)
            { throw new InvalidOperationException("The full scope caption has no actual native text."); }
            var slot = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(text);
            var glyphs = new List<Windows.Foundation.Rect>();
            for (var offset = 0; offset <= text.ContentEnd.Offset - text.ContentStart.Offset; offset++)
            {
                if (text.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward) is { } pointer)
                { glyphs.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
            }
            // The native label mapper intentionally converts display digits without changing the bound Latin value.
            var expectedText = label.FlowDirection == FlowDirection.LeftToRight ? label.Text
                : Vafadar.Localization.Formatting.NativeDigits.Apply(label.Text);
            var captionValid = !text.IsTextTrimmed && text.IsTextScaleFactorEnabled && text.Text == expectedText
                && glyphs.Count > 0 && glyphs.Min(rect => rect.Left) >= -1
                && glyphs.Max(rect => rect.Right) <= Math.Min(slot.Width, label.Width) + 1
                && glyphs.Max(rect => rect.Bottom) <= Math.Min(slot.Height, label.Height) + 1;
            valid &= captionValid;
            captions.Add(new { label.Text, NativeText = text.Text, ExpectedText = expectedText, label.Width, label.Height, text.FontSize,
                text.IsTextScaleFactorEnabled, text.IsTextTrimmed, slot, Glyphs = glyphs, CaptionPass = captionValid });
            await CaptureAsync(app, folder, language + "-transactions-scope-visible-" + index);
        }
        if (!valid)
        {
            File.WriteAllText(Path.Combine(folder, language + "-transactions-scope-geometry-failure.json"),
                JsonSerializer.Serialize(new { root.ActualWidth, root.ActualHeight, Targets = targets, Captions = captions }));
            throw new InvalidOperationException("A report-scope caption or clear action is incomplete in its actual native viewport.");
        }
        var resultViewport = page.FindByName<CollectionView>("ContentViewport")
            ?? throw new InvalidOperationException("The transaction result viewport is missing.");
        static Microsoft.UI.Xaml.Controls.ScrollViewer? FindResultScroll(Microsoft.UI.Xaml.DependencyObject element)
        {
            if (element is Microsoft.UI.Xaml.Controls.ScrollViewer found) { return found; }
            for (var index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element); index++)
            {
                if (FindResultScroll(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, index)) is { } child)
                { return child; }
            }
            return null;
        }
        var listScroll = resultViewport.Handler?.PlatformView is Microsoft.UI.Xaml.DependencyObject listRoot
            ? FindResultScroll(listRoot) : null;
        if (listScroll is null || listScroll.ViewportHeight < 44)
        { throw new InvalidOperationException("The complete result has no usable native scroll viewport."); }
        if (listScroll.ScrollableHeight > 1)
        { NativeScrollProvider(listScroll).SetScrollPercent(-1, 100); await Task.Delay(300); }
        var visibleAmounts = VisualDescendants(page).OfType<Label>()
            .Where(label => label.BindingContext is Presentation.EntryRow row && label.Text == row.AmountText)
            .Where(label => label.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock)
            .Select(label =>
            {
                var text = (Microsoft.UI.Xaml.Controls.TextBlock)label.Handler!.PlatformView!;
                var origin = text.TransformToVisual(listScroll).TransformPoint(new Windows.Foundation.Point());
                return new { text.Text, text.ActualWidth, text.ActualHeight, origin, text.IsTextTrimmed };
            }).Where(amount => amount.origin.Y >= -1 && amount.origin.Y + amount.ActualHeight <= listScroll.ViewportHeight + 1
                && !amount.IsTextTrimmed && amount.ActualHeight > 0).ToArray();
        if (visibleAmounts.Length == 0)
        { throw new InvalidOperationException("Native result scrolling did not reveal an original monetary row."); }
        await CaptureAsync(app, folder, language + "-transactions-scope-results-end");
        if (listScroll.ScrollableHeight > 1)
        { NativeScrollProvider(listScroll).SetScrollPercent(-1, 0); await Task.Delay(300); }
        if (controls[^1].Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button clear
            || FrameworkElementAutomationPeer.CreatePeerForElement(clear)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
        { throw new InvalidOperationException("The visible scope Clear action lost its native Invoke pattern."); }
        try
        {
            invoke.Invoke();
            await Task.Delay(300);
            if (vm.ScopeNote is not null || vm.CategoryFilterName is not null || vm.CustomPeriodText is not null)
            { throw new InvalidOperationException("Native Clear did not remove the existing complete report scope."); }
            await CaptureAsync(app, folder, language + "-transactions-scope-cleared");
            if (before != await StoredAsync()) { throw new InvalidOperationException("Native Clear changed stored data."); }
        }
        finally
        {
            // Replay the same real route query to restore every scope field, including private financial scope.
            await Shell.Current.GoToAsync("//transactions", new Dictionary<string, object>(query));
            await vm.LoadAsync();
            await Task.Delay(500);
        }
        if (!ReferenceEquals(Shell.Current.CurrentPage, page)
            || (vm.ScopeNote, vm.CategoryFilterName, vm.CustomPeriodText) != scope
            || results != Results() || before != await StoredAsync())
        {
            File.WriteAllText(Path.Combine(folder, language + "-transactions-scope-restore-failure.json"),
                JsonSerializer.Serialize(new { SamePage = ReferenceEquals(Shell.Current.CurrentPage, page),
                    ExpectedScope = scope, vm.ScopeNote, vm.CategoryFilterName, vm.CustomPeriodText,
                    QueryKeys = query.Keys, ExpectedResults = results, ActualResults = Results(), StoredEqual = before == await StoredAsync() }));
            throw new InvalidOperationException("The original report scope/result or complete stored data was not restored.");
        }
        if (viewport.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer scroll && scroll.ScrollableHeight > 1)
        { NativeScrollProvider(scroll).SetScrollPercent(-1, 0); await Task.Delay(300); }
        File.WriteAllText(Path.Combine(folder, language + "-transactions-scope-targets.json"),
            JsonSerializer.Serialize(new { root.ActualWidth, root.ActualHeight, Targets = targets, Captions = captions,
                vm.ScopeNote, vm.CategoryFilterName, vm.CustomPeriodText, CompleteClearTargetsVisible = valid,
                ResultViewportHeight = listScroll.ViewportHeight, VisibleOriginalAmounts = visibleAmounts,
                NativeResultScrolledAndRestored = true,
                NativeClearInvoked = true, OriginalScopeAndResultRestored = true,
                CompleteSettingsAccountsEntriesSavedFiltersUnchanged = true, NoSave = true }));
    }

    // Expand the existing form through its native button; observing filters must never save a filter or money.
    private static async Task ReviewTransactionFiltersAsync(App app, IServiceProvider services,
        TransactionsPage page, string folder, string language)
    {
        var vm = (TransactionsViewModel)page.BindingContext;
        await vm.LoadAsync();
        await Task.Delay(300);
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new
        {
            Settings = await store.GetSettingsAsync(), Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), SavedFilters = await store.GetSavedFiltersAsync(),
        });
        var before = await StoredAsync();
        var originalOpen = vm.IsFilterOpen;
        var originalRows = vm.Days.SelectMany(day => day).ToArray();
        static void Invoke(Button button)
        {
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke)
                    is not IInvokeProvider invoke)
            { throw new InvalidOperationException("The actual filter button has no native Invoke pattern."); }
            invoke.Invoke();
        }
        var toggle = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.Command, vm.ToggleFilterCommand));
        try
        {
            if (!vm.IsFilterOpen) { Invoke(toggle); }
            await Task.Delay(500);
            if (!vm.IsFilterOpen || !ReferenceEquals(Shell.Current.CurrentPage, page))
            { throw new InvalidOperationException("Native filter expansion did not retain the active transaction page."); }
            await CaptureAsync(app, folder, language + "-transactions-filters-expanded");
            System.Windows.Input.ICommand[] commands = [vm.SavedFilterMenuCommand, vm.ApplySavedFilterCommand, vm.StartSelectingCommand];
            var targets = VisualDescendants(page).OfType<Button>()
                .Where(button => commands.Any(command => ReferenceEquals(button.Command, command)))
                .Where(button => button.IsVisible && button.Width > 0 && button.Height > 0)
                .Select(button =>
                {
                    var native = button.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.Button
                        ?? throw new InvalidOperationException("A filter action has no realized native target.");
                    var peer = FrameworkElementAutomationPeer.CreatePeerForElement(native)
                        ?? throw new InvalidOperationException("A filter action has no associated native peer.");
                    return new { button.Text, Name = peer.GetName(), button.Width, button.Height,
                        NativeWidth = native.ActualWidth, NativeHeight = native.ActualHeight,
                        HasInvoke = peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider };
                }).ToArray();
            var viewport = page.FindByName<CollectionView>("ContentViewport")
                ?? throw new InvalidOperationException("The real transaction viewport is missing.");
            if (before != await StoredAsync() || !originalRows.SequenceEqual(vm.Days.SelectMany(day => day)))
            { throw new InvalidOperationException("Opening filters changed stored rows or the complete transaction result."); }
            var complete = targets.Length == vm.SavedFilters.Count + 2;
            var valid = complete && targets.All(target => target.Width >= 44 && target.Height >= 44
                && target.NativeWidth >= 44 && target.NativeHeight >= 44 && target.HasInvoke
                && !string.IsNullOrWhiteSpace(target.Name));
            File.WriteAllText(Path.Combine(folder, language + "-transactions-filters-targets.json"),
                JsonSerializer.Serialize(new { Targets = targets, CompleteTargets = complete,
                    viewport.Width, viewport.Height, viewport.Y, NativeFilterExpansion = true,
                    StoredRowsAndCompleteResultUnchanged = true, NoSave = true, TargetsPass = valid }));
            if (!valid) { throw new InvalidOperationException("An expanded transaction filter action lacks a complete native name or 44-unit target."); }
        }
        finally
        {
            if (vm.IsFilterOpen != originalOpen) { Invoke(toggle); }
            if (before != await StoredAsync()) { throw new InvalidOperationException("Filter review changed stored data."); }
        }
    }
}
#endif
