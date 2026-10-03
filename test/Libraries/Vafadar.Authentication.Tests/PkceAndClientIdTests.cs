using Vafadar.Authentication.Google;
using Vafadar.Authentication.OAuth;

namespace Vafadar.Authentication.Tests;

public sealed class PkceAndClientIdTests
{
    [Fact]
    public void Challenge_matches_the_example_of_RFC_7636()
    {
        // RFC 7636, appendix B.
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", PkceCodes.ChallengeOf("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"));
    }

    [Fact]
    public void New_codes_are_random_url_safe_and_consistent()
    {
        var first = PkceCodes.Create();
        var second = PkceCodes.Create();

        Assert.NotEqual(first.Verifier, second.Verifier);
        Assert.Equal(43, first.Verifier.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", first.Verifier);
        Assert.Matches("^[A-Za-z0-9_-]+$", first.Challenge);
        Assert.Equal(PkceCodes.ChallengeOf(first.Verifier), first.Challenge);
        Assert.Matches("^[A-Za-z0-9_-]{22}$", PkceCodes.NewState());
    }

    [Fact]
    public void Ios_redirect_uses_the_reversed_client_id()
    {
        const string clientId = "123456-abc9def.apps.googleusercontent.com";

        Assert.True(GoogleClientIds.IsValid(clientId));
        Assert.Equal("com.googleusercontent.apps.123456-abc9def", GoogleClientIds.ReversedClientId(clientId));
        Assert.Equal("com.googleusercontent.apps.123456-abc9def:/oauth2redirect", GoogleClientIds.IosRedirectUri(clientId).ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("apps.googleusercontent.com")]
    [InlineData("123.example.com")]
    [InlineData("12.34.apps.googleusercontent.com")]
    [InlineData("12 34.apps.googleusercontent.com")]
    public void Malformed_client_ids_are_rejected(string? clientId)
    {
        Assert.False(GoogleClientIds.IsValid(clientId));
        if (clientId is not null)
        {
            Assert.Throws<ArgumentException>(() => GoogleClientIds.ReversedClientId(clientId));
        }
    }
}
