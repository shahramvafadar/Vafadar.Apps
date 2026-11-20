using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// Family trees (step 16.4, D-054, PRIV-002, PRIV-003): a tree has an owner, who names the accounts that may
/// change it and those that may only see it; every change is kept with who made it; nobody else sees it.
/// </summary>
public sealed class FamilyTreesTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;
    private readonly string _tree;

    public FamilyTreesTests()
    {
        _store = PeopleStore.Open(TestDatabase.For(Path.Combine(_folder, "people.db")), _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
        _dara = _store.Register("Dara", Password).Session!.Id;
        _tree = _store.AddTree(_mina, " The Rad family ")!;
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

    private string Person(string name, int? born = null, int? died = null, string? by = null) => _store.AddPerson(by ?? _mina, _tree, name, born, died, deceased: false)!;

    [Fact]
    public void A_tree_belongs_to_who_made_it_and_exists_for_nobody_else()
    {
        var info = Assert.Single(_store.Trees(_mina));

        Assert.Equal(("The Rad family", "Mina", TreeRole.Owner, 0), (info.Name, info.Owner.Name, info.Role, info.People));
        Assert.Empty(_store.Trees(_sara));
        Assert.NotNull(_store.Tree(_mina, _tree));
        Assert.Null(_store.Tree(_sara, _tree));
        Assert.Null(_store.Tree(null, _tree));
        Assert.Null(_store.Tree(_mina, "no-such-tree"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("two\nlines")]
    public void A_tree_needs_a_plain_name(string name)
    {
        Assert.Null(_store.AddTree(_mina, name));
        Assert.False(_store.RenameTree(_mina, _tree, name));
    }

    [Fact]
    public void An_account_owns_only_so_many_trees()
    {
        for (var number = 1; number < PeopleStore.MostTrees; number++)
        {
            Assert.NotNull(_store.AddTree(_mina, $"Tree {number}"));
        }

        Assert.Null(_store.AddTree(_mina, "One more"));
        Assert.NotNull(_store.AddTree(_sara, "Sara's own"));
    }

    [Fact]
    public void A_record_says_a_name_and_the_years_that_are_known_and_needs_no_account()
    {
        var zal = Person("Zal");
        var rostam = Person(" Rostam ", born: 1300, died: 1380);

        var tree = _store.Tree(_mina, _tree)!;

        Assert.Equal(["Rostam", "Zal"], tree.People.Select(person => person.Name));
        Assert.Equal((1300, 1380, true), (tree.Person(rostam).Born.Year(), tree.Person(rostam).Died.Year(), tree.Person(rostam).Deceased));
        Assert.Equal(((int?)null, (int?)null, false), (tree.Person(zal).Born.Year(), tree.Person(zal).Died.Year(), tree.Person(zal).Deceased));
        Assert.Equal(2, _store.Trees(_mina)[0].People);
    }

    [Theory]
    [InlineData("", null, null)]
    [InlineData("Zal", 1300, 1299)]
    public void A_record_that_says_something_impossible_is_not_kept(string name, int? born, int? died)
    {
        var rostam = Person("Rostam");

        Assert.Null(_store.AddPerson(_mina, _tree, name, born, died, deceased: false));
        Assert.False(_store.ChangePerson(_mina, _tree, rostam, name, born, died, deceased: false));
        Assert.Equal("Rostam", Assert.Single(_store.Tree(_mina, _tree)!.People).Name);
    }

    [Fact]
    public void Parents_children_and_partners_are_links_between_records()
    {
        var (zal, rudabeh, rostam) = (Person("Zal"), Person("Rudabeh"), Person("Rostam"));

        Assert.Equal("done", _store.Link(_mina, _tree, "parent", zal, rostam));
        Assert.Equal("done", _store.Link(_mina, _tree, "parent", rudabeh, rostam));
        Assert.Equal("done", _store.Link(_mina, _tree, "partner", zal, rudabeh));

        var tree = _store.Tree(_mina, _tree)!;
        Assert.Equal([zal, rudabeh], tree.Person(rostam).Parents);
        Assert.Equal([rostam], tree.Person(zal).Children);
        Assert.Equal([rudabeh], tree.Person(zal).Partners);
        Assert.Equal([zal], tree.Person(rudabeh).Partners);
        Assert.Equal(3, tree.Links.Length);
    }

    [Fact]
    public void A_link_that_cannot_be_says_why()
    {
        var (zal, rostam, sohrab) = (Person("Zal"), Person("Rostam"), Person("Sohrab"));
        var other = _store.AddPerson(_mina, _store.AddTree(_mina, "Another tree")!, "Stranger", null, null, deceased: false)!;
        _store.Link(_mina, _tree, "parent", zal, rostam);
        _store.Link(_mina, _tree, "parent", rostam, sohrab);
        _store.Link(_mina, _tree, "partner", zal, rostam);

        Assert.Equal("same", _store.Link(_mina, _tree, "parent", zal, zal));
        Assert.Equal("exists", _store.Link(_mina, _tree, "parent", zal, rostam));
        Assert.Equal("exists", _store.Link(_mina, _tree, "parent", rostam, zal));
        Assert.Equal("exists", _store.Link(_mina, _tree, "partner", rostam, zal));

        // Nobody is their own ancestor.
        Assert.Equal("loop", _store.Link(_mina, _tree, "parent", sohrab, zal));
        Assert.Equal("kind", _store.Link(_mina, _tree, "cousin", zal, sohrab));
        Assert.Equal("unknown", _store.Link(_mina, _tree, "parent", zal, "nobody"));
        Assert.Equal("unknown", _store.Link(_mina, _tree, "parent", zal, other));
        Assert.Equal("refused", _store.Link(_sara, _tree, "parent", zal, sohrab));
        Assert.Equal(3, _store.Tree(_mina, _tree)!.Links.Length);
    }

    [Fact]
    public void Removing_a_record_removes_its_links_and_a_link_is_removed_alone()
    {
        var (zal, rostam, sohrab) = (Person("Zal"), Person("Rostam"), Person("Sohrab"));
        _store.Link(_mina, _tree, "parent", zal, rostam);
        _store.Link(_mina, _tree, "parent", rostam, sohrab);

        Assert.True(_store.Unlink(_mina, _tree, _store.Tree(_mina, _tree)!.Links[0].Number));
        Assert.False(_store.Unlink(_mina, _tree, 99999));
        Assert.Empty(_store.Tree(_mina, _tree)!.Person(rostam).Parents);

        Assert.True(_store.RemovePerson(_mina, _tree, sohrab));
        Assert.False(_store.RemovePerson(_mina, _tree, sohrab));
        var tree = _store.Tree(_mina, _tree)!;
        Assert.Equal(["Rostam", "Zal"], tree.People.Select(person => person.Name));
        Assert.Empty(tree.Links);
    }

    [Fact]
    public void The_owner_names_who_may_see_and_who_may_change_among_the_people_they_know()
    {
        Friends(_mina, _sara, "Sara");
        Friends(_mina, _dara, "Dara");
        var zal = _store.Register("Zal", Password).Session!.Id;
        var rostam = Person("Rostam");

        // Somebody the owner has no relationship with is not named.
        Assert.False(_store.SetTreeMember(_mina, _tree, zal, TreeRole.Viewer));
        Assert.False(_store.SetTreeMember(_mina, _tree, "nobody", TreeRole.Viewer));
        Assert.False(_store.SetTreeMember(_mina, _tree, _sara, TreeRole.Owner));
        Assert.True(_store.SetTreeMember(_mina, _tree, _sara, TreeRole.Viewer));
        Assert.True(_store.SetTreeMember(_mina, _tree, _dara, TreeRole.Editor));

        // Who may see, sees and changes nothing.
        var seen = _store.Tree(_sara, _tree)!;
        Assert.Equal((TreeRole.Viewer, "Mina"), (seen.Tree.Role, seen.Tree.Owner.Name));
        Assert.Equal("Rostam", Assert.Single(seen.People).Name);
        Assert.Empty(seen.Members);
        Assert.Null(_store.AddPerson(_sara, _tree, "Sohrab", null, null, deceased: false));
        Assert.False(_store.ChangePerson(_sara, _tree, rostam, "Somebody else", null, null, deceased: false));
        Assert.False(_store.RemovePerson(_sara, _tree, rostam));

        // Who may change, changes the records and the links and nothing about the tree itself.
        var sohrab = _store.AddPerson(_dara, _tree, "Sohrab", null, null, deceased: false)!;
        Assert.Equal("done", _store.Link(_dara, _tree, "parent", rostam, sohrab));
        Assert.True(_store.ChangePerson(_dara, _tree, rostam, "Rostam of Zabol", 1300, null, deceased: true));
        Assert.False(_store.RenameTree(_dara, _tree, "Dara's tree"));
        Assert.False(_store.RemoveTree(_dara, _tree));
        Assert.False(_store.SetTreeMember(_dara, _tree, _sara, TreeRole.Editor));
        Assert.Equal(TreeRole.Editor, Assert.Single(_store.Trees(_dara)).Role);

        // The owner sees who was named, and takes it back.
        Assert.Equal(["Dara:Editor", "Sara:Viewer"], _store.Tree(_mina, _tree)!.Members.Select(member => $"{member.Account.Name}:{member.Role}"));
        Assert.True(_store.SetTreeMember(_mina, _tree, _dara, TreeRole.Viewer));
        Assert.Null(_store.AddPerson(_dara, _tree, "One more", null, null, deceased: false));
        Assert.True(_store.SetTreeMember(_mina, _tree, _sara, TreeRole.None));
        Assert.False(_store.SetTreeMember(_mina, _tree, _sara, TreeRole.None));
        Assert.Null(_store.Tree(_sara, _tree));
    }

    [Fact]
    public void Every_change_is_kept_with_who_made_it_the_newest_first()
    {
        Friends(_mina, _dara, "Dara");
        _store.SetTreeMember(_mina, _tree, _dara, TreeRole.Editor);
        var rostam = Person("Rostam");
        _clock.Now += TimeSpan.FromHours(1);
        var sohrab = Person("Sohrab", by: _dara);
        _store.Link(_dara, _tree, "parent", rostam, sohrab);
        _store.ChangePerson(_mina, _tree, sohrab, "Sohrab the young", null, null, deceased: true);
        _store.Unlink(_mina, _tree, _store.Tree(_mina, _tree)!.Links[0].Number);
        _store.RemovePerson(_dara, _tree, sohrab);
        _store.RenameTree(_mina, _tree, "The family of Zal");

        // Read by the owner, who reads every line; with whom the tree is shared is not said to others (D-070).
        var history = _store.Tree(_mina, _tree)!.History;

        Assert.Equal(
            [
                "Mina tree-renamed The family of Zal",
                "Dara person-removed Sohrab the young",
                "Mina parent-removed Rostam – Sohrab the young",
                "Mina person-changed Sohrab the young",
                "Dara parent-added Rostam – Sohrab",
                "Dara person-added Sohrab",
                "Mina person-added Rostam",
                "Mina member-editor Dara",
                "Mina tree-made The Rad family",
            ],
            history.Select(change => $"{change.By} {change.What} {change.Subject}"));
        Assert.Equal(_clock.Now.UtcDateTime, history[0].At);
        Assert.Equal(_clock.Now.UtcDateTime - TimeSpan.FromHours(1), history[^1].At);
    }

    [Fact]
    public void What_was_changed_stays_when_who_changed_it_is_deleted()
    {
        Friends(_mina, _dara, "Dara");
        _store.SetTreeMember(_mina, _tree, _dara, TreeRole.Editor);
        Person("Sohrab", by: _dara);
        _store.Close(_dara, Password);
        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromMinutes(1);
        _store.Purge();

        var tree = _store.Tree(_mina, _tree)!;

        Assert.Equal("Sohrab", Assert.Single(tree.People).Name);
        Assert.Contains(tree.History, change => change is { By: null, What: "person-added", Subject: "Sohrab" });
        Assert.Empty(tree.Members);
    }

    [Fact]
    public void Two_whose_relationship_ended_name_each_other_in_no_tree()
    {
        var relationship = Friends(_mina, _sara, "Sara");
        _store.SetTreeMember(_mina, _tree, _sara, TreeRole.Editor);
        Assert.NotNull(_store.Tree(_sara, _tree));

        _store.End(_sara, relationship);

        Assert.Null(_store.Tree(_sara, _tree));
        Assert.Empty(_store.Tree(_mina, _tree)!.Members);
    }

    [Fact]
    public void A_tree_is_removed_by_its_owner_and_goes_with_its_owner()
    {
        Friends(_mina, _sara, "Sara");
        _store.SetTreeMember(_mina, _tree, _sara, TreeRole.Viewer);
        Person("Rostam");

        Assert.False(_store.RemoveTree(_sara, _tree));
        Assert.True(_store.RemoveTree(_mina, _tree));
        Assert.Empty(_store.Trees(_mina));
        Assert.Empty(_store.Trees(_sara));

        // A closed account's tree is seen by nobody at once, and is gone when the account is deleted.
        var another = _store.AddTree(_mina, "Another")!;
        _store.SetTreeMember(_mina, another, _sara, TreeRole.Viewer);
        _store.Close(_mina, Password);
        Assert.Empty(_store.Trees(_sara));
        Assert.Null(_store.Tree(_sara, another));
        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromMinutes(1);
        _store.Purge();
        Assert.Empty(_store.Trees(_sara));
    }

    [Fact]
    public void A_tree_holds_only_so_many_records()
    {
        for (var number = 0; number < PeopleStore.MostPeople; number++)
        {
            Assert.NotNull(_store.AddPerson(_mina, _tree, $"Person {number}", null, null, deceased: false));
        }

        Assert.Null(_store.AddPerson(_mina, _tree, "One more", null, null, deceased: false));
    }
}
