#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Qualifies original retained-review errors through native Invoke on an independently guarded desktop.</summary>
    private static async Task ReviewCsvInputAsync(App app, IServiceProvider services, string folder)
    {
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ICommercialWriteAccessSource>(new EditorReadinessAccess()).AddZananceData(Path.Combine(folder, "csv-input.db")).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>());
        var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
        var day = DateOnly.FromDateTime(DateTime.Now);
        var account = new Account { Name = "Fictitious retained input account", CurrencyCode = "EUR", OpeningDate = day };
        await store.SaveAccountAsync(account); await ApplyOwnedReviewModeAsync(store);
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Date = day, Amount = 1234, Title = "Fictitious retained input", Note = "Fictitious complete note" };
        var picked = new PickedDataFile("fictitious-complete.csv", CsvExport.Write([entry], new Dictionary<Guid, Account> { [account.Id] = account }, _ => "", true));
        var translator = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var emptyOnly = Environment.GetEnvironmentVariable("VAFADAR_STORE_DRAFT_REVIEW") == "csv-input-empty";
        var captures = new List<object>(); var observations = new List<object>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        await store.GetSettingsAsync(); var before = await StoredAsync();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            var context = language + "-" + choice + "-" + width;
            services.GetRequiredService<ThemeService>().Set(choice); app.Windows[0].Width = width; app.Windows[0].Height = 820;
            await SetLanguageAsync(app, local, language);
            var effects = new CsvInputReviewFiles { Selected = picked };
            var vm = new ImportExportViewModel(store, translator, new DateFormatter(local), local, TimeProvider.System, holdings, effects, new EditorWriteReviewInteraction());
            await vm.LoadAsync(); var page = new ImportExportPage(vm).WithFlowDirection(local); ProfileNativeReads.Add(page);
            var root = app.Windows[0].Page ?? throw new IOException("Original review root is missing.");
            await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false); await Task.Delay(150);
            var scroll = VisualDescendants(page).OfType<ScrollView>().Single(view => view.Orientation == ScrollOrientation.Vertical);
            var pick = VisualDescendants(page).OfType<Button>().Single(button => button.Command == vm.PickCommand);
            async Task PickAsync()
            {
                await ScrollToViewIfNeededAsync(scroll, pick); InvokeChoiceReview(pick, ProfileNativeReads);
                if (vm.PickCommand.ExecutionTask is { } pending) await pending;
                await Task.Delay(80);
            }
            await PickAsync();
            if (!vm.CanRunImport || vm.ImportCount != 1) throw new IOException("The original complete review was not published.");
            var originalRows = JsonSerializer.Serialize(vm.LedgerPreviewRows); var originalText = vm.PreviewText;
            foreach (var kind in emptyOnly ? new CsvReadLimit?[] { null } : Enum.GetValues<CsvReadLimit>().Select(limit => (CsvReadLimit?)limit).Append(null))
            {
                var budget = CsvReadLimits.Import;
                var maximum = kind switch { CsvReadLimit.Bytes => budget.MaximumBytes, CsvReadLimit.Characters => budget.MaximumCharacters,
                    CsvReadLimit.Rows => budget.MaximumRows, CsvReadLimit.Columns => budget.MaximumColumns, CsvReadLimit.Cells => budget.MaximumCells,
                    _ => budget.MaximumFieldCharacters };
                effects.Refusal = kind is { } limit ? new CsvReadLimitException(limit, maximum) : null;
                effects.Selected = emptyOnly ? new("fictitious-empty.csv", "") : kind is null ? new("fictitious-unfinished.csv", "a,\"unfinished") : picked;
                await PickAsync();
                if (vm.FileName != picked.Name || vm.PreviewText != originalText || JsonSerializer.Serialize(vm.LedgerPreviewRows) != originalRows
                    || !vm.CanRunImport || vm.IsBusy || vm.ImportError is null)
                    throw new IOException("Rejected CSV replaced the original native review.");
                var label = VisualDescendants(page).OfType<Label>().Single(view => Visible(view) && view.Text == vm.ImportError);
                CheckCaption(label, ProfileNativeReads);
                if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native || native.IsTextTrimmed)
                    throw new IOException("Original CSV refusal text is clipped.");
                observations.Add(new { Context = context, Refusal = kind?.ToString() ?? (emptyOnly ? "Empty" : "Malformed"), label.Text,
                    native.ActualWidth, native.ActualHeight, native.IsTextTrimmed, OriginalReviewRetained = true });
            }
            var name = context + (emptyOnly ? "-csv-input-empty.png" : "-csv-input.png");
            var destination = Path.Combine(folder, language); Directory.CreateDirectory(destination);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(destination, name));
            captures.Add(new { Language = language, File = name });
            await root.Navigation.PopModalAsync(false);
        }
        if (before != await StoredAsync()) throw new IOException("Read-only input qualification changed stored fictitious data.");
        await File.WriteAllTextAsync(Path.Combine(folder, "csv-input-observations.json"), JsonSerializer.Serialize(observations));
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Package = AppInfo.Current.PackageName,
            ProcessId = Environment.ProcessId, Captures = captures, FinancialRowsUnchangedAfterSeeding = true, OriginalReviewRetained = true,
            OriginalErrorChecks = observations.Count, FinancialCommandsInvoked = false, SystemSettingsChanged = false }));
    }

    /// <summary>Only the native file-selection effect is supplied; the real page/parser/view model own every result.</summary>
    private sealed class CsvInputReviewFiles : IDataFileInteraction
    {
        internal PickedDataFile? Selected;
        internal Exception? Refusal;
        public Task<PickedDataFile?> PickCsvAsync(string title) => Refusal is { } error ? Task.FromException<PickedDataFile?>(error) : Task.FromResult(Selected);
        public Task<bool> ConfirmExportAsync(string reason) => throw new IOException("Export is outside the input review.");
        public Task ShareCsvAsync(string fileName, string title, string content) => throw new IOException("Sharing is outside the input review.");
    }
}
#endif
