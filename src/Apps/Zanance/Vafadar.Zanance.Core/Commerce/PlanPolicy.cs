namespace Vafadar.Zanance.Core.Commerce;

/// <summary>Commercial operations from the owner-approved matrix; eligibility does not imply platform readiness.</summary>
public enum CommercialFeature
{
    /// <summary>Languages, calendars, holidays, currencies, digits and formatting.</summary>
    RegionalDisplay,
    /// <summary>App lock, encryption and data protection whenever implemented.</summary>
    Security,
    /// <summary>Themes, accessibility and both experience modes.</summary>
    Appearance,
    /// <summary>Unlimited ordinary ledger recording, within resource quotas.</summary>
    Transactions,
    /// <summary>Viewing all existing data and its calculations.</summary>
    History,
    /// <summary>Corrections, reconciliation, overdue settlement and closing a loan.</summary>
    Corrections,
    /// <summary>Essential financial and incomplete-data warnings.</summary>
    Warnings,
    /// <summary>Backup and restore, including data above quotas.</summary>
    BackupRestore,
    /// <summary>Deleting owned data.</summary>
    DeleteData,
    /// <summary>Basic CSV and the user's own data export.</summary>
    BasicExport,
    /// <summary>Optional backup to the user's Drive or OneDrive.</summary>
    CloudBackup,
    /// <summary>Creating personal financial accounts within their quota.</summary>
    FinancialAccounts,
    /// <summary>Monthly limits budgets within their quota.</summary>
    BasicBudgets,
    /// <summary>Account-balance goals within their quota.</summary>
    BasicGoals,
    /// <summary>Basic recurring plans within their quota.</summary>
    BasicPlans,
    /// <summary>Basic reminders for eligible plans.</summary>
    PlanReminders,
    /// <summary>Income, expense and balance reports.</summary>
    BasicReports,
    /// <summary>Basic forecast through the current financial month end.</summary>
    BasicForecast,
    /// <summary>The financial month start setting.</summary>
    FinancialMonthStart,
    /// <summary>Quick templates within their quota.</summary>
    QuickTemplates,
    /// <summary>Tags, manually attached files, notes and search.</summary>
    EntryDocumentation,
    /// <summary>Bulk archive, delete and categorize.</summary>
    BulkCorrections,
    /// <summary>Saved filters within their quota.</summary>
    SavedFilters,
    /// <summary>Home customization.</summary>
    HomeCustomization,
    /// <summary>Currency display units, including Toman.</summary>
    DisplayUnits,
    /// <summary>Reimbursements, refunds, transfers across currencies and fees.</summary>
    TransfersRefundsReimbursements,
    /// <summary>Aggregate recording.</summary>
    AggregatedEntries,
    /// <summary>Generic CSV mapping and migration presets.</summary>
    CsvImport,
    /// <summary>The Android quick-add widget when available.</summary>
    QuickAddWidget,
    /// <summary>Local profiles within the device quota.</summary>
    LocalProfiles,
    /// <summary>Envelope/flex methods, rollover and weekly/two-week budgets.</summary>
    AdvancedBudgets,
    /// <summary>Earmark/quantity goals and contribution plans.</summary>
    AdvancedGoals,
    /// <summary>Advanced recurrence, contracts and automatic posting.</summary>
    AdvancedPlans,
    /// <summary>Wealth analysis, KPIs, commitments and advanced history reports.</summary>
    AdvancedReports,
    /// <summary>Advanced PDF reporting, distinct from exporting owned data.</summary>
    AdvancedPdf,
    /// <summary>Detailed forecasts, scenarios, saved forecasts and comparison.</summary>
    AdvancedForecast,
    /// <summary>Creating holdings, purchases/sales and price entries.</summary>
    ManageHoldings,
    /// <summary>Viewing existing holdings and their quantities, prices and history.</summary>
    ViewHoldings,
    /// <summary>Loan rate and repayment schedule analysis.</summary>
    LoanAnalysis,
    /// <summary>Creating split transactions.</summary>
    SplitTransactions,
    /// <summary>Automatic categorization rules.</summary>
    CategorizationRules,
    /// <summary>On-device receipt recognition.</summary>
    ReceiptReading,
    /// <summary>Goal-contribution and period-review reminders.</summary>
    ContributionReviewReminders,
    /// <summary>Syncing one's own personal data, never implied by cloud backup.</summary>
    PersonalSync,
    /// <summary>Hosting a shared space within the identity quota.</summary>
    HostSharedSpace,
    /// <summary>Joining the exact invited space after membership authorization.</summary>
    JoinSharedSpace,
    /// <summary>Syncing only the exact authorized shared space.</summary>
    SharedSpaceSync,
    /// <summary>Separate AI credits, not included in a tier.</summary>
    AiCredit,
    /// <summary>A separate country/year tax add-on.</summary>
    TaxAddOn,
    /// <summary>A separately approved bank-connection add-on.</summary>
    BankConnection,
}

/// <summary>The reason a commercial operation is eligible or needs another right.</summary>
public enum FeaturePermission
{
    /// <summary>Eligible, still subject to quotas, authorization and platform readiness.</summary>
    Allowed,
    /// <summary>Requires the local Plus capability set.</summary>
    RequiresPlus,
    /// <summary>Requires personal Pro services.</summary>
    RequiresPro,
    /// <summary>Requires active membership in this exact active Pro owner's space.</summary>
    RequiresMembership,
    /// <summary>Requires a separately approved add-on; no tier includes it.</summary>
    RequiresAddOn,
    /// <summary>The requested service belongs to a different scope.</summary>
    OutsideScope,
}

/// <summary>Pure owner-approved capability policy (D-117), separate from presentation and future enforcement.</summary>
public static class PlanPolicy
{
    /// <summary>Returns eligibility; unknown values fail explicitly instead of granting access.</summary>
    public static FeaturePermission Check(CommercialFeature feature, CapabilityContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!Enum.IsDefined(feature)) throw new ArgumentOutOfRangeException(nameof(feature));
        var shared = context.Scope.Kind == EntitlementScopeKind.SharedSpace;
        if (shared && !context.HasSharedMembership) return FeaturePermission.RequiresMembership;
        // Payment expiry never blocks retained history, corrections or export. Server roles/retention remain
        // independent; this does not invent the still-open OD-05 grace period or authorize a network request.
        if (shared && !context.HasSharedAccess && !PreservesExistingData(feature)) return FeaturePermission.RequiresPro;
        return feature switch
        {
            CommercialFeature.AiCredit or CommercialFeature.TaxAddOn or CommercialFeature.BankConnection
                => FeaturePermission.RequiresAddOn,
            CommercialFeature.PersonalSync or CommercialFeature.HostSharedSpace
                => shared ? FeaturePermission.OutsideScope : context.PersonalPlan == ProductPlan.Pro
                    ? FeaturePermission.Allowed : FeaturePermission.RequiresPro,
            CommercialFeature.JoinSharedSpace or CommercialFeature.SharedSpaceSync
                => shared ? FeaturePermission.Allowed : FeaturePermission.OutsideScope,
            CommercialFeature.AdvancedBudgets or CommercialFeature.AdvancedGoals or CommercialFeature.AdvancedPlans
                or CommercialFeature.AdvancedReports or CommercialFeature.AdvancedPdf or CommercialFeature.AdvancedForecast
                or CommercialFeature.ManageHoldings or CommercialFeature.LoanAnalysis or CommercialFeature.SplitTransactions
                or CommercialFeature.CategorizationRules or CommercialFeature.ReceiptReading or CommercialFeature.ContributionReviewReminders
                => context.LocalPlan >= ProductPlan.Plus ? FeaturePermission.Allowed : FeaturePermission.RequiresPlus,
            // Creating a personal profile is a device operation: guest Plus never spreads outside the shared space.
            CommercialFeature.LocalProfiles => shared ? FeaturePermission.OutsideScope : FeaturePermission.Allowed,
            CommercialFeature.RegionalDisplay or CommercialFeature.Security or CommercialFeature.Appearance
                or CommercialFeature.Transactions or CommercialFeature.History or CommercialFeature.Corrections
                or CommercialFeature.ViewHoldings or CommercialFeature.Warnings or CommercialFeature.BackupRestore or CommercialFeature.DeleteData
                or CommercialFeature.BasicExport or CommercialFeature.CloudBackup or CommercialFeature.FinancialAccounts
                or CommercialFeature.BasicBudgets or CommercialFeature.BasicGoals or CommercialFeature.BasicPlans
                or CommercialFeature.PlanReminders or CommercialFeature.BasicReports or CommercialFeature.BasicForecast
                or CommercialFeature.FinancialMonthStart or CommercialFeature.QuickTemplates or CommercialFeature.EntryDocumentation
                or CommercialFeature.BulkCorrections or CommercialFeature.SavedFilters or CommercialFeature.HomeCustomization
                or CommercialFeature.DisplayUnits or CommercialFeature.TransfersRefundsReimbursements or CommercialFeature.AggregatedEntries
                or CommercialFeature.CsvImport or CommercialFeature.QuickAddWidget => FeaturePermission.Allowed,
            _ => throw new ArgumentOutOfRangeException(nameof(feature)),
        };
    }

    /// <summary>Identifies rights retained when the shared host's paid services expire.</summary>
    private static bool PreservesExistingData(CommercialFeature feature) => feature is
        CommercialFeature.RegionalDisplay or CommercialFeature.Security or CommercialFeature.Appearance
        or CommercialFeature.History or CommercialFeature.Corrections or CommercialFeature.Warnings
        or CommercialFeature.BackupRestore or CommercialFeature.DeleteData or CommercialFeature.BasicExport
        or CommercialFeature.CloudBackup or CommercialFeature.BasicReports or CommercialFeature.ViewHoldings;
}
