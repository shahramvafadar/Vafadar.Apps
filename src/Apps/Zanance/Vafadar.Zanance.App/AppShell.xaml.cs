using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Backup;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Features.Categories;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.App.Features.Settings;

namespace Vafadar.Zanance.App;

public partial class AppShell : Shell
{
    private readonly HashSet<Features.Home.HomePage> _publishedHomes = [];
    private bool _navigationRetired;

    /// <summary>Records actual published Home instances; lazy ShellContent.Content may not expose its cached page.</summary>
    internal void RegisterHomePublication(Features.Home.HomePage home)
    {
        if (_navigationRetired) home.RetirePublication();
        else _publishedHomes.Add(home);
    }
    /// <summary>Route of the account list.</summary>
    public const string AccountsRoute = AppRoutes.AccountsRoute;

    /// <summary>Route of the account editor (query: <c>id</c> for an existing account).</summary>
    public const string AccountEditorRoute = AppRoutes.AccountEditorRoute;

    /// <summary>Route of the account details and reconciliation (query: <c>id</c>).</summary>
    public const string AccountDetailRoute = AppRoutes.AccountDetailRoute;

    /// <summary>Route of the settings page.</summary>
    public const string SettingsRoute = AppRoutes.SettingsRoute;

    /// <summary>Route of the entry editor (query: <c>id</c>, <c>duplicate</c>, <c>refundOf</c> or <c>kind</c>).</summary>
    public const string EntryEditorRoute = AppRoutes.EntryEditorRoute;

    /// <summary>Route of the entry details (query: <c>id</c>).</summary>
    public const string EntryDetailRoute = AppRoutes.EntryDetailRoute;

    /// <summary>Route of the category list.</summary>
    public const string CategoriesRoute = AppRoutes.CategoriesRoute;

    /// <summary>Route of the category editor (query: <c>id</c> for an existing category).</summary>
    public const string CategoryEditorRoute = AppRoutes.CategoryEditorRoute;

    /// <summary>Route of the plan editor (query: <c>id</c> for an existing plan).</summary>
    public const string PlanEditorRoute = AppRoutes.PlanEditorRoute;

    /// <summary>Route of the plan details (query: <c>id</c>).</summary>
    public const string PlanDetailRoute = AppRoutes.PlanDetailRoute;

    /// <summary>Route of an occurrence (query: <c>plan</c> and <c>date</c>, the original date).</summary>
    public const string OccurrenceRoute = AppRoutes.OccurrenceRoute;

    /// <summary>Route of backup and restore.</summary>
    public const string BackupRoute = AppRoutes.BackupRoute;

    /// <summary>Route of the monthly budget (a top tab of Insights).</summary>
    public const string BudgetRoute = AppRoutes.BudgetRoute;

    /// <summary>Route of the budget editor (query: <c>year</c>, <c>month</c>, <c>calendar</c>, <c>currency</c>).</summary>
    public const string BudgetEditorRoute = AppRoutes.BudgetEditorRoute;

    /// <summary>Route of the reports (a top tab of Insights).</summary>
    public const string ReportsRoute = AppRoutes.ReportsRoute;

    /// <summary>Route of the forecast (a top tab of Insights).</summary>
    public const string ForecastRoute = AppRoutes.ForecastRoute;

    /// <summary>Route of the manual exchange rates.</summary>
    public const string RatesRoute = AppRoutes.RatesRoute;

    /// <summary>Route of the Home customisation (§21.5).</summary>
    public const string HomeLayoutRoute = AppRoutes.HomeLayoutRoute;

    /// <summary>Route of the display units such as the toman (FX-07).</summary>
    public const string DisplayUnitsRoute = AppRoutes.DisplayUnitsRoute;

    /// <summary>Route of the local profiles (§3).</summary>
    public const string ProfilesRoute = AppRoutes.ProfilesRoute;

    /// <summary>Route of About Zanance (D-39).</summary>
    public const string AboutRoute = AppRoutes.AboutRoute;

    /// <summary>Route of the full third-party notices.</summary>
    public const string NoticesRoute = AppRoutes.NoticesRoute;

    /// <summary>Route of CSV import and export.</summary>
    public const string ImportExportRoute = AppRoutes.ImportExportRoute;

    /// <summary>Route of the quick templates (TX-04).</summary>
    public const string TemplatesRoute = AppRoutes.TemplatesRoute;

    /// <summary>Route of the savings goals (F2-GOAL; a top tab of Insights).</summary>
    public const string GoalsRoute = AppRoutes.GoalsRoute;

    /// <summary>Route of the goal editor.</summary>
    public const string GoalEditorRoute = AppRoutes.GoalEditorRoute;

    /// <summary>Route of one goal.</summary>
    public const string GoalDetailRoute = AppRoutes.GoalDetailRoute;

    /// <summary>Route of the quantity holdings (ZEX phase 3; More &gt; Money).</summary>
    public const string HoldingsRoute = AppRoutes.HoldingsRoute;

    /// <summary>Route of one asset type.</summary>
    public const string HoldingDetailRoute = AppRoutes.HoldingDetailRoute;

    /// <summary>Route of the asset type editor.</summary>
    public const string AssetTypeEditorRoute = AppRoutes.AssetTypeEditorRoute;

    /// <summary>Route of the editor of one holding event (purchase, sale, move, correction ...).</summary>
    public const string AssetEventEditorRoute = AppRoutes.AssetEventEditorRoute;

    /// <summary>Route of the explanation sheet of a KPI (ZEX-UI14).</summary>
    public const string KpiSheetRoute = AppRoutes.KpiSheetRoute;

    /// <summary>Route of the period-end review (ZEX-S0610).</summary>
    public const string ReviewRoute = AppRoutes.ReviewRoute;

    /// <summary>Route of a saved forecast compared with reality (ZEX-S0804).</summary>
    public const string SnapshotRoute = AppRoutes.SnapshotRoute;

    /// <summary>Route of the split editor (F2-TX-01).</summary>
    public const string SplitRoute = AppRoutes.SplitRoute;

    /// <summary>Route of the estimated repayment schedule of a loan (F2-DEBT-02).</summary>
    public const string LoanScheduleRoute = AppRoutes.LoanScheduleRoute;

    /// <summary>Route of the open reimbursements (F2-TX-03).</summary>
    public const string ReimbursementsRoute = AppRoutes.ReimbursementsRoute;

    /// <summary>Route of the categorization rules (F2-TX-04).</summary>
    public const string RulesRoute = AppRoutes.RulesRoute;

    /// <summary>Route of the final settlement of advance payments (F2-CON-04).</summary>
    public const string SettlementRoute = AppRoutes.SettlementRoute;

    public AppShell()
    {
        InitializeComponent();
        Navigated += OnRetainedDetailsTabActivated;
#if WINDOWS
        // Growing child headers and visible Insights navigation keep their own Auto row above the retained body.
        // Shell's Windows TitleView clips large captions; its Insights drop-down also hides destinations (D-83, D-85).
        Navigated += (_, _) =>
        {
            // D-375: Home already owns its growing title/Add row; never wrap it in a second header.
            if (CurrentPage is not { } page || page is Features.Home.HomePage or Features.Plans.PlansPage || GetTitleView(page) is not null)
            {
                return;
            }

            if (Navigation.NavigationStack.Count > 1 && page is ContentPage contentPage)
            {
                Presentation.PageHeader.Attach(contentPage, FlowDirection == FlowDirection.RightToLeft);
            }
            else if (page is ContentPage insightsPage && Presentation.InsightsTabs.RouteOf(page) is { } route)
            {
                Presentation.InsightsTabs.Attach(insightsPage, route);
            }
        };
#endif
        Routing.RegisterRoute(AccountsRoute, typeof(AccountsPage));
        Routing.RegisterRoute(AccountEditorRoute, typeof(AccountEditorPage));
        Routing.RegisterRoute(AccountDetailRoute, typeof(AccountDetailPage));
        Routing.RegisterRoute(SettingsRoute, typeof(SettingsPage));
        Routing.RegisterRoute(AppRoutes.ActiveItemsRoute, typeof(ActiveItemsPage));
        Routing.RegisterRoute(AppRoutes.ProductPlansRoute, typeof(ProductPlansPage));
        Routing.RegisterRoute(AppRoutes.BillingIdentityRoute, typeof(Features.Commerce.Billing.BillingIdentityPage));
        Routing.RegisterRoute(EntryEditorRoute, typeof(EntryEditorPage));
        Routing.RegisterRoute(EntryDetailRoute, typeof(EntryDetailPage));
        Routing.RegisterRoute(CategoriesRoute, typeof(CategoriesPage));
        Routing.RegisterRoute(CategoryEditorRoute, typeof(CategoryEditorPage));
        Routing.RegisterRoute(PlanEditorRoute, typeof(PlanEditorPage));
        Routing.RegisterRoute(PlanDetailRoute, typeof(PlanDetailPage));
        Routing.RegisterRoute(OccurrenceRoute, typeof(OccurrencePage));
        Routing.RegisterRoute(BackupRoute, typeof(BackupPage));
        Routing.RegisterRoute(AppRoutes.PersonalSyncRoute, typeof(Features.Sync.PersonalSyncPage));
        Routing.RegisterRoute(AppRoutes.HouseholdRoute, typeof(Features.Sharing.HouseholdPage));
        Routing.RegisterRoute(AppRoutes.HouseholdCostsRoute, typeof(Features.Sharing.HouseholdCostsPage));
        Routing.RegisterRoute(BudgetEditorRoute, typeof(BudgetEditorPage));
        Routing.RegisterRoute(RatesRoute, typeof(RatesPage));
        Routing.RegisterRoute(AppRoutes.MarketReferenceRoute, typeof(Features.Rates.MarketReferencePage));
        Routing.RegisterRoute(HomeLayoutRoute, typeof(Features.Home.HomeLayoutPage));
        Routing.RegisterRoute(DisplayUnitsRoute, typeof(DisplayUnitsPage));
        Routing.RegisterRoute(ProfilesRoute, typeof(Features.Profiles.ProfilesPage));
        Routing.RegisterRoute(AboutRoute, typeof(Features.About.AboutPage));
        Routing.RegisterRoute(NoticesRoute, typeof(Features.About.NoticesPage));
        Routing.RegisterRoute(ImportExportRoute, typeof(ImportExportPage));
        Routing.RegisterRoute(AppRoutes.AccountantExportRoute, typeof(AccountantExportPage));
        Routing.RegisterRoute(AppRoutes.AnalysisRoute, typeof(Features.Analysis.AiAnalysisPage));
        Routing.RegisterRoute(TemplatesRoute, typeof(Features.Templates.TemplatesPage));
        Routing.RegisterRoute(GoalEditorRoute, typeof(Features.Goals.GoalEditorPage));
        Routing.RegisterRoute(GoalDetailRoute, typeof(Features.Goals.GoalDetailPage));
        Routing.RegisterRoute(HoldingsRoute, typeof(Features.Holdings.HoldingsPage));
        Routing.RegisterRoute(HoldingDetailRoute, typeof(Features.Holdings.HoldingDetailPage));
        Routing.RegisterRoute(AssetTypeEditorRoute, typeof(Features.Holdings.AssetTypeEditorPage));
        Routing.RegisterRoute(AssetEventEditorRoute, typeof(Features.Holdings.AssetEventEditorPage));
        Routing.RegisterRoute(KpiSheetRoute, typeof(Features.Reports.KpiSheetPage));
        Routing.RegisterRoute(ReviewRoute, typeof(Features.Reports.PeriodReviewPage));
        Routing.RegisterRoute(SnapshotRoute, typeof(Features.Forecast.SnapshotPage));
        Routing.RegisterRoute(SplitRoute, typeof(SplitEditorPage));
        Routing.RegisterRoute(ReimbursementsRoute, typeof(ReimbursementsPage));
        Routing.RegisterRoute(LoanScheduleRoute, typeof(LoanSchedulePage));
        Routing.RegisterRoute(RulesRoute, typeof(Features.Categories.RulesPage));
        Routing.RegisterRoute(SettlementRoute, typeof(SettlementPage));
    }

    /// <summary>Refreshes retained financial details after another tab without replacing their original input drafts.</summary>
    private async void OnRetainedDetailsTabActivated(object? sender, ShellNavigatedEventArgs args)
    {
        // D-394/D-401: a retained Android detail can become visible without a new Page.OnAppearing callback.
        // Keep the original covered loader; it shares an already pending appearance read and never posts money.
        if (_navigationRetired || args.Source is not (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)) return;
        if (CurrentPage is AccountDetailPage account) await Presentation.Failures.GuardAsync(account.RefreshForNavigationAsync);
        else if (CurrentPage is OccurrencePage occurrence) await Presentation.Failures.GuardAsync(occurrence.RefreshForNavigationAsync);
    }

    /// <summary>Keeps the actual restore source/accepted follow-through attached through Back and tab navigation.</summary>
    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        if (CurrentPage?.BindingContext is BackupViewModel backup && !backup.CanLeave && args.CanCancel) args.Cancel();
        if (CurrentPage?.BindingContext is Features.Sync.PersonalSyncViewModel sync && !sync.CanLeave && args.CanCancel) args.Cancel();
        if (CurrentPage?.BindingContext is Features.Sharing.HouseholdViewModel household && !household.CanLeave && args.CanCancel) args.Cancel();
        if (CurrentPage?.BindingContext is Features.Sharing.HouseholdCostsViewModel costs && !costs.CanLeave && args.CanCancel) args.Cancel();
        base.OnNavigating(args);
    }

    /// <summary>Stops translated navigation titles on a retired shell from reaching its disconnected native menu.</summary>
    internal void RetireNavigationBindings()
    {
        _navigationRetired = true;
        Navigated -= OnRetainedDetailsTabActivated;
#if ANDROID
        if (Handler is AccessibleShellRenderer renderer) renderer.RetireNavigation();
#endif
        foreach (var home in _publishedHomes) home.RetirePublication();
        // D-82: Android's retired ShellItemRenderer can retain section events after its Shell handler is cleared.
        // A later translated title then reads a missing MauiContext and aborts the entire language notification.
        // Unsubscribe only this retired navigation graph; keep current titles and every active form unchanged.
        foreach (var item in Items)
        {
            item.RemoveBinding(BaseShellItem.TitleProperty);
            foreach (var section in item.Items)
            {
                section.RemoveBinding(BaseShellItem.TitleProperty);
                foreach (var content in section.Items)
                {
                    if (content.Content is Features.Home.HomePage home) home.RetirePublication();
                    content.RemoveBinding(BaseShellItem.TitleProperty);
                }
            }
        }
    }
}
