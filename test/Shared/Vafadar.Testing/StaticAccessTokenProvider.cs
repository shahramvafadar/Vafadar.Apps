using Vafadar.Authentication;

namespace Vafadar.Testing;

/// <summary>An <see cref="IAccessTokenProvider"/> that always returns the same token and records requested scopes.</summary>
public sealed class StaticAccessTokenProvider(string token = "test-token") : IAccessTokenProvider
{
    /// <summary>Gets the token returned for every request.</summary>
    public string Token { get; } = token;

    /// <summary>Gets the scopes of every token request.</summary>
    public List<IReadOnlyCollection<string>> RequestedScopes { get; } = [];

    /// <inheritdoc />
    public ValueTask<string> GetAccessTokenAsync(IReadOnlyCollection<string> scopes, CancellationToken cancellationToken = default)
    {
        RequestedScopes.Add(scopes);
        return ValueTask.FromResult(Token);
    }
}
