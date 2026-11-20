using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// Seeing as somebody else (step 19.6, ADR 0018, PRIV-015): an account looks at its own page and its own tree
/// as the public, as one of its circles or as an account it knows would – by the rules that draw the real
/// page – and learns nothing by it that is somebody else's to tell.
/// </summary>
public sealed class SeenAsTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;
    private readonly long _close;

    public SeenAsTests()
    {
        _store = PeopleStore.Open(TestDatabase.For(Path.Combine(_folder, "people.db")), _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
        _dara = _store.Register("Dara", Password).Session!.Id;
        _close = _store.AddCircle(_mina, "Close")!.Value;
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private string Shown(string from, string to, string toName, string kind)
    {
        Assert.Equal("asked", _store.Ask(from, toName, kind));
        var id = _store.Relationships(to).Single(relationship => relationship.State == RelationshipState.Requested && !relationship.AskedByMe).Id;
        Assert.True(_store.Answer(to, id, accept: true));
        return id;
    }

    private void Photo(string id, ScopeKind shown, long? circle = null) =>
        Assert.True(_store.AddPhoto(_mina, id, new PhotoFile($"{id}.jpg", "image/jpeg", 800, 600, 12345), id, null, shown, circle));

    private static string[] Related(SeenAs seen) => [.. seen.Profile.Relationships.Select(shown => $"{shown.Other.Name}:{shown.Kind}")];

    [Fact]
    public void The_page_is_seen_as_the_public_a_circle_and_a_named_account_see_it()
    {
        var friend = Shown(_mina, _sara, "Sara", "friend");
        var colleague = Shown(_mina, _dara, "Dara", "colleague");
        _store.SetCircle(_mina, _close, [_sara]);
        Assert.True(_store.Show(_mina, friend, ScopeKind.Public));
        Assert.True(_store.Show(_sara, friend, ScopeKind.Public));
        Assert.True(_store.Show(_mina, colleague, ScopeKind.Circle, _close));
        Photo("for-nobody", ScopeKind.Private);
        Photo("for-the-circle", ScopeKind.Circle, _close);
        Photo("for-everybody", ScopeKind.Public);
        var school = _store.AddAffiliation(_mina, new("School of Kabul"))!.Value;
        Assert.True(_store.ShowAffiliation(_mina, school, ScopeKind.Circle, _close, ScopeKind.Private, null));

        var everybody = _store.SeeAs(_mina, "public")!;
        var circle = _store.SeeAs(_mina, $"circle-{_close}")!;
        var sara = _store.SeeAs(_mina, $"account-{_sara}")!;
        var dara = _store.SeeAs(_mina, $"account-{_dara}")!;

        Assert.Equal(("public", null), (everybody.Kind, everybody.Name));
        Assert.Equal(["Sara:friend"], Related(everybody));
        Assert.Equal(["for-everybody"], everybody.Photos.Select(photo => photo.Id));
        Assert.Empty(everybody.Profile.Affiliations);

        // The circle: what is shown to everybody and what is shown to it.
        Assert.Equal(("circle", "Close"), (circle.Kind, circle.Name));
        Assert.Equal(["Dara:colleague", "Sara:friend"], Related(circle));
        Assert.Equal(["for-everybody", "for-the-circle"], circle.Photos.Select(photo => photo.Id).Order());
        Assert.Single(circle.Profile.Affiliations);

        // Sara is in the circle; Dara is not, and sees the relationship he is a side of.
        Assert.Equal(("account", "Sara"), (sara.Kind, sara.Name));
        Assert.Equal(["Dara:colleague", "Sara:friend"], Related(sara));
        Assert.Equal(["for-everybody", "for-the-circle"], sara.Photos.Select(photo => photo.Id).Order());
        Assert.Equal(["Dara:colleague", "Sara:friend"], Related(dara));
        Assert.Equal(["for-everybody"], dara.Photos.Select(photo => photo.Id));
        Assert.Empty(dara.Profile.Affiliations);

        // What a look shows is what the real page shows that viewer – of the account's own things.
        Assert.Equal(_store.LifeOf(_sara, _mina).Select(photo => photo.Id).Order(), sara.Photos.Select(photo => photo.Id).Order());
        Assert.Equal(_store.LifeOf(null, _mina).Select(photo => photo.Id), everybody.Photos.Select(photo => photo.Id));
        Assert.Equal(_store.Profile(null, _mina)!.Relationships.Select(shown => shown.Other.Name), everybody.Profile.Relationships.Select(shown => shown.Other.Name));

        // Looking as a circle leaves nothing behind: the real viewers see what they saw.
        Assert.Equal(["for-everybody"], _store.LifeOf(_dara, _mina).Select(photo => photo.Id));
    }

    [Fact]
    public void A_look_tells_nothing_that_is_the_other_sides_to_tell()
    {
        var friend = Shown(_mina, _sara, "Sara", "friend");
        Shown(_mina, _dara, "Dara", "colleague");
        Assert.True(_store.Show(_mina, friend, ScopeKind.Public));

        // Sara keeps the relationship to the two: the public does not see it – and Mina, looking as the public,
        // sees what everybody sees. Looking as Dara she sees what she herself lets him see, whatever Sara chose:
        // the answer is the same when Sara opens it, so it says nothing about her choice.
        Assert.Empty(Related(_store.SeeAs(_mina, "public")!));
        var before = Related(_store.SeeAs(_mina, $"account-{_dara}")!);
        Assert.True(_store.Show(_sara, friend, ScopeKind.Public));
        Assert.Equal(before, Related(_store.SeeAs(_mina, $"account-{_dara}")!));
        Assert.Equal(["Dara:colleague", "Sara:friend"], before);

        // A photograph of somebody else that names Mina is that owner's to show: no look of Mina's lists it.
        Assert.True(_store.AddPhoto(_sara, "saras", new PhotoFile("saras.jpg", "image/jpeg", 800, 600, 12345), "Sara's", null, ScopeKind.Public));
        Assert.True(_store.TagPhoto(_sara, "saras", _mina));
        Assert.True(_store.AnswerTag(_mina, "saras", approve: true));
        Assert.Empty(_store.SeeAs(_mina, $"account-{_dara}")!.Photos);
        Assert.Empty(_store.SeeAs(_mina, $"account-{_sara}")!.Photos);
    }

    [Theory]
    [InlineData("somebody")]
    [InlineData("circle-999")]
    [InlineData("circle-x")]
    [InlineData("account-nobody")]
    [InlineData("")]
    public void Nobody_looks_as_somebody_they_cannot_name(string asWhom)
    {
        Assert.Null(_store.SeeAs(_mina, asWhom));

        // Not as an account that is no relationship of theirs, and not as a circle of somebody else.
        Assert.Null(_store.SeeAs(_mina, $"account-{_sara}"));
        Assert.Null(_store.SeeAs(_sara, $"circle-{_close}"));
    }

    [Fact]
    public void A_tree_is_seen_as_a_member_sees_it_and_as_everybody_does_only_when_it_is_public()
    {
        Shown(_mina, _sara, "Sara", "family");
        Shown(_mina, _dara, "Dara", "friend");
        var tree = _store.AddTree(_mina, "The Rad family")!;
        _store.AddPerson(_mina, tree, "Zal", 1870, 1950, deceased: true);
        _store.SetTreeMember(_mina, tree, _sara, TreeRole.Viewer);

        // Sara sees it – without anything to do, and without who else it is shared with or its history.
        var sara = _store.SeeTreeAs(_mina, tree, $"account-{_sara}")!;
        Assert.Equal(("account", "Sara", "Zal"), (sara.Kind, sara.Name, sara.Tree!.People.Single().Name));
        Assert.Equal((0, 0, false), (sara.Tree.Members.Length, sara.Tree.History.Length, sara.Tree.CanClaim));

        // Dara, the public and a circle do not: the tree is shared with accounts and is not public.
        Assert.Null(_store.SeeTreeAs(_mina, tree, $"account-{_dara}")!.Tree);
        Assert.Null(_store.SeeTreeAs(_mina, tree, "public")!.Tree);
        Assert.Null(_store.SeeTreeAs(_mina, tree, $"circle-{_close}")!.Tree);

        Assert.True(_store.AskPublication(_mina, tree));
        Assert.True(_store.Publish(_mina, tree));
        Assert.Equal("Zal", _store.SeeTreeAs(_mina, tree, "public")!.Tree!.People.Single().Name);
        Assert.NotNull(_store.SeeTreeAs(_mina, tree, $"circle-{_close}")!.Tree);

        // Only the owner looks at a tree as somebody else.
        Assert.Null(_store.SeeTreeAs(_sara, tree, "public"));
        Assert.Null(_store.SeeTreeAs(_mina, "no-such-tree", "public"));
    }
}
