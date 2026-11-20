using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;
using Vafadar.Temporal;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// Relationships as records of their own (step 19.6, ADR 0018, R-PER-02): two accounts show several side by
/// side, each with its kind, its direction, where and when it holds; kinds of one group exclude each other;
/// each is agreed to, shown and ended by itself; and one that is to be called something else holds as it is
/// until the other side agrees.
/// </summary>
public sealed class SeveralRelationshipsTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;

    public SeveralRelationshipsTests()
    {
        _store = PeopleStore.Open(TestDatabase.For(Path.Combine(_folder, "people.db")), _clock);
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

    // One asks, the other accepts; returns the identifier of the relationship.
    private string Shown(string from, string to, string toName, string kind, RelationshipTerms? terms = null)
    {
        Assert.Equal("asked", _store.Ask(from, toName, kind, terms));
        var id = _store.Relationships(to).Single(relationship => relationship.State == RelationshipState.Requested && !relationship.AskedByMe).Id;
        Assert.True(_store.Answer(to, id, accept: true));
        return id;
    }

    private static Occurrence Year(int year) => new(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, year));

    private string[] Seen(string? viewer, string of) =>
        [.. _store.Profile(viewer, of)!.Relationships.Select(shown => $"{shown.Other.Name}:{shown.Kind}{(shown.Terms.Context is { } context ? $"@{context}" : null)}")];

    [Fact]
    public void A_friend_can_be_a_colleague_and_a_classmate_too()
    {
        Shown(_mina, _sara, "Sara", "close-friend");
        Shown(_sara, _mina, "Mina", "colleague", new(Context: "The Zabol Press"));
        Shown(_mina, _sara, "Sara", "classmate", new(Context: "School of Kabul", Since: Year(1998), Until: Year(2002)));

        Assert.Equal(["Sara:close-friend", "Sara:colleague@The Zabol Press", "Sara:classmate@School of Kabul"], Seen(_mina, _mina));
        var classmate = _store.Relationships(_sara).Single(relationship => relationship.Kind == "classmate");
        Assert.Equal((Year(1998), Year(2002), "School of Kabul"), (classmate.Terms.Since, classmate.Terms.Until, classmate.Terms.Context));
    }

    [Fact]
    public void A_colleague_at_two_employers_is_two_relationships_and_at_one_is_one()
    {
        Shown(_mina, _sara, "Sara", "colleague", new(Context: "The Zabol Press"));

        // The same kind in the same place – however it is written, and whoever asks – is there already.
        Assert.Equal("exists", _store.Ask(_mina, "Sara", "colleague", new(Context: " the zabol press ")));
        Assert.Equal("exists", _store.Ask(_sara, "Mina", "colleague", new(Context: "The Zabol Press")));

        Assert.Equal("asked", _store.Ask(_sara, "Mina", "colleague", new(Context: "Kabul Books")));
        Assert.Equal("exists", _store.Ask(_mina, "Sara", "colleague", new(Context: "Kabul Books")));
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "colleague"));
    }

    [Theory]
    [InlineData("friend", "close-friend")]
    [InlineData("parent", "sibling")]
    [InlineData("guardian", "adoptive-parent")]
    [InlineData("partner", "family")]
    public void Two_people_hold_one_family_role_and_one_step_of_friendship(string has, string asked)
    {
        Shown(_mina, _sara, "Sara", has);

        Assert.Equal("excluded", _store.Ask(_mina, "Sara", asked));
        Assert.Equal("excluded", _store.Ask(_sara, "Mina", asked));

        // With somebody else the same kind is free: a child has a parent by birth and a guardian in two people.
        Assert.Equal("asked", _store.Ask(_dara, "Sara", asked));
    }

    [Fact]
    public void A_kind_with_a_direction_says_who_holds_the_role_to_each_side()
    {
        // Mina says: Sara is my guardian, since 2010.
        Shown(_mina, _sara, "Sara", "guardian", new(OtherHolds: true, Since: Year(2010), Until: Year(2018)));
        // Dara says: I am the parent of Sara.
        Shown(_dara, _sara, "Sara", "parent");

        Assert.True(_store.Relationships(_mina).Single().Terms.OtherHolds);
        var sara = _store.Relationships(_sara).ToDictionary(relationship => relationship.Other.Name);
        Assert.False(sara["Mina"].Terms.OtherHolds);
        Assert.True(sara["Dara"].Terms.OtherHolds);
        Assert.False(_store.Relationships(_dara).Single().Terms.OtherHolds);

        // A page about Sara reads the roles from her side.
        var page = _store.Profile(_sara, _sara)!.Relationships.ToDictionary(shown => shown.Other.Name);
        Assert.Equal((false, Year(2010)), (page["Mina"].Terms.OtherHolds, page["Mina"].Terms.Since));
        Assert.True(page["Dara"].Terms.OtherHolds);

        // A kind without a direction has none, whatever was sent.
        Shown(_mina, _dara, "Dara", "friend", new(OtherHolds: true));
        Assert.False(_store.Relationships(_dara).Single(relationship => relationship.Kind == "friend").Terms.OtherHolds);
    }

    [Theory]
    [InlineData(121, 2000, 2001)]
    [InlineData(10, 2005, 2001)]
    public void A_context_and_a_time_that_are_none_are_refused(int context, int since, int until)
    {
        Assert.Equal("terms", _store.Ask(_mina, "Sara", "colleague", new(Context: new string('a', context), Since: Year(since), Until: Year(until))));
        Assert.Equal("terms", _store.Ask(_mina, "Sara", "colleague", new(Context: "Two\nlines")));
        Assert.Empty(_store.Relationships(_mina));
    }

    [Fact]
    public void Each_relationship_is_agreed_to_shown_and_ended_by_itself()
    {
        var friend = Shown(_mina, _sara, "Sara", "friend");
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "colleague", new(Context: "The Zabol Press")));
        var colleague = _store.Relationships(_sara).Single(relationship => relationship.Kind == "colleague").Id;

        // Being friends is no agreement to be shown as colleagues.
        Assert.Equal(["Sara:friend"], Seen(_mina, _mina));
        Assert.True(_store.Answer(_sara, colleague, accept: false));
        Assert.Equal("declined-lately", _store.Ask(_mina, "Sara", "colleague", new(Context: "The Zabol Press")));

        // What was declined is that kind in that place, not everything between the two.
        var classmate = Shown(_mina, _sara, "Sara", "classmate", new(Context: "School of Kabul"));

        // Each has its own audience on each side.
        Assert.True(_store.Show(_mina, classmate, ScopeKind.Public));
        Assert.True(_store.Show(_sara, classmate, ScopeKind.Public));
        Assert.Equal(["Sara:classmate@School of Kabul"], Seen(null, _mina));
        Assert.Equal(["Sara:friend", "Sara:classmate@School of Kabul"], Seen(_sara, _mina));

        // Ending one leaves the other, and the two still know each other.
        var circle = _store.AddCircle(_mina, "Close")!.Value;
        Assert.True(_store.SetCircle(_mina, circle, [_sara]));
        Assert.True(_store.End(_sara, friend));
        Assert.Equal(["Sara:classmate@School of Kabul"], Seen(_mina, _mina));
        Assert.Equal(["Sara"], _store.Circles(_mina).Single().Members.Select(member => member.Name));

        // Ending the last one: they are no longer people each other knows.
        Assert.True(_store.End(_mina, classmate));
        Assert.Empty(_store.Circles(_mina).Single().Members);
    }

    [Fact]
    public void A_relationship_that_is_to_be_called_something_else_holds_as_it_is_until_the_other_agrees()
    {
        var friend = Shown(_mina, _sara, "Sara", "friend");
        Assert.True(_store.Show(_mina, friend, ScopeKind.Public));
        Assert.True(_store.Show(_sara, friend, ScopeKind.Public));

        Assert.Equal("same", _store.AskChange(_sara, friend, "friend"));
        Assert.Equal("refused", _store.AskChange(_dara, friend, "close-friend"));
        Assert.Equal("asked", _store.AskChange(_sara, friend, "close-friend"));

        // "Close friend" would exclude "friend" – but it is asked to take its place, not to stand beside it.
        Assert.Equal("exists", _store.AskChange(_mina, friend, "close-friend"));
        var request = _store.Relationships(_mina).Single(relationship => relationship.State == RelationshipState.Requested);
        Assert.Equal((friend, "close-friend", false), (request.Replaces, request.Kind, request.AskedByMe));

        // Until Mina agrees, everybody sees what was agreed to.
        Assert.Equal(["Sara:friend"], Seen(null, _mina));

        Assert.True(_store.Answer(_mina, request.Id, accept: true));

        // The new one took the place of the old, with the audience each side had chosen.
        Assert.Equal(["Sara:close-friend"], Seen(null, _mina));
        var kept = Assert.Single(_store.Relationships(_sara));
        Assert.Equal((RelationshipState.Accepted, ScopeKind.Public, null), (kept.State, kept.Shown, kept.Replaces));
    }

    [Fact]
    public void A_change_that_is_declined_or_taken_back_leaves_the_relationship_as_it_was()
    {
        var friend = Shown(_mina, _sara, "Sara", "friend");
        var circle = _store.AddCircle(_mina, "Close")!.Value;
        _store.SetCircle(_mina, circle, [_sara]);

        Assert.Equal("asked", _store.AskChange(_mina, friend, "close-friend"));
        var request = _store.Relationships(_sara).Single(relationship => relationship.Replaces == friend).Id;
        Assert.True(_store.Answer(_sara, request, accept: false));

        Assert.Equal(["Sara:friend"], Seen(_mina, _mina));
        Assert.Equal(["Sara"], _store.Circles(_mina).Single().Members.Select(member => member.Name));

        // Taken back by who asked, nothing of it is kept; and a request goes with the relationship it is about.
        Assert.Equal("asked", _store.AskChange(_sara, friend, "close-friend"));
        Assert.True(_store.Cancel(_sara, _store.Relationships(_sara).Single(relationship => relationship.Replaces == friend).Id));
        Assert.Equal("asked", _store.AskChange(_sara, friend, "close-friend"));
        Assert.True(_store.End(_mina, friend));
        Assert.Empty(_store.Relationships(_mina));
        Assert.Empty(_store.Relationships(_sara));
    }

    [Fact]
    public void The_copy_of_my_data_says_each_relationship_with_what_it_says()
    {
        Shown(_mina, _sara, "Sara", "guardian", new(OtherHolds: true, Since: Year(2010)));
        Shown(_mina, _dara, "Dara", "colleague", new(Context: "The Zabol Press"));

        var written = _store.Export(_mina, Password)!["relationships"]!.AsArray().ToDictionary(item => (string)item!["with"]!, item => item!);

        Assert.Equal(("guardian", true, null), ((string?)written["Sara"]["kind"], (bool?)written["Sara"]["theyHoldTheRole"], (string?)written["Sara"]["context"]));
        Assert.Equal(TemporalJson.Serialize(Year(2010)), written["Sara"]["since"]!.ToJsonString());
        Assert.Equal(("colleague", null, "The Zabol Press"), ((string?)written["Dara"]["kind"], (bool?)written["Dara"]["theyHoldTheRole"], (string?)written["Dara"]["context"]));
    }
}
