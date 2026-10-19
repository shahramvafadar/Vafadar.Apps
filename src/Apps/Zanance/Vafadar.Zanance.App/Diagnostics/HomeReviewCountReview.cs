#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Qualifies the original Home review count using a distinct fictitious cross-currency transaction.</summary>
internal static class HomeReviewCountReview
{
    /// <summary>Publishes original pages, checks native captions and preserves every seeded fixture field.</summary>
    internal static async Task<object> RunAsync(App app, IServiceProvider services, string folder, double[] widths,
        Func<string, Task> setLanguage, Func<HomePage, string, Task> inspect, CancellationToken ct, bool retainedFixture = false, bool gettingStarted = false)
    {
        var package = AppInfo.Current.PackageName;
        if (package != "pro.vafadar.zanance.androidhomereview" && !package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal))
            throw new IOException("Independent original Home review identity required.");
        Directory.CreateDirectory(folder);
        for (var item = new DirectoryInfo(folder); item is not null; item = item.Parent)
            if ((item.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected review folder refused.");
        var path = Path.Combine(folder, gettingStarted ? "getting-started.db" : "review-count.db");
        if (File.Exists(path) != retainedFixture) throw new IOException("Review-count fixture ownership does not match the requested continuation.");
        using var data = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        if (!retainedFixture) await Task.Run(() => data.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = data.GetRequiredService<ZananceStore>();
        var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        var source = new Account { Name = "Fictitious EUR", CurrencyCode = "EUR", OpeningDate = today, OpeningBalance = 20000 };
        var destination = new Account { Name = "Fictitious USD", CurrencyCode = "USD", OpeningDate = today, OpeningBalance = 10000 };
        var entry = new LedgerEntry { AccountId = source.Id, ToAccountId = destination.Id, Kind = EntryKind.Transfer,
            Date = today, Amount = 100, ToAmount = 120, Review = ReviewState.Unreviewed, Title = "Fictitious FX review", Tags = ["preserved"] };
        if (!retainedFixture)
        {
            await store.SaveAccountAsync(source, ct); await store.SaveAccountAsync(destination, ct);
            await store.SaveEntryAsync(entry, cancellationToken: ct);
        }
        else
        {
            var accounts = await store.GetAccountsAsync(cancellationToken: ct);
            var entries = await store.GetEntriesAsync(cancellationToken: ct);
            if (accounts.Count != 2 || entries.Count != 1) throw new IOException("Retained original review fixture shape differs.");
            source = accounts.Single(value => value.Name == "Fictitious EUR");
            destination = accounts.Single(value => value.Name == "Fictitious USD");
            entry = entries[0];
            if (source.CurrencyCode != "EUR" || destination.CurrencyCode != "USD" || source.OpeningBalance != 20000 || destination.OpeningBalance != 10000
                || source.OpeningDate != destination.OpeningDate || entry.Date != source.OpeningDate || entry.AccountId != source.Id || entry.ToAccountId != destination.Id
                || entry.Kind != EntryKind.Transfer || entry.Amount != 100 || entry.ToAmount != 120 || entry.Review != ReviewState.Unreviewed
                || entry.Title != "Fictitious FX review" || !entry.Tags.SequenceEqual(new[] { "preserved" }))
                throw new IOException("Retained original review inputs differ; do not replace them.");
        }
        var plans = data.GetRequiredService<PlanStore>();
        if (gettingStarted)
        {
            if (retainedFixture) throw new IOException("Fresh original getting-started fixture required.");
            await plans.SaveScheduleAsync(new Vafadar.Zanance.Core.Plans.Schedule
            {
                Name = "Fictitious one-off payment", AccountId = source.Id, Amount = 50,
                Rule = new() { Start = today.AddDays(10), Frequency = Vafadar.Zanance.Core.Plans.Frequency.Once },
            }, ct);
            await store.SaveBudgetAsync(new Vafadar.Zanance.Core.Budgets.Budget
            {
                CurrencyCode = "EUR", Period = Vafadar.Zanance.Core.Budgets.BudgetPeriod.Week,
                PeriodStart = today, AccountIds = [source.Id],
            }, ct);
        }
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
            Entries = await store.GetEntriesAsync(cancellationToken: ct), Settings = await store.GetSettingsAsync(ct),
            Plans = await plans.GetSchedulesAsync(ct), Budgets = await store.GetBudgetsAsync(ct) });
        var before = await StoredAsync();
        var actualEntries = await store.GetEntriesAsync(cancellationToken: ct);
        if (actualEntries.Count != 1 || actualEntries[0].Id != entry.Id || actualEntries[0].Review != ReviewState.Unreviewed)
            throw new IOException("Original fictitious transfer was not stored.");
        var local = services.GetRequiredService<ILocalizationService>();
        var theme = services.GetRequiredService<ThemeService>();
        var originalLanguage = local.CurrentLanguage.CultureName; var originalTheme = theme.Choice;
        var observations = new List<object>();
        var completed = new HashSet<string>(StringComparer.Ordinal);
        var retainedCases = 0;
        var progress = Path.Combine(folder, "review-count-progress.json");
        if (retainedFixture && File.Exists(progress))
        {
            var priorText = await File.ReadAllTextAsync(progress, ct);
            using var prior = JsonDocument.Parse(priorText);
            foreach (var item in prior.RootElement.EnumerateArray())
            {
                var context = item.GetProperty("Context").GetString();
                // The retained partial run passed only these exact English contexts before the
                // Persian digit observer failed. Never infer source/configuration for other cases.
                if (context is not ("en-Light-412" or "en-Dark-412") || !widths.SequenceEqual(new[] { 412d })
                    || item.GetProperty("Count").GetInt32() != 1 || !item.GetProperty("CompleteFixtureUnchanged").GetBoolean()
                    || item.GetProperty("UnreviewedText").GetString() != "Transactions to review: 1" || !completed.Add(context))
                    throw new IOException("Unbound retained review-count observation refused.");
                observations.Add(item.Clone()); retainedCases++;
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "review-count-retained-progress.json"), priorText, ct);
        }
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                theme.Set(choice); await setLanguage(language);
                var shell = Shell.Current ?? throw new IOException("Original Shell is unavailable.");
                foreach (var width in widths)
                {
                    var context = language + "-" + choice + "-" + width;
                    if (completed.Contains(context)) continue;
#if WINDOWS
                    app.Windows[0].Width = width; app.Windows[0].Height = 820;
#endif
                    var model = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store);
                    var page = new HomePage(model) { FlowDirection = local.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                    await shell.Navigation.PushAsync(page, false);
                    try
                    {
                        await page.PendingPublication.WaitAsync(ct);
                        var expectedText = services.GetRequiredService<Translator>().Format("Home_Unreviewed", 1);
                        if (!model.Loading.IsReady || model.UnreviewedCount != 1 || model.UnreviewedText != expectedText
                            || model.ConfirmedBalanceText is null || model.Balances.Count != 2 || !model.OpenUnreviewedCommand.CanExecute(null))
                            throw new IOException("One FX transaction did not publish as one original Home review item.");
                        if (gettingStarted && (model.FirstEntryDone || model.PlanDone || model.BudgetDone || !model.ShowGettingStarted))
                            throw new IOException("Transfer, one-off plan or weekly budget incorrectly completed a named Home step.");
                        await inspect(page, context);
                        if (before != await StoredAsync()) throw new IOException("Read-only Home review changed stored fixture fields.");
                        observations.Add(new { Context = context, Count = model.UnreviewedCount, model.UnreviewedText,
                            model.ConfirmedBalanceText, CompleteFixtureUnchanged = true, GettingStarted = gettingStarted, model.FirstEntryDone, model.PlanDone, model.BudgetDone, model.ShowGettingStarted });
                        await File.WriteAllTextAsync(progress, JsonSerializer.Serialize(observations), ct);
                    }
                    finally { await shell.Navigation.PopAsync(false); }
                }
            }
        }
        finally { theme.Set(originalTheme); await setLanguage(originalLanguage); }
        return new { Package = package, ProcessId = Environment.ProcessId, CoreMvid = typeof(LedgerCalculator).Module.ModuleVersionId,
            AppMvid = typeof(HomeViewModel).Module.ModuleVersionId, Observations = observations, CompleteFixtureUnchanged = before == await StoredAsync(),
            OneStoredTransfer = true, RetainedFixture = retainedFixture, RetainedCases = retainedCases, NoMainFinancialWrites = true };
    }
}
#endif
