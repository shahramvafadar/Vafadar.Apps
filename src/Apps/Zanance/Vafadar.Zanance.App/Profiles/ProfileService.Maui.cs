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
    public Task MigrateAsync() => services.MigrateLocalDatabaseAsync<ZananceDbContext>();
    /// <inheritdoc />
    public async Task<bool> AuthenticateAsync(string reason) => await appLock.Authenticator.AuthenticateAsync(reason) == AuthenticationOutcome.Success;
    /// <inheritdoc />
    public Task ReloadLockAsync() => appLock.ReloadAsync();
    /// <inheritdoc />
    public void DismissUndo() => undo.Dismiss();
}
