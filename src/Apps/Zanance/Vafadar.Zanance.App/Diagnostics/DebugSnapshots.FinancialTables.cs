#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Maui.Localization;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Forecast;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Features.Reports;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using NativeText = Microsoft.UI.Xaml.Controls.TextBlock;
using NativeScroll = Microsoft.UI.Xaml.Controls.ScrollViewer;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks original financial tables using fictitious display packets, native scroll patterns and an independently owned unchanged ledger.</summary>
    private static async Task ReviewFinancialTablesAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.financialtables" || !Path.GetFullPath(folder).StartsWith(
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "financial-tables-d311")), StringComparison.OrdinalIgnoreCase))
            throw new IOException("Explicit independent financial table review required.");
        var beforeOnly = Environment.GetEnvironmentVariable("VAFADAR_TABLE_BEFORE") == "1";
        var templatePaths = new[] { "src/Apps/Zanance/Vafadar.Zanance.App/Features/Reports/ReportsPage.xaml", "src/Apps/Zanance/Vafadar.Zanance.App/Features/Forecast/SnapshotPage.xaml" };
        var hashes = JsonSerializer.Serialize(templatePaths.Select(name => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(name)))));
        var manifest = Path.Combine(folder, "template-hashes.json");
        if (File.Exists(manifest) && await File.ReadAllTextAsync(manifest) != hashes) throw new IOException("Original templates changed; use a separately identified qualification directory.");
        if (!File.Exists(manifest)) await File.WriteAllTextAsync(manifest, hashes);
        if (File.Exists(Path.Combine(folder, "financial-tables.complete.json"))) throw new IOException("Do not repeat a completed review.");
        var path = Path.Combine(folder, "financial-tables.db"); var resuming = File.Exists(path);
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider(); fixture.MigrateLocalDatabase<ZananceDbContext>();
        var store = fixture.GetRequiredService<ZananceStore>(); var time = services.GetRequiredService<TimeProvider>();
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        if (!resuming)
        {
            var account = new Account { Name = "Fictitious financial table account", CurrencyCode = "EUR", OpeningDate = today, OpeningBalance = 10000 };
            await store.SaveAccountAsync(account);
            await store.SaveEntryAsync(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1234, Date = today });
        }
        else
        {
            var accounts = await store.GetAccountsAsync(); var entries = await store.GetEntriesAsync();
            if (accounts.Count != 1 || entries.Count != 1 || accounts[0].Name != "Fictitious financial table account" || accounts[0].OpeningBalance != 10000
                || entries[0].AccountId != accounts[0].Id || entries[0].Amount != 1234 || entries[0].Kind != EntryKind.Expense)
                throw new IOException("Original failed fixture differs; never recreate its financial inputs.");
        }
        var original = await HouseholdFinancialFingerprintAsync(fixture);
        var local = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        var dates = services.GetRequiredService<IDateFormatter>(); var themes = services.GetRequiredService<ThemeService>(); var window = app.Windows[0];
        var progress = Path.Combine(folder, "financial-tables-contexts.json");
        var observations = File.Exists(progress) ? JsonSerializer.Deserialize<List<JsonElement>>(await File.ReadAllTextAsync(progress)) ?? throw new IOException("Original context progress is invalid.") : new List<JsonElement>();
        foreach (var language in beforeOnly ? new[] { "de" } : new[] { "en", "fa", "de" })
        foreach (var theme in beforeOnly ? new[] { ThemeChoice.Light } : new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in beforeOnly ? new[] { 360 } : new[] { 360, 412, 1280 })
        {
            if (observations.Any(row => row.GetProperty("Language").GetString() == language && row.GetProperty("Theme").GetString() == theme.ToString() && row.GetProperty("Width").GetInt32() == width)) continue;
            themes.Set(theme); await SetLanguageAsync(app, local, language); window.Width = width; window.Height = 820;
            var positive = MoneyText.Format(long.MaxValue, "EUR", local.CurrentCulture);
            var negative = MoneyText.Format(-long.MaxValue, "EUR", local.CurrentCulture);
            var zero = MoneyText.Format(0, "EUR", local.CurrentCulture);
            var date = dates.Format(today, DateFormatStyle.MonthYear);
            var reports = new ReportsViewModel(store, fixture.GetRequiredService<PlanStore>(), fixture.GetRequiredService<GoalStore>(),
                fixture.GetRequiredService<HoldingStore>(), services.GetRequiredService<IBackupService>(),
                new GoalPresenter(translator, dates, local, fixture.GetRequiredService<HoldingStore>(), services.GetRequiredService<HoldingText>()),
                translator, dates, local, time, services.GetRequiredService<Security.AppLockService>(), fixture.GetRequiredService<ReportWorkStore>());
            var page = new ReportsPage(reports).WithFlowDirection(local); var root = window.Page ?? throw new IOException("Original review root missing.");
            await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false); await reports.LoadAsync();
            reports.IsEmpty = false; reports.CanUseAdvancedReports = true; reports.IsAdvanced = true;
            reports.Trend.Clear(); reports.Trend.Add(new TrendPoint(date, 1, 0, positive, zero, positive, Palette.IncomeText));
            reports.Trend.Add(new TrendPoint(date + " *", 0, 1, zero, positive, negative, Palette.ExpenseText));
            await Task.Delay(250);
            await CheckAsync(page, date, positive, negative, "trend");
            if (beforeOnly) throw new IOException("The original financial table unexpectedly passed; inspect before changing it.");
            reports.PackageIndex = 4; await reports.LoadAsync(); reports.CanUseAdvancedReports = true; reports.IsAdvanced = true; reports.HasHistory = true;
            reports.HistoryRows.Clear(); reports.HistoryRows.Add(new WealthHistoryRow(date, 1, positive, false));
            await Task.Delay(250); await CheckAsync(page, date, positive, null, "history");
            await root.Navigation.PopModalAsync(false);
            var snapshot = new SnapshotViewModel(store, translator, dates, local, time, fixture.GetRequiredService<ForecastWorkStore>());
            var saved = new SnapshotPage(snapshot).WithFlowDirection(local); await root.Navigation.PushModalAsync(new NavigationPage(saved).WithFlowDirection(local), false); await snapshot.LoadAsync();
            snapshot.NotFound = false; snapshot.CanCompare = true; snapshot.NotStarted = false; snapshot.Name = "Fictitious financial table";
            snapshot.Rows.Clear(); snapshot.Rows.Add(new ComparisonRow(dates.Format(today), zero, negative, negative, Palette.ExpenseText));
            snapshot.Parts.Clear(); snapshot.Parts.Add(new AmountLine(translator["Snapshot_UnplannedSpending"], negative, true, true));
            await Task.Delay(250); await CheckAsync(saved, dates.Format(today), positive, negative, "comparison");
            await root.Navigation.PopModalAsync(false);
            if (original != await HouseholdFinancialFingerprintAsync(fixture)) throw new IOException("Presentation-only review changed complete original data.");
            observations.Add(JsonSerializer.SerializeToElement(new { Language = language, Theme = theme.ToString(), Width = width, OriginalStoredDataUnchanged = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "financial-tables-contexts.json"), JsonSerializer.Serialize(observations));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "financial-tables.complete.json"), JsonSerializer.Serialize(new { Contexts = observations.Count, OriginalStoredDataUnchanged = true, NoSaveOrSystemChange = true }));

        static bool Visible(VisualElement value)
        {
            for (Element? element = value; element is not null; element = element.Parent)
                if (element is VisualElement { IsVisible: false }) return false;
            return true;
        }

        async Task CheckAsync(ContentPage page, string date, string positive, string? negative, string kind)
        {
            var packetPath = Path.Combine(folder, $"{local.CurrentCulture.Name}-{Application.Current!.UserAppTheme}-{window.Width}-{kind}.json");
            // A fixture-only continuation keeps already passed unchanged templates, never another production revision.
            if (!beforeOnly && File.Exists(packetPath)) return;
            var outer = VisualDescendants(page).OfType<ScrollView>().First(value => value.Orientation != ScrollOrientation.Horizontal);
            var identity = VisualDescendants(page).OfType<Label>().First(label => label.Text == date && Visible(label));
            await ScrollToViewIfNeededAsync(outer, identity); await Task.Delay(100);
            if (beforeOnly) await CaptureWindowAsync(window, Path.Combine(folder, kind + "-original.png"));
            CheckCaption(identity);
            var values = VisualDescendants(page).OfType<Label>().Where(label => (label.Text == positive || label.Text == negative) && Visible(label)).ToArray();
            if (values.Length == 0) throw new IOException("Original financial values are not realized.");
            foreach (var value in values)
            {
                await ScrollToViewIfNeededAsync(outer, value);
                if (value.Parent is ScrollView { Orientation: ScrollOrientation.Horizontal } scroll)
                {
                    if (scroll.Handler?.PlatformView is not NativeScroll native) throw new IOException("Missing real amount viewport.");
                    CheckCaption(value);
                    if (page.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement pageRoot) throw new IOException("Missing actual page viewport.");
                    var transform = native.TransformToVisual(pageRoot);
                    var viewportOrigin = transform.TransformPoint(new(0, 0));
                    var viewportEnd = transform.TransformPoint(new(native.ActualWidth, 0));
                    // A mirrored native coordinate system reverses the two corners; compare the actual interval.
                    var left = Math.Min(viewportOrigin.X, viewportEnd.X); var right = Math.Max(viewportOrigin.X, viewportEnd.X);
                    await File.WriteAllTextAsync(Path.Combine(folder, $"{local.CurrentCulture.Name}-{Application.Current!.UserAppTheme}-{window.Width}-{kind}-viewport.json"),
                        JsonSerializer.Serialize(new { FirstCorner = viewportOrigin.X, SecondCorner = viewportEnd.X, Left = left, Right = right, native.ViewportWidth, PageWidth = pageRoot.ActualWidth }));
                    if (native.ViewportWidth <= 0 || left < -1 || right > pageRoot.ActualWidth + 1)
                        throw new IOException("Original amount viewport exceeds the actual page width.");
                    var peer = new ScrollViewerAutomationPeer(native);
                    if (native.ScrollableWidth > 1)
                    {
                        if (peer.GetPattern(PatternInterface.Scroll) is not IScrollProvider pattern) throw new IOException("Oversized original value has no native scroll pattern.");
                        pattern.SetScrollPercent(100, -1); await Task.Delay(50);
                        if (native.HorizontalOffset < native.ScrollableWidth - 1) throw new IOException("Original unit/end cannot be reached.");
                        pattern.SetScrollPercent(0, -1); await Task.Delay(50);
                        if (native.HorizontalOffset > 1) throw new IOException("Original sign/start cannot be restored.");
                    }
                }
                else if (value.Handler?.PlatformView is NativeText native && page.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement root)
                {
                    var origin = native.TransformToVisual(root).TransformPoint(new(0, 0));
                    var proof = new { Kind = kind, value.Text, native.ActualWidth, native.ActualHeight, Left = origin.X, Right = origin.X + native.ActualWidth, PageWidth = root.ActualWidth };
                    await File.WriteAllTextAsync(Path.Combine(folder, kind + "-original-observation.json"), JsonSerializer.Serialize(proof));
                    CheckCaption(value);
                    if (origin.X < -1 || origin.X + native.ActualWidth > root.ActualWidth + 1) throw new IOException("Original monetary caption exceeds the actual page viewport.");
                }
                else throw new IOException("Missing original native monetary caption.");
            }
            var packet = new { Kind = kind, Identity = date, CanonicalAmounts = values.Select(value => value.Text).ToArray(), CompleteGlyphsAndOriginalPackets = true,
                OriginalNativeAmountViewports = values.Count(value => value.Parent is ScrollView), BothNativeScrollEndsChecked = true };
            await File.WriteAllTextAsync(Path.Combine(folder, $"{local.CurrentCulture.Name}-{Application.Current!.UserAppTheme}-{window.Width}-{kind}.json"), JsonSerializer.Serialize(packet));
            await CaptureWindowAsync(window, Path.Combine(folder, $"{local.CurrentCulture.Name}-{Application.Current!.UserAppTheme}-{window.Width}-{kind}.png"));
        }
    }
}
#endif
