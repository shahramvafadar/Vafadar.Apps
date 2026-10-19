#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original retained aggregate editor regression over a separate fictitious SQLite fixture.</summary>
internal static class RetainedAggregateReview
{
    /// <summary>Loads actual stored rows and original controls; never saves an editor draft.</summary>
    internal static async Task<object> RunAsync(App app, IServiceProvider services, string folder, int[] widths,
        Func<string, Task> languageChanged, Func<EntryEditorPage, EntryEditorViewModel, string, Task> inspect, CancellationToken ct)
    {
        if (!(AppInfo.Current.PackageName.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal) ||
            AppInfo.Current.PackageName == "pro.vafadar.zanance.androidhomereview")) throw new IOException("Independent editor identity required.");
        var path = Path.Combine(folder, "retained-aggregate.db");
        var fresh = !File.Exists(path);
        await using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = fixture.GetRequiredService<ZananceStore>();
        var from = new DateOnly(2026, 10, 1); var to = from.AddDays(6);
        var account = new Account { Name = "D392 fictitious account", CurrencyCode = "EUR", OpeningDate = from };
        if (fresh)
        {
            await store.SaveAccountAsync(account, ct);
            await store.UpdateSettingsAsync(value => { value.Mode = ExperienceMode.Simple; value.DefaultAccountId = account.Id; }, ct);
        }
        var rows = new[] { new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Date = to, Amount = 10000,
                Title = "D392 retained expense", IsAggregated = true, AggregatedFrom = from, AggregatedTo = to },
            new LedgerEntry { Kind = EntryKind.Income, AccountId = account.Id, Date = to, Amount = 8000,
                Title = "D392 retained income", IsAggregated = true, AggregatedFrom = from, AggregatedTo = to,
                Payee = "D392 fictitious payee", Note = "Original retained note", Tags = ["D392"] },
            new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, Date = to, Amount = 700,
                Title = "D392 ordinary expense" } };
        if (fresh)
        {
            foreach (var row in rows) await store.SaveEntryAsync(row, ct);
        }
        else
        {
            // Continue only the original marked shape after a native-observer correction, without fixture writes.
            var accounts = await store.GetAccountsAsync(cancellationToken: ct); var retained = await store.GetEntriesAsync(cancellationToken: ct);
            if (accounts.Count != 1 || accounts[0].Name != account.Name || accounts[0].CurrencyCode != "EUR" || accounts[0].OpeningDate != from ||
                (await store.GetSettingsAsync(ct)).Mode != ExperienceMode.Simple || retained.Count != rows.Length)
                throw new IOException("Original retained editor fixture differs; never reseed.");
            foreach (var expected in rows)
            {
                var original = retained.Single(value => value.Title == expected.Title);
                if (original.AccountId != accounts[0].Id || original.Kind != expected.Kind || original.Amount != expected.Amount || original.Date != to ||
                    original.IsAggregated != expected.IsAggregated || original.AggregatedFrom != expected.AggregatedFrom || original.AggregatedTo != expected.AggregatedTo ||
                    original.Payee != expected.Payee || original.Note != expected.Note || !original.Tags.SequenceEqual(expected.Tags))
                    throw new IOException("Original retained editor row differs; never replace it.");
            }
            rows = rows.Select(expected => retained.Single(value => value.Title == expected.Title)).ToArray();
        }
        async Task<string> Stored() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
            Entries = await store.GetEntriesAsync(cancellationToken: ct), Settings = await store.GetSettingsAsync(ct) });
        var before = await Stored(); var local = services.GetRequiredService<ILocalizationService>();
        var themes = services.GetRequiredService<ThemeService>(); var oldTheme = themes.Choice; var oldLanguage = local.CurrentLanguage;
        var contexts = new List<string>();
        async Task Theme(ThemeChoice choice)
        {
            if (themes.Choice == choice) return;
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { themes.Set(choice); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        var load = typeof(EntryEditorViewModel).GetMethod("ApplyQueryAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in widths)
            {
                await Theme(theme); await languageChanged(language);
#if WINDOWS
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
#endif
                var context = language + "-" + theme + "-" + width;
                foreach (var row in rows)
                {
#if WINDOWS
                    var retained = (Environment.GetEnvironmentVariable("VAFADAR_RETAINED_AGGREGATE_COMPLETED") ?? string.Empty).Split(',');
                    if (row.IsAggregated && retained.Contains(context + "-" + row.Kind, StringComparer.Ordinal)) continue;
#endif
                    var vm = ActivatorUtilities.CreateInstance<EntryEditorViewModel>(services, store, fixture.GetRequiredService<ReceiptWorkStore>());
                    var page = new EntryEditorPage(vm).WithFlowDirection(local);
                    var root = app.Windows[0].Page ?? throw new IOException();
                    // The independent navigation wrapper avoids a second Shell empty-query delivery.
                    await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false);
                    try
                    {
                        await ((Task)load.Invoke(vm, [new Dictionary<string, object> { ["id"] = row.Id }])!).WaitAsync(ct);
                        if (vm.IsAggregated != row.IsAggregated || vm.CanAggregate != row.IsAggregated ||
                            vm.EntryTitle != row.Title || vm.IsAggregated && (!vm.ShowDetails || vm.AggregatedFrom != from || vm.AggregatedTo != to))
                            throw new IOException("Original retained aggregate publication differs: " + JsonSerializer.Serialize(new
                            { row.Kind, row.IsAggregated, vm.CanAggregate, vm.ShowDetails, vm.AggregatedFrom, vm.AggregatedTo }));
                        if (row.IsAggregated)
                        {
                            await inspect(page, vm, context + "-" + row.Kind);
                            // An unsaved off/on change must retain access to the original switch and range.
                            vm.IsAggregated = false;
                            if (!vm.CanAggregate) throw new IOException("Retained capability disappeared during an unsaved change.");
                            vm.IsAggregated = true;
                            if (!vm.CanAggregate || vm.AggregatedFrom != from || vm.AggregatedTo != to)
                                throw new IOException("Unsaved toggle changed the original range.");
                        }
                    }
                    finally { await root.Navigation.PopModalAsync(false); }
                    if (before != await Stored()) throw new IOException("Read-only editor review changed stored inputs.");
                }
                contexts.Add(context);
            }
        }
        finally { await Theme(oldTheme); await languageChanged(oldLanguage.CultureName); }
        return new { Contexts = contexts, RetainedRowsPerContext = 2, OrdinarySimpleRowsPerContext = 1,
            OriginalStoredInputsUnchanged = before == await Stored(), OriginalThemeAndLanguageRestored = true, EditorSaves = 0 };
    }
}
#endif
