#if DEBUG && WINDOWS
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks original retained tabs, full read/error covers and native Retry in an independent desktop.</summary>
    private static async Task ReviewOccurrenceRefreshWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var theme = services.GetRequiredService<ThemeService>(); var store = services.GetRequiredService<ZananceStore>();
        var oldTheme = theme.Choice; var oldLanguage = local.CurrentLanguage;
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var before = await BaseRows(); var captures = new List<object>(); var cases = new List<object>();
        async Task Ready(Func<bool> ready)
        {
            var end = DateTime.UtcNow.AddSeconds(40);
            while (!ready()) { if (DateTime.UtcNow >= end) throw new IOException("Original occurrence native state not ready; retain the original process."); await Task.Delay(25); }
        }
        async Task Theme(ThemeChoice choice)
        {
            if (theme.Choice == choice) return;
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => done.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await done.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        IEnumerable<Microsoft.UI.Xaml.DependencyObject> Tree(Microsoft.UI.Xaml.DependencyObject node)
        {
            yield return node;
            for (var i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node); i++)
                foreach (var child in Tree(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node, i))) yield return child;
        }
        IEnumerable<AutomationPeer> Peers(AutomationPeer peer)
        {
            yield return peer;
            foreach (var child in peer.GetChildren() ?? []) foreach (var nested in Peers(child)) yield return nested;
        }
        async Task SelectTab(string route)
        {
            var shell = (AppShell)Shell.Current;
            var target = shell.CurrentItem.Items.Single(section => section.Items.Any(content => content.Route == route));
            if (ReferenceEquals(shell.CurrentItem.CurrentItem, target)) return;
            app.Windows[0].Width = 1280; await Task.Delay(100);
            var name = text[route == "home" ? "Tab_Home" : "Tab_Plans"];
            ISelectionItemProvider[] Choices() => Tree(((Microsoft.UI.Xaml.Window)app.Windows[0].Handler!.PlatformView!).Content)
                .OfType<Microsoft.UI.Xaml.FrameworkElement>().Select(FrameworkElementAutomationPeer.CreatePeerForElement)
                .OfType<AutomationPeer>().SelectMany(Peers).Distinct().Where(peer => peer.GetName() == name)
                .Select(peer => peer.GetPattern(PatternInterface.SelectionItem)).OfType<ISelectionItemProvider>().ToArray();
            await Ready(() => Choices().Length == 1); Choices()[0].Select();
            await Ready(() => ReferenceEquals(shell.CurrentItem.CurrentItem, target));
        }
        bool Visible(Element item)
        {
            for (Element? current = item; current is not null; current = current.Parent)
                if (current is VisualElement view && !view.IsVisible) return false;
            return true;
        }
        async Task Invoke(OccurrencePage page, ICommand command)
        {
            var button = VisualDescendants(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command));
            if (!Visible(button)) throw new IOException("Original occurrence command is covered.");
            if (command != ((OccurrenceViewModel)page.BindingContext).ReloadCommand)
                await ScrollToViewIfNeededAsync(page.FindByName<ScrollView>("ValidationViewport"), button);
            await Task.Delay(100);
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true } native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider action)
                throw new IOException("Original native occurrence Invoke unavailable.");
            action.Invoke(); await Task.Yield();
        }
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await Shell.Current.GoToAsync("//home"); await Theme(choice); await SetLanguageAsync(app, local, language);
                var context = language + "-" + choice; var directory = Path.Combine(folder, context); Directory.CreateDirectory(directory);
                var result = await OccurrenceRefreshNativeReview.RunAsync(services, directory, context,
                    async page => { await SelectTab("plans"); await Shell.Current.Navigation.PushAsync(page, false); },
                    () => SelectTab("home"), () => SelectTab("plans"),
                    async (state, page, vm) =>
                    {
                        foreach (var width in state == "loaded" ? new[] { 412d } : new[] { 360d, 412d, 1280d })
                        {
                            app.Windows[0].Width = width; app.Windows[0].Height = 892; await Task.Delay(100);
                            if (state == "loaded")
                            {
                                var entry = VisualDescendants(page).OfType<Entry>().Single(item => SemanticProperties.GetDescription(item) == text["Occurrence_ActualAmount"]);
                                if (entry.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox box || box.Text != vm.ActualAmountText)
                                    throw new IOException("Original native payment draft differs.");
                            }
                            else
                            {
                                if (VisualDescendants(page).OfType<Entry>().Any(Visible)) throw new IOException("A covered original input remains visible.");
                                var message = state == "loading" ? text["Occurrence_Loading"] : text["Occurrence_ReadFailed"];
                                var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == message && Visible(item));
                                await Ready(() => label.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0 });
                                if (((Microsoft.UI.Xaml.Controls.TextBlock)label.Handler!.PlatformView!).IsTextTrimmed) throw new IOException("Original occurrence feedback is truncated.");
                            }
                            var image = width + "-" + state + ".png";
                            await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, image)); captures.Add(new { Language = context, File = image });
                        }
                    }, Invoke,
                    async page => { if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original occurrence page changed."); await Shell.Current.Navigation.PopAsync(false); }, CancellationToken.None);
                cases.Add(result);
            }
        }
        finally { await Shell.Current.GoToAsync("//home"); await Theme(oldTheme); await SetLanguageAsync(app, local, oldLanguage.CultureName); }
        if (before != await BaseRows()) throw new IOException("The original independent base store changed.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures, Cases = cases,
            OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = true, OriginalThemeAndLanguageRestored = true }));
    }
}
#endif
