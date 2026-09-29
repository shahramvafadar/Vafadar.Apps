using Microsoft.Extensions.DependencyInjection;

namespace Vafadar.Authentication.Maui;

/// <summary>The OAuth clients of an app. An empty value means that provider is not offered.</summary>
public sealed class CloudSignInOptions
{
    /// <summary>
    /// Gets or sets the Microsoft Entra application (client) id, a public client for personal and work accounts.
    /// Android needs the redirect <c>msal{client-id}://auth</c> in the app manifest; Windows uses <c>http://localhost</c>.
    /// </summary>
    public string? MicrosoftClientId { get; set; }

    /// <summary>
    /// Gets or sets the Google OAuth client id of the Android app. Android signs in with the package name and the signing
    /// certificate registered for this client; the value only tells that Google is configured.
    /// </summary>
    public string? GoogleAndroidClientId { get; set; }
}

/// <summary>Which providers can be used on this device with the configured clients.</summary>
public sealed class CloudSignIn(CloudSignInOptions options)
{
    /// <summary>Gets the configured clients.</summary>
    public CloudSignInOptions Options { get; } = options;

    /// <summary>Gets a value indicating whether OneDrive sign-in is available (Android and Windows).</summary>
    public bool IsMicrosoftAvailable =>
#if ANDROID || WINDOWS
        !string.IsNullOrWhiteSpace(Options.MicrosoftClientId);
#else
        false;
#endif

    /// <summary>Gets a value indicating whether Google Drive sign-in is available (Android).</summary>
    public bool IsGoogleAvailable =>
#if ANDROID
        !string.IsNullOrWhiteSpace(Options.GoogleAndroidClientId);
#else
        false;
#endif

    /// <summary>Gets a value indicating whether any provider is available.</summary>
    public bool IsAnyAvailable => IsMicrosoftAvailable || IsGoogleAvailable;

    /// <summary>Returns whether <paramref name="provider"/> is available.</summary>
    public bool IsAvailable(ExternalIdentityProvider provider) => provider switch
    {
        ExternalIdentityProvider.Microsoft => IsMicrosoftAvailable,
        ExternalIdentityProvider.Google => IsGoogleAvailable,
        _ => false,
    };
}

/// <summary>Registers the sign-in services.</summary>
public static class CloudSignInServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="CloudSignIn"/> and, for every provider available on this platform with a configured client, an
    /// <see cref="IExternalSignInService"/> and <see cref="IAccessTokenProvider"/> keyed by
    /// <see cref="ExternalIdentityProvider"/> (as the Google Drive and OneDrive storages expect).
    /// </summary>
    public static IServiceCollection AddVafadarCloudSignIn(this IServiceCollection services, Action<CloudSignInOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        var options = new CloudSignInOptions();
        configure(options);
        var available = new CloudSignIn(options);
        services.AddSingleton(options);
        services.AddSingleton(available);

#if ANDROID || WINDOWS
        if (available.IsMicrosoftAvailable)
        {
            services.AddKeyedSingleton<IExternalSignInService>(ExternalIdentityProvider.Microsoft, (_, _) => new MicrosoftSignInService(options.MicrosoftClientId!));
            services.AddKeyedSingleton<IAccessTokenProvider>(ExternalIdentityProvider.Microsoft, (sp, key) => sp.GetRequiredKeyedService<IExternalSignInService>(key));
        }
#endif

#if ANDROID
        if (available.IsGoogleAvailable)
        {
            services.AddKeyedSingleton<IExternalSignInService>(ExternalIdentityProvider.Google, (_, _) => new GoogleSignInService());
            services.AddKeyedSingleton<IAccessTokenProvider>(ExternalIdentityProvider.Google, (sp, key) => sp.GetRequiredKeyedService<IExternalSignInService>(key));
        }
#endif

        return services;
    }
}
