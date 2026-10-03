namespace Vafadar.Authentication.OAuth;

/// <summary>
/// Shows the provider's sign-in page in the system browser and returns the parameters of the redirect back to the app.
/// The app never sees the user's password; it only receives the authorization code at its redirect URI.
/// </summary>
/// <remarks>
/// Platform implementations: a loopback listener on the desktop (<see cref="LoopbackAuthorizationBrowser"/>) and the
/// system authentication session with a custom URL scheme on iOS (in <c>Vafadar.Authentication.Maui</c>).
/// </remarks>
public interface IAuthorizationBrowser
{
    /// <summary>
    /// Prepares one authorization: decides the redirect URI (for example a free loopback port) that the authorization
    /// request must name. Dispose the session when the authorization is finished or abandoned.
    /// </summary>
    ValueTask<IAuthorizationBrowserSession> StartAsync(CancellationToken cancellationToken = default);
}

/// <summary>One authorization in progress, see <see cref="IAuthorizationBrowser.StartAsync"/>.</summary>
public interface IAuthorizationBrowserSession : IAsyncDisposable
{
    /// <summary>Gets the redirect URI to send in the authorization and token requests.</summary>
    Uri RedirectUri { get; }

    /// <summary>
    /// Opens <paramref name="authorizationUri"/> in the system browser and waits for the redirect to
    /// <see cref="RedirectUri"/>.
    /// </summary>
    /// <returns>The query parameters of the redirect (for example <c>code</c> and <c>state</c>, or <c>error</c>).</returns>
    /// <exception cref="OperationCanceledException">The user closed the browser, or the wait timed out.</exception>
    Task<IReadOnlyDictionary<string, string>> AuthorizeAsync(Uri authorizationUri, CancellationToken cancellationToken = default);
}
