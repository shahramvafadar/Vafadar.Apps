#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Only picker/authentication/share effects are fictitious; native commands use original application/writer sources.</summary>
    private sealed class DataFileReviewEffects : IDataFileInteraction
    {
        internal PickedDataFile? Picked;
        public Task<PickedDataFile?> PickCsvAsync(string title) => Task.FromResult(Picked);
        public Task<bool> ConfirmExportAsync(string reason) => throw new InvalidOperationException("No export belongs to this independent import review.");
        public Task ShareCsvAsync(string fileName, string title, string content) => throw new InvalidOperationException("No share belongs to this independent import review.");
    }

    // D-166 / AT-168: actual native review/import/Undo with independent SQLite and retained exact financial packets.
    private static async Task ReviewDataFileOutcomesAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "import-outcomes.db"); var access = new EditorReadinessAccess();
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
        var day = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
        var account = new Account { Name = "Fictitious original funding", CurrencyCode = "EUR", OpeningDate = day };
        await store.SaveAccountAsync(account); await ApplyOwnedReviewModeAsync(store);
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
        var before = await StoredAsync(); var translator = services.GetRequiredService<Translator>(); var localization = services.GetRequiredService<ILocalizationService>();
        var scope = new EntitlementScope(EntitlementScopeKind.PersonalProfile, Guid.Parse("87878787-1212-3434-5656-787878787878"));
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language); access.Current = CommercialWriteAccess.Inactive;
            var effects = new DataFileReviewEffects(); var dialogs = new EditorWriteReviewInteraction();
            var row = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1234, Date = day,
                Title = "Fictitious complete reviewed purchase", Note = "Fictitious retained complete note", Tags = ["retained"] };
            effects.Picked = new("fictitious-import.csv", CsvExport.Write([row], new Dictionary<Guid, Account> { [account.Id] = account }, _ => "", true));
            var vm = new ImportExportViewModel(store, translator, new DateFormatter(localization), localization, TimeProvider.System, holdings, effects, dialogs);
            await vm.LoadAsync(); var page = new ImportExportPage(vm); ProfileNativeReads.Add(page);
            await Shell.Current.Navigation.PushModalAsync(page); await Task.Delay(200);
            var scroll = FindScrollView(page) ?? throw new InvalidOperationException("Missing actual import viewport.");
            var pick = VisualDescendants(page).OfType<Button>().Single(button => button.Command == vm.PickCommand);
            await ScrollToViewIfNeededAsync(scroll, pick); InvokeChoiceReview(pick, ProfileNativeReads);
            if (vm.PickCommand.ExecutionTask is { } picking) await picking;
            var preview = vm.PreviewText; var originalName = vm.FileName;
            void CheckPinned()
            {
                foreach (var text in new[] { vm.ImportError, vm.ImportResult, vm.UndoError }.Where(text => text is not null))
                    CheckCaption(VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == text), ProfileNativeReads);
            }
            async Task InvokeAsync(CommunityToolkit.Mvvm.Input.IAsyncRelayCommand command)
            {
                // The picker/review publishes new visibility before WinUI has realized its growing footer.
                await Task.Delay(200);
                var action = WrappingActions(page).Single(item => item.Command == command);
                if (command == vm.UndoCommand) await ScrollToViewIfNeededAsync(scroll, action);
                CheckCaption(VisualDescendants(action).OfType<Label>().Single(), ProfileNativeReads);
                InvokeChoiceReview(VisualDescendants(action).OfType<Button>().Single(), ProfileNativeReads);
                if (command.ExecutionTask is { } running) await running; await Task.Delay(150);
            }
            await InvokeAsync(vm.RefreshPreviewCommand);
            if (before != await StoredAsync() || preview != vm.PreviewText || originalName != vm.FileName) throw new InvalidOperationException("Native fresh review changed original data.");
            access.Current = new(path, new(ProductPlan.Free, new(EntitlementScopeKind.SharedSpace, scope.Id)));
            await InvokeAsync(vm.ImportCommand); CheckPinned();
            if (before != await StoredAsync() || preview != vm.PreviewText || !vm.CanRunImport) throw new InvalidOperationException("Original refusal lost its complete review.");
            await CaptureAsync(app, folder, language + "-import-outcomes-rejected");
            effects.Picked = null; await ScrollToViewIfNeededAsync(scroll, pick); InvokeChoiceReview(pick, ProfileNativeReads);
            if (vm.PickCommand.ExecutionTask is { } cancelled) await cancelled;
            if (before != await StoredAsync() || preview != vm.PreviewText || originalName != vm.FileName) throw new InvalidOperationException("Native picker cancellation lost review.");
            access.Current = new(path, new(ProductPlan.Free, scope)); var notified = 0;
            EventHandler refusal = (_, _) => { notified++; throw new IOException("Fictitious actual committed subscriber refusal."); };
            store.Changed += refusal;
            try
            {
                await InvokeAsync(vm.ImportCommand); CheckPinned();
                if (vm.CanRunImport || vm.HasPreview || vm.CanReviewPreview || notified != 1 || (await store.GetEntriesAsync()).Single().Id != row.Id)
                    throw new InvalidOperationException("Actual accepted import lost its original result or stayed resubmittable.");
                await CaptureAsync(app, folder, language + "-import-outcomes-committed");
                await InvokeAsync(vm.UndoCommand); CheckPinned();
                if (vm.Batches.Count != 0 || notified != 2 || before != await StoredAsync()) throw new InvalidOperationException("Actual accepted Undo lost original complete rows.");
                await CaptureAsync(app, folder, language + "-import-outcomes-undone");
            }
            finally { store.Changed -= refusal; }
            await Shell.Current.Navigation.PopModalAsync();
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-import-outcomes-proof.json"), JsonSerializer.Serialize(new
            { ActualOriginalPageAndNativeReviewImportUndo = true, RetainedRefusalAndPickerCancel = true, CompletePinnedReasonResultAndTarget = true,
                RealAtomicImportAndUndoWithOriginalCommittedResults = true, Complete25OriginalTablesRestored = true,
                ExplicitFictitiousPickerConsentFailureReportingAndCachedAccess = true, NoProductionActivationOrDesktopInput = true }));
        }
    }
}
#endif
