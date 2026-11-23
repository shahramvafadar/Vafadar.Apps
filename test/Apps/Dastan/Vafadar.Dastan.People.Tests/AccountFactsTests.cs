using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// The facts an account states about itself (delivery plan 25.5, D-088): a year of birth, where it lives, the
/// languages it speaks and one address of a page of its own – none asked for, each with an audience of its
/// own that begins with nobody else. An account that states nothing and one that states nothing to a viewer
/// look the same to that viewer (PRIV-012). All of it is made up.
/// </summary>
public sealed class AccountFactsTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 23, 9, 0, 0, TimeSpan.Zero));
    private readonly string _database;
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;
    private readonly long _close;

    public AccountFactsTests()
    {
        _database = TestDatabase.For(Path.Combine(_folder, "people.db"));
        _store = PeopleStore.Open(_database, _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
        _dara = _store.Register("Dara", Password).Session!.Id;
        Friends(_mina, _sara, "Sara");
        Friends(_mina, _dara, "Dara");
        _close = _store.AddCircle(_mina, "Close")!.Value;
        Assert.True(_store.SetCircle(_mina, _close, [_sara]));
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private void Friends(string from, string to, string toName)
    {
        Assert.Equal("asked", _store.Ask(from, toName, "friend"));
        Assert.True(_store.Answer(to, _store.Relationships(to).Single(relationship => relationship.Other.Id == from).Id, accept: true));
    }

    private static FactAudience Everybody => new(ScopeKind.Public);

    private FactAudience Circle => new(ScopeKind.Circle, _close);

    // Everything stated: the year for everybody, where she lives for her circle, the languages for nobody else, the page for everybody.
    private AccountFacts Whole() => new(1985, Everybody, "Shiraz", "Iran", Circle, ["fa", "en", "de"], FactAudience.Nobody, "https://example.org/mina", Everybody);

    private ShownFacts Seen(string? viewer) => _store.Profile(viewer, _mina)!.Facts;

    [Fact]
    public void An_account_that_states_nothing_has_nothing_for_anybody()
    {
        Assert.Equal(AccountFacts.None with { Languages = [] }, _store.Facts(_mina)! with { Languages = [] });
        Assert.Empty(_store.Facts(_mina)!.Languages);
        Assert.All(new[] { null, _mina, _sara, _dara }, viewer => Assert.Same(ShownFacts.None, Seen(viewer)));
        Assert.Null(_store.Facts("no-such-account"));
    }

    [Fact]
    public void Each_fact_is_seen_by_who_its_own_audience_allows_and_always_by_the_account()
    {
        Assert.Equal("done", _store.StateFacts(_mina, Whole()));

        var mine = _store.Facts(_mina)!;
        Assert.Equal((1985, "Shiraz", "Iran", "https://example.org/mina"), (mine.BornYear, mine.Town, mine.Country, mine.Link));
        Assert.Equal(["fa", "en", "de"], mine.Languages);
        Assert.Equal((Everybody, Circle, FactAudience.Nobody, Everybody), (mine.Born, mine.Lives, mine.Speaks, mine.Linked));

        // The account itself.
        var own = Seen(_mina);
        Assert.Equal((1985, "Shiraz", "Iran", "https://example.org/mina"), (own.BornYear, own.Town, own.Country, own.Link));
        Assert.Equal(["fa", "en", "de"], own.Languages);

        // Somebody without an account, and a friend outside the circle: what is for everybody.
        foreach (var viewer in new[] { null, _dara })
        {
            var shown = Seen(viewer);
            Assert.Equal((1985, null, null, "https://example.org/mina"), (shown.BornYear, shown.Town, shown.Country, shown.Link));
            Assert.Empty(shown.Languages);
        }

        // The circle: where she lives as well, and still not the languages.
        var close = Seen(_sara);
        Assert.Equal((1985, "Shiraz", "Iran"), (close.BornYear, close.Town, close.Country));
        Assert.Empty(close.Languages);
    }

    [Fact]
    public void A_town_without_a_country_and_a_country_without_a_town_are_both_where_somebody_lives()
    {
        Assert.Equal("done", _store.StateFacts(_mina, AccountFacts.None with { Town = "  Shiraz ", Lives = Everybody }));
        Assert.Equal(("Shiraz", null), (Seen(null).Town, Seen(null).Country));

        Assert.Equal("done", _store.StateFacts(_mina, AccountFacts.None with { Country = "Iran", Lives = Everybody }));
        Assert.Equal((null, "Iran"), (Seen(null).Town, Seen(null).Country));
    }

    [Theory]
    [InlineData(1899, "year")]
    [InlineData(2026, "year")]
    [InlineData(3000, "year")]
    [InlineData(0, "year")]
    public void A_year_that_is_no_year_of_birth_is_refused(int year, string problem)
    {
        Assert.Equal(problem, _store.StateFacts(_mina, AccountFacts.None with { BornYear = year }));
        Assert.Null(_store.Facts(_mina)!.BornYear);
    }

    [Fact]
    public void The_first_and_the_last_year_an_account_can_state_are_kept()
    {
        Assert.Equal("done", _store.StateFacts(_mina, AccountFacts.None with { BornYear = PeopleStore.EarliestBornYear }));
        Assert.Equal("done", _store.StateFacts(_mina, AccountFacts.None with { BornYear = 2025 }));
        Assert.Equal(2025, _store.Facts(_mina)!.BornYear);
    }

    [Theory]
    [InlineData("http://example.org/mina")]
    [InlineData("example.org/mina")]
    [InlineData("https://localhost/mina")]
    [InlineData("https://mina:secret@example.org/")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://example.org/a page")]
    [InlineData("mailto:mina@example.org")]
    public void An_address_that_is_no_page_under_https_is_refused(string link)
    {
        Assert.Equal("link", _store.StateFacts(_mina, AccountFacts.None with { Link = link, Linked = Everybody }));
        Assert.Null(_store.Facts(_mina)!.Link);
    }

    [Fact]
    public void What_is_no_fact_is_refused_and_nothing_changes()
    {
        Assert.Equal("done", _store.StateFacts(_mina, Whole()));
        var saras = _store.AddCircle(_sara, "Hers")!.Value;

        Assert.Equal("long", _store.StateFacts(_mina, Whole() with { Town = new string('x', PeopleStore.LongestPlaceOfAccount + 1) }));
        Assert.Equal("long", _store.StateFacts(_mina, Whole() with { Country = "Two\nlines" }));
        Assert.Equal("languages", _store.StateFacts(_mina, Whole() with { Languages = ["fa", "xx"] }));
        Assert.Equal("languages", _store.StateFacts(_mina, Whole() with { Languages = ["fa", "fa"] }));
        Assert.Equal("languages", _store.StateFacts(_mina, Whole() with { Languages = ["fa", "en", "de", "fr", "it", "es", "ar", "tr", "ru"] }));
        Assert.Equal("link", _store.StateFacts(_mina, Whole() with { Link = "https://example.org/" + new string('x', PeopleStore.LongestLink) }));
        Assert.Equal("refused", _store.StateFacts(_mina, Whole() with { Born = new((ScopeKind)9) }));
        Assert.Equal("refused", _store.StateFacts(_mina, Whole() with { Lives = new(ScopeKind.Circle) }));
        Assert.Equal("refused", _store.StateFacts(_mina, Whole() with { Speaks = new(ScopeKind.Circle, saras) }));
        Assert.Equal("refused", _store.StateFacts("no-such-account", Whole()));

        var kept = _store.Facts(_mina)!;
        Assert.Equal((1985, "Shiraz", "Iran", "https://example.org/mina"), (kept.BornYear, kept.Town, kept.Country, kept.Link));
        Assert.Equal(["fa", "en", "de"], kept.Languages);

        // As many languages as an account may name are kept.
        Assert.Equal("done", _store.StateFacts(_mina, Whole() with { Languages = ["fa", "en", "de", "fr", "it", "es", "ar", "tr"] }));
    }

    [Fact]
    public void A_fact_that_is_taken_away_is_for_nobody_and_keeps_no_audience()
    {
        Assert.Equal("done", _store.StateFacts(_mina, Whole()));
        Assert.Equal("done", _store.StateFacts(_mina, AccountFacts.None with { Born = Everybody, Lives = Everybody, Speaks = Everybody, Linked = Everybody, Town = "  ", Link = " " }));

        var kept = _store.Facts(_mina)!;
        Assert.Equal((null, null, null, null), (kept.BornYear, kept.Town, kept.Country, kept.Link));
        Assert.Empty(kept.Languages);
        Assert.Equal((FactAudience.Nobody, FactAudience.Nobody, FactAudience.Nobody, FactAudience.Nobody), (kept.Born, kept.Lives, kept.Speaks, kept.Linked));
        Assert.Same(ShownFacts.None, Seen(null));
    }

    [Fact]
    public void A_circle_that_is_deleted_takes_what_was_for_it_back_to_the_account_alone()
    {
        Assert.Equal("done", _store.StateFacts(_mina, Whole() with { Born = Circle, Speaks = Circle, Linked = Circle }));
        Assert.Equal(1985, Seen(_sara).BornYear);

        Assert.True(_store.RemoveCircle(_mina, _close));

        Assert.Same(ShownFacts.None, Seen(_sara));
        var kept = _store.Facts(_mina)!;
        Assert.Equal((FactAudience.Nobody, FactAudience.Nobody, FactAudience.Nobody, FactAudience.Nobody), (kept.Born, kept.Lives, kept.Speaks, kept.Linked));
        Assert.Equal((1985, "Shiraz"), (kept.BornYear, kept.Town));
    }

    [Fact]
    public void Looking_as_somebody_else_shows_what_that_viewer_is_shown()
    {
        Assert.Equal("done", _store.StateFacts(_mina, Whole()));

        var asPublic = _store.SeeAs(_mina, "public")!.Profile.Facts;
        var asCircle = _store.SeeAs(_mina, $"circle-{_close}")!.Profile.Facts;
        var asDara = _store.SeeAs(_mina, $"account-{_dara}")!.Profile.Facts;

        Assert.Equal((1985, null, "https://example.org/mina"), (asPublic.BornYear, asPublic.Town, asPublic.Link));
        Assert.Equal((1985, "Shiraz", "Iran"), (asCircle.BornYear, asCircle.Town, asCircle.Country));
        Assert.Equal((1985, null), (asDara.BornYear, asDara.Town));
        Assert.Empty(asCircle.Languages);
    }

    [Fact]
    public void Who_was_blocked_and_an_account_that_is_closed_are_told_nothing()
    {
        Assert.Equal("done", _store.StateFacts(_mina, Whole()));

        Assert.True(_store.Block(_mina, _dara));
        Assert.Null(_store.Profile(_dara, _mina));

        Assert.True(_store.Accounts.Close(_mina, Password).Done);
        Assert.Null(_store.Profile(null, _mina));
        Assert.Null(_store.Facts(_mina));
        Assert.Equal("refused", _store.StateFacts(_mina, Whole()));
    }

    [Fact]
    public void A_copy_of_ones_data_holds_the_facts_with_who_each_is_shown_to()
    {
        Assert.Equal("done", _store.StateFacts(_mina, Whole()));

        var account = _store.Export(_mina, Password)!["account"]!;

        Assert.Equal((1985, "public"), ((int?)account["bornYear"], (string?)account["bornYearShownTo"]));
        Assert.Equal(("Shiraz", "Iran", "circle: Close"), ((string?)account["town"], (string?)account["country"], (string?)account["livesShownTo"]));
        Assert.Equal(["fa", "en", "de"], account["languages"]!.AsArray().Select(tag => (string?)tag));
        Assert.Equal("private", (string?)account["languagesShownTo"]);
        Assert.Equal(("https://example.org/mina", "public"), ((string?)account["link"], (string?)account["linkShownTo"]));

        // An account that states nothing says so: the keys are there and hold nothing.
        var other = _store.Export(_sara, Password)!["account"]!.AsObject();
        Assert.True(other.ContainsKey("bornYear") && other["bornYear"] is null && other["bornYearShownTo"] is null && other["languages"] is null && other["link"] is null && other["livesShownTo"] is null);
    }

    [Fact]
    public void The_languages_an_account_can_name_have_each_a_tag_once_and_a_name_of_their_own()
    {
        Assert.Equal(AccountLanguages.Known.Length, AccountLanguages.Known.Select(language => language.Tag).Distinct().Count());
        Assert.All(AccountLanguages.Known, language => Assert.False(string.IsNullOrWhiteSpace(language.Name)));
        Assert.Equal("فارسی", AccountLanguages.Name("fa"));
        Assert.Equal("xx", AccountLanguages.Name("xx"));
        Assert.True(AccountLanguages.Is("en") && !AccountLanguages.Is("EN") && !AccountLanguages.Is(string.Empty));
    }

    [Fact]
    public void The_tables_are_what_the_history_of_their_changes_of_form_makes_them()
    {
        using var context = new PeopleContext(PeopleStore.Options(_database));

        // A change of the model that no change of form carries would be found here – also one written by hand.
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Empty(context.Database.GetPendingMigrations());
    }
}
