#if DEBUG && ANDROID
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Opt-in import screen check restricted to the isolated, marked reminder/import fixture profile.</summary>
internal static class DebugImportLinks
{
    private sealed record ReminderIds(Guid AccountId, Guid? GoalId = null);
    private sealed record ImportIds(Guid AccountId, Guid AggregateId, List<Guid> DetailIds);

    /// <summary>Prepares a fictitious preview only; the native UI performs the actual import and Undo.</summary>
    public static bool StartIfRequested(App app, IServiceProvider services)
    {
        if (Environment.GetEnvironmentVariable("VAFADAR_IMPORT_LINK_PROOF") != "1") { return false; }
        app.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), async () =>
        {
            var report = Path.Combine(FileSystem.AppDataDirectory, "import-link-proof.json");
            var stage = "ReadFixture";
            try { await RunAsync(app, services, report, value => stage = value); }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { Passed = false, Stage = stage, Error = ex.GetType().Name }));
            }
        });
        return true;
    }

    private static async Task RunAsync(App app, IServiceProvider services, string report, Action<string> stage)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var marker = Path.Combine(FileSystem.AppDataDirectory, "import-link-fixture.json");
        var reminderMarker = Path.Combine(FileSystem.AppDataDirectory, "review-reminder-fixture.json");
        var reminder = File.Exists(reminderMarker) ? JsonSerializer.Deserialize<ReminderIds>(await File.ReadAllTextAsync(reminderMarker)) : null;
        var fixture = File.Exists(marker) ? JsonSerializer.Deserialize<ImportIds>(await File.ReadAllTextAsync(marker)) : null;
        var accounts = await store.GetAccountsAsync();
        var entries = await store.GetEntriesAsync();
        var goals = await services.GetRequiredService<GoalStore>().GetGoalsAsync();
        if (accounts.Any(a => !(a.Id == reminder?.AccountId && a.Name is "Reminder fixture account" or "Review fixture account")
                && !(a.Id == fixture?.AccountId && a.Name == "Import fixture EUR"))
            || entries.Any(e => fixture is null || e.AccountId != fixture.AccountId || (e.Id != fixture.AggregateId && !fixture.DetailIds.Contains(e.Id)))
            || goals.Any(g => g.Id != reminder?.GoalId)
            || (await services.GetRequiredService<HoldingStore>().GetTypesAsync()).Count > 0 || (await services.GetRequiredService<PlanStore>().GetSchedulesAsync()).Count > 0)
        {
            throw new InvalidOperationException("Only marked fictitious reminder/import data may be used.");
        }

        stage("PrepareImportFixture");
        LedgerEntry aggregate;
        if (fixture is null)
        {
            // The earlier reminder fixture bypassed onboarding, so its category catalogue can still be empty.
            await store.EnsureDefaultCategoriesAsync();
            var category = (await store.GetCategoriesAsync()).First(c => c.Kind == CategoryKind.Expense && c.SystemKey == "Food");
            var account = new Account { Name = "Import fixture EUR", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 8, 1) };
            await store.SaveAccountAsync(account);
            aggregate = new LedgerEntry { AccountId = account.Id, CategoryId = category.Id, Kind = EntryKind.Expense, Amount = 41200,
                IsAggregated = true, Date = new DateOnly(2026, 9, 30), AggregatedFrom = new DateOnly(2026, 9, 1), AggregatedTo = new DateOnly(2026, 9, 30) };
            await store.SaveEntryAsync(aggregate);
            fixture = new ImportIds(account.Id, aggregate.Id, Enumerable.Range(0, 6).Select(_ => Guid.CreateVersion7()).ToList());
            await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(fixture));
        }
        else
        {
            aggregate = await store.GetEntryAsync(fixture.AggregateId)
                ?? throw new InvalidOperationException("The partial-coverage fixture aggregate must remain present.");
        }

        stage("OpenImportScreen");
        var localization = services.GetRequiredService<ILocalizationService>();
        localization.SetLanguage(localization.SupportedLanguages.First(l => l.CultureName == "en"));
        app.UserAppTheme = AppTheme.Light;
        app.ShowMainShell();
        await Shell.Current.GoToAsync(AppShell.ImportExportRoute);
        await Task.Delay(500);
        if (Shell.Current.CurrentPage.BindingContext is not ImportExportViewModel vm)
        {
            throw new InvalidOperationException("Import screen did not open.");
        }
        stage("PreviewFile");
        await vm.PreviewFixtureAsync(DebugImportFixture.Details(aggregate, fixture.DetailIds));
        var current = await store.GetEntriesAsync();
        await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new
        {
            Passed = current.Where(e => e.AccountId == fixture.AccountId).Sum(e => e.Amount) == 41200,
            vm.ImportCount, OverlapCount = vm.Overlaps.Count, vm.CanImport,
            AggregateAmount = aggregate.Amount, LedgerCount = current.Count,
        }));
    }
}
#endif
