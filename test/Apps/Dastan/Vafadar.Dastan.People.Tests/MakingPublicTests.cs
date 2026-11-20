using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// Making a family tree public (step 16.6, D-053, D-055, PRIV-014): an act of the owner that needs the consent
/// of every living person the tree names; the check says who stands in the way; consent that is taken back
/// hides the tree at once.
/// </summary>
public sealed class MakingPublicTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;
    private readonly string _tree;
    private readonly string _zal;
    private readonly string _saraRecord;
    private readonly string _relationship;

    public MakingPublicTests()
    {
        _store = PeopleStore.Open(TestDatabase.For(Path.Combine(_folder, "people.db")), _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
        _dara = _store.Register("Dara", Password).Session!.Id;
        _tree = _store.AddTree(_mina, "The Rad family")!;
        _zal = _store.AddPerson(_mina, _tree, "Zal", 1870, 1950, deceased: true)!;
        _saraRecord = _store.AddPerson(_mina, _tree, "Sara Rad", 1990, null, deceased: false)!;
        _store.Link(_mina, _tree, "parent", _zal, _saraRecord);
        _relationship = Friends(_mina, _sara, "Sara");
        Friends(_mina, _dara, "Dara");
        _store.SetTreeMember(_mina, _tree, _sara, TreeRole.Viewer);
        _store.SetTreeMember(_mina, _tree, _dara, TreeRole.Viewer);
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private string Friends(string from, string to, string toName)
    {
        Assert.Equal("asked", _store.Ask(from, toName, "family"));
        var id = _store.Relationships(to).Single(relationship => relationship.Other.Id == from).Id;
        Assert.True(_store.Answer(to, id, accept: true));
        return id;
    }

    // Sara says the record is her and Mina confirms.
    private void SaraIsHerRecord()
    {
        Assert.Equal("claimed", _store.Claim(_sara, _tree, _saraRecord));
        Assert.True(_store.ReviewClaim(_mina, _tree, _store.Tree(_mina, _tree)!.Claims.Single().Number, confirm: true));
    }

    private string[] Blockers() => [.. _store.Tree(_mina, _tree)!.Publication.Blockers.Select(blocker => $"{blocker.Name}:{blocker.Why}")];

    [Fact]
    public void A_tree_is_public_for_nobody_until_its_owner_asks_and_everybody_it_names_agrees()
    {
        Assert.Null(_store.PublicTree(_tree));
        Assert.Same(PublicationState.None, _store.Tree(_mina, _tree)!.Publication);
        Assert.False(_store.Publish(_mina, _tree));
        Assert.False(_store.Consent(_sara, _tree, agree: true));

        Assert.False(_store.AskPublication(_sara, _tree));
        Assert.True(_store.AskPublication(_mina, _tree));
        Assert.False(_store.AskPublication(_mina, _tree));

        // Zal died: he stands in nobody's way. Sara is living and is no account yet: nobody can agree for her.
        Assert.Equal(["Sara Rad:no-account"], Blockers());
        Assert.False(_store.Publish(_mina, _tree));

        SaraIsHerRecord();
        Assert.Equal(["Sara Rad:no-consent"], Blockers());
        Assert.False(_store.Publish(_mina, _tree));
        var asked = _store.Tree(_sara, _tree)!.Publication;
        Assert.Equal((true, true, false, false), (asked.Asked, asked.AskedOfMe, asked.Consented, asked.IsPublic));
        Assert.Empty(asked.Blockers);

        Assert.True(_store.Consent(_sara, _tree, agree: true));
        Assert.False(_store.Consent(_sara, _tree, agree: true));
        Assert.Empty(Blockers());

        // Everybody agreed – and still nothing is public until the owner does it.
        Assert.Null(_store.PublicTree(_tree));
        Assert.True(_store.Publish(_mina, _tree));
        Assert.False(_store.Publish(_mina, _tree));
        Assert.NotNull(_store.PublicTree(_tree));
        Assert.True(_store.Tree(_mina, _tree)!.Publication.IsPublic);
    }

    [Fact]
    public void Everybody_sees_the_people_and_the_links_of_a_public_tree_and_nothing_else()
    {
        SaraIsHerRecord();
        _store.AskPublication(_mina, _tree);
        _store.Consent(_sara, _tree, agree: true);
        _store.Publish(_mina, _tree);

        var tree = _store.PublicTree(_tree)!;

        Assert.Equal(("The Rad family", TreeRole.None), (tree.Tree.Name, tree.Tree.Role));
        Assert.Equal(["Sara Rad", "Zal"], tree.People.Select(person => person.Name));
        Assert.Equal((1870, 1950), (tree.Person(_zal).Born.Year(), tree.Person(_zal).Died.Year()));
        Assert.Equal([_zal], tree.Person(_saraRecord).Parents);
        Assert.Single(tree.Links);

        // Not which account a record is, who the tree is shared with, its history, or a claim.
        Assert.All(tree.People, person => Assert.Null(person.Account));
        Assert.Empty(tree.Members);
        Assert.Empty(tree.History);
        Assert.Empty(tree.Claims);
        Assert.Same(PublicationState.None, tree.Publication);
        Assert.Null(_store.PublicTree("no-such-tree"));
    }

    [Fact]
    public void Only_who_is_named_agrees_and_only_for_themselves()
    {
        SaraIsHerRecord();
        _store.AskPublication(_mina, _tree);

        // Dara sees the tree and is no record of it; Mina owns it and is none either.
        Assert.False(_store.Consent(_dara, _tree, agree: true));
        Assert.False(_store.Consent(_mina, _tree, agree: true));
        Assert.False(_store.Tree(_dara, _tree)!.Publication.AskedOfMe);
        Assert.False(_store.Publish(_dara, _tree));
        Assert.False(_store.Unpublish(_dara, _tree));
        Assert.Equal(["Sara Rad:no-consent"], Blockers());
    }

    [Fact]
    public void Consent_that_is_taken_back_hides_the_tree_at_once_and_given_again_shows_it()
    {
        SaraIsHerRecord();
        _store.AskPublication(_mina, _tree);
        _store.Consent(_sara, _tree, agree: true);
        _store.Publish(_mina, _tree);

        Assert.True(_store.Consent(_sara, _tree, agree: false));

        Assert.Null(_store.PublicTree(_tree));
        var state = _store.Tree(_mina, _tree)!.Publication;
        Assert.Equal((true, false), (state.Published, state.IsPublic));
        Assert.Equal(["Sara Rad:no-consent"], Blockers());

        Assert.True(_store.Consent(_sara, _tree, agree: true));
        Assert.NotNull(_store.PublicTree(_tree));
    }

    [Fact]
    public void A_living_person_added_or_changed_afterwards_stands_in_the_way_until_they_agree()
    {
        SaraIsHerRecord();
        _store.AskPublication(_mina, _tree);
        _store.Consent(_sara, _tree, agree: true);
        _store.Publish(_mina, _tree);

        // Somebody new who is living and is no account: the tree cannot be public with them in it.
        var rostam = _store.AddPerson(_mina, _tree, "Rostam", null, null, deceased: false)!;
        Assert.Null(_store.PublicTree(_tree));
        Assert.Equal(["Rostam:no-account"], Blockers());
        _store.RemovePerson(_mina, _tree, rostam);
        Assert.NotNull(_store.PublicTree(_tree));

        // Sara agreed to be named as her record was: changed, it is agreed to again.
        _store.ChangePerson(_mina, _tree, _saraRecord, "Sara Rad of Tehran", 1990, null, deceased: false);
        Assert.Null(_store.PublicTree(_tree));
        Assert.Equal(["Sara Rad of Tehran:no-consent"], Blockers());
        _store.Consent(_sara, _tree, agree: true);
        Assert.Equal("Sara Rad of Tehran", _store.PublicTree(_tree)!.Person(_saraRecord).Name);

        // Somebody who was said to have died and is living after all.
        _store.ChangePerson(_mina, _tree, _zal, "Zal", 1870, null, deceased: false);
        Assert.Null(_store.PublicTree(_tree));
        Assert.Equal(["Zal:no-account"], Blockers());
    }

    [Theory]
    [InlineData("unclaim")]
    [InlineData("end")]
    [InlineData("unshare")]
    [InlineData("close")]
    public void Who_is_no_record_of_the_tree_any_more_agreed_to_nothing(string how)
    {
        SaraIsHerRecord();
        _store.AskPublication(_mina, _tree);
        _store.Consent(_sara, _tree, agree: true);
        _store.Publish(_mina, _tree);

        Assert.True(how switch
        {
            "unclaim" => _store.Unclaim(_sara, _tree, _saraRecord),
            "end" => _store.End(_sara, _relationship),
            "unshare" => _store.SetTreeMember(_mina, _tree, _sara, TreeRole.None),
            _ => _store.Close(_sara, Password).Done,
        });

        // Her record names a living person who is no account now: the tree is hidden at once.
        Assert.Null(_store.PublicTree(_tree));
        Assert.Equal(["Sara Rad:no-account"], Blockers());
    }

    [Fact]
    public void The_owner_takes_it_back_and_what_was_agreed_is_forgotten()
    {
        SaraIsHerRecord();
        _store.AskPublication(_mina, _tree);
        _store.Consent(_sara, _tree, agree: true);
        _store.Publish(_mina, _tree);

        Assert.True(_store.Unpublish(_mina, _tree));
        Assert.False(_store.Unpublish(_mina, _tree));

        Assert.Null(_store.PublicTree(_tree));
        Assert.Same(PublicationState.None, _store.Tree(_sara, _tree)!.Publication);

        // Asked again, everybody is asked again.
        _store.AskPublication(_mina, _tree);
        Assert.Equal(["Sara Rad:no-consent"], Blockers());
        Assert.False(_store.Tree(_sara, _tree)!.Publication.Consented);
    }

    [Fact]
    public void A_tree_of_people_who_all_died_needs_nobodys_consent()
    {
        var old = _store.AddTree(_mina, "Long ago")!;
        var sam = _store.AddPerson(_mina, old, "Sam", 1800, 1870, deceased: true)!;
        _store.AddPerson(_mina, old, "Simin", null, null, deceased: true);

        Assert.True(_store.AskPublication(_mina, old));
        Assert.Empty(_store.Tree(_mina, old)!.Publication.Blockers);
        Assert.True(_store.Publish(_mina, old));
        Assert.Equal("Sam", _store.PublicTree(old)!.Person(sam).Name);
    }

    [Fact]
    public void A_tree_of_a_closed_account_is_public_no_more_and_gone_with_it()
    {
        var old = _store.AddTree(_mina, "Long ago")!;
        _store.AddPerson(_mina, old, "Sam", 1800, 1870, deceased: true);
        _store.AskPublication(_mina, old);
        _store.Publish(_mina, old);
        _store.Close(_mina, Password);

        Assert.Null(_store.PublicTree(old));

        _store.Reopen("Mina", Password);
        Assert.NotNull(_store.PublicTree(old));
    }

    [Fact]
    public void Asking_agreeing_and_making_public_are_in_the_history_of_the_tree()
    {
        SaraIsHerRecord();
        _store.AskPublication(_mina, _tree);
        _store.Consent(_sara, _tree, agree: true);
        _store.Publish(_mina, _tree);
        _store.Consent(_sara, _tree, agree: false);
        _store.Unpublish(_mina, _tree);

        Assert.Equal(
            [
                "Mina unpublished The Rad family",
                "Sara consent-withdrawn Sara Rad",
                "Mina published The Rad family",
                "Sara consent-given Sara Rad",
                "Mina publication-asked The Rad family",
            ],
            _store.Tree(_mina, _tree)!.History.Take(5).Select(change => $"{change.By} {change.What} {change.Subject}"));
    }
}
