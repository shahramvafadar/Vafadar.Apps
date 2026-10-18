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
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Interaction;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks original table templates and complete canonical amount viewports without invoking financial commands.</summary>
    private static async Task<object> ReviewFinancialTablesAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct,
        bool legendsOnly = false, bool budgetTitlesOnly = false, bool homeLegendsOnly = false, bool planRowsOnly = false)
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
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Settings = await store.GetSettingsAsync(), Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), Budgets = await store.GetBudgetsAsync() });
        var before = await StoredAsync(); var local = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var observations = new List<object>();
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        {
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            if (themes.Choice != theme)
            {
                app.ShellRebuilt += Rebuilt;
                try { themes.Set(theme); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
                finally { app.ShellRebuilt -= Rebuilt; }
            }
            await RootAsync();
            foreach (var language in new[] { "en", "fa", "de" })
            {
                await SetLanguageAsync(app, local, language, ct); await RootAsync(); var dates = new DateFormatter(local);
                if (planRowsOnly)
                {
                    var home = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store);
                    var homePage = new HomePage(home).WithFlowDirection(local);
                    await Shell.Current.Navigation.PushAsync(homePage);
                    await homePage.PendingPublication;
                    var due = new PlanRow(Guid.NewGuid(), today, "Fictitious rent / Miete / اجاره", "Fictitious account",
                        MoneyText.Format(long.MinValue, "EUR", local.CurrentCulture), Palette.ExpenseText,
                        FluentIcons.Common.Symbol.CalendarClock, Palette.PlanText, Palette.PlanBackground,
                        translator["Plan_Tomorrow"], Palette.PlanText, Palette.PlanBackground)
                    { DayText = "18", MonthText = "Oct", TileText = Palette.PlanText, TileBackground = Palette.PlanBackground, TileStroke = Palette.PlanLine, BadgeStroke = Palette.PlanLine };
                    home.Upcoming.Clear(); home.Upcoming.Add(due); home.HasUpcoming = true;
                    await Task.Delay(250, ct);
                    var homeCount = await CheckAsync(homePage, due.Title, due.AmountText, due.AmountText);
                    CheckPlanTarget(homePage, due, home.OpenOccurrenceCommand);
                    await Shell.Current.Navigation.PopAsync();

                    var plans = ActivatorUtilities.CreateInstance<PlansViewModel>(services, store, fixture.GetRequiredService<PlanStore>());
                    var plansPage = new PlansPage(plans).WithFlowDirection(local);
                    await Shell.Current.Navigation.PushAsync(plansPage);
                    // The original empty-page caption is published after the original appearance read completes.
                    for (var wait = 0; wait < 300; wait++)
                    {
                        if (plansPage.Handler?.PlatformView is Android.Views.View nativePage
                            && NativeTexts(nativePage).Contains(translator["Plans_Empty"])) break;
                        if (wait == 299) throw new ReviewCheckException("Original empty plan publication did not finish.");
                        await Task.Delay(50, ct);
                    }
                    var definition = due with { OriginalDate = null, DayText = null,
                        AmountText = MoneyText.Format(long.MaxValue, "EUR", local.CurrentCulture) };
                    plans.Rows = new[] { definition }; plans.HasNoPlans = false; plans.IsEmpty = false;
                    await Task.Delay(250, ct);
                    var planCount = await CheckAsync(plansPage, definition.Title, definition.AmountText, definition.AmountText);
                    CheckPlanTarget(plansPage, definition, plans.OpenCommand);
                    await Shell.Current.Navigation.PopAsync();
                    if (homeCount != 1 || planCount != 1 || before != await StoredAsync())
                        throw new ReviewCheckException("Original plan-row presentation or stored inputs changed.");
                    observations.Add(new { Language = language, Theme = theme.ToString(), OriginalHomeAndPlansHosts = true,
                        CompleteMonetaryViewports = homeCount + planCount, OriginalCommandsAndTargets = true,
                        DateAndIconVariants = true, ActualNativeAccessibleNames = true, FinancialCommandsInvoked = false });
                    continue;
                }
                if (homeLegendsOnly)
                {
                    var home = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store);
                    var homePage = new HomePage(home).WithFlowDirection(local);
                    await Shell.Current.Navigation.PushAsync(homePage);
                    await homePage.PendingPublication;
                    var original = home.Slices.First();
                    home.Slices[0] = original with
                    {
                        Name = "Food, household groceries and everyday essentials / Lebensmittel und Haushaltsbedarf / خوراک و نیازهای روزانه",
                        AmountText = MoneyText.Format(long.MinValue, "EUR", local.CurrentCulture),
                        PercentText = 1d.ToString("P0", local.CurrentCulture),
                    };
                    home.SliceTotalText = MoneyText.Format(long.MaxValue, "EUR", local.CurrentCulture);
                    var categoryRows = homePage.FindByName<VerticalStackLayout>("CategoryRows");
                    homePage.PreviewCategorySectionForReview(false);
                    if (BindableLayout.GetItemsSource(categoryRows) is not null)
                        throw new ReviewCheckException("Hidden Home category rows remain attached.");
                    homePage.PreviewCategorySectionForReview(true);
                    if (!ReferenceEquals(BindableLayout.GetItemsSource(categoryRows), home.Slices))
                        throw new ReviewCheckException("Visible Home category source is incomplete.");
                    await Task.Delay(250, ct);
                    var amounts = await CheckAsync(homePage, home.Slices[0].Name, home.SliceTotalText, home.Slices[0].AmountText);
                    if (amounts != 2) throw new ReviewCheckException("Both complete Home monetary viewports are required.");
                    var row = Views(homePage).OfType<Grid>().Single(value => ReferenceEquals(value.BindingContext, home.Slices[0]) && value.Children.OfType<Button>().Any());
                    CheckText(row.Children.OfType<Label>().Single(value => value.Text == home.Slices[0].PercentText));
                    var action = row.Children.OfType<Button>().Single();
                    if (!ReferenceEquals(action.Command, home.OpenSliceCommand) || !ReferenceEquals(action.CommandParameter, home.Slices[0])
                        || action.Width < 44 || action.Height < 44 || SemanticProperties.GetDescription(action) != home.Slices[0].Name)
                        throw new ReviewCheckException("Original category target, command or accessible name changed.");
                    if (action.Handler?.PlatformView is not Android.Views.View nativeAction)
                        throw new ReviewCheckException("Original native category action is unavailable.");
                    using var node = nativeAction.CreateAccessibilityNodeInfo();
                    var nativeName = node?.ContentDescription?.ToString() ?? node?.Text?.ToString() ?? string.Empty;
                    if (!nativeName.Contains(home.Slices[0].Name, StringComparison.Ordinal))
                        throw new ReviewCheckException("Native category accessible name is incomplete.");
                    await Shell.Current.Navigation.PopAsync();
                    if (before != await StoredAsync()) throw new ReviewCheckException("Home presentation review changed original stored inputs.");
                    observations.Add(new { Language = language, Theme = theme.ToString(), CompleteNameAndPercent = true,
                        CompleteMonetaryViewports = amounts, ActualNativeAccessibleName = true, OriginalCommandAndTarget = true,
                        HiddenRowsDetached = true, VisibleOriginalSourceComplete = true, LayoutNeverSaved = true });
                    continue;
                }
                if (budgetTitlesOnly)
                {
                    var budget = new BudgetViewModel(store, fixture.GetRequiredService<PlanStore>(), fixture.GetRequiredService<GoalStore>(),
                        translator, dates, local, time, services.GetRequiredService<IAppInteraction>());
                    var budgetPage = new BudgetPage(budget).WithFlowDirection(local);
                    await Shell.Current.Navigation.PushAsync(budgetPage); await budget.LoadAsync();
                    budget.HasBudget = true; budget.IsFlex = true; budget.FlexLines.Clear();
                    foreach (var key in new[] { "Flex_Fixed", "Flex_NonMonthly" })
                        budget.FlexLines.Add(new FlexLine(translator[key], "Fictitious presentation only", FluentIcons.Common.Symbol.CalendarClock,
                            Palette.PlanText, Palette.PlanBackground, Palette.PlanLine));
                    await Task.Delay(250, ct);
                    var outer = Views(budgetPage).OfType<ScrollView>().First(value => value.Orientation != ScrollOrientation.Horizontal);
                    foreach (var line in budget.FlexLines)
                    {
                        var label = Views(budgetPage).OfType<Label>().Single(value => value.Text == line.Title && Visible(value));
                        await outer.ScrollToAsync(label, ScrollToPosition.Center, false); await Task.Delay(100, ct); CheckText(label);
                    }
                    await Shell.Current.Navigation.PopAsync();
                    if (before != await StoredAsync()) throw new ReviewCheckException("Caption-only budget review changed original inputs.");
                    observations.Add(new { Language = language, Theme = theme.ToString(), OriginalFlexCaptions = 2,
                        CompleteText = true, BudgetMethodNeverSaved = true });
                    continue;
                }
                var positive = MoneyText.Format(long.MaxValue, "EUR", local.CurrentCulture); var negative = MoneyText.Format(-long.MaxValue, "EUR", local.CurrentCulture);
                var zero = MoneyText.Format(0, "EUR", local.CurrentCulture); var month = dates.Format(today, DateFormatStyle.MonthYear);
                var reports = new ReportsViewModel(store, fixture.GetRequiredService<PlanStore>(), fixture.GetRequiredService<GoalStore>(),
                    fixture.GetRequiredService<HoldingStore>(), services.GetRequiredService<IBackupService>(),
                    new GoalPresenter(translator, dates, local, fixture.GetRequiredService<HoldingStore>(), services.GetRequiredService<HoldingText>()),
                    translator, dates, local, time, services.GetRequiredService<Security.AppLockService>(), fixture.GetRequiredService<ReportWorkStore>());
                var page = new ReportsPage(reports).WithFlowDirection(local); await Shell.Current.Navigation.PushAsync(page); await reports.LoadAsync();
                reports.IsEmpty = false; reports.CanUseAdvancedReports = true; reports.IsAdvanced = true;
                if (legendsOnly)
                {
                    const string categoryName = "Food, household groceries and everyday essentials / Lebensmittel und Haushaltsbedarf / خوراک و نیازهای روزانه";
                    const string compositionName = "Savings and long-term household reserves / Langfristige Haushaltsruecklagen / پس‌انداز بلندمدت خانواده";
                    reports.CategoryRows.Clear();
                    reports.CategoryRows.Add(new CategoryReportRow([], categoryName, Palette.ExpenseText, positive, zero,
                        negative, Palette.ExpenseText, translator["Report_Gross"] + " " + positive));
                    await Task.Delay(250, ct);
                    var categoryAmounts = await CheckAsync(page, categoryName, positive, negative);
                    reports.PackageIndex = 3; await reports.LoadAsync();
                    reports.CanUseAdvancedReports = true; reports.IsAdvanced = true; reports.HasComposition = true;
                    reports.CompositionRows.Clear(); reports.CompositionRows.Add(new CompositionRow(compositionName, 1, positive, "100 %",
                        Palette.IncomeText, new SolidColorBrush(Palette.IncomeText)));
                    await Task.Delay(250, ct);
                    var compositionAmounts = await CheckAsync(page, compositionName, positive, negative);
                    await Shell.Current.Navigation.PopAsync();
                    if (before != await StoredAsync()) throw new ReviewCheckException("Legend review changed original financial inputs.");
                    observations.Add(new { Language = language, Theme = theme.ToString(), CategoryAmounts = categoryAmounts,
                        CompositionAmounts = compositionAmounts, CompleteTextAndScrollEnds = true, FictitiousPresentationOnly = true });
                    continue;
                }
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
        return new { Comparison = comparison, OriginalReportsAndSnapshotPages = !legendsOnly && !budgetTitlesOnly && !homeLegendsOnly && !planRowsOnly,
            OriginalReportLegends = legendsOnly, OriginalBudgetTitles = budgetTitlesOnly, OriginalHomeLegends = homeLegendsOnly,
            OriginalPlanRows = planRowsOnly,
            OriginalStoredInputsUnchanged = true, NoMainDataTouched = true,
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
        static void CheckPlanTarget(ContentPage page, PlanRow model, System.Windows.Input.ICommand command)
        {
            var row = Views(page).OfType<PlanRowView>().Single(value => ReferenceEquals(value.BindingContext, model));
            var button = ((Grid)row.Content).Children.OfType<Button>().Single();
            if (!ReferenceEquals(button.Command, command) || !ReferenceEquals(button.CommandParameter, model)
                || button.Width < 44 || button.Height < 44 || SemanticProperties.GetDescription(button) != model.Description
                || button.Handler?.PlatformView is not Android.Views.View native)
                throw new ReviewCheckException("Original plan command, target or complete accessible identity changed.");
            using var node = native.CreateAccessibilityNodeInfo();
            var name = node?.ContentDescription?.ToString() ?? node?.Text?.ToString() ?? string.Empty;
            if (!name.Contains(model.Description, StringComparison.Ordinal))
                throw new ReviewCheckException("Original native plan accessible name is incomplete.");
        }
        async Task<int> CheckAsync(ContentPage page, string date, string positive, string negative)
        {
            // The plan fixture has one realized CollectionView item, rather than a vertical ScrollView host.
            var outer = page is PlansPage ? null : Views(page).OfType<ScrollView>().First(value => value.Orientation != ScrollOrientation.Horizontal);
            var identity = Views(page).OfType<Label>().First(label => label.Text == date && Visible(label));
            if (outer is not null) await outer.ScrollToAsync(identity, ScrollToPosition.Start, false);
            await Task.Delay(100, ct); CheckText(identity);
            var amounts = Views(page).OfType<AmountReadout>().Where(value => (value.AmountText == positive || value.AmountText == negative) && Visible(value)).ToArray();
            if (amounts.Length == 0) throw new ReviewCheckException("Original amount viewports are not realized.");
            foreach (var amount in amounts)
            {
                if (outer is not null) await outer.ScrollToAsync(amount, ScrollToPosition.Start, false);
                await Task.Delay(100, ct);
                var caption = Views(amount).OfType<Label>().Single(label => label.Text == amount.AmountText); CheckText(caption);
                var scroll = Views(amount).OfType<ScrollView>().Single();
                if (scroll.Content is not VisualElement content) throw new ReviewCheckException("Original complete amount is missing.");
                var end = Math.Max(0, content.Width - scroll.Width); await scroll.ScrollToAsync(end, 0, false); await Task.Delay(100, ct);
                if (Math.Abs(scroll.ScrollX - end) > 2) throw new ReviewCheckException("Original unit/end cannot be reached.");
                await scroll.ScrollToAsync(0, 0, false); await Task.Delay(100, ct);
                if (scroll.ScrollX > 1) throw new ReviewCheckException("Original sign/start cannot be restored.");
                if (amount.Width > page.Width + 1) throw new ReviewCheckException("Original amount viewport exceeds the page.");
                if ((legendsOnly || homeLegendsOnly || planRowsOnly) && amount.Parent is Grid row)
                {
                    foreach (var button in row.Children.OfType<Button>())
                    {
                        if (button.Handler?.PlatformView is not Android.Views.View target
                            || scroll.Handler?.PlatformView is not Android.Views.View viewport)
                            throw new ReviewCheckException("Original legend target geometry unavailable.");
                        var targetPosition = new int[2]; var viewportPosition = new int[2];
                        target.GetLocationOnScreen(targetPosition); viewport.GetLocationOnScreen(viewportPosition);
                        if (targetPosition[1] + target.Height > viewportPosition[1] + 1)
                            throw new ReviewCheckException("Native category target overlaps the amount viewport.");
                    }
                }
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
