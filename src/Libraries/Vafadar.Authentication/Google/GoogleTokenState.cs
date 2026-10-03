using System.Text.Json.Serialization;

namespace Vafadar.Authentication.Google;

/// <summary>What is kept in protected storage after a Google sign-in on iOS or Windows.</summary>
/// <param name="RefreshToken">The refresh token; it is the only secret and never leaves the protected storage.</param>
/// <param name="Subject">Google's stable account id (<c>sub</c> of the ID token).</param>
/// <param name="Email">The account e-mail, shown in the app.</param>
/// <param name="Scopes">The scopes the user granted.</param>
internal sealed record GoogleTokenState(string RefreshToken, string? Subject, string? Email, IReadOnlyList<string> Scopes);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(GoogleTokenState))]
internal sealed partial class GoogleAuthJsonContext : JsonSerializerContext;
