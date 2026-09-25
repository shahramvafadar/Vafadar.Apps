namespace Vafadar.Authentication;

/// <summary>
/// Supplies OAuth access tokens for calling an external API (e.g. Google Drive or Microsoft Graph).
/// </summary>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Returns a valid access token for <paramref name="scopes"/>, refreshing it silently when needed.
    /// </summary>
    /// <exception cref="AuthenticationRequiredException">
    /// No account is signed in, or the user must sign in interactively (e.g. consent for new scopes).
    /// </exception>
    ValueTask<string> GetAccessTokenAsync(IReadOnlyCollection<string> scopes, CancellationToken cancellationToken = default);
}
