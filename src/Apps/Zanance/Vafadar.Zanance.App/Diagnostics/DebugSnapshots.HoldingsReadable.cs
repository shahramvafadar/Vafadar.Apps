#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Retires the original native Shell while its real Home read waits for drained SQLite access.</summary>
    private static async Task ReviewRetiredHomeReadAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance")
            throw new InvalidOperationException("Retirement review requires its independent installation.");
        var shell = app.Windows[0].Page as AppShell ?? throw new InvalidOperationException("Original Shell is unavailable.");
        await shell.GoToAsync("//home");
        var home = shell.CurrentPage as Features.Home.HomePage ?? throw new InvalidOperationException("Original Home is unavailable.");
        await home.PendingPublication;
        var vm = (Features.Home.HomeViewModel)home.BindingContext;
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var before = await ProtectedRowsFingerprintAsync(services);
        var changes = 0;
        System.Collections.Specialized.NotifyCollectionChangedEventHandler changed = (_, _) => changes++;
        vm.Balances.CollectionChanged += changed;
        Task pending;
        try
        {
            using (var owner = await location.Access.AcquireExclusiveAsync())
            {
                pending = vm.LoadAsync();
                if (pending.IsCompleted) throw new InvalidOperationException("Home read did not wait for actual maintenance ownership.");
                shell.RetireNavigationBindings();
            }
            await pending;
            if (changes != 0) throw new IOException("Retired Home published into its original native balance collection.");
            var localization = services.GetRequiredService<ILocalizationService>();
            await SetLanguageAsync(app, localization, localization.CurrentLanguage.CultureName == "en" ? "de" : "en");
            var replacement = app.Windows[0].Page as AppShell;
            if (replacement is null || ReferenceEquals(replacement, shell))
                throw new InvalidOperationException("Actual replacement Shell did not publish.");
            await replacement.GoToAsync("//home");
            if (replacement.CurrentPage is not Features.Home.HomePage current)
                throw new InvalidOperationException("Current Home did not publish.");
            await current.PendingPublication;
            if (changes != 0 || await ProtectedRowsFingerprintAsync(services) != before)
                throw new IOException("Shell replacement changed retired balance rows or financial storage.");
            await File.WriteAllTextAsync(Path.Combine(folder, "home-retirement-proof.json"), JsonSerializer.Serialize(new
            { ActualPendingSqliteRead = true, OriginalShellRetirement = true, RetiredNativeCollectionChanges = changes,
                ActualReplacementHomePublished = true, CompleteStoredRowsUnchanged = true, FinancialFingerprint = before }));
        }
        finally { vm.Balances.CollectionChanged -= changed; }
    }

    /// <summary>Reviews original holding pages using only explicitly marked independent fictional data and unsaved native forms.</summary>
    private static async Task ReviewReadableHoldingsAsync(App app, IServiceProvider services, string folder, bool bottomOnly = false)
    {
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.storageacceptance"
            || !File.Exists(Path.Combine(Path.GetDirectoryName(location.Path)!, "protected-review-fixture.json")))
            throw new IOException("Holding review requires its independent marked native installation.");
        var shell = app.Windows[0].Page as AppShell ?? throw new InvalidOperationException("Native holding Shell is unavailable.");
        if (shell.CurrentPage is Features.Home.HomePage home) await home.PendingPublication;
        // Fixture writes must not retire an in-flight Home resource review before native navigation starts.
        await shell.GoToAsync("//more");
        var store = services.GetRequiredService<ZananceStore>();
        await ApplyOwnedReviewModeAsync(store);
        var holdings = services.GetRequiredService<HoldingStore>();
        var type = (await holdings.GetTypesAsync()).SingleOrDefault(value => value.Name == "Synthetic long-caption holding review");
        if (type is null)
        {
            type = new AssetType { Name = "Synthetic long-caption holding review", PriceCurrencyCode = "EUR", Metal = Metal.Gold, PurityPer10000 = 7500 };
            if (!await holdings.SaveTypeAsync(type)) throw new IOException("Synthetic asset type was not prepared.");
            var place = await holdings.EnsureDefaultLocationAsync("Synthetic recorded storage location");
            var day = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
            var saved = await holdings.SaveEventAsync(new AssetEvent
            { AssetTypeId = type.Id, LocationId = place.Id, Kind = AssetEventKind.Opening, Quantity = 123456789,
                BasisAmount = 12345678900, Date = day.AddDays(-30) }, []);
            if (!saved.Succeeded) throw new IOException("Synthetic holding opening was not prepared.");
            await holdings.SaveValuationAsync(new AssetValuation
            { AssetTypeId = type.Id, CurrencyCode = "EUR", Date = day, PricePerUnitMilli = 987654321 });
        }
        var before = await ProtectedRowsFingerprintAsync(services);
        var localization = services.GetRequiredService<ILocalizationService>();
        var themes = services.GetRequiredService<Presentation.ThemeService>();
        var observations = new List<object>(); var cases = 0;
        foreach (var theme in new[] { Presentation.ThemeChoice.Light, Presentation.ThemeChoice.Dark })
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            themes.Set(theme);
            await SetLanguageAsync(app, localization, language);
            shell = app.Windows[0].Page as AppShell
                ?? throw new InvalidOperationException("Translated holding Shell is unavailable.");
            app.Windows[0].Width = width; app.Windows[0].Height = 820;
            var prefix = $"{language}-{theme.ToString().ToLowerInvariant()}-{width}";
            await shell.GoToAsync(AppShell.HoldingDetailRoute, new Dictionary<string, object> { ["id"] = type.Id });
            await Task.Delay(650);
            var detail = shell.CurrentPage;
            if (bottomOnly)
            {
                var scroll = VisualDescendants(detail).OfType<ScrollView>().First();
                foreach (var action in VisualDescendants(detail).OfType<Presentation.WrappingAction>().Where(Visible))
                {
                    await ScrollToViewIfNeededAsync(scroll, action);
                    var button = VisualDescendants(action).OfType<Button>().Single();
                    if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                        || native.ActualWidth < 44 || native.ActualHeight < 44)
                        throw new InvalidOperationException("A holding action lacks its full native target.");
                    var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native)
                        ?? throw new InvalidOperationException("Holding native action peer is unavailable.");
                    if (peer.GetName() != action.Text || peer.GetHelpText() != (SemanticProperties.GetHint(action) ?? string.Empty))
                        throw new InvalidOperationException("Holding action lost its original spoken name or hint.");
                    foreach (var caption in VisualDescendants(action).OfType<Label>().Where(Visible)) CheckCaption(caption, observations);
                }
                foreach (var row in VisualDescendants(detail).OfType<Grid>().Where(row =>
                    row.BindingContext is Features.Holdings.HoldingEventRow or Features.Holdings.ValuationRow
                    && row.Children.OfType<Button>().Any()))
                {
                    await ScrollToViewIfNeededAsync(scroll, row);
                    foreach (var caption in VisualDescendants(row).OfType<Label>().Where(Visible)) CheckCaption(caption, observations);
                    var button = row.Children.OfType<Button>().Single();
                    if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                        || native.ActualWidth < 44 || native.ActualHeight < 44)
                        throw new InvalidOperationException("Holding history lost its original native row target.");
                    var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native)!;
                    if (peer.GetName() != SemanticProperties.GetDescription(button) || button.Command is null || button.CommandParameter != row.BindingContext)
                        throw new InvalidOperationException("Holding history lost its exact original identity or command.");
                }
                if (scroll.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer viewport)
                    throw new InvalidOperationException("Native holding history viewport is unavailable.");
                if (viewport.ScrollableHeight > 0) NativeScrollProvider(viewport).SetScrollPercent(-1, 100);
                await Task.Delay(200);
                await CaptureAsync(app, folder, prefix + "-holding-history");
                if (await ProtectedRowsFingerprintAsync(services) != before)
                    throw new IOException("Native holding action/history review changed stored rows.");
                await shell.GoToAsync(".."); cases++; continue;
            }
            await CaptureAsync(app, folder, prefix + "-holding-detail");
            foreach (var action in VisualDescendants(detail).OfType<Presentation.WrappingAction>().Where(Visible))
                foreach (var caption in VisualDescendants(action).OfType<Label>().Where(Visible)) CheckCaption(caption, observations);
            await shell.GoToAsync("..");
            foreach (var route in new[] { AppShell.AssetTypeEditorRoute, AppShell.AssetEventEditorRoute })
            {
                var query = route == AppShell.AssetTypeEditorRoute
                    ? new Dictionary<string, object> { ["id"] = type.Id }
                    : new Dictionary<string, object> { ["type"] = type.Id, ["kind"] = "Purchase" };
                await shell.GoToAsync(route, query); await Task.Delay(450);
                var page = app.Windows[0].Page!.Navigation.ModalStack.LastOrDefault() ?? shell.CurrentPage;
                if (page is not Features.Holdings.AssetTypeEditorPage and not Features.Holdings.AssetEventEditorPage)
                    throw new InvalidOperationException($"Original holding editor route published {page.GetType().Name} instead of its editor.");
                var name = prefix + (route == AppShell.AssetTypeEditorRoute ? "-asset-type" : "-asset-event");
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, name + ".png"));
                var scroll = VisualDescendants(page).OfType<ScrollView>().Single();
                // Off-viewport WinUI pickers have no realized selected caption. Bring each original
                // control into the actual native viewport before measuring it; absence is never acceptance.
                foreach (var picker in VisualDescendants(page).OfType<Picker>().Where(picker => Visible(picker) && picker.SelectedIndex >= 0))
                {
                    await ScrollToViewIfNeededAsync(scroll, picker);
                    observations.Add(DebugPickerText.Check(picker, folder, name));
                }
                foreach (var action in VisualDescendants(page).OfType<Presentation.WrappingAction>().Where(Visible))
                {
                    foreach (var caption in VisualDescendants(action).OfType<Label>().Where(Visible)) CheckCaption(caption, observations);
                }
                if (scroll.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer viewport && viewport.ScrollableHeight > 0)
                    NativeScrollProvider(viewport).SetScrollPercent(-1, 0);
                await Task.Delay(150);
                var cancel = VisualDescendants(page).OfType<Button>().Single(button =>
                    SemanticProperties.GetDescription(button) == Translator.Instance["Common_Cancel"]);
                InvokeChoiceReview(cancel, observations); await Task.Delay(150);
                if (app.Windows[0].Page!.Navigation.ModalStack.Contains(page))
                    throw new IOException("Native Cancel retained the unsaved holding form.");
            }
            if (await ProtectedRowsFingerprintAsync(services) != before)
                throw new IOException("Holding presentation/cancellation changed complete stored rows.");
            cases++;
        }
        await File.WriteAllTextAsync(Path.Combine(folder, bottomOnly ? "holdings-bottom-proof.json" : "holdings-readable-proof.json"), JsonSerializer.Serialize(new
        { Cases = cases, PagesPerCase = bottomOnly ? 1 : 3, NativeCancellation = !bottomOnly, NativeNamesAndHints = bottomOnly, CompleteStoredRowsUnchanged = true,
            FinancialFingerprint = before, NativeCaptionObservations = observations.Count }));
    }
}
#endif
