#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks the original Home first frame, failed reload and native retry against actual protected SQLite.</summary>
    private static async Task ReviewHomeLoadingAsync(App app, IServiceProvider services, string folder)
    {
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance"
            || !File.Exists(Path.Combine(Path.GetDirectoryName(location.Path)!, "protected-review-fixture.json")))
            throw new IOException("Home loading requires an independent marked protected installation.");
        var before = await ProtectedRowsFingerprintAsync(services);
        var local = services.GetRequiredService<ILocalizationService>();
        var themes = services.GetRequiredService<ThemeService>();
        var observations = new List<object>(); var contexts = 0;
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            themes.Set(theme); await SetLanguageAsync(app, local, language);
            app.Windows[0].Width = width; app.Windows[0].Height = 820;
            if (Shell.Current is not AppShell shell || shell.CurrentPage is not HomePage original)
                throw new IOException("Original Home root is unavailable.");
            await original.PendingPublication;
            var factory = new HomeReadObservationFactory(services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
            var store = new ZananceStore(factory, services.GetRequiredService<TimeProvider>(), services.GetRequiredService<ICommercialWriteAccessSource>());
            var vm = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store);
            var page = ActivatorUtilities.CreateInstance<HomePage>(services, vm);
            page.FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            await shell.Navigation.PushAsync(page, false);
            try
            {
                await factory.Captured.Task.WaitAsync(TimeSpan.FromSeconds(10));
                await Task.Delay(150);
                var viewport = page.FindByName<ScrollView>("ContentViewport");
                var add = page.FindByName<Button>("AddAction");
                void RequireCovered()
                {
                    if (vm.Loading.IsReady || viewport.IsVisible || add.IsVisible || add.IsEnabled)
                        throw new IOException("Unpublished Home exposed its defaults or actions.");
                }
                RequireCovered();
                if (!vm.Loading.IsLoading || vm.HasAccounts) throw new IOException("First-frame fixture is not waiting before original account publication.");
                CheckCaption(page.FindByName<Label>("LoadingCaption"), observations);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-loading.png"));
                factory.Release.TrySetResult(); await page.PendingPublication;
                if (!vm.Loading.IsReady || !vm.HasAccounts || !viewport.IsVisible || !add.IsEnabled)
                    throw new IOException("Complete original Home remained unavailable.");
                var amounts = VisualDescendants(page).OfType<AmountReadout>().Where(row => row.BindingContext is Features.Accounts.CurrencyTotal).ToArray();
                var handlers = amounts.Select(row => row.Handler).ToArray();
                factory.FailNext = true;
                var refused = false;
                try { await vm.LoadAsync(); }
                catch (IOException) { refused = true; }
                if (!refused || !vm.Loading.HasFailed) throw new IOException("The original failed read did not retain failure/retry state.");
                RequireCovered(); await Task.Delay(100);
                CheckCaption(page.FindByName<Label>("LoadFailureCaption"), observations);
                var retry = page.FindByName<WrappingAction>("RetryLoadAction");
                CheckCaption(VisualDescendants(retry).OfType<Label>().Single(), observations);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-failed.png"));
                InvokeChoiceReview(VisualDescendants(retry).OfType<Button>().Single());
                if (vm.RetryLoadCommand.ExecutionTask is not { } retryTask) throw new IOException("Native retry did not invoke the original command.");
                await retryTask;
                if (!vm.Loading.IsReady || vm.Loading.HasFailed || !vm.HasAccounts || !add.IsEnabled)
                    throw new IOException("Native retry did not restore the complete original dashboard.");
                for (var index = 0; index < amounts.Length; index++)
                    if (!ReferenceEquals(amounts[index].Handler, handlers[index])) throw new IOException("Loading cover disconnected an original amount handler.");
                contexts++;
            }
            finally
            {
                factory.Release.TrySetResult(); page.RetirePublication();
                await shell.Navigation.PopAsync(false); await original.PendingPublication;
            }
        }
        var layoutCases = await ReviewCapturedHomeLayoutsAsync(app, services, local);
        if (before != await ProtectedRowsFingerprintAsync(services)) throw new IOException("Home loading/retry wrote financial rows.");
        await File.WriteAllTextAsync(Path.Combine(folder, "home-loading-proof.json"), JsonSerializer.Serialize(new
        { Contexts = contexts, InitialDefaultsCovered = true, PrematureActionsUnavailable = true,
            FailedReloadCovered = true, NativeRetries = contexts, OriginalAmountHandlersRetained = true,
            FinancialRowsUnchanged = true, FinancialFingerprint = before, NativeCaptionObservations = observations.Count,
            CapturedLayoutCases = layoutCases }));
    }

    /// <summary>Checks actual saved/default/legacy layouts under the cover, restoring only independently owned fixture choices.</summary>
    private static async Task<int> ReviewCapturedHomeLayoutsAsync(App app, IServiceProvider services, ILocalizationService local)
    {
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        var settings = await db.Settings.AsNoTracking().SingleAsync();
        var hadLegacy = Preferences.Default.ContainsKey("home_layout");
        var legacy = Preferences.Default.Get("home_layout", string.Empty);
        const string syntheticLegacy = "Recent,Budget,-Accounts,-Upcoming";
        var cases = 0;
        try
        {
            Preferences.Default.Set("home_layout", syntheticLegacy);
            foreach (var language in new[] { "en", "fa", "de" })
            {
                await SetLanguageAsync(app, local, language);
                if (Shell.Current?.CurrentPage is not HomePage page || page.BindingContext is not HomeViewModel vm)
                    throw new IOException("Original Home is unavailable for captured layout review.");
                await page.PendingPublication;
                foreach (var layout in new string?[] { null, string.Empty, "Budget,Recent,-Upcoming,-Accounts" })
                {
                    // Change only this synthetic profile's exact layout field, preserving every timestamp/financial value.
                    await db.Settings.Where(row => row.Id == settings.Id).ExecuteUpdateAsync(update => update.SetProperty(row => row.HomeLayout, layout));
                    await vm.LoadAsync();
                    var expected = Core.Dashboard.HomeLayout.Parse(layout ?? syntheticLegacy);
                    var sections = page.FindByName<VerticalStackLayout>("Sections");
                    var ordered = expected.Sections.Select(state => page.FindByName<VerticalStackLayout>(state.Section + "Section")).ToArray();
                    if (vm.CapturedHomeLayout != layout || !sections.Children.Where(child => ordered.Contains(child)).SequenceEqual(ordered)
                        || expected.Sections.Any(state => !state.IsVisible && page.FindByName<VerticalStackLayout>(state.Section + "Section").IsVisible))
                        throw new IOException("Captured profile/default/legacy layout differs from the original native sections.");
                    var handlers = ordered.Select(section => section.Handler).ToArray();
                    page.ApplyLayout(); page.ApplyLayout();
                    if (!ordered.Select(section => section.Handler).SequenceEqual(handlers))
                        throw new IOException("Repeated captured layout detached original native section handlers.");
                    cases++;
                }
            }
        }
        finally
        {
            await db.Settings.Where(row => row.Id == settings.Id).ExecuteUpdateAsync(update => update.SetProperty(row => row.HomeLayout, settings.HomeLayout));
            if (hadLegacy) Preferences.Default.Set("home_layout", legacy); else Preferences.Default.Remove("home_layout");
            if (Shell.Current?.CurrentPage?.BindingContext is HomeViewModel vm) await vm.LoadAsync();
        }
        return cases;
    }

    /// <summary>Delays and fails only the first captured original read; all queries and data remain in production code.</summary>
    private sealed class HomeReadObservationFactory(IDbContextFactory<ZananceDbContext> inner) : IDbContextFactory<ZananceDbContext>
    {
        private bool held;
        /// <summary>Signals that the actual protected context and its original lease were captured.</summary>
        public TaskCompletionSource Captured { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Releases that same captured read without replacing the context or file.</summary>
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Requests one fictitious I/O failure after the next actual context capture.</summary>
        public bool FailNext { get; set; }
        /// <inheritdoc />
        public ZananceDbContext CreateDbContext() => inner.CreateDbContext();
        /// <inheritdoc />
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
        {
            var db = await inner.CreateDbContextAsync(ct);
            try
            {
                if (!held) { held = true; Captured.TrySetResult(); await Release.Task.WaitAsync(ct); }
                if (FailNext) { FailNext = false; throw new IOException("Fictitious original Home read failure."); }
                return db;
            }
            catch { await db.DisposeAsync(); throw; }
        }
    }
}
#endif
