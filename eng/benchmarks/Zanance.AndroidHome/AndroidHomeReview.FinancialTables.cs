using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
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

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks original table templates and complete canonical amount viewports without invoking financial commands.</summary>
    private static async Task<object> ReviewFinancialTablesAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        async Task RootAsync()
        {
            for (var i = 0; i < 300; i++)
            {
                if (Shell.Current?.CurrentPage?.Handler?.PlatformView is Android.Views.View { IsAttachedToWindow: true } native)
                { await WaitLayoutAsync(native, ct); return; }
                await Task.Delay(50, ct);
            }
            throw new ReviewCheckException("Original review Shell did not attach.");
        }
        app.OpenReviewShell(); await RootAsync();
        var path = Path.Combine(root, "financial-tables.db"); var retained = File.Exists(path);
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = fixture.GetRequiredService<ZananceStore>(); var time = services.GetRequiredService<TimeProvider>();
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        if (!retained)
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
                throw new ReviewCheckException("Original failed fixture differs; never recreate it.");
        }
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Settings = await store.GetSettingsAsync(), Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync() });
        var before = await StoredAsync(); var local = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var observations = new List<object>();
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        {
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { themes.Set(theme); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
            await RootAsync();
            foreach (var language in new[] { "en", "fa", "de" })
            {
                await SetLanguageAsync(app, local, language, ct); await RootAsync(); var dates = new DateFormatter(local);
                var positive = MoneyText.Format(long.MaxValue, "EUR", local.CurrentCulture); var negative = MoneyText.Format(-long.MaxValue, "EUR", local.CurrentCulture);
                var zero = MoneyText.Format(0, "EUR", local.CurrentCulture); var month = dates.Format(today, DateFormatStyle.MonthYear);
                var reports = new ReportsViewModel(store, fixture.GetRequiredService<PlanStore>(), fixture.GetRequiredService<GoalStore>(),
                    fixture.GetRequiredService<HoldingStore>(), services.GetRequiredService<IBackupService>(),
                    new GoalPresenter(translator, dates, local, fixture.GetRequiredService<HoldingStore>(), services.GetRequiredService<HoldingText>()),
                    translator, dates, local, time, services.GetRequiredService<Security.AppLockService>(), fixture.GetRequiredService<ReportWorkStore>());
                var page = new ReportsPage(reports).WithFlowDirection(local); await Shell.Current.Navigation.PushAsync(page); await reports.LoadAsync();
                reports.IsEmpty = false; reports.CanUseAdvancedReports = true; reports.IsAdvanced = true;
                reports.Trend.Clear(); reports.Trend.Add(new TrendPoint(month, 1, 0, positive, zero, positive, Palette.IncomeText));
                reports.Trend.Add(new TrendPoint(month + " *", 0, 1, zero, positive, negative, Palette.ExpenseText));
                await Task.Delay(250, ct); var trend = await CheckAsync(page, month, positive, negative);
                reports.PackageIndex = 4; await reports.LoadAsync(); reports.CanUseAdvancedReports = true; reports.IsAdvanced = true; reports.HasHistory = true;
                reports.HistoryRows.Clear(); reports.HistoryRows.Add(new WealthHistoryRow(month, 1, positive, false));
                await Task.Delay(250, ct); var history = await CheckAsync(page, month, positive, negative); await Shell.Current.Navigation.PopAsync();
                var snapshot = new SnapshotViewModel(store, translator, dates, local, time, fixture.GetRequiredService<ForecastWorkStore>());
                var saved = new SnapshotPage(snapshot).WithFlowDirection(local); await Shell.Current.Navigation.PushAsync(saved); await snapshot.LoadAsync();
                snapshot.NotFound = false; snapshot.CanCompare = true; snapshot.NotStarted = false; snapshot.Name = "Fictitious financial table";
                snapshot.Rows.Clear(); snapshot.Rows.Add(new ComparisonRow(dates.Format(today), zero, negative, negative, Palette.ExpenseText));
                snapshot.Parts.Clear(); snapshot.Parts.Add(new AmountLine(translator["Snapshot_UnplannedSpending"], negative, true, true));
                await Task.Delay(250, ct); var comparisonCount = await CheckAsync(saved, dates.Format(today), positive, negative); await Shell.Current.Navigation.PopAsync();
                if (before != await StoredAsync()) throw new ReviewCheckException("Presentation-only review changed original financial inputs.");
                observations.Add(new { Language = language, Theme = theme.ToString(), TrendAmounts = trend, HistoryAmounts = history, ComparisonAmounts = comparisonCount, CompleteTextAndScrollEnds = true });
            }
        }
        return new { Comparison = comparison, OriginalReportsAndSnapshotPages = true, OriginalStoredInputsUnchanged = true, NoMainDataTouched = true,
            NoFinancialSaveOrSystemSettings = true, ActualPhoneAcceptance = false, Observations = observations };

        static IEnumerable<Microsoft.Maui.Controls.View> Views(IVisualTreeElement node)
        {
            if (node is Microsoft.Maui.Controls.View view) yield return view;
            foreach (var child in node.GetVisualChildren()) foreach (var childView in Views(child)) yield return childView;
        }
        static bool Visible(VisualElement value)
        {
            for (Element? node = value; node is not null; node = node.Parent) if (node is VisualElement { IsVisible: false }) return false;
            return true;
        }
        async Task<int> CheckAsync(ContentPage page, string date, string positive, string negative)
        {
            var outer = Views(page).OfType<ScrollView>().First(value => value.Orientation != ScrollOrientation.Horizontal);
            var identity = Views(page).OfType<Label>().First(label => label.Text == date && Visible(label));
            await outer.ScrollToAsync(identity, ScrollToPosition.Start, false); await Task.Delay(100, ct); CheckText(identity);
            var amounts = Views(page).OfType<AmountReadout>().Where(value => (value.AmountText == positive || value.AmountText == negative) && Visible(value)).ToArray();
            if (amounts.Length == 0) throw new ReviewCheckException("Original amount viewports are not realized.");
            foreach (var amount in amounts)
            {
                await outer.ScrollToAsync(amount, ScrollToPosition.Start, false); await Task.Delay(100, ct);
                var caption = Views(amount).OfType<Label>().Single(label => label.Text == amount.AmountText); CheckText(caption);
                var scroll = Views(amount).OfType<ScrollView>().Single();
                if (scroll.Content is not VisualElement content) throw new ReviewCheckException("Original complete amount is missing.");
                var end = Math.Max(0, content.Width - scroll.Width); await scroll.ScrollToAsync(end, 0, false); await Task.Delay(100, ct);
                if (Math.Abs(scroll.ScrollX - end) > 2) throw new ReviewCheckException("Original unit/end cannot be reached.");
                await scroll.ScrollToAsync(0, 0, false); await Task.Delay(100, ct);
                if (scroll.ScrollX > 1) throw new ReviewCheckException("Original sign/start cannot be restored.");
                if (amount.Width > page.Width + 1) throw new ReviewCheckException("Original amount viewport exceeds the page.");
            }
            return amounts.Length;
        }
        static void CheckText(Label label)
        {
            if (label.Handler?.PlatformView is not Android.Widget.TextView native || native.Layout is not { } layout ||
                layout.Height > native.Height - native.TotalPaddingTop - native.TotalPaddingBottom + 1)
                throw new ReviewCheckException("Original financial caption has no complete native allocation.");
            for (var line = 0; line < layout.LineCount; line++)
                if (layout.GetEllipsisCount(line) != 0 || layout.GetLineRight(line) > native.Width - native.TotalPaddingRight + 1 || layout.GetLineLeft(line) < -1)
                    throw new ReviewCheckException("Original financial caption is truncated.");
        }
    }
}
