#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original budget page financial publication on separately owned fictional SQLite sources, without UI writes.</summary>
internal static class BudgetFinancialNativeReview
{
    /// <summary>Checks all three original monthly methods in each real language/theme and retains each completed observation.</summary>
    internal static async Task<object> RunAsync(IServiceProvider services, string root,
        Func<string, ThemeChoice, Task> context, Func<BudgetPage, string, Task> observe, CancellationToken ct)
    {
        var observations = new List<object>(); var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var local = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        var dates = services.GetRequiredService<IDateFormatter>(); var time = services.GetRequiredService<TimeProvider>();
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        foreach (var method in new[] { BudgetMethod.Limits, BudgetMethod.Envelopes, BudgetMethod.Flex })
        {
            var source = Path.Combine(root, "original-budget-" + method); Directory.CreateDirectory(source);
            var path = Path.Combine(source, "budget.db"); var exists = File.Exists(path);
            await using var fixture = new ServiceCollection().AddSingleton(time).AddZananceData(path).BuildServiceProvider();
            fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
            if (!exists)
            {
                await using var db = await fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
                var account = new Account { Name = "Original budget cash", CurrencyCode = "EUR", OpeningBalance = 10000, OpeningDate = today.AddMonths(-2) };
                var category = new Category { Name = "Original food", Kind = CategoryKind.Expense };
                var goal = new Goal { Name = "Original earmark", CurrencyCode = "EUR", AccountId = account.Id, TargetAmount = 3000 };
                db.Accounts.Add(account); db.Categories.Add(category); db.Goals.Add(goal);
                db.GoalAllocations.Add(new GoalAllocation { GoalId = goal.Id, AccountId = account.Id, Date = today, Amount = 2500 });
                db.Settings.Add(new ZananceSettings { Mode = ExperienceMode.Advanced, DefaultAccountId = account.Id });
                db.Budgets.Add(new Core.Budgets.Budget { Year = today.Year, Month = today.Month, CurrencyCode = "EUR", Method = method,
                    TotalLimit = 2000, AlertsEnabled = false, CategoryLimits = [new() { CategoryId = category.Id, Limit = 1000 }] });
                foreach (var row in new[] { (EntryKind.Expense, 1000L, today, ReviewState.Confirmed),
                    (EntryKind.Refund, 400L, today, ReviewState.Confirmed), (EntryKind.Expense, 200L, today, ReviewState.Unreviewed),
                    (EntryKind.Expense, 100L, today.AddMonths(-1), ReviewState.Confirmed), (EntryKind.Income, 99999L, today.AddMonths(1), ReviewState.Confirmed) })
                    db.Entries.Add(new LedgerEntry { AccountId = account.Id, CategoryId = category.Id, Kind = row.Item1, Amount = row.Item2, Date = row.Item3, Review = row.Item4 });
                db.Schedules.Add(new Schedule { Name = "Original monthly bill", AccountId = account.Id, Amount = 2000,
                    Kind = EntryKind.Expense, Rule = new() { Frequency = Frequency.Monthly, Start = today } });
                await db.SaveChangesAsync(ct);
            }
            var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
            foreach (var language in new[] { "en", "fa", "de" }) foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                var key = method + "-" + language + "-" + theme;
                var receipt = Path.Combine(root, key + "-complete.json");
                if (File.Exists(receipt)) { observations.Add(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(receipt, ct))); continue; }
                await context(language, theme);
                var vm = new BudgetViewModel(store, fixture.GetRequiredService<PlanStore>(), fixture.GetRequiredService<GoalStore>(),
                    translator, dates, local, time, services.GetRequiredService<IAppInteraction>());
                var page = new BudgetPage(vm).WithFlowDirection(local);
                await Shell.Current.Navigation.PushModalAsync(page);
                try
                {
                    await PlanEditorNativeReview.WaitAsync(() => vm.Actions.CanInteract && page.IsLoaded, ct);
                    string Money(long value) => MoneyText.Format(value, "EUR", local.CurrentCulture);
                    var total = vm.TotalLines.Single();
                    if (!vm.HasBudget || total.SpentText != Money(800) || total.LimitText != translator.Format("Budget_Of", Money(2000))
                        || vm.IsFlex != (method == BudgetMethod.Flex) || vm.IsEnvelopes != (method == BudgetMethod.Envelopes))
                        throw new IOException("Original complete budget total or method changed.");
                    if (method == BudgetMethod.Flex)
                    {
                        if (vm.CategoryLines.Count != 0 || vm.FlexLines.Count != 2) throw new IOException("Original flex publication incomplete.");
                    }
                    else if (vm.CategoryLines.Single().SpentText != Money(800)) throw new IOException("Original category publication incomplete.");
                    if (method == BudgetMethod.Envelopes && (vm.UnassignedText != Money(6400) || vm.EnvelopeLines.Count != 3))
                        throw new IOException("Original complete cash, goal earmark or envelopes changed.");
                    await observe(page, key);
                }
                finally
                {
                    if (ReferenceEquals(Shell.Current.Navigation.ModalStack.LastOrDefault(), page)) await Shell.Current.Navigation.PopModalAsync();
                }
                if (vm.Actions.CanInteract || before != await ImportFinancialNativeReview.StoredAsync(fixture, ct))
                    throw new IOException("Original read-only budget review changed rows or retained native ownership.");
                var result = new { Method = method.ToString(), Language = language, Theme = theme.ToString(),
                    TotalSpent = 800, Limit = 2000, Cash = method == BudgetMethod.Envelopes ? 9100 : (long?)null,
                    Earmarked = method == BudgetMethod.Envelopes ? 2500 : (long?)null, Unassigned = method == BudgetMethod.Envelopes ? 6400 : (long?)null,
                    OriginalRowsUnchanged = true, FinancialSaves = 0, OriginalPageRetired = true };
                await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(result), ct); observations.Add(result);
            }
        }
        if (baseline != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new IOException("Independent base rows changed.");
        return new { Scope = "budget-financial-read", Observations = observations, OriginalBaseUnchanged = true, FinancialSaves = 0 };
    }
}
#endif
