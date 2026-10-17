#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // D-331: real page and native paging, with an independent source and no Import/Undo invocation.
    private static async Task ReviewImportPreviewAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.importpreview")
            throw new IOException("Independent import preview identity required.");
        var path = Path.Combine(folder, "import-preview.db");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ICommercialWriteAccessSource>(new EditorReadinessAccess()).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var store = fixture.GetRequiredService<ZananceStore>();
        var holdings = fixture.GetRequiredService<HoldingStore>();
        var day = new DateOnly(2026, 10, 18);
        var existing = await store.GetAccountsAsync();
        var account = existing.SingleOrDefault(row => row.Name == "Fictitious reviewed euro account")
            ?? new Account { Name = "Fictitious reviewed euro account", CurrencyCode = "EUR", OpeningDate = day };
        var destination = existing.SingleOrDefault(row => row.Name == "Fictitious dollar destination")
            ?? new Account { Name = "Fictitious dollar destination", CurrencyCode = "USD", OpeningDate = day };
        if (existing.Count == 0) { await store.SaveAccountAsync(account); await store.SaveAccountAsync(destination); }
        await ApplyOwnedReviewModeAsync(store);
        var entries = Enumerable.Range(1, 12).Select(index => new LedgerEntry
        {
            AccountId = account.Id, Date = day, Kind = EntryKind.Expense, Amount = index * 123,
            Title = "Fictitious interpreted source row " + index
        }).ToArray();
        entries[0].Kind = EntryKind.Transfer; entries[0].ToAccountId = destination.Id; entries[0].ToAmount = 456;
        var csv = Csv.Read(CsvExport.Write(entries, new Dictionary<Guid, Account>
            { [account.Id] = account, [destination.Id] = destination }, _ => "", true), ',').Select(row => row.ToArray()).ToList();
        var invalid = csv[1].ToArray(); invalid[0] = "invalid-fictitious-id"; csv.Add(invalid);
        var effects = new DataFileReviewEffects { Picked = new("fictitious-reviewed.csv", Csv.Write(csv)) };
        var translator = services.GetRequiredService<Translator>();
        var localization = services.GetRequiredService<ILocalizationService>();
        foreach (var language in languages)
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            services.GetRequiredService<ThemeService>().Set(theme);
            app.Windows[0].Width = width; app.Windows[0].Height = 820;
            var context = language + "-" + theme + "-" + width;
            if (File.Exists(Path.Combine(folder, context + "-import-preview-proof.json"))) continue;
            await SetLanguageAsync(app, localization, language);
            var vm = new ImportExportViewModel(store, translator, new DateFormatter(localization), localization,
                TimeProvider.System, holdings, effects, new EditorWriteReviewInteraction());
            await vm.LoadAsync();
            var page = new ImportExportPage(vm).WithFlowDirection(localization); ProfileNativeReads.Add(page);
            var root = app.Windows[0].Page ?? throw new IOException("Missing original review root.");
            await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(localization), false); await Task.Delay(200);
            // A modal NavigationPage is not itself a ContentPage; inspect the original page's actual viewport.
            var scroll = VisualDescendants(page).OfType<ScrollView>().Single(view => view.Orientation == ScrollOrientation.Vertical);
            var pick = VisualDescendants(page).OfType<Button>().Single(button => button.Command == vm.PickCommand);
            await ScrollToViewIfNeededAsync(scroll, pick); InvokeChoiceReview(pick, ProfileNativeReads);
            if (vm.PickCommand.ExecutionTask is { } picking) await picking;
            await Task.Delay(300);
            if (vm.LedgerPreviewRows.Count != 10 || vm.LedgerPreviewRows[0].DestinationAmount is null)
                throw new InvalidOperationException("The real source/destination interpretation is missing.");
            async Task InspectAsync(string suffix)
            {
                await CaptureAsync(app, folder, context + "-before-inspection-" + suffix);
                foreach (var row in vm.LedgerPreviewRows)
                {
                    foreach (var text in new[] { row.Heading, row.Title, row.Accounts, row.Status }.Where(value => value.Length > 0))
                    {
                        var label = VisualDescendants(page).OfType<Label>().First(item => Visible(item) && item.Text == text);
                        await ScrollToViewIfNeededAsync(scroll, label);
                        if (label.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock native)
                            await File.AppendAllTextAsync(Path.Combine(folder, "native-labels.jsonl"), JsonSerializer.Serialize(new
                            { context, label.Text, label.Width, label.Height, native.ActualWidth, native.ActualHeight,
                                native.IsTextTrimmed, native.IsTextScaleFactorEnabled, Visible = Visible(label) }) + Environment.NewLine);
                        CheckCaption(label, ProfileNativeReads);
                    }
                }
                var first = VisualDescendants(page).OfType<Label>().First(item => item.Text == vm.LedgerPreviewRows[0].Heading);
                await ScrollToViewIfNeededAsync(scroll, first);
                await CaptureAsync(app, folder, context + "-import-preview-" + suffix);
            }
            await InspectAsync("first");
            var next = WrappingActions(page).Single(item => item.Command == vm.NextLedgerPageCommand);
            await ScrollToViewIfNeededAsync(scroll, next);
            InvokeChoiceReview(VisualDescendants(next).OfType<Button>().Single(), ProfileNativeReads); await Task.Delay(300);
            if (vm.LedgerPreviewRows.Count != 3 || vm.LedgerPreviewRows[^1].Amount is not null || vm.CanShowNextLedgerPage)
                throw new InvalidOperationException("The final original invalid row is unreachable.");
            await InspectAsync("last");
            var previous = WrappingActions(page).Single(item => item.Command == vm.PreviousLedgerPageCommand);
            await ScrollToViewIfNeededAsync(scroll, previous);
            InvokeChoiceReview(VisualDescendants(previous).OfType<Button>().Single(), ProfileNativeReads); await Task.Delay(200);
            if (vm.LedgerPreviewRows[0].SourceLine != 2 || (await store.GetEntriesAsync()).Count != 0)
                throw new InvalidOperationException("Native paging changed the original financial store.");
            await root.Navigation.PopModalAsync(false);
            await File.WriteAllTextAsync(Path.Combine(folder, context + "-import-preview-proof.json"), JsonSerializer.Serialize(new
            {
                OriginalPageNativePickCommandAndPaging = true, OriginalThirteenSourceRowsReachable = true,
                CompleteNativeScaledIdentityAndStatus = true, SeparateDestinationCurrency = true,
                FinalInvalidRowReachable = true, NoFinancialImportOrUndoInvoked = true, StoredEntryCount = 0
            }));
        }
    }
}
#endif
