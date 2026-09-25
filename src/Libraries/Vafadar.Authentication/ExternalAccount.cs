namespace Vafadar.Authentication;

/// <summary>
/// A signed-in external account.
/// </summary>
/// <param name="Provider">The provider the account belongs to.</param>
/// <param name="Id">The provider's stable identifier of the account.</param>
/// <param name="Email">The account e-mail address, when the provider returns one.</param>
/// <param name="DisplayName">The user's display name, when the provider returns one.</param>
public sealed record ExternalAccount(ExternalIdentityProvider Provider, string Id, string? Email, string? DisplayName);
