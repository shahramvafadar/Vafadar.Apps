#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Explicit confirmation answers confined to an independent fictitious budget review.</summary>
    private sealed class BudgetActionsReviewInteraction : IAppInteraction
    {
        internal Func<Task<bool>> Confirm = () => Task.FromResult(false);
        internal readonly List<Exception> Failures = [];
        public Task AlertAsync(string title, string message, string cancel) => throw new InvalidOperationException("Unexpected review alert.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Confirm();
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1)
            => throw new InvalidOperationException("Unexpected review prompt.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new InvalidOperationException("Unexpected review choice.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new InvalidOperationException("Unexpected review navigation.");
        public Task ShowFailureAsync(Exception exception) { Failures.Add(exception); return Task.CompletedTask; }
    }

    // D-153 / AT-155: actual original page/targets and independent reviewed write rejection, never release activation.
    private static async Task ReviewBudgetActionsAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "budget-actions-fixture.db");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var seed = fixture.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "Fictitious original archived account", CurrencyCode = "EUR", OpeningDate = new(2026, 9, 1), IsArchived = true };
        var category = new Category { Name = "Fictitious original archived category", Kind = CategoryKind.Expense, IsArchived = true };
        await seed.SaveAccountAsync(account); await seed.SaveCategoryAsync(category); await PrepareGoalViewSettingsAsync(seed);
        var today = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
        var budget = new Budget { Year = today.Year, Month = today.Month, Calendar = PeriodCalendar.Gregorian,
            CurrencyCode = "EUR", Method = BudgetMethod.Envelopes, TotalLimit = 12345, AccountIds = [account.Id],
            CategoryLimits = [new() { CategoryId = category.Id, Limit = 2345 }] };
        await seed.SaveBudgetAsync(budget);
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
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete budget action review schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
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
            var interaction = new BudgetActionsReviewInteraction();
            var vm = new BudgetViewModel(store, fixture.GetRequiredService<PlanStore>(), fixture.GetRequiredService<GoalStore>(),
                translator, new DateFormatter(localization), localization, TimeProvider.System, interaction);
            var page = new BudgetPage(vm); await Shell.Current.Navigation.PushModalAsync(page); await vm.LoadAsync(); await Task.Delay(250);
            var viewport = FindScrollView(page) ?? throw new InvalidOperationException("Original budget view lost its scroll body.");
            var observations = new List<object>();
            async Task CheckAsync(string scenario)
            {
                foreach (var action in VisualDescendants(page).OfType<WrappingAction>().Where(Visible))
                {
                    await ScrollToViewIfNeededAsync(viewport, action); await Task.Delay(120);
                    foreach (var label in VisualDescendants(action).OfType<Label>().Where(Visible)) CheckCaption(label);
                    var button = VisualDescendants(action).OfType<Button>().Single();
                    if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.ActualWidth < 44 || native.ActualHeight < 44
                        || Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(native) != action.Text)
                        throw new InvalidOperationException("Budget action lost its complete native name or target.");
                    observations.Add(new { scenario, action.Text, native.ActualWidth, native.ActualHeight, native.IsEnabled });
                }
                await CaptureAsync(app, folder, language + "-budget-actions-" + scenario);
            }
            if (!vm.Actions.CanEdit || !vm.Actions.CanDelete || vm.Actions.CanCopyNext || vm.Actions.WorkText is null || vm.TotalLines.Count != 1)
                throw new InvalidOperationException("Paid original history/correction or independent copy rights were lost.");
            await CheckAsync("original");
            var delete = VisualDescendants(page).OfType<WrappingAction>().Single(action => action.Command == vm.DeleteCommand);
            InvokeChoiceReview(VisualDescendants(delete).OfType<Button>().Single());
            if (vm.DeleteCommand.ExecutionTask is { } cancelled) await cancelled;
            if (await StoredAsync() != original || vm.Actions.NeedsReload) throw new InvalidOperationException("Cancelled native delete wrote data.");
            interaction.Confirm = async () =>
            {
                await using var db = await factory.CreateDbContextAsync();
                await db.Budgets.Where(row => row.Id == budget.Id).ExecuteUpdateAsync(p => p.SetProperty(row => row.TotalLimit, 99999));
                return true;
            };
            InvokeChoiceReview(VisualDescendants(delete).OfType<Button>().Single());
            if (vm.DeleteCommand.ExecutionTask is { } rejected) await rejected;
            if (!vm.Actions.NeedsReload || interaction.Failures.Count != 1 || (await seed.GetBudgetsAsync()).Single().TotalLimit != 99999)
                throw new InvalidOperationException("A native confirmation deleted a changed original budget.");
            var concurrent = await StoredAsync(); await CheckAsync("rejected");
            var retry = VisualDescendants(page).OfType<WrappingAction>().Single(action => action.Command == vm.Actions.ReloadCommand);
            InvokeChoiceReview(VisualDescendants(retry).OfType<Button>().Single());
            if (vm.Actions.ReloadCommand.ExecutionTask is { } reload) await reload;
            if (!vm.Actions.CanInteract || vm.TotalLines.Count != 1 || await StoredAsync() != concurrent)
                throw new InvalidOperationException("Native retry lost original history or repeated a write.");
            await CheckAsync("reloaded");
            await Shell.Current.Navigation.PopModalAsync();
            await using (var db = await factory.CreateDbContextAsync())
                await db.Budgets.Where(row => row.Id == budget.Id).ExecuteUpdateAsync(p => p.SetProperty(row => row.TotalLimit, 12345));
            if (await StoredAsync() != original) throw new InvalidOperationException("The complete original budget action fixture was not restored.");
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-budget-actions-proof.json"), JsonSerializer.Serialize(new
            { ActualNativeActions = true, OriginalPaidHistoryAndCorrection = true, IndependentCopyDeleteRights = true,
                CancelledDeleteNoWrite = true, ChangedConfirmationRejected = true, ExplicitReadOnlyRetry = true,
                Complete25TableRowsRestored = true, RegistrationUnchanged = true, Observations = observations }));
        }
    }
}
#endif
