using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies original CSV error surfaces and retained reviews without importing a financial record.</summary>
    private static async Task<object> ReviewCsvInputAsync(App app, IServiceProvider services, string root, string comparison, bool emptyOnly, bool quotationOnly, CancellationToken ct)
    {
        var theme = services.GetRequiredService<ThemeService>(); var originalTheme = theme.Choice;
        var local = services.GetRequiredService<ILocalizationService>(); var originalLanguage = local.CurrentLanguage;
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        using var fixture = new ServiceCollection().AddZananceData(Path.Combine(root, "csv-input.db")).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = fixture.GetRequiredService<ZananceStore>(); var holdings = fixture.GetRequiredService<HoldingStore>();
        var day = new DateOnly(2026, 10, 19);
        var account = (await store.GetAccountsAsync(cancellationToken: ct)).SingleOrDefault(row => row.Name == "Fictitious CSV retained input")
            ?? new Account { Name = "Fictitious CSV retained input", CurrencyCode = "EUR", OpeningDate = day };
        if ((await store.GetAccountsAsync(cancellationToken: ct)).Count == 0) await store.SaveAccountAsync(account, ct);
        var entry = new LedgerEntry(Guid.ParseExact(comparison, "N")) { AccountId = account.Id, Kind = EntryKind.Expense,
            Date = day, Amount = 1234, Title = "Fictitious complete retained input", Note = "Fictitious preserved note",
            CreatedAt = new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.Zero) };
        var picked = new PickedDataFile("fictitious-complete.csv", CsvExport.Write([entry], new Dictionary<Guid, Account> { [account.Id] = account }, _ => "", true));
        var inputHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(picked.Text))).ToLowerInvariant();
        var sourceMvid = typeof(AndroidHomeReview).Module.ModuleVersionId;
        var translator = services.GetRequiredService<Translator>(); var observations = new List<JsonElement>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
            Entries = await store.GetEntriesAsync(cancellationToken: ct), Settings = await store.GetSettingsAsync(ct) });
        await store.GetSettingsAsync(ct); var before = await StoredAsync();
        try
        {
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                var context = language + "-" + choice; var receipt = Path.Combine(root, (emptyOnly ? "csv-input-empty-" : quotationOnly ? "csv-quotation-" : "csv-input-") + comparison + "-" + context + ".json");
                if (File.Exists(receipt))
                {
                    using var retained = JsonDocument.Parse(await File.ReadAllTextAsync(receipt, ct)); var prior = retained.RootElement;
                    if (prior.GetProperty("Comparison").GetString() != comparison || prior.GetProperty("SourceMvid").GetGuid() != sourceMvid
                        || prior.GetProperty("InputSHA256").GetString() != inputHash || !prior.GetProperty("OriginalReviewRetained").GetBoolean()
                        || prior.GetProperty("Errors").GetArrayLength() != (emptyOnly || quotationOnly ? 1 : 7)) throw new ReviewCheckException("Retained CSV input context does not match.");
                    observations.Add(prior.Clone()); continue;
                }
                await AwaitReimbursementFocusAsync(ct); theme.Set(choice);
                local.SetLanguage(local.SupportedLanguages.Single(value => value.CultureName == language));
                await Task.Delay(200, ct); await AwaitRootHomeAsync(ct);
                var effects = new CsvInputFiles { Selected = picked };
                var vm = new ImportExportViewModel(store, translator, new DateFormatter(local), local, services.GetRequiredService<TimeProvider>(), holdings, effects, services.GetRequiredService<IAppInteraction>());
                await vm.LoadAsync(); var page = new ImportExportPage(vm).WithFlowDirection(local);
                await Shell.Current.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false); await Task.Delay(200, ct);
                var scroll = ReimbursementViews(page).OfType<ScrollView>().Single(view => view.Orientation == ScrollOrientation.Vertical);
                var button = ReimbursementViews(page).OfType<Button>().Single(value => value.Command == vm.PickCommand);
                async Task PickAsync()
                {
                    await scroll.ScrollToAsync(button, ScrollToPosition.Center, false); await Task.Delay(75, ct);
                    if (button.Handler?.PlatformView is not Android.Views.View native || !native.IsShown || !native.HasWindowFocus || !native.Enabled || !native.PerformClick())
                        throw new ReviewCheckException("The original focused CSV picker button is unavailable.");
                    if (vm.PickCommand.ExecutionTask is { } pending) await pending;
                    await Task.Delay(75, ct);
                }
                await PickAsync();
                if (!vm.CanRunImport || vm.ImportCount != 1) throw new ReviewCheckException("Original complete CSV review did not publish.");
                var originalRows = JsonSerializer.Serialize(vm.LedgerPreviewRows); var originalText = vm.PreviewText; var errors = new List<object>();
                foreach (var kind in emptyOnly || quotationOnly ? new CsvReadLimit?[] { null } : Enum.GetValues<CsvReadLimit>().Select(value => (CsvReadLimit?)value).Append(null))
                {
                    var budget = CsvReadLimits.Import;
                    var maximum = kind switch { CsvReadLimit.Bytes => budget.MaximumBytes, CsvReadLimit.Characters => budget.MaximumCharacters,
                        CsvReadLimit.Rows => budget.MaximumRows, CsvReadLimit.Columns => budget.MaximumColumns, CsvReadLimit.Cells => budget.MaximumCells, _ => budget.MaximumFieldCharacters };
                    effects.Refusal = kind is { } limit ? new CsvReadLimitException(limit, maximum) : null;
                    effects.Selected = emptyOnly ? new("fictitious-empty.csv", "") : kind is null ? new("fictitious-malformed.csv", quotationOnly ? "a,\"12\"34" : "a,\"unfinished") : picked;
                    await PickAsync();
                    if (vm.FileName != picked.Name || vm.PreviewText != originalText || JsonSerializer.Serialize(vm.LedgerPreviewRows) != originalRows
                        || !vm.CanRunImport || vm.IsBusy || vm.ImportError is null) throw new ReviewCheckException("Rejected CSV changed the original native review.");
                    var label = ReimbursementViews(page).OfType<Label>().Single(value => value.Text == vm.ImportError);
                    var complete = false; object? geometry = null;
                    for (var attempt = 0; attempt < 40; attempt++)
                    {
                        if (label.Handler?.PlatformView is Android.Widget.TextView native)
                        {
                            await WaitLayoutAsync(native, ct); using var visible = new Android.Graphics.Rect();
                            var shown = native.GetGlobalVisibleRect(visible); var layout = native.Layout;
                            var expected = label.FlowDirection == FlowDirection.LeftToRight ? label.Text : NativeDigits.Apply(label.Text);
                            var ellipsis = layout is null ? -1 : Enumerable.Range(0, layout.LineCount).Sum(line => layout.GetEllipsisCount(line));
                            complete = native.IsShown && native.HasWindowFocus && shown && native.Text == expected && layout is not null && ellipsis == 0
                                && native.Height - native.TotalPaddingTop - native.TotalPaddingBottom >= layout.Height
                                && visible.Height() >= native.Height - 2 && visible.Width() >= native.Width - 2;
                            geometry = new { native.Width, native.Height, VisibleWidth = visible.Width(), VisibleHeight = visible.Height(),
                                LayoutHeight = layout?.Height, Ellipsis = ellipsis, Complete = complete };
                            if (complete) break;
                        }
                        await Task.Delay(50, ct);
                    }
                    if (!complete) throw new ReviewCheckException("Original CSV refusal text is not completely visible.");
                    errors.Add(new { Refusal = kind?.ToString() ?? (emptyOnly ? "Empty" : quotationOnly ? "MisplacedQuotation" : "Malformed"), OriginalText = label.Text, Geometry = geometry });
                }
                await Shell.Current.Navigation.PopModalAsync(false); await AwaitRootHomeAsync(ct);
                if (before != await StoredAsync()) throw new ReviewCheckException("Input review changed stored fictitious records.");
                var result = new { Comparison = comparison, Context = context, SourceMvid = sourceMvid, InputSHA256 = inputHash, Errors = errors,
                    OriginalReviewRetained = true, OriginalNativePickerButtonsInvoked = true, StoredInputsUnchanged = true,
                    ActualConfiguration = DeviceEvidence(), FinancialImportOrUndoInvoked = false, SystemSettingsChanged = false };
                await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(result), ct); observations.Add(JsonSerializer.SerializeToElement(result));
            }
        }
        finally { local.SetLanguage(originalLanguage); theme.Set(originalTheme); }
        return new { Comparison = comparison, Observations = observations, OriginalStoredInputsUnchanged = before == await StoredAsync(),
            OriginalThemeRestored = theme.Choice == originalTheme, OriginalLanguageRestored = local.CurrentLanguage == originalLanguage,
            NativeFilePickerAdapterAcceptance = false, FinancialImportOrUndoInvoked = false, MainPackageTouched = false };
    }

    /// <summary>Supplies only selected-file effects; original native page and production analysis handle every refusal.</summary>
    private sealed class CsvInputFiles : IDataFileInteraction
    {
        internal PickedDataFile? Selected;
        internal Exception? Refusal;
        public Task<PickedDataFile?> PickCsvAsync(string title) => Refusal is { } error ? Task.FromException<PickedDataFile?>(error) : Task.FromResult(Selected);
        public Task<bool> ConfirmExportAsync(string reason) => throw new ReviewCheckException("Export is outside input review.");
        public Task ShareCsvAsync(string fileName, string title, string content) => throw new ReviewCheckException("Sharing is outside input review.");
    }
}
