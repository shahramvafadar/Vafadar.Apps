using Vafadar.Core.Domain;
using Vafadar.Finance.Core.Budgets;

namespace Vafadar.Finance.Core.Settings;

/// <summary>Level of detail shown in the UI; unrelated to Free/Pro (UX-01, MON-05).</summary>
public enum ExperienceMode
{
    /// <summary>Few, essential controls.</summary>
    Simple = 0,

    /// <summary>All controls directly available.</summary>
    Advanced = 1,
}

/// <summary>
/// Finance-specific settings. Stored in the database (one row) so that backups contain them (BAK-03).
/// UI language and display calendar are app-wide preferences of Vafadar.Localization.
/// </summary>
public sealed class FinanceSettings : Entity, IAuditableEntity
{
    /// <summary>Gets or sets the report currency.</summary>
    public string ReportCurrencyCode { get; set; } = "EUR";

    /// <summary>Gets or sets the account preselected in the entry form (ACC-02).</summary>
    public Guid? DefaultAccountId { get; set; }

    /// <summary>Gets or sets the experience mode.</summary>
    public ExperienceMode Mode { get; set; }

    /// <summary>Gets or sets the calendar of new budgets (default Gregorian, §13.1).</summary>
    public PeriodCalendar BudgetCalendar { get; set; }

    /// <summary>Gets or sets the first day of the week.</summary>
    public DayOfWeek WeekStart { get; set; } = DayOfWeek.Monday;

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
