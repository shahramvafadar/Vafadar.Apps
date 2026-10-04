using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Backup;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Features.Categories;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.App.Features.Settings;

namespace Vafadar.Zanance.App;

public partial class AppShell : Shell
{
    /// <summary>Route of the account list.</summary>
    public const string AccountsRoute = "accounts";

    /// <summary>Route of the account editor (query: <c>id</c> for an existing account).</summary>
    public const string AccountEditorRoute = "account";

    /// <summary>Route of the account details and reconciliation (query: <c>id</c>).</summary>
    public const string AccountDetailRoute = "accountdetail";

    /// <summary>Route of the settings page.</summary>
    public const string SettingsRoute = "settings";

    /// <summary>Route of the entry editor (query: <c>id</c>, <c>duplicate</c>, <c>refundOf</c> or <c>kind</c>).</summary>
    public const string EntryEditorRoute = "entry";

    /// <summary>Route of the entry details (query: <c>id</c>).</summary>
    public const string EntryDetailRoute = "entrydetail";

    /// <summary>Route of the category list.</summary>
    public const string CategoriesRoute = "categories";

    /// <summary>Route of the category editor (query: <c>id</c> for an existing category).</summary>
    public const string CategoryEditorRoute = "category";

    /// <summary>Route of the plan editor (query: <c>id</c> for an existing plan).</summary>
    public const string PlanEditorRoute = "plan";

    /// <summary>Route of the plan details (query: <c>id</c>).</summary>
    public const string PlanDetailRoute = "plandetail";

    /// <summary>Route of an occurrence (query: <c>plan</c> and <c>date</c>, the original date).</summary>
    public const string OccurrenceRoute = "occurrence";

    /// <summary>Route of backup and restore.</summary>
    public const string BackupRoute = "backup";

    /// <summary>Route of the monthly budget (a top tab of Insights).</summary>
    public const string BudgetRoute = "//insights/budget";

    /// <summary>Route of the budget editor (query: <c>year</c>, <c>month</c>, <c>calendar</c>, <c>currency</c>).</summary>
    public const string BudgetEditorRoute = "budgeteditor";

    /// <summary>Route of the reports (a top tab of Insights).</summary>
    public const string ReportsRoute = "//insights/reports";

    /// <summary>Route of the forecast (a top tab of Insights).</summary>
    public const string ForecastRoute = "//insights/forecast";

    /// <summary>Route of the manual exchange rates.</summary>
    public const string RatesRoute = "rates";

    /// <summary>Route of the Home customisation (§21.5).</summary>
    public const string HomeLayoutRoute = "homelayout";

    /// <summary>Route of the display units such as the toman (FX-07).</summary>
    public const string DisplayUnitsRoute = "displayunits";

    /// <summary>Route of the local profiles (§3).</summary>
    public const string ProfilesRoute = "profiles";

    /// <summary>Route of About Zanance (D-39).</summary>
    public const string AboutRoute = "about";

    /// <summary>Route of the full third-party notices.</summary>
    public const string NoticesRoute = "notices";

    /// <summary>Route of CSV import and export.</summary>
    public const string ImportExportRoute = "importexport";

    /// <summary>Route of the quick templates (TX-04).</summary>
    public const string TemplatesRoute = "templates";

    /// <summary>Route of the savings goals (F2-GOAL; a top tab of Insights).</summary>
    public const string GoalsRoute = "//insights/goals";

    /// <summary>Route of the goal editor.</summary>
    public const string GoalEditorRoute = "goal";

    /// <summary>Route of one goal.</summary>
    public const string GoalDetailRoute = "goaldetail";

    /// <summary>Route of the quantity holdings (ZEX phase 3; More &gt; Money).</summary>
    public const string HoldingsRoute = "holdings";

    /// <summary>Route of one asset type.</summary>
    public const string HoldingDetailRoute = "holdingdetail";

    /// <summary>Route of the asset type editor.</summary>
    public const string AssetTypeEditorRoute = "assettype";

    /// <summary>Route of the editor of one holding event (purchase, sale, move, correction ...).</summary>
    public const string AssetEventEditorRoute = "assetevent";

    /// <summary>Route of the explanation sheet of a KPI (ZEX-UI14).</summary>
    public const string KpiSheetRoute = "kpi";

    /// <summary>Route of the period-end review (ZEX-S0610).</summary>
    public const string ReviewRoute = "review";

    /// <summary>Route of a saved forecast compared with reality (ZEX-S0804).</summary>
    public const string SnapshotRoute = "snapshot";

    /// <summary>Route of the split editor (F2-TX-01).</summary>
    public const string SplitRoute = "split";

    /// <summary>Route of the estimated repayment schedule of a loan (F2-DEBT-02).</summary>
    public const string LoanScheduleRoute = "loanschedule";

    /// <summary>Route of the open reimbursements (F2-TX-03).</summary>
    public const string ReimbursementsRoute = "reimbursements";

    /// <summary>Route of the categorization rules (F2-TX-04).</summary>
    public const string RulesRoute = "rules";

    /// <summary>Route of the final settlement of advance payments (F2-CON-04).</summary>
    public const string SettlementRoute = "settlement";

    public AppShell()
    {
        InitializeComponent();
#if WINDOWS
        // A page opened from another one gets a visible back button next to its title (the window's arrow is tiny); the
        // Insights pages get their four tabs as the title, which the shell otherwise only lists in a drop-down (D-43).
        Navigated += (_, _) =>
        {
            if (CurrentPage is not { } page || GetTitleView(page) is not null)
            {
                return;
            }

            if (Navigation.NavigationStack.Count > 1)
            {
                SetTitleView(page, new Presentation.PageHeader(page, FlowDirection == FlowDirection.RightToLeft));
            }
            else if (Presentation.InsightsTabs.RouteOf(page) is { } route)
            {
                SetTitleView(page, new Presentation.InsightsTabs(route));
            }
        };
#endif
        Routing.RegisterRoute(AccountsRoute, typeof(AccountsPage));
        Routing.RegisterRoute(AccountEditorRoute, typeof(AccountEditorPage));
        Routing.RegisterRoute(AccountDetailRoute, typeof(AccountDetailPage));
        Routing.RegisterRoute(SettingsRoute, typeof(SettingsPage));
        Routing.RegisterRoute(EntryEditorRoute, typeof(EntryEditorPage));
        Routing.RegisterRoute(EntryDetailRoute, typeof(EntryDetailPage));
        Routing.RegisterRoute(CategoriesRoute, typeof(CategoriesPage));
        Routing.RegisterRoute(CategoryEditorRoute, typeof(CategoryEditorPage));
        Routing.RegisterRoute(PlanEditorRoute, typeof(PlanEditorPage));
        Routing.RegisterRoute(PlanDetailRoute, typeof(PlanDetailPage));
        Routing.RegisterRoute(OccurrenceRoute, typeof(OccurrencePage));
        Routing.RegisterRoute(BackupRoute, typeof(BackupPage));
        Routing.RegisterRoute(BudgetEditorRoute, typeof(BudgetEditorPage));
        Routing.RegisterRoute(RatesRoute, typeof(RatesPage));
        Routing.RegisterRoute(HomeLayoutRoute, typeof(Features.Home.HomeLayoutPage));
        Routing.RegisterRoute(DisplayUnitsRoute, typeof(DisplayUnitsPage));
        Routing.RegisterRoute(ProfilesRoute, typeof(Features.Profiles.ProfilesPage));
        Routing.RegisterRoute(AboutRoute, typeof(Features.About.AboutPage));
        Routing.RegisterRoute(NoticesRoute, typeof(Features.About.NoticesPage));
        Routing.RegisterRoute(ImportExportRoute, typeof(ImportExportPage));
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
}
