namespace Vafadar.Authentication;

/// <summary>A platform an app can run on, as far as cloud sign-in is concerned.</summary>
public enum SignInPlatform
{
    /// <summary>Any platform without cloud sign-in (for example tests or Mac Catalyst).</summary>
    Other = 0,

    /// <summary>Android.</summary>
    Android = 1,

    /// <summary>iOS and iPadOS.</summary>
    Ios = 2,

    /// <summary>The Windows desktop.</summary>
    Windows = 3,
}

/// <summary>
/// The provider availability matrix: which provider can be offered on which platform with which OAuth client.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Provider</term><description>Android · iOS · Windows</description></listheader>
/// <item><term>Microsoft (OneDrive)</term><description>MSAL.NET with the one Entra public client on all three.</description></item>
/// <item><term>Google (Google Drive)</term><description>
/// Android: Play services authorization API with the Android client · iOS: system browser with the iOS client ·
/// Windows: loopback with the Desktop client. Each platform has its own Google client id.
/// </description></item>
/// </list>
/// A provider is offered only when its client id for that platform is configured; a build without ids is offline.
/// </remarks>
public static class CloudSignInMatrix
{
    /// <summary>Returns whether <paramref name="provider"/> is offered on <paramref name="platform"/>.</summary>
    /// <param name="provider">The provider.</param>
    /// <param name="platform">The platform the app runs on.</param>
    /// <param name="microsoftClientId">The Microsoft Entra application (client) id, shared by all platforms.</param>
    /// <param name="googleAndroidClientId">The Google OAuth client id of type Android.</param>
    /// <param name="googleIosClientId">The Google OAuth client id of type iOS.</param>
    /// <param name="googleWindowsClientId">The Google OAuth client id of type Desktop app.</param>
    public static bool IsAvailable(
        ExternalIdentityProvider provider,
        SignInPlatform platform,
        string? microsoftClientId,
        string? googleAndroidClientId,
        string? googleIosClientId,
        string? googleWindowsClientId) => provider switch
        {
            ExternalIdentityProvider.Microsoft => platform is SignInPlatform.Android or SignInPlatform.Ios or SignInPlatform.Windows
                && !string.IsNullOrWhiteSpace(microsoftClientId),
            // iOS and Windows build their redirect from the id, so a malformed value counts as not configured.
            ExternalIdentityProvider.Google => platform == SignInPlatform.Android
                ? !string.IsNullOrWhiteSpace(googleAndroidClientId)
                : Google.GoogleClientIds.IsValid(GoogleClientId(platform, googleAndroidClientId, googleIosClientId, googleWindowsClientId)),
            _ => false,
        };

    /// <summary>Returns the Google client id that belongs to <paramref name="platform"/>, or <see langword="null"/>.</summary>
    public static string? GoogleClientId(SignInPlatform platform, string? android, string? ios, string? windows) => platform switch
    {
        SignInPlatform.Android => android,
        SignInPlatform.Ios => ios,
        SignInPlatform.Windows => windows,
        _ => null,
    };
}
