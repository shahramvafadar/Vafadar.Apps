#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Controls;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks original compact and saved-filter native actions using a fresh independently owned transaction file.</summary>
    private static async Task ReviewFilterActionsAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "filter-actions.db");
        if (File.Exists(path)) throw new IOException("A fresh independent filter fixture is required.");
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var store = fixture.GetRequiredService<ZananceStore>(); var time = services.GetRequiredService<TimeProvider>();
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var account = new Account { Name = "Fictitious original filter account", CurrencyCode = "EUR", OpeningDate = today, OpeningBalance = 10000 };
        await store.SaveAccountAsync(account);
        await store.SaveEntryAsync(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1234, Date = today,
            Title = "Fictitious retained filter result", Tags = ["retained"] });
        string[] names = ["Fictitious original transaction filter with complete long name", "Fiktiver gespeicherter Transaktionsfilter mit langem Namen", "فیلتر آزمایشی تراکنش‌های اصلی با نام کامل و توضیح بلند"];
        foreach (var name in names)
            await store.SaveSavedFilterAsync(new SavedFilter { Name = name, Period = 3, Search = "#retained" });
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Settings = await store.GetSettingsAsync(),
            Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Filters = await store.GetSavedFiltersAsync() });
        var before = await StoredAsync(); var translator = services.GetRequiredService<Translator>();
        var localization = services.GetRequiredService<ILocalizationService>(); var observations = new List<object>();
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
            var vm = new TransactionsViewModel(store, translator, localization, new DateFormatter(localization), time,
                new UndoService(store, time), services.GetRequiredService<IAppInteraction>());
            var page = new TransactionsPage(vm); await Shell.Current.Navigation.PushAsync(page); await vm.LoadAsync();
            vm.IsFilterOpen = true; await Task.Delay(300);
            var viewport = page.FindByName<ScrollView>("FiltersViewport");
            async Task CheckAsync(Button button, string kind)
            {
                await ScrollToViewIfNeededAsync(viewport, button);
                if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native)
                    throw new IOException("Original filter action has no native button.");
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(native);
                var valid = native.ActualWidth >= 44 && native.ActualHeight >= 44 && !string.IsNullOrWhiteSpace(peer?.GetName())
                    && peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider;
                observations.Add(new { Language = language, Kind = kind, Name = peer?.GetName(), native.ActualWidth, native.ActualHeight, Pass = valid });
                await File.WriteAllTextAsync(Path.Combine(folder, "filter-actions-observations.json"), JsonSerializer.Serialize(observations));
                if (!valid) throw new IOException("Original filter action lost its complete native name or 44-unit target.");
            }
            foreach (var chips in VisualDescendants(page).OfType<ChoiceChips>())
                foreach (var button in VisualDescendants(chips).OfType<Button>()) await CheckAsync(button, "compact-choice");
            await CheckAsync(VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.Command, vm.SavedFilterMenuCommand)), "saved-filter-menu");
            var savedButtons = VisualDescendants(page).OfType<Button>().Where(button => ReferenceEquals(button.Command, vm.ApplySavedFilterCommand)).ToArray();
            if (savedButtons.Length != names.Length) throw new IOException("Original saved filter set is incomplete.");
            foreach (var button in savedButtons)
            {
                await CheckAsync(button, "saved-filter");
                var filter = (SavedFilter)button.CommandParameter;
                if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                    || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetName() != filter.Name)
                    throw new IOException("Saved filter lost its complete original identity.");
                if (button.Parent is Grid grid)
                    foreach (var label in VisualDescendants(grid).OfType<Label>().Where(Visible)) CheckCaption(label);
                var scroller = (Microsoft.UI.Xaml.Controls.ScrollViewer)viewport.Handler!.PlatformView!;
                var origin = native.TransformToVisual(scroller).TransformPoint(new Windows.Foundation.Point());
                if (origin.X < -1 || origin.X + native.ActualWidth > scroller.ViewportWidth + 1)
                    throw new IOException("Saved filter caption/action escapes its actual native width.");
                InvokeChoiceReview(button); await Task.Delay(150);
                if (vm.SearchText != "#retained" || vm.PeriodIndex != 3 || vm.Days.SelectMany(day => day).Count() != 1)
                    throw new IOException("Original saved filter action did not apply its exact query.");
                await CaptureAsync(app, folder, language + "-filter-" + Array.IndexOf(names, filter.Name));
            }
            if (before != await StoredAsync()) throw new IOException("Filter review changed original stored rows.");
            InvokeSnapshotBack(page); await Task.Delay(100);
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "filter-actions-proof.json"), JsonSerializer.Serialize(new
        { OriginalNativeActions = true, CompleteNamesAndTargets = true, ExactSavedQueriesApplied = true,
            CompleteStoredInputsUnchanged = true, FictitiousIndependentFile = true, Observations = observations }));
    }
}
#endif
