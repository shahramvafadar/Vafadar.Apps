#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Data.Rates;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original daily-reference and pre-payment pages over isolated synthetic evidence, without upstream or payment dispatch.</summary>
internal static class EcbReferenceReview
{
    /// <summary>Runs only affected daily provenance and free-original surfaces; platform callbacks inspect native controls.</summary>
    internal static async Task<object> RunAsync(App app, IServiceProvider services, string folder, int[] widths,
        Func<string, Task> languageChanged, Func<Page, string, Task> inspect,
        Func<Button, Task> invoke, CancellationToken ct)
    {
        if (!(AppInfo.Current.PackageName.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal) ||
            AppInfo.Current.PackageName == "pro.vafadar.zanance.androidhomereview")) throw new IOException("Independent ECB review identity required.");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddZananceData(Path.Combine(folder, "ecb-synthetic.db")).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = fixture.GetRequiredService<MarketReferenceStore>(); var finance = fixture.GetRequiredService<ZananceStore>();
        var now = DateTimeOffset.UtcNow; var day = DateOnly.FromDateTime(now.UtcDateTime); var hash = new string('a', 64);
        MarketQuote Quote(string id)
        {
            var symbol = EcbReference.Catalog.Symbols.Single(value => value.Id == id); var direct = symbol.Base == "EUR";
            return new(2, new("ecb", EcbReference.Revision, id, day), EcbReference.Catalog with { Symbols = [symbol] }, symbol,
                new(EcbReference.QuoteId(day.AddDays(-1), symbol, hash), day.AddDays(-1), null, direct ? 1.25m : 0.72m)
                { Daily = new(1, hash, now, null, direct ? 1m : 1.25m, direct ? 1.25m : 0.9m, direct ? EcbReference.Direct : EcbReference.Division) },
                now, now.AddMinutes(5), now.AddDays(7));
        }
        var quotes = new[] { Quote("EUR-USD"), Quote("USD-CHF") };
        if ((await store.HistoryAsync(ct: ct)).Count == 0) await store.ApplyAsync(await store.ReviewRateAsync(quotes[1], ct), false, () => { }, ct);
        async Task<string> Stored() => JsonSerializer.Serialize(new { Rates = await finance.GetRatesAsync(ct),
            Entries = await finance.GetEntriesAsync(cancellationToken: ct), History = await store.HistoryAsync(ct: ct) });
        var before = await Stored(); var local = services.GetRequiredService<ILocalizationService>();
        var themes = services.GetRequiredService<ThemeService>(); var oldTheme = themes.Choice; var oldLanguage = local.CurrentLanguage;
        var text = services.GetRequiredService<Translator>(); var contexts = new List<string>();
        async Task SetThemeAsync(ThemeChoice choice)
        {
            if (themes.Choice == choice) return;
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { themes.Set(choice); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in widths)
            {
                ct.ThrowIfCancellationRequested(); await SetThemeAsync(theme); await languageChanged(language);
#if WINDOWS
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
#endif
                var context = language + "-" + theme + "-" + width;
                var effects = new Effects(Path.Combine(folder, "ecb-synthetic.db"), quotes);
                var vm = new MarketReferenceViewModel(store, fixture.GetRequiredService<HoldingStore>(), effects, text, local, new DateFormatter(local), TimeProvider.System);
                await vm.LoadAsync(); var page = new MarketReferencePage(vm).WithFlowDirection(local);
                var root = app.Windows[0].Page ?? throw new IOException();
                await PushAsync(page);
                try
                {
                    for (var wait = 0; wait < 200 && (!vm.Loaded || vm.IsBusy); wait++) await Task.Delay(25, ct);
                    if (!vm.Loaded || vm.IsBusy) throw new IOException("Original reference page remained covered.");
                    foreach (var pair in new[] { "EUR-USD", "USD-CHF" })
                    {
                        vm.Selected = vm.Cached.Single(value => value.Quote.Symbol.Id == pair);
                        vm.SavedSource = vm.History.Single();
                        var button = Views(page).OfType<Button>().Single(value => value.Command == vm.ReviewCommand);
                        await invoke(button); if (vm.ReviewCommand.ExecutionTask is { } pending) await pending;
                        if (vm.ReviewText is null || !vm.ReviewText.Contains(text["Market_EcbFreeOriginal"], StringComparison.Ordinal) ||
                            !vm.ReviewText.Contains(text[pair == "EUR-USD" ? "Market_EcbDirect" : "Market_EcbCalculated"], StringComparison.Ordinal) ||
                            !vm.ReviewText.Contains(text["Market_EcbDateOnly"], StringComparison.Ordinal) || vm.ReviewText.Contains("0001", StringComparison.Ordinal))
                            throw new IOException("Original daily precision or terms differ.");
                        await inspect(page, context + "-" + pair);
                    }
                }
                finally { await PopAsync(); }
                var plans = new ProductPlansPage(new ProductPlansViewModel(fixture.GetRequiredService<CommercialAccessStore>(), text,
                    services.GetRequiredService<IAppInteraction>()), local).WithFlowDirection(local);
                await PushAsync(plans);
                try
                {
                    var planModel = (ProductPlansViewModel)plans.BindingContext;
                    for (var wait = 0; wait < 200 && !planModel.Loading.IsReady; wait++) await Task.Delay(25, ct);
                    if (!planModel.Loading.IsReady) throw new IOException("Original plan page remained covered.");
                    await inspect(plans, context + "-plans");
                }
                finally { await PopAsync(); }
                if (before != await Stored() || effects.Confirms != 0) throw new IOException("Read-only source review changed finance or opened payment.");
                contexts.Add(context);
                Task PushAsync(Page target)
                {
#if ANDROID
                    // Use the real Shell detail stack: a diagnostic modal can remain shown without input-window focus.
                    return root.Navigation.PushAsync(target, false);
#else
                    return root.Navigation.PushModalAsync(new NavigationPage(target).WithFlowDirection(local), false);
#endif
                }
                async Task PopAsync()
                {
#if ANDROID
                    await root.Navigation.PopAsync(false);
#else
                    await root.Navigation.PopModalAsync(false);
#endif
                }
            }
        }
        finally { await SetThemeAsync(oldTheme); await languageChanged(oldLanguage.CultureName); }
        return new { Contexts = contexts, OriginalStoredInputsUnchanged = before == await Stored(),
            OriginalThemeRestored = themes.Choice == oldTheme, OriginalLanguageRestored = local.CurrentLanguage == oldLanguage,
            UpstreamRequests = 0, PaymentOrFinancialCommandsInvoked = false, SyntheticSourceEvidence = true, MainPackageTouched = false };
    }
    private static IEnumerable<Element> Views(Element parent)
    { yield return parent; if (parent is IVisualTreeElement tree) foreach (var child in tree.GetVisualChildren().OfType<Element>()) foreach (var value in Views(child)) yield return value; }
    private sealed class Effects(string file, MarketQuote[] quotes) : IMarketRuntime
    {
        internal int Confirms;
        public bool NetworkAvailable => false;
        public MarketUiAccess Capture() => new(file, () => { }, CancellationToken.None);
        public Task<IReadOnlyList<MarketQuote>> ReadAsync(MarketUiAccess access, CancellationToken ct) => Task.FromResult<IReadOnlyList<MarketQuote>>(quotes);
        public Task OpenAccountAsync() => throw new IOException("No identity navigation in source review.");
        public Task<IMarketRemote> OpenAsync(MarketUiAccess access, CancellationToken ct) => throw new IOException("No network in source review.");
        public Task<bool> ConfirmAsync(string title, string originalTerms, string accept, string cancel)
        { Confirms++; throw new IOException("Financial Save is outside native source layout review."); }
    }
}
#endif
