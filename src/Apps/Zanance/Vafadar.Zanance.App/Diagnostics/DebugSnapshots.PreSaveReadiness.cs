#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // D-161 / AT-163: captured restrictions are shown in actual editors over an independently owned file.
    private static async Task ReviewPreSaveReadinessAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "pre-save-readiness.db"); var access = new ActiveItemsReviewAccess();
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<Vafadar.Zanance.Data.Commerce.ICommercialWriteAccessSource>(access).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
        var day = DateOnly.FromDateTime(DateTime.Now); var (year, month) = PeriodMath.MonthOf(day, PeriodCalendar.Gregorian, 1);
        var account = new Account { Name = "Fictitious original funding", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 10000 };
        var draftAccount = new Account { Name = "Fictitious retained draft funding", CurrencyCode = "EUR", OpeningDate = day };
        await store.SaveAccountAsync(account); await store.SaveAccountAsync(draftAccount);
        var goals = fixture.GetRequiredService<GoalStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
        await goals.SaveGoalAsync(new Goal { Name = "Fictitious original capacity", Type = GoalType.AccountBalance, AccountId = account.Id,
            CurrencyCode = "EUR", TargetAmount = 20000 });
        await store.SaveBudgetAsync(new Budget { Year = year, Month = month, CurrencyCode = "EUR", AccountIds = [account.Id], TotalLimit = 12345 });
        await ApplyOwnedReviewModeAsync(store);
        access.Current = new(path, new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("84848484-4545-5656-6767-898989898989"))));
        var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand(); var tables = new List<string>();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete pre-Save fixture schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
                command.CommandText = "SELECT * FROM \"" + table + "\""; using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
                while (await reader.ReadAsync()) { var values = new object?[reader.FieldCount]; for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i); rows.Add(JsonSerializer.Serialize(values)); }
                rows.Sort(StringComparer.Ordinal); all[table] = rows;
            }
            return JsonSerializer.Serialize(all);
        }
        var before = await StoredAsync(); var translator = services.GetRequiredService<Translator>();
        var localization = services.GetRequiredService<ILocalizationService>();
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language); var interaction = new GoalEditorReviewInteraction();
            var dates = new DateFormatter(localization);
            async Task CheckAsync(ContentPage page, string text, System.Windows.Input.ICommand save, string name)
            {
                await Task.Delay(250);
                var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == text && Visible(item));
                if (FindScrollView(page) is { } scroll) await ScrollToViewIfNeededAsync(scroll, label);
                CheckCaption(label, ProfileNativeReads);
                var target = VisualDescendants(page).OfType<Button>().Single(button => button.Command == save);
                if (target.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.IsEnabled || save.CanExecute(null))
                    throw new InvalidOperationException("The blocked native Save lost its original disabled state.");
                if (await StoredAsync() != before) throw new InvalidOperationException("Pre-Save presentation wrote complete original rows.");
                await CaptureAsync(app, folder, language + "-pre-save-" + name);
            }
            var goal = new GoalEditorViewModel(store, goals, translator, localization, dates, TimeProvider.System, holdings,
                new HoldingText(translator, localization, dates), interaction, new GoalEditorReviewNotifications());
            await goal.ApplyQueryAsync(new Dictionary<string, object>()); goal.Account = goal.Accounts.Single(item => item.Id == draftAccount.Id);
            goal.Name = "Fictitious complete unsaved name"; goal.Note = "Fictitious complete retained note";
            goal.AmountText = MoneyText.ForInput(45678, "EUR", localization.CurrentCulture);
            var original = (goal.Name, goal.Note, goal.AmountText, goal.Account.Id, goal.FirstDate);
            var goalPage = new GoalEditorPage(goal, localization); ProfileNativeReads.Add(goalPage);
            await Shell.Current.Navigation.PushModalAsync(goalPage);
            await CheckAsync(goalPage, goal.WorkText ?? throw new InvalidOperationException("Missing original quota explanation."), goal.SaveCommand, "goal-quota");
            if (goal.IsAdvanced)
            {
                goal.TypeIndex = 1;
                await CheckAsync(goalPage, translator["Commercial_RequiresPlus"], goal.SaveCommand, "goal-plus");
                goal.TypeIndex = 0;
            }
            if (original != (goal.Name, goal.Note, goal.AmountText, goal.Account?.Id, goal.FirstDate))
                throw new InvalidOperationException("Changing a blocked affordance lost the complete original goal draft.");
            var cancel = VisualDescendants(goalPage).OfType<Button>().Single(button => button.Command == goal.CancelCommand);
            InvokeChoiceReview(cancel, ProfileNativeReads); if (goal.CancelCommand.ExecutionTask is { } leaving) await leaving;
            var budget = new BudgetEditorViewModel(store, translator, localization, interaction);
            await budget.ApplyQueryAsync(new Dictionary<string, object> { ["year"] = year, ["month"] = month, ["calendar"] = PeriodCalendar.Gregorian, ["currency"] = "USD" });
            budget.TotalText = MoneyText.ForInput(98765, "USD", localization.CurrentCulture); var raw = budget.TotalText;
            var budgetPage = new BudgetEditorPage(budget, localization); ProfileNativeReads.Add(budgetPage);
            await Shell.Current.Navigation.PushModalAsync(budgetPage);
            await CheckAsync(budgetPage, budget.WorkText ?? throw new InvalidOperationException("Missing original budget capacity explanation."), budget.SaveCommand, "budget-quota");
            if (budget.TotalText != raw) throw new InvalidOperationException("The blocked budget discarded its raw input.");
            cancel = VisualDescendants(budgetPage).OfType<Button>().Single(button => button.Command == budget.CancelCommand);
            InvokeChoiceReview(cancel, ProfileNativeReads); if (budget.CancelCommand.ExecutionTask is { } budgetLeaving) await budgetLeaving;
            if (interaction.Failures.Count != 0 || await StoredAsync() != before) throw new InvalidOperationException("Native pre-Save review failed or wrote data.");
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-pre-save-readiness-proof.json"), JsonSerializer.Serialize(new
            { ActualGoalAndBudgetPages = true, CompleteQuotaAndTierReasons = true, OriginalNativeSaveDisabled = true, NativeCancelKeepsStoredRows = true,
                CompleteRawDraftsRetained = true, Complete25TablesUnchanged = true, NoSaveOrCommercialActivation = true, IsAdvanced = goal.IsAdvanced }));
        }
    }
}
#endif
