#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Controls;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Fictitious Free input confined to an independently owned snapshot database and page, never release DI.</summary>
    private sealed class ActiveItemsReviewAccess : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal bool Fail;
        public CommercialWriteAccess Capture(string databasePath) => Fail
            ? throw new InvalidOperationException("Fictitious covered-load failure.") : Current;
    }

    // D-142 / AT-144: actual form/native controls and real SQLite, with no Save or production commercial activation.
    private static async Task ReviewActiveItemsAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "active-items-fixture.db");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var store = fixture.GetRequiredService<ZananceStore>();
        var accounts = Enumerable.Range(0, 5).Select(index => new Account
        {
            Name = "Fictitious household account with a complete long identity " + index,
            CurrencyCode = "EUR", OpeningDate = new(2026, 10, 12), IsArchived = index == 4,
        }).ToArray();
        foreach (var account in accounts) await store.SaveAccountAsync(account);
        await fixture.GetRequiredService<PlanStore>().SaveSchedulesAsync(Enumerable.Range(0, 8).Select(index => new Schedule
        {
            Name = "Fictitious monthly household plan with a complete long identity " + index,
            AccountId = accounts[0].Id, Amount = 1200, Rule = new() { Start = new(2026, 10, 12), Frequency = Frequency.Monthly },
            State = index == 6 ? ScheduleState.Paused : index == 7 ? ScheduleState.Ended : ScheduleState.Active,
            PausedFrom = index == 6 ? new DateOnly(2026, 10, 12) : null,
        }));
        for (var index = 0; index < 5; index++) await fixture.GetRequiredService<GoalStore>().SaveGoalAsync(new Goal
        {
            Name = "Fictitious household savings goal with a complete long identity " + index,
            CurrencyCode = "EUR", TargetAmount = 5000, AccountId = accounts[index].Id,
            Type = index == 1 ? GoalType.Earmark : index == 2 ? GoalType.HoldingQuantity : GoalType.AccountBalance,
            State = index == 2 ? GoalState.Paused : index == 3 ? GoalState.Completed : index == 4 ? GoalState.Archived : GoalState.Active,
        });
        var budgetOnly = Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") == "active-budget-definitions";
        if (budgetOnly)
        {
            var today = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
            var next = today.AddMonths(1); var past = today.AddMonths(-1);
            var budgets = new[]
            {
                new Budget { CurrencyCode = "EUR", Year = today.Year, Month = today.Month,
                    AccountIds = [accounts[0].Id, accounts[4].Id], Method = BudgetMethod.Envelopes, TotalLimit = 12345 },
                new Budget { CurrencyCode = "eur", Year = next.Year, Month = next.Month,
                    AccountIds = [accounts[4].Id, accounts[0].Id, accounts[4].Id], TotalLimit = 23456 },
                new Budget { CurrencyCode = "USD", Year = today.Year, Month = today.Month, TotalLimit = 34567 },
                new Budget { CurrencyCode = "GBP", Year = past.Year, Month = past.Month, TotalLimit = 45678 },
            };
            await using var db = await factory.CreateDbContextAsync();
            db.Budgets.AddRange(budgets); await db.SaveChangesAsync();
        }
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync();
            await db.Database.OpenConnectionAsync(); var connection = db.Database.GetDbConnection();
            using var command = connection.CreateCommand(); var tables = new List<string>();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using (var reader = await command.ExecuteReaderAsync()) { while (await reader.ReadAsync()) tables.Add(reader.GetString(0)); }
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete choice-review schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
                command.CommandText = "SELECT * FROM \"" + table + "\"";
                using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
                while (await reader.ReadAsync())
                {
                    var values = new object?[reader.FieldCount];
                    for (var index = 0; index < values.Length; index++) values[index] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                    rows.Add(JsonSerializer.Serialize(values));
                }
                rows.Sort(StringComparer.Ordinal); all[table] = rows;
            }
            return JsonSerializer.Serialize(all);
        }
        var before = await StoredAsync();
        var localization = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>();
        var interaction = services.GetRequiredService<IAppInteraction>();
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
            var access = new ActiveItemsReviewAccess { Current = new(path,
                new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("41414141-5252-6363-7474-858585858585")))) };
            var vm = new ActiveItemsViewModel(new(factory, access), translator, interaction, new(factory, access, TimeProvider.System));
            var page = new ActiveItemsPage(vm, localization);
            await Shell.Current.Navigation.PushAsync(page);
            for (var attempt = 0; attempt < 100 && !vm.Loading.IsReady; attempt++) await Task.Delay(50);
            if (!vm.Loading.IsReady) throw new InvalidOperationException("The actual choice page did not publish.");
            await Task.Delay(350);
            if (budgetOnly)
            {
                await ReviewActiveBudgetDefinitionsAsync(app, page, vm, factory, folder, language, StoredAsync, before);
                continue;
            }
            if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") == "active-items-save-feedback")
            {
                await ReviewActiveItemsSaveFeedbackAsync(app, page, vm, factory, folder, language);
                if (await StoredAsync() != before) throw new InvalidOperationException("Failed native Save wrote a complete fixture row.");
                File.WriteAllText(Path.Combine(folder, language + "-active-items-save-feedback-proof.json"), JsonSerializer.Serialize(new
                { ActualNativeRejectedSave = true, ExactDraftRetained = true, Complete25TablesUnchanged = true,
                    FeedbackFullyVisibleBesideSaveOutsideScroll = true }));
                continue;
            }
            if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") == "active-items-discard")
            {
                await ReviewActiveItemsDiscardAsync(app, page, vm, folder, language);
                if (await StoredAsync() != before) throw new InvalidOperationException("Native discard wrote complete fixture rows.");
                File.WriteAllText(Path.Combine(folder, language + "-active-items-discard-proof.json"), JsonSerializer.Serialize(new
                { ActualNativeBackKindAndReloadConsent = true, CancellationPreservesExactChoice = true,
                    ExplicitDiscardLoadsWithoutSave = true, Complete25TablesUnchanged = true, SharedBusyGuard = true }));
                continue;
            }
            var viewport = VisualDescendants(page).OfType<ScrollView>().Single();
            var observations = new List<object>();
            void InvokeReload() => InvokeChoiceReview(VisualDescendants(WrappingActions(page)
                .Single(action => action.Command == vm.ReloadCommand && Visible(action))).OfType<Button>().Single());
            async Task CheckAsync(string scenario)
            {
                // Read actual arranged captions and associated peers; offscreen rows remain part of the growing content.
                foreach (var label in VisualDescendants(page).OfType<Label>().Where(Visible)) CheckCaption(label);
                foreach (var button in VisualDescendants(page).OfType<Button>().Where(Visible))
                {
                    if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.ActualWidth < 44 || native.ActualHeight < 44)
                        throw new InvalidOperationException("A choice action lost its 44-pixel native target.");
                    var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native)
                        as Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer
                        ?? throw new InvalidOperationException("Missing associated native choice peer.");
                    if (button.CommandParameter is ActiveItemRow row && (peer.GetName() != row.AccessibilityText || native.IsEnabled != row.CanToggle))
                        throw new InvalidOperationException("The choice row lost its complete spoken name or original eligibility.");
                    observations.Add(new { scenario, Name = peer.GetName(), native.ActualWidth, native.ActualHeight, native.IsEnabled });
                }
                if (await StoredAsync() != before) throw new InvalidOperationException("Presentation review wrote a choice or financial row.");
                await CaptureAsync(app, folder, language + "-active-items-" + scenario);
            }
            await CheckAsync("accounts");
            foreach (var row in vm.Items.Where(row => row.IsEligible).ToArray())
            {
                var button = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.CommandParameter, row));
                await ScrollToViewIfNeededAsync(viewport, button); InvokeChoiceReview(button); await Task.Delay(150);
            }
            if (!vm.OverCapacity || vm.CanSave || vm.CapacityError is null) throw new InvalidOperationException("Native choices did not expose capacity feedback.");
            await viewport.ScrollToAsync(0, 0, false); await Task.Delay(200); await CheckAsync("capacity");
            foreach (var row in vm.Items.Where(row => row.IsSelected).ToArray())
            {
                var button = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.CommandParameter, row));
                await ScrollToViewIfNeededAsync(viewport, button); InvokeChoiceReview(button); await Task.Delay(100);
            }
            await viewport.ScrollToAsync(0, 0, false); await Task.Delay(150);
            var chips = VisualDescendants(page).OfType<ChoiceChips>().Single();
            InvokeChoiceReview(VisualDescendants(chips).OfType<Button>().ElementAt(1)); await vm.PendingKindChange; await Task.Delay(300);
            if (vm.KindIndex != 1 || vm.Items.Count != 8 || vm.IsDirty) throw new InvalidOperationException("Native Plan choice did not publish its original states.");
            await CheckAsync("plans");
            var paused = vm.Items.Single(row => row.StateText == translator["Plan_Paused"]);
            var pausedButton = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.CommandParameter, paused));
            await ScrollToViewIfNeededAsync(viewport, pausedButton); InvokeChoiceReview(pausedButton); await Task.Delay(200);
            if (!paused.IsSelected || !vm.IsDirty || paused.StateText != translator["Plan_Paused"]) throw new InvalidOperationException("A paused choice incorrectly resumed the plan.");
            await CheckAsync("paused"); InvokeChoiceReview(pausedButton); await Task.Delay(100);
            await viewport.ScrollToAsync(0, 0, false); await Task.Delay(150);
            InvokeChoiceReview(VisualDescendants(chips).OfType<Button>().ElementAt(2)); await vm.PendingKindChange; await Task.Delay(300);
            if (vm.KindIndex != 2 || vm.Items.Count != 5 || vm.IsDirty) throw new InvalidOperationException("Native goal choices did not publish.");
            await CheckAsync("goals");
            var pausedGoal = vm.Items.Single(row => row.StateText == translator["GoalState_Paused"]);
            var completedGoal = vm.Items.Single(row => row.StateText == translator["GoalState_Completed"]);
            if (!pausedGoal.IsEligible || completedGoal.CanToggle || completedGoal.IsEligible
                || pausedGoal.DetailText != translator["GoalType_HoldingQuantity"])
                throw new InvalidOperationException("Original goal state/measurement was replaced by choice or currency.");
            var goalButton = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.CommandParameter, pausedGoal));
            await ScrollToViewIfNeededAsync(viewport, goalButton); InvokeChoiceReview(goalButton); await Task.Delay(150);
            if (!pausedGoal.IsSelected || !vm.CanSave || pausedGoal.StateText != translator["GoalState_Paused"])
                throw new InvalidOperationException("Choosing the paused goal resumed it or lost its one slot.");
            await CheckAsync("paused-goal");
            var secondGoal = vm.Items.First(row => row.IsEligible && row != pausedGoal);
            var secondGoalButton = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.CommandParameter, secondGoal));
            await ScrollToViewIfNeededAsync(viewport, secondGoalButton); InvokeChoiceReview(secondGoalButton); await Task.Delay(150);
            if (!vm.OverCapacity || vm.CanSave || vm.CapacityError is null) throw new InvalidOperationException("Goal allowance feedback was not visible.");
            await viewport.ScrollToAsync(0, 0, false); await Task.Delay(150); await CheckAsync("goal-capacity");
            await ScrollToViewIfNeededAsync(viewport, secondGoalButton); InvokeChoiceReview(secondGoalButton);
            await ScrollToViewIfNeededAsync(viewport, goalButton); InvokeChoiceReview(goalButton); await Task.Delay(150);
            access.Current = CommercialWriteAccess.Inactive;
            InvokeReload(); if (vm.ReloadCommand.ExecutionTask is { } reload) await reload;
            await viewport.ScrollToAsync(0, 0, false); await Task.Delay(250);
            if (vm.CanChoose || vm.CanSave || vm.Items.Count(row => row.IsSelected) != 3) throw new InvalidOperationException("Inactive release unexpectedly required a subset.");
            await CheckAsync("unlimited");
            access.Fail = true; InvokeReload(); if (vm.ReloadCommand.ExecutionTask is { } failed) await failed;
            await Task.Delay(250); if (!vm.Loading.HasFailed || vm.CanInteract) throw new InvalidOperationException("Failed publication uncovered the form.");
            await CheckAsync("failure"); access.Fail = false;
            var retry = WrappingActions(page).Single(action => action.Command == vm.ReloadCommand && Visible(action));
            InvokeChoiceReview(VisualDescendants(retry).OfType<Button>().Single());
            if (vm.ReloadCommand.ExecutionTask is { } retried) await retried;
            await Task.Delay(250); if (!vm.Loading.IsReady) throw new InvalidOperationException("Native retry did not publish the form.");
            await CheckAsync("retry");
            InvokeSnapshotBack(page); await Task.Delay(350);
            if (ReferenceEquals(Shell.Current.CurrentPage, page)) throw new InvalidOperationException("Native Back did not leave the choice page.");
            File.WriteAllText(Path.Combine(folder, language + "-active-items-proof.json"), JsonSerializer.Serialize(new
            { CompleteStoredTables = 25, CompleteRowsUnchanged = await StoredAsync() == before, ActualNativeRowsAndKindInvoked = true,
                PausedStatePreserved = true, ActualNativeGoalsAndOneSlotCapacity = true, CompletedGoalsUnselectable = true, QuantityGoalDoesNotDisplayMoneyCurrency = true, InactiveReleaseDoesNotLimit = true, CoveredFailureAndNativeRetry = true,
                NativeBack = true, NoSaveInvoked = true, FictitiousFreeOnly = true, observations }));
        }
    }

    // Actual rejected choice Save must explain its failure beside the pinned action, even at the list's far end.
    private static async Task ReviewActiveItemsSaveFeedbackAsync(App app, ActiveItemsPage page, ActiveItemsViewModel vm,
        IDbContextFactory<ZananceDbContext> factory, string folder, string language)
    {
        var row = vm.Items.First(item => item.IsEligible);
        var viewport = VisualDescendants(page).OfType<ScrollView>().Single();
        var rowButton = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.CommandParameter, row));
        await ScrollToViewIfNeededAsync(viewport, rowButton); InvokeChoiceReview(rowButton); await Task.Delay(150);
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FictitiousChoiceSaveFailure BEFORE INSERT ON ResourceSelections BEGIN SELECT RAISE(ABORT, 'Fictitious choice failure'); END;");
        try
        {
            await viewport.ScrollToAsync(0, viewport.ContentSize.Height, false); await Task.Delay(200);
            InvokeWrappingAction(page, vm.SaveCommand); if (vm.SaveCommand.ExecutionTask is { } saving) await saving;
            await Task.Delay(250);
            if (!row.IsSelected || !vm.IsDirty || vm.Feedback != Translator.Instance["ActiveItems_SaveFailed"] || !vm.CanSave)
                throw new InvalidOperationException("The actual rejected Save lost its draft or translated retry feedback.");
            var error = VisualDescendants(page).OfType<Label>().Single(label => label.Text == vm.Feedback && Visible(label));
            CheckCaption(error);
            if (error.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native
                || page.Content.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement root)
                throw new InvalidOperationException("Missing actual feedback/root allocation.");
            var rect = native.TransformToVisual(root).TransformBounds(new Windows.Foundation.Rect(0, 0, native.ActualWidth, native.ActualHeight));
            var inViewport = false;
            for (Element? ancestor = error.Parent; ancestor is not null; ancestor = ancestor.Parent)
                if (ReferenceEquals(ancestor, viewport)) inViewport = true;
            await CaptureAsync(app, folder, language + "-active-items-save-feedback");
            File.WriteAllText(Path.Combine(folder, language + "-active-items-save-feedback-geometry.json"), JsonSerializer.Serialize(new
            { error.Text, rect, root.ActualWidth, root.ActualHeight, InsideScrollableList = inViewport, viewport.ScrollY }));
            if (inViewport || rect.Top < -1 || rect.Bottom > root.ActualHeight + 1 || rect.Left < -1 || rect.Right > root.ActualWidth + 1)
                throw new InvalidOperationException("The rejected Save feedback can be hidden outside the current viewport/action row.");
        }
        finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS FictitiousChoiceSaveFailure;"); }
        await ScrollToViewIfNeededAsync(viewport, rowButton); InvokeChoiceReview(rowButton); await Task.Delay(100);
        InvokeSnapshotBack(page); await Task.Delay(250);
        if (ReferenceEquals(Shell.Current.CurrentPage, page)) throw new InvalidOperationException("Feedback review did not return without a financial Save.");
    }

    // Resolve dirty actual native Back/kind/reload dialogs without saving the fictitious selection.
    private static async Task ReviewActiveItemsDiscardAsync(App app, ActiveItemsPage page, ActiveItemsViewModel vm, string folder, string language)
    {
        async Task AnswerAsync(bool discard, string scenario)
        {
            if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
                throw new InvalidOperationException("Missing own native discard dialog root.");
            Microsoft.UI.Xaml.Controls.ContentDialog? dialog = null;
            for (var attempt = 0; attempt < 100 && dialog is null; attempt++)
            {
                dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root)
                    .Select(popup => popup.Child).OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().SingleOrDefault();
                if (dialog is null) await Task.Delay(50);
            }
            if (dialog is null || !vm.IsBusy || vm.CanLeave || vm.CanInteract)
                throw new InvalidOperationException("Dirty native transition did not retain its full consent guard.");
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, language + "-active-items-" + scenario + ".png"));
            var answer = FindNativeButton(dialog, Translator.Instance[discard ? "ActiveItems_Discard" : "Common_Cancel"])
                ?? throw new InvalidOperationException("Missing native discard answer.");
            var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(answer)
                as Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer;
            if (peer?.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
                is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
                throw new InvalidOperationException("Missing associated native discard answer pattern.");
            invoke.Invoke(); await Task.Delay(250);
        }
        var row = vm.Items.First(item => item.IsEligible);
        var rowButton = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.CommandParameter, row));
        await ScrollToViewIfNeededAsync(VisualDescendants(page).OfType<ScrollView>().Single(), rowButton);
        InvokeChoiceReview(rowButton); await Task.Delay(150);
        InvokeSnapshotBack(page); await AnswerAsync(false, "back-cancel");
        if (vm.LeaveCommand.ExecutionTask is { } leaving) await leaving;
        if (!ReferenceEquals(Shell.Current.CurrentPage, page) || !row.IsSelected || !vm.IsDirty || vm.IsBusy)
            throw new InvalidOperationException("Native Back cancellation lost the actual unsaved choice.");
        var chips = VisualDescendants(page).OfType<ChoiceChips>().Single();
        await ScrollToViewIfNeededAsync(VisualDescendants(page).OfType<ScrollView>().Single(), chips);
        InvokeChoiceReview(VisualDescendants(chips).OfType<Button>().ElementAt(2)); await AnswerAsync(false, "kind-cancel"); await vm.PendingKindChange;
        if (vm.KindIndex != 0 || !ReferenceEquals(vm.Items.First(item => item.Id == row.Id), row) || !row.IsSelected || !vm.IsDirty)
            throw new InvalidOperationException("Native kind cancellation lost the original identity or draft.");
        void Reload() => InvokeChoiceReview(VisualDescendants(WrappingActions(page)
            .Single(action => action.Command == vm.ReloadCommand && Visible(action))).OfType<Button>().Single());
        Reload(); await AnswerAsync(false, "reload-cancel"); if (vm.ReloadCommand.ExecutionTask is { } cancelled) await cancelled;
        if (!row.IsSelected || !vm.IsDirty) throw new InvalidOperationException("Native reload cancellation discarded a choice.");
        Reload(); await AnswerAsync(true, "reload-discard"); if (vm.ReloadCommand.ExecutionTask is { } discarded) await discarded;
        if (vm.IsDirty || vm.Items.Any(item => item.IsSelected)) throw new InvalidOperationException("Explicit native reload retained the rejected draft.");
        row = vm.Items.First(item => item.IsEligible);
        rowButton = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.CommandParameter, row));
        await ScrollToViewIfNeededAsync(VisualDescendants(page).OfType<ScrollView>().Single(), rowButton);
        InvokeChoiceReview(rowButton); await Task.Delay(150); InvokeSnapshotBack(page); await AnswerAsync(true, "back-discard");
        if (vm.LeaveCommand.ExecutionTask is { } accepted) await accepted;
        if (ReferenceEquals(Shell.Current.CurrentPage, page) || vm.IsBusy) throw new InvalidOperationException("Explicit native discard did not leave through the guarded command.");
    }

    // The normal registered Settings action opens the normal inactive form, without replacing its runtime source.
    private static async Task ReviewActiveItemsEntryAsync(App app, IServiceProvider services, string folder, string language)
    {
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            var names = new List<string>();
            using (var reader = await command.ExecuteReaderAsync()) { while (await reader.ReadAsync()) names.Add(reader.GetString(0)); }
            var tables = new SortedDictionary<string, List<string>>();
            foreach (var name in names)
            {
                command.CommandText = "SELECT * FROM \"" + name + "\""; using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
                while (await reader.ReadAsync())
                {
                    var values = new object?[reader.FieldCount];
                    for (var index = 0; index < values.Length; index++) values[index] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                    rows.Add(JsonSerializer.Serialize(values));
                }
                rows.Sort(StringComparer.Ordinal); tables[name] = rows;
            }
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete registered-form review schema.");
            return JsonSerializer.Serialize(tables);
        }
        var before = await StoredAsync();
        await Shell.Current.GoToAsync(AppShell.SettingsRoute);
        var settings = await AwaitActiveItemsReviewPageAsync<Features.Settings.SettingsPage>();
        var settingsVm = (Features.Settings.SettingsViewModel)settings.BindingContext;
        for (var attempt = 0; attempt < 100 && !settingsVm.Loading.IsReady; attempt++) await Task.Delay(50);
        if (!settingsVm.Loading.IsReady) throw new InvalidOperationException("Registered Settings did not finish loading.");
        var action = WrappingActions(settings).Single(item => item.Command == settingsVm.OpenActiveItemsCommand);
        await ScrollToViewIfNeededAsync(FindScrollView(settings) ?? throw new InvalidOperationException("Missing actual Settings viewport."), action); await Task.Delay(250);
        foreach (var label in VisualDescendants(action).OfType<Label>().Where(Visible)) CheckCaption(label);
        var button = VisualDescendants(action).OfType<Button>().Single();
        if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.ActualWidth < 44 || native.ActualHeight < 44
            || new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native).GetName() != Translator.Instance["ActiveItems_Title"])
            throw new InvalidOperationException("The Settings entry lacks its complete native caption/name/target.");
        await CaptureAsync(app, folder, language + "-active-items-settings-entry"); InvokeChoiceReview(button);
        if (settingsVm.OpenActiveItemsCommand.ExecutionTask is { } opening) await opening;
        var page = await AwaitActiveItemsReviewPageAsync<ActiveItemsPage>(); var vm = (ActiveItemsViewModel)page.BindingContext;
        for (var attempt = 0; attempt < 100 && !vm.Loading.IsReady; attempt++) await Task.Delay(50);
        if (!vm.Loading.IsReady || vm.CanChoose || vm.CanSave) throw new InvalidOperationException("The normal registered form activated a choice limit.");
        await Task.Delay(200); await CaptureAsync(app, folder, language + "-active-items-registered");
        InvokeChoiceReview(VisualDescendants(VisualDescendants(page).OfType<HelpButton>().Single()).OfType<Button>().Single());
        await Task.Delay(700);
        await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, language + "-help-ActiveItems.png"));
        if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
            throw new InvalidOperationException("Missing actual choice help root.");
        var dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root)
            .Select(popup => popup.Child).OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().Single();
        var close = FindNativeButton(dialog, Translator.Instance["Common_Ok"])
            ?? throw new InvalidOperationException("Missing actual choice help dismissal.");
        var closePeer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(close)
            as Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer;
        if (closePeer?.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
            is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider dismiss)
            throw new InvalidOperationException("Missing native choice help dismissal pattern.");
        dismiss.Invoke(); await Task.Delay(250);
        var kinds = VisualDescendants(page).OfType<ChoiceChips>().Single();
        await ScrollToViewIfNeededAsync(FindScrollView(page) ?? throw new InvalidOperationException("Missing registered form viewport."), kinds);
        InvokeChoiceReview(VisualDescendants(kinds).OfType<Button>().ElementAt(2)); await vm.PendingKindChange; await Task.Delay(250);
        if (vm.KindIndex != 2 || vm.CanChoose || vm.CanSave) throw new InvalidOperationException("Registered Goals activated a required subset.");
        foreach (var label in VisualDescendants(page).OfType<Label>().Where(Visible)) CheckCaption(label);
        await CaptureAsync(app, folder, language + "-active-items-registered-goals");
        InvokeSnapshotBack(page); await Task.Delay(300);
        if (!ReferenceEquals(Shell.Current.CurrentPage, settings)) throw new InvalidOperationException("Registered Back replaced the retained Settings page.");
        InvokeSnapshotBack(settings); await Task.Delay(300);
        if (await StoredAsync() != before) throw new InvalidOperationException("Registered choice navigation wrote complete financial/settings rows.");
        File.WriteAllText(Path.Combine(folder, language + "-active-items-entry-proof.json"), JsonSerializer.Serialize(new
        { ActualRegisteredSettingsAction = true, CompleteNativeEntryCaptionNameAndTarget = true, NativeHelpAndBack = true,
            ActualRegisteredGoalKind = true, InactiveCommercialSource = true, Complete25TablesUnchanged = true, RetainedSettingsPage = true }));
    }

    private static async Task<T> AwaitActiveItemsReviewPageAsync<T>() where T : ContentPage
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (Shell.Current.CurrentPage is T page) return page;
            await Task.Delay(50);
        }
        throw new InvalidOperationException("The actual registered choice-review route did not arrive: " + typeof(T).Name);
    }

    private static bool Visible(Element element)
    {
        for (Element? current = element; current is not null; current = current.Parent)
            if (current is VisualElement { IsVisible: false }) return false;
        return true;
    }

    private static void CheckCaption(Label label)
    {
        if (string.IsNullOrEmpty(label.Text)) return;
        if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native || native.IsTextTrimmed
            || !native.IsTextScaleFactorEnabled || native.ActualWidth < 1 || native.ActualHeight < 1)
            throw new InvalidOperationException("Missing complete native scaled choice caption: " + label.Text);
        var glyphs = new List<Windows.Foundation.Rect>();
        for (var offset = 0; offset <= native.ContentEnd.Offset - native.ContentStart.Offset; offset++)
            if (native.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward) is { } pointer)
                glyphs.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward));
        if (glyphs.Count == 0 || glyphs.Min(rect => rect.Left) < -1 || glyphs.Max(rect => rect.Right) > label.Width + 1
            || glyphs.Max(rect => rect.Bottom) > label.Height + 1)
            throw new InvalidOperationException("Choice-caption glyphs exceed their actual growing allocation: " + label.Text);
    }

    private static void InvokeChoiceReview(Button button)
    {
        if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native) throw new InvalidOperationException("Missing native choice button.");
        var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native)
            as Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer;
        if (peer?.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
            throw new InvalidOperationException("Missing real native choice Invoke pattern.");
        invoke.Invoke();
    }
}
#endif
