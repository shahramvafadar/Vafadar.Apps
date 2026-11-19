using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// What a review of the store of the visitors made firmer (step 18.7): an invitation that is needed makes one
/// account, a wrong password is counted wherever it is tried, a name goes with its account out of the histories
/// of others, an account is asked before it is named in a public tree whatever its record says, and an objection
/// takes a place only while it holds something back.
/// </summary>
public sealed class FirmerAccountsTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Wrong = "a wrong pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private int _number;

    public FirmerAccountsTests()
    {
        _store = PeopleStore.Open(TestDatabase.For(Path.Combine(_folder, "people.db")), _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private string Photo(string owner, ScopeKind shown = ScopeKind.Public)
    {
        var id = $"photo{++_number:D4}";
        Assert.True(_store.AddPhoto(owner, id, new PhotoFile($"{id}-file.jpg", "image/jpeg", 800, 600, 12345), "At the sea", null, shown));
        return id;
    }

    [Fact]
    public void The_name_of_an_account_that_is_deleted_leaves_the_history_of_a_tree_it_was_shown()
    {
        var tree = _store.AddTree(_mina, "The Rad family")!;
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "family"));
        Assert.True(_store.Answer(_sara, _store.Relationships(_sara).Single().Id, accept: true));
        Assert.True(_store.SetTreeMember(_mina, tree, _sara, TreeRole.Viewer));
        Assert.Contains(_store.Tree(_mina, tree)!.History, change => change is { What: "member-viewer", Subject: "Sara" });

        Assert.True(_store.Close(_sara, Password).Done);
        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromDays(1);
        Assert.Equal(1, _store.Purge());

        // The line stays – the tree was shown to somebody – and names nobody any more.
        var history = _store.Tree(_mina, tree)!.History;
        Assert.Contains(history, change => change is { What: "member-viewer", Subject: "" });
        Assert.DoesNotContain(history, change => change.Subject.Contains("Sara", StringComparison.Ordinal));
        Assert.Contains(history, change => change is { What: "tree-made", Subject: "The Rad family" });
    }

    [Fact]
    public void An_account_is_asked_before_a_tree_that_names_it_is_public_whatever_its_record_says()
    {
        var tree = _store.AddTree(_mina, "The Rad family")!;
        var record = _store.AddPerson(_mina, tree, "Sara Rad", 1990, null, deceased: false)!;
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "family"));
        Assert.True(_store.Answer(_sara, _store.Relationships(_sara).Single().Id, accept: true));
        Assert.True(_store.SetTreeMember(_mina, tree, _sara, TreeRole.Viewer));
        Assert.Equal("claimed", _store.Claim(_sara, tree, record));
        Assert.True(_store.ReviewClaim(_mina, tree, _store.Tree(_mina, tree)!.Claims.Single().Number, confirm: true));

        Assert.True(_store.AskPublication(_mina, tree));
        Assert.True(_store.Consent(_sara, tree, agree: true));
        Assert.True(_store.Publish(_mina, tree));
        Assert.NotNull(_store.PublicTree(tree));

        // Sara takes her agreement back: the tree is not public. Ticking "has died" on her record does not
        // answer for her – her account is open and is asked itself.
        Assert.True(_store.Consent(_sara, tree, agree: false));
        Assert.Null(_store.PublicTree(tree));
        Assert.True(_store.ChangePerson(_mina, tree, record, "Sara Rad", 1990, null, deceased: true));
        Assert.Null(_store.PublicTree(tree));
        Assert.Single(_store.Tree(_mina, tree)!.Publication.Blockers);
        Assert.True(_store.Tree(_sara, tree)!.Publication.AskedOfMe);

        Assert.True(_store.Consent(_sara, tree, agree: true));
        Assert.NotNull(_store.PublicTree(tree));
    }

    [Fact]
    public void An_objection_takes_a_place_only_while_the_photograph_is_shown_to_everybody()
    {
        var photos = Enumerable.Range(0, PeopleStore.MostObjections).Select(_ => Photo(_mina)).ToList();
        Assert.All(photos, photo => Assert.True(_store.ObjectToPhoto(_sara, photo)));
        Assert.False(_store.ObjectToPhoto(_sara, Photo(_mina)));

        // The owner shows one of them to nobody: the objection holds nothing back and cannot be found by
        // who made it, so it takes no place.
        Assert.True(_store.SharePhoto(_mina, photos[0], ScopeKind.Private));
        Assert.True(_store.ObjectToPhoto(_sara, Photo(_mina)));
    }

    [Fact]
    public void Somebody_who_said_a_photograph_does_not_show_them_can_object_when_it_is_shown_to_everybody()
    {
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "friend"));
        Assert.True(_store.Answer(_sara, _store.Relationships(_sara).Single().Id, accept: true));
        var photo = Photo(_mina);
        Assert.True(_store.TagPhoto(_mina, photo, _sara));
        Assert.True(_store.AnswerTag(_sara, photo, approve: false));
        Assert.NotNull(_store.Photo(null, photo));

        Assert.True(_store.ObjectToPhoto(_sara, photo));

        Assert.Null(_store.Photo(null, photo));
        Assert.Contains(_store.AskedForEverybody(_sara), asked => asked.Id == photo);
        Assert.True(_store.WithdrawObjection(_sara, photo));
        Assert.NotNull(_store.Photo(null, photo));
    }

    [Fact]
    public void With_whom_a_tree_is_shared_and_who_claimed_a_record_is_read_by_its_owner()
    {
        var dara = _store.Register("Dara", Password).Session!.Id;
        var tree = _store.AddTree(_mina, "The Rad family")!;
        var record = _store.AddPerson(_mina, tree, "Sara Rad", 1990, null, deceased: false)!;
        foreach (var (account, name) in new[] { (_sara, "Sara"), (dara, "Dara") })
        {
            Assert.Equal("asked", _store.Ask(_mina, name, "family"));
            Assert.True(_store.Answer(account, _store.Relationships(account).Single().Id, accept: true));
        }

        Assert.True(_store.SetTreeMember(_mina, tree, _sara, TreeRole.Viewer));
        Assert.True(_store.SetTreeMember(_mina, tree, dara, TreeRole.Editor));
        Assert.Equal("claimed", _store.Claim(_sara, tree, record));
        Assert.True(_store.ReviewClaim(_mina, tree, _store.Tree(_mina, tree)!.Claims.Single().Number, confirm: false));

        string[] Lines(string viewer) => [.. _store.Tree(viewer, tree)!.History.Select(change => $"{change.By} {change.What} {change.Subject}")];

        // The owner reads everything.
        Assert.Equal(
            ["Mina claim-refused Sara Rad", "Sara claim-made Sara Rad", "Mina member-editor Dara", "Mina member-viewer Sara", "Mina person-added Sara Rad", "Mina tree-made The Rad family"],
            Lines(_mina));

        // Dara, who may change the tree, reads what was done to it – not with whom it is shared, not who claimed.
        Assert.Equal(["Mina person-added Sara Rad", "Mina tree-made The Rad family"], Lines(dara));

        // Sara reads the same, and the line she wrote herself.
        Assert.Equal(["Sara claim-made Sara Rad", "Mina person-added Sara Rad", "Mina tree-made The Rad family"], Lines(_sara));
    }

    [Fact]
    public void A_copy_of_ones_data_names_an_invitation_that_can_still_be_used_without_its_code()
    {
        var open = _store.Invite(_mina)!;
        var used = _store.Invite(_mina)!;
        var revoked = _store.Invite(_mina)!;
        Assert.True(_store.Register("Dara", Password, used).Done);
        Assert.True(_store.Revoke(_mina, revoked));

        var copy = _store.Export(_mina, Password)!;
        var invitations = copy["invitations"]!.AsArray().ToDictionary(invitation => (string)invitation!["state"]!, invitation => (string?)invitation!["code"]);

        Assert.Equal(3, invitations.Count);
        Assert.Null(invitations["open"]);
        Assert.Equal(used, invitations["used"]);
        Assert.Equal(revoked, invitations["revoked"]);
        Assert.DoesNotContain(open, copy.ToJsonString(), StringComparison.Ordinal);

        // The code is where it was: on the page of the account.
        Assert.Contains(_store.Invitations(_mina), invitation => invitation.Code == open);
    }
}
