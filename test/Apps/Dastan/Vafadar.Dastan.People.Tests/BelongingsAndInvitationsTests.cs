using System.Collections.Immutable;
using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// What belongs to an account (step 16.2): the reading position and the list that follow the visitor, bringing
/// in what a device holds, and the invitations a visitor passes on (D-049, PRIV-006).
/// </summary>
public sealed class BelongingsAndInvitationsTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Story = "story-rostam-sohrab";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;

    public BelongingsAndInvitationsTests()
    {
        _store = PeopleStore.Open(Path.Combine(_folder, "people.db"), _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static KeptPage Page(string id, string kind = "entity") => new(Story, kind, id);

    [Fact]
    public void A_new_account_has_read_nothing_and_kept_nothing()
    {
        var has = _store.Belongings(_mina);

        Assert.Empty(has.Positions);
        Assert.Empty(has.Kept);
    }

    [Fact]
    public void A_reading_position_is_kept_per_collection_moved_forward_and_back_and_forgotten()
    {
        _store.SetPosition(_mina, Story, 3);
        _store.SetPosition(_mina, "subject-club", 1);
        Assert.Equal(3, _store.Belongings(_mina).Positions[Story]);

        _store.SetPosition(_mina, Story, 2);
        Assert.Equal((2, 1), (_store.Belongings(_mina).Positions[Story], _store.Belongings(_mina).Positions["subject-club"]));

        _store.SetPosition(_mina, Story, null);
        Assert.Equal(["subject-club"], _store.Belongings(_mina).Positions.Keys);
    }

    [Fact]
    public void The_list_keeps_the_page_kept_last_at_its_end_and_no_page_twice()
    {
        _store.Keep(_mina, Page("character-rostam"));
        _store.Keep(_mina, Page("character-sohrab"));
        _store.Keep(_mina, Page("character-rostam"));
        _store.Keep(_mina, Page("character-rostam", "media"));

        Assert.Equal([Page("character-sohrab"), Page("character-rostam"), Page("character-rostam", "media")], _store.Belongings(_mina).Kept);

        _store.Drop(_mina, Page("character-rostam"));
        _store.Drop(_mina, Page("character-nobody"));
        Assert.Equal([Page("character-sohrab"), Page("character-rostam", "media")], _store.Belongings(_mina).Kept);
    }

    [Fact]
    public void A_full_list_lets_its_oldest_page_go()
    {
        for (var number = 0; number <= PeopleStore.MostKept; number++)
        {
            _store.Keep(_mina, Page($"entity-{number}"));
        }

        var kept = _store.Belongings(_mina).Kept;

        Assert.Equal(PeopleStore.MostKept, kept.Length);
        Assert.Equal((Page("entity-1"), Page($"entity-{PeopleStore.MostKept}")), (kept[0], kept[^1]));
    }

    [Fact]
    public void What_belongs_to_one_account_is_not_another_accounts()
    {
        var sara = _store.Register("Sara", Password).Session!.Id;
        _store.SetPosition(_mina, Story, 3);
        _store.Keep(_mina, Page("character-rostam"));

        _store.Drop(sara, Page("character-rostam"));

        Assert.Equal(Belongings.None.Kept, _store.Belongings(sara).Kept);
        Assert.Empty(_store.Belongings(sara).Positions);
        Assert.Single(_store.Belongings(_mina).Kept);
    }

    [Fact]
    public void Bringing_in_a_device_adds_what_the_account_lacks_and_overwrites_nothing()
    {
        _store.SetPosition(_mina, Story, 1);
        _store.Keep(_mina, Page("character-rostam"));
        var device = new Belongings(
            ImmutableDictionary<string, int>.Empty.Add(Story, 4).Add("subject-club", 2),
            [Page("character-sohrab"), Page("character-rostam"), Page("character-sohrab")]);

        var came = _store.BringIn(_mina, device);

        Assert.Equal((1, 1), came);
        var has = _store.Belongings(_mina);

        // The account said how far the story was read: the device does not move it forward.
        Assert.Equal((1, 2), (has.Positions[Story], has.Positions["subject-club"]));
        Assert.Equal([Page("character-rostam"), Page("character-sohrab")], has.Kept);
        Assert.Equal((0, 0), _store.BringIn(_mina, device));
    }

    [Fact]
    public void A_closed_account_has_nothing_and_takes_nothing_and_a_deleted_one_leaves_nothing()
    {
        _store.SetPosition(_mina, Story, 3);
        _store.Keep(_mina, Page("character-rostam"));
        _store.Close(_mina, Password);

        Assert.Same(Belongings.None, _store.Belongings(_mina));
        _store.Keep(_mina, Page("character-sohrab"));
        _store.SetPosition(_mina, Story, 4);

        // Opened again within the thirty days it has what it had.
        var again = _store.Reopen("Mina", Password)!;
        Assert.Equal((3, 1), (_store.Belongings(again.Id).Positions[Story], _store.Belongings(again.Id).Kept.Length));

        _store.Close(_mina, Password);
        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromMinutes(1);
        Assert.Equal(1, _store.Purge());
        var another = _store.Register("Mina", Password).Session!.Id;
        Assert.Empty(_store.Belongings(another).Kept);
        Assert.Empty(_store.Belongings(another).Positions);
    }

    [Fact]
    public void An_invitation_is_a_code_that_says_nothing_and_lasts_fourteen_days()
    {
        var code = _store.Invite(_mina)!;

        Assert.Matches("^[A-HJ-KM-NP-Z2-9]{20}$", code);
        Assert.DoesNotContain("MINA", code, StringComparison.OrdinalIgnoreCase);
        Assert.True(_store.IsInvited(code));
        Assert.True(_store.IsInvited(code));
        var info = Assert.Single(_store.Invitations(_mina));
        Assert.Equal((code, new DateOnly(2026, 12, 2), InvitationState.Open, true), (info.Code, info.LastDay, info.State, info.IsOpen));

        _clock.Now += TimeSpan.FromDays(14);
        Assert.True(_store.IsInvited(code));
        _clock.Now += TimeSpan.FromDays(1);
        Assert.False(_store.IsInvited(code));
        Assert.Equal((InvitationState.Open, true, false), (_store.Invitations(_mina)[0].State, _store.Invitations(_mina)[0].RunOut, _store.Invitations(_mina)[0].IsOpen));
    }

    [Fact]
    public void An_invitation_makes_an_account_once_and_nothing_else()
    {
        var code = _store.Invite(_mina)!;

        var sara = _store.Register("Sara", Password, code);

        Assert.True(sara.Done);
        Assert.Equal(InvitationState.Used, Assert.Single(_store.Invitations(_mina)).State);
        Assert.False(_store.IsInvited(code));

        // Nothing follows from it: the new account has nothing of the sender's and made no invitation.
        Assert.Empty(_store.Belongings(sara.Session!.Id).Kept);
        Assert.Empty(_store.Invitations(sara.Session.Id));

        // Used once: the same code leads a third person to an account like anybody's, and stays used by the second.
        Assert.True(_store.Register("Dara", Password, code).Done);
        Assert.Equal(InvitationState.Used, Assert.Single(_store.Invitations(_mina)).State);
    }

    [Theory]
    [InlineData("")]
    [InlineData("NOSUCHCODE")]
    [InlineData("a code that is far too long to be the code of an invitation of this site")]
    public void A_code_that_is_no_invitation_stops_nobody_from_making_an_account(string code)
    {
        Assert.False(_store.IsInvited(code));
        Assert.True(_store.Register("Sara", Password, code).Done);
    }

    [Fact]
    public void Only_who_made_an_invitation_takes_it_back_and_only_while_nobody_used_it()
    {
        var sara = _store.Register("Sara", Password).Session!.Id;
        var code = _store.Invite(_mina)!;
        var used = _store.Invite(_mina)!;
        _store.Register("Dara", Password, used);

        Assert.False(_store.Revoke(sara, code));
        Assert.True(_store.IsInvited(code));
        Assert.True(_store.Revoke(_mina, code));
        Assert.False(_store.Revoke(_mina, code));
        Assert.False(_store.Revoke(_mina, used));
        Assert.False(_store.Revoke(_mina, "NOSUCHCODE"));

        Assert.False(_store.IsInvited(code));
        Assert.Equal([InvitationState.Used, InvitationState.Revoked], _store.Invitations(_mina).Select(invitation => invitation.State).Order());
        Assert.True(_store.Register("Zal", Password, code).Done);
    }

    [Fact]
    public void An_account_has_only_so_many_open_invitations_at_a_time()
    {
        var codes = Enumerable.Range(0, PeopleStore.OpenInvitations).Select(_ => _store.Invite(_mina)).ToList();

        Assert.DoesNotContain(null, codes);
        Assert.Equal(PeopleStore.OpenInvitations, codes.Distinct().Count());
        Assert.Null(_store.Invite(_mina));

        // One that was taken back, used or has run out makes room.
        _store.Revoke(_mina, codes[0]!);
        Assert.NotNull(_store.Invite(_mina));
        Assert.Null(_store.Invite(_mina));
        _clock.Now += PeopleStore.InvitationLastsFor + TimeSpan.FromDays(1);
        Assert.NotNull(_store.Invite(_mina));
    }

    [Fact]
    public void The_invitation_of_a_closed_account_invites_nobody_and_goes_with_the_account()
    {
        var code = _store.Invite(_mina)!;
        _store.Close(_mina, Password);

        Assert.False(_store.IsInvited(code));
        Assert.Null(_store.Invite(_mina));

        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromMinutes(1);
        _store.Purge();
        Assert.Empty(_store.Invitations(_mina));
    }

    [Fact]
    public void That_an_invitation_was_used_stays_when_who_used_it_is_deleted()
    {
        var code = _store.Invite(_mina)!;
        var sara = _store.Register("Sara", Password, code).Session!.Id;
        _store.Close(sara, Password);
        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromMinutes(1);

        Assert.Equal(1, _store.Purge());

        Assert.Equal(InvitationState.Used, Assert.Single(_store.Invitations(_mina)).State);
        Assert.False(_store.IsInvited(code));
    }
}
