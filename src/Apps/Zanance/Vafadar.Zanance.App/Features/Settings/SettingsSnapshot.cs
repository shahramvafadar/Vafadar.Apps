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
    long? EssentialSuggestion, bool LockAvailable, bool NotificationsEnabled)
{
    /// <summary>Completes all asynchronous reads before any editable field is published (D-79).</summary>
    internal static async Task<SettingsSnapshot> ReadAsync(ZananceStore store, ILocalizationService localization,
        TimeProvider time, Func<Task<bool>> lockAvailable, bool notificationsSupported, Func<Task<bool>> notificationsEnabled,
        CancellationToken cancellationToken = default)
    {
        var settings = await store.GetSettingsAsync(cancellationToken);
        var accounts = await store.GetAccountsAsync(cancellationToken: cancellationToken);
        var entries = await store.GetEntriesAsync(cancellationToken: cancellationToken);
        var currency = settings.EssentialEstimateCurrency ?? settings.DefaultCurrencyCode;
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var suggestion = Core.Reports.LiquidityCalculator.SuggestPerDay(accounts, entries, currency, today,
            Presentation.Calendars.ToPeriod(localization.CurrentCalendar), settings.MonthStartDay);
        var available = settings.AppLockEnabled || await lockAvailable();
        var enabled = notificationsSupported && await notificationsEnabled();
        cancellationToken.ThrowIfCancellationRequested();
        return new(settings, [.. accounts.Where(a => !a.IsArchived && EntryAccountContract.IsValidDefault(a))],
            suggestion, available, enabled);
    }
}
