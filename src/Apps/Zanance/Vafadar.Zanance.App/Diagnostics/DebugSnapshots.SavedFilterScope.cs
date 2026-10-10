#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // Compare the same real saved-filter action before and after an independent fictitious report scope.
    private static async Task ReviewSavedFilterScopeAsync(App app, IServiceProvider services,
        TransactionsPage page, string folder, string language)
    {
        var vm = (TransactionsViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        var saved = vm.SavedFilters.Single(filter => filter.Name == "Food");
        var accounts = await store.GetAccountsAsync();
        // This route belongs to the fictitious snapshot walk-through; create its missing review-state fixture only.
        if (!(await store.GetEntriesAsync()).Any(entry => entry.Title == "Saved scope unreviewed fixture"))
        {
            await store.SaveEntryAsync(new LedgerEntry { Title = "Saved scope unreviewed fixture", Kind = EntryKind.Expense,
                AccountId = accounts.First(account => account.Type == Core.Accounts.AccountType.Checking).Id,
                CategoryId = saved.CategoryIds[0], Amount = 1, Date = today, Review = ReviewState.Unreviewed });
            await vm.LoadAsync();
        }
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new
        { Settings = await store.GetSettingsAsync(), Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), SavedFilters = await store.GetSavedFiltersAsync() });
        string Results() => JsonSerializer.Serialize(vm.Days.Select(day => new { day.Header, day.NetText, Rows = day.ToArray() }));
        var before = await StoredAsync();
        var originalOpen = vm.IsFilterOpen;
        static void Invoke(Button button)
        {
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
            { throw new InvalidOperationException("The saved-filter review needs the associated native Invoke pattern."); }
            invoke.Invoke();
        }
        async Task ApplyAsync()
        {
            if (!vm.IsFilterOpen)
            { Invoke(VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.Command, vm.ToggleFilterCommand))); }
            await Task.Delay(300);
            var button = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.Command, vm.ApplySavedFilterCommand)
                && button.CommandParameter is SavedFilter filter && filter.Id == saved.Id);
            await ScrollToViewIfNeededAsync(page.FindByName<ScrollView>("FiltersViewport"), button);
            Invoke(button);
            await Task.Delay(300);
            if (!ReferenceEquals(Shell.Current.CurrentPage, page))
            { throw new InvalidOperationException("The saved-filter review page was retired."); }
        }
        try
        {
            vm.ClearFiltersCommand.Execute(null);
            await ApplyAsync();
            var expected = Results();
            if (vm.Days.Sum(day => day.Count) == 0)
            { throw new InvalidOperationException("The fictitious saved filter needs an independently nonempty baseline."); }
            await CaptureAsync(app, folder, language + "-saved-scope-baseline");
            var cases = new List<object>();
            foreach (var (name, key, value) in new (string Name, string Key, object Value)[]
            {
                ("currency", "currency", "USD"),
                ("accounts", "accounts", new[] { accounts.Single(account => account.Type == Core.Accounts.AccountType.Loan).Id }),
                ("confirmed", "confirmedOnly", true),
            })
            {
                await Shell.Current.GoToAsync("//transactions", new Dictionary<string, object>
                { ["from"] = today.AddDays(-30), ["to"] = today, ["kind"] = KindFilter.Expenses,
                    ["categories"] = saved.CategoryIds, [key] = value, ["scope"] = "Fictitious " + name + " report" });
                await vm.LoadAsync();
                await Task.Delay(300);
                if (Results() == expected)
                { throw new InvalidOperationException("The independent report restriction did not exercise its fixture."); }
                await ApplyAsync();
                var actual = Results();
                var storedEqual = before == await StoredAsync();
                await CaptureAsync(app, folder, language + "-saved-scope-after-" + name);
                var valid = actual == expected && vm.ScopeNote is null && storedEqual;
                var result = new { Case = name, ExpectedResults = expected, ActualResults = actual, vm.ScopeNote, SameSavedFilter = saved.Id,
                    NativeSavedFilterInvoked = true, CompleteStoredRowsUnchanged = storedEqual, NoSave = true, Pass = valid };
                cases.Add(result);
                File.WriteAllText(Path.Combine(folder, language + "-saved-filter-scope-" + name + ".json"), JsonSerializer.Serialize(result));
                if (!valid)
                { throw new InvalidOperationException("The same saved filter inherited an unrelated report scope."); }
            }
            File.WriteAllText(Path.Combine(folder, language + "-saved-filter-scope.json"), JsonSerializer.Serialize(new
            { Cases = cases, NativeSavedFilterInvoked = true, CompleteStoredRowsUnchanged = true, NoSave = true, Pass = true }));
        }
        finally
        {
            vm.ClearFiltersCommand.Execute(null);
            if (vm.IsFilterOpen != originalOpen)
            { Invoke(VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.Command, vm.ToggleFilterCommand))); }
            if (before != await StoredAsync())
            { throw new InvalidOperationException("The saved-filter scope review changed complete stored data."); }
        }
    }
}
#endif
