#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Data;
using NativeNode = Microsoft.UI.Xaml.DependencyObject;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Original native preset review and Cancel only; independent fictional source, no bank request or financial write.</summary>
    private static async Task ReviewBankCsvAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.bankcsvreview" || Path.GetFullPath(folder) !=
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "bank-csv-native-d297"))) throw new IOException("Exact independent bank CSV review required.");
        var complete = Path.Combine(folder, "complete.json"); if (File.Exists(complete)) throw new IOException("Completed bank cases must not be replayed.");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddZananceData(Path.Combine(folder, "bank-fixture.db")).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
        await store.EnsureDefaultCategoriesAsync();
        if ((await store.GetAccountsAsync()).Count == 0) await store.SaveAccountAsync(new Account
            { Name = "Fictitious original bank / نمونهٔ بانک / Beispielkonto", CurrencyCode = "EUR", Type = AccountType.Checking, OpeningDate = new(2026, 1, 1) });
        await store.GetSettingsAsync();
        var before = await HouseholdFinancialFingerprintAsync(fixture);
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var window = app.Windows[0]; var progress = Path.Combine(folder, "cases.json");
        var finished = File.Exists(progress) ? JsonSerializer.Deserialize<List<string>>(await File.ReadAllTextAsync(progress))! : [];
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            var key = $"{language}-{theme}-{width}"; if (finished.Contains(key)) continue;
            themes.Set(theme); await SetLanguageAsync(app, local, language); window.Width = width; window.Height = 820;
            var effects = new BankLayoutFiles();
            var model = new ImportExportViewModel(store, text, services.GetRequiredService<IDateFormatter>(), local,
                TimeProvider.System, fixture.GetRequiredService<HoldingStore>(), effects, services.GetRequiredService<IAppInteraction>());
            var page = new ImportExportPage(model).WithFlowDirection(local); var root = window.Page ?? throw new IOException();
            await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false);
            try
            {
                for (var wait = 0; wait < 200 && model.Accounts.Count == 0; wait++) await Task.Delay(25);
                if (model.Accounts.Count != 1) throw new IOException("Original target account failed to load.");
                var reads = new List<object>(); var scroll = VisualDescendants(page).OfType<ScrollView>().Single();
                var pick = VisualDescendants(page).OfType<WrappingAction>().Single(a => ReferenceEquals(a.Command, model.PickStarMoneyCommand));
                await ScrollToViewIfNeededAsync(scroll, pick); InvokeChoiceReview(VisualDescendants(pick).OfType<Button>().Single(), reads);
                for (var wait = 0; wait < 400 && model.IsBusy; wait++) await Task.Delay(25);
                if (!model.HasPreview || model.BankLines.Count != 3 || model.CanRunImport || effects.Picks != 1) throw new IOException("Original bank source must require explicit cashflow consent: " + model.ImportError);
                var toggle = VisualDescendants(page).OfType<Switch>().Single(s => SemanticProperties.GetDescription(s) == text["BankCsv_Acknowledge"]);
                await ScrollToViewIfNeededAsync(scroll, toggle);
                if (toggle.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ToggleSwitch nativeToggle ||
                    FrameworkElementAutomationPeer.CreatePeerForElement(nativeToggle)?.GetPattern(PatternInterface.Toggle) is not IToggleProvider toggling)
                    throw new IOException("Original bank consent has no native toggle pattern.");
                toggling.Toggle(); await Task.Delay(60); if (!model.BankAcknowledged || !model.CanRunImport) throw new IOException("Native cashflow consent did not enable the reviewed source.");
                var labels = VisualDescendants(page.FindByName<VerticalStackLayout>("BankPresetChooser"))
                    .Concat(VisualDescendants(page.FindByName<VerticalStackLayout>("BankPresetReview"))).OfType<Label>().Where(Visible).ToArray();
                var index = 0;
                foreach (var label in labels)
                {
                    if (label.Text is not { Length: > 1 }) continue;
                    await ScrollToViewIfNeededAsync(scroll, label); await Task.Delay(25); CheckCaption(label, reads);
                    await CaptureWindowAsync(window, Path.Combine(folder, $"{key}-{index++}.png"));
                }
                foreach (var action in VisualDescendants(page.FindByName<VerticalStackLayout>("BankPresetReview")).OfType<WrappingAction>())
                    if (VisualDescendants(action).OfType<Button>().Single().Handler?.PlatformView is not NativeButton { ActualWidth: >= 44, ActualHeight: >= 44 })
                        throw new IOException("Bank preview action target is smaller than44.");
                var import = VisualDescendants(page).OfType<WrappingAction>().Single(a => ReferenceEquals(a.Command, model.ImportCommand));
                InvokeChoiceReview(VisualDescendants(import).OfType<Button>().Single(), reads);
                if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } xamlRoot }) throw new IOException();
                NativeButton? cancel = null;
                for (var wait = 0; wait < 200 && cancel is null; wait++)
                {
                    cancel = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).SelectMany(p => Nodes(p.Child)).OfType<NativeButton>()
                        .FirstOrDefault(b => FrameworkElementAutomationPeer.CreatePeerForElement(b)?.GetName() == text["Common_Cancel"]);
                    if (cancel is null) await Task.Delay(25);
                }
                if (cancel is null || FrameworkElementAutomationPeer.CreatePeerForElement(cancel)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke) throw new IOException("Native bank-import Cancel unavailable.");
                await CaptureWindowAsync(window, Path.Combine(folder, key + "-confirm-cancel.png")); invoke.Invoke();
                for (var wait = 0; wait < 200 && model.IsBusy; wait++) await Task.Delay(25);
                var after = await HouseholdFinancialFingerprintAsync(fixture);
                await File.WriteAllTextAsync(Path.Combine(folder, key + "-cancel-state.json"), JsonSerializer.Serialize(new { model.CanRunImport, model.IsBusy, model.BankAcknowledged, model.HasPreview, model.ImportError, Before = before, After = after }));
                if (!model.CanRunImport || after != before) throw new IOException("Cancel changed source finance or lost valid consent.");
                finished.Add(key); await File.WriteAllTextAsync(progress, JsonSerializer.Serialize(finished));
                await File.WriteAllTextAsync(Path.Combine(folder, key + "-proof.json"), JsonSerializer.Serialize(new { NativeReads = reads.Count, FinancialHash = before, NativePickPortToggleCancel = true, NativeFilePicker = false }));
            }
            finally { await root.Navigation.PopModalAsync(false); }
        }
        await File.WriteAllTextAsync(complete, JsonSerializer.Serialize(new { Cases = finished.Count, FinancialHash = before, BankNetworkCalls = 0 }));
        static IEnumerable<NativeNode> Nodes(NativeNode node)
        {
            yield return node;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Nodes(VisualTreeHelper.GetChild(node, i))) yield return child;
        }
    }
    private sealed class BankLayoutFiles : IDataFileInteraction
    {
        internal int Picks;
        public Task<PickedDataFile?> PickCsvAsync(string title) => throw new IOException("No generic source belongs to bank layout review.");
        public Task<PickedDataFile?> PickBankCsvAsync(string title, BankCsvEncoding encoding)
        {
            Picks++;
            string[] header = ["BuchDatum", "Betrag", "BtgWaehr", "AgName1", "Storno", "ID", "QkIBAN", "BuchText", .. Enumerable.Range(1, 14).Select(i => "VWZ" + i)];
            var rows = new List<IReadOnlyList<string>> { header };
            for (var i = 0; i < 3; i++)
            {
                var row = header.ToDictionary(h => h, _ => ""); row["BuchDatum"] = "17.10.2026"; row["Betrag"] = i == 0 ? "2345,67" : "12,34-";
                row["BtgWaehr"] = "EUR"; row["AgName1"] = "Fictitious Müller / نمونه"; row["ID"] = (100 + i).ToString(System.Globalization.CultureInfo.InvariantCulture);
                row["QkIBAN"] = "DE00000000000000004321"; row["BuchText"] = "Fictitious source"; row["VWZ1"] = "Fictitious first reference"; row["VWZ14"] = "Fictitious last reference";
                rows.Add(header.Select(h => row[h]).ToArray());
            }
            return Task.FromResult<PickedDataFile?>(new("fictitious-starmoney.csv", Csv.Write(rows, ';')));
        }
        public Task<bool> ConfirmExportAsync(string reason) => throw new IOException("No export belongs to bank layout.");
        public Task ShareCsvAsync(string name, string title, string text) => throw new IOException("No external sharing belongs to bank layout.");
    }
}
#endif
