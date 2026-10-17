#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Qualifies the original empty plan viewport in independent hidden Windows layouts without financial writes.</summary>
    private static async Task ReviewPlanPublicationAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.planpublication" || !Path.GetFullPath(folder).StartsWith(
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "plan-publication-d318")) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase)) throw new IOException("Explicit independently owned plan layout review required.");
        if (File.Exists(Path.Combine(folder, "complete.json"))) throw new IOException("Do not repeat completed layout qualification.");
        var path = Path.Combine(folder, "plans-layout.db"); var continuing = File.Exists(path);
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var store = fixture.GetRequiredService<ZananceStore>(); var plans = fixture.GetRequiredService<PlanStore>();
        var time = services.GetRequiredService<TimeProvider>();
        if (!continuing) await store.SaveAccountAsync(new Account { Name = "D318 independent plan layout account", CurrencyCode = "EUR",
            Type = AccountType.Checking, OpeningDate = DateOnly.FromDateTime(time.GetLocalNow().DateTime), OpeningBalance = 100000 });
        var accounts = await store.GetAccountsAsync();
        if (accounts.Count != 1 || accounts[0].Name != "D318 independent plan layout account" || accounts[0].OpeningBalance != 100000
            || (await plans.GetSchedulesAsync()).Count != 0 || (await store.GetEntriesAsync()).Count != 0)
            throw new IOException("Preserve the original isolated empty plan fixture.");
        async Task<string> PacketAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync(), Schedules = await plans.GetSchedulesAsync() });
        var before = await PacketAsync();
        var local = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var window = app.Windows[0];
        var progress = Path.Combine(folder, "contexts.json");
        var observations = File.Exists(progress) ? JsonSerializer.Deserialize<List<JsonElement>>(await File.ReadAllTextAsync(progress))
            ?? throw new IOException("Original layout progress is invalid.") : new List<JsonElement>();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            if (observations.Any(row => row.GetProperty("Language").GetString() == language
                && row.GetProperty("Theme").GetString() == theme.ToString() && row.GetProperty("Width").GetInt32() == width)) continue;
            themes.Set(theme); await SetLanguageAsync(app, local, language); window.Width = width; window.Height = 820;
            var vm = new PlansViewModel(store, plans, translator, local, services.GetRequiredService<IDateFormatter>(), time,
                services.GetRequiredService<ReminderService>());
            var page = new PlansPage(vm).WithFlowDirection(local); var root = window.Page ?? throw new IOException("Original review root missing.");
            await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false);
            await vm.LoadAsync(); await Task.Delay(300);
            var viewport = page.FindByName<ScrollView>("EmptyViewport"); var action = page.FindByName<WrappingAction>("EmptyAddAction");
            var add = VisualDescendants(action).OfType<Button>().Single();
            await ScrollToViewIfNeededAsync(viewport, add); await Task.Delay(150);
            if (!vm.HasNoPlans || !vm.IsEmpty || vm.Rows.Count != 0 || !viewport.IsVisible || !add.IsVisible || !add.IsEnabled
                || !ReferenceEquals(add.Command, vm.AddCommand)) throw new IOException("Original empty plan state/command differs.");
            foreach (var label in VisualDescendants(viewport).OfType<Label>()) CheckCaption(label);
            if (add.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                || page.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement nativePage)
                throw new IOException("Original native empty-plan target missing.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(native);
            if (peer?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider || peer.GetName() != translator["Plans_Add"]
                || native.ActualWidth < 44 || native.ActualHeight < 44)
                throw new IOException("Original complete named Add Invoke target missing.");
            var origin = native.TransformToVisual(nativePage).TransformPoint(new(0, 0));
            if (origin.X < -1 || origin.X + native.ActualWidth > nativePage.ActualWidth + 1
                || origin.Y < -1 || origin.Y + native.ActualHeight > nativePage.ActualHeight + 1)
                throw new IOException("Original Add target cannot be reached within its own viewport.");
            await CaptureWindowAsync(window, Path.Combine(folder, $"{language}-{theme}-{width}.png"));
            var targetWidth = native.ActualWidth; var targetHeight = native.ActualHeight; var completeName = peer.GetName();
            await root.Navigation.PopModalAsync(false);
            if (before != await PacketAsync()) throw new IOException("Layout qualification changed stored inputs.");
            observations.Add(JsonSerializer.SerializeToElement(new { Language = language, Theme = theme.ToString(), Width = width,
                TargetWidth = targetWidth, TargetHeight = targetHeight, CompleteName = completeName, OriginalDataPreserved = true }));
            await File.WriteAllTextAsync(progress, JsonSerializer.Serialize(observations));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Contexts = observations.Count,
            OriginalDataPreserved = true, NoSaveOrSystemChange = true }));
    }
}
#endif
