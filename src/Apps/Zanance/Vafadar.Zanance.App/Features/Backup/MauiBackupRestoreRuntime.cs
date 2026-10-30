using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Backup.Storage;
using Vafadar.Localization;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Backup;

/// <summary>Original native consent, safety/source recovery and UI/security completion for one attached backup page.</summary>
internal sealed class MauiBackupRestoreRuntime(IBackupService backup, AutoPostProcessor autoPost, ReminderService reminders,
    AppLockService appLock, ILocalizationService localization, ZananceStore store, LocalFolderBackupStorage safety,
    Func<Page> page, Func<Task> returnOnboarding, IBackupSafetyCopyMaintenance? maintenance = null, LocalDatabaseLocation<ZananceDbContext>? location = null) : IBackupRestoreRuntime
{
    private bool _returnedOnboarding;
    /// <inheritdoc />
    public Action CaptureSourceValidation()
    {
        var publication = location?.CaptureValidation()
            ?? throw new InvalidOperationException("The original restore publication is unavailable.");
        return () =>
        {
            publication();
            if (appLock.IsLocked) throw new InvalidOperationException("The original restore is covered.");
        };
    }
    /// <inheritdoc />
    public Task<bool> ConfirmReplacementAsync() => page().DisplayAlertAsync(Vafadar.Localization.Translator.Instance["Backup_ReplaceTitle"],
        Vafadar.Localization.Translator.Instance["Backup_ReplaceMessage"], Vafadar.Localization.Translator.Instance["Backup_Replace"], Vafadar.Localization.Translator.Instance["Common_Cancel"]);
    /// <inheritdoc />
    public Task<bool> AuthenticateAsync() => appLock.ConfirmAsync(Vafadar.Localization.Translator.Instance["Lock_ConfirmRestore"]);
    /// <inheritdoc />
    public Task CreateSafetyCopyAsync() => CreateSafetyCopyAsync(static () => { }, CancellationToken.None);
    /// <inheritdoc />
    public Task CreateSafetyCopyAsync(Action validateOwner, CancellationToken ct) => autoPost.RunExclusiveAsync(async () =>
    {
        ct.ThrowIfCancellationRequested(); validateOwner();
        var file = await backup.CreateSafetyCopyAsync(safety, ct);
        if (maintenance is not null) await maintenance.AfterUploadAsync(file);
    }, ct);
    /// <inheritdoc />
    public Task RestorePrimaryAsync(byte[] package, string? password) =>
        RestorePrimaryAsync(package, password, static () => { }, CancellationToken.None);
    /// <inheritdoc />
    public Task RestorePrimaryAsync(byte[] package, string? password, Action validateOwner, CancellationToken ct) => autoPost.RunExclusiveAsync(async () =>
    {
        ct.ThrowIfCancellationRequested(); validateOwner();
        using var original = BackupRestorePublication.Enter(validateOwner);
        await backup.RestorePackageAsync(package, password, ct);
        // No post-commit guard: a completed original replacement must remain a known accepted result.
    }, ct);
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
