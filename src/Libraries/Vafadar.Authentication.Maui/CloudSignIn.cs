using Microsoft.Extensions.DependencyInjection;
#if IOS || WINDOWS
using Vafadar.Authentication.Google;
using Vafadar.Authentication.OAuth;
#endif

namespace Vafadar.Authentication.Maui;

/// <summary>The OAuth clients of an app. An empty value means that provider is not offered.</summary>
/// <remarks>
/// All values are client ids of public clients, never secrets. Apps keep them out of tracked source and pass them in
/// from their build-time secrets (see <c>eng/AppSecrets.targets</c>).
/// </remarks>
public sealed class CloudSignInOptions
{
    /// <summary>
    /// Gets or sets the Microsoft Entra application (client) id, a public client for personal and work accounts, used on
    /// every platform. Redirects: Android <c>msal{client-id}://auth</c> (activity in the app manifest), iOS
    /// <c>msauth.{bundle-id}://auth</c> (URL scheme in Info.plist), Windows <c>http://localhost</c>.
    /// </summary>
    public string? MicrosoftClientId { get; set; }

    /// <summary>
    /// Gets or sets the Google OAuth client id of type Android. Android signs in with the package name and the signing
    /// certificate registered for this client; the value only tells that Google is configured.
    /// </summary>
    public string? GoogleAndroidClientId { get; set; }

    /// <summary>
    /// Gets or sets the Google OAuth client id of type iOS (bundle id of the app). Its reversed form is the redirect URL
    /// scheme, which the app must register in <c>CFBundleURLTypes</c>.
    /// </summary>
    public string? GoogleIosClientId { get; set; }

    /// <summary>
    /// Gets or sets the Google OAuth client id of type Desktop app, used on Windows with a loopback redirect
    /// (<c>http://127.0.0.1:{port}/</c>) and PKCE, without a client secret.
    /// </summary>
    public string? GoogleWindowsClientId { get; set; }

    /// <summary>
    /// Gets or sets the text of the page the browser shows after a Google sign-in on Windows ("you can close this tab");
    /// called when the page is shown, so it follows the current app language. English when not set.
    /// </summary>
    public Func<string>? BrowserCompletionMessage { get; set; }
}

/// <summary>Which providers can be used on this device with the configured clients.</summary>
public sealed class CloudSignIn(CloudSignInOptions options)
{
    /// <summary>Gets the platform the app runs on.</summary>
    public static SignInPlatform CurrentPlatform =>
#if ANDROID
        SignInPlatform.Android;
#elif IOS
        SignInPlatform.Ios;
#elif WINDOWS
        SignInPlatform.Windows;
#else
        SignInPlatform.Other;
#endif

    /// <summary>Gets the configured clients.</summary>
    public CloudSignInOptions Options { get; } = options;

    /// <summary>Gets a value indicating whether OneDrive sign-in is available (Android, iOS and Windows).</summary>
    public bool IsMicrosoftAvailable => IsAvailable(ExternalIdentityProvider.Microsoft);

    /// <summary>Gets a value indicating whether Google Drive sign-in is available (Android, iOS and Windows).</summary>
    public bool IsGoogleAvailable => IsAvailable(ExternalIdentityProvider.Google);

    /// <summary>Gets a value indicating whether any provider is available.</summary>
    public bool IsAnyAvailable => IsMicrosoftAvailable || IsGoogleAvailable;

    /// <summary>Returns whether <paramref name="provider"/> is available (see <see cref="CloudSignInMatrix"/>).</summary>
    public bool IsAvailable(ExternalIdentityProvider provider) => CloudSignInMatrix.IsAvailable(
        provider,
        CurrentPlatform,
        Options.MicrosoftClientId,
        Options.GoogleAndroidClientId,
        Options.GoogleIosClientId,
        Options.GoogleWindowsClientId);
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

#if ANDROID || IOS || WINDOWS
        if (available.IsMicrosoftAvailable)
        {
            services.AddKeyedSingleton<IExternalSignInService>(ExternalIdentityProvider.Microsoft, (_, _) => new MicrosoftSignInService(options.MicrosoftClientId!));
            services.AddKeyedSingleton<IAccessTokenProvider>(ExternalIdentityProvider.Microsoft, (sp, key) => sp.GetRequiredKeyedService<IExternalSignInService>(key));
        }

        if (available.IsGoogleAvailable)
        {
            services.AddKeyedSingleton<IExternalSignInService>(ExternalIdentityProvider.Google, (_, _) => CreateGoogleSignIn(options));
            services.AddKeyedSingleton<IAccessTokenProvider>(ExternalIdentityProvider.Google, (sp, key) => sp.GetRequiredKeyedService<IExternalSignInService>(key));
        }
#endif

        return services;
    }

#if ANDROID || IOS || WINDOWS
    // One Google implementation per platform (see CloudSignInMatrix): Play services on Android, the system browser with
    // PKCE on iOS and Windows. The refresh token of iOS and Windows stays in the keychain or under DPAPI.
    private static IExternalSignInService CreateGoogleSignIn(CloudSignInOptions options)
    {
#if ANDROID
        return new GoogleSignInService();
#elif IOS
        var clientId = options.GoogleIosClientId!;
        return new GoogleInstalledAppSignInService(
            clientId,
            new WebAuthenticatorAuthorizationBrowser(GoogleClientIds.IosRedirectUri(clientId)),
            new KeychainValueStore("vafadar.auth.google"));
#else
        return new GoogleInstalledAppSignInService(
            options.GoogleWindowsClientId!,
            new LocalizedLoopbackBrowser(options.BrowserCompletionMessage),
            new DpapiFileValueStore(Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "google"), "google.token"));
#endif
    }
#endif

#if WINDOWS
    private static async Task OpenInBrowserAsync(Uri uri, CancellationToken cancellationToken)
    {
        // The default browser of the user, never an embedded web view (Google blocks those for sign-in).
        if (!await Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync(uri))
        {
            throw new InvalidOperationException("No browser could be opened for the sign-in.");
        }
    }

    // A loopback browser that reads the completion text for every sign-in, so the page follows the app language.
    private sealed class LocalizedLoopbackBrowser(Func<string>? message) : IAuthorizationBrowser
    {
        public ValueTask<IAuthorizationBrowserSession> StartAsync(CancellationToken cancellationToken = default)
        {
            var browser = message?.Invoke() is { Length: > 0 } text
                ? new LoopbackAuthorizationBrowser(OpenInBrowserAsync) { CompletionMessage = text }
                : new LoopbackAuthorizationBrowser(OpenInBrowserAsync);
            return browser.StartAsync(cancellationToken);
        }
    }
#endif
}
