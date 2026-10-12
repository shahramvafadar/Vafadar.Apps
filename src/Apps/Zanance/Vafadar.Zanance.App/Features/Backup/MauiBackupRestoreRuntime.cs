using Vafadar.Backup;
using Vafadar.Backup.Storage;
using Vafadar.Localization;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>Original native consent, safety/source recovery and UI/security completion for one attached backup page.</summary>
internal sealed class MauiBackupRestoreRuntime(IBackupService backup, AutoPostProcessor autoPost, ReminderService reminders,
    AppLockService appLock, ILocalizationService localization, ZananceStore store, LocalFolderBackupStorage safety,
    Func<Page> page, Func<Task> returnOnboarding) : IBackupRestoreRuntime
{
    private bool _returnedOnboarding;
    /// <inheritdoc />
    public Task<bool> ConfirmReplacementAsync() => page().DisplayAlertAsync(Vafadar.Localization.Translator.Instance["Backup_ReplaceTitle"],
        Vafadar.Localization.Translator.Instance["Backup_ReplaceMessage"], Vafadar.Localization.Translator.Instance["Backup_Replace"], Vafadar.Localization.Translator.Instance["Common_Cancel"]);
    /// <inheritdoc />
    public Task<bool> AuthenticateAsync() => appLock.ConfirmAsync(Vafadar.Localization.Translator.Instance["Lock_ConfirmRestore"]);
    /// <inheritdoc />
    public async Task CreateSafetyCopyAsync() => await backup.CreateSafetyCopyAsync(safety);
    /// <inheritdoc />
    public Task RestorePrimaryAsync(byte[] package, string? password) => autoPost.RunExclusiveAsync(() => backup.RestorePackageAsync(package, password));
    /// <inheritdoc />
    public Task ApplyDisplayAsync() { localization.Initialize(); Presentation.DigitPreferences.Apply(localization); return Task.CompletedTask; }
    /// <inheritdoc />
    public Task CompleteOnboardingAsync() => store.CompleteRestoredOnboardingAsync();
    /// <inheritdoc />
    public Task ReloadLockAsync() => appLock.ReloadAsync();
    /// <inheritdoc />
    public async Task RefreshFinancialWorkAsync(DateOnly day) { await autoPost.RunAsync(day); await reminders.RefreshAsync(); }
    /// <inheritdoc />
    public async Task OpenRestoredProfileAsync(bool onboarding)
    {
        if (onboarding && !_returnedOnboarding) { await returnOnboarding(); _returnedOnboarding = true; }
        if (Application.Current is not App app) throw new InvalidOperationException("The restored application window is unavailable.");
        app.ShowCurrentProfile();
    }
}
