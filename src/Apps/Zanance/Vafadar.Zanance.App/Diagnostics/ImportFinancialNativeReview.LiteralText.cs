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
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class ImportFinancialNativeReview
{
    /// <summary>Runs only the new literal-text contract through original native preview/import controls.</summary>
    private static async Task<object> RunLiteralAsync(IServiceProvider services, string folder, string context,
        Func<ImportExportPage, Task> show, Func<ImportExportPage, ICommand, Task> invoke,
        Func<string, ImportExportPage, string, Task> inspect, Func<ImportExportPage, Task> close, CancellationToken ct)
    {
        var package = AppInfo.Current.PackageName;
        if (!package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)
            && package != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent literal-text caller required.");
        var final = Environment.GetEnvironmentVariable("VAFADAR_IMPORT_LITERAL_SOURCE_RUN") is { Length: > 0 };
#if ANDROID
        final |= Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_csv_literal_final", false) == true;
#endif
        if (final) return await RunLiteralFinalAsync(services, folder, context, show, invoke, inspect, close, ct);
        Directory.CreateDirectory(folder);
        var original = await StoredAsync(services, ct);
        var text = services.GetRequiredService<Translator>();
        var local = services.GetRequiredService<ILocalizationService>();
        var time = services.GetRequiredService<TimeProvider>();
        var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var results = new List<JsonElement>();
        foreach (var holdingsFile in new[] { false, true })
        {
            var prefix = holdingsFile ? "literal-holdings" : "literal-ledger";
            var receipt = Path.Combine(folder, prefix + "-complete.json");
            if (File.Exists(receipt))
            {
                using var accepted = JsonDocument.Parse(await File.ReadAllTextAsync(receipt, ct));
                if (accepted.RootElement.GetProperty("Context").GetString() != context
                    || !accepted.RootElement.GetProperty("AcceptedOnce").GetBoolean()) throw new IOException("Original literal-text receipt differs.");
                results.Add(accepted.RootElement.Clone());
                continue;
            }
            var file = Path.Combine(folder, prefix + ".db");
            if (File.Exists(file)) throw new IOException("Preserve partial literal-text source and accepted actions; never reseed or replay import.");
            await using var fixture = new ServiceCollection().AddSingleton(time).AddZananceData(file).BuildServiceProvider();
            await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            var store = fixture.GetRequiredService<ZananceStore>();
            var holdings = fixture.GetRequiredService<HoldingStore>();
            await store.GetSettingsAsync(ct);
            var account = new Account { Name = "'=D499 Cash", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 10000 };
            var other = new Account { Name = "=D499 Cash", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 20000 };
            await store.SaveAccountAsync(account, ct); await store.SaveAccountAsync(other, ct);
            var entry = new LedgerEntry { AccountId = account.Id, Date = day, Kind = EntryKind.Expense, Amount = 100,
                Title = "'=D499 original purchase", Payee = "'@D499 payee", Note = "  D499 first\nlast line  ",
                ReimbursableAmount = 50, ReimbursedBy = "'-D499 employer" };
            var type = new AssetType { Name = "'=D499 asset", Kind = AssetKind.Other, Dimension = AssetDimension.Count,
                CountUnitName = "'unit", PriceCurrencyCode = "EUR", Note = "  D499 type note  " };
            var location = new AssetLocation { Name = "'@D499 safe" };
            var opening = new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Kind = AssetEventKind.Opening,
                Date = day, Quantity = 3000, BasisAmount = 3000, Note = "'=D499 opening note" };
            var csv = holdingsFile ? HoldingsCsv.Write([type], [location], [opening], [], true)
                : CsvExport.Write([entry], new Dictionary<Guid, Account> { [account.Id] = account, [other.Id] = other }, _ => string.Empty, true);
            await File.WriteAllTextAsync(Path.Combine(folder, prefix + "-source.json"), JsonSerializer.Serialize(new
                { Context = context, account, other, entry, type, location, opening, Csv = csv }), ct);
            var vm = new ImportExportViewModel(store, text, new DateFormatter(local), local, time, holdings,
                new Files(new("fictitious-" + prefix + ".csv", csv)), new Dialogs());
            await vm.LoadAsync();
            var page = new ImportExportPage(vm); await show(page);
            async Task Invoke(IAsyncRelayCommand command)
            {
                await invoke(page, command);
                if (command.ExecutionTask is { } pending) await pending;
                if (vm.IsBusy) throw new IOException("Preserve pending original literal-text command.");
            }
            await Invoke(vm.PickCommand);
            if (!vm.CanRunImport || vm.ShowMapping || vm.InvalidLines.Count != 0) throw new IOException("Original version-three review unavailable.");
            if (!holdingsFile)
            {
                var preview = vm.LedgerPreviewRows.Single();
                if (!preview.Title.Contains(entry.Title, StringComparison.Ordinal)
                    || !preview.Accounts.Contains(account.Name, StringComparison.Ordinal)) throw new IOException("Literal account/title preview changed.");
                await inspect(prefix + "-title", page, preview.Title);
                await inspect(prefix + "-account", page, preview.Accounts);
            }
            else await inspect(prefix + "-preview", page, vm.PreviewText!);
            await File.WriteAllTextAsync(Path.Combine(folder, prefix + "-import-started.json"),
                JsonSerializer.Serialize(new { Context = context, Source = file, At = time.GetUtcNow() }), ct);
            await Invoke(vm.ImportCommand);
            if (vm.CanRunImport || vm.ImportResult is null || vm.ImportError is not null) throw new IOException("Preserve original literal-text outcome.");
            await File.WriteAllTextAsync(Path.Combine(folder, prefix + "-accepted.json"),
                JsonSerializer.Serialize(new { Context = context, AcceptedOnce = true, vm.ImportResult }), ct);
            if (holdingsFile)
            {
                var savedType = (await holdings.GetTypesAsync(ct)).Single();
                var savedLocation = (await holdings.GetLocationsAsync(ct)).Single();
                var saved = (await holdings.GetEventsAsync(cancellationToken: ct)).Single();
                if (savedType.Name != type.Name || savedType.CountUnitName != type.CountUnitName || savedType.Note != type.Note
                    || savedLocation.Name != location.Name || saved.Quantity != 3000 || saved.BasisAmount != 3000 || saved.Note != opening.Note
                    || (await store.GetEntriesAsync(cancellationToken: ct)).Count != 0) throw new IOException("Original literal holdings source differs.");
            }
            else
            {
                var saved = (await store.GetEntriesAsync(cancellationToken: ct)).Single();
                if (saved.AccountId != account.Id || saved.Title != entry.Title || saved.Payee != entry.Payee
                    || saved.Note != entry.Note || saved.ReimbursedBy != entry.ReimbursedBy || saved.Amount != 100
                    || LedgerCalculator.Balance(account, [saved], day) != 9900 || LedgerCalculator.Balance(other, [saved], day) != 20000)
                    throw new IOException("Original literal account or metadata differs after import.");
            }
            await inspect(prefix + "-accepted", page, vm.ImportResult);
            await close(page);
            if (original != await StoredAsync(services, ct)) throw new IOException("Original independent base changed.");
            var result = JsonSerializer.SerializeToElement(new { Context = context, Holdings = holdingsFile,
                AcceptedOnce = true, OriginalVersion = "3", LiteralTextEqual = true, FinancialMeaningEqual = true,
                OriginalBaseEqual = true, NativePickAndImport = true, RealPickerAndConsent = false });
            await File.WriteAllTextAsync(receipt, result.GetRawText(), ct); results.Add(result);
        }
        return new { Context = context, Cases = results, Completed = true, OriginalBaseEqual = true };
    }
}
#endif
