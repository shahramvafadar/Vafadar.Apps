using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// The relationships two accounts show together and the circles that name an audience (step 16.3, PRIV-005 to
/// PRIV-009, PRIV-012): asked by one, accepted by the other and by nobody else, shown to who both allow, ended
/// by either.
/// </summary>
public sealed class RelationshipsAndCirclesTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;

    public RelationshipsAndCirclesTests()
    {
        _store = PeopleStore.Open(Path.Combine(_folder, "people.db"), _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
        _dara = _store.Register("Dara", Password).Session!.Id;
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    // Mina asks Sara, Sara accepts; returns the identifier of the relationship.
    private string Friends(string from, string to, string toName, string kind = "friend")
    {
        Assert.Equal("asked", _store.Ask(from, toName, kind));
        var id = _store.Relationships(to).Single(relationship => relationship.Other.Id == from).Id;
        Assert.True(_store.Answer(to, id, accept: true));
        return id;
    }

    private string[] Seen(string? viewer, string of) => [.. _store.Profile(viewer, of)!.Relationships.Select(shown => $"{shown.Other.Name}:{shown.Kind}")];

    [Fact]
    public void A_request_waits_for_the_other_side_and_is_seen_by_the_two_only()
    {
        Assert.Equal("asked", _store.Ask(_mina, " sara ", "close-friend"));

        var asked = Assert.Single(_store.Relationships(_mina));
        Assert.Equal(("Sara", "close-friend", RelationshipState.Requested, true), (asked.Other.Name, asked.Kind, asked.State, asked.AskedByMe));
        var waiting = Assert.Single(_store.Relationships(_sara));
        Assert.Equal(("Mina", RelationshipState.Requested, false), (waiting.Other.Name, waiting.State, waiting.AskedByMe));
        Assert.Equal((0, 1, 0), (_store.Waiting(_mina), _store.Waiting(_sara), _store.Waiting(_dara)));
        Assert.Empty(_store.Relationships(_dara));

        // A request is no relationship: nobody is shown anything.
        Assert.Empty(Seen(_mina, _mina));
        Assert.Empty(Seen(_sara, _mina));
        Assert.Empty(Seen(null, _sara));
    }

    [Theory]
    [InlineData("Nobody", "friend", "unknown")]
    [InlineData("Mina", "friend", "self")]
    [InlineData("Sara", "enemy", "kind")]
    [InlineData("Sara", "", "kind")]
    public void What_cannot_be_asked_says_why(string name, string kind, string outcome)
    {
        Assert.Equal(outcome, _store.Ask(_mina, name, kind));
        Assert.Empty(_store.Relationships(_mina));
    }

    [Fact]
    public void Two_accounts_have_one_request_or_relationship_at_a_time()
    {
        _store.Ask(_mina, "Sara", "friend");

        Assert.Equal("exists", _store.Ask(_mina, "Sara", "family"));
        Assert.Equal("exists", _store.Ask(_sara, "Mina", "friend"));

        _store.Answer(_sara, _store.Relationships(_sara)[0].Id, accept: true);
        Assert.Equal("exists", _store.Ask(_sara, "Mina", "colleague"));
    }

    [Fact]
    public void Only_the_side_that_was_asked_answers()
    {
        _store.Ask(_mina, "Sara", "friend");
        var id = _store.Relationships(_mina)[0].Id;

        Assert.False(_store.Answer(_mina, id, accept: true));
        Assert.False(_store.Answer(_dara, id, accept: true));
        Assert.False(_store.Answer(_sara, "no-such-relationship", accept: true));
        Assert.Equal(RelationshipState.Requested, _store.Relationships(_sara)[0].State);

        Assert.True(_store.Answer(_sara, id, accept: true));
        Assert.False(_store.Answer(_sara, id, accept: true));
        Assert.False(_store.Answer(_sara, id, accept: false));
        Assert.Equal(RelationshipState.Accepted, _store.Relationships(_mina)[0].State);
        Assert.Equal(0, _store.Waiting(_sara));
    }

    [Fact]
    public void An_accepted_relationship_is_shown_to_the_two_and_to_nobody_else_until_both_say_so()
    {
        var id = Friends(_mina, _sara, "Sara");

        Assert.Equal(["Sara:friend"], Seen(_mina, _mina));
        Assert.Equal(["Mina:friend"], Seen(_sara, _sara));
        Assert.Equal(["Sara:friend"], Seen(_sara, _mina));
        Assert.Empty(Seen(_dara, _mina));
        Assert.Empty(Seen(null, _mina));

        // One side opens it to everybody: the other's restriction still holds (PRIV-008).
        Assert.True(_store.Show(_mina, id, ScopeKind.Public));
        Assert.Empty(Seen(_dara, _mina));
        Assert.Empty(Seen(null, _sara));

        Assert.True(_store.Show(_sara, id, ScopeKind.Public));
        Assert.Equal(["Sara:friend"], Seen(_dara, _mina));
        Assert.Equal(["Mina:friend"], Seen(null, _sara));

        // And one side taking it back hides it from everybody else at once.
        Assert.True(_store.Show(_sara, id, ScopeKind.Private));
        Assert.Empty(Seen(_dara, _mina));
        Assert.Empty(Seen(null, _mina));
        Assert.Equal(["Sara:friend"], Seen(_sara, _mina));
    }

    [Fact]
    public void A_side_sets_its_own_audience_and_no_other()
    {
        var id = Friends(_mina, _sara, "Sara");

        Assert.False(_store.Show(_dara, id, ScopeKind.Public));
        Assert.False(_store.Show(_mina, "no-such-relationship", ScopeKind.Public));
        Assert.False(_store.Show(_mina, id, (ScopeKind)7));
        Assert.True(_store.Show(_mina, id, ScopeKind.Public));

        Assert.Equal(ScopeKind.Public, _store.Relationships(_mina)[0].Shown);
        Assert.Equal(ScopeKind.Private, _store.Relationships(_sara)[0].Shown);
    }

    [Fact]
    public void A_circle_names_people_its_owner_knows_and_shows_a_relationship_to_them_only()
    {
        var withSara = Friends(_mina, _sara, "Sara");
        Friends(_mina, _dara, "Dara", "colleague");
        var zal = _store.Register("Zal", Password).Session!.Id;
        var circle = _store.AddCircle(_mina, " Close ")!.Value;

        // Somebody the owner has no relationship with does not come into a circle.
        Assert.True(_store.SetCircle(_mina, circle, [_dara, zal, _dara, "nobody"]));
        var info = Assert.Single(_store.Circles(_mina));
        Assert.Equal("Close", info.Name);
        Assert.Equal(["Dara"], info.Members.Select(member => member.Name));

        Assert.True(_store.Show(_mina, withSara, ScopeKind.Circle, circle));
        Assert.True(_store.Show(_sara, withSara, ScopeKind.Public));

        Assert.Equal((ScopeKind.Circle, circle), (_store.Relationships(_mina).Single(relationship => relationship.Id == withSara).Shown, _store.Relationships(_mina).Single(relationship => relationship.Id == withSara).Circle!.Value));
        Assert.Contains("Sara:friend", Seen(_dara, _mina));
        Assert.DoesNotContain("Sara:friend", Seen(zal, _mina));
        Assert.Empty(Seen(null, _mina));
    }

    [Fact]
    public void A_relationship_is_shown_to_a_circle_of_its_own_side_only()
    {
        var id = Friends(_mina, _sara, "Sara");
        var saras = _store.AddCircle(_sara, "Family")!.Value;

        Assert.False(_store.Show(_mina, id, ScopeKind.Circle, saras));
        Assert.False(_store.Show(_mina, id, ScopeKind.Circle));
        Assert.False(_store.SetCircle(_mina, saras, [_sara]));
        Assert.False(_store.RemoveCircle(_mina, saras));
        Assert.Equal(ScopeKind.Private, _store.Relationships(_mina)[0].Shown);
    }

    [Fact]
    public void Removing_a_circle_narrows_what_was_shown_to_it_and_widens_nothing()
    {
        var withSara = Friends(_mina, _sara, "Sara");
        Friends(_mina, _dara, "Dara");
        var circle = _store.AddCircle(_mina, "Close")!.Value;
        _store.SetCircle(_mina, circle, [_dara]);
        _store.Show(_mina, withSara, ScopeKind.Circle, circle);
        _store.Show(_sara, withSara, ScopeKind.Public);
        Assert.Contains("Sara:friend", Seen(_dara, _mina));

        Assert.True(_store.RemoveCircle(_mina, circle));

        Assert.Empty(_store.Circles(_mina));
        Assert.DoesNotContain("Sara:friend", Seen(_dara, _mina));
        Assert.Equal((ScopeKind.Private, (long?)null), (_store.Relationships(_mina).Single(relationship => relationship.Id == withSara).Shown, _store.Relationships(_mina).Single(relationship => relationship.Id == withSara).Circle));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a name that is far too long to be the name of a circle")]
    [InlineData("two\nlines")]
    public void A_circle_needs_a_plain_name(string name)
    {
        Assert.Null(_store.AddCircle(_mina, name));
    }

    [Fact]
    public void An_account_has_no_two_circles_of_one_name_and_only_so_many()
    {
        Assert.NotNull(_store.AddCircle(_mina, "Family"));
        Assert.Null(_store.AddCircle(_mina, "family"));
        Assert.NotNull(_store.AddCircle(_sara, "Family"));

        for (var number = 1; number < PeopleStore.MostCircles; number++)
        {
            Assert.NotNull(_store.AddCircle(_mina, $"Circle {number}"));
        }

        Assert.Null(_store.AddCircle(_mina, "One more"));
    }

    [Fact]
    public void Either_side_ends_a_relationship_and_each_leaves_the_circles_of_the_other()
    {
        var id = Friends(_mina, _sara, "Sara");
        var circle = _store.AddCircle(_mina, "Close")!.Value;
        _store.SetCircle(_mina, circle, [_sara]);
        _store.Show(_mina, id, ScopeKind.Public);
        _store.Show(_sara, id, ScopeKind.Public);

        Assert.False(_store.End(_dara, id));
        Assert.True(_store.End(_sara, id));
        Assert.False(_store.End(_mina, id));

        Assert.Empty(_store.Relationships(_mina));
        Assert.Empty(_store.Relationships(_sara));
        Assert.Empty(Seen(null, _mina));
        Assert.Empty(Seen(_mina, _mina));
        Assert.Empty(Assert.Single(_store.Circles(_mina)).Members);

        // Ended is not declined: the two may ask each other again.
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "friend"));
    }

    [Fact]
    public void A_request_is_taken_back_by_who_asked_before_it_is_answered()
    {
        _store.Ask(_mina, "Sara", "friend");
        var id = _store.Relationships(_mina)[0].Id;

        Assert.False(_store.Cancel(_sara, id));
        Assert.True(_store.Cancel(_mina, id));

        Assert.Empty(_store.Relationships(_sara));
        Assert.False(_store.Answer(_sara, id, accept: true));
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "friend"));

        // An accepted relationship is not taken back, it is ended.
        var accepted = _store.Relationships(_sara)[0].Id;
        _store.Answer(_sara, accepted, accept: true);
        Assert.False(_store.Cancel(_mina, accepted));
    }

    [Fact]
    public void Who_was_declined_waits_before_asking_the_same_account_again()
    {
        _store.Ask(_mina, "Sara", "friend");
        Assert.True(_store.Answer(_sara, _store.Relationships(_sara)[0].Id, accept: false));

        Assert.Empty(_store.Relationships(_mina));
        Assert.Empty(_store.Relationships(_sara));
        Assert.Equal("declined-lately", _store.Ask(_mina, "Sara", "friend"));

        // The one who declined may ask, and somebody else may be asked.
        Assert.Equal("asked", _store.Ask(_mina, "Dara", "friend"));
        _clock.Now += PeopleStore.AskAgainAfter + TimeSpan.FromMinutes(1);
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "friend"));
    }

    [Fact]
    public void An_account_has_only_so_many_requests_waiting()
    {
        for (var number = 0; number < PeopleStore.OpenRequests; number++)
        {
            _store.Register($"Visitor {number}", Password);
            Assert.Equal("asked", _store.Ask(_mina, $"Visitor {number}", "friend"));
        }

        Assert.Equal("too-many", _store.Ask(_mina, "Sara", "friend"));
    }

    [Fact]
    public void A_closed_account_shows_nothing_is_shown_nowhere_and_is_not_asked()
    {
        var id = Friends(_mina, _sara, "Sara");
        _store.Show(_mina, id, ScopeKind.Public);
        _store.Show(_sara, id, ScopeKind.Public);
        _store.Close(_sara, Password);

        Assert.Null(_store.Profile(null, _sara));
        Assert.Empty(Seen(null, _mina));
        Assert.Empty(_store.Relationships(_mina));
        Assert.Equal("unknown", _store.Ask(_dara, "Sara", "friend"));

        // Opened again within the thirty days, what it had is there again.
        _store.Reopen("Sara", Password);
        Assert.Equal(["Sara:friend"], Seen(null, _mina));

        // Deleted for good, the relationship goes with it.
        _store.Close(_sara, Password);
        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromMinutes(1);
        _store.Purge();
        _store.Register("Sara", Password);
        Assert.Empty(Seen(_mina, _mina));
        Assert.Empty(_store.Relationships(_mina));
    }

    [Fact]
    public void A_page_about_an_account_that_does_not_exist_is_nothing()
    {
        Assert.Null(_store.Profile(_mina, "no-such-account"));
        Assert.Equal("Mina", _store.Profile(null, _mina)!.Person.Name);
    }
}
