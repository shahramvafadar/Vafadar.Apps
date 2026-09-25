using Microsoft.Extensions.DependencyInjection;
using Vafadar.Authentication;

namespace Vafadar.Backup.OneDrive;

/// <summary>
/// Registers <see cref="OneDriveBackupStorage"/>.
/// </summary>
public static class OneDriveServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="OneDriveBackupStorage"/> (also as <see cref="IBackupStorage"/>). Requires an
    /// <see cref="IAccessTokenProvider"/> registered with the key <see cref="ExternalIdentityProvider.Microsoft"/>.
    /// </summary>
    public static IServiceCollection AddOneDriveBackupStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient(OneDriveBackupStorage.HttpClientName);
        services.AddTransient(sp => new OneDriveBackupStorage(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(OneDriveBackupStorage.HttpClientName),
            sp.GetRequiredKeyedService<IAccessTokenProvider>(ExternalIdentityProvider.Microsoft)));
        services.AddTransient<IBackupStorage>(sp => sp.GetRequiredService<OneDriveBackupStorage>());

        return services;
    }
}
