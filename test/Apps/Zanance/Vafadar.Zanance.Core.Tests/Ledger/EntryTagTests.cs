using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.Core.Tests.Ledger;

public sealed class EntryTagTests
{
    [Fact]
    public void Tags_are_trimmed_unique_and_limited()
    {
        Assert.Equal(["car", "Trip 2027"], EntryTags.Parse(" #car, Car ,Trip 2027،, "));
        Assert.Empty(EntryTags.Parse("   "));
        Assert.Equal(EntryTags.MaxTags, EntryTags.Parse(string.Join(',', Enumerable.Range(1, 20))).Count);
        Assert.Equal(EntryTags.MaxLength, EntryTags.Parse(new string('x', 50)).Single().Length);
    }

    [Fact]
    public void Tags_are_displayed_in_an_isolate_that_matches_their_script()
    {
        Assert.Equal("\u2066\u200E#home\u200E\u2069", EntryTags.Display("home"));
        Assert.Equal("\u2067\u200F#سفر\u200F\u2069", EntryTags.Display("سفر"));
    }

    [Fact]
    public void Tags_are_found_by_search_kept_by_duplicate_and_listed_by_use()
    {
        var account = Guid.CreateVersion7();
        var fuel = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account, Amount = 5_000, Date = new DateOnly(2027, 1, 2), Tags = ["car", "trip"] };
        var toll = new LedgerEntry { Kind = EntryKind.Expense, AccountId = account, Amount = 800, Date = new DateOnly(2027, 1, 3), Tags = ["trip"] };

        var found = EntrySearch.Apply([fuel, toll], new EntryFilter(Text: "#car"), _ => null, new Dictionary<Guid, Accounts.Account>()).ToList();

        Assert.Equal([fuel], found);
        Assert.Equal(["car", "trip"], EntryActions.Duplicate(fuel, new DateOnly(2027, 2, 1)).Tags);
        Assert.Equal(["trip", "car"], EntryTags.InUse([fuel, toll]));
    }
}