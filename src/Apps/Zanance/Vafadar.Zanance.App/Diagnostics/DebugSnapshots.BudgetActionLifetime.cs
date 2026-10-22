#if DEBUG && WINDOWS
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Exercises original native budget commands and accepted observer failures on one independent source.</summary>
    private static async Task ReviewBudgetActionLifetimeWindowsAsync(App app, IServiceProvider services, string folder)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(8)); var ct = timeout.Token;
        var baseRows = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var observationsOnly = Environment.GetEnvironmentVariable("VAFADAR_BUDGET_ACTION_OBSERVATIONS_ONLY") == "1";
        var sourceRun = Environment.GetEnvironmentVariable("VAFADAR_BUDGET_ACTION_SOURCE_RUN");
        var sourceFolder = string.IsNullOrEmpty(sourceRun) ? folder : Guid.TryParseExact(sourceRun, "N", out _)
            ? Path.Combine(Directory.GetParent(folder)!.FullName, sourceRun) : throw new IOException("Invalid original budget source.");
        var path = Path.Combine(sourceFolder, "budget-action-lifetime.db");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
        var today = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
        if (string.IsNullOrEmpty(sourceRun))
        {
        var account = new Account { Name = "Original independent budget account", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) };
        await store.SaveAccountAsync(account, ct); await PrepareGoalViewSettingsAsync(store);
        var source = new Core.Budgets.Budget { Year = today.Year, Month = today.Month, CurrencyCode = "EUR", TotalLimit = 12345, AlertsEnabled = false };
        var (previousYear, previousMonth) = PeriodMath.Previous(today.Year, today.Month);
        var (nextYear, nextMonth) = PeriodMath.Next(today.Year, today.Month);
        await store.SaveBudgetAsync(source, ct);
        await store.SaveBudgetAsync(BudgetPlanning.CopyTo(source, previousYear, previousMonth), ct);
        var next = BudgetPlanning.CopyTo(source, nextYear, nextMonth); next.TotalLimit = 99999; await store.SaveBudgetAsync(next, ct);
        }
        else
        {
            var originals = await store.GetBudgetsAsync(ct);
            var (year, month) = PeriodMath.Next(today.Year, today.Month);
            if (originals.Count != 3 || originals.Single(item => item.Year == year && item.Month == month).TotalLimit != (observationsOnly ? 12345 : 99999))
                throw new IOException("The retained budget source already changed; do not replay accepted writes.");
            await using var existing = await fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
            if (await existing.Entries.CountAsync(ct) != 0) throw new IOException("Unexpected retained financial rows.");
        }
        async Task<string> NonBudgets()
        {
            var all = JsonSerializer.Deserialize<SortedDictionary<string, List<string>>>(await ImportFinancialNativeReview.StoredAsync(fixture, ct))!;
            all.Remove("Budgets"); all.Remove("BudgetCategoryLimits"); return JsonSerializer.Serialize(all);
        }
        var originalNonBudgets = await NonBudgets(); var captures = new List<object>(); var outcomes = new List<object>();
        var localization = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var language = localization.CurrentLanguage.CultureName; var theme = themes.Choice;
        var accepted = 0; var retries = 0; BudgetPage? ownedPage = null;
        async Task Invoke(Page page, IAsyncRelayCommand command)
        {
            var action = VisualDescendants(page).OfType<WrappingAction>().Single(item => ReferenceEquals(item.Command, command));
            var viewport = FindScrollView(page) ?? throw new IOException("Original budget viewport missing.");
            await ScrollToViewIfNeededAsync(viewport, action);
            await PlanEditorNativeReview.WaitAsync(() => action.IsLoaded && action.Width > 0, ct);
            var button = VisualDescendants(action).OfType<Button>().Single();
            object TargetState() => new
            {
                action.Text, CommandReady = command.CanExecute(null), action.Width, action.Height, action.IsLoaded,
                NativeWidth = (button.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.Button)?.ActualWidth,
                NativeHeight = (button.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.Button)?.ActualHeight,
                NativeEnabled = (button.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.Button)?.IsEnabled,
            };
            try
            {
                await PlanEditorNativeReview.WaitAsync(() => command.CanExecute(null)
                    && button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button { IsEnabled: true, ActualWidth: >= 44, ActualHeight: >= 44 }, ct)
                    .WaitAsync(TimeSpan.FromSeconds(20), ct);
            }
            catch
            {
                await File.WriteAllTextAsync(Path.Combine(folder, "native-target-failure.json"), JsonSerializer.Serialize(TargetState()), CancellationToken.None); throw;
            }
            InvokeChoiceReview(button);
            await PlanEditorNativeReview.WaitAsync(() => command.ExecutionTask is not null, ct);
            if (command.ExecutionTask is not { } operation) throw new IOException("Original native budget command was not invoked.");
            await operation.WaitAsync(ct);
        }
        try
        {
            foreach (var name in new[] { "en", "fa", "de" }) foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetStoreThemeAsync(app, themes, choice); await SetLanguageAsync(app, localization, name);
                var interaction = new BudgetActionsReviewInteraction { Confirm = () => Task.FromResult(true) };
                var vm = new BudgetViewModel(store, fixture.GetRequiredService<PlanStore>(), fixture.GetRequiredService<GoalStore>(), translator,
                    new DateFormatter(localization), localization, TimeProvider.System, interaction);
                var page = new BudgetPage(vm); ownedPage = page; await Shell.Current.Navigation.PushModalAsync(page);
                await PlanEditorNativeReview.WaitAsync(() => page.IsLoaded && vm.Actions.CanInteract, ct);
                if (!observationsOnly && name == "en" && choice == ThemeChoice.Light)
                {
                    // One replacement, one deletion and one previous-copy; do not seed or replay them per language.
                    EventHandler changed = (_, _) => { accepted++; throw new IOException("Deliberate accepted budget observer failure."); };
                    store.Changed += changed;
                    try
                    {
                        foreach (var operation in new[] { "replace", "delete", "previous" })
                        {
                            var command = operation switch { "replace" => vm.Actions.CopyToNextCommand, "delete" => vm.Actions.DeleteCommand, _ => vm.Actions.CopyPreviousCommand };
                            await Invoke(page, command);
                            if (vm.Actions.Feedback != translator["Occurrence_SavedRefreshFailed"] || !vm.Actions.NeedsReload || interaction.Failures.Count != 0)
                                throw new IOException("A known accepted native budget result was reported as an unsaved failure.");
                            var committedRows = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
                            await command.ExecuteAsync(null);
                            if (committedRows != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new IOException("Accepted original command repeated.");
                            outcomes.Add(new { operation, Feedback = vm.Actions.Feedback, Accepted = accepted, NoRepeatedWrite = true });
                            await Invoke(page, vm.Actions.ReloadCommand); retries++;
                            if (!vm.Actions.CanInteract || committedRows != await ImportFinancialNativeReview.StoredAsync(fixture, ct))
                                throw new IOException("Native budget retry changed committed rows.");
                        }
                    }
                    finally { store.Changed -= changed; }
                }
                if (!vm.HasBudget || vm.TotalLines.Count != 1 || !vm.Actions.CanDelete) throw new IOException("Original budget presentation or current actions missing.");
                var retained = vm.Actions.DeleteCommand;
                var viewport = FindScrollView(page) ?? throw new IOException("Original budget viewport missing.");
                var delete = VisualDescendants(page).OfType<WrappingAction>().Single(item => ReferenceEquals(item.Command, retained));
                await ScrollToViewIfNeededAsync(viewport, delete);
                foreach (var label in VisualDescendants(delete).OfType<Label>().Where(Visible)) CheckCaption(label);
                var button = VisualDescendants(delete).OfType<Button>().Single();
                await PlanEditorNativeReview.WaitAsync(() => button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button
                    { IsEnabled: true, ActualWidth: >= 44, ActualHeight: >= 44 }, ct).WaitAsync(TimeSpan.FromSeconds(20), ct);
                var native = (Microsoft.UI.Xaml.Controls.Button)button.Handler!.PlatformView!;
                var nativeName = Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(native);
                await File.WriteAllTextAsync(Path.Combine(folder, name + "-" + choice + "-native-target.json"), JsonSerializer.Serialize(new
                { delete.Text, nativeName, native.ActualHeight, native.ActualWidth, native.IsEnabled }), ct);
                if (nativeName != delete.Text) throw new IOException("Original complete translated budget action lost its native name.");
                var directory = Path.Combine(folder, name); Directory.CreateDirectory(directory); var file = "budget-actions-" + choice + ".png";
                await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, file));
                captures.Add(new { Language = name, Theme = choice.ToString(), File = file, native.ActualWidth, native.ActualHeight, delete.Text });
                var beforeLeave = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
                await Shell.Current.Navigation.PopModalAsync(); ownedPage = null;
                await PlanEditorNativeReview.WaitAsync(() => !vm.Actions.CanInteract, ct);
                if (vm.Actions.CanInteract || retained.CanExecute(null)) throw new IOException("Disappeared original budget command is still armed.");
                await retained.ExecuteAsync(null);
                if (beforeLeave != await ImportFinancialNativeReview.StoredAsync(fixture, ct)) throw new IOException("Retired native page changed original rows.");
            }
            await using var db = await fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
            var ledgerRows = await db.Entries.CountAsync(ct);
            var budgets = await db.Budgets.AsNoTracking().ToListAsync(ct);
            if (accepted != (observationsOnly ? 0 : 3) || retries != (observationsOnly ? 0 : 3) || ledgerRows != 0 || budgets.Count != 3 || budgets.Any(item => item.TotalLimit != 12345)
                || originalNonBudgets != await NonBudgets() || baseRows != await ImportFinancialNativeReview.StoredAsync(services, ct))
                throw new IOException("Original accepted budget packet or independent base changed.");
            await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
            {
                Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Scope = "budget-action-lifetime", Captures = captures,
                NativeAcceptedWrites = accepted, NativeRetries = retries, LedgerRows = ledgerRows, Outcomes = outcomes,
                ObservationsOnly = observationsOnly, RetainedSourceRun = sourceRun,
                OriginalBaseStoreUnchanged = true, NonBudgetRowsUnchanged = true, NoAcceptedCommandReplayed = true,
            }), ct);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "original-failure.txt"), error.ToString(), CancellationToken.None); throw;
        }
        finally
        {
            try
            {
                if (ownedPage is not null && ReferenceEquals(Shell.Current.Navigation.ModalStack.LastOrDefault(), ownedPage))
                    await Shell.Current.Navigation.PopModalAsync();
                await SetStoreThemeAsync(app, themes, theme); await SetLanguageAsync(app, localization, language);
            }
            catch (Exception cleanup)
            { await File.WriteAllTextAsync(Path.Combine(folder, "cleanup-failure.txt"), cleanup.ToString(), CancellationToken.None); }
        }
    }
}
#endif
