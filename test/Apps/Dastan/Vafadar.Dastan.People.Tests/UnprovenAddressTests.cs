using System.Text.RegularExpressions;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// An account made through a provider that names an address without proving it (step 20.3, ADR 0016): the
/// account is none until the visitor has confirmed the address by a mail of the site, whichever way they come.
/// Test data.
/// </summary>
public sealed partial class UnprovenAddressTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-unproven-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero));
    private readonly string _database;
    private readonly PeopleStore _store;

    public UnprovenAddressTests()
    {
        _database = TestDatabase.For(Path.Combine(_folder, "people.db"));
        _store = PeopleStore.Open(_database, _clock, "https://dastan.test");
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    // The kinds of the mails that wait in the outbox for an address, the oldest first, and their texts.
    private List<(string Kind, string Text)> Mail(string to)
    {
        using var connection = new Npgsql.NpgsqlConnection(_database);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """select "Kind", "Text" from delivery."Mail" where "To" = @to order by "CreatedAt", "Key" """;
        command.Parameters.AddWithValue("to", to);
        using var rows = command.ExecuteReader();
        var mail = new List<(string, string)>();
        while (rows.Read())
        {
            mail.Add((rows.GetString(0), rows.GetString(1)));
        }

        return mail;
    }

    private (string Id, string Token) Link(string to)
    {
        var link = LinkForm().Match(Mail(to).Last(mail => mail.Kind == "confirm-address").Text);
        Assert.True(link.Success);
        return (link.Groups[1].Value, link.Groups[2].Value);
    }

    [GeneratedRegex(@"https://dastan\.test/(?:en|fa)/account/confirm\?id=([0-9a-f]+)&token=([0-9a-f]+)")]
    private static partial Regex LinkForm();

    [Fact]
    public void An_address_a_provider_only_names_makes_no_account_until_it_is_confirmed_by_mail()
    {
        var made = _store.Accounts.RegisterWith("Mina", "microsoft", "subject-1", "mina@example.org", "en", terms: "2026-11-19");

        // Asked for, and nothing more: no sign-in, no codes, and coming back through the provider signs in nobody.
        Assert.True(made.Done);
        Assert.Null(made.Session);
        Assert.Empty(made.RecoveryCodes);
        Assert.Null(_store.Accounts.SignInWith("microsoft", "subject-1"));

        var (id, token) = Link("mina@example.org");
        Assert.Equal(("mina@example.org", false), _store.Accounts.Address(id));

        var confirmed = _store.Accounts.Confirm(id, token);

        Assert.True(confirmed.Done);
        Assert.Equal(PeopleStore.RecoveryCodeCount, confirmed.RecoveryCodes.Length);
        Assert.Equal(("mina@example.org", true), _store.Accounts.Address(id));
        Assert.Equal(confirmed.Session, _store.Accounts.SignInWith("microsoft", "subject-1"));
    }

    [Fact]
    public void An_address_somebody_only_wrote_tells_them_nothing_about_who_has_it()
    {
        Assert.True(_store.Accounts.RegisterWith("Mina", "google", "subject-1", "mina@example.org", "en", proven: true).Done);

        // Somebody else comes through a provider that proves nothing and writes Mina's address.
        var made = _store.Accounts.RegisterWith("Sara", "microsoft", "subject-2", "MINA@example.org", "en");

        // The answer is the one of a mail that was sent; no account was made, and nothing waits for this sign-in.
        Assert.True(made.Done);
        Assert.Null(made.Session);
        Assert.False(_store.Accounts.Waits("microsoft", "subject-2"));
        Assert.Null(_store.Accounts.SignInWith("microsoft", "subject-2"));

        // Mina is told that somebody tried, and gets no link.
        Assert.Equal(["address-in-use"], Mail("mina@example.org").Select(mail => mail.Kind));

        // The name was not taken by it either.
        Assert.True(_store.Accounts.RegisterWith("Sara", "microsoft", "subject-2", "sara@example.org", "en").Done);
        Assert.True(_store.Accounts.Waits("microsoft", "subject-2"));
    }

    [Fact]
    public void Who_waits_can_ask_for_the_mail_again_and_correct_the_address_and_the_old_link_is_over()
    {
        Assert.True(_store.Accounts.RegisterWith("Mina", "microsoft", "subject-1", "mian@example.org", "en").Done);
        Assert.True(_store.Accounts.Waits("microsoft", "subject-1"));
        var (id, mistyped) = Link("mian@example.org");

        // Coming back any number of times makes nothing new and signs nobody in.
        Assert.Equal(["login-taken"], _store.Accounts.RegisterWith("Mina Two", "microsoft", "subject-1", "other@example.org", "en").Problems);
        Assert.Null(_store.Accounts.SignInWith("microsoft", "subject-1"));

        // Too soon after the first mail nothing is sent, and the answer is the same.
        Assert.Equal("sent", _store.Accounts.ConfirmWaiting("microsoft", "subject-1", "mina@example.org"));
        Assert.Empty(Mail("mina@example.org"));

        _clock.Now += VisitorAccounts.SendAgainAfter + TimeSpan.FromSeconds(1);
        Assert.Equal("mail", _store.Accounts.ConfirmWaiting("microsoft", "subject-1", "no address"));
        Assert.Equal("none", _store.Accounts.ConfirmWaiting("microsoft", "nobody"));
        Assert.Equal("sent", _store.Accounts.ConfirmWaiting("microsoft", "subject-1", "mina@example.org"));

        // The link that went to the mistyped address confirms nothing any more: whoever has that mailbox does
        // not make the account theirs.
        Assert.False(_store.Accounts.Confirm(id, mistyped).Done);
        Assert.Equal(("mina@example.org", false), _store.Accounts.Address(id));
        var (again, token) = Link("mina@example.org");
        Assert.Equal(id, again);
        Assert.True(_store.Accounts.Confirm(id, token).Done);
        Assert.False(_store.Accounts.Confirm(id, token).Done);
        Assert.False(_store.Accounts.Waits("microsoft", "subject-1"));
        Assert.NotNull(_store.Accounts.SignInWith("microsoft", "subject-1"));

        // Nothing waits any more, so nothing is sent.
        Assert.Equal("none", _store.Accounts.ConfirmWaiting("microsoft", "subject-1"));
    }

    [Fact]
    public void Correcting_to_an_address_that_has_an_account_takes_nothing_and_says_nothing()
    {
        Assert.True(_store.Accounts.RegisterWith("Sara", "google", "subject-9", "sara@example.org", "en", proven: true).Done);
        Assert.True(_store.Accounts.RegisterWith("Mina", "microsoft", "subject-1", "mina@example.org", "en").Done);
        var (id, _) = Link("mina@example.org");
        _clock.Now += VisitorAccounts.SendAgainAfter + TimeSpan.FromSeconds(1);

        Assert.Equal("sent", _store.Accounts.ConfirmWaiting("microsoft", "subject-1", "sara@example.org"));

        Assert.Equal(("mina@example.org", false), _store.Accounts.Address(id));
        Assert.Equal(["address-in-use"], Mail("sara@example.org").Select(mail => mail.Kind));
    }

    [Fact]
    public void Accounts_without_a_record_of_their_proof_are_listed_and_asked_once()
    {
        // Two accounts as the earlier version made them: confirmed, through a provider, and nothing says how.
        var mina = _store.Accounts.RegisterWith("Mina", "microsoft", "subject-1", "mina@example.org", "en", proven: true).Session!;
        var sara = _store.Accounts.RegisterWith("Sara", "google", "subject-2", "sara@example.org", "en", proven: true).Session!;
        Write("""update identity."VisitorAccounts" set "AddressProof" = null""");
        var dara = _store.Accounts.RegisterWith("Dara", "microsoft", "subject-3", "dara@example.org", "en", proven: true).Session!;

        Assert.Equal(["Mina:microsoft", "Sara:google"], _store.Accounts.WithoutProof().Select(account => $"{account.Name}:{string.Join('+', account.Providers)}"));

        Assert.Equal(1, _store.Accounts.AskForProof("microsoft"));

        // Mina is asked: nothing of hers is gone, no sign-in of hers holds – not the one she had, not a new one
        // through the provider – and the link is in her mail.
        Assert.False(_store.Holds(mina));
        Assert.Null(_store.Accounts.SignInWith("microsoft", "subject-1"));
        Assert.True(_store.Accounts.Waits("microsoft", "subject-1"));
        Assert.True(_store.Holds(sara));
        Assert.True(_store.Holds(dara));
        var (id, token) = Link("mina@example.org");
        Assert.Equal(mina.Id, id);

        // A second run asks nobody again.
        Assert.Equal(0, _store.Accounts.AskForProof("microsoft"));

        var confirmed = _store.Accounts.Confirm(id, token);
        Assert.True(confirmed.Done);
        Assert.True(_store.Holds(confirmed.Session!));
        Assert.Equal(["Sara:google"], _store.Accounts.WithoutProof().Select(account => $"{account.Name}:{string.Join('+', account.Providers)}"));
    }

    private void Write(string sql)
    {
        using var connection = new Npgsql.NpgsqlConnection(_database);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
