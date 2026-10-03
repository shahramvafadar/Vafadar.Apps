namespace Vafadar.Authentication.Google;

/// <summary>Helpers for Google OAuth client ids (<c>{number}-{key}.apps.googleusercontent.com</c>).</summary>
public static class GoogleClientIds
{
    private const string Suffix = ".apps.googleusercontent.com";

    /// <summary>Returns whether <paramref name="clientId"/> has the form of a Google OAuth client id.</summary>
    public static bool IsValid(string? clientId) =>
        clientId is { Length: > 27 } && clientId.EndsWith(Suffix, StringComparison.Ordinal) && !clientId.AsSpan(0, clientId.Length - Suffix.Length).ContainsAny(".:/ ");

    /// <summary>
    /// Returns the reversed client id, e.g. <c>com.googleusercontent.apps.123-abc</c> for
    /// <c>123-abc.apps.googleusercontent.com</c>: the URL scheme an iOS client may use for its redirect. The iOS app
    /// registers it in <c>CFBundleURLTypes</c> (Zanance generates that entry at build time, see
    /// <c>Vafadar.Zanance.App.csproj</c>).
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="clientId"/> is not a Google client id.</exception>
    public static string ReversedClientId(string clientId)
    {
        if (!IsValid(clientId))
        {
            throw new ArgumentException("Not a Google OAuth client id.", nameof(clientId));
        }

        return "com.googleusercontent.apps." + clientId[..^Suffix.Length];
    }

    /// <summary>Returns the iOS redirect URI <c>{reversed client id}:/oauth2redirect</c>.</summary>
    public static Uri IosRedirectUri(string clientId) => new(ReversedClientId(clientId) + ":/oauth2redirect");
}
