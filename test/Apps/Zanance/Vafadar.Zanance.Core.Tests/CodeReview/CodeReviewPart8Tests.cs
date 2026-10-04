using Vafadar.Zanance.Core.Accounts;

namespace Vafadar.Zanance.Core.Tests.CodeReview;

/// <summary>Regression tests of the code review, part 8: accounts and holdings.</summary>
public sealed class CodeReviewPart8Tests
{
    private static readonly Account Main = new() { Name = "Main account", CurrencyCode = "EUR" };
    private static readonly Account Savings = new() { Name = "پس‌انداز ملی", CurrencyCode = "IRR", IsArchived = true };

    [Theory]
    [InlineData("Main account")]
    [InlineData("  main ACCOUNT ")]
    [InlineData("پس انداز ملی")]
    [InlineData("پس‌انداز ملي")]
    public void A_name_used_by_another_account_is_taken_in_any_spelling(string name)
    {
        // CR08 (follow-up of CR04-06): CSV files and pickers tell accounts apart by name, so a name is used once – also
        // when the other account is archived, and whatever case, half-space or Arabic letter form is typed.
        Assert.True(AccountNames.IsTaken([Main, Savings], name));
    }

    [Fact]
    public void An_account_keeps_its_own_name_and_a_new_name_is_free()
    {
        Assert.False(AccountNames.IsTaken([Main, Savings], "Main account", Main.Id));
        Assert.False(AccountNames.IsTaken([Main, Savings], "Cash"));
        Assert.False(AccountNames.IsTaken([Main, Savings], "   "));
    }
}
