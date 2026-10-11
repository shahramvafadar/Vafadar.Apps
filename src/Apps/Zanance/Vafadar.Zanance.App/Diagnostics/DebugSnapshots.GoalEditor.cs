#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Native-effect answers confined to the independently owned review; no fake controls or release registration.</summary>
    private sealed class GoalEditorReviewInteraction : IAppInteraction
    {
        internal readonly List<Exception> Failures = [];
        public Task AlertAsync(string title, string message, string cancel) => throw new InvalidOperationException("Unexpected review alert.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Task.FromResult(true);
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1)
            => throw new InvalidOperationException("Unexpected review prompt.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new InvalidOperationException("Unexpected review choice.");
        public async Task NavigateAsync(string route, IDictionary<string, object>? parameters = null)
        { if (route != "..") throw new InvalidOperationException("Unexpected review route."); await Shell.Current.Navigation.PopModalAsync(); }
        public Task ShowFailureAsync(Exception exception) { Failures.Add(exception); return Task.CompletedTask; }
    }

    /// <summary>Fictitious unavailable native notification device; the editor must never request it while opening.</summary>
    private sealed class GoalEditorReviewNotifications : IGoalEditorNotifications
    {
        public bool IsSupported => false;
        public Task<bool> EnableAsync() => throw new InvalidOperationException("Review opening requested notification permission.");
    }

    // D-147 / AT-149: actual modal, real native Invoke targets and complete independently owned data.
    private static async Task ReviewGoalEditorAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "goal-editor-fixture.db");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var store = fixture.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "Fictitious original archived funding", CurrencyCode = "EUR", OpeningDate = new(2026, 9, 1), IsArchived = true };
        await store.SaveAccountAsync(account);
        var categories = new[] { new Category { Name = "Fictitious original household category", Kind = CategoryKind.Expense, IsArchived = true },
            new Category { Name = "Fictitious retained second category", Kind = CategoryKind.Expense } };
        foreach (var category in categories) await store.SaveCategoryAsync(category);
        var goal = new Goal { Name = "Fictitious original household savings goal", Type = GoalType.AccountBalance, AccountId = account.Id,
            CurrencyCode = "EUR", TargetAmount = 50000, Note = "Fictitious original note", HomePin = 1 };
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Interval = 3, Start = new(2026, 10, 31),
            Calendar = PeriodCalendar.Persian, End = EndKind.AfterCount, Count = 12, MissingDay = MissingDayPolicy.Skip,
            DayRule = MonthDayRule.LastDayOfMonth, WeekendDays = RecurrenceRule.MaskOf([DayOfWeek.Saturday, DayOfWeek.Sunday]),
            WeekendShift = WeekendShift.After, HolidayRegion = "DE" };
        await fixture.GetRequiredService<GoalStore>().SaveGoalWithContributionPlanAsync(goal,
            new ContributionPlan { GoalId = goal.Id, Method = ContributionMethod.SpendingCut, Amount = 1000, CategoryIds = [.. categories.Select(c => c.Id)], Rule = rule });
        await PrepareGoalViewSettingsAsync(store);
        var access = new ActiveItemsReviewAccess { Current = new(path, new(ProductPlan.Free,
            new(EntitlementScopeKind.PersonalProfile, Guid.Parse("54545454-3434-4545-6767-898989898989")))) };
        var goals = new GoalStore(factory, access); var localization = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>(); var holdings = fixture.GetRequiredService<HoldingStore>();
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand(); var tables = new List<string>();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using (var reader = await command.ExecuteReaderAsync()) { while (await reader.ReadAsync()) tables.Add(reader.GetString(0)); }
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete editor review schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
                command.CommandText = "SELECT * FROM \"" + table + "\""; using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
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
            await SetLanguageAsync(app, localization, language); var dates = services.GetRequiredService<IDateFormatter>();
            var interaction = new GoalEditorReviewInteraction(); var vm = new GoalEditorViewModel(store, goals, translator, localization,
                dates, TimeProvider.System, holdings, new HoldingText(translator, localization, dates), interaction, new GoalEditorReviewNotifications());
            await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id });
            var page = new GoalEditorPage(vm, localization); await Shell.Current.Navigation.PushModalAsync(page); await Task.Delay(200);
            var viewport = FindScrollView(page) ?? throw new InvalidOperationException("Editor lost its actual scroll body.");
            var observations = new List<object>();
            async Task CheckAsync(string scenario)
            {
                // Check the changed editor captions, not decorative DateField glyphs covered by D-81's own review.
                var captions = new[] { vm.WorkText, vm.ScheduleSummary, vm.ShortMonthNote, vm.RetainedCategoriesText,
                    vm.CurrentMethodText, vm.SaveError, vm.PreviewProgress, vm.PreviewPlan,
                    vm.PreviewRequired, vm.PreviewMethod, translator["Goal_ReloadReview"] }
                    .Concat(vm.UpcomingDates).Concat(vm.ScheduleNames).Concat(vm.MethodNames).ToHashSet();
                foreach (var label in VisualDescendants(page).OfType<Label>().Where(label => Visible(label) && captions.Contains(label.Text))) CheckCaption(label);
                foreach (var action in VisualDescendants(page).OfType<WrappingAction>().Where(Visible))
                {
                    foreach (var label in VisualDescendants(action).OfType<Label>().Where(Visible)) CheckCaption(label);
                    var button = VisualDescendants(action).OfType<Button>().Single();
                    if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.ActualWidth < 44 || native.ActualHeight < 44
                        || Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(native) != action.Text)
                        throw new InvalidOperationException("Editor lost a complete spoken native action/44-pixel target.");
                    observations.Add(new { scenario, action.Text, native.ActualWidth, native.ActualHeight, native.IsEnabled });
                }
                await CaptureAsync(app, folder, language + "-goal-editor-" + scenario);
            }
            await CheckAsync("original");
            foreach (var caption in new[] { vm.WorkText, vm.ScheduleSummary, vm.RetainedCategoriesText })
            {
                var label = VisualDescendants(page).OfType<Label>().Single(item => item.Text == caption);
                await ScrollToViewIfNeededAsync(viewport, label); await CheckAsync("rule-" + observations.Count);
            }
            vm.Note = "Fictitious unsaved retained correction";
            var raw = (vm.AmountText, vm.ContributionText, vm.ScheduleIndex, vm.MethodIndex);
            vm.RefreshDisplay();
            if ((vm.AmountText, vm.ContributionText, vm.ScheduleIndex, vm.MethodIndex) != raw)
                throw new InvalidOperationException("Display refresh changed an original rule or explicit numeric draft.");
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Goals SET Name={"Fictitious concurrently changed name"}, Note={"Fictitious concurrent note"} WHERE Id={goal.Id}");
            var concurrent = await StoredAsync();
            var save = VisualDescendants(page).OfType<WrappingAction>().Single(action => action.Command == vm.SaveCommand);
            InvokeChoiceReview(VisualDescendants(save).OfType<Button>().Single()); await Task.Delay(100);
            if (vm.SaveCommand.ExecutionTask is { } saved) await saved;
            await Task.Delay(200);
            File.WriteAllText(Path.Combine(folder, language + "-stale-save-observed.json"), JsonSerializer.Serialize(new
            { Failures = interaction.Failures.Select(exception => exception.Message).ToArray(), vm.CanSave, vm.Note,
                RowsUnchanged = await StoredAsync() == concurrent, OriginalChanged = concurrent != original, vm.SaveError }));
            if (interaction.Failures.Count != 1 || vm.CanSave || vm.Note != "Fictitious unsaved retained correction" || await StoredAsync() != concurrent)
                throw new InvalidOperationException("Native stale Save replaced original rows or discarded input.");
            await CheckAsync("stale-save");
            var retry = VisualDescendants(page).OfType<WrappingAction>().Single(action => action.Command == vm.ReloadCommand);
            InvokeChoiceReview(VisualDescendants(retry).OfType<Button>().Single()); await Task.Delay(150);
            if (vm.ReloadCommand.ExecutionTask is { } reloaded) await reloaded;
            await Task.Delay(250);
            if (!vm.RequiresReview || vm.CanSave || vm.Name != "Fictitious concurrently changed name" || vm.Note != "Fictitious unsaved retained correction")
                throw new InvalidOperationException("Native reload failed to retain explicit drafts and publish current saved values.");
            await CheckAsync("review");
            var summary = VisualDescendants(page).OfType<Label>().Single(label => label.Text == vm.ReloadedSavedSummary);
            await ScrollToViewIfNeededAsync(viewport, summary); await Task.Delay(200); CheckCaption(summary);
            await CaptureAsync(app, folder, language + "-goal-editor-saved-summary");
            var keep = VisualDescendants(page).OfType<WrappingAction>().Single(action => action.Command == vm.AcceptReviewCommand);
            InvokeChoiceReview(VisualDescendants(keep).OfType<Button>().Single()); await Task.Delay(100);
            if (!vm.CanSave || vm.RequiresReview || await StoredAsync() != concurrent) throw new InvalidOperationException("Review action saved money or lost its explicit draft.");
            // Only the actor's exact independently owned fields are restored; never touch profile/security data.
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Goals SET Name={goal.Name}, Note={goal.Note} WHERE Id={goal.Id}");
            var close = VisualDescendants(page).OfType<WrappingAction>().Single(action => action.Command == vm.CancelCommand);
            InvokeChoiceReview(VisualDescendants(close).OfType<Button>().Single()); await Task.Delay(100);
            if (vm.CancelCommand.ExecutionTask is { } closed) await closed;
            if (await StoredAsync() != original) throw new InvalidOperationException("Editor review changed complete original fixture rows.");
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlRawAsync("UPDATE Settings SET Mode=0");
            var simple = new GoalEditorViewModel(store, goals, translator, localization, dates, TimeProvider.System, holdings,
                new HoldingText(translator, localization, dates), interaction, new GoalEditorReviewNotifications());
            await simple.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = goal.Id });
            var simplePage = new GoalEditorPage(simple, localization); await Shell.Current.Navigation.PushModalAsync(simplePage); await Task.Delay(150);
            if (simple.ShowMethods || simple.ScheduleIndex != 3 || simple.MethodIndex != (int)ContributionMethod.SpendingCut)
                throw new InvalidOperationException("Actual Simple mode replaced the complete original method/rule.");
            var method = VisualDescendants(simplePage).OfType<Label>().Single(label => Visible(label) && label.Text == simple.CurrentMethodText);
            CheckCaption(method); await ScrollToViewIfNeededAsync(FindScrollView(simplePage)!, method);
            await CaptureAsync(app, folder, language + "-goal-editor-simple-method");
            var simpleClose = VisualDescendants(simplePage).OfType<WrappingAction>().Single(action => action.Command == simple.CancelCommand);
            InvokeChoiceReview(VisualDescendants(simpleClose).OfType<Button>().Single()); await Task.Delay(100);
            if (simple.CancelCommand.ExecutionTask is { } simpleClosed) await simpleClosed;
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlRawAsync("UPDATE Settings SET Mode=1");
            if (await StoredAsync() != original) throw new InvalidOperationException("Simple review changed complete original fixture rows.");
            File.WriteAllText(Path.Combine(folder, language + "-goal-editor-proof.json"), JsonSerializer.Serialize(new
            { ActualModalAndNativeActions = true, Complete25TablesRestored = true, RetainedRulesCategoriesAndDrafts = true,
                ActualSimpleMethodCaption = true, RejectedStaleSave = true, ExplicitReviewedReload = true, NativeClose = true,
                RegistrationUnchanged = true, Observations = observations }));
        }
    }
}
#endif
