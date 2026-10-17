#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Rates;
using NativeNode = Microsoft.UI.Xaml.DependencyObject;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Actual reference controls and native Cancel over an independent fictitious source, never a provider or production financial write.</summary>
    private static async Task ReviewMarketAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.marketreview" || Path.GetFullPath(folder) !=
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "market-native-d299"))) throw new IOException("Exact independent market review required.");
        var complete = Path.Combine(folder, "complete.json"); if (File.Exists(complete)) throw new IOException("Completed market cases must not be replayed.");
        var path = Path.Combine(folder, "market-fixture.db");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var sourceStore = fixture.GetRequiredService<MarketReferenceStore>();
        var now = DateTimeOffset.UtcNow; var day = DateOnly.FromDateTime(now.UtcDateTime);
        var symbol = new MarketSymbol("EURUSD", "Fictitious reference / مرجع آزمایشی / Beispielkurs", "EUR", "USD", MarketUnit.Currency);
        var catalog = new MarketCatalog(1, "fictitious-native-source", "fixture-v1", "Fictitious original / منشأ آزمایشی / Beispielquelle",
            "https://source.example.invalid/reference", "https://source.example.invalid/licence", "Fictitious native qualification only",
            "Europe/Berlin", 60, 3600, true, new(2020, 1, 1), true, [symbol]);
        var quote = new MarketQuote(1, new(catalog.Provider, catalog.Revision, symbol.Id, day), catalog, symbol,
            new("original-layout-observation", day, now.AddHours(-1), 1.23456789m), now.AddMinutes(-2), now.AddMinutes(-1), now.AddMinutes(58));
        await using (var db = await fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync())
            if (!await db.MarketReferenceReceipts.AnyAsync())
                await sourceStore.ApplyAsync(await sourceStore.ReviewRateAsync(quote), true, () => { });
        var before = await HouseholdFinancialFingerprintAsync(fixture);
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var window = app.Windows[0]; var progress = Path.Combine(folder, "cases.json");
        var finished = File.Exists(progress) ? JsonSerializer.Deserialize<List<string>>(await File.ReadAllTextAsync(progress))! : [];
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            var key = $"{language}-{theme}-{width}"; if (finished.Contains(key)) continue;
            themes.Set(theme); await SetLanguageAsync(app, local, language); window.Width = width; window.Height = 820;
            var effects = new MarketLayoutRuntime(path, quote, services.GetRequiredService<IAppInteraction>());
            var model = new MarketReferenceViewModel(sourceStore, fixture.GetRequiredService<HoldingStore>(), effects, text, local,
                services.GetRequiredService<IDateFormatter>(), TimeProvider.System);
            var page = new MarketReferencePage(model).WithFlowDirection(local); var root = window.Page ?? throw new IOException();
            PageHeader.Attach(page, local.IsRightToLeft);
            var navigation = new NavigationPage(page).WithFlowDirection(local); NavigationPage.SetHasNavigationBar(page, false);
            await root.Navigation.PushModalAsync(navigation, false);
            try
            {
                for (var wait = 0; wait < 200 && !model.Loaded; wait++) await Task.Delay(25);
                if (!model.Loaded || model.Selected is not null || model.CanApply || model.History.Count != 1 || effects.Opens != 0) throw new IOException("Original local reference/history publication failed.");
                // Fixture selection changes no finance; the reviewed Save/Cancel actions themselves use actual native Invoke.
                model.Selected = model.Cached.Single(); model.SavedSource = model.History.Single();
                var reads = new List<object>(); var scroll = VisualDescendants(page).OfType<ScrollView>().Single();
                await Action(model.ReviewCommand);
                if (model.ReviewText is null || !model.ReviewText.Contains("original-layout-observation", StringComparison.Ordinal)) throw new IOException("Original source terms unavailable.");
                await CaptureWindowAsync(window, Path.Combine(folder, key + "-source.png"));
                await Action(model.ApplyCommand);
                if (model.Error != text["Market_StaleRequired"] || effects.Confirms != 0) throw new IOException("Stale reference bypassed explicit acknowledgment.");
                var box = VisualDescendants(page).OfType<CheckBox>().Single(control => SemanticProperties.GetDescription(control) == text["Market_StaleConsent"]);
                await ScrollToViewIfNeededAsync(scroll, box);
                if (box.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.CheckBox nativeBox ||
                    FrameworkElementAutomationPeer.CreatePeerForElement(nativeBox)?.GetPattern(PatternInterface.Toggle) is not IToggleProvider toggle) throw new IOException("Native stale choice unavailable.");
                toggle.Toggle(); await Task.Delay(50);
                var apply = VisualDescendants(page).OfType<WrappingAction>().Single(a => ReferenceEquals(a.Command, model.ApplyCommand));
                InvokeChoiceReview(VisualDescendants(apply).OfType<Button>().Single(), reads);
                if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } xamlRoot }) throw new IOException();
                NativeButton? cancel = null;
                for (var wait = 0; wait < 200 && cancel is null; wait++)
                {
                    cancel = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).SelectMany(p => Nodes(p.Child)).OfType<NativeButton>()
                        .FirstOrDefault(b => FrameworkElementAutomationPeer.CreatePeerForElement(b)?.GetName() == text["Common_Cancel"]);
                    if (cancel is null) await Task.Delay(25);
                }
                if (cancel is null || FrameworkElementAutomationPeer.CreatePeerForElement(cancel)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                {
                    await File.WriteAllTextAsync(Path.Combine(folder, key + "-failure-state.json"), JsonSerializer.Serialize(new { model.Error,
                        model.IsBusy, model.CanApply, model.StaleAcknowledged, effects.Confirms,
                        Buttons = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).SelectMany(p => Nodes(p.Child)).OfType<NativeButton>()
                            .Select(b => FrameworkElementAutomationPeer.CreatePeerForElement(b)?.GetName()).ToArray() }));
                    throw new IOException("Native reference Cancel unavailable.");
                }
                await CaptureWindowAsync(window, Path.Combine(folder, key + "-cancel.png")); invoke.Invoke();
                for (var wait = 0; wait < 200 && model.IsBusy; wait++) await Task.Delay(25);
                foreach (var label in VisualDescendants(page).OfType<Label>().Where(l => Visible(l) && l.FontAutoScalingEnabled && !string.IsNullOrEmpty(l.Text)))
                { await ScrollToViewIfNeededAsync(scroll, label); await Task.Delay(20); CheckCaption(label, reads); }
                foreach (var action in VisualDescendants(page).OfType<WrappingAction>().Where(Visible))
                    if (VisualDescendants(action).OfType<Button>().Single().Handler?.PlatformView is not NativeButton { ActualWidth: >= 44, ActualHeight: >= 44 })
                        throw new IOException("Reference action target smaller than44.");
                await Action(model.CheckCommand); if (model.Availability != text["Market_Unconfigured"]) throw new IOException("Closed provider/manual fallback unavailable.");
                if (effects.Confirms != 1 || effects.Opens != 1 || model.Outcome is not null || before != await HouseholdFinancialFingerprintAsync(fixture)) throw new IOException("Original native Cancel changed finance.");
                finished.Add(key); await File.WriteAllTextAsync(progress, JsonSerializer.Serialize(finished));
                await File.WriteAllTextAsync(Path.Combine(folder, key + "-proof.json"), JsonSerializer.Serialize(new { NativeReads = reads.Count, FinancialHash = before,
                    NativeCancel = true, StaleConsent = true, FictitiousSource = true, RealProvider = false }));
                async Task Action(System.Windows.Input.ICommand command)
                {
                    var target = VisualDescendants(page).OfType<WrappingAction>().Single(a => ReferenceEquals(a.Command, command));
                    await ScrollToViewIfNeededAsync(scroll, target); InvokeChoiceReview(VisualDescendants(target).OfType<Button>().Single(), reads);
                    await Task.Delay(50); for (var wait = 0; wait < 200 && model.IsBusy; wait++) await Task.Delay(25);
                }
            }
            finally { await root.Navigation.PopModalAsync(false); }
        }
        await File.WriteAllTextAsync(complete, JsonSerializer.Serialize(new { Cases = finished.Count, FinancialHash = before, RealProvider = false }));
        static IEnumerable<NativeNode> Nodes(NativeNode node)
        { yield return node; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Nodes(VisualTreeHelper.GetChild(node, i))) yield return child; }
    }
    private sealed class MarketLayoutRuntime(string path, MarketQuote quote, IAppInteraction interaction) : IMarketRuntime, IMarketRemote
    {
        internal int Opens, Confirms;
        public bool NetworkAvailable => true;
        public string Destination => "https://localhost/fictitious-market-layout";
        public MarketUiAccess Capture() => new(path, () => { }, CancellationToken.None);
        public Task OpenAccountAsync() => throw new IOException("No account navigation belongs to this fixture.");
        public Task<IReadOnlyList<MarketQuote>> ReadAsync(MarketUiAccess access, CancellationToken ct) => Task.FromResult<IReadOnlyList<MarketQuote>>([quote]);
        public Task<IMarketRemote> OpenAsync(MarketUiAccess access, CancellationToken ct) { Opens++; return Task.FromResult<IMarketRemote>(this); }
        public Task<bool> ConfirmAsync(string title, string terms, string accept, string cancel) { Confirms++; return interaction.ConfirmAsync(title, terms, accept, cancel); }
        public Task<MarketAvailability> AvailabilityAsync(CancellationToken ct) => Task.FromResult(new MarketAvailability(null));
        public Task<MarketQuote> FetchAsync(MarketCatalog catalog, MarketRequest request, CancellationToken ct) => throw new IOException("No provider fetch belongs to this layout fixture.");
        public void Dispose() { }
    }
}
#endif
