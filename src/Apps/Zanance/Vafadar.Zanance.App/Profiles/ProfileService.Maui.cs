using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Maui.Security;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Profiles;

public sealed partial class ProfileService
{
    /// <summary>Returns the last selected native profile database before dependency injection is built.</summary>
    public static string StartupDatabasePath(string mainPath) => StartupDatabasePath(mainPath, new ProfilePreferences());

    /// <summary>Returns the native backup set at the instant a backup operation starts.</summary>
    public static string? CurrentBackupSet() => CurrentBackupSet(new ProfilePreferences());
}

/// <summary>Native preference access needed by the pre-container profile path and deferred backup-set callbacks.</summary>
internal sealed class ProfilePreferences : ISettingsStore
{
    /// <inheritdoc />
    public string? Get(string key) => Preferences.Default.Get<string?>(key, null);
    /// <inheritdoc />
    public void Set(string key, string? value) { if (value is null) { Remove(key); } else { Preferences.Default.Set(key, value); } }
    /// <inheritdoc />
    public void Remove(string key) => Preferences.Default.Remove(key);
}

/// <summary>Existing MAUI authentication and data services used by the platform-independent profile flow.</summary>
internal sealed class MauiProfileRuntime(IServiceProvider services, AutoPostProcessor autoPost, AppLockService appLock, UndoService undo) : IProfileRuntime
{
    /// <inheritdoc />
    public Task RunExclusiveAsync(Func<Task> action) => autoPost.RunExclusiveAsync(action);
    /// <inheritdoc />
    public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => services.MigrateLocalDatabaseForMaintenanceAsync<ZananceDbContext>(owner);
    /// <inheritdoc />
    public Task PrepareNewDatabaseAsync(LocalDatabaseLocation<ZananceDbContext> location, string profileId, string originalPath,
        DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct)
    {
        if (services.GetService<SqlCipherProfileSession<ZananceDbContext>>() is { } session)
            return session.PrepareNewAsync(profileId, originalPath, owner, ct);
        ct.ThrowIfCancellationRequested(); _ = location.CapturePublication(owner);
        if (location.IsProtected) throw new InvalidOperationException("Protected profile preparation is unavailable.");
        return Task.CompletedTask;
    }
    /// <inheritdoc />
    public async Task OpenDatabaseAsync(LocalDatabaseLocation<ZananceDbContext> location, string originalPath, DatabaseAccessGate.ExclusiveLease owner)
    {
        if (services.GetService<SqlCipherProfileSession<ZananceDbContext>>() is { } protectedSession)
            await protectedSession.BeginSwitchAsync(originalPath, owner);
        else
        {
            // Existing plaintext hosts retain their original behavior. Protected locations refuse this route.
            location.MoveTo(originalPath, owner);
        }
        await MigrateAsync(owner);
    }
    /// <inheritdoc />
    public void FinishDatabaseTransition(bool accepted, DatabaseAccessGate.ExclusiveLease owner) =>
        services.GetService<SqlCipherProfileSession<ZananceDbContext>>()?.FinishSwitch(accepted, owner);
    /// <inheritdoc />
    public async Task<bool> AuthenticateAsync(string reason) => await appLock.Authenticator.AuthenticateAsync(reason) == AuthenticationOutcome.Success;
    /// <inheritdoc />
    public Task ReloadLockAsync() => appLock.ReloadAsync();
    /// <inheritdoc />
    public void DismissUndo() => undo.Dismiss();
}
