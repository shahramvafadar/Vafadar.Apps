#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original ordinary CSV financial review, refusal, explicit refresh and atomic import/Undo in owned files.</summary>
internal static class ImportFinancialNativeReview
{
    /// <summary>Supplies only one fictitious selected CSV; no export or private source extraction is possible.</summary>
    private sealed class Files(PickedDataFile selected) : IDataFileInteraction
    {
        public Task<PickedDataFile?> PickCsvAsync(string title) => Task.FromResult<PickedDataFile?>(selected);
        public Task<bool> ConfirmExportAsync(string reason) => throw new IOException("No export belongs to this review.");
        public Task ShareCsvAsync(string fileName, string title, string content) => throw new IOException("No share belongs to this review.");
    }

    /// <summary>Only Undo consent is supplied; any unexpected alert or failure refuses the native qualification.</summary>
    private sealed class Dialogs : IAppInteraction
    {
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected review alert.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Task.FromResult(true);
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("No prompt belongs to this review.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("No menu belongs to this review.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new IOException("No application navigation belongs to this financial fixture.");
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Unexpected original import failure.", exception);
    }

    /// <summary>Uses actual native page callbacks; refuses any existing fixture rather than replaying accepted money.</summary>
    internal static async Task<object> RunAsync(IServiceProvider services, string folder, string context,
        Func<ImportExportPage, Task> show, Func<ImportExportPage, ICommand, Task> invoke,
        Func<string, ImportExportPage, string, Task> inspect, Func<ImportExportPage, Task> close, CancellationToken ct,
        IReadOnlyDictionary<bool, JsonElement>? completed = null)
    {
        var package = AppInfo.Current.PackageName;
        if (!package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)
            && package != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent import review identity required.");
        Directory.CreateDirectory(folder);
        var text = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var time = services.GetRequiredService<TimeProvider>(); var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var results = new List<object>();
        foreach (var generic in new[] { false, true })
        {
            if (completed is not null && completed.TryGetValue(generic, out var previous))
            { results.Add(previous); continue; } // Original accepted import/Undo is never replayed for an observer repair.
            var prefix = generic ? "mapped" : "own"; var file = Path.Combine(folder, prefix + ".db");
            if (File.Exists(file))
            {
                results.Add(await ResumeAcceptedAsync(services, file, context, prefix, show, invoke, inspect, close, ct));
                continue;
            }
            await using var fixture = new ServiceCollection().AddZananceData(file).AddSingleton(time).BuildServiceProvider();
            await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
            await store.GetSettingsAsync(ct);
            var account = new Account { Name = "D405 " + prefix + " " + context, CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 90000 };
            await store.SaveAccountAsync(account, ct);
            var row = new LedgerEntry { AccountId = account.Id, Date = day.AddDays(-1), Kind = EntryKind.Expense,
                Amount = 1234, Title = "D405 original historical purchase", Note = "D405 original retained note" };
            var csv = generic ? "date;amount;title\r\n" + row.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                + ";-12.34;D405 original mapped purchase\r\n"
                : CsvExport.Write([row], new Dictionary<Guid, Account> { [account.Id] = account }, _ => "", true);
            var vm = new ImportExportViewModel(store, text, new DateFormatter(local), local, time, holdings,
                new Files(new("fictitious-" + prefix + ".csv", csv)), new Dialogs());
            await vm.LoadAsync(); var page = new ImportExportPage(vm); await show(page);
            async Task Invoke(IAsyncRelayCommand command)
            {
                await invoke(page, command);
                if (command.ExecutionTask is { } pending) await pending;
                if (vm.IsBusy) throw new IOException("Preserve the pending original financial command.");
            }
            await Invoke(vm.PickCommand);
            if (generic) { vm.CalendarIndex = 0; vm.SeparatorIndex = 0; vm.DateFormat = "yyyy-MM-dd"; await Invoke(vm.PreviewCommand); }
            if (!vm.CanRunImport || vm.WarningText != text.Format("Import_BeforeOpening", 1)) throw new IOException("Original historical boundary missing.");
            var source = vm.FileName; var originalPreview = vm.PreviewText;
            account.OpeningDate = day.AddDays(-2); await store.SaveAccountAsync(account, ct);
            var before = await StoredAsync(fixture, ct);
            await Invoke(vm.ImportCommand);
            if (vm.CanRunImport || vm.ImportResult is not null || vm.ImportError != text["Import_Overlap_ChangedPreview"]
                || source != vm.FileName || originalPreview != vm.PreviewText || before != await StoredAsync(fixture, ct))
                throw new IOException("A stale original review wrote data or lost its raw source.");
            await inspect(prefix + "-refused", page, vm.ImportError);
            await Invoke(vm.RefreshPreviewCommand);
            if (!vm.CanRunImport || vm.WarningText is not null) throw new IOException("Explicit original refresh did not review the new boundary.");
            await Invoke(vm.ImportCommand);
            var saved = (await store.GetEntriesAsync(cancellationToken: ct)).Single();
            if (saved.Amount != 1234 || saved.Date != row.Date || LedgerCalculator.Balance(account, [saved], day) != 88766
                || vm.CanRunImport || vm.ImportResult != text.Format("Import_Done", 1, 0)) throw new IOException("Original accepted import outcome differs.");
            await File.WriteAllTextAsync(Path.Combine(folder, prefix + "-accepted.json"), JsonSerializer.Serialize(new
                { Context = context, OriginalEntry = saved.Id, Count = 1, Balance = 88766, AcceptedOnce = true }), ct);
            await inspect(prefix + "-accepted", page, vm.ImportResult);
            await Invoke(vm.UndoCommand);
            if (before != await StoredAsync(fixture, ct)) throw new IOException("Original Undo did not restore the complete fixture.");
            await close(page);
            var result = new { Context = context, Generic = generic, HistoricalRefusal = true, RawSourceRetained = true,
                NativeExplicitRefresh = true, OriginalImportAndUndoOnce = true, CompleteStoredRowsRestored = true,
                FictitiousPickerAndUndoConsent = true, NoInstalledPrivateData = true };
            await File.WriteAllTextAsync(Path.Combine(folder, prefix + "-complete.json"), JsonSerializer.Serialize(result), ct); results.Add(result);
        }
        return new { Context = context, Results = results };
    }

    /// <summary>Finishes only the original retained batch/Undo after an accepted-write observation failure.</summary>
    private static async Task<object> ResumeAcceptedAsync(IServiceProvider services, string file, string context, string prefix,
        Func<ImportExportPage, Task> show, Func<ImportExportPage, ICommand, Task> invoke,
        Func<string, ImportExportPage, string, Task> inspect, Func<ImportExportPage, Task> close, CancellationToken ct)
    {
        var folder = Path.GetDirectoryName(file)!;
        using var receipt = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, prefix + "-accepted.json"), ct));
        var accepted = receipt.RootElement;
        if (accepted.GetProperty("Context").GetString() != context || !accepted.GetProperty("AcceptedOnce").GetBoolean())
            throw new IOException("Original accepted fixture receipt required; never replay import.");
        await using var fixture = new ServiceCollection().AddZananceData(file).BuildServiceProvider();
        var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
        var entry = (await store.GetEntriesAsync(cancellationToken: ct)).Single();
        var account = (await store.GetAccountsAsync(cancellationToken: ct)).Single();
        if (entry.Id != accepted.GetProperty("OriginalEntry").GetGuid() || entry.Amount != 1234
            || account.OpeningBalance != 90000 || (await store.GetImportBatchesAsync(ct)).Count != 1)
            throw new IOException("Original accepted financial fixture differs.");
        // The newly owned original fixture had no ledger/journal rows. Preserve every other typed
        // table through the pending Undo, rather than inventing a lost in-memory pre-write packet.
        var otherRows = await StoredAsync(fixture, ct, omitImportedRows: true);
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var vm = new ImportExportViewModel(store, text, new DateFormatter(local), local, services.GetRequiredService<TimeProvider>(),
            holdings, new Files(new("unused-retained-source.csv", "")), new Dialogs());
        await vm.LoadAsync(); var page = new ImportExportPage(vm); await show(page);
        await inspect(prefix + "-retained-batch", page, vm.Batches.Single().Text);
        await invoke(page, vm.UndoCommand); if (vm.UndoCommand.ExecutionTask is { } undo) await undo;
        if ((await store.GetEntriesAsync(cancellationToken: ct)).Count != 0 || (await store.GetImportBatchesAsync(ct)).Count != 0
            || otherRows != await StoredAsync(fixture, ct, omitImportedRows: true)) throw new IOException("Original pending Undo did not restore its known empty financial baseline.");
        await close(page);
        var result = new { Context = context, Generic = prefix == "mapped", OriginalImportAndUndoOnce = true,
            CompleteStoredRowsRestored = true, ResumedOriginalAcceptedBatchWithoutImportReplay = true,
            AllOtherTypedRowsUnchangedThroughUndo = true, OriginalKnownEmptyLedgerAndJournalRestored = true,
            OriginalTransientAcceptedCaptionQualified = false, NativeRetainedHistoryQualified = true };
        await File.WriteAllTextAsync(Path.Combine(folder, prefix + "-complete.json"), JsonSerializer.Serialize(result), ct);
        return result;
    }

    /// <summary>Compares every typed row in the independently owned fixture without a hardcoded historical table count.</summary>
    internal static async Task<string> StoredAsync(IServiceProvider services, CancellationToken ct, bool omitImportedRows = false)
    {
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct); using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
        var tables = new List<string>();
        using (var reader = await command.ExecuteReaderAsync(ct)) while (await reader.ReadAsync(ct)) tables.Add(reader.GetString(0));
        var mapped = db.Model.GetEntityTypes().Select(entity => entity.GetTableName()).OfType<string>().Distinct(StringComparer.Ordinal);
        if (mapped.Except(tables, StringComparer.Ordinal).Any()) throw new IOException("Incomplete original mapped fixture schema.");
        var rows = new SortedDictionary<string, List<string>>();
        foreach (var table in tables)
        {
            if (omitImportedRows && table is "Entries" or "ImportLinks") continue;
            command.CommandText = "SELECT * FROM \"" + table.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            using var reader = await command.ExecuteReaderAsync(ct); var values = new List<string>();
            while (await reader.ReadAsync(ct))
            {
                var packet = new object?[reader.FieldCount];
                for (var i = 0; i < packet.Length; i++) packet[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                values.Add(JsonSerializer.Serialize(packet));
            }
            values.Sort(StringComparer.Ordinal); rows.Add(table, values);
        }
        return JsonSerializer.Serialize(rows);
    }
}
#endif
