#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Profiles;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Records native-effect requests for an independent fixture; no desktop notification or permission is touched.</summary>
    private sealed class ProfileReminderReviewScheduler : IReminderScheduler
    {
        public event EventHandler<string>? Tapped { add { } remove { } }
        public event EventHandler<SnoozeRequest>? SnoozeRequested { add { } remove { } }
        internal IReadOnlyList<ReminderNotification> Pending = [];
        internal Action? AfterLabels;
        internal Func<Task>? AfterPublication;
        public bool IsSupported => true;
        public Task<bool> AreEnabledAsync() => Task.FromResult(true);
        public Task<bool> RequestPermissionAsync() => throw new InvalidOperationException("Unexpected fixture permission request.");
        public void SetSnoozeActions(string oneHour, string tomorrow) => AfterLabels?.Invoke();
        public async Task ReplaceAllAsync(IReadOnlyList<ReminderNotification> reminders)
        {
            Pending = reminders.ToArray();
            if (AfterPublication is { } publication) await publication();
        }
        public Task ShowAsync(ReminderNotification reminder) => throw new InvalidOperationException("Unexpected immediate fixture notification.");
        public Task RemoveAsync(int notificationId) => Task.CompletedTask;
    }

    // D-158 / AT-160: actual native Profiles Save invokes the real catalogue watcher and complete queue guards.
    private static async Task ReviewProfileReminderWorkAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "zanance.db"); var preferences = new InMemorySettingsStore();
        var financial = new ActiveItemsReviewAccess { Current = CommercialWriteAccess.Inactive };
        var device = new ProfileReviewAccess(); var translator = services.GetRequiredService<Translator>();
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ISettingsStore>(preferences).AddSingleton(translator).AddSingleton<IProfileRuntime, ProfileReviewRuntime>()
            .AddSingleton<ICommercialWriteAccessSource>(financial).AddSingleton<IProfileWorkAccessSource>(device)
            .AddZananceData(path).AddSingleton<ProfileService>()
            .DecorateZananceCommercialAccess((_, source) => new ProfileCommercialAccessSource(source, preferences, device, path)).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
        var plans = fixture.GetRequiredService<PlanStore>(); var profiles = fixture.GetRequiredService<ProfileService>();
        var later = profiles.Create("Fictitious other original profile"); var day = DateOnly.FromDateTime(DateTime.Now);
        var account = new Account { Name = "Fictitious original reminder funding", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 10000 };
        await store.SaveAccountAsync(account);
        var originalPlan = new Schedule { Name = "Fictitious original due plan", AccountId = account.Id, Amount = 1234,
            ReminderEnabled = true, ReminderDaysBefore = 0, Rule = new() { Start = day.AddDays(1) } };
        await plans.SaveScheduleAsync(originalPlan);
        var inputReview = Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") == "reminder-input-work";
        var goals = fixture.GetRequiredService<GoalStore>();
        var originalGoal = new Vafadar.Zanance.Core.Goals.Goal { Name = "Fictitious original balance goal", AccountId = account.Id,
            Type = Vafadar.Zanance.Core.Goals.GoalType.AccountBalance, CurrencyCode = "EUR", TargetAmount = 20000 };
        if (inputReview)
            await goals.SaveGoalWithContributionPlanAsync(originalGoal, new Vafadar.Zanance.Core.Goals.ContributionPlan
            { GoalId = originalGoal.Id, Amount = 1000, ReminderEnabled = true, Rule = new() { Start = day.AddDays(1) } });
        await store.UpdateSettingsAsync(s => { s.ReviewReminderEnabled = true; s.MonthStartDay = day.Day < 28 ? day.Day + 1 : 1; });
        financial.Current = new(path, new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("45454545-5656-6767-7878-898989898989"))));
        device.Current = new(path, new(QuotaScopeKind.Device, Guid.Parse("75757575-8686-9797-a8a8-b9b9b9b9b9b9")),
            new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("65656565-7676-8787-9898-a9a9a9a9a9a9"))));
        profiles.SaveWorkChoice(profiles.ReadWorkReview(), [""]); var originalPacket = preferences.Get("profiles.work.catalogue");
        if (inputReview)
        {
            financial.Current = new(path, new(ProductPlan.Plus, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("45454545-5656-6767-7878-898989898989"))));
            device.Current = new(path, new(QuotaScopeKind.Device, Guid.Parse("75757575-8686-9797-a8a8-b9b9b9b9b9b9")),
                new(ProductPlan.Plus, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("65656565-7676-8787-9898-a9a9a9a9a9a9"))));
        }
        var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand(); var tables = new List<string>();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete profile-reminder fixture schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
                command.CommandText = "SELECT * FROM \"" + table + "\""; using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
                while (await reader.ReadAsync()) { var values = new object?[reader.FieldCount]; for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i); rows.Add(JsonSerializer.Serialize(values)); }
                rows.Sort(StringComparer.Ordinal); all[table] = rows;
            }
            return JsonSerializer.Serialize(all);
        }
        var before = await StoredAsync(); var scheduler = new ProfileReminderReviewScheduler();
        var localization = services.GetRequiredService<ILocalizationService>();
        await using var reminders = new ReminderService(scheduler, store, plans, fixture.GetRequiredService<GoalStore>(), fixture.GetRequiredService<HoldingStore>(),
            preferences, translator, new DateFormatter(localization), localization, TimeProvider.System,
            fixture.GetRequiredService<ResourceChoiceStore>(), fixture.GetRequiredService<BudgetChoiceStore>());
        reminders.WatchChanges(profiles);
        if (inputReview)
        {
            await ReviewReminderInputsAsync(app, services, fixture, store, plans, goals, factory, reminders, scheduler,
                account, originalPlan, originalGoal, StoredAsync, before, folder, languages);
            return;
        }
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language); await reminders.RefreshAsync();
            if (scheduler.Pending.Count != 1 || scheduler.Pending.Any(n => n.Link == "review"))
                throw new InvalidOperationException("Original Free plan/reminder policy changed.");
            var vm = new ProfilesViewModel(profiles, translator, services.GetRequiredService<Interaction.IAppInteraction>(), services.GetRequiredService<Interaction.IAppFlowHost>());
            var page = new ProfilesPage(vm, localization); ProfileNativeReads.Add(page);
            await Shell.Current.Navigation.PushAsync(page); await vm.EnsureLoadedAsync(); await Task.Delay(200);
            async Task ChooseAsync(string id, int pending, string scenario)
            {
                foreach (var row in vm.Profiles.Where(row => row.IsSelected && row.Profile.Id != id).ToArray())
                {
                    var target = VisualDescendants(page).OfType<Button>().Single(b => b.Command == vm.ToggleCommand && ReferenceEquals(b.CommandParameter, row));
                    InvokeChoiceReview(target, ProfileNativeReads); await Task.Delay(100);
                }
                var chosen = vm.Profiles.Single(row => row.Profile.Id == id);
                if (!chosen.IsSelected)
                {
                    var target = VisualDescendants(page).OfType<Button>().Single(b => b.Command == vm.ToggleCommand && ReferenceEquals(b.CommandParameter, chosen));
                    InvokeChoiceReview(target, ProfileNativeReads); await Task.Delay(100);
                }
                var save = VisualDescendants(WrappingActions(page).Single(action => action.Command == vm.SaveChoiceCommand)).OfType<Button>().Single();
                InvokeChoiceReview(save, ProfileNativeReads); if (vm.SaveChoiceCommand.ExecutionTask is { } saving) await saving;
                await reminders.PendingRefresh.WaitAsync(TimeSpan.FromSeconds(10));
                if (scheduler.Pending.Count != pending || vm.IsDirty || !vm.CanInteract || !profiles.Current.IsMain || await StoredAsync() != before)
                    throw new InvalidOperationException("Actual native choice did not rebuild the original queue without financial writes.");
                foreach (var label in VisualDescendants(page).OfType<Label>().Where(Visible)) CheckCaption(label, ProfileNativeReads);
                await CaptureAsync(app, folder, language + "-profile-reminder-work-" + scenario);
            }
            await ChooseAsync(later.Id, 0, "withdrawn"); await ChooseAsync("", 1, "restored");
            if (scheduler.Pending.Single().Body != translator["Reminder_Generic"])
                throw new InvalidOperationException("The original restored queue lost generic live-language privacy.");
            InvokeSnapshotBack(page); await Task.Delay(150);
            var originalLanguage = localization.CurrentLanguage;
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnRebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += OnRebuilt;
            try
            {
                scheduler.AfterLabels = () => localization.SetLanguage(localization.SupportedLanguages.First(item => item.CultureName == (language == "de" ? "en" : "de")));
                await reminders.RefreshAsync();
                if (scheduler.Pending.Count != 0 || await StoredAsync() != before)
                    throw new InvalidOperationException("Live native display retirement left mixed-language work or changed financial rows.");
                await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(10));
                await WaitForReopenedLayoutAsync(Shell.Current.CurrentPage);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, language + "-profile-reminder-work-retired-display-window.png"));
            }
            finally { app.ShellRebuilt -= OnRebuilt; scheduler.AfterLabels = null; }
            await SetLanguageAsync(app, localization, originalLanguage.CultureName);
            await reminders.RefreshAsync();
            if (scheduler.Pending.Count != 1) throw new InvalidOperationException("Read-only display retry lost valid original work.");
            preferences.Set("profiles.work.catalogue", originalPacket);
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-profile-reminder-work-proof.json"), JsonSerializer.Serialize(new
            { ActualNativeChoiceSaveAndWatcher = true, Complete25TablesUnchanged = true, OriginalCatalogueRestored = true,
                OriginalPlanQueueWithdrawnAndRestored = true, SeparateFreeReviewPolicy = true, GenericLiveLanguageBody = true, LiveNativeDisplayRetirementAndRetry = true,
                NoFinancialSaveOrDesktopNotification = true, ProductionSourcesInactive = true }));
        }
    }
}
#endif
