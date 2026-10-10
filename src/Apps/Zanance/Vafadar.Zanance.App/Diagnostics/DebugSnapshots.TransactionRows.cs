#if DEBUG && WINDOWS
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // AT-109 checks the actual bound snapshot and only the walk-through's fictitious display state, without a Save.
    private static async Task ReviewTransactionRowsAsync(IServiceProvider services, TransactionsViewModel vm, string folder, string language)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var unchanged = System.Text.Json.JsonSerializer.Serialize(await store.GetEntriesAsync());
        var originalKind = vm.KindIndex;
        var originalUnits = DisplayUnits.All;
        var first = vm.Days.SelectMany(day => day).ToDictionary(row => row.Id);
        if (first.Count == 0) { throw new InvalidOperationException("Fictitious transaction rows are required for the reuse review."); }
        var checkedRows = 0;
        try
        {
            vm.KindIndex = originalKind == 1 ? 0 : 1;
            vm.KindIndex = originalKind;
            var repeated = vm.Days.SelectMany(day => day).ToDictionary(row => row.Id);
            if (first.Count != repeated.Count || first.Any(pair => !ReferenceEquals(pair.Value, repeated[pair.Key])))
            { throw new InvalidOperationException("Repeated filters did not preserve row identity in the same snapshot."); }

            // A process-local unit replacement exercises the display-context guard before a data reload.
            DisplayUnits.Set([new DisplayUnit("EUR", "Fictitious unit", 1)]);
            vm.KindIndex = originalKind == 1 ? 0 : 1;
            vm.KindIndex = originalKind;
            var converted = vm.Days.SelectMany(day => day).ToDictionary(row => row.Id);
            if (converted.Any(pair => ReferenceEquals(first[pair.Key], pair.Value)))
            { throw new InvalidOperationException("A unit change reused a stale formatted row."); }
            checkedRows += await CheckPresentedRowsAsync();

            DisplayUnits.Set(originalUnits);
            await vm.LoadAsync();
            var fresh = vm.Days.SelectMany(day => day).ToDictionary(row => row.Id);
            if (fresh.Count != first.Count || fresh.Any(pair => ReferenceEquals(first[pair.Key], pair.Value)))
            { throw new InvalidOperationException("A fresh data snapshot retained old row objects."); }
            checkedRows += await CheckPresentedRowsAsync();
            if (unchanged != System.Text.Json.JsonSerializer.Serialize(await store.GetEntriesAsync()))
            { throw new InvalidOperationException("The presentation review changed fictitious ledger data."); }

            File.WriteAllText(Path.Combine(folder, $"{language}-transaction-row-reuse.json"), System.Text.Json.JsonSerializer.Serialize(new
            { Rows = first.Count, CheckedRows = checkedRows, ReusedAcrossFilters = true, FreshAfterReload = true,
                DisplayUnitInvalidated = true, FinancialDataUnchanged = true }));
        }
        finally
        {
            DisplayUnits.Set(originalUnits);
            vm.KindIndex = originalKind;
            await vm.LoadAsync();
        }

        async Task<int> CheckPresentedRowsAsync()
        {
            var accounts = (await store.GetAccountsAsync()).ToDictionary(account => account.Id);
            var entries = (await store.GetEntriesAsync()).ToDictionary(entry => entry.Id);
            var translator = services.GetRequiredService<Translator>();
            var categories = new CategoryLookup(await store.GetCategoriesAsync(), translator);
            var presenter = new EntryPresenter(accounts, categories, translator, services.GetRequiredService<ILocalizationService>().CurrentCulture);
            var rows = vm.Days.SelectMany(day => day).ToArray();
            foreach (var row in rows)
            {
                var expected = presenter.Row(entries[row.Id]);
                if (row.Title != expected.Title || row.Subtitle != expected.Subtitle || row.AmountText != expected.AmountText
                    || row.Icon != expected.Icon || !row.IconColor.Equals(expected.IconColor) || !row.IconBackground.Equals(expected.IconBackground)
                    || !row.AmountColor.Equals(expected.AmountColor) || row.IsUnreviewed != expected.IsUnreviewed)
                { throw new InvalidOperationException($"A fictitious cached row differs from the current complete display presentation: title={row.Title == expected.Title}, subtitle={row.Subtitle == expected.Subtitle}, amount={row.AmountText == expected.AmountText}, icon={row.Icon == expected.Icon}, iconColor={row.IconColor.Equals(expected.IconColor)}, background={row.IconBackground.Equals(expected.IconBackground)}, amountColor={row.AmountColor.Equals(expected.AmountColor)}, review={row.IsUnreviewed == expected.IsUnreviewed}."); }
            }
            return rows.Length;
        }
    }
}
#endif
