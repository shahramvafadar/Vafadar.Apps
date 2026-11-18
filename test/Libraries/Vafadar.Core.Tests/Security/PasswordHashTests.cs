using Vafadar.Core.Security;

namespace Vafadar.Core.Tests.Security;

/// <summary>What is kept of a password: a derived value that only the password matches.</summary>
public sealed class PasswordHashTests
{
    [Fact]
    public void A_password_matches_what_was_derived_from_it_and_no_other_does()
    {
        var kept = PasswordHash.Derive("a long pass phrase");

        Assert.True(PasswordHash.Matches("a long pass phrase", kept));
        Assert.False(PasswordHash.Matches("a long pass phrasE", kept));
        Assert.False(PasswordHash.Matches(string.Empty, kept));
    }

    [Fact]
    public void What_is_kept_names_its_method_and_work_and_never_holds_the_password()
    {
        var kept = PasswordHash.Derive("a long pass phrase");

        Assert.StartsWith("pbkdf2-sha256$600000$", kept, StringComparison.Ordinal);
        Assert.DoesNotContain("a long pass phrase", kept, StringComparison.Ordinal);
        Assert.Equal(4, kept.Split('$').Length);
    }

    [Fact]
    public void The_same_password_is_kept_differently_each_time()
    {
        Assert.NotEqual(PasswordHash.Derive("a long pass phrase"), PasswordHash.Derive("a long pass phrase"));
    }

    [Fact]
    public void A_value_kept_with_less_work_still_matches_its_password()
    {
        // A value as an earlier version kept it: the same method with fewer rounds.
        const string Earlier = "pbkdf2-sha256$1000$AAAAAAAAAAAAAAAAAAAAAA==$";
        var key = Convert.ToBase64String(System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2("old password", new byte[16], 1000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32));

        Assert.True(PasswordHash.Matches("old password", Earlier + key));
        Assert.False(PasswordHash.Matches("another password", Earlier + key));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain text")]
    [InlineData("md5$1$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$many$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$0$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$1000$not base64!$AAAA")]
    [InlineData("pbkdf2-sha256$1000$AAAA")]
    public void A_text_that_is_no_kept_value_matches_nothing(string kept)
    {
        Assert.False(PasswordHash.Matches("a long pass phrase", kept));
    }
}
