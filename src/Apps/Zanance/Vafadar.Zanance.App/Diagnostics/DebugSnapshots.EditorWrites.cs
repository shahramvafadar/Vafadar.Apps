#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Only original modal return/consent failures are fictitious; Save always uses the actual SQLite writer.</summary>
    private sealed class EditorWriteReviewInteraction : IAppInteraction
    {
        internal bool RefuseReturn;
        public Task AlertAsync(string title, string message, string cancel) => throw new InvalidOperationException("No native alert belongs to this owned editor review.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Task.FromResult(true);
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new InvalidOperationException("No native prompt belongs to this review.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new InvalidOperationException("No menu belongs to this review.");
        public async Task NavigateAsync(string route, IDictionary<string, object>? parameters = null)
        {
            if (route != "..") throw new InvalidOperationException("Unexpected owned editor destination.");
            if (RefuseReturn) throw new InvalidOperationException("Fictitious original modal return refusal.");
            await Shell.Current.Navigation.PopModalAsync();
        }
        public Task ShowFailureAsync(Exception exception) => Task.CompletedTask;
    }

    // D-163 / AT-165: original editor sources, actual native Save/Close and actual atomic writes in an independent file.
    private static async Task ReviewEditorWritesAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "editor-writes.db"); var access = new ActiveItemsReviewAccess();
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
        var plans = fixture.GetRequiredService<PlanStore>(); var day = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
        var account = new Account { Name = "Fictitious original funding", CurrencyCode = "EUR", OpeningDate = day };
        await store.SaveAccountAsync(account);
        await store.SaveAccountAsync(new() { Name = "Fictitious original second", CurrencyCode = "EUR", OpeningDate = day });
        await store.SaveAccountAsync(new() { Name = "Fictitious original third", CurrencyCode = "EUR", OpeningDate = day });
        for (var i = 0; i < 5; i++) await plans.SaveScheduleAsync(new() { Name = "Fictitious original plan " + i, AccountId = account.Id, Amount = 1234,
            Rule = new() { Frequency = Frequency.Once, Start = day } });
        await ApplyOwnedReviewModeAsync(store);
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
        var scope = new EntitlementScope(EntitlementScopeKind.PersonalProfile, Guid.Parse("45454545-8989-0101-1212-343434343434"));
        await using var reminders = new ReminderService(new NoReminderScheduler(), store, plans, fixture.GetRequiredService<GoalStore>(),
            fixture.GetRequiredService<HoldingStore>(), new Vafadar.Core.Settings.InMemorySettingsStore(), translator,
            new DateFormatter(localization), localization, TimeProvider.System);
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
            foreach (var isPlan in new[] { false, true })
            {
                access.Current = new(path, new(ProductPlan.Plus, scope));
                var effects = new EditorWriteReviewInteraction(); ContentPage page;
                AccountEditorViewModel? accountEditor = null; PlanEditorViewModel? planEditor = null;
                if (isPlan)
                {
                    planEditor = new(store, plans, translator, localization, new DateFormatter(localization), TimeProvider.System, reminders, effects);
                    await planEditor.ApplyQueryAsync(new Dictionary<string, object>()); planEditor.Name = "Fictitious complete raw new plan";
                    planEditor.Note = "Fictitious complete retained note"; planEditor.AmountText = MoneyText.ForInput(45678, "EUR", localization.CurrentCulture);
                    page = new PlanEditorPage(planEditor);
                }
                else
                {
                    accountEditor = new(store, plans, translator, localization, TimeProvider.System, effects);
                    await accountEditor.ApplyQueryAsync(new Dictionary<string, object>()); accountEditor.Form.Name = "Fictitious complete raw new account";
                    accountEditor.Form.OpeningText = MoneyText.ForInput(45678, "EUR", localization.CurrentCulture); page = new AccountEditorPage(accountEditor);
                }
                ProfileNativeReads.Add(page); await Shell.Current.Navigation.PushModalAsync(page); await Task.Delay(250);
                var saveCommand = isPlan ? planEditor!.SaveCommand : accountEditor!.SaveCommand;
                var save = VisualDescendants(page).OfType<Button>().Single(button => button.Command == saveCommand);
                if (VisualDescendants(page).OfType<Entry>().First().Handler?.PlatformView
                    is not Microsoft.UI.Xaml.Controls.TextBox { IsEnabled: true })
                    throw new InvalidOperationException("A loaded original draft lost its enabled native input.");
                var raw = isPlan ? JsonSerializer.Serialize(new { planEditor!.Name, planEditor.Note, planEditor.AmountText, planEditor.Start, planEditor.Account }) : accountEditor!.Form.Snapshot();
                access.Current = new(path, new(ProductPlan.Free, scope));
                InvokeChoiceReview(save, ProfileNativeReads); if (saveCommand.ExecutionTask is { } writing) await writing;
                var reason = isPlan ? planEditor!.SaveError : accountEditor!.SaveError;
                if (reason is null || !reason.Contains(translator["Commercial_DraftKept"], StringComparison.Ordinal) || await StoredAsync() != before)
                    throw new InvalidOperationException("The actual quota writer lost its precise retained-draft rejection.");
                await Task.Delay(150);
                var caption = VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == reason);
                CheckCaption(caption, ProfileNativeReads);
                if (caption.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native || native.ActualHeight < 1)
                    throw new InvalidOperationException("The pinned original rejection is not realized.");
                var nextRaw = isPlan ? JsonSerializer.Serialize(new { planEditor!.Name, planEditor.Note, planEditor.AmountText, planEditor.Start, planEditor.Account }) : accountEditor!.Form.Snapshot();
                if (raw != nextRaw) throw new InvalidOperationException("Native rejection discarded original input.");
                await CaptureAsync(app, folder, language + "-editor-write-" + (isPlan ? "plan" : "account") + "-rejected");
                access.Current = new(path, new(ProductPlan.Plus, scope)); effects.RefuseReturn = true;
                if (isPlan) await planEditor!.ReviewWorkCommand.ExecuteAsync(null); else await accountEditor!.ReviewWorkCommand.ExecuteAsync(null);
                InvokeChoiceReview(save, ProfileNativeReads); if (saveCommand.ExecutionTask is { } accepted) await accepted;
                reason = isPlan ? planEditor!.SaveError : accountEditor!.SaveError;
                if (reason != translator["Editor_SavedReturnFailed"] || (isPlan ? planEditor!.CanSave : accountEditor!.CanSave)
                    || save.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: false })
                    throw new InvalidOperationException("A committed original write remained resubmittable after return failure.");
                await Task.Delay(150);
                caption = VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == reason); CheckCaption(caption, ProfileNativeReads);
                var input = VisualDescendants(page).OfType<Entry>().First();
                if (input.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox { IsEnabled: false })
                    throw new InvalidOperationException("A committed form still accepts new native input.");
                var closeCommand = isPlan ? planEditor!.CancelCommand : accountEditor!.CancelCommand;
                var close = VisualDescendants(page).OfType<Button>().Single(button => button.Command == closeCommand);
                if (close.Text != translator["Common_Close"]) throw new InvalidOperationException("The original committed action still says Cancel.");
                var stored = await StoredAsync(); await saveCommand.ExecuteAsync(null);
                if (stored != await StoredAsync()) throw new InvalidOperationException("A committed original command repeated its write.");
                await CaptureAsync(app, folder, language + "-editor-write-" + (isPlan ? "plan" : "account") + "-saved-return");
                effects.RefuseReturn = false; InvokeChoiceReview(close, ProfileNativeReads); if (closeCommand.ExecutionTask is { } leaving) await leaving;
                // Remove only this independently owned successful test row; every original fixture field remains exact.
                await using (var db = await factory.CreateDbContextAsync())
                {
                    if (isPlan) await db.Schedules.Where(row => row.Name == "Fictitious complete raw new plan").ExecuteDeleteAsync();
                    else await db.Accounts.Where(row => row.Name == "Fictitious complete raw new account").ExecuteDeleteAsync();
                }
                if (await StoredAsync() != before) throw new InvalidOperationException("The owned editor fixture did not restore every original row.");
            }
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-editor-writes-proof.json"), JsonSerializer.Serialize(new
            { ActualOriginalAccountAndPlanPages = true, ActualNativeSaveAndClose = true, ActualAtomicQuotaAndAcceptedWriters = true,
                CompleteRawDraftsRetained = true, PinnedCompleteReason = true, CommittedInputAndSaveDisabled = true,
                ExplicitFictitiousReturnAndConsentPorts = true, Complete25OriginalTablesRestored = true, NoOriginalOwnerFinancialWrite = true }));
        }
    }
}
#endif
