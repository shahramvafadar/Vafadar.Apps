#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Uses the original Shell tabs and account page over a separate mutable fictitious ledger.</summary>
    private static async Task ReviewAccountTabRefreshWindowsAsync(App app, IServiceProvider services, string folder)
    {
        if (!AppInfo.Current.PackageName.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal))
            throw new IOException("Independent account review identity required.");
        var continuation = Environment.GetEnvironmentVariable("VAFADAR_ACCOUNT_TAB_CONTINUATION");
        var source = string.IsNullOrEmpty(continuation) ? null : Path.Combine(Path.GetDirectoryName(folder)!, continuation);
        if (source is not null && (continuation!.Length != 32 || continuation.Any(character => !Uri.IsHexDigit(character))))
            throw new IOException("Exact independent review identity required.");
        if (source is not null && (!File.Exists(Path.Combine(source, "error.txt"))
            || File.Exists(Path.Combine(source, "complete.json"))
            || !File.ReadAllText(Path.Combine(source, "error.txt")).Contains("Original native account value differs.", StringComparison.Ordinal)))
            throw new IOException("Continue only the terminal native-digit observation; never restart an expired live review.");
        var path = Path.Combine(source ?? folder, "account-tab-refresh.db");
        if (source is null && File.Exists(path) || source is not null && !File.Exists(path))
            throw new IOException("Keep the original review; never reseed an observed account.");
        await using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>());
        var writer = fixture.GetRequiredService<ZananceStore>();
        await writer.EnsureDefaultCategoriesAsync();
        var day = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        var account = new Account { Name = "D394 retained account", Type = AccountType.Asset, CurrencyCode = "EUR",
            OpeningDate = day, OpeningBalance = 100_000, IncludeInTotals = false };
        if (source is null) await writer.SaveAccountAsync(account);
        else account = (await writer.GetAccountsAsync()).Single();
        var category = (await writer.GetCategoriesAsync()).First(value => value.SystemKey == "Food");
        var entry = new LedgerEntry { AccountId = account.Id, CategoryId = category.Id, Date = day,
            Kind = EntryKind.Expense, Amount = 1_000, Title = "D394 original expense", Note = "Original retained note" };
        if (source is null)
        {
            if (!(await writer.SaveEntryAsync(entry)).Succeeded) throw new IOException("Independent initial expense refused.");
        }
        else
        {
            entry = (await writer.GetEntriesAsync()).Single();
            if (entry.Amount != 1_300 || entry.Note != "Original retained note" || entry.Title != "D394 original expense"
                || account.Name != "D394 retained account" || entry.AccountId != account.Id || account.OpeningBalance != 100_000)
                throw new IOException("Original partial fixture differs; no continuation writes.");
        }
        var baseStore = services.GetRequiredService<ZananceStore>();
        async Task<string> OriginalBase() => JsonSerializer.Serialize(new { Accounts = await baseStore.GetAccountsAsync(),
            Entries = await baseStore.GetEntriesAsync(), Settings = await baseStore.GetSettingsAsync() });
        var baseBefore = await OriginalBase();
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var oldTheme = themes.Choice; var oldLanguage = local.CurrentLanguage;
        var captures = new List<object>(); var observations = new List<object>();
        async Task Theme(ThemeChoice choice)
        {
            if (themes.Choice == choice) return;
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { themes.Set(choice); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        async Task Ready(Func<bool> ready)
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (!ready())
            {
                if (DateTime.UtcNow >= deadline) throw new IOException("Original account publication did not complete.");
                await Task.Delay(25);
            }
        }
        IEnumerable<Microsoft.UI.Xaml.DependencyObject> NativeNodes(Microsoft.UI.Xaml.DependencyObject node)
        {
            yield return node;
            for (var index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node); index++)
                foreach (var child in NativeNodes(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node, index))) yield return child;
        }
        async Task SelectTab(AppShell shell, string route)
        {
            var target = shell.CurrentItem.Items.Single(section => section.Items.Any(content => content.Route == route));
            if (ReferenceEquals(shell.CurrentItem.CurrentItem, target)) return;
            var key = route switch { "more" => "Tab_More", "transactions" => "Tab_Transactions", "home" => "Tab_Home", _ => throw new IOException() };
            var nativeWindow = (Microsoft.UI.Xaml.Window)app.Windows[0].Handler!.PlatformView!;
            IEnumerable<Microsoft.UI.Xaml.Automation.Peers.AutomationPeer> Peers(Microsoft.UI.Xaml.Automation.Peers.AutomationPeer peer)
            {
                yield return peer;
                foreach (var child in peer.GetChildren() ?? [])
                    foreach (var descendant in Peers(child)) yield return descendant;
            }
            // WinUI selection belongs to the associated data peer, not its visual container.
            Microsoft.UI.Xaml.Automation.Provider.ISelectionItemProvider[] Choices() => NativeNodes(nativeWindow.Content)
                .OfType<Microsoft.UI.Xaml.FrameworkElement>()
                .Select(element => Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(element))
                .OfType<Microsoft.UI.Xaml.Automation.Peers.AutomationPeer>().SelectMany(Peers).Distinct()
                .Where(peer => peer.GetName() == text[key])
                .Select(peer => peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.SelectionItem))
                .OfType<Microsoft.UI.Xaml.Automation.Provider.ISelectionItemProvider>().ToArray();
            var inventory = NativeNodes(nativeWindow.Content).OfType<Microsoft.UI.Xaml.FrameworkElement>()
                .Select(element => new { Type = element.GetType().Name,
                    Name = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(element)?.GetName() })
                .Where(value => !string.IsNullOrEmpty(value.Name)).ToArray();
            await File.WriteAllTextAsync(Path.Combine(folder, "tab-pattern-inventory.json"), JsonSerializer.Serialize(inventory));
            await Ready(() => Choices().Length == 1);
            Choices()[0].Select();
            await Ready(() => ReferenceEquals(shell.CurrentItem.CurrentItem, target));
        }
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                var context = language + "-" + theme;
                if (source is not null && language == "en")
                {
                    var receipt = Path.Combine(source, context + "-account-tab-refresh.json");
                    observations.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(receipt)));
                    var destination = Path.Combine(folder, language); Directory.CreateDirectory(destination);
                    var retainedImage = context + "-account-tab-refresh.png";
                    File.Copy(Path.Combine(source, language, retainedImage), Path.Combine(destination, retainedImage));
                    captures.Add(new { Language = language, File = retainedImage });
                    continue;
                }
                await Shell.Current.GoToAsync("//home"); await Theme(theme); await SetLanguageAsync(app, local, language);
                // Keep all original Shell destinations realized rather than selecting Windows' overflow menu.
                app.Windows[0].Width = 1280; app.Windows[0].Height = 892;
                var shell = Shell.Current as AppShell ?? throw new IOException("Original Shell required.");
                var reader = fixture.GetRequiredService<LoanWorkStore>();
                var installment = new LoanInstallmentSession(reader, writer, text, local, services.GetRequiredService<IAppInteraction>());
                var vm = ActivatorUtilities.CreateInstance<AccountDetailViewModel>(services, writer,
                    fixture.GetRequiredService<GoalStore>(), fixture.GetRequiredService<HoldingStore>(), reader, installment);
                vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = account.Id });
                var page = new AccountDetailPage(vm).WithFlowDirection(local);
                await SelectTab(shell, "more"); await shell.Navigation.PushAsync(page, false);
                string Balance(long expense) => MoneyText.Format(account.OpeningBalance - expense, "EUR", local.CurrentCulture);
                var beforeAmount = (await writer.GetEntryAsync(entry.Id))!.Amount;
                await Ready(() => vm.Loading.IsReady && vm.BalanceText == Balance(beforeAmount));
                vm.ObservedText = "1234.56"; vm.Reason = "D394 retained reconciliation"; vm.ReconcileDate = day.AddDays(-1);
                await SelectTab(shell, "transactions");
                var changed = (await writer.GetEntryAsync(entry.Id))!;
                // Continue the failed native digit observation without replaying its already accepted edit.
                var retainedEdit = source is not null && context == "fa-Light";
                if (!retainedEdit)
                {
                    changed.Amount += 100;
                    if (!(await writer.SaveEntryAsync(changed)).Succeeded) throw new IOException("Independent edit refused.");
                }
                var sources = new List<string>();
                void Navigated(object? sender, ShellNavigatedEventArgs args)
                { if (ReferenceEquals(shell.CurrentPage, page)) sources.Add(args.Source.ToString()); }
                shell.Navigated += Navigated;
                try
                {
                    await SelectTab(shell, "more");
                    await Ready(() => ReferenceEquals(shell.CurrentPage, page) && vm.Loading.IsReady && vm.BalanceText == Balance(changed.Amount));
                    if (sources.Count == 0 || vm.ObservedText != "1234.56" || vm.Reason != "D394 retained reconciliation" || vm.ReconcileDate != day.AddDays(-1))
                        throw new IOException("Retained activation or reconciliation draft changed.");
                    var amount = VisualDescendants(page).OfType<Label>().First(value => value.Text == vm.BalanceText);
                    await Ready(() => amount.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0 });
                    var native = (Microsoft.UI.Xaml.Controls.TextBlock)amount.Handler!.PlatformView!;
                    await Ready(() => native.Text == NativeDigits.Apply(vm.BalanceText));
                    var output = Path.Combine(folder, language); Directory.CreateDirectory(output);
                    var image = context + "-account-tab-refresh.png";
                    await CaptureWindowAsync(app.Windows[0], Path.Combine(output, image)); captures.Add(new { Language = language, File = image });
                    var observation = new { Context = context, AccountId = account.Id, EntryId = entry.Id, BeforeExpenseMinor = beforeAmount,
                        EditedExpenseMinor = changed.Amount, ExpectedBalanceMinor = account.OpeningBalance - changed.Amount,
                        OriginalNativeMoneyPacket = native.Text, Sources = sources, ReconciliationDraftPreserved = true,
                        AcceptedEditRetainedWithoutReplay = retainedEdit };
                    observations.Add(observation);
                    await File.WriteAllTextAsync(Path.Combine(folder, context + "-account-tab-refresh.json"), JsonSerializer.Serialize(observation));
                }
                finally { shell.Navigated -= Navigated; }
            }
        }
        finally { await Shell.Current.GoToAsync("//home"); await Theme(oldTheme); await SetLanguageAsync(app, local, oldLanguage.CultureName); }
        var originalBaseUnchanged = baseBefore == await OriginalBase();
        var finalEntries = await writer.GetEntriesAsync(); var finalAccounts = await writer.GetAccountsAsync();
        if (!originalBaseUnchanged || finalEntries.Count != 1 || finalAccounts.Count != 1 || finalEntries[0].Id != entry.Id
            || finalEntries[0].Amount != 1_600 || finalEntries[0].Note != entry.Note || finalAccounts[0].OpeningBalance != account.OpeningBalance)
            throw new IOException("Independent account writes or untouched base inputs differ.");
        await File.WriteAllTextAsync(Path.Combine(folder, "account-tab-refresh.json"), JsonSerializer.Serialize(new
        { Observations = observations, SeparateFictitiousFinancialEdits = 6, OriginalBaseStoreUnchanged = originalBaseUnchanged,
            OriginalEntryIdentityAndMetadataRetained = true, OriginalThemeAndLanguageRestored = true }));
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures,
            OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = originalBaseUnchanged }));
    }
}
#endif
