using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Vafadar.Backup;

/// <summary>
/// Registers backup services.
/// </summary>
public static class BackupServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IBackupService"/>. Register the app's <see cref="IBackupSource"/>s and <see cref="IBackupStorage"/>s
    /// separately. Requires <see cref="Core.Hosting.IAppEnvironment"/> and <see cref="Core.Settings.ISettingsStore"/>.
    /// </summary>
    public static IServiceCollection AddVafadarBackup(this IServiceCollection services, Action<BackupOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new BackupOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IBackupService, BackupService>();

        return services;
    }
}
