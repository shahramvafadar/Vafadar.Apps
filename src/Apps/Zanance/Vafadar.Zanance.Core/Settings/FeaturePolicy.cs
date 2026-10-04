namespace Vafadar.Zanance.Core.Settings;

/// <summary>A part of the app whose presentation depends on the experience mode (05 §2, ZEX-S0501).</summary>
public enum Feature
{
    // Never hidden in Simple (05 §1 rule 3); listed so that the table is the complete audit.

    /// <summary>Balances and totals in their own currency.</summary>
    NativeTotals,

    /// <summary>Correcting a balance and seeing the difference (SA25).</summary>
    Reconcile,

    /// <summary>Financial warnings: shortfall, overdue, headroom below zero, old backup.</summary>
    Warnings,

    /// <summary>App lock, backup, restore and deleting data (SA27).</summary>
    Security,

    /// <summary>The basic CSV export and import (SA26).</summary>
    BasicExport,

    /// <summary>The data status and the reason of any incomplete number (SA23).</summary>
    DataStatus,

    /// <summary>The guided period-end review.</summary>
    PeriodReview,

    /// <summary>Holdings that exist, with their quantity (SA20).</summary>
    HoldingsList,

    // Simple shows a summary of existing data; Advanced shows every control.

    /// <summary>Payee, note, foreign amount and tags directly in the entry editor (SA03).</summary>
    EntryDetails,

    /// <summary>Custom recurrence, month-day rules, end and the second reminder of a plan (SA13–SA15).</summary>
    PlanRules,

    /// <summary>Budget method, rollover and category limits (SA16).</summary>
    BudgetOptions,

    /// <summary>Weekly and two-week budget periods (SA17).</summary>
    BudgetPeriods,

    /// <summary>The goal trend per period, capacity in a quantity and essential coverage (SA19).</summary>
    GoalDetails,

    /// <summary>The forecast end and lowest point on Home, scenarios and snapshots (SA24).</summary>
    ForecastDetails,

    /// <summary>Long lists, ratios, composition and wealth change in the reports (SA23).</summary>
    ReportDetails,

    /// <summary>Asset types, places, valuations and purity of holdings (SA20).</summary>
    HoldingDetails,

    // Created and edited in Advanced only; existing data stays visible in Simple.

    /// <summary>Groups, include in totals, usable for payments and country of an account (SA02).</summary>
    AccountOptions,

    /// <summary>One converted total under the native ones (SA05).</summary>
    ConvertedTotals,

    /// <summary>Valuation currency, rate freshness and the essential-spending estimate in Settings (SA05).</summary>
    MoneySettings,

    /// <summary>Turning an asset account into holdings (SA20).</summary>
    AccountConversion,

    /// <summary>Recording purchases, sales and moves of holdings; Simple records corrections (SA20).</summary>
    HoldingEvents,

    /// <summary>The holdings CSV export (SA26).</summary>
    HoldingsExport,

    /// <summary>Goal types other than a balance goal, and contribution methods (SA18).</summary>
    GoalOptions,

    /// <summary>Creating an aggregated entry for a date range (SA03).</summary>
    AggregatedEntries,

    /// <summary>A fee in the destination currency of a transfer (SA04).</summary>
    TransferFee,
}

/// <summary>What Simple shows of a feature (05 §1).</summary>
public enum FeatureVisibility
{
    /// <summary>Shown in both modes.</summary>
    Always,

    /// <summary>Simple shows a summary when data exists, details one tap away.</summary>
    Summary,

    /// <summary>Simple shows nothing unless data exists, then a summary.</summary>
    Advanced,
}

/// <summary>One row of the policy table: the feature, its area of the matrix (05 §2) and what Simple shows.</summary>
public sealed record FeatureRule(Feature Feature, string Area, FeatureVisibility Simple);

/// <summary>
/// The single Simple/Advanced policy (05 §1 rule 5, ZEX-S0501): pages ask it instead of checking the mode themselves. The
/// mode only decides what is shown, never what is calculated; calculators do not take the mode (ZEX-AT28).
/// </summary>
public static class FeaturePolicy
{
    /// <summary>Gets the policy table, one row per <see cref="Feature"/>.</summary>
    public static IReadOnlyList<FeatureRule> Rules { get; } =
    [
        new(Feature.NativeTotals, "SA05", FeatureVisibility.Always),
        new(Feature.Reconcile, "SA25", FeatureVisibility.Always),
        new(Feature.Warnings, "SA14", FeatureVisibility.Always),
        new(Feature.Security, "SA27", FeatureVisibility.Always),
        new(Feature.BasicExport, "SA26", FeatureVisibility.Always),
        new(Feature.DataStatus, "SA23", FeatureVisibility.Always),
        new(Feature.PeriodReview, "SA23", FeatureVisibility.Always),
        new(Feature.HoldingsList, "SA20", FeatureVisibility.Always),
        new(Feature.EntryDetails, "SA03", FeatureVisibility.Summary),
        new(Feature.PlanRules, "SA13", FeatureVisibility.Summary),
        new(Feature.BudgetOptions, "SA16", FeatureVisibility.Summary),
        new(Feature.BudgetPeriods, "SA17", FeatureVisibility.Summary),
        new(Feature.GoalDetails, "SA19", FeatureVisibility.Summary),
        new(Feature.ForecastDetails, "SA24", FeatureVisibility.Summary),
        new(Feature.ReportDetails, "SA23", FeatureVisibility.Summary),
        new(Feature.HoldingDetails, "SA20", FeatureVisibility.Summary),
        new(Feature.AccountOptions, "SA02", FeatureVisibility.Advanced),
        new(Feature.ConvertedTotals, "SA05", FeatureVisibility.Advanced),
        new(Feature.MoneySettings, "SA05", FeatureVisibility.Advanced),
        new(Feature.AccountConversion, "SA20", FeatureVisibility.Advanced),
        new(Feature.HoldingEvents, "SA20", FeatureVisibility.Advanced),
        new(Feature.HoldingsExport, "SA26", FeatureVisibility.Advanced),
        new(Feature.GoalOptions, "SA18", FeatureVisibility.Advanced),
        new(Feature.AggregatedEntries, "SA03", FeatureVisibility.Advanced),
        new(Feature.TransferFee, "SA04", FeatureVisibility.Advanced),
    ];

    private static readonly Dictionary<Feature, FeatureRule> ByFeature = Rules.ToDictionary(r => r.Feature);

    /// <summary>Returns the rule of a feature.</summary>
    public static FeatureRule RuleOf(Feature feature) => ByFeature[feature];

    /// <summary>Returns whether the full controls of a feature are shown in the given mode.</summary>
    public static bool Shows(Feature feature, ExperienceMode mode) =>
        mode == ExperienceMode.Advanced || RuleOf(feature).Simple == FeatureVisibility.Always;

    /// <summary>Returns whether the full controls of a feature are shown with these settings.</summary>
    public static bool Shows(this ZananceSettings settings, Feature feature)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return Shows(feature, settings.Mode);
    }

    /// <summary>
    /// Returns whether a feature is shown at least as a summary: always in Advanced, and in Simple whenever data of it
    /// exists, so nothing disappears when switching (05 §1 rule 2, ZEX-S0502).
    /// </summary>
    public static bool ShowsExisting(this ZananceSettings settings, Feature feature, bool hasData) =>
        settings.Shows(feature) || hasData;
}
