using Microsoft.Extensions.DependencyInjection;
using Vafadar.Authentication;

namespace Vafadar.Backup.GoogleDrive;

/// <summary>
/// Registers <see cref="GoogleDriveBackupStorage"/>.
/// </summary>
public static class GoogleDriveServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="GoogleDriveBackupStorage"/> (also as <see cref="IBackupStorage"/>). Requires an
    /// <see cref="IAccessTokenProvider"/> registered with the key <see cref="ExternalIdentityProvider.Google"/>.
    /// </summary>
    public static IServiceCollection AddGoogleDriveBackupStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // A whole backup goes up or down in one request; on a slow mobile connection that takes longer than the
        // default of 100 seconds.
        services.AddHttpClient(GoogleDriveBackupStorage.HttpClientName, client => client.Timeout = GoogleDriveBackupStorage.TransferTimeout);
        services.AddTransient(sp => new GoogleDriveBackupStorage(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(GoogleDriveBackupStorage.HttpClientName),
            sp.GetRequiredKeyedService<IAccessTokenProvider>(ExternalIdentityProvider.Google)));
        services.AddTransient<IBackupStorage>(sp => sp.GetRequiredService<GoogleDriveBackupStorage>());

        return services;
    }
}
