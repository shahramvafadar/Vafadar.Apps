#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // D-152 / AT-154: native actions over an independent fictitious database, never the owner's profile or security state.
    private static async Task ReviewBudgetEditorAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "budget-editor-fixture.db");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var seedStore = fixture.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "Fictitious original archived account", CurrencyCode = "EUR", OpeningDate = new(2026, 9, 1), IsArchived = true };
        var category = new Category { Name = "Fictitious original archived category", Kind = CategoryKind.Expense, IsArchived = true };
        await seedStore.SaveAccountAsync(account); await seedStore.SaveCategoryAsync(category);
        var budget = new Budget { Year = 2026, Month = 10, CurrencyCode = "EUR", Method = BudgetMethod.Envelopes, Rollover = BudgetRollover.Surplus,
            TotalLimit = 12345, AccountIds = [account.Id], CategoryLimits = [new() { CategoryId = category.Id, Limit = 2345 }] };
        await seedStore.SaveBudgetAsync(budget);
        await PrepareGoalViewSettingsAsync(seedStore);
        var access = new ActiveItemsReviewAccess { Current = new(path, new(ProductPlan.Free,
            new(EntitlementScopeKind.PersonalProfile, Guid.Parse("82828282-3434-4545-6767-898989898989")))) };
        var store = new ZananceStore(factory, TimeProvider.System, access);
        var localization = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand(); var tables = new List<string>();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using (var reader = await command.ExecuteReaderAsync()) { while (await reader.ReadAsync()) tables.Add(reader.GetString(0)); }
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete budget editor review schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
                // Keep the exact complete native SQLite row packet rather than a financial subset.
                command.CommandText = "SELECT * FROM \"" + table + "\"";
                using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
                while (await reader.ReadAsync())
                {
                    var values = new object?[reader.FieldCount];
                    for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    rows.Add(JsonSerializer.Serialize(values));
                }
                rows.Sort(StringComparer.Ordinal); all[table] = rows;
            }
            return JsonSerializer.Serialize(all);
        }
        var original = await StoredAsync();
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
            var interaction = new GoalEditorReviewInteraction(); var vm = new BudgetEditorViewModel(store, translator, localization, interaction);
            await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = budget.Id });
            var page = new BudgetEditorPage(vm, localization); await Shell.Current.Navigation.PushModalAsync(page); await Task.Delay(250);
            var viewport = FindScrollView(page) ?? throw new InvalidOperationException("The actual budget editor lost its scroll body.");
            var observations = new List<object>();
            async Task CheckAsync(string scenario)
            {
                foreach (var action in VisualDescendants(page).OfType<WrappingAction>().Where(Visible))
                {
                    foreach (var label in VisualDescendants(action).OfType<Label>().Where(Visible)) CheckCaption(label);
                    var button = VisualDescendants(action).OfType<Button>().Single();
                    if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.ActualWidth < 44 || native.ActualHeight < 44
                        || Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(native) != action.Text)
                        throw new InvalidOperationException("Budget editor lost a complete native action name or 44-pixel target.");
                    observations.Add(new { scenario, action.Text, native.ActualWidth, native.ActualHeight, native.IsEnabled });
                }
                await CaptureAsync(app, folder, language + "-budget-editor-" + scenario);
            }
            if (!vm.IsReady || !vm.CanSave || vm.MethodIndex != 1 || vm.RolloverIndex != 1)
                throw new InvalidOperationException("Opening lost the retained paid original correction.");
            await CheckAsync("original");
            foreach (var row in vm.Limits)
            {
                var caption = VisualDescendants(page).OfType<Label>().Single(label => label.Text == row.Name);
                await ScrollToViewIfNeededAsync(viewport, caption); await Task.Delay(150); CheckCaption(caption);
            }
            vm.TotalText = "invalid"; vm.Limits.Single(row => row.CategoryId == category.Id).Text = "invalid";
            var save = VisualDescendants(page).OfType<WrappingAction>().Single(action => action.Command == vm.SaveCommand);
            InvokeChoiceReview(VisualDescendants(save).OfType<Button>().Single());
            if (vm.SaveCommand.ExecutionTask is { } invalidSave) await invalidSave;
            await Task.Delay(150);
            if (vm.TotalError is null || vm.Limits.Single(row => row.CategoryId == category.Id).Error is null || await StoredAsync() != original)
                throw new InvalidOperationException("Native field validation was incomplete or wrote data.");
            await CheckAsync("field-errors");
            vm.TotalText = Core.Money.MoneyText.ForInput(32145, "EUR", localization.CurrentCulture);
            vm.Limits.Single(row => row.CategoryId == category.Id).Text = Core.Money.MoneyText.ForInput(4456, "EUR", localization.CurrentCulture);
            var raw = (vm.TotalText, vm.Limits.Single(row => row.CategoryId == category.Id).Text, vm.MethodIndex, vm.RolloverIndex);
            vm.RefreshDisplay(); await vm.ApplyQueryAsync(new Dictionary<string, object>());
            if (raw != (vm.TotalText, vm.Limits.Single(row => row.CategoryId == category.Id).Text, vm.MethodIndex, vm.RolloverIndex))
                throw new InvalidOperationException("A retained display refresh rewrote budget input.");
            await using (var db = await factory.CreateDbContextAsync())
                await db.Budgets.Where(row => row.Id == budget.Id).ExecuteUpdateAsync(p => p.SetProperty(row => row.TotalLimit, 99999).SetProperty(row => row.AlertsEnabled, false));
            var concurrent = await StoredAsync();
            InvokeChoiceReview(VisualDescendants(save).OfType<Button>().Single());
            if (vm.SaveCommand.ExecutionTask is { } staleSave) await staleSave;
            await Task.Delay(200);
            if (vm.CanSave || interaction.Failures.Count != 1 || await StoredAsync() != concurrent || vm.TotalText != raw.TotalText)
                throw new InvalidOperationException("Native stale Save lost edits or mutated the changed original.");
            await CheckAsync("stale-save");
            var retry = VisualDescendants(page).OfType<WrappingAction>().Single(action => action.Command == vm.ReloadCommand);
            InvokeChoiceReview(VisualDescendants(retry).OfType<Button>().Single());
            if (vm.ReloadCommand.ExecutionTask is { } reloaded) await reloaded;
            await Task.Delay(200);
            if (!vm.RequiresReview || vm.CanSave || vm.AlertsEnabled || vm.TotalText != raw.TotalText)
                throw new InvalidOperationException("Native reload did not preserve dirty input and publish unchanged current fields.");
            await CheckAsync("review");
            var summary = VisualDescendants(page).OfType<Label>().Single(label => label.Text == vm.ReloadedSavedSummary);
            await ScrollToViewIfNeededAsync(viewport, summary); await Task.Delay(150); CheckCaption(summary);
            await CaptureAsync(app, folder, language + "-budget-editor-saved-summary");
            var keep = VisualDescendants(page).OfType<WrappingAction>().Single(action => action.Command == vm.AcceptReviewCommand);
            InvokeChoiceReview(VisualDescendants(keep).OfType<Button>().Single()); await Task.Delay(150);
            if (!vm.CanSave || vm.RequiresReview || await StoredAsync() != concurrent)
                throw new InvalidOperationException("Native review acceptance changed money or lost the retained draft.");
            var close = VisualDescendants(page).OfType<Button>().Single(button => button.Command == vm.CancelCommand);
            InvokeChoiceReview(close);
            if (vm.CancelCommand.ExecutionTask is { } closed) await closed;
            await using (var db = await factory.CreateDbContextAsync())
                await db.Budgets.Where(row => row.Id == budget.Id).ExecuteUpdateAsync(p => p.SetProperty(row => row.TotalLimit, 12345).SetProperty(row => row.AlertsEnabled, true));
            if (await StoredAsync() != original) throw new InvalidOperationException("The original complete budget editor fixture was not restored.");
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-budget-editor-proof.json"), JsonSerializer.Serialize(new
            { ActualNativeActions = true, OriginalScopeAndLimits = true, FieldErrorsTogether = true, StaleSaveRejected = true,
                ExplicitReviewedReload = true, Complete25TableRowsRestored = true, RegistrationUnchanged = true, Observations = observations }));
        }
    }
}
#endif
