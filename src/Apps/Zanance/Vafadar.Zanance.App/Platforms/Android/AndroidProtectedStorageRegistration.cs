using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Registers one lazy native storage owner for the existing application contracts.</summary>
internal static class AndroidProtectedStorageRegistration
{
    /// <summary>Replaces plaintext startup with explicit covered native authority; construction never opens storage.</summary>
    internal static void AddAndroidProtectedStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<AndroidProtectedInstallation>();
        services.AddSingleton<AndroidBackupRecovery>();
        services.AddSingleton(provider =>
        {
            var installation = provider.GetRequiredService<AndroidProtectedInstallation>();
            return new AndroidProtectedStorageHost(provider, installation.BeginAsync, installation.CompleteAsync);
        });
        services.AddSingleton<IAppDatabaseStartup>(provider => provider.GetRequiredService<AndroidProtectedStorageHost>());
        services.AddSingleton<IProfileCreationHost>(provider => provider.GetRequiredService<AndroidProtectedStorageHost>());
        services.AddSingleton<IProfileRetirementHost>(provider => provider.GetRequiredService<AndroidProtectedStorageHost>());
        services.AddSingleton<IBackupPackageRestoreReceiver>(provider => provider.GetRequiredService<AndroidProtectedStorageHost>());
        services.AddSingleton<IBackupSafetyCopyProtection>(provider => provider.GetRequiredService<AndroidProtectedStorageHost>());
        services.AddSingleton<IBackupSafetyCopyMaintenance>(provider => provider.GetRequiredService<AndroidProtectedStorageHost>());
        services.AddSingleton<IProtectedProfileSessionSource>(provider => provider.GetRequiredService<AndroidProtectedStorageHost>());
    }
}
