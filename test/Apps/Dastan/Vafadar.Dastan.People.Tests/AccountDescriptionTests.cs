using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// What an account says about itself on its page (D-086): a few words read by who their scope allows, and a
/// picture – one of the account's own photographs – seen by who may see that photograph. An account that says
/// nothing and one that says nothing to a viewer look the same to that viewer (PRIV-012).
/// </summary>
public sealed class AccountDescriptionTests : IDisposable
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

    public AccountDescriptionTests()
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

    private string Photo(string owner, string id, ScopeKind shown, long? circle = null)
    {
        Assert.True(_store.AddPhoto(owner, id, new PhotoFile($"{id}.jpg", "image/jpeg", 800, 600, 12345), "A portrait", null, shown, circle));
        return id;
    }

    private ProfileView Seen(string? viewer) => _store.Profile(viewer, _mina)!;

    [Fact]
    public void An_account_that_says_nothing_has_nothing_for_anybody()
    {
        Assert.Equal(new AccountDescription(null, ScopeKind.Private, null, null), _store.Description(_mina));
        Assert.All(new[] { null, _mina, _sara, _dara }, viewer => Assert.Equal((null, null), (Seen(viewer).About, Seen(viewer).Picture)));
        Assert.Null(_store.Description("no-such-account"));
    }

    [Theory]
    [InlineData(ScopeKind.Public, true, true, true)]
    [InlineData(ScopeKind.Circle, false, true, false)]
    [InlineData(ScopeKind.Private, false, false, false)]
    public void The_words_are_read_by_who_their_scope_allows_and_always_by_the_account(ScopeKind shown, bool everybody, bool sara, bool dara)
    {
        Assert.Equal("done", _store.DescribeAccount(_mina, "  I collect old maps.\r\nAnd stories. ", shown, shown == ScopeKind.Circle ? _close : null, null));

        const string Words = "I collect old maps.\nAnd stories.";
        Assert.Equal(Words, _store.Description(_mina)!.About);
        Assert.Equal(Words, Seen(_mina).About);
        Assert.Equal(everybody ? Words : null, Seen(null).About);
        Assert.Equal(sara ? Words : null, Seen(_sara).About);
        Assert.Equal(dara ? Words : null, Seen(_dara).About);
        Assert.Equal((shown, shown == ScopeKind.Circle ? _close : (long?)null), (_store.Description(_mina)!.AboutShown, _store.Description(_mina)!.AboutCircle));
    }

    [Fact]
    public void What_is_no_description_is_refused_and_nothing_changes()
    {
        Assert.Equal("done", _store.DescribeAccount(_mina, "Kept.", ScopeKind.Public, null, null));
        var saras = _store.AddCircle(_sara, "Hers")!.Value;

        Assert.Equal("long", _store.DescribeAccount(_mina, new string('x', PeopleStore.LongestAccountAbout + 1), ScopeKind.Public, null, null));
        Assert.Equal("refused", _store.DescribeAccount(_mina, "Other words.", (ScopeKind)9, null, null));
        Assert.Equal("refused", _store.DescribeAccount(_mina, "Other words.", ScopeKind.Circle, null, null));
        Assert.Equal("refused", _store.DescribeAccount(_mina, "Other words.", ScopeKind.Circle, saras, null));
        Assert.Equal("refused", _store.DescribeAccount("no-such-account", "Other words.", ScopeKind.Public, null, null));
        Assert.Equal("picture", _store.DescribeAccount(_mina, "Other words.", ScopeKind.Public, null, "no-such-photograph"));
        Assert.Equal("picture", _store.DescribeAccount(_mina, "Other words.", ScopeKind.Public, null, Photo(_sara, "saras", ScopeKind.Public)));

        Assert.Equal(new AccountDescription("Kept.", ScopeKind.Public, null, null), _store.Description(_mina));
        Assert.Equal("done", _store.DescribeAccount(_mina, new string('x', PeopleStore.LongestAccountAbout), ScopeKind.Public, null, null));
    }

    [Fact]
    public void Words_that_are_taken_away_are_for_nobody_and_keep_no_scope()
    {
        Assert.Equal("done", _store.DescribeAccount(_mina, "For everybody.", ScopeKind.Public, null, null));
        Assert.Equal("done", _store.DescribeAccount(_mina, "   ", ScopeKind.Public, null, null));

        Assert.Equal(new AccountDescription(null, ScopeKind.Private, null, null), _store.Description(_mina));
        Assert.Null(Seen(null).About);
    }

    [Fact]
    public void The_picture_is_seen_by_who_may_see_that_photograph_and_by_nobody_else()
    {
        var open = Photo(_mina, "open", ScopeKind.Public);
        var close = Photo(_mina, "close", ScopeKind.Circle, _close);
        var hidden = Photo(_mina, "hidden", ScopeKind.Private);

        Assert.Equal("done", _store.DescribeAccount(_mina, null, ScopeKind.Private, null, open));
        Assert.All(new[] { null, _mina, _sara, _dara }, viewer => Assert.Equal(open, Seen(viewer).Picture?.Id));

        Assert.Equal("done", _store.DescribeAccount(_mina, null, ScopeKind.Private, null, close));
        Assert.Equal((null, close, close, null), (Seen(null).Picture?.Id, Seen(_mina).Picture?.Id, Seen(_sara).Picture?.Id, Seen(_dara).Picture?.Id));

        Assert.Equal("done", _store.DescribeAccount(_mina, null, ScopeKind.Private, null, hidden));
        Assert.Equal((null, hidden, null, null), (Seen(null).Picture?.Id, Seen(_mina).Picture?.Id, Seen(_sara).Picture?.Id, Seen(_dara).Picture?.Id));

        // Who may see the photograph is said where the photograph is: shown to everybody, it is the picture for everybody.
        Assert.True(_store.SharePhoto(_mina, hidden, ScopeKind.Public));
        Assert.Equal(hidden, Seen(null).Picture?.Id);

        // The words have their own scope: a public picture does not show private words, and the other way round.
        Assert.Equal("done", _store.DescribeAccount(_mina, "Only mine.", ScopeKind.Private, null, hidden));
        Assert.Equal((null, hidden), (Seen(_sara).About, Seen(_sara).Picture?.Id));
    }

    [Fact]
    public void A_photograph_that_is_removed_is_the_picture_of_nobody_any_more()
    {
        var portrait = Photo(_mina, "portrait", ScopeKind.Public);
        Assert.Equal("done", _store.DescribeAccount(_mina, "Words.", ScopeKind.Public, null, portrait));

        Assert.NotNull(_store.RemovePhoto(_mina, portrait));

        Assert.Null(Seen(null).Picture);
        Assert.Null(_store.Description(_mina)!.Picture);
        Assert.Equal("Words.", Seen(null).About);

        // It is gone for good: naming it again is naming a photograph that is not the account's.
        Assert.Equal("picture", _store.DescribeAccount(_mina, "Words.", ScopeKind.Public, null, portrait));
        Assert.Equal("done", _store.DescribeAccount(_mina, "Words.", ScopeKind.Public, null, null));
    }

    [Fact]
    public void A_circle_that_is_deleted_takes_the_words_back_to_the_account_alone()
    {
        Assert.Equal("done", _store.DescribeAccount(_mina, "For the close ones.", ScopeKind.Circle, _close, null));
        Assert.Equal("For the close ones.", Seen(_sara).About);

        Assert.True(_store.RemoveCircle(_mina, _close));

        Assert.Null(Seen(_sara).About);
        Assert.Equal(new AccountDescription("For the close ones.", ScopeKind.Private, null, null), _store.Description(_mina));
    }

    [Fact]
    public void Looking_as_somebody_else_shows_what_that_viewer_is_shown()
    {
        var close = Photo(_mina, "close", ScopeKind.Circle, _close);
        Assert.Equal("done", _store.DescribeAccount(_mina, "For the close ones.", ScopeKind.Circle, _close, close));

        var asPublic = _store.SeeAs(_mina, "public")!.Profile;
        var asCircle = _store.SeeAs(_mina, $"circle-{_close}")!.Profile;
        var asSara = _store.SeeAs(_mina, $"account-{_sara}")!.Profile;
        var asDara = _store.SeeAs(_mina, $"account-{_dara}")!.Profile;

        Assert.Equal((null, null), (asPublic.About, asPublic.Picture?.Id));
        Assert.Equal(("For the close ones.", close), (asCircle.About, asCircle.Picture?.Id));
        Assert.Equal(("For the close ones.", close), (asSara.About, asSara.Picture?.Id));
        Assert.Equal((null, null), (asDara.About, asDara.Picture?.Id));
    }

    [Fact]
    public void Who_was_blocked_and_an_account_that_is_closed_are_told_nothing()
    {
        Assert.Equal("done", _store.DescribeAccount(_mina, "For everybody.", ScopeKind.Public, null, Photo(_mina, "open", ScopeKind.Public)));

        Assert.True(_store.Block(_mina, _dara));
        Assert.Null(_store.Profile(_dara, _mina));

        Assert.True(_store.Accounts.Close(_mina, Password).Done);
        Assert.Null(_store.Profile(null, _mina));
        Assert.Null(_store.Description(_mina));
        Assert.Equal("refused", _store.DescribeAccount(_mina, "After closing.", ScopeKind.Public, null, null));
    }

    [Fact]
    public void A_copy_of_ones_data_holds_what_the_account_says_about_itself()
    {
        var portrait = Photo(_mina, "portrait", ScopeKind.Public);
        Assert.Equal("done", _store.DescribeAccount(_mina, "For the close ones.", ScopeKind.Circle, _close, portrait));

        var account = _store.Export(_mina, Password)!["account"]!;

        Assert.Equal(("For the close ones.", "circle", "Close", portrait), ((string?)account["about"], (string?)account["aboutShownTo"], (string?)account["aboutCircle"], (string?)account["picture"]));

        // An account that says nothing says so: the keys are there and hold nothing.
        var other = _store.Export(_sara, Password)!["account"]!.AsObject();
        Assert.True(other.ContainsKey("about") && other["about"] is null && other["aboutShownTo"] is null && other["picture"] is null);
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
