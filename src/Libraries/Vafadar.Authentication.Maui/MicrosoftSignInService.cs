#if ANDROID || IOS || WINDOWS
using Microsoft.Identity.Client;

namespace Vafadar.Authentication.Maui;

/// <summary>
/// Microsoft sign-in with MSAL through the system browser, for personal and work or school accounts. The token cache
/// stays on the device: MSAL's protected storage on Android, the keychain on iOS, DPAPI on Windows. Only the scopes of
/// the caller are requested (for backups: the app's own OneDrive folder). No client secret exists for this public
/// client, and no broker app (Authenticator) is used.
/// </summary>
public sealed class MicrosoftSignInService : IExternalSignInService
{
    private readonly IPublicClientApplication _client;
#if WINDOWS
    private readonly SemaphoreSlim _cacheReady = new(1, 1);
    private bool _cacheRegistered;
#endif

    /// <summary>Creates the service for an Entra public client.</summary>
    public MicrosoftSignInService(string clientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        var builder = PublicClientApplicationBuilder.Create(clientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount);
#if ANDROID
        builder = builder
            .WithRedirectUri($"msal{clientId}://auth")
            .WithParentActivityOrWindow(() => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity);
#elif IOS
        // Redirect msauth.{bundle-id}://auth (registered in the Entra app as an iOS platform and in CFBundleURLTypes).
        // The token cache lives in the keychain group of the bundle id; the entitlement keychain-access-groups lists
        // $(AppIdentifierPrefix){bundle-id}, and MSAL adds the team prefix itself.
        var bundleId = Microsoft.Maui.ApplicationModel.AppInfo.Current.PackageName;
        builder = builder
            .WithRedirectUri($"msauth.{bundleId}://auth")
            .WithIosKeychainSecurityGroup(bundleId)
            .WithParentActivityOrWindow(() => Microsoft.Maui.ApplicationModel.Platform.GetCurrentUIViewController()
                ?? throw new InvalidOperationException("No view controller to sign in from."));
#else
        builder = builder.WithRedirectUri("http://localhost");
#endif
        _client = builder.Build();
    }

    /// <inheritdoc />
    public ExternalIdentityProvider Provider => ExternalIdentityProvider.Microsoft;

    /// <inheritdoc />
    public async ValueTask<ExternalAccount?> GetCurrentAccountAsync(CancellationToken cancellationToken = default)
    {
        var account = await FirstAccountAsync();
        return account is null ? null : ToAccount(account);
    }

    /// <inheritdoc />
    public async ValueTask<string> GetAccessTokenAsync(IReadOnlyCollection<string> scopes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        var account = await FirstAccountAsync() ?? throw new AuthenticationRequiredException(Provider);
        try
        {
            var result = await _client.AcquireTokenSilent(scopes, account).ExecuteAsync(cancellationToken);
            return result.AccessToken;
        }
        catch (MsalUiRequiredException ex)
        {
            throw new AuthenticationRequiredException("Microsoft sign-in is needed again.", ex);
        }
        catch (MsalException ex) when (IsNetworkProblem(ex))
        {
            throw new HttpRequestException("Microsoft sign-in could not be reached.", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<ExternalAccount> SignInAsync(IReadOnlyCollection<string> scopes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        await EnsureCacheAsync();
        try
        {
            var result = await _client.AcquireTokenInteractive(scopes)
                .WithPrompt(Prompt.SelectAccount)
                .WithUseEmbeddedWebView(false)
                .ExecuteAsync(cancellationToken);
            return ToAccount(result.Account);
        }
        catch (MsalClientException ex) when (ex.ErrorCode == MsalError.AuthenticationCanceledError)
        {
            throw new OperationCanceledException("The sign-in was cancelled.", ex, cancellationToken);
        }
        catch (MsalException ex) when (IsNetworkProblem(ex))
        {
            throw new HttpRequestException("Microsoft sign-in could not be reached.", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask SignOutAsync(CancellationToken cancellationToken = default)
    {
        await EnsureCacheAsync();
        foreach (var account in await _client.GetAccountsAsync())
        {
            await _client.RemoveAsync(account);
        }
    }

    private async Task<IAccount?> FirstAccountAsync()
    {
        await EnsureCacheAsync();
        return (await _client.GetAccountsAsync()).FirstOrDefault();
    }

    // Callers treat HttpRequestException as "offline"; MSAL wraps network failures in its own exceptions.
    private static bool IsNetworkProblem(MsalException exception) =>
        exception.InnerException is HttpRequestException or TaskCanceledException or System.Net.Sockets.SocketException
        || exception.ErrorCode is MsalError.RequestTimeout or "network_not_available";

    private static ExternalAccount ToAccount(IAccount account) =>
        new(ExternalIdentityProvider.Microsoft, account.HomeAccountId?.Identifier ?? account.Username, account.Username, null);

    // Android and iOS persist the cache themselves; the desktop needs a cache file, protected with DPAPI for the Windows user.
    private async Task EnsureCacheAsync()
    {
#if WINDOWS
        if (_cacheRegistered)
        {
            return;
        }

        await _cacheReady.WaitAsync();
        try
        {
            if (!_cacheRegistered)
            {
                var folder = Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "msal");
                Directory.CreateDirectory(folder);
                var properties = new Microsoft.Identity.Client.Extensions.Msal.StorageCreationPropertiesBuilder("msal.cache", folder).Build();
                var helper = await Microsoft.Identity.Client.Extensions.Msal.MsalCacheHelper.CreateAsync(properties);
                helper.RegisterCache(_client.UserTokenCache);
                _cacheRegistered = true;
            }
        }
        finally
        {
            _cacheReady.Release();
        }
#else
        await Task.CompletedTask;
#endif
    }
}
#endif
