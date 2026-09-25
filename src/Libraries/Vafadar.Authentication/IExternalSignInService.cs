namespace Vafadar.Authentication;

/// <summary>
/// Signs the user in and out of an external account and provides access tokens for it.
/// </summary>
/// <remarks>
/// Implementations are platform specific (system browser / native account APIs) and are registered per provider as
/// keyed services, using <see cref="ExternalIdentityProvider"/> as the key.
/// </remarks>
public interface IExternalSignInService : IAccessTokenProvider
{
    /// <summary>Gets the provider this service signs in to.</summary>
    ExternalIdentityProvider Provider { get; }

    /// <summary>Returns the signed-in account, or <see langword="null"/> if nobody is signed in.</summary>
    ValueTask<ExternalAccount?> GetCurrentAccountAsync(CancellationToken cancellationToken = default);

    /// <summary>Signs in interactively and requests consent for <paramref name="scopes"/>.</summary>
    /// <exception cref="OperationCanceledException">The user cancelled the sign-in.</exception>
    ValueTask<ExternalAccount> SignInAsync(IReadOnlyCollection<string> scopes, CancellationToken cancellationToken = default);

    /// <summary>Signs out and removes cached tokens from the device.</summary>
    ValueTask SignOutAsync(CancellationToken cancellationToken = default);
}
