#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Plans;
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
    /// <summary>Independent verified-input/failure fixture only; never a production grant.</summary>
    private sealed class EditorReadinessAccess : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal bool Refuse;
        public CommercialWriteAccess Capture(string path) => Refuse ? throw new InvalidOperationException("Fictitious original availability refusal.") : Current;
    }

    // D-165 / AT-167: actual original editors and native review/Close with unchanged complete independent data.
    private static async Task ReviewEditorReadinessAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "editor-readiness.db"); var access = new EditorReadinessAccess();
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>(); var plans = fixture.GetRequiredService<PlanStore>();
        var day = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
        var funding = new Account { Name = "Fictitious original funding", CurrencyCode = "EUR", OpeningDate = day };
        await store.SaveAccountAsync(funding);
        await store.SaveAccountAsync(new() { Name = "Fictitious original second", CurrencyCode = "EUR", OpeningDate = day });
        await store.SaveAccountAsync(new() { Name = "Fictitious original third", CurrencyCode = "EUR", OpeningDate = day });
        for (var i = 0; i < 5; i++) await plans.SaveScheduleAsync(new() { Name = "Fictitious original plan " + i, AccountId = funding.Id,
            Amount = 1234, Rule = new() { Frequency = Frequency.Once, Start = day } });
        await ApplyOwnedReviewModeAsync(store); var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
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
        var before = await StoredAsync(); var translator = services.GetRequiredService<Translator>(); var localization = services.GetRequiredService<ILocalizationService>();
        var scope = new EntitlementScope(EntitlementScopeKind.PersonalProfile, Guid.Parse("78787878-3434-4545-5656-676767676767"));
        await using var reminders = new ReminderService(new NoReminderScheduler(), store, plans, fixture.GetRequiredService<GoalStore>(),
            fixture.GetRequiredService<HoldingStore>(), new Vafadar.Core.Settings.InMemorySettingsStore(), translator,
            new DateFormatter(localization), localization, TimeProvider.System);
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
            foreach (var isPlan in new[] { false, true })
            {
                access.Current = new(path, new(ProductPlan.Free, scope)); var effects = new EditorWriteReviewInteraction();
                AccountEditorViewModel? account = null; PlanEditorViewModel? plan = null; ContentPage page;
                if (isPlan)
                {
                    plan = new(store, plans, translator, localization, new DateFormatter(localization), TimeProvider.System, reminders, effects);
                    await plan.ApplyQueryAsync(new Dictionary<string, object>()); plan.Name = "Fictitious complete unsaved plan"; plan.Note = "Fictitious complete retained note";
                    plan.AmountText = MoneyText.ForInput(45678, "EUR", localization.CurrentCulture); page = new PlanEditorPage(plan);
                }
                else
                {
                    account = new(store, plans, translator, localization, TimeProvider.System, effects); await account.ApplyQueryAsync(new Dictionary<string, object>());
                    account.Form.Name = "Fictitious complete unsaved account"; account.Form.OpeningText = MoneyText.ForInput(45678, "EUR", localization.CurrentCulture);
                    account.IsDefault = true; page = new AccountEditorPage(account);
                }
                ProfileNativeReads.Add(page); await Shell.Current.Navigation.PushModalAsync(page); await Task.Delay(200);
                var saveCommand = isPlan ? plan!.SaveCommand : account!.SaveCommand;
                var reviewCommand = isPlan ? plan!.ReviewWorkCommand : account!.ReviewWorkCommand;
                var cancelCommand = isPlan ? plan!.CancelCommand : account!.CancelCommand;
                var raw = isPlan ? JsonSerializer.Serialize(new { plan!.Name, plan.Note, plan.AmountText, plan.Start, plan.Account, plan.ReminderDaysText, plan.ReminderTime }) : account!.Form.Snapshot() + "|" + account.IsDefault;
                async Task CheckAsync(bool allowed, string name)
                {
                    await Task.Delay(120);
                    if (isPlan ? plan!.CanSave != allowed : account!.CanSave != allowed) throw new InvalidOperationException("Original readiness lost its actual Save state.");
                    var save = VisualDescendants(page).OfType<Button>().Single(button => button.Command == saveCommand);
                    if (save.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.IsEnabled != allowed)
                        throw new InvalidOperationException("The original native Save disagrees with its captured decision.");
                    var text = isPlan ? plan!.WorkText : account!.WorkText;
                    if (text is not null) CheckCaption(VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == text), ProfileNativeReads);
                    var action = WrappingActions(page).Single(item => item.Command == reviewCommand);
                    CheckCaption(VisualDescendants(action).OfType<Label>().Single(), ProfileNativeReads);
                    var input = VisualDescendants(page).OfType<Entry>().First();
                    if (input.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox { IsEnabled: true })
                        throw new InvalidOperationException("Availability review disabled or lost the retained native input.");
                    var current = isPlan ? JsonSerializer.Serialize(new { plan!.Name, plan.Note, plan.AmountText, plan.Start, plan.Account, plan.ReminderDaysText, plan.ReminderTime }) : account!.Form.Snapshot() + "|" + account.IsDefault;
                    if (current != raw || before != await StoredAsync()) throw new InvalidOperationException("Readiness discarded raw input or wrote complete original rows.");
                    await CaptureAsync(app, folder, language + "-editor-readiness-" + (isPlan ? "plan" : "account") + "-" + name);
                }
                await CheckAsync(false, "quota");
                access.Current = new(path, new(ProductPlan.Plus, scope));
                var review = VisualDescendants(WrappingActions(page).Single(item => item.Command == reviewCommand)).OfType<Button>().Single();
                InvokeChoiceReview(review, ProfileNativeReads); if (reviewCommand.ExecutionTask is { } refreshing) await refreshing;
                await CheckAsync(true, "fresh");
                access.Refuse = true; InvokeChoiceReview(review, ProfileNativeReads); if (reviewCommand.ExecutionTask is { } failure) await failure;
                await CheckAsync(false, "read-failed");
                access.Refuse = false; InvokeChoiceReview(review, ProfileNativeReads); if (reviewCommand.ExecutionTask is { } retry) await retry;
                await CheckAsync(true, "retry");
                var cancel = VisualDescendants(page).OfType<Button>().Single(button => button.Command == cancelCommand);
                InvokeChoiceReview(cancel, ProfileNativeReads); if (cancelCommand.ExecutionTask is { } leaving) await leaving;
                if (before != await StoredAsync()) throw new InvalidOperationException("Original editor cancellation saved money or preferences.");
            }
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-editor-readiness-proof.json"), JsonSerializer.Serialize(new
            { ActualOriginalAccountAndPlanPages = true, BeforeAttemptNativeSaveDisabled = true, CompleteNativeReasonAndReviewTarget = true,
                NativeExplicitRefreshFailureAndRetry = true, CompleteRawDraftAnd25TablesUnchanged = true,
                ExplicitFictitiousConsentAndIndependentCachedAccess = true, NoSaveOrPaidActivation = true }));
        }
    }
}
#endif
