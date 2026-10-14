using Microsoft.Extensions.DependencyInjection;

namespace Vafadar.Zanance.App.Security;

/// <summary>Registers lazy Windows DPAPI storage and shared explicit recovery without construction-time database effects.</summary>
internal static class WindowsProtectedStorageRegistration
{
    /// <summary>Composes the covered Windows owner and original profile/backup contracts.</summary>
    internal static void AddWindowsProtectedStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<WindowsProtectedInstallation>();
        services.AddSingleton<WindowsBackupRecovery>();
        services.AddSingleton<ProtectedBackupRecovery>(provider => provider.GetRequiredService<WindowsBackupRecovery>());
        services.AddSingleton(provider =>
        {
            var installation = provider.GetRequiredService<WindowsProtectedInstallation>();
            return new WindowsProtectedStorageHost(provider, installation.BeginAsync, installation.CompleteAsync);
        });
        services.AddSingleton<ProtectedStorageHost>(provider => provider.GetRequiredService<WindowsProtectedStorageHost>());
        services.AddProtectedStorageContracts();
    }
}
