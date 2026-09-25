namespace Vafadar.Authentication;

/// <summary>
/// An external account provider a user can sign in with.
/// </summary>
/// <remarks>
/// Also used as the key of keyed service registrations, e.g.
/// <c>services.AddKeyedSingleton&lt;IAccessTokenProvider&gt;(ExternalIdentityProvider.Google, ...)</c>.
/// </remarks>
public enum ExternalIdentityProvider
{
    /// <summary>A Google account (Google Drive).</summary>
    Google = 1,

    /// <summary>A Microsoft personal or work account (OneDrive).</summary>
    Microsoft = 2,
}
