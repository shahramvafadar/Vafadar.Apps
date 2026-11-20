using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// The claim of an account on the record of a person (step 16.4, D-052, PRIV-004): confirmed or refused by the
/// one who made the record, never by the claimant; confirming merges nothing and widens no audience.
/// </summary>
public sealed class IdentityClaimsTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;
    private readonly string _tree;
    private readonly string _record;
    private readonly string _relationship;

    public IdentityClaimsTests()
    {
        _store = PeopleStore.Open(TestDatabase.For(Path.Combine(_folder, "people.db")), _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
        _dara = _store.Register("Dara", Password).Session!.Id;
        _tree = _store.AddTree(_mina, "The Rad family")!;
        _record = _store.AddPerson(_mina, _tree, "Sara Rad", 1990, null, deceased: false)!;
        _relationship = Friends(_mina, _sara, "Sara");
        Friends(_mina, _dara, "Dara");
        _store.SetTreeMember(_mina, _tree, _sara, TreeRole.Viewer);
        _store.SetTreeMember(_mina, _tree, _dara, TreeRole.Editor);
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

    private long Waiting(string viewer) => _store.Tree(viewer, _tree)!.Claims.Single().Number;

    [Fact]
    public void A_claim_waits_for_who_made_the_record_and_is_seen_by_the_two_only()
    {
        Assert.True(_store.Tree(_sara, _tree)!.CanClaim);

        Assert.Equal("claimed", _store.Claim(_sara, _tree, _record));

        // Nothing is confirmed yet: the record names nobody.
        Assert.Null(_store.Tree(_mina, _tree)!.Person(_record).Account);
        var mine = Assert.Single(_store.Tree(_sara, _tree)!.Claims);
        Assert.Equal((_record, "Sara", true, false), (mine.Person, mine.By.Name, mine.Mine, mine.CanReview));
        var asked = Assert.Single(_store.Tree(_mina, _tree)!.Claims);
        Assert.Equal((true, false), (asked.CanReview, asked.Mine));
        Assert.Empty(_store.Tree(_dara, _tree)!.Claims);
        Assert.False(_store.Tree(_sara, _tree)!.CanClaim);
    }

    [Fact]
    public void Confirmed_by_who_made_the_record_it_names_the_account_and_nothing_else_changes()
    {
        _store.Claim(_sara, _tree, _record);
        var before = _store.Tree(_sara, _tree)!;

        Assert.True(_store.ReviewClaim(_mina, _tree, Waiting(_mina), confirm: true));

        var after = _store.Tree(_dara, _tree)!;
        Assert.Equal("Sara", after.Person(_record).Account?.Name);
        Assert.Equal(("Sara Rad", 1990), (after.Person(_record).Name, after.Person(_record).Born.Year()));
        Assert.Empty(after.Claims);

        // No audience is wider: who may see, may still only see; who may not see, still sees nothing.
        Assert.Equal(TreeRole.Viewer, _store.Tree(_sara, _tree)!.Tree.Role);
        Assert.Equal(before.People.Length, _store.Tree(_sara, _tree)!.People.Length);
        Assert.Null(_store.AddPerson(_sara, _tree, "One more", null, null, deceased: false));
        var zal = _store.Register("Zal", Password).Session!.Id;
        Assert.Null(_store.Tree(zal, _tree));
        Assert.Empty(_store.Profile(null, _sara)!.Relationships);
    }

    [Fact]
    public void Nobody_confirms_their_own_claim_or_one_that_is_not_theirs_to_answer()
    {
        _store.Claim(_sara, _tree, _record);
        var claim = Waiting(_sara);

        Assert.False(_store.ReviewClaim(_sara, _tree, claim, confirm: true));
        Assert.False(_store.ReviewClaim(_dara, _tree, claim, confirm: true));
        Assert.False(_store.ReviewClaim(_mina, _tree, 99999, confirm: true));
        Assert.Null(_store.Tree(_mina, _tree)!.Person(_record).Account);

        Assert.True(_store.ReviewClaim(_mina, _tree, claim, confirm: false));
        Assert.False(_store.ReviewClaim(_mina, _tree, claim, confirm: true));
        Assert.Null(_store.Tree(_mina, _tree)!.Person(_record).Account);
        Assert.Empty(_store.Tree(_sara, _tree)!.Claims);
        Assert.True(_store.Tree(_sara, _tree)!.CanClaim);
    }

    [Fact]
    public void A_record_made_by_an_editor_is_confirmed_by_that_editor_not_by_the_owner()
    {
        var made = _store.AddPerson(_dara, _tree, "Sara the younger", null, null, deceased: false)!;
        _store.Claim(_sara, _tree, made);
        var claim = Waiting(_sara);

        Assert.Empty(_store.Tree(_mina, _tree)!.Claims);
        Assert.True(Assert.Single(_store.Tree(_dara, _tree)!.Claims).CanReview);
        Assert.False(_store.ReviewClaim(_mina, _tree, claim, confirm: true));
        Assert.True(_store.ReviewClaim(_dara, _tree, claim, confirm: true));
        Assert.Equal("Sara", _store.Tree(_mina, _tree)!.Person(made).Account?.Name);
    }

    [Fact]
    public void When_who_made_the_record_is_gone_the_owner_answers()
    {
        var made = _store.AddPerson(_dara, _tree, "Sara the younger", null, null, deceased: false)!;
        _store.SetTreeMember(_mina, _tree, _dara, TreeRole.None);

        Assert.Equal("claimed", _store.Claim(_sara, _tree, made));

        Assert.True(Assert.Single(_store.Tree(_mina, _tree)!.Claims).CanReview);
        Assert.True(_store.ReviewClaim(_mina, _tree, Waiting(_mina), confirm: true));
    }

    [Fact]
    public void What_cannot_be_claimed_says_why()
    {
        var zal = _store.Register("Zal", Password).Session!.Id;
        var other = _store.AddPerson(_mina, _tree, "Somebody else", null, null, deceased: false)!;
        var minas = _store.AddPerson(_mina, _tree, "Mina Rad", null, null, deceased: false)!;

        Assert.Equal("refused", _store.Claim(zal, _tree, _record));
        Assert.Equal("unknown", _store.Claim(_sara, _tree, "no-such-record"));

        // In her own tree Mina made the record herself: nobody else could confirm that it is her, and it is
        // not offered to her. To somebody else who sees the tree it is.
        Assert.Equal("nobody", _store.Claim(_mina, _tree, minas));
        Assert.False(_store.Tree(_mina, _tree)!.Person(minas).Claimable);
        Assert.True(_store.Tree(_sara, _tree)!.Person(minas).Claimable);

        Assert.Equal("claimed", _store.Claim(_sara, _tree, _record));
        Assert.Equal("own", _store.Claim(_sara, _tree, other));
        _store.ReviewClaim(_mina, _tree, Waiting(_mina), confirm: true);
        Assert.Equal("own", _store.Claim(_sara, _tree, other));
        Assert.Equal("taken", _store.Claim(_dara, _tree, _record));
    }

    [Fact]
    public void An_account_takes_back_its_claim_and_that_a_record_is_it()
    {
        _store.Claim(_sara, _tree, _record);

        Assert.False(_store.Unclaim(_dara, _tree, _record));
        Assert.True(_store.Unclaim(_sara, _tree, _record));
        Assert.Empty(_store.Tree(_mina, _tree)!.Claims);
        Assert.False(_store.Unclaim(_sara, _tree, _record));

        _store.Claim(_sara, _tree, _record);
        _store.ReviewClaim(_mina, _tree, Waiting(_mina), confirm: true);

        // Nobody else takes it back for her – not even the owner.
        Assert.False(_store.Unclaim(_mina, _tree, _record));
        Assert.True(_store.Unclaim(_sara, _tree, _record));
        Assert.Null(_store.Tree(_mina, _tree)!.Person(_record).Account);
        Assert.True(_store.Tree(_sara, _tree)!.CanClaim);
    }

    [Fact]
    public void Who_may_see_the_tree_no_more_is_no_record_of_it()
    {
        _store.Claim(_sara, _tree, _record);
        _store.ReviewClaim(_mina, _tree, Waiting(_mina), confirm: true);
        var made = _store.AddPerson(_mina, _tree, "Dara Rad", null, null, deceased: false)!;
        _store.Claim(_dara, _tree, made);

        _store.End(_sara, _relationship);
        _store.SetTreeMember(_mina, _tree, _dara, TreeRole.None);

        var tree = _store.Tree(_mina, _tree)!;
        Assert.Null(tree.Person(_record).Account);
        Assert.Empty(tree.Claims);
    }

    [Fact]
    public void A_deleted_account_is_no_record_any_more_and_the_record_stays()
    {
        _store.Claim(_sara, _tree, _record);
        _store.ReviewClaim(_mina, _tree, Waiting(_mina), confirm: true);
        _store.Close(_sara, Password);

        // Closed, the record names nobody at once.
        Assert.Null(_store.Tree(_mina, _tree)!.Person(_record).Account);

        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromMinutes(1);
        _store.Purge();
        var record = _store.Tree(_mina, _tree)!.Person(_record);
        Assert.Equal(("Sara Rad", null), (record.Name, record.Account));
    }

    [Fact]
    public void A_claim_and_its_answer_are_in_the_history_of_the_tree()
    {
        _store.Claim(_sara, _tree, _record);
        _store.ReviewClaim(_mina, _tree, Waiting(_mina), confirm: true);
        _store.Unclaim(_sara, _tree, _record);

        Assert.Equal(
            ["Sara claim-withdrawn Sara Rad", "Mina claim-confirmed Sara Rad", "Sara claim-made Sara Rad"],
            _store.Tree(_mina, _tree)!.History.Take(3).Select(change => $"{change.By} {change.What} {change.Subject}"));
    }
}
