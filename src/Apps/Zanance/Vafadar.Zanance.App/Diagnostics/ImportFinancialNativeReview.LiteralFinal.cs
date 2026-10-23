#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class ImportFinancialNativeReview
{
    /// <summary>Reuses the accepted own-file source read-only, then runs only the previously unexecuted generic case.</summary>
    private static async Task<object> RunLiteralFinalAsync(IServiceProvider services, string folder, string context,
        Func<ImportExportPage, Task> show, Func<ImportExportPage, ICommand, Task> invoke,
        Func<string, ImportExportPage, string, Task> inspect, Func<ImportExportPage, Task> close, CancellationToken ct)
    {
        var output = folder;
        if (Environment.GetEnvironmentVariable("VAFADAR_IMPORT_LITERAL_SOURCE_RUN") is { Length: > 0 } sourceRun)
        {
            if (!Guid.TryParseExact(sourceRun, "N", out _)) throw new IOException("Exact original literal-text source required.");
            var prior = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "store-drafts", sourceRun);
            using var process = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(prior, "process.json"), ct));
            using var complete = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(prior, "complete.json"), ct));
            if (process.RootElement.GetProperty("Package").GetString() != "pro.vafadar.zanance.storepreview." + sourceRun[..12]
                || !complete.RootElement.GetProperty("LiteralText").GetBoolean()) throw new IOException("Original literal source identity differs.");
            folder = Path.Combine(prior, context);
        }
        Directory.CreateDirectory(output);
        var text = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var time = services.GetRequiredService<TimeProvider>(); var original = await StoredAsync(services, ct);
        var results = new List<object>();
        async Task Invoke(ImportExportPage page, ImportExportViewModel vm, IAsyncRelayCommand command)
        {
            await invoke(page, command);
            if (command.ExecutionTask is { } pending) await pending;
            if (vm.IsBusy) throw new IOException("Preserve the pending literal-text continuation.");
        }
        using (var accepted = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "literal-ledger-complete.json"), ct)))
            if (accepted.RootElement.GetProperty("Context").GetString() != context || !accepted.RootElement.GetProperty("AcceptedOnce").GetBoolean())
                throw new IOException("Original accepted literal ledger is required; never recreate it.");
        using (var source = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "literal-ledger-source.json"), ct)))
        await using (var fixture = new ServiceCollection().AddSingleton(time).AddZananceData(Path.Combine(folder, "literal-ledger.db")).BuildServiceProvider())
        {
            var before = await StoredAsync(fixture, ct);
            var rows = Csv.Read(source.RootElement.GetProperty("Csv").GetString()!, ',').Select(row => row.ToList()).ToArray();
            var index = rows[0].IndexOf("format_version");
            if (index < 0 || rows[1][index] != "3") throw new IOException("Original literal marker missing.");
            rows[0].Insert(index, "optional_column"); rows[1].Insert(index, string.Empty);
            var vm = new ImportExportViewModel(fixture.GetRequiredService<ZananceStore>(), text, new DateFormatter(local), local, time,
                fixture.GetRequiredService<HoldingStore>(), new Files(new("fictitious-named-marker.csv", Csv.Write(rows))), new Dialogs());
            await vm.LoadAsync(); var page = new ImportExportPage(vm); await show(page);
            await Invoke(page, vm, vm.PickCommand);
            var preview = vm.LedgerPreviewRows.Single();
            if (vm.CanRunImport || vm.InvalidLines.Count != 0 || vm.ImportCount != 0
                || !preview.Title.Contains("'=D499 original purchase", StringComparison.Ordinal)
                || !preview.Accounts.Contains("'=D499 Cash", StringComparison.Ordinal)) throw new IOException("Named marker preview differs from accepted original source.");
            await inspect("literal-marker-title", page, preview.Title);
            await inspect("literal-marker-account", page, preview.Accounts);
            await close(page);
            if (before != await StoredAsync(fixture, ct)) throw new IOException("Read-only marker continuation changed the accepted source.");
            results.Add(new { HeaderReadOnly = true, LiteralTextEqual = true, FinancialWrites = 0, OriginalSourceEqual = true, NativePickOnly = true });
        }
        var file = Path.Combine(output, "literal-generic.db");
        if (File.Exists(file)) throw new IOException("Preserve original generic partial source and actions; no reseed or import replay.");
        await using (var fixture = new ServiceCollection().AddSingleton(time).AddZananceData(file).BuildServiceProvider())
        {
            await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            var store = fixture.GetRequiredService<ZananceStore>(); await store.GetSettingsAsync(ct);
            var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
            var account = new Account { Name = "D499 generic cash", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 10000 };
            await store.SaveAccountAsync(account, ct);
            const string title = "'=D499 generic purchase"; const string note = "  D499 generic note  ";
            var csv = Csv.Write([new[] { "date", "amount", "title", "note" },
                new[] { day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), "-1.00", title, note }]);
            var vm = new ImportExportViewModel(store, text, new DateFormatter(local), local, time, fixture.GetRequiredService<HoldingStore>(),
                new Files(new("fictitious-literal-generic.csv", csv)), new Dialogs());
            await vm.LoadAsync(); var page = new ImportExportPage(vm); await show(page);
            await Invoke(page, vm, vm.PickCommand);
            vm.CalendarIndex = 0; vm.SeparatorIndex = 0; vm.DateFormat = "yyyy-MM-dd";
            await Invoke(page, vm, vm.PreviewCommand);
            if (!vm.CanRunImport || !vm.LedgerPreviewRows.Single().Title.Contains(title, StringComparison.Ordinal))
                throw new IOException("Original generic literal preview unavailable.");
            await inspect("literal-generic-title", page, vm.LedgerPreviewRows.Single().Title);
            await inspect("literal-generic-account", page, vm.LedgerPreviewRows.Single().Accounts);
            await File.WriteAllTextAsync(Path.Combine(output, "literal-generic-import-started.json"), JsonSerializer.Serialize(new { Context = context, At = time.GetUtcNow() }), ct);
            await Invoke(page, vm, vm.ImportCommand);
            await File.WriteAllTextAsync(Path.Combine(output, "literal-generic-accepted.json"), JsonSerializer.Serialize(new { Context = context, AcceptedOnce = !vm.CanRunImport, vm.ImportResult }), ct);
            var saved = (await store.GetEntriesAsync(cancellationToken: ct)).Single();
            if (vm.CanRunImport || vm.ImportResult is null || vm.ImportError is not null || saved.AccountId != account.Id
                || saved.Title != title || saved.Note != note || saved.Amount != 100 || LedgerCalculator.Balance(account, [saved], day) != 9900)
                throw new IOException("Preserve accepted generic source; its literal outcome differs.");
            await inspect("literal-generic-accepted", page, vm.ImportResult);
            await close(page);
            results.Add(new { Generic = true, AcceptedOnce = true, LiteralTextEqual = true, FinancialMeaningEqual = true });
        }
        if (original != await StoredAsync(services, ct)) throw new IOException("Original independent base changed during literal continuation.");
        return new { Context = context, Cases = results, Completed = true, FinalContinuation = true, OriginalBaseEqual = true };
    }
}
#endif
