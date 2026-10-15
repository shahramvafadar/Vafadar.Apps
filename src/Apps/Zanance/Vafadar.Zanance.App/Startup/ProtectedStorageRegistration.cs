using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;

namespace Vafadar.Zanance.App.Security;

/// <summary>Publishes one lazy native owner through the original application storage contracts.</summary>
internal static class ProtectedStorageRegistration
{
    /// <summary>Registers shared contracts without resolving or opening any native financial/key storage.</summary>
    internal static void AddProtectedStorageContracts(this IServiceCollection services)
    {
        services.AddSingleton<IProtectedBackupRecovery>(provider => provider.GetRequiredService<ProtectedBackupRecovery>());
        services.AddSingleton<IAppDatabaseStartup>(provider => provider.GetRequiredService<ProtectedStorageHost>());
        services.AddSingleton<IProfileCreationHost>(provider => provider.GetRequiredService<ProtectedStorageHost>());
        services.AddSingleton<IProfileRetirementHost>(provider => provider.GetRequiredService<ProtectedStorageHost>());
        services.AddSingleton<IBackupPackageRestoreReceiver>(provider => provider.GetRequiredService<ProtectedStorageHost>());
        services.AddSingleton<IBackupSafetyCopyProtection>(provider => provider.GetRequiredService<ProtectedStorageHost>());
        services.AddSingleton<IBackupSafetyCopyMaintenance>(provider => provider.GetRequiredService<ProtectedStorageHost>());
        services.AddSingleton<IAutomaticLocalBackupProtection>(provider => provider.GetRequiredService<ProtectedStorageHost>());
        services.AddSingleton<IProtectedProfileSessionSource>(provider => provider.GetRequiredService<ProtectedStorageHost>());
    }
}
