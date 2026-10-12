#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Explicit fictitious consent/return responses; original UI, reader and writer remain real.</summary>
    private sealed class ArchiveReviewEffects : IAppInteraction
    {
        internal bool Confirm; internal bool RefuseReturn;
        public Task AlertAsync(string title, string message, string cancel) => Task.CompletedTask;
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Task.FromResult(Confirm);
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new InvalidOperationException("No prompt belongs to this review.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new InvalidOperationException("No menu belongs to this review.");
        public async Task NavigateAsync(string route, IDictionary<string, object>? parameters = null)
        {
            if (route != ".." || RefuseReturn) throw new InvalidOperationException("Fictitious owned archive return refusal.");
            await Shell.Current.Navigation.PopModalAsync();
        }
        public Task ShowFailureAsync(Exception exception) => Task.CompletedTask;
    }

    // D-164 / AT-166: owned original archive button, actual SQL rollback and accepted/frozen outcome.
    private static async Task ReviewAccountArchiveAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var translator = services.GetRequiredService<Translator>(); var localization = services.GetRequiredService<ILocalizationService>();
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
            var path = Path.Combine(folder, "account-archive-" + language + ".db");
            await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System).AddZananceData(path).BuildServiceProvider();
            fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>(); var plans = fixture.GetRequiredService<PlanStore>();
            var day = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
            var original = new Account { Name = "Fictitious original archive account", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 12345 };
            await store.SaveAccountAsync(original);
            var active = new Schedule { Name = "Fictitious original active plan", AccountId = original.Id, Amount = 1234, AutoPost = true,
                Rule = new() { Frequency = Frequency.Once, Start = day.AddDays(1) } };
            var paused = new Schedule { Name = "Fictitious original paused plan", AccountId = original.Id, Amount = 2345,
                State = ScheduleState.Paused, PausedFrom = day, Rule = new() { Frequency = Frequency.Once, Start = day.AddDays(1) } };
            await plans.SaveSchedulesAsync([active, paused]); await ApplyOwnedReviewModeAsync(store);
            var settings = await store.GetSettingsAsync(); settings.DefaultAccountId = original.Id; await store.SaveSettingsAsync(settings);
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
            var before = await StoredAsync(); var effects = new ArchiveReviewEffects();
            var vm = new AccountEditorViewModel(store, plans, translator, localization, TimeProvider.System, effects);
            await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = original.Id });
            vm.Form.Name = "Fictitious complete unsaved identity"; vm.Form.OpeningText = "999"; var raw = vm.Form.Snapshot();
            var page = new AccountEditorPage(vm); ProfileNativeReads.Add(page); await Shell.Current.Navigation.PushModalAsync(page); await Task.Delay(200);
            var archive = VisualDescendants(page).OfType<Button>().Single(button => button.Command == vm.ToggleArchiveCommand);
            await ScrollToViewIfNeededAsync(FindScrollView(page)!, archive); InvokeChoiceReview(archive, ProfileNativeReads);
            if (vm.ToggleArchiveCommand.ExecutionTask is { } declined) await declined;
            if (raw != vm.Form.Snapshot() || await StoredAsync() != before) throw new InvalidOperationException("Declined archive lost raw input or original rows.");
            await using (var db = await factory.CreateDbContextAsync())
                await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER OwnedArchiveRefusal BEFORE UPDATE ON Accounts BEGIN SELECT RAISE(ABORT, 'Owned native archive refusal'); END;");
            effects.Confirm = true; InvokeChoiceReview(archive, ProfileNativeReads);
            if (vm.ToggleArchiveCommand.ExecutionTask is { } rejected) await rejected;
            if (vm.SaveError != translator["Account_ArchiveNeedsReview"] || raw != vm.Form.Snapshot() || await StoredAsync() != before)
                throw new InvalidOperationException("Actual SQL refusal left partial archive/default/plan changes or lost the draft.");
            await Task.Delay(150);
            var caption = VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == vm.SaveError);
            CheckCaption(caption, ProfileNativeReads); await CaptureAsync(app, folder, language + "-account-archive-rejected");
            await using (var db = await factory.CreateDbContextAsync()) await db.Database.ExecuteSqlRawAsync("DROP TRIGGER OwnedArchiveRefusal;");
            effects.RefuseReturn = true; InvokeChoiceReview(archive, ProfileNativeReads);
            if (vm.ToggleArchiveCommand.ExecutionTask is { } accepted) await accepted;
            if (vm.SaveError != translator["Editor_SavedReturnFailed"] || vm.CanInteract || !vm.IsArchived || vm.IsDefault)
                throw new InvalidOperationException("Accepted archive lost committed state or default cleanup.");
            var stored = (await store.GetAccountsAsync()).Single();
            if (!stored.IsArchived || stored.Name != original.Name || stored.OpeningBalance != original.OpeningBalance
                || vm.Form.Name != original.Name || (await store.GetSettingsAsync()).DefaultAccountId is not null)
                throw new InvalidOperationException("Archive silently saved unsaved financial fields or missed its exact default cleanup.");
            if ((await plans.GetSchedulesAsync()).Any(row => row.State != ScheduleState.Ended || row.AutoPost || row.PausedFrom is not null))
                throw new InvalidOperationException("Related active/paused plans did not end in the same accepted action.");
            var acceptedRows = await StoredAsync(); await vm.ToggleArchiveCommand.ExecuteAsync(null);
            if (acceptedRows != await StoredAsync()) throw new InvalidOperationException("A committed archive repeated its write.");
            await Task.Delay(150); caption = VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == vm.SaveError);
            CheckCaption(caption, ProfileNativeReads); await CaptureAsync(app, folder, language + "-account-archive-saved-return");
            var close = VisualDescendants(page).OfType<Button>().Single(button => button.Command == vm.CancelCommand);
            if (close.Text != translator["Common_Close"]) throw new InvalidOperationException("Accepted archive has no actual Close caption.");
            effects.RefuseReturn = false; InvokeChoiceReview(close, ProfileNativeReads); if (vm.CancelCommand.ExecutionTask is { } leaving) await leaving;
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-account-archive-proof.json"), JsonSerializer.Serialize(new
            { OriginalNativeArchiveAndClose = true, DeclinedRawDraftRetained = true, ActualSQLiteRefusalAll25RowsUnchanged = true,
                ActualAtomicAcceptedAccountPlansDefault = true, UnsavedFinancialFieldsNotStored = true, CommittedArchiveCannotRepeat = true,
                ExplicitFictitiousConsentAlertAndReturnPorts = true, OnlyIndependentOwnedFixtureWrites = true }));
        }
    }
}
#endif
