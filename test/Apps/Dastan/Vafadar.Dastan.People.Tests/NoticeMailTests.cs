using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// What the site mails an account about what others did (step 19.4, ADR 0016; R-PER-03, PRIV-007): a request
/// to show a relationship, a tag, a request to agree – which an account can turn off – and a decision of who
/// runs the site with its reasons, which is always sent; and an invitation that is mailed. Test data.
/// </summary>
public sealed class NoticeMailTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-notices-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 19, 9, 0, 0, TimeSpan.Zero));
    private readonly string _database;
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;

    public NoticeMailTests()
    {
        _database = TestDatabase.For(Path.Combine(_folder, "people.db"));
        _store = PeopleStore.Open(_database, _clock, "https://dastan.test/");
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

    private static string Sara => TestAccounts.MailOf("Sara");

    private static string Mina => TestAccounts.MailOf("Mina");

    // What waits in the outbox for an address, the oldest first – without the note that the made-up account of
    // a test got its password (TestAccounts), which is no part of what is tried here.
    private List<(string Kind, string Language, string Subject, string Text, string Html)> Mail(string to)
    {
        using var connection = new Npgsql.NpgsqlConnection(_database);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """select "Kind", "Language", "Subject", "Text", "Html" from delivery."Mail" where "To" = @to and "Kind" <> 'password-changed' order by "CreatedAt", "Key" """;
        command.Parameters.AddWithValue("to", to);
        using var rows = command.ExecuteReader();
        var mail = new List<(string, string, string, string, string)>();
        while (rows.Read())
        {
            mail.Add((rows.GetString(0), rows.GetString(1), rows.GetString(2), rows.GetString(3), rows.GetString(4)));
        }

        return mail;
    }

    private void Friends()
    {
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "family"));
        Assert.True(_store.Answer(_sara, _store.Relationships(_sara)[0].Id, accept: true));
    }

    private string Photo(string id = "photo-1", ScopeKind shown = ScopeKind.Private)
    {
        Assert.True(_store.AddPhoto(_mina, id, new PhotoFile($"{id}.jpg", "image/jpeg", 800, 600, 12345), "At the sea", null, shown));
        return id;
    }

    [Fact]
    public void Who_is_asked_to_show_a_relationship_is_told_where_to_answer_and_nothing_else()
    {
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "family"));

        var told = Assert.Single(Mail(Sara));
        Assert.Equal(("relationship-asked", "en", "Mina asks something of you on Dastan"), (told.Kind, told.Language, told.Subject));
        Assert.Contains("https://dastan.test/en/account/people", told.Text, StringComparison.Ordinal);
        Assert.Contains("Nothing is shown unless you accept.", told.Text, StringComparison.Ordinal);
        Assert.Contains("You can turn these messages off", told.Text, StringComparison.Ordinal);
        Assert.Contains("<a href=\"https://dastan.test/en/account/people\">", told.Html, StringComparison.Ordinal);

        // Not the kind of the relationship, and nothing to who asked.
        Assert.DoesNotContain("family", told.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Mail(Mina));

        // A request that is refused – they have one already – writes nothing more.
        Assert.Equal("exists", _store.Ask(_mina, "Sara", "friend"));
        Assert.Single(Mail(Sara));
    }

    [Fact]
    public void An_account_that_turned_the_messages_off_is_told_nothing_of_what_others_ask()
    {
        Assert.True(_store.Accounts.Info(_sara)!.Notices);
        Assert.True(_store.Accounts.SetNotices(_sara, on: false));
        Assert.False(_store.Accounts.Info(_sara)!.Notices);
        Assert.False(_store.Accounts.SetNotices("no-such-account", on: false));

        Friends();
        _store.TagPhoto(_mina, Photo(), _sara);

        Assert.Empty(Mail(Sara));

        // What who runs the site decides is told all the same.
        var tree = _store.AddTree(_sara, "The Rad family")!;
        Assert.NotNull(_store.Restrict(RestrictionKind.Tree, tree, "It names a living person against their will.", "Terms, section 5", noticed: true));
        Assert.Equal("restricted", Assert.Single(Mail(Sara)).Kind);

        // Turned on again, the next request is told.
        Assert.True(_store.Accounts.SetNotices(_sara, on: true));
        _store.TagPhoto(_mina, Photo("photo-2"), _sara);
        Assert.Equal(["photo-tagged", "restricted"], Mail(Sara).Select(mail => mail.Kind).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void A_tag_and_a_photograph_for_everybody_are_told_to_who_is_on_it_once()
    {
        Friends();

        // A photograph for everybody that names somebody is held back, and seen by them meanwhile.
        var photo = Photo(shown: ScopeKind.Public);
        Assert.True(_store.TagPhoto(_mina, photo, _sara));

        var tagged = Mail(Sara).Single(mail => mail.Kind == "photo-tagged");
        Assert.Contains("Mina tagged you on one of their photographs.", tagged.Text, StringComparison.Ordinal);
        Assert.Contains("https://dastan.test/en/account/photos", tagged.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("At the sea", tagged.Text, StringComparison.Ordinal);

        // Taken back and put on again, the tag does not write again; a tag of the owner on themselves writes nothing.
        Assert.True(_store.UntagPhoto(_mina, photo, _sara));
        Assert.True(_store.TagPhoto(_mina, photo, _sara));
        Assert.True(_store.TagPhoto(_mina, photo, _mina));
        Assert.Single(Mail(Sara), mail => mail.Kind == "photo-tagged");
        Assert.Empty(Mail(Mina));

        // Who approves a tag is asked on the page they do it on, not by mail. A photograph they are on that is
        // shown to everybody later asks them by mail.
        Assert.True(_store.AnswerTag(_sara, photo, approve: true));
        Assert.DoesNotContain(Mail(Sara), mail => mail.Kind == "photo-everybody-asked");
        Assert.True(_store.SharePhoto(_mina, photo, ScopeKind.Private));
        Assert.True(_store.SharePhoto(_mina, photo, ScopeKind.Public));
        var asked = Mail(Sara).Single(mail => mail.Kind == "photo-everybody-asked");
        Assert.Contains("It is not shown to everybody unless you agree.", asked.Text, StringComparison.Ordinal);

        // Once a day at most.
        Assert.True(_store.SharePhoto(_mina, photo, ScopeKind.Private));
        Assert.True(_store.SharePhoto(_mina, photo, ScopeKind.Public));
        Assert.Single(Mail(Sara), mail => mail.Kind == "photo-everybody-asked");
        _clock.Now = _clock.Now.AddDays(1);
        Assert.True(_store.SharePhoto(_mina, photo, ScopeKind.Private));
        Assert.True(_store.SharePhoto(_mina, photo, ScopeKind.Public));
        Assert.Equal(2, Mail(Sara).Count(mail => mail.Kind == "photo-everybody-asked"));
    }

    [Fact]
    public void Who_a_tree_names_is_told_that_it_is_to_be_public_in_their_language()
    {
        var dara = _store.Accounts.RegisterWith("Dara", "made-up", "subject-of-dara", "dara@example.org", "fa").Session!.Id;
        Friends();
        Assert.Equal("asked", _store.Ask(_mina, "Dara", "family"));
        Assert.True(_store.Answer(dara, _store.Relationships(dara)[0].Id, accept: true));
        var tree = _store.AddTree(_mina, "The Rad family")!;
        var record = _store.AddPerson(_mina, tree, "Dara Rad", null, null, deceased: false)!;
        _store.AddPerson(_mina, tree, "Zal", 1270, null, deceased: true);
        Assert.True(_store.SetTreeMember(_mina, tree, dara, TreeRole.Viewer));
        Assert.True(_store.SetTreeMember(_mina, tree, _sara, TreeRole.Viewer));
        _store.Claim(dara, tree, record);
        Assert.True(_store.ReviewClaim(_mina, tree, _store.Tree(_mina, tree)!.Claims[0].Number, confirm: true));

        Assert.True(_store.AskPublication(_mina, tree));

        var told = Mail("dara@example.org").Single(mail => mail.Kind == "tree-publication-asked");
        Assert.Equal("fa", told.Language);
        Assert.Contains("«Mina» می‌خواهد شجره‌نامه‌ای را در داستان عمومی کند", told.Text, StringComparison.Ordinal);
        Assert.Contains("https://dastan.test/fa/account/trees", told.Text, StringComparison.Ordinal);
        Assert.Contains("dir=\"rtl\"", told.Html, StringComparison.Ordinal);

        // Nothing of the tree is in the message, and who is only a member of the tree is not asked.
        Assert.DoesNotContain("Rad", told.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(Mail(Sara), mail => mail.Kind == "tree-publication-asked");
    }

    [Theory]
    [InlineData(RestrictionKind.Photo, "A photograph of yours was hidden")]
    [InlineData(RestrictionKind.Tree, "A family tree of yours is no longer public")]
    [InlineData(RestrictionKind.Account, "Your account was closed")]
    public void A_decision_of_who_runs_the_site_is_mailed_with_its_reasons_and_so_is_its_end(RestrictionKind kind, string what)
    {
        var target = kind switch
        {
            RestrictionKind.Photo => Photo(),
            RestrictionKind.Tree => _store.AddTree(_mina, "The Rad family")!,
            _ => "Mina",
        };

        var number = _store.Restrict(kind, target, "It shows a person who objected. <b>", "Terms of use, section 5", noticed: true)!.Value;

        var told = Assert.Single(Mail(Mina));
        Assert.Equal(("restricted", $"{what} (decision number {number})"), (told.Kind, told.Subject));
        foreach (var part in new[] { $"Number of the decision: {number}.", "The facts: It shows a person who objected. <b>", "The ground: Terms of use, section 5", "Somebody gave notice of the content", "No automated means were used", "within six months", "https://dastan.test/en/imprint" })
        {
            Assert.Contains(part, told.Text, StringComparison.Ordinal);
        }

        // What an operator wrote is text in the HTML part, never markup.
        Assert.Contains("objected. &lt;b&gt;", told.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>", told.Html, StringComparison.Ordinal);

        // What cannot be restricted writes nothing.
        Assert.Null(_store.Restrict(RestrictionKind.Photo, "no-such-photo", "A reason.", "A ground.", noticed: false));
        Assert.Single(Mail(Mina));

        Assert.True(_store.Lift(number));
        var lifted = Mail(Mina).Single(mail => mail.Kind == "restriction-lifted");
        Assert.Equal($"Decision number {number} was lifted", lifted.Subject);
        Assert.Equal(kind == RestrictionKind.Account, lifted.Text.Contains("is open again", StringComparison.Ordinal));
        Assert.False(_store.Lift(number));
        Assert.Equal(2, Mail(Mina).Count);
    }

    [Fact]
    public void An_invitation_is_mailed_to_an_address_that_is_kept_for_nothing_else()
    {
        Assert.Equal("mail", _store.InviteByMail(_mina, "no address", "en"));
        Assert.Empty(_store.Invitations(_mina));

        Assert.Equal("sent", _store.InviteByMail(_mina, " friend@example.org ", "fa"));

        var invitation = Assert.Single(_store.Invitations(_mina));
        var mailed = Assert.Single(Mail("friend@example.org"));
        Assert.Equal(("invitation", "fa", "«Mina» شما را به داستان دعوت کرده است"), (mailed.Kind, mailed.Language, mailed.Subject));
        Assert.Contains($"https://dastan.test/fa/account?invitation={invitation.Code}", mailed.Text, StringComparison.Ordinal);
        Assert.Contains("2026-12-03", mailed.Text, StringComparison.Ordinal);
        Assert.True(_store.IsInvited(invitation.Code));

        // The address is in the message and nowhere else of the store.
        using (var connection = new Npgsql.NpgsqlConnection(_database))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = TestDatabase.Sql("select count(*) from Invitations where cast(Invitations as text) like '%friend@example.org%'");
            Assert.Equal(0L, command.ExecuteScalar());
        }

        // As many open invitations as an account may have, and no more – by mail as by link.
        while (_store.InviteByMail(_mina, "friend@example.org", "en") == "sent")
        {
        }

        Assert.Equal(PeopleStore.OpenInvitations, _store.Invitations(_mina).Count(open => open.IsOpen));
        Assert.Equal("invitations", _store.InviteByMail(_mina, "other@example.org", "en"));
        Assert.Empty(Mail("other@example.org"));
    }
}
