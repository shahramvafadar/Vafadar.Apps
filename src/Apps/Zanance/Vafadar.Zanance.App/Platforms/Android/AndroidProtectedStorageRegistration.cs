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
        services.AddSingleton<ProtectedBackupRecovery>(provider => provider.GetRequiredService<AndroidBackupRecovery>());
        services.AddSingleton(provider =>
        {
            var installation = provider.GetRequiredService<AndroidProtectedInstallation>();
            return new AndroidProtectedStorageHost(provider, installation.BeginAsync, installation.CompleteAsync);
        });
        services.AddSingleton<ProtectedStorageHost>(provider => provider.GetRequiredService<AndroidProtectedStorageHost>());
        services.AddProtectedStorageContracts();
    }
}
