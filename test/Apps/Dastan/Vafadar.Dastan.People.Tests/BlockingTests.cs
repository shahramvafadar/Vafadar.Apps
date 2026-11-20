using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// Blocking an account (step 19.6, ADR 0018, PRIV-009): what the two showed together ends at once; the blocked
/// account cannot ask, tag, invite or find the one that blocked it, and nothing tells it why.
/// </summary>
public sealed class BlockingTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;

    public BlockingTests()
    {
        _store = PeopleStore.Open(TestDatabase.For(Path.Combine(_folder, "people.db")), _clock, site: "https://dastan.test");
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

    private string Shown(string from, string to, string toName, string kind)
    {
        Assert.Equal("asked", _store.Ask(from, toName, kind));
        var id = _store.Relationships(to).Single(relationship => relationship.State == RelationshipState.Requested && !relationship.AskedByMe).Id;
        Assert.True(_store.Answer(to, id, accept: true));
        return id;
    }

    [Fact]
    public void Blocking_ends_everything_the_two_showed_together_and_nothing_between_others()
    {
        Shown(_mina, _sara, "Sara", "friend");
        Shown(_sara, _mina, "Mina", "colleague");
        Assert.Equal("asked", _store.Ask(_sara, "Mina", "classmate"));
        Shown(_mina, _dara, "Dara", "friend");
        var circle = _store.AddCircle(_mina, "Close")!.Value;
        _store.SetCircle(_mina, circle, [_sara, _dara]);
        var tree = _store.AddTree(_mina, "The Rad family")!;
        _store.SetTreeMember(_mina, tree, _sara, TreeRole.Editor);
        Assert.True(_store.AddPhoto(_mina, "at-the-sea", new PhotoFile("at-the-sea.jpg", "image/jpeg", 800, 600, 12345), "At the sea", null, ScopeKind.Private));
        Assert.True(_store.TagPhoto(_mina, "at-the-sea", _sara));

        Assert.True(_store.Block(_mina, _sara));

        Assert.Equal(["Dara"], _store.Relationships(_mina).Select(relationship => relationship.Other.Name));
        Assert.Empty(_store.Relationships(_sara));
        Assert.Equal(["Dara"], _store.Circles(_mina).Single().Members.Select(member => member.Name));
        Assert.Null(_store.Tree(_sara, tree));
        Assert.Empty(_store.Photo(_mina, "at-the-sea")!.Tags);
        Assert.Equal(["Sara"], _store.Blocked(_mina).Select(person => person.Name));
        Assert.Empty(_store.Blocked(_sara));
    }

    [Fact]
    public void Who_was_blocked_cannot_ask_or_find_the_account_and_reads_what_a_stranger_reads_of_nobody()
    {
        Assert.True(_store.Block(_mina, _sara));

        // To Sara, Mina is an account that is not there – the answer a wrong name gets.
        Assert.Equal(_store.Ask(_sara, "Nobody", "friend"), _store.Ask(_sara, "Mina", "friend"));
        Assert.Null(_store.Profile(_sara, _mina));
        Assert.Empty(_store.Relationships(_mina));

        // Everybody else finds Mina as before, and Mina still finds Sara – and is told that she blocks her.
        Assert.NotNull(_store.Profile(_dara, _mina));
        Assert.NotNull(_store.Profile(null, _mina));
        Assert.True(_store.Profile(_mina, _sara)!.Blocked);
        Assert.False(_store.Profile(_dara, _sara)!.Blocked);

        // Who blocks somebody does not ask them either, until the block is taken back.
        Assert.Equal("blocked", _store.Ask(_mina, "Sara", "friend"));
    }

    [Fact]
    public void An_invitation_by_mail_to_somebody_who_blocked_the_account_is_not_sent_and_reads_as_sent()
    {
        Assert.True(_store.Block(_mina, _sara));
        var before = _store.Invitations(_sara).Count;

        Assert.Equal("sent", _store.InviteByMail(_sara, TestAccounts.MailOf("Mina"), "en"));

        Assert.Equal(before, _store.Invitations(_sara).Count);
        Assert.Equal("sent", _store.InviteByMail(_sara, "somebody.else@example.org", "en"));
        Assert.Equal(before + 1, _store.Invitations(_sara).Count);
    }

    [Fact]
    public void A_block_is_one_sided_is_made_once_and_taken_back_without_restoring_anything()
    {
        Shown(_mina, _sara, "Sara", "friend");

        Assert.True(_store.Block(_mina, _sara));
        Assert.False(_store.Block(_mina, _sara));
        Assert.False(_store.Block(_mina, _mina));
        Assert.False(_store.Block(_mina, "no-such-account"));
        Assert.False(_store.Unblock(_sara, _mina));

        Assert.True(_store.Unblock(_mina, _sara));
        Assert.False(_store.Unblock(_mina, _sara));
        Assert.Empty(_store.Blocked(_mina));
        Assert.Empty(_store.Relationships(_mina));

        // From here on the two can ask each other like anybody.
        Assert.Equal("asked", _store.Ask(_sara, "Mina", "friend"));
    }

    [Fact]
    public void The_copy_of_my_data_names_who_i_block_and_a_closed_account_takes_its_blocks_along()
    {
        Assert.True(_store.Block(_mina, _sara));
        Assert.True(_store.Block(_dara, _mina));

        Assert.Equal("""["Sara"]""", _store.Export(_mina, Password)!["blocked"]!.ToJsonString());

        // Dara is not told to Mina by anything; and when Sara is gone, she is no line of the list any more.
        Assert.True(_store.Close(_sara, Password).Done);
        Assert.Empty(_store.Blocked(_mina));
    }
}
