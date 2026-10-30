using Vafadar.Localization;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Settings;

/// <summary>One complete settings read, including suggestions and device availability, ready for synchronous presentation.</summary>
/// <param name="Settings">The existing profile preferences; this read does not update them.</param>
/// <param name="DefaultAccounts">Unarchived accounts eligible as an entry default, in display order.</param>
/// <param name="EssentialSuggestion">The existing suggestion calculation, never a saved estimate.</param>
/// <param name="LockAvailable">Whether the existing device-authentication choice can be shown.</param>
/// <param name="NotificationsEnabled">Whether notifications are currently allowed, without asking permission.</param>
internal sealed record SettingsSnapshot(ZananceSettings Settings, IReadOnlyList<Account> DefaultAccounts,
    long? EssentialSuggestion, bool LockAvailable, bool NotificationsEnabled, EssentialEstimateReview EstimateReview, SettingsWorkSnapshot WorkReview)
{
    /// <summary>Completes all asynchronous reads before any editable field is published (D-79).</summary>
    internal static async Task<SettingsSnapshot> ReadAsync(ZananceStore store, ILocalizationService localization,
        TimeProvider time, Func<Task<bool>> lockAvailable, bool notificationsSupported, Func<Task<bool>> notificationsEnabled,
        CancellationToken cancellationToken = default, Func<string, bool>? acceptsFile = null)
    {
        var calendar = Presentation.Calendars.ToPeriod(localization.CurrentCalendar);
        var work = await store.ReadSettingsWorkSnapshotAsync(calendar, acceptsFile, cancellationToken);
        var settings = work.Settings; var accounts = work.Accounts;
        var currency = settings.EssentialEstimateCurrency ?? settings.DefaultCurrencyCode;
        var suggestion = await Task.Run(() => Core.Reports.LiquidityCalculator.SuggestPerDay(accounts, work.Entries, currency,
            work.Day, calendar, settings.MonthStartDay), cancellationToken);
        var available = settings.AppLockEnabled || await lockAvailable().WaitAsync(cancellationToken);
        var enabled = notificationsSupported && await notificationsEnabled().WaitAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (work.Day != DateOnly.FromDateTime(time.GetLocalNow().DateTime) || calendar != Presentation.Calendars.ToPeriod(localization.CurrentCalendar)) throw new InvalidOperationException("The original settings calendar changed.");
        await store.ValidateSettingsWorkSnapshotAsync(work, cancellationToken);
        return new(settings, [.. accounts.Where(a => !a.IsArchived && EntryAccountContract.IsValidDefault(a))],
            suggestion, available, enabled, work.EstimateReview, work);
    }
}
