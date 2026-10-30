using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Backup;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.App.Features.Categories;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.App.Features.Settings;

namespace Vafadar.Zanance.App;

public partial class AppShell : Shell
{
    private readonly HashSet<Features.Home.HomePage> _publishedHomes = [];
    private readonly HashSet<AccountsPage> _publishedAccountOverviews = [];
    private readonly HashSet<AccountDetailPage> _publishedAccountDetails = [];
    private readonly HashSet<LoanSchedulePage> _publishedLoanSchedules = [];
    private readonly HashSet<ReimbursementsPage> _publishedReimbursements = [];
    private readonly HashSet<PlanDetailPage> _publishedPlanDetails = [];
    private readonly HashSet<PlansPage> _publishedPlanCenters = [];
    private readonly HashSet<Features.Templates.TemplatesPage> _publishedTemplateCenters = [];
    private readonly HashSet<TransactionsPage> _publishedTransactionCenters = [];
    private readonly HashSet<Features.Reports.PeriodReviewPage> _publishedPeriodReviews = [];
    private readonly HashSet<RulesPage> _publishedRules = [];
    private readonly HashSet<CategoryEditorPage> _publishedCategoryEditors = [];
    private readonly HashSet<CategoriesPage> _publishedCategoryCenters = [];
    private readonly List<WeakReference<AccountEditorPage>> _publishedAccountEditors = [];
    private readonly List<WeakReference<GoalEditorPage>> _publishedGoalEditors = [];
    private readonly List<WeakReference<BudgetEditorPage>> _publishedBudgetEditors = [];
    private readonly List<WeakReference<AssetTypeEditorPage>> _publishedAssetTypeEditors = [];
    private readonly List<WeakReference<Features.Reports.ReportsPage>> _publishedReports = [];
    private readonly List<WeakReference<Features.Reports.KpiSheetPage>> _publishedReportSheets = [];
    private readonly List<WeakReference<Features.Forecast.ForecastPage>> _publishedForecastWorkspaces = [];
    private readonly List<WeakReference<Features.Forecast.SnapshotPage>> _publishedSavedComparisons = [];
    private readonly List<WeakReference<HoldingDetailPage>> _publishedHoldingDetails = [];
    private readonly List<WeakReference<AssetEventEditorPage>> _publishedAssetEventEditors = [];
    private bool _navigationRetired;
    private readonly List<WeakReference<Features.Sync.PersonalSyncPage>> _publishedPersonalSync = [];
    private readonly List<WeakReference<Features.Sharing.HouseholdPage>> _publishedHouseholds = [];
    private readonly List<WeakReference<Features.Commerce.Billing.BillingIdentityPage>> _publishedBillingIdentities = [];
    /// <summary>Registers only the weak actual original identity page, never credential values.</summary>
    internal void RegisterBillingIdentity(Features.Commerce.Billing.BillingIdentityPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _publishedBillingIdentities.RemoveAll(reference => !reference.TryGetTarget(out _));
        if (!_publishedBillingIdentities.Any(reference => reference.TryGetTarget(out var original) && ReferenceEquals(original, page)))
            _publishedBillingIdentities.Add(new(page));
    }
    /// <summary>Only the current actual native root may own optional identity publication.</summary>
    internal bool OwnsBillingIdentityPublication => !_navigationRetired;
    private readonly List<WeakReference<ProductPlansPage>> _publishedProductPlans = [];
    /// <summary>Registers only the weak actual original plan/access page, including covered native consent.</summary>
    internal void RegisterProductPlans(ProductPlansPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        if (!_publishedProductPlans.Any(reference => reference.TryGetTarget(out var original) && ReferenceEquals(original, page)))
            _publishedProductPlans.Add(new(page));
    }
    /// <summary>Only the unchanged original native root can own plan/access publication.</summary>
    internal bool OwnsProductPlansPublication => !_navigationRetired;
    private readonly List<WeakReference<Features.Sharing.HouseholdCostsPage>> _publishedHouseholdCosts = [];
    /// <summary>Registers the actual original cost page without retaining a departed native root.</summary>
    internal void RegisterHouseholdCosts(Features.Sharing.HouseholdCostsPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        if (!_publishedHouseholdCosts.Any(reference => reference.TryGetTarget(out var original) && ReferenceEquals(original, page)))
            _publishedHouseholdCosts.Add(new(page));
    }
    /// <summary>Only the current shell may retain original household publication.</summary>
    internal bool OwnsHouseholdPublication => !_navigationRetired;
    /// <summary>Retires actual household pages when this native root is replaced.</summary>
    internal void RegisterHousehold(Features.Sharing.HouseholdPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        if (!_publishedHouseholds.Any(reference => reference.TryGetTarget(out var original) && ReferenceEquals(original, page)))
            _publishedHouseholds.Add(new(page));
    }
    /// <summary>Gets whether this actual root can still own the original optional sync workspace.</summary>
    internal bool OwnsPersonalSyncPublication => !_navigationRetired;
    /// <summary>Registers weak original sync pages before their initial local read or native consent.</summary>
    internal void RegisterPersonalSync(Features.Sync.PersonalSyncPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        if (!_publishedPersonalSync.Any(reference => reference.TryGetTarget(out var original) && ReferenceEquals(original, page)))
            _publishedPersonalSync.Add(new(page));
    }
    private readonly List<WeakReference<ActiveItemsPage>> _publishedActiveItems = [];
    /// <summary>Gets whether this actual root can still own its original resource-choice drafts.</summary>
    internal bool OwnsActiveItemsPublication => !_navigationRetired;
    /// <summary>Registers weak original choice pages before starting their actual reads.</summary>
    internal void RegisterActiveItems(ActiveItemsPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        if (!_publishedActiveItems.Any(reference => reference.TryGetTarget(out var original) && ReferenceEquals(original, page)))
            _publishedActiveItems.Add(new(page));
    }
    private readonly List<WeakReference<Features.Profiles.ProfilesPage>> _publishedProfiles = [];
    /// <summary>Gets whether this native root still owns its original profile catalogue.</summary>
    internal bool OwnsProfilesPublication => !_navigationRetired;
    /// <summary>Registers a weak original catalogue for explicit root retirement.</summary>
    internal void RegisterProfiles(Features.Profiles.ProfilesPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        if (!_publishedProfiles.Any(reference => reference.TryGetTarget(out var original) && ReferenceEquals(original, page)))
            _publishedProfiles.Add(new(page));
    }
    /// <summary>Gets whether this actual native root can still own an original account editor or accepted return.</summary>
    internal bool OwnsAccountEditorPublication => !_navigationRetired;
    /// <summary>Gets whether this actual root still owns an original goal editor.</summary>
    internal bool OwnsGoalEditorPublication => !_navigationRetired;
    /// <summary>Gets whether this original root still owns its budget form.</summary>
    internal bool OwnsBudgetEditorPublication => !_navigationRetired;
    /// <summary>Gets whether this actual root still owns its original asset-type forms.</summary>
    internal bool OwnsAssetTypeEditorPublication => !_navigationRetired;
    /// <summary>Gets whether this actual root still owns original event forms and their consent.</summary>
    internal bool OwnsAssetEventEditorPublication => !_navigationRetired;
    /// <summary>Gets whether this root still owns its original holding detail frames.</summary>
    internal bool OwnsHoldingDetailPublication => !_navigationRetired;
    /// <summary>Gets whether this root still owns original report source and its explicit explanation handoffs.</summary>
    internal bool OwnsReportPublication => !_navigationRetired;
    /// <summary>Tracks weak original restore pages without retaining replaced roots or selected packages.</summary>
    private readonly List<WeakReference<Features.Backup.BackupPage>> _backupRestorePages = [];
    /// <summary>Gets whether the actual original root still owns restore consent.</summary>
    internal bool OwnsBackupRestorePublication => !_navigationRetired;
    /// <summary>Registers the actual original backup/restore page.</summary>
    internal void RegisterBackupRestore(Features.Backup.BackupPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _backupRestorePages.RemoveAll(item => !item.TryGetTarget(out _));
        if (!_backupRestorePages.Any(item => item.TryGetTarget(out var old) && ReferenceEquals(old, page))) _backupRestorePages.Add(new(page));
    }
    /// <summary>Tracks weak original CSV pages without retaining replaced roots.</summary>
    private readonly List<WeakReference<Features.DataFiles.ImportExportPage>> _dataFilePages = [];
    /// <summary>Gets whether the actual root owns original CSV source and disclosure consent.</summary>
    internal bool OwnsDataFilePublication => !_navigationRetired;
    /// <summary>Registers only the original CSV page.</summary>
    internal void RegisterDataFiles(Features.DataFiles.ImportExportPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _dataFilePages.RemoveAll(item => !item.TryGetTarget(out _));
        if (!_dataFilePages.Any(item => item.TryGetTarget(out var old) && ReferenceEquals(old, page))) _dataFilePages.Add(new(page));
    }
    /// <summary>Tracks only weak original accountant disclosure pages for terminal root retirement.</summary>
    private readonly List<WeakReference<Features.DataFiles.AccountantExportPage>> _accountantExportPages = [];
    /// <summary>Gets whether this actual root owns original accountant source and disclosure consent.</summary>
    internal bool OwnsAccountantExportPublication => !_navigationRetired;
    /// <summary>Registers only the actual original accountant page.</summary>
    internal void RegisterAccountantExport(Features.DataFiles.AccountantExportPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _accountantExportPages.RemoveAll(item => !item.TryGetTarget(out _));
        if (!_accountantExportPages.Any(item => item.TryGetTarget(out var old) && ReferenceEquals(old, page))) _accountantExportPages.Add(new(page));
    }
    /// <summary>Tracks only weak original reference pages for terminal root retirement.</summary>
    private readonly List<WeakReference<Features.Rates.MarketReferencePage>> _marketReferencePages = [];
    /// <summary>Gets whether this actual root owns original reference source and consent.</summary>
    internal bool OwnsMarketReferencePublication => !_navigationRetired;
    /// <summary>Registers the actual reference page without retaining a replacement application root.</summary>
    internal void RegisterMarketReference(Features.Rates.MarketReferencePage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _marketReferencePages.RemoveAll(item => !item.TryGetTarget(out _));
        if (!_marketReferencePages.Any(item => item.TryGetTarget(out var old) && ReferenceEquals(old, page))) _marketReferencePages.Add(new(page));
    }
    /// <summary>Tracks only weak original display-unit pages for terminal root retirement.</summary>
    private readonly List<WeakReference<Features.Rates.DisplayUnitsPage>> _displayUnitPages = [];
    /// <summary>Gets whether this actual root owns original display-unit publication.</summary>
    internal bool OwnsDisplayUnitsPublication => !_navigationRetired;
    /// <summary>Registers only the actual original unit page, without retaining its replacement root.</summary>
    internal void RegisterDisplayUnits(Features.Rates.DisplayUnitsPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _displayUnitPages.RemoveAll(item => !item.TryGetTarget(out _));
        if (!_displayUnitPages.Any(item => item.TryGetTarget(out var old) && ReferenceEquals(old, page))) _displayUnitPages.Add(new(page));
    }
    /// <summary>Tracks only weak original manual-rate pages for terminal root retirement.</summary>
    private readonly List<WeakReference<Features.Rates.RatesPage>> _ratesPages = [];
    /// <summary>Gets whether this actual root owns the original manual-rate publication.</summary>
    internal bool OwnsRatesPublication => !_navigationRetired;
    /// <summary>Registers the actual manual-rate page without retaining a replacement root.</summary>
    internal void RegisterRates(Features.Rates.RatesPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _ratesPages.RemoveAll(item => !item.TryGetTarget(out _));
        if (!_ratesPages.Any(item => item.TryGetTarget(out var old) && ReferenceEquals(old, page))) _ratesPages.Add(new(page));
    }
    /// <summary>Tracks only weak original settings pages for terminal root retirement.</summary>
    private readonly List<WeakReference<Features.Settings.SettingsPage>> _settingsPages = [];
    /// <summary>Gets whether this actual root still owns original settings publication.</summary>
    internal bool OwnsSettingsPublication => !_navigationRetired;
    /// <summary>Retains only weak original settings roots for terminal native retirement.</summary>
    internal void RegisterSettings(Features.Settings.SettingsPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _settingsPages.RemoveAll(item => !item.TryGetTarget(out _));
        if (!_settingsPages.Any(item => item.TryGetTarget(out var old) && ReferenceEquals(old, page))) _settingsPages.Add(new(page));
    }
    /// <summary>Tracks only weak original report pages for terminal root retirement.</summary>
    internal void RegisterReport(Features.Reports.ReportsPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _publishedReports.RemoveAll(reference => !reference.TryGetTarget(out _));
        if (!_publishedReports.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, page))) _publishedReports.Add(new(page));
    }
    /// <summary>Tracks only weak original report explanations, including their pending native close.</summary>
    internal void RegisterReportSheet(Features.Reports.KpiSheetPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _publishedReportSheets.RemoveAll(reference => !reference.TryGetTarget(out _));
        if (!_publishedReportSheets.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, page))) _publishedReportSheets.Add(new(page));
    }
    /// <summary>Gets whether this root still owns its original process-local forecast workspaces.</summary>
    internal bool OwnsForecastWorkspacePublication => !_navigationRetired;
    /// <summary>Tracks only weak native workspaces for terminal root retirement.</summary>
    internal void RegisterForecastWorkspace(Features.Forecast.ForecastPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _publishedForecastWorkspaces.RemoveAll(reference => !reference.TryGetTarget(out _));
        if (!_publishedForecastWorkspaces.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, page))) _publishedForecastWorkspaces.Add(new(page));
    }
    /// <summary>Gets whether this root still owns its original saved forecast comparisons.</summary>
    internal bool OwnsSavedComparisonPublication => !_navigationRetired;
    /// <summary>Tracks weak original saved-comparison pages without retaining their data through a completed root.</summary>
    internal void RegisterSavedComparison(Features.Forecast.SnapshotPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _publishedSavedComparisons.RemoveAll(reference => !reference.TryGetTarget(out _));
        if (!_publishedSavedComparisons.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, page)))
            _publishedSavedComparisons.Add(new(page));
    }
    /// <summary>Registers only weak native detail pages for terminal root retirement.</summary>
    internal void RegisterHoldingDetail(HoldingDetailPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _publishedHoldingDetails.RemoveAll(reference => !reference.TryGetTarget(out _));
        if (!_publishedHoldingDetails.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, page)))
            _publishedHoldingDetails.Add(new(page));
    }

    /// <summary>Records actual published Home instances; lazy ShellContent.Content may not expose its cached page.</summary>
    internal void RegisterHomePublication(Features.Home.HomePage home)
    {
        if (_navigationRetired) home.RetirePublication();
        else _publishedHomes.Add(home);
    }

    /// <summary>Tracks original lazy account pages so shell replacement retires pending reads and native rows too.</summary>
    internal void RegisterAccountOverview(AccountsPage page)
    {
        if (_navigationRetired) page.RetireShellPublication();
        else _publishedAccountOverviews.Add(page);
    }

    /// <summary>Registers actual original account-detail owners so root retirement also retires pending financial consent.</summary>
    internal void RegisterAccountDetail(AccountDetailPage page)
    {
        if (_navigationRetired) page.RetireShellCommands();
        else _publishedAccountDetails.Add(page);
    }
    /// <summary>Registers actual original schedule owners so root retirement cancels their unfinished reads.</summary>
    internal void RegisterLoanSchedule(LoanSchedulePage page)
    {
        if (_navigationRetired) page.RetireShellPublication();
        else _publishedLoanSchedules.Add(page);
    }
    /// <summary>Registers actual original reimbursement owners so root replacement retires pending reads and rows.</summary>
    internal void RegisterReimbursements(ReimbursementsPage page)
    {
        if (_navigationRetired) page.RetireShellPublication();
        else _publishedReimbursements.Add(page);
    }
    /// <summary>Registers original plan-detail owners so root replacement retires pending reads and native consent.</summary>
    internal void RegisterPlanDetail(PlanDetailPage page)
    {
        if (_navigationRetired) page.RetireShellPublication();
        else _publishedPlanDetails.Add(page);
    }
    /// <summary>Registers the original plan-center root so retained source and native continuations retire together.</summary>
    internal void RegisterPlanCenter(PlansPage page)
    {
        if (_navigationRetired) page.RetireShellPublication();
        else _publishedPlanCenters.Add(page);
    }
    /// <summary>Registers actual template list roots so native commands cannot outlive their original page.</summary>
    internal void RegisterTemplateCenter(Features.Templates.TemplatesPage page)
    {
        if (_navigationRetired) page.RetireShellPublication();
        else _publishedTemplateCenters.Add(page);
    }
    /// <summary>Registers original Transactions roots so retained rows and native consent retire with their Shell.</summary>
    internal void RegisterTransactionCenter(TransactionsPage page)
    {
        if (_navigationRetired) page.RetireShellPublication();
        else _publishedTransactionCenters.Add(page);
    }
    /// <summary>Tracks original checklist pages so retained progress and row owners retire with their root.</summary>
    internal void RegisterPeriodReview(Features.Reports.PeriodReviewPage page)
    {
        if (_navigationRetired) page.RetireShellPublication();
        else _publishedPeriodReviews.Add(page);
    }
    /// <summary>Tracks original rule pages and their pending native consent across root replacement.</summary>
    internal void RegisterRules(RulesPage page)
    {
        if (_navigationRetired) page.RetireShellPublication();
        else _publishedRules.Add(page);
    }
    /// <summary>Tracks actual modal category editors so source/draft/native consent retire with their original root.</summary>
    internal void RegisterCategoryEditor(CategoryEditorPage page)
    {
        if (_navigationRetired) page.RetireShellPublication();
        else _publishedCategoryEditors.Add(page);
    }
    /// <summary>Tracks complete original category overview rows across lazy native roots.</summary>
    internal void RegisterCategories(CategoriesPage page)
    {
        if (_navigationRetired) page.RetireShellPublication();
        else _publishedCategoryCenters.Add(page);
    }
    /// <summary>Route of the account list.</summary>
    public const string AccountsRoute = AppRoutes.AccountsRoute;

    /// <summary>Tracks original account form roots without retaining every completed modal for the entire session.</summary>
    internal void RegisterAccountEditor(AccountEditorPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _publishedAccountEditors.RemoveAll(reference => !reference.TryGetTarget(out _));
        if (!_publishedAccountEditors.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, page)))
            _publishedAccountEditors.Add(new(page));
    }

    /// <summary>Tracks weak original goal modal owners so completed forms are not retained by the root.</summary>
    internal void RegisterGoalEditor(GoalEditorPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _publishedGoalEditors.RemoveAll(reference => !reference.TryGetTarget(out _));
        if (!_publishedGoalEditors.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, page)))
            _publishedGoalEditors.Add(new(page));
    }

    /// <summary>Tracks weak actual budget modal owners so completed forms are not retained by their root.</summary>
    internal void RegisterBudgetEditor(BudgetEditorPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _publishedBudgetEditors.RemoveAll(reference => !reference.TryGetTarget(out _));
        if (!_publishedBudgetEditors.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, page)))
            _publishedBudgetEditors.Add(new(page));
    }

    /// <summary>Tracks weak actual type editors without retaining completed forms through the root.</summary>
    internal void RegisterAssetTypeEditor(AssetTypeEditorPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _publishedAssetTypeEditors.RemoveAll(reference => !reference.TryGetTarget(out _));
        if (!_publishedAssetTypeEditors.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, page)))
            _publishedAssetTypeEditors.Add(new(page));
    }

    /// <summary>Tracks only weak actual original event pages, including routed modal forms.</summary>
    internal void RegisterAssetEventEditor(AssetEventEditorPage page)
    {
        if (_navigationRetired) { page.RetireShellPublication(); return; }
        _publishedAssetEventEditors.RemoveAll(reference => !reference.TryGetTarget(out _));
        if (!_publishedAssetEventEditors.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, page)))
            _publishedAssetEventEditors.Add(new(page));
    }

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
        // D-577: the list can be retained too; otherwise it keeps pre-transfer/refund balances until Back/reopen.
        if (CurrentPage is Features.Sync.PersonalSyncPage personalSync) await Presentation.Failures.GuardAsync(personalSync.RefreshForNavigationAsync);
        if (CurrentPage is Features.Sharing.HouseholdPage householdPage) await Presentation.Failures.GuardAsync(householdPage.RefreshForNavigationAsync);
        if (CurrentPage is Features.Commerce.Billing.BillingIdentityPage identityPage) await Presentation.Failures.GuardAsync(identityPage.RefreshForNavigationAsync);
        if (CurrentPage is ProductPlansPage productPlansPage) await Presentation.Failures.GuardAsync(productPlansPage.RefreshForNavigationAsync);
        if (CurrentPage is Features.Sharing.HouseholdCostsPage householdCostsPage) await Presentation.Failures.GuardAsync(householdCostsPage.RefreshForNavigationAsync);
        else if (CurrentPage is ActiveItemsPage activeItems) await Presentation.Failures.GuardAsync(activeItems.RefreshForNavigationAsync);
        else if (CurrentPage is Features.Profiles.ProfilesPage profiles) await Presentation.Failures.GuardAsync(profiles.RefreshForNavigationAsync);
        else if (CurrentPage is AccountsPage accounts) await Presentation.Failures.GuardAsync(accounts.RefreshForNavigationAsync);
        else if (CurrentPage is AccountDetailPage account) await Presentation.Failures.GuardAsync(account.RefreshForNavigationAsync);
        else if (CurrentPage is HoldingDetailPage holdingDetail) await Presentation.Failures.GuardAsync(holdingDetail.RefreshForNavigationAsync);
        else if (CurrentPage is LoanSchedulePage schedule) await Presentation.Failures.GuardAsync(schedule.RefreshForNavigationAsync);
        else if (CurrentPage is ReimbursementsPage reimbursements) await Presentation.Failures.GuardAsync(reimbursements.RefreshForNavigationAsync);
        else if (CurrentPage is PlansPage planCenter) await Presentation.Failures.GuardAsync(planCenter.RefreshForNavigationAsync);
        else if (CurrentPage is Features.Templates.TemplatesPage templates) await Presentation.Failures.GuardAsync(templates.RefreshForNavigationAsync);
        else if (CurrentPage is TransactionsPage transactions) await Presentation.Failures.GuardAsync(transactions.RefreshForNavigationAsync);
        else if (CurrentPage is CategoriesPage categories) await Presentation.Failures.GuardAsync(categories.RefreshForNavigationAsync);
        else if (CurrentPage is CategoryEditorPage categoryEditor) await Presentation.Failures.GuardAsync(categoryEditor.RefreshForNavigationAsync);
        else if (CurrentPage is RulesPage rules) await Presentation.Failures.GuardAsync(rules.RefreshForNavigationAsync);
        else if (CurrentPage is Features.Reports.PeriodReviewPage review) await Presentation.Failures.GuardAsync(review.RefreshForNavigationAsync);
        else if (CurrentPage is PlanDetailPage planDetail) await Presentation.Failures.GuardAsync(planDetail.RefreshForNavigationAsync);
        else if (CurrentPage is OccurrencePage occurrence) await Presentation.Failures.GuardAsync(occurrence.RefreshForNavigationAsync);
        // D-497: Insights can retain both the forecast and its saved comparison while another tab changes the ledger.
        else if (CurrentPage is Features.Backup.BackupPage backupRestore) await Presentation.Failures.GuardAsync(backupRestore.RefreshForNavigationAsync);
        else if (CurrentPage is Features.DataFiles.ImportExportPage dataFiles) await Presentation.Failures.GuardAsync(dataFiles.RefreshForNavigationAsync);
        else if (CurrentPage is Features.DataFiles.AccountantExportPage accountantExport) await Presentation.Failures.GuardAsync(accountantExport.RefreshForNavigationAsync);
        else if (CurrentPage is Features.Rates.MarketReferencePage marketReference) await Presentation.Failures.GuardAsync(marketReference.RefreshForNavigationAsync);
        else if (CurrentPage is Features.Rates.DisplayUnitsPage units) await Presentation.Failures.GuardAsync(units.RefreshForNavigationAsync);
        else if (CurrentPage is Features.Rates.RatesPage rates) await Presentation.Failures.GuardAsync(rates.RefreshForNavigationAsync);
        else if (CurrentPage is Features.Settings.SettingsPage settings) await Presentation.Failures.GuardAsync(settings.RefreshForNavigationAsync);
        else if (CurrentPage is Features.Reports.ReportsPage report) await Presentation.Failures.GuardAsync(report.RefreshForNavigationAsync);
        else if (CurrentPage is Features.Forecast.ForecastPage forecast) await Presentation.Failures.GuardAsync(forecast.RefreshForNavigationAsync);
        else if (CurrentPage is Features.Forecast.SnapshotPage snapshot) await Presentation.Failures.GuardAsync(snapshot.RefreshForNavigationAsync);
    }

    /// <summary>Keeps the actual restore source/accepted follow-through attached through Back and tab navigation.</summary>
    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        // D-654: permit only the exact registered authentication modal, never a general busy Push.
        var originalBackupPage = CurrentPage?.BindingContext is BackupViewModel ? CurrentPage : Security.NativeSensitivePresentation.Underlying(this);
        var backup = originalBackupPage?.BindingContext as BackupViewModel;
        var ownBackupAuthentication = backup?.IsConfirmingSensitiveOperation == true && originalBackupPage is not null
            && Security.NativeSensitivePresentation.Allows(this, originalBackupPage, args);
        if (backup is not null && !backup.CanLeave && args.CanCancel && !ownBackupAuthentication) args.Cancel();
        var ownCommandReturn = Interaction.OwnedCommandReturn.Allows(this, args);
        var originalChoicePage = CurrentPage?.BindingContext is ActiveItemsViewModel ? CurrentPage : Security.NativeSensitivePresentation.Underlying(this);
        if (originalChoicePage?.BindingContext is ActiveItemsViewModel choices && !choices.CanLeave && args.CanCancel
            && !ownCommandReturn && !Security.NativeSensitivePresentation.Allows(this, originalChoicePage, args)) args.Cancel();
        var originalProfilesPage = CurrentPage?.BindingContext is Features.Profiles.ProfilesViewModel ? CurrentPage : Security.NativeSensitivePresentation.Underlying(this);
        if (originalProfilesPage?.BindingContext is Features.Profiles.ProfilesViewModel profiles && !profiles.CanLeave && args.CanCancel
            && !ownCommandReturn && !Security.NativeSensitivePresentation.Allows(this, originalProfilesPage, args)) args.Cancel();
        var originalSyncPage = CurrentPage?.BindingContext is Features.Sync.PersonalSyncViewModel ? CurrentPage : Security.NativeSensitivePresentation.Underlying(this);
        if (originalSyncPage?.BindingContext is Features.Sync.PersonalSyncViewModel sync && !sync.CanLeave && args.CanCancel
            && !Security.NativeSensitivePresentation.Allows(this, originalSyncPage, args)) args.Cancel();
        var originalHouseholdPage = CurrentPage?.BindingContext is Features.Sharing.HouseholdViewModel ? CurrentPage : Security.NativeSensitivePresentation.Underlying(this);
        if (originalHouseholdPage?.BindingContext is Features.Sharing.HouseholdViewModel household && !household.CanLeave && args.CanCancel
            && !Security.NativeSensitivePresentation.Allows(this, originalHouseholdPage, args)) args.Cancel();
        var originalIdentityPage = CurrentPage is Features.Commerce.Billing.BillingIdentityPage ? CurrentPage : Security.NativeSensitivePresentation.Underlying(this);
        if (originalIdentityPage?.BindingContext is Features.Commerce.Billing.BillingIdentityViewModel identity && !identity.CanLeave && args.CanCancel
            && !Security.NativeSensitivePresentation.Allows(this, originalIdentityPage, args)) args.Cancel();
        var originalProductPlansPage = CurrentPage is ProductPlansPage ? CurrentPage : Security.NativeSensitivePresentation.Underlying(this);
        if (originalProductPlansPage?.BindingContext is ProductPlansViewModel productPlans && !productPlans.CanLeave && args.CanCancel
            && !Security.NativeSensitivePresentation.Allows(this, originalProductPlansPage, args)) args.Cancel();
        var originalCostsPage = CurrentPage?.BindingContext is Features.Sharing.HouseholdCostsViewModel ? CurrentPage : Security.NativeSensitivePresentation.Underlying(this);
        if (originalCostsPage?.BindingContext is Features.Sharing.HouseholdCostsViewModel costs && !costs.CanLeave && args.CanCancel
            && !Security.NativeSensitivePresentation.Allows(this, originalCostsPage, args))
        {
            args.Cancel();
            if (costs.NeedsLeaveConfirmation && !costs.HasPendingOperation)
            {
                var originalDestination = args.Target.Location;
                Dispatcher.Dispatch(() => _ = Presentation.Failures.GuardAsync(() =>
                    costs.RequestLeaveAsync(() => GoToAsync(originalDestination))));
            }
        }
        base.OnNavigating(args);
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is Features.Commerce.Billing.BillingIdentityPage departingIdentity) departingIdentity.RetirePublication();
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is ProductPlansPage departingProductPlans) departingProductPlans.RetirePublication();
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is Features.Sync.PersonalSyncPage syncPage) syncPage.RetirePublication();
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is Features.Sharing.HouseholdPage departingHousehold) departingHousehold.RetirePublication();
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is Features.Sharing.HouseholdCostsPage departingCosts) departingCosts.RetirePublication();
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is ActiveItemsPage choicePage) choicePage.RetirePublication();
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is Features.Profiles.ProfilesPage profilePage) profilePage.RetirePublication();
        // D-627: a routed Accounts page can remain cached across tabs without OnDisappearing.
        // Retire only after the original leave guards accept this actual tab departure.
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is AccountsPage accounts)
            accounts.RetirePublication();
        // D-628: original account conversion/reconciliation dialogs cannot outlive an accepted retained-tab departure.
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is AccountDetailPage accountDetail)
            accountDetail.RetireCommands();
        // D-630: the original routed schedule is retained by the same native tab/root rules as account detail.
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is LoanSchedulePage schedule)
            schedule.RetirePublication();
        // D-631: a retained original reimbursement page must not publish across an accepted tab departure.
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is ReimbursementsPage reimbursements)
            reimbursements.RetirePublication();
        // D-632: a native confirmation and row target cannot outlive the original retained plan-detail page.
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is PlanDetailPage planDetail)
            planDetail.RetirePublication();
        // D-633: the retained plan-center root and its permission query cannot publish after a tab leaves.
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is PlansPage planCenter)
            planCenter.RetirePublication();
        // D-634: More can retain a routed template page without another native disappearance callback.
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is Features.Templates.TemplatesPage templates)
            templates.RetirePublication();
        // D-635: a retained transaction tab must retire the same source, selection and native consent.
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is TransactionsPage transactions)
            transactions.RetirePublication();
        // D-636: retained checklist rows must not save progress after actual tab departure.
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is Features.Reports.PeriodReviewPage review)
            review.RetirePublication();
        // D-637: an original rule confirmation cannot outlive an accepted retained-tab departure.
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is RulesPage rules)
            rules.RetirePublication();
        // D-638: a modal editor and its actual original consent cannot survive an accepted tab departure.
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged))
        {
            if (CurrentPage is CategoriesPage categories) categories.RetirePublication();
            if (CurrentPage is AccountEditorPage accountEditor) accountEditor.RetirePublication();
            if (CurrentPage is Features.Backup.BackupPage backupRestore) backupRestore.RetirePublication();
            if (CurrentPage is Features.DataFiles.AccountantExportPage accountantExport) accountantExport.RetirePublication();
            if (CurrentPage is Features.Rates.MarketReferencePage marketReference) marketReference.RetirePublication();
            if (CurrentPage is Features.Rates.DisplayUnitsPage units) units.RetirePublication();
            if (CurrentPage is Features.Rates.RatesPage rates) rates.RetirePublication();
            if (CurrentPage is Features.Settings.SettingsPage settings) settings.RetirePublication();
            if (CurrentPage is Features.Reports.ReportsPage report) report.RetirePublication();
            if (CurrentPage is Features.Reports.KpiSheetPage reportSheet) reportSheet.RetirePublication();
            if (Navigation.ModalStack.LastOrDefault() is Features.Reports.KpiSheetPage modalReportSheet) modalReportSheet.RetirePublication();
            if (CurrentPage is Features.Forecast.ForecastPage forecastWorkspace) forecastWorkspace.RetirePublication();
            if (CurrentPage is Features.Forecast.SnapshotPage savedComparison) savedComparison.RetirePublication();
            if (CurrentPage is HoldingDetailPage holdingDetail) holdingDetail.RetirePublication();
            if (CurrentPage is AssetEventEditorPage eventEditor) eventEditor.RetirePublication();
            if (Navigation.ModalStack.LastOrDefault() is AssetEventEditorPage modalEvent) modalEvent.RetirePublication();
            if (Navigation.ModalStack.LastOrDefault() is NavigationPage eventNavigation && eventNavigation.CurrentPage is AssetEventEditorPage wrappedEvent) wrappedEvent.RetirePublication();
            if (CurrentPage is AssetTypeEditorPage typeEditor) typeEditor.RetirePublication();
            if (Navigation.ModalStack.LastOrDefault() is AssetTypeEditorPage modalType) modalType.RetirePublication();
            if (Navigation.ModalStack.LastOrDefault() is NavigationPage typeNavigation && typeNavigation.CurrentPage is AssetTypeEditorPage wrappedType) wrappedType.RetirePublication();
            if (CurrentPage is BudgetEditorPage budgetEditor) budgetEditor.RetirePublication();
            if (Navigation.ModalStack.LastOrDefault() is BudgetEditorPage modalBudget) modalBudget.RetirePublication();
            if (Navigation.ModalStack.LastOrDefault() is NavigationPage budgetNavigation && budgetNavigation.CurrentPage is BudgetEditorPage wrappedBudget) wrappedBudget.RetirePublication();
            if (CurrentPage is GoalEditorPage goalEditor) goalEditor.RetirePublication();
            if (Navigation.ModalStack.LastOrDefault() is GoalEditorPage modalGoal) modalGoal.RetirePublication();
            if (Navigation.ModalStack.LastOrDefault() is NavigationPage goalNavigation && goalNavigation.CurrentPage is GoalEditorPage wrappedGoal) wrappedGoal.RetirePublication();
            if (Navigation.ModalStack.LastOrDefault() is AccountEditorPage modalAccount) modalAccount.RetirePublication();
            if (Navigation.ModalStack.LastOrDefault() is NavigationPage accountNavigation && accountNavigation.CurrentPage is AccountEditorPage wrappedAccount) wrappedAccount.RetirePublication();
            if (CurrentPage is CategoryEditorPage categoryEditor) categoryEditor.RetirePublication();
            if (Navigation.ModalStack.LastOrDefault() is CategoryEditorPage modalCategory) modalCategory.RetirePublication();
            if (Navigation.ModalStack.LastOrDefault() is NavigationPage modalNavigation && modalNavigation.CurrentPage is CategoryEditorPage wrappedCategory) wrappedCategory.RetirePublication();
        }
        // D-406: Android can retain a routed More page across tabs without OnDisappearing.
        // Retire its original financial review only after navigation survives the existing leave guards.
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage is ImportExportPage import)
            import.RetirePublication();
        // D-413: a retained holding detail must retire the same original price authority on tab departure.
        if (!args.Cancelled && args.Source is (ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            && CurrentPage?.BindingContext is Features.Holdings.HoldingDetailViewModel holding)
        {
            holding.Retire();
        }
    }

    /// <summary>Stops translated navigation titles on a retired shell from reaching its disconnected native menu.</summary>
    internal void RetireNavigationBindings()
    {
        _navigationRetired = true;
        foreach (var reference in _publishedPersonalSync)
            if (reference.TryGetTarget(out var sync)) sync.RetireShellPublication();
        _publishedPersonalSync.Clear();
        foreach (var reference in _publishedHouseholds)
            if (reference.TryGetTarget(out var household)) household.RetireShellPublication();
        _publishedHouseholds.Clear();
        foreach (var reference in _publishedBillingIdentities)
            if (reference.TryGetTarget(out var identity)) identity.RetireShellPublication();
        _publishedBillingIdentities.Clear();
        foreach (var reference in _publishedProductPlans)
            if (reference.TryGetTarget(out var plans)) plans.RetireShellPublication();
        _publishedProductPlans.Clear();
        foreach (var reference in _publishedHouseholdCosts)
            if (reference.TryGetTarget(out var costs)) costs.RetireShellPublication();
        _publishedHouseholdCosts.Clear();
        foreach (var reference in _publishedActiveItems)
            if (reference.TryGetTarget(out var choices)) choices.RetireShellPublication();
        _publishedActiveItems.Clear();
        foreach (var reference in _publishedProfiles)
            if (reference.TryGetTarget(out var profiles)) profiles.RetireShellPublication();
        _publishedProfiles.Clear();
        foreach (var accounts in _publishedAccountOverviews) accounts.RetireShellPublication();
        _publishedAccountOverviews.Clear();
        foreach (var account in _publishedAccountDetails) account.RetireShellCommands();
        _publishedAccountDetails.Clear();
        foreach (var schedule in _publishedLoanSchedules) schedule.RetireShellPublication();
        _publishedLoanSchedules.Clear();
        foreach (var reimbursements in _publishedReimbursements) reimbursements.RetireShellPublication();
        _publishedReimbursements.Clear();
        foreach (var planDetail in _publishedPlanDetails) planDetail.RetireShellPublication();
        _publishedPlanDetails.Clear();
        foreach (var planCenter in _publishedPlanCenters) planCenter.RetireShellPublication();
        _publishedPlanCenters.Clear();
        foreach (var templates in _publishedTemplateCenters) templates.RetireShellPublication();
        _publishedTemplateCenters.Clear();
        foreach (var transactions in _publishedTransactionCenters) transactions.RetireShellPublication();
        _publishedTransactionCenters.Clear();
        foreach (var review in _publishedPeriodReviews) review.RetireShellPublication();
        _publishedPeriodReviews.Clear();
        foreach (var rules in _publishedRules) rules.RetireShellPublication();
        _publishedRules.Clear();
        foreach (var editor in _publishedCategoryEditors) editor.RetireShellPublication();
        _publishedCategoryEditors.Clear();
        foreach (var categories in _publishedCategoryCenters) categories.RetireShellPublication();
        _publishedCategoryCenters.Clear();
        foreach (var reference in _publishedAccountEditors)
            if (reference.TryGetTarget(out var editor)) editor.RetireShellPublication();
        _publishedAccountEditors.Clear();
        foreach (var reference in _publishedGoalEditors)
            if (reference.TryGetTarget(out var editor)) editor.RetireShellPublication();
        _publishedGoalEditors.Clear();
        foreach (var reference in _publishedBudgetEditors)
            if (reference.TryGetTarget(out var editor)) editor.RetireShellPublication();
        _publishedBudgetEditors.Clear();
        foreach (var reference in _publishedAssetTypeEditors)
            if (reference.TryGetTarget(out var editor)) editor.RetireShellPublication();
        _publishedAssetTypeEditors.Clear();
        foreach (var reference in _accountantExportPages) if (reference.TryGetTarget(out var accountantExport)) accountantExport.RetireShellPublication();
        _accountantExportPages.Clear();
        foreach (var reference in _dataFilePages) if (reference.TryGetTarget(out var dataFiles)) dataFiles.RetireShellPublication();
        _dataFilePages.Clear();
        foreach (var reference in _backupRestorePages) if (reference.TryGetTarget(out var backupRestore)) backupRestore.RetireShellPublication();
        _backupRestorePages.Clear();
        foreach (var reference in _marketReferencePages) if (reference.TryGetTarget(out var marketReference)) marketReference.RetireShellPublication();
        _marketReferencePages.Clear();
        foreach (var reference in _displayUnitPages) if (reference.TryGetTarget(out var units)) units.RetireShellPublication();
        _displayUnitPages.Clear();
        foreach (var reference in _ratesPages) if (reference.TryGetTarget(out var rates)) rates.RetireShellPublication();
        _ratesPages.Clear();
        foreach (var reference in _settingsPages) if (reference.TryGetTarget(out var settings)) settings.RetireShellPublication();
        _settingsPages.Clear();
        foreach (var reference in _publishedReports)
            if (reference.TryGetTarget(out var report)) report.RetireShellPublication();
        _publishedReports.Clear();
        foreach (var reference in _publishedReportSheets)
            if (reference.TryGetTarget(out var sheet)) sheet.RetireShellPublication();
        _publishedReportSheets.Clear();
        foreach (var reference in _publishedForecastWorkspaces)
            if (reference.TryGetTarget(out var workspace)) workspace.RetireShellPublication();
        _publishedForecastWorkspaces.Clear();
        foreach (var reference in _publishedSavedComparisons)
            if (reference.TryGetTarget(out var comparison)) comparison.RetireShellPublication();
        _publishedSavedComparisons.Clear();
        foreach (var reference in _publishedHoldingDetails)
            if (reference.TryGetTarget(out var detail)) detail.RetireShellPublication();
        _publishedHoldingDetails.Clear();
        foreach (var reference in _publishedAssetEventEditors)
            if (reference.TryGetTarget(out var editor)) editor.RetireShellPublication();
        _publishedAssetEventEditors.Clear();
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
