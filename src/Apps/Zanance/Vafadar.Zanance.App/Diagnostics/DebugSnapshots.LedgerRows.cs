#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks complete original ledger/account row packets and selection without financial writes.</summary>
    private static async Task ReviewLedgerRowsAsync(App app, IServiceProvider services, string folder)
    {
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var before = await StoredAsync();
        var local = services.GetRequiredService<ILocalizationService>();
        var languages = (Environment.GetEnvironmentVariable("VAFADAR_LEDGER_ROWS_LANGUAGES") ?? "en,fa,de").Split(',');
        if (languages.Length == 0 || languages.Distinct(StringComparer.Ordinal).Count() != languages.Length
            || languages.Any(language => language is not ("en" or "fa" or "de")))
            throw new IOException("Explicit original ledger review languages required.");
        var captures = new List<object>();
        var selectedCases = (Environment.GetEnvironmentVariable("VAFADAR_LEDGER_ROWS_CONTEXTS") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        foreach (var language in languages)
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360d, 412d, 1280d })
        {
            var caseName = language + "-" + theme + "-" + width;
            if (selectedCases.Count != 0 && !selectedCases.Contains(caseName)) continue;
            var output = Path.Combine(folder, language + "-" + theme + "-" + width);
            Directory.CreateDirectory(output);
            var observations = new List<object>();
            var complete = true;
            void Mark(string stage) => File.WriteAllText(Path.Combine(folder, "current-phase.json"), JsonSerializer.Serialize(new { Case = caseName, Stage = stage }));
            Mark("home-navigation");
            await Shell.Current.GoToAsync("//home");
            Mark("window-resize");
            app.Windows[0].Width = width;
            Mark("theme-switch");
            await SetStoreThemeAsync(app, services.GetRequiredService<ThemeService>(), theme);
            Mark("language-switch");
            await SetLanguageAsync(app, local, language);
            Mark("home-publication");
            await WaitForStorePageAsync(app);
            var page = (HomePage)Shell.Current.CurrentPage;
            await page.PendingPublication;
            var home = (HomeViewModel)page.BindingContext;
            var negative = Core.Money.MoneyText.Format(long.MinValue, "EUR", local.CurrentCulture);
            var positive = Core.Money.MoneyText.Format(long.MaxValue, "EUR", local.CurrentCulture);
            var multiple = positive + " · " + Core.Money.MoneyText.Format(long.MinValue, "USD", local.CurrentCulture);
            var recent = home.RecentEntries.First() with { AmountText = negative };
            home.RecentEntries[0] = recent;
            await Task.Delay(200);
            var recentGrid = MoneyRow(page, recent, negative);
            await page.FindByName<ScrollView>("ContentViewport").ScrollToAsync(recentGrid, ScrollToPosition.Start, false);
            complete &= await CheckAsync(page, recentGrid, negative, "home-entry", home.OpenEntryCommand, recent);

            var account = home.Accounts.First() with { BalanceText = negative, IsNegative = true };
            home.Accounts[0] = account;
            page.PreviewAccountSectionForReview(true);
            await Task.Delay(200);
            var accountRow = MoneyRow(page, account, negative);
            await page.FindByName<ScrollView>("ContentViewport").ScrollToAsync(accountRow, ScrollToPosition.Start, false);
            complete &= await CheckAsync(page, accountRow, negative, "home-account", null, account);

            await Shell.Current.GoToAsync("//transactions");
            var transactionsPage = (TransactionsPage)Shell.Current.CurrentPage;
            var transactions = (TransactionsViewModel)transactionsPage.BindingContext;
            await transactions.LoadAsync();
            var firstDay = transactions.Days.First();
            var entry = firstDay.First() with { AmountText = positive };
            var day = new EntryDayGroup(firstDay.Header, multiple, new[] { entry });
            transactions.Days = new[] { day };
            await Task.Delay(300);
            var entryRow = MoneyRow(transactionsPage, entry, positive);
            complete &= await CheckAsync(transactionsPage, entryRow, positive, "transactions-entry", transactions.OpenCommand, entry);
            await RevealCollectionEdgeAsync(transactionsPage, end: false);
            var dayRow = MoneyRow(transactionsPage, day, multiple);
            complete &= await CheckAsync(transactionsPage, dayRow, multiple, "day-net", null, day);
            // Exercise only selection through the real unchanged command; no bulk write or detail editor opens.
            transactions.StartSelectingCommand.Execute(null);
            await Task.Delay(100);
            transactionsPage.FindByName<CollectionView>("ContentViewport").ScrollTo(entry, day, ScrollToPosition.Start, false);
            await Task.Delay(200);
            entryRow = MoneyRow(transactionsPage, entry, positive);
            var button = entryRow.Children.OfType<Button>().Single();
            await RevealCollectionElementAsync(transactionsPage, button);
            Mark("selection-invoke");
            InvokeChoiceReview(button);
            await Task.Delay(100);
            if (!transactions.HasSelection || !entry.Selection.IsSelected || entryRow.BackgroundColor != Palette.PrimarySoft)
                throw new IOException("Original transaction selection feedback failed.");
            entryRow = MoneyRow(transactionsPage, entry, positive);
            button = entryRow.Children.OfType<Button>().Single();
            await RevealCollectionElementAsync(transactionsPage, button);
            Mark("selection-clear");
            InvokeChoiceReview(button);
            await Task.Delay(100);
            transactions.StopSelectingCommand.Execute(null);
            if (transactions.IsSelecting || transactions.HasSelection || entry.Selection.IsSelected)
                throw new IOException("Original transaction selection did not clear.");

            await Shell.Current.GoToAsync("//home");
            await Shell.Current.GoToAsync(AppShell.AccountsRoute);
            await WaitForStorePageAsync(app);
            var accountsPage = (AccountsPage)Shell.Current.CurrentPage;
            var accounts = (AccountsViewModel)accountsPage.BindingContext;
            for (var wait = 0; accounts.Groups.Count == 0 && wait < 200; wait++) await Task.Delay(50);
            var group = accounts.Groups.First();
            var item = group.Items.First() with { BalanceText = positive, IsNegative = false };
            var modified = group with { Items = new[] { item }, TotalsText = multiple };
            accounts.Groups.Clear(); accounts.Groups.Add(modified);
            await Task.Delay(200);
            var itemRow = MoneyRow(accountsPage, item, positive);
            await accountsPage.FindByName<ScrollView>("ContentViewport").ScrollToAsync(itemRow, ScrollToPosition.Start, false);
            complete &= await CheckAsync(accountsPage, itemRow, positive, "account-list", null, item);
            var totalsRow = MoneyRow(accountsPage, modified, multiple);
            await accountsPage.FindByName<ScrollView>("ContentViewport").ScrollToAsync(totalsRow, ScrollToPosition.Start, false);
            complete &= await CheckAsync(accountsPage, totalsRow, multiple, "account-group", null, modified);
            File.WriteAllText(Path.Combine(output, "observed.json"), JsonSerializer.Serialize(observations));
            if (!complete) throw new IOException("Original ledger/account packets have no independent native monetary viewports.");
            if (before != await StoredAsync()) throw new IOException("Ledger presentation changed original stored inputs.");
            File.WriteAllText(Path.Combine(output, "complete.json"), JsonSerializer.Serialize(new
                { CompletePacketsAndScrollEnds = true, OriginalSelectionAndTargets = true, FinancialCommandsInvoked = false }));
            captures.Add(new { Language = language + "-" + theme + "-" + width, File = "home-entry.png" });

            async Task<bool> CheckAsync(ContentPage host, Grid row, string packet, string name,
                System.Windows.Input.ICommand? command, object parameter)
            {
                Mark(name + "-measure");
                await Task.Delay(100);
                var caption = VisualDescendants(row).OfType<Label>().Single(value => value.Text == packet);
                if (caption.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native)
                    throw new IOException("Original native ledger caption unavailable.");
                native.UpdateLayout();
                if (host is TransactionsPage txHost) await RevealCollectionElementAsync(txHost, caption.Parent as VisualElement ?? caption);
                var button = row.Children.OfType<Button>().SingleOrDefault();
                var wired = command is null || button is not null && ReferenceEquals(button.Command, command)
                    && ReferenceEquals(button.CommandParameter, parameter);
                var independent = caption.Parent is ScrollView;
                observations.Add(new { Host = name, packet, native.ActualWidth, native.ActualHeight, native.FontSize,
                    native.IsTextTrimmed, native.IsTextScaleFactorEnabled, IndependentViewport = independent, Wired = wired });
                await CaptureWindowAsync(app.Windows[0], Path.Combine(output, name + ".png"));
                if (!wired || button is not null && (button.Width < 44 || button.Height < 44))
                    throw new IOException("Original ledger target or binding changed.");
                if (!independent) return false;
                await CheckLegendAmountAsync(host, row, packet);
                Mark(name + "-complete");
                return true;
            }
        }
        File.WriteAllText(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        {
            Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures,
            FinancialRowsUnchangedAfterSeeding = before == await StoredAsync(), FinancialCommandsInvoked = false,
            WindowsLayoutStressOnly = true,
        }));

        static Grid MoneyRow(ContentPage host, object context, string packet) => VisualDescendants(host).OfType<Grid>()
            .First(row => ReferenceEquals(row.BindingContext, context)
                && row.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement { IsLoaded: true, XamlRoot: not null } &&
                (row.Children.OfType<Label>().Any(label => label.Text == packet)
                 || row.Children.OfType<AmountReadout>().Any(value => value.AmountText == packet)));
    }

    /// <summary>Finds the original grouped collection's native vertical viewport without desktop input.</summary>
    private static Microsoft.UI.Xaml.Controls.ScrollViewer LedgerCollectionViewport(TransactionsPage page)
    {
        if (page.FindByName<CollectionView>("ContentViewport").Handler?.PlatformView is not Microsoft.UI.Xaml.DependencyObject root)
            throw new IOException("Original grouped collection is not realized.");
        return Native(root).OfType<Microsoft.UI.Xaml.Controls.ScrollViewer>().OrderByDescending(view => view.ScrollableHeight).First();

        static IEnumerable<Microsoft.UI.Xaml.DependencyObject> Native(Microsoft.UI.Xaml.DependencyObject node)
        {
            yield return node;
            for (var index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node); index++)
                foreach (var child in Native(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node, index))) yield return child;
        }
    }

    /// <summary>Reveals the original collection boundary through its real native Scroll pattern.</summary>
    private static async Task RevealCollectionEdgeAsync(TransactionsPage page, bool end)
    {
        var viewport = LedgerCollectionViewport(page);
        if (viewport.ScrollableHeight > 1)
        {
            var peer = new Microsoft.UI.Xaml.Automation.Peers.ScrollViewerAutomationPeer(viewport);
            if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Scroll)
                is not Microsoft.UI.Xaml.Automation.Provider.IScrollProvider scroll) throw new IOException("Original grouped list has no native Scroll pattern.");
            scroll.SetScrollPercent(-1, end ? 100 : 0);
            await Task.Delay(100);
        }
    }

    /// <summary>Reacquires a real visible native target before using its action or amount viewport.</summary>
    private static async Task RevealCollectionElementAsync(TransactionsPage page, VisualElement element)
    {
        var viewport = LedgerCollectionViewport(page);
        if (element.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement native || native.XamlRoot is null)
            throw new IOException("Original collection element is disconnected.");
        var point = native.TransformToVisual(viewport).TransformPoint(new(0, 0));
        var offset = Math.Clamp(viewport.VerticalOffset + point.Y, 0, viewport.ScrollableHeight);
        if (viewport.ScrollableHeight > 1)
        {
            var peer = new Microsoft.UI.Xaml.Automation.Peers.ScrollViewerAutomationPeer(viewport);
            if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Scroll)
                is not Microsoft.UI.Xaml.Automation.Provider.IScrollProvider scroll) throw new IOException("Original grouped list has no native Scroll pattern.");
            scroll.SetScrollPercent(-1, offset / viewport.ScrollableHeight * 100);
            await Task.Delay(100);
        }
        point = native.TransformToVisual(viewport).TransformPoint(new(0, 0));
        var visible = Math.Min(point.Y + native.ActualHeight, viewport.ViewportHeight) - Math.Max(point.Y, 0);
        if (visible < Math.Min(44, native.ActualHeight) - 1) throw new IOException("Original collection target is not reachable in its viewport.");
    }
}
#endif
