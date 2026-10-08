using Vafadar.Core.Domain;
using Vafadar.Zanance.Core.Budgets;

namespace Vafadar.Zanance.Core.Settings;

/// <summary>Level of detail shown in the UI; unrelated to Free/Pro (UX-01, MON-05).</summary>
public enum ExperienceMode
{
    /// <summary>Few, essential controls.</summary>
    Simple = 0,

    /// <summary>All controls directly available.</summary>
    Advanced = 1,
}

/// <summary>
/// Zanance-specific settings. Stored in the database (one row) so that backups contain them (BAK-03).
/// UI language and display calendar are app-wide preferences of Vafadar.Localization.
/// </summary>
public sealed class ZananceSettings : Entity, IAuditableEntity
{
    /// <summary>
    /// Gets or sets the valuation currency (shown as *Valuation currency*; the property keeps its old name): the currency
    /// of converted totals and converted charts only. It is never the default of new items (ZEX-P01).
    /// </summary>
    public string ReportCurrencyCode { get; set; } = "EUR";

    /// <summary>
    /// Gets or sets the currency preselected for new accounts, goals, budgets, rates and display units (ZEX-P01).
    /// Changing it never changes existing data or the currency of an entry, which is always its account's.
    /// </summary>
    public string DefaultCurrencyCode { get; set; } = "EUR";

    /// <summary>
    /// Gets or sets a value indicating whether converted totals are shown at all (Advanced may switch the valuation
    /// currency off; then only native totals appear).
    /// </summary>
    public bool ValuationCurrencyEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets after how many days a rate may be outdated (7, 30 or 90; 0 = never), see
    /// <see cref="Rates.RateFreshness"/> (ZEX-P06).
    /// </summary>
    public int RateFreshnessDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the currency of the budget shown on Home and first on the Budget page; <see langword="null"/> = the
    /// currency of the default account (ZEX-P02).
    /// </summary>
    public string? HomeBudgetCurrencyCode { get; set; }

    /// <summary>
    /// Gets or sets the display units of this profile (<c>CODE|name|exponent;…</c>, FX-07). Stored here so that they belong
    /// to the profile and are in its backups (ZEX-P20); <see langword="null"/> = not copied from the device yet.
    /// </summary>
    public string? DisplayUnits { get; set; }

    /// <summary>Gets or sets the Home layout of this profile (<see cref="Dashboard.HomeLayout"/> text, ZEX-P20); <see langword="null"/> = not set.</summary>
    public string? HomeLayout { get; set; }

    /// <summary>
    /// Gets or sets the explicit estimate of day-to-day essential spending used by the liquidity headroom (04 §3) in minor
    /// units of <see cref="EssentialEstimateCurrency"/> per <see cref="EssentialEstimatePeriod"/>; <see langword="null"/> = not set,
    /// then the forecast says "Day-to-day spending not included". Zanance only suggests a value, never sets it.
    /// </summary>
    public long? EssentialEstimate { get; set; }

    /// <summary>Gets or sets the period of <see cref="EssentialEstimate"/>.</summary>
    public EstimatePeriod EssentialEstimatePeriod { get; set; }

    /// <summary>Gets or sets the currency of <see cref="EssentialEstimate"/>.</summary>
    public string? EssentialEstimateCurrency { get; set; }

    /// <summary>
    /// Gets or sets the progress of the period-end review (ZEX-S0610): "2026-09" for a finished review of that financial
    /// month, or "2026-09:unreviewed,plans" with the steps done so far.
    /// </summary>
    public string? ReviewProgress { get; set; }

    /// <summary>Gets or sets the optional period-end review reminder; off until explicitly enabled in this profile.</summary>
    public bool ReviewReminderEnabled { get; set; }

    /// <summary>Gets or sets the account preselected in the entry form (ACC-02).</summary>
    public Guid? DefaultAccountId { get; set; }

    /// <summary>Gets or sets the experience mode.</summary>
    public ExperienceMode Mode { get; set; }

    /// <summary>Gets or sets the calendar of new budgets (default Gregorian, §13.1).</summary>
    public PeriodCalendar BudgetCalendar { get; set; }

    /// <summary>
    /// Gets or sets the day the financial month starts on (1–28, §10.3 pay-cycle periods). Budgets, Home, reports and the
    /// transaction filters use this month; plans keep their calendar dates.
    /// </summary>
    public int MonthStartDay { get; set; } = 1;

    /// <summary>
    /// Gets or sets a day on which a two-week budget period starts (e.g. a payday); the periods repeat every 14 days from
    /// it. <see langword="null"/> = the start of the current week.
    /// </summary>
    public DateOnly? FortnightStart { get; set; }

    /// <summary>Gets or sets the default reminder lead time in days (REM-01, default 3).</summary>
    public int ReminderDaysBefore { get; set; } = 3;

    /// <summary>Gets or sets the default reminder time of day (default 09:00).</summary>
    public TimeOnly ReminderTime { get; set; } = new(9, 0);

    /// <summary>Gets or sets a value indicating whether notifications may show amounts and names (REM-05, default off).</summary>
    public bool NotificationsShowDetails { get; set; }

    /// <summary>Gets or sets a value indicating whether the app lock is enabled (SEC-01).</summary>
    public bool AppLockEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether onboarding was completed.</summary>
    public bool OnboardingCompleted { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>The period of an amount estimate. New values are appended, never renumbered.</summary>
public enum EstimatePeriod
{
    /// <summary>Per day.</summary>
    Day = 0,

    /// <summary>Per week.</summary>
    Week = 1,

    /// <summary>Per financial month.</summary>
    Month = 2,
}
