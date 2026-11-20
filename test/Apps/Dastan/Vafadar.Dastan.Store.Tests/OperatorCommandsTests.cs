using Vafadar.Dastan.Content;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.People;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// The commands of who runs the site for notices about content and for restrictions (row 18.8): a notice is
/// listed, read and settled; a photograph, a tree or an account is restricted with its reasons and the
/// restriction is lifted; the record of a person is taken out of a tree.
/// </summary>
public sealed class OperatorCommandsTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private static readonly DateOnly Today = new(2026, 11, 19);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-operator-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 19, 9, 0, 0, TimeSpan.Zero));

    private string Notices => People;

    private string People => TestDatabase.For(Path.Combine(_folder, "people.db"));


    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private (int Exit, string Output) Run(params string[] arguments)
    {
        using var output = new StringWriter();
        return (ContentCommands.Run(arguments, output, Today, _clock), output.ToString());
    }

    [Fact]
    public void A_notice_is_listed_read_settled_and_forgotten_after_its_time()
    {
        var box = new NoticeBox(Notices);
        var reference = box.Take("en", "https://dastan.online/en/tree/abc", "image", "The photograph shows me and I never agreed.", "A Person", "person@example.org", goodFaith: true, _clock.GetUtcNow().UtcDateTime)!;

        var listed = Run("notices", Notices);
        Assert.Equal(ContentCommands.Done, listed.Exit);
        Assert.Contains("1 notice waits.", listed.Output, StringComparison.Ordinal);
        Assert.Contains($"{reference}  2026-11-19 09:00 UTC  image  https://dastan.online/en/tree/abc  (left an address)", listed.Output, StringComparison.Ordinal);

        var shown = Run("notices", "show", reference, Notices);
        Assert.Contains("The photograph shows me and I never agreed.", shown.Output, StringComparison.Ordinal);
        Assert.Contains("A Person, person@example.org", shown.Output, StringComparison.Ordinal);
        Assert.Contains("good faith:  declared", shown.Output, StringComparison.Ordinal);

        Assert.Equal(ContentCommands.NotUsable, Run("notices", "settle", reference, Notices, "--decision", " ").Exit);
        Assert.Equal(ContentCommands.NotUsable, Run("notices", "settle", "20261119-NOSUCHID", Notices, "--decision", "Removed.").Exit);
        Assert.Equal(ContentCommands.Done, Run("notices", "settle", reference, Notices, "--decision", "The photograph was hidden, decision no. 1.").Exit);
        Assert.Contains("0 notices wait.", Run("notices", Notices).Output, StringComparison.Ordinal);
        Assert.Contains("decision:    The photograph was hidden, decision no. 1.", Run("notices", "show", reference, Notices).Output, StringComparison.Ordinal);

        // Kept for a year after the decision, and not a day longer.
        _clock.Advance(TimeSpan.FromDays(364));
        Assert.Contains("0 settled notices removed.", Run("notices", "forget", Notices).Output, StringComparison.Ordinal);
        _clock.Advance(TimeSpan.FromDays(2));
        Assert.Contains("1 settled notice removed.", Run("notices", "forget", Notices).Output, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.NotUsable, Run("notices", "show", reference, Notices).Exit);
        // A database without the tables, and none at all: the command says what is missing and does nothing.
        var early = Run("notices", TestDatabase.Empty());
        Assert.Equal(ContentCommands.NotUsable, early.Exit);
        Assert.Contains("database migrate", early.Output, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.NotUsable, Run("notices", TestDatabase.Missing()).Exit);
    }

    [Fact]
    public void A_database_is_brought_to_form_by_the_command_and_its_connection_is_never_a_word_of_it()
    {
        var empty = TestDatabase.Empty();

        // A database without the tables is not worked on, and nothing makes them by the way.
        var early = Run("people", "restrictions", "--people", empty);
        Assert.Equal(ContentCommands.NotUsable, early.Exit);
        Assert.Contains("database migrate", early.Output, StringComparison.Ordinal);

        Assert.Equal(ContentCommands.Done, Run("database", "migrate", "--database", empty).Exit);
        Assert.Equal(ContentCommands.Done, Run("database", "migrate", "--database", empty).Exit);
        Assert.Contains("0 restrictions stand.", Run("people", "restrictions", "--people", empty).Output, StringComparison.Ordinal);

        // The word "env" stands for the variable; without the variable the command says so and does nothing.
        var before = Environment.GetEnvironmentVariable("DASTAN_DATABASE");
        try
        {
            Environment.SetEnvironmentVariable("DASTAN_DATABASE", null);
            Assert.Equal(ContentCommands.NotUsable, Run("people", "restrictions", "--people", "env").Exit);
            Environment.SetEnvironmentVariable("DASTAN_DATABASE", empty);
            Assert.Equal(ContentCommands.Done, Run("people", "restrictions", "--people", "env").Exit);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DASTAN_DATABASE", before);
        }

        Assert.Equal(ContentCommands.NotUsable, Run("database", "migrate", "--database", "Host=127.0.0.1;Port=1;Timeout=2").Exit);
    }

    [Fact]
    public void The_mail_that_waits_and_the_mail_that_was_given_up_is_listed_tried_again_and_forgotten()
    {
        var outbox = new MailOutbox(People);
        var now = _clock.GetUtcNow().UtcDateTime;
        outbox.Put(new OutgoingMail("confirm:1", "confirm-address", "mina@example.org", "en", "Confirm your address", "A link nobody else may read."), now);
        outbox.Put(new OutgoingMail("confirm:2", "confirm-address", "sara@example.org", "en", "Confirm your address", "Another link."), now);

        // One message cannot be sent, however often it is tried.
        var refusing = new Refusing("sara@example.org");
        for (var attempt = 0; attempt < MailOutbox.MostAttempts; attempt++)
        {
            outbox.Deliver(refusing, now.AddDays(attempt), "dastan.test");
        }

        outbox.Put(new OutgoingMail("confirm:3", "confirm-address", "omid@example.org", "en", "Confirm your address", "A third link."), now.AddDays(8));
        _clock.Advance(TimeSpan.FromDays(8));

        var listed = Run("mail", People);
        Assert.Equal(ContentCommands.Done, listed.Exit);
        Assert.Contains("1 message waits to be sent; 1 was given up.", listed.Output, StringComparison.Ordinal);
        Assert.Contains("waits     confirm:3  confirm-address  to omid@example.org  tried 0×", listed.Output, StringComparison.Ordinal);
        Assert.Contains($"given up  confirm:2  confirm-address  to sara@example.org  tried {MailOutbox.MostAttempts}×, last: InvalidOperationException", listed.Output, StringComparison.Ordinal);

        // What a message says is never listed.
        Assert.DoesNotContain("link", listed.Output, StringComparison.Ordinal);

        Assert.Equal(ContentCommands.NotUsable, Run("mail", "again", "confirm:1", People).Exit);
        Assert.Equal(ContentCommands.Done, Run("mail", "again", "confirm:2", People).Exit);
        Assert.Contains("2 messages wait to be sent; 0 were given up.", Run("mail", People).Output, StringComparison.Ordinal);

        // The record of what left is kept for a while and then removed; what waits is never removed.
        Assert.Contains("0 messages that left", Run("mail", "forget", People).Output, StringComparison.Ordinal);
        Assert.Contains("1 message that left", Run("mail", "forget", People, "--kept-days", "7").Output, StringComparison.Ordinal);
        Assert.Equal(2, outbox.Waiting().Count);
        Assert.Equal(ContentCommands.NotUnderstood, Run("mail", "forget", People, "--kept-days", "many").Exit);
        Assert.Equal(ContentCommands.NotUsable, Run("mail", TestDatabase.Empty()).Exit);
    }

    // Sends to everybody but one address.
    private sealed class Refusing(string address) : IMailSender
    {
        public void Send(OutgoingMail mail, string messageId)
        {
            if (mail.To == address)
            {
                throw new InvalidOperationException("The server refused the address.");
            }
        }
    }

    [Fact]
    public void The_site_and_the_tool_each_get_a_role_that_may_do_what_the_program_does_and_no_more()
    {
        var owner = People;
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var (site, tool) = ($"dastan_site_{suffix}", $"dastan_tool_{suffix}");
        string As(string role) => new Npgsql.NpgsqlConnectionStringBuilder(owner) { Username = role }.ConnectionString;

        void Sql(string connection, string sql)
        {
            using var database = new Npgsql.NpgsqlConnection(connection);
            database.Open();
            using var command = database.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        string Refused(string connection, string sql) => Assert.Throws<Npgsql.PostgresException>(() => Sql(connection, sql)).SqlState;

        // The roles are made by who runs the database; the command only grants.
        Sql(owner, $"create role {site} login; create role {tool} login");
        try
        {
            Assert.Equal(ContentCommands.Done, Run("database", "grant", "site", site, "--database", owner).Exit);
            Assert.Equal(ContentCommands.Done, Run("database", "grant", "tool", tool, "--database", owner).Exit);
            Assert.Equal(ContentCommands.Done, Run("database", "grant", "tool", tool, "--database", owner).Exit);

            // The tool does the work of the editors with its role: a collection, a revision, a release.
            using var copy = new WorkingCopy();
            using (var editors = EditorialStore.Open(As(tool), _clock, copy.Samples))
            {
                Assert.Empty(editors.Import(copy.Sample, "an editor", "The first import.").Problems);
                foreach (var statement in editors.Statements(WorkingCopy.Collection))
                {
                    Assert.True(editors.Review(WorkingCopy.Collection, statement.Statement, ReviewAct.Reviewed, "a reviewer", Today, "Read against the text.").Done);
                }

                Assert.Equal(1, editors.Publish(WorkingCopy.Collection, Today, "a publisher").Number);
                Assert.Null(editors.Release(WorkingCopy.Collection, 1, "a publisher"));

                // The accounts of the editors are the tool's too: one is made and signs in.
                Assert.True(editors.AddEditor("Omid", Password).Done);
                Assert.NotNull(editors.SignIn("Omid", Password));
            }

            // The site keeps an account, takes a report and a notice, and reads what was released.
            using (var people = PeopleStore.Open(As(site), _clock))
            {
                Assert.NotNull(people.Register("Mina", Password).Session);
                Assert.Empty(people.Accounts.Register("Sara", "sara@example.org", Password, "en"));
            }

            Assert.True(new ReportBox(As(site)).Take("story-x", 1, "en", "evidence/entity/x", "A report.", Today));
            Assert.NotNull(new NoticeBox(As(site)).Take("en", "https://dastan.online/en/tree/abc", "image", "The photograph shows me and I never agreed.", null, null, goodFaith: true, _clock.GetUtcNow().UtcDateTime));
            Sql(As(site), """select "Json" from published."Revisions"; select "Number" from published."Releases" """);

            // And no more. The site: nothing of the editors, no change of what was published, no look at a report again.
            const string denied = Npgsql.PostgresErrorCodes.InsufficientPrivilege;
            Assert.Equal(denied, Refused(As(site), """select 1 from editorial."Records" """));
            Assert.Equal(denied, Refused(As(site), """select 1 from identity."EditorAccounts" """));
            Assert.Equal(denied, Refused(As(site), """update identity."EditorAccounts" set "Roles" = 'writer,reviewer,publisher'"""));
            Assert.Equal(denied, Refused(As(site), """update published."Revisions" set "By" = 'somebody'"""));
            Assert.Equal(denied, Refused(As(site), """delete from published."Releases" """));
            Assert.Equal(denied, Refused(As(site), """update delivery."Reports" set "Text" = 'changed'"""));
            Assert.Equal(denied, Refused(As(site), """delete from personal."__EFMigrationsHistory" """));
            Assert.Equal(denied, Refused(As(site), "create table personal.more (id int)"));

            // The tool: nothing of the visitors, a revision is never changed, a report is noted and not removed.
            Assert.Equal(denied, Refused(As(tool), """select 1 from personal."Visitors" """));
            Assert.Equal(denied, Refused(As(tool), """select 1 from identity."VisitorAccounts" """));
            Assert.Equal(denied, Refused(As(tool), """select 1 from identity."VisitorTokens" """));
            Assert.Equal(denied, Refused(As(tool), """select 1 from identity."VisitorLogins" """));
            Assert.Equal(denied, Refused(As(tool), """update published."Revisions" set "By" = 'somebody'"""));
            Assert.Equal(denied, Refused(As(tool), """delete from published."Releases" """));
            Assert.Equal(denied, Refused(As(tool), """delete from delivery."Reports" """));
            Assert.True(new ReportBox(As(tool)).Settle(Assert.Single(new ReportBox(As(tool)).Waiting()).Id, "a reviewer", Today));

            // What is not asked for completely is not done: a program that is none, a name that is no name of a role.
            Assert.Equal(ContentCommands.NotUnderstood, Run("database", "grant", "everybody", site, "--database", owner).Exit);
            Assert.Equal(ContentCommands.NotUnderstood, Run("database", "grant", "site", "Robert\"; drop role postgres; --", "--database", owner).Exit);
            Assert.Equal(ContentCommands.NotUsable, Run("database", "grant", "site", "no_such_role_" + suffix, "--database", owner).Exit);
        }
        finally
        {
            // A role belongs to the whole server: it is taken out with everything it was given.
            using var database = new Npgsql.NpgsqlConnection(owner);
            database.Open();
            foreach (var role in new[] { site, tool })
            {
                using var command = database.CreateCommand();
                command.CommandText = $"drop owned by {role}; drop role {role}";
                command.ExecuteNonQuery();
            }
        }
    }

    [Fact]
    public void A_tree_and_an_account_are_restricted_with_their_reasons_listed_and_lifted()
    {
        string tree, zal;
        using (var store = PeopleStore.Open(People, _clock))
        {
            var mina = store.Register("Mina", Password).Session!.Id;
            tree = store.AddTree(mina, "The Rad family")!;
            zal = store.AddPerson(mina, tree, "Zal", 1870, 1950, deceased: true)!;
        }

        var restricted = Run("people", "restrict", "tree", tree, "--people", People, "--site", "https://dastan.test", "--reason", "It names a living person who objected.", "--ground", "Section 7 of the terms", "--noticed");
        Assert.Equal(ContentCommands.Done, restricted.Exit);
        Assert.Contains("restricted, decision no. 1.", restricted.Output, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.Done, Run("people", "restrict", "account", "Mina", "--people", People, "--site", "https://dastan.test", "--reason", "Repeated breaches.", "--ground", "Section 10 of the terms").Exit);
        Assert.Equal(ContentCommands.NotUsable, Run("people", "restrict", "photo", "no-such-photo", "--people", People, "--site", "https://dastan.test", "--reason", "A reason.", "--ground", "A ground.").Exit);
        Assert.Equal(ContentCommands.NotUsable, Run("people", "restrictions", "--people", TestDatabase.Missing()).Exit);

        var standing = Run("people", "restrictions", "--people", People).Output;
        Assert.Contains("2 restrictions stand.", standing, StringComparison.Ordinal);
        Assert.Contains($"1  2026-11-19  tree  {tree}  of Mina  (after a notice)", standing, StringComparison.Ordinal);
        Assert.Contains("reason: Repeated breaches.", standing, StringComparison.Ordinal);

        Assert.Contains($"{zal}  Zal", Run("people", "persons", tree, "--people", People).Output, StringComparison.Ordinal);

        // Nothing is left behind in a store that only ever had the locks: the repair finds nothing, with and without changing.
        Assert.Contains("0 places in a circle to take out", Run("people", "repair", "--people", People).Output, StringComparison.Ordinal);
        var repaired = Run("people", "repair", "--people", People, "--change");
        Assert.Equal(ContentCommands.Done, repaired.Exit);
        Assert.Contains("0 places in a tree taken out", repaired.Output, StringComparison.Ordinal);
        Assert.Contains("0 trees in which somebody is their own ancestor", repaired.Output, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.Done, Run("people", "person", "remove", tree, zal, "--people", People).Exit);
        Assert.Equal(ContentCommands.NotUsable, Run("people", "person", "remove", tree, zal, "--people", People).Exit);

        // An account that is not closed is not forgotten; one its visitor closed is, at once.
        Assert.Equal(ContentCommands.NotUsable, Run("people", "forget", "Nobody", "--people", People).Exit);
        Assert.Equal(ContentCommands.Done, Run("people", "forget", "Mina", "--people", People).Exit);
        using (var closed = PeopleStore.Open(People, _clock))
        {
            Assert.Equal(1, closed.Purge());
        }

        Assert.Equal(ContentCommands.NotUsable, Run("people", "lift", "2", "--people", People, "--site", "https://dastan.test").Exit);
        Assert.Equal(ContentCommands.NotUnderstood, Run("people", "lift", "two", "--people", People, "--site", "https://dastan.test").Exit);
        using var gone = PeopleStore.Open(People, _clock);
        Assert.Null(gone.SignIn("Mina", Password));
        Assert.Empty(gone.AllRestrictions());
    }
}
