using Vafadar.Finance.App.Features.Accounts;
using Vafadar.Finance.App.Features.Backup;
using Vafadar.Finance.App.Features.Budget;
using Vafadar.Finance.App.Features.Categories;
using Vafadar.Finance.App.Features.DataFiles;
using Vafadar.Finance.App.Features.Entries;
using Vafadar.Finance.App.Features.Forecast;
using Vafadar.Finance.App.Features.Plans;
using Vafadar.Finance.App.Features.Rates;
using Vafadar.Finance.App.Features.Reports;
using Vafadar.Finance.App.Features.Settings;

namespace Vafadar.Finance.App;

public partial class AppShell : Shell
{
    /// <summary>Route of the account list.</summary>
    public const string AccountsRoute = "accounts";

    /// <summary>Route of the account editor (query: <c>id</c> for an existing account).</summary>
    public const string AccountEditorRoute = "account";

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

    /// <summary>Route of the monthly budget.</summary>
    public const string BudgetRoute = "budget";

    /// <summary>Route of the budget editor (query: <c>year</c>, <c>month</c>, <c>calendar</c>, <c>currency</c>).</summary>
    public const string BudgetEditorRoute = "budgeteditor";

    /// <summary>Route of the reports.</summary>
    public const string ReportsRoute = "reports";

    /// <summary>Route of the forecast.</summary>
    public const string ForecastRoute = "forecast";

    /// <summary>Route of the manual exchange rates.</summary>
    public const string RatesRoute = "rates";

    /// <summary>Route of CSV import and export.</summary>
    public const string ImportExportRoute = "importexport";

    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(AccountsRoute, typeof(AccountsPage));
        Routing.RegisterRoute(AccountEditorRoute, typeof(AccountEditorPage));
        Routing.RegisterRoute(SettingsRoute, typeof(SettingsPage));
        Routing.RegisterRoute(EntryEditorRoute, typeof(EntryEditorPage));
        Routing.RegisterRoute(EntryDetailRoute, typeof(EntryDetailPage));
        Routing.RegisterRoute(CategoriesRoute, typeof(CategoriesPage));
        Routing.RegisterRoute(CategoryEditorRoute, typeof(CategoryEditorPage));
        Routing.RegisterRoute(PlanEditorRoute, typeof(PlanEditorPage));
        Routing.RegisterRoute(PlanDetailRoute, typeof(PlanDetailPage));
        Routing.RegisterRoute(OccurrenceRoute, typeof(OccurrencePage));
        Routing.RegisterRoute(BackupRoute, typeof(BackupPage));
        Routing.RegisterRoute(BudgetRoute, typeof(BudgetPage));
        Routing.RegisterRoute(BudgetEditorRoute, typeof(BudgetEditorPage));
        Routing.RegisterRoute(ReportsRoute, typeof(ReportsPage));
        Routing.RegisterRoute(ForecastRoute, typeof(ForecastPage));
        Routing.RegisterRoute(RatesRoute, typeof(RatesPage));
        Routing.RegisterRoute(ImportExportRoute, typeof(ImportExportPage));
    }
}
