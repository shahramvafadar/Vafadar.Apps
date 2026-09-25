namespace Vafadar.Authentication;

/// <summary>
/// Thrown when an operation needs the user to sign in (again) interactively.
/// </summary>
public sealed class AuthenticationRequiredException : Exception
{
    /// <summary>Creates the exception.</summary>
    public AuthenticationRequiredException()
        : this("The user must sign in.")
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    public AuthenticationRequiredException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and the underlying error.</summary>
    public AuthenticationRequiredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates the exception for a provider.</summary>
    public AuthenticationRequiredException(ExternalIdentityProvider provider)
        : this($"Signing in to {provider} is required.")
    {
        Provider = provider;
    }

    /// <summary>Gets the provider that requires sign-in, when known.</summary>
    public ExternalIdentityProvider? Provider { get; }
}
