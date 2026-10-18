#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks original loading/retry and virtualized monetary rows without saving financial data.</summary>
    private static async Task ReviewReimbursementsAsync(App app, IServiceProvider services, string folder)
    {
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var stored = await StoredAsync();
        var local = services.GetRequiredService<ILocalizationService>();
        var captures = new List<object>();
        var emptyOnly = Environment.GetEnvironmentVariable("VAFADAR_REIMBURSEMENTS_EMPTY_ONLY") == "1";
        var visibleOnly = Environment.GetEnvironmentVariable("VAFADAR_REIMBURSEMENTS_VISIBILITY_ONLY") == "1";
        var languages = (Environment.GetEnvironmentVariable("VAFADAR_REIMBURSEMENTS_LANGUAGES") ?? "en,fa,de").Split(',');
        if (languages.Length == 0 || languages.Distinct().Count() != languages.Length || languages.Any(value => value is not ("en" or "fa" or "de")))
            throw new IOException("Explicit original reimbursement languages required.");
        foreach (var language in languages)
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360d, 412d, 1280d })
        {
            var name = language + "-" + theme + "-" + width;
            var output = Path.Combine(folder, name); Directory.CreateDirectory(output);
            void Mark(string stage) => File.WriteAllText(Path.Combine(folder, "current-phase.json"), JsonSerializer.Serialize(new { Case = name, Stage = stage }));
            Mark("context");
            await Shell.Current.GoToAsync("//home"); app.Windows[0].Width = width;
            await SetStoreThemeAsync(app, services.GetRequiredService<ThemeService>(), theme);
            await SetLanguageAsync(app, local, language); await WaitForStorePageAsync(app);
            var vm = services.GetRequiredService<ReimbursementsViewModel>();
            var page = new ReimbursementsPage(vm);
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Published(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
            { if (args.PropertyName == nameof(vm.Loading.IsReady) && vm.Loading.IsReady) ready.TrySetResult(); }
            vm.Loading.PropertyChanged += Published;
            try { await Shell.Current.Navigation.PushAsync(page); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { vm.Loading.PropertyChanged -= Published; }
            if (vm.Items.Count == 0) throw new IOException("Original fictitious reimbursement seed missing.");
            if (emptyOnly)
            {
                await vm.Loading.RunAsync(() => Task.CompletedTask, () => { vm.Items.ReplaceAll([]); vm.TotalText = string.Empty; vm.IsEmpty = true; });
                await Task.Delay(150);
                var emptyText = services.GetRequiredService<Translator>()["Reimbursements_Empty"];
                var emptyCaption = VisualDescendants(page).OfType<Label>().Single(value => value.Text == emptyText);
                await RevealReimbursementElementAsync(page.FindByName<ScrollView>("EmptyReimbursementViewport"), emptyCaption);
                CheckReimbursementEmptySeparation(page, emptyCaption, services.GetRequiredService<Translator>()["Reimbursements_Intro"]);
                CheckCaption(emptyCaption);
                if (!vm.Loading.IsReady || !vm.IsEmpty || vm.Items.Count != 0 || stored != await StoredAsync())
                    throw new IOException("Presented empty reimbursement state changed original inputs.");
                await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "first.png"));
                File.WriteAllText(Path.Combine(output, "complete.json"), JsonSerializer.Serialize(new
                    { PresentedEmptyStateOnly = true, NativeVerticalVisibilityRequired = true, CompleteCaption = true, StoredInputsUnchanged = true }));
                captures.Add(new { Language = name, File = "first.png" });
                if (captures.Count < languages.Length * 6) await Shell.Current.Navigation.PopAsync();
                continue;
            }
            var original = vm.Items[0];
            var packet = Core.Money.MoneyText.Format(long.MaxValue, "EUR", local.CurrentCulture);
            var rows = Enumerable.Range(0, 4096).Select(index => original with
            {
                Title = original.Title + " · " + index + " · " + new string('W', 30), AmountText = packet,
            }).ToArray();
            var resets = 0;
            void Reset(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
            { if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset) resets++; }
            vm.Items.CollectionChanged += Reset;
            vm.Items.ReplaceAll(rows);
            vm.Items.CollectionChanged -= Reset;
            if (resets != 1) throw new IOException("Reimbursement batch was not published atomically.");
            var list = page.FindByName<CollectionView>("ReimbursementList");
            var observed = new List<object>();
            foreach (var edge in new[] { 0, rows.Length - 1 })
            {
                Mark("edge-" + edge);
                list.ScrollTo(rows[edge], position: edge == 0 ? ScrollToPosition.Start : ScrollToPosition.End, animate: false);
                // Variable-height virtualized rows refine their extent after layout. Use the original native
                // Scroll pattern at the requested boundary, as a real end-of-list action does (D-344).
                await Task.Delay(100);
                if (list.Handler?.PlatformView is Microsoft.UI.Xaml.DependencyObject nativeList)
                {
                    var viewport = NativeReimbursementChildren(nativeList).OfType<Microsoft.UI.Xaml.Controls.ScrollViewer>()
                        .OrderByDescending(value => value.ScrollableHeight).First();
                    if (viewport.ScrollableHeight > 1)
                    {
                        var peer = new Microsoft.UI.Xaml.Automation.Peers.ScrollViewerAutomationPeer(viewport);
                        if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Scroll)
                            is not Microsoft.UI.Xaml.Automation.Provider.IScrollProvider pattern)
                            throw new IOException("Original reimbursement viewport has no native Scroll pattern.");
                        pattern.SetScrollPercent(-1, edge == 0 ? 0 : 100);
                    }
                }
                Grid? row = null;
                for (var wait = 0; wait < 200 && row is null; wait++)
                {
                    await Task.Delay(50);
                    row = VisualDescendants(page).OfType<Grid>().FirstOrDefault(value => ReferenceEquals(value.BindingContext, rows[edge])
                        && value.Children.OfType<AmountReadout>().Any());
                }
                if (row is null) throw new IOException("Original reimbursement edge row did not realize.");
                var amount = row.Children.OfType<AmountReadout>().Single();
                var button = VisualDescendants(row).OfType<Button>().Single();
                var caption = VisualDescendants(row).OfType<Label>().Single(value => value.Text == rows[edge].Title);
                if (caption.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native)
                    throw new IOException("Original reimbursement caption missing.");
                await RevealReimbursementElementAsync(list, caption);
                native.UpdateLayout();
                if (native.IsTextTrimmed || !native.IsTextScaleFactorEnabled || button.Width < 44 || button.Height < 44
                    || !ReferenceEquals(button.Command, vm.OpenCommand) || !ReferenceEquals(button.CommandParameter, rows[edge])
                    || SemanticProperties.GetDescription(button) != rows[edge].Title || amount.AmountText != packet)
                    throw new IOException("Original reimbursement caption, monetary packet or navigation target changed.");
                await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "caption-" + edge + ".png"));
                await RevealReimbursementElementAsync(list, amount);
                await CheckLegendAmountAsync(page, row, packet);
                var realized = VisualDescendants(page).OfType<AmountReadout>().Count(value => value.Handler is not null);
                if (realized >= 200) throw new IOException("Original reimbursement list realized the entire batch.");
                observed.Add(new { Edge = edge, RealizedReadouts = realized, OriginalTarget = true, CompletePacketAndNativeScroll = true });
                await CaptureWindowAsync(app.Windows[0], Path.Combine(output, edge == 0 ? "first.png" : "last.png"));
            }
            if (!visibleOnly)
            {
            Mark("loading-failure-retry");
            var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var loading = vm.Loading.RunAsync(() => held.Task, () => { });
            if (ReimbursementVisible(list) || vm.Loading.IsReady || !vm.Loading.IsLoading) throw new IOException("Incomplete reimbursement result is visible.");
            vm.OpenCommand.Execute(rows[0]);
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Covered reimbursement target navigated.");
            held.SetException(new IOException("Fictitious explicit read failure."));
            try { await loading; throw new IOException("Expected explicit read failure missing."); }
            catch (IOException) when (vm.Loading.HasFailed) { }
            await Task.Delay(100);
            var retry = page.FindByName<Button>("RetryLoadButton");
            InvokeChoiceReview(retry);
            for (var wait = 0; !vm.Loading.IsReady && wait < 200; wait++) await Task.Delay(50);
            if (!vm.Loading.IsReady || vm.Loading.HasFailed || vm.Items.Count != 1 || !ReimbursementVisible(list))
                throw new IOException("Original native reimbursement retry failed.");
            vm.OpenCommand.Execute(rows[0]);
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Retired reimbursement target navigated.");
            }
            if (!visibleOnly)
            {
                await vm.Loading.RunAsync(() => Task.CompletedTask, () => { vm.Items.ReplaceAll([]); vm.TotalText = string.Empty; vm.IsEmpty = true; });
                await Task.Delay(150);
                var emptyCaption = VisualDescendants(page).OfType<Label>().Single(value => value.Text == services.GetRequiredService<Translator>()["Reimbursements_Empty"]);
                await RevealReimbursementElementAsync(page.FindByName<ScrollView>("EmptyReimbursementViewport"), emptyCaption);
                CheckCaption(emptyCaption);
                CheckReimbursementEmptySeparation(page, emptyCaption, services.GetRequiredService<Translator>()["Reimbursements_Intro"]);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "empty.png"));
            }
            if (stored != await StoredAsync()) throw new IOException("Reimbursement presentation changed stored finance.");
            File.WriteAllText(Path.Combine(output, "observed.json"), JsonSerializer.Serialize(observed));
            File.WriteAllText(Path.Combine(output, "complete.json"), JsonSerializer.Serialize(new { VirtualizedEdges = true,
                OriginalCaptionsAndPackets = true, CoveredAndRetiredTargetsRefused = !visibleOnly, NativeRetry = !visibleOnly, NativeVerticalVisibilityRequired = true, StoredInputsUnchanged = true }));
            captures.Add(new { Language = name, File = "first.png" });
            // The final receipt belongs to this component; do not start an unrelated Home reload after completion.
            if (captures.Count < languages.Length * 6) await Shell.Current.Navigation.PopAsync();
        }
        File.WriteAllText(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Package = AppInfo.Current.PackageName,
            ProcessId = Environment.ProcessId, Captures = captures, FinancialRowsUnchangedAfterSeeding = true }));
    }

    /// <summary>Reveals the original caption or amount through its native vertical Scroll pattern and checks both edges.</summary>
    private static async Task RevealReimbursementElementAsync(VisualElement list, VisualElement element)
    {
        if (list.Handler?.PlatformView is not Microsoft.UI.Xaml.DependencyObject root
            || element.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement target || target.XamlRoot is null)
            throw new IOException("Original reimbursement visibility target missing.");
        var viewport = NativeReimbursementChildren(root).OfType<Microsoft.UI.Xaml.Controls.ScrollViewer>()
            .OrderByDescending(value => value.ScrollableHeight).First();
        var point = target.TransformToVisual(viewport).TransformPoint(new(0, 0));
        var offset = Math.Clamp(viewport.VerticalOffset + point.Y, 0, viewport.ScrollableHeight);
        if (viewport.ScrollableHeight > 1)
        {
            var peer = new Microsoft.UI.Xaml.Automation.Peers.ScrollViewerAutomationPeer(viewport);
            if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Scroll)
                is not Microsoft.UI.Xaml.Automation.Provider.IScrollProvider pattern)
                throw new IOException("Original reimbursement visibility has no native Scroll pattern.");
            pattern.SetScrollPercent(-1, offset / viewport.ScrollableHeight * 100);
            await Task.Delay(100);
        }
        point = target.TransformToVisual(viewport).TransformPoint(new(0, 0));
        if (point.Y < -1 || point.Y + target.ActualHeight > viewport.ViewportHeight + 1)
            throw new IOException("Original reimbursement target is outside its vertical native viewport.");
    }

    /// <summary>Checks effective visibility through the original page's ancestors.</summary>
    private static bool ReimbursementVisible(VisualElement element)
    {
        for (Element? current = element; current is not null; current = current.Parent)
            if (current is VisualElement { IsVisible: false }) return false;
        return true;
    }

    /// <summary>Refuses the actual native header/empty-caption overlap seen in the earlier candidate.</summary>
    private static void CheckReimbursementEmptySeparation(ContentPage page, Label empty, string intro)
    {
        var first = VisualDescendants(page).OfType<Label>().Single(value => value.Text == intro && ReimbursementVisible(value));
        if (first.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement start
            || empty.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement end
            || page.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement root)
            throw new IOException("Original empty reimbursement geometry missing.");
        var top = start.TransformToVisual(root).TransformPoint(new(0, 0)).Y;
        var bottom = end.TransformToVisual(root).TransformPoint(new(0, 0)).Y;
        if (top + start.ActualHeight > bottom + 1) throw new IOException("Original reimbursement intro overlaps its empty caption.");
    }

    /// <summary>Finds only native descendants of the original list viewport.</summary>
    private static IEnumerable<Microsoft.UI.Xaml.DependencyObject> NativeReimbursementChildren(Microsoft.UI.Xaml.DependencyObject node)
    {
        yield return node;
        for (var index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node); index++)
            foreach (var child in NativeReimbursementChildren(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node, index))) yield return child;
    }
}
#endif
