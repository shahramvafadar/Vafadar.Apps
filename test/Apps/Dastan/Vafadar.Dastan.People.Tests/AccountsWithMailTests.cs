using System.Text.RegularExpressions;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// The accounts of the visitors on ASP.NET Core Identity (step 19.4, ADR 0016): an account has a name, a mail
/// address and a password; the address belongs to one account and is confirmed, by a link that is mailed,
/// before the account is one; a wrong password is counted and pauses the account; and whether an address has
/// an account is told to nobody but who has the address. Test data.
/// </summary>
public sealed partial class AccountsWithMailTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-accounts-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 19, 9, 0, 0, TimeSpan.Zero));
    private readonly string _database;

    public AccountsWithMailTests() => _database = TestDatabase.For(Path.Combine(_folder, "people.db"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private PeopleStore Open() => PeopleStore.Open(_database, _clock, "https://dastan.test");

    // What waits in the outbox for an address, the oldest first: kind, subject, text and HTML.
    private List<(string Kind, string Language, string Subject, string Text, string Html)> Mail(string to)
    {
        using var connection = new Npgsql.NpgsqlConnection(_database);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """select "Kind", "Language", "Subject", "Text", "Html" from delivery."Mail" where "To" = @to order by "CreatedAt", "Key" """;
        command.Parameters.AddWithValue("to", to);
        using var rows = command.ExecuteReader();
        var mail = new List<(string, string, string, string, string)>();
        while (rows.Read())
        {
            mail.Add((rows.GetString(0), rows.GetString(1), rows.GetString(2), rows.GetString(3), rows.GetString(4)));
        }

        return mail;
    }

    // The identifier and the token of the link in the newest mail that confirms an address.
    private (string Id, string Token) Link(string to)
    {
        var link = LinkForm().Match(Mail(to).Last(mail => mail.Kind == "confirm-address").Text);
        Assert.True(link.Success);
        return (link.Groups[1].Value, link.Groups[2].Value);
    }

    [GeneratedRegex(@"https://dastan\.test/(?:en|fa)/account/confirm\?id=([0-9a-f]+)&token=([0-9a-f]+)")]
    private static partial Regex LinkForm();

    [Fact]
    public void An_account_is_one_once_the_link_that_was_mailed_to_its_address_is_opened()
    {
        using var store = Open();
        Assert.Empty(store.Accounts.Register("Mina", "mina@example.org", Password, "en", terms: "2026-11-19"));

        // The mail is written, in the language of the page, with one link, as text and as HTML.
        var mail = Assert.Single(Mail("mina@example.org"));
        Assert.Equal(("confirm-address", "en", "Confirm your mail address for Dastan"), (mail.Kind, mail.Language, mail.Subject));
        Assert.Contains("Hello Mina,", mail.Text, StringComparison.Ordinal);
        Assert.Contains("lang=\"en\" dir=\"ltr\"", mail.Html, StringComparison.Ordinal);
        var (id, token) = Link("mina@example.org");
        Assert.Contains($"token={token}", mail.Html, StringComparison.Ordinal);

        // Before that, the right password signs nobody in – and says why only to who knows the password.
        Assert.Equal(SignInOutcome.Unconfirmed, store.Accounts.SignIn("Mina", Password).Outcome);
        Assert.Equal(SignInOutcome.Refused, store.Accounts.SignIn("Mina", "not the pass phrase").Outcome);
        Assert.Equal(("mina@example.org", false), store.Accounts.Address(id));

        // A token that is not the one of the mail confirms nothing.
        Assert.False(store.Accounts.Confirm(id, new string('0', 64)).Done);
        Assert.False(store.Accounts.Confirm("nobody", token).Done);

        var confirmed = store.Accounts.Confirm(id, token);
        Assert.True(confirmed.Done);
        Assert.Equal(PeopleStore.RecoveryCodeCount, confirmed.RecoveryCodes.Length);
        Assert.Equal(("mina@example.org", true), store.Accounts.Address(id));

        // The link works once.
        Assert.False(store.Accounts.Confirm(id, token).Done);

        // Now the account signs in, by its name or by its address, and what belongs to it is under its identifier.
        var byName = store.Accounts.SignIn("mina", Password);
        var byAddress = store.Accounts.SignIn("MINA@example.org", Password);
        Assert.Equal((SignInOutcome.SignedIn, id, "Mina"), (byName.Outcome, byName.Session!.Id, byName.Session.Name));
        Assert.Equal(byName.Session, byAddress.Session);
        Assert.True(store.Holds(byName.Session));
        Assert.Equal("2026-11-19", store.TermsOf(id));
        Assert.NotNull(store.AddTree(id, "The Rad family"));
    }

    [Fact]
    public void The_mail_is_written_in_the_language_of_the_page_also_from_right_to_left()
    {
        using var store = Open();
        Assert.Empty(store.Accounts.Register("مینا راد", "mina@example.org", Password, "fa"));

        var mail = Assert.Single(Mail("mina@example.org"));
        Assert.Equal(("fa", "نشانی ایمیل خود را در داستان تأیید کنید"), (mail.Language, mail.Subject));
        Assert.Contains("مینا راد گرامی،", mail.Text, StringComparison.Ordinal);
        Assert.Contains("https://dastan.test/fa/account/confirm?id=", mail.Text, StringComparison.Ordinal);
        Assert.Contains("lang=\"fa\" dir=\"rtl\"", mail.Html, StringComparison.Ordinal);

        // A name in any script signs in.
        var (id, token) = Link("mina@example.org");
        Assert.True(store.Accounts.Confirm(id, token).Done);
        Assert.Equal(SignInOutcome.SignedIn, store.Accounts.SignIn("مینا راد", Password).Outcome);
    }

    [Theory]
    [InlineData("Mi", "mina@example.org", Password, "name-length")]
    [InlineData("Mina<script>", "mina@example.org", Password, "name-letters")]
    [InlineData("Mina", "mina.example.org", Password, "mail")]
    [InlineData("Mina", "mina@example.org\r\nBcc: all@example.org", Password, "mail")]
    [InlineData("Mina", "mina@example.org", "too short", "password-short")]
    public void What_cannot_be_an_account_is_said_and_nothing_is_kept_or_mailed(string name, string mail, string password, string problem)
    {
        using var store = Open();

        Assert.Equal([problem], store.Accounts.Register(name, mail, password, "en"));

        Assert.Empty(Mail("mina@example.org"));
        Assert.Equal(SignInOutcome.Refused, store.Accounts.SignIn("Mina", password).Outcome);
    }

    [Fact]
    public void A_name_belongs_to_one_account_and_that_is_said()
    {
        using var store = Open();
        Assert.Empty(store.Accounts.Register("Mina Rad", "mina@example.org", Password, "en"));

        Assert.Equal(["taken"], store.Accounts.Register("mina rad", "another@example.org", Password, "en"));
        Assert.Empty(Mail("another@example.org"));
    }

    [Fact]
    public void An_address_belongs_to_one_account_and_only_who_has_the_address_learns_it()
    {
        using var store = Open();
        Assert.Empty(store.Accounts.Register("Mina", "mina@example.org", Password, "fa"));

        // Somebody else tries with the same address: the answer is the one everybody gets.
        Assert.Empty(store.Accounts.Register("Sara", "MINA@example.org", Password, "en"));
        Assert.Empty(store.Accounts.Register("Omid", "mina@example.org", Password, "en"));

        // No second account, no name taken by the try – and one mail to who has the address, in their language,
        // however often it is tried within the hour.
        Assert.Equal(SignInOutcome.Refused, store.Accounts.SignIn("Sara", Password).Outcome);
        Assert.Empty(store.Accounts.Register("Sara", "sara@example.org", Password, "en"));
        var told = Assert.Single(Mail("mina@example.org"), mail => mail.Kind == "address-in-use");
        Assert.Equal("fa", told.Language);
        Assert.DoesNotContain("token=", told.Text, StringComparison.Ordinal);

        _clock.Now += TimeSpan.FromHours(1);
        Assert.Empty(store.Accounts.Register("Omid", "mina@example.org", Password, "en"));
        Assert.Equal(2, Mail("mina@example.org").Count(mail => mail.Kind == "address-in-use"));
    }

    [Fact]
    public void Five_wrong_passwords_pause_an_account_also_for_the_right_one()
    {
        using var store = Open();
        store.Accounts.Register("Mina", "mina@example.org", Password, "en");
        var (id, token) = Link("mina@example.org");
        Assert.True(store.Accounts.Confirm(id, token).Done);

        for (var attempt = 0; attempt < PeopleStore.FailuresAllowed; attempt++)
        {
            Assert.Equal(SignInOutcome.Refused, store.Accounts.SignIn("Mina", "not the pass phrase").Outcome);
        }

        Assert.Equal(SignInOutcome.Refused, store.Accounts.SignIn("Mina", Password).Outcome);
        _clock.Now += PeopleStore.LockedFor - TimeSpan.FromSeconds(1);
        Assert.Equal(SignInOutcome.Refused, store.Accounts.SignIn("mina@example.org", Password).Outcome);
        _clock.Now += TimeSpan.FromSeconds(2);
        Assert.Equal(SignInOutcome.SignedIn, store.Accounts.SignIn("Mina", Password).Outcome);

        // The right password in between begins the count again.
        for (var attempt = 0; attempt < PeopleStore.FailuresAllowed - 1; attempt++)
        {
            store.Accounts.SignIn("Mina", "not the pass phrase");
        }

        Assert.Equal(SignInOutcome.SignedIn, store.Accounts.SignIn("Mina", Password).Outcome);
        Assert.Equal(SignInOutcome.Refused, store.Accounts.SignIn("Mina", "not the pass phrase").Outcome);
        Assert.Equal(SignInOutcome.SignedIn, store.Accounts.SignIn("Mina", Password).Outcome);
    }

    [Fact]
    public void The_link_ends_after_a_day_and_the_mail_is_sent_again_on_request_but_not_at_once()
    {
        using var store = Open();
        store.Accounts.Register("Mina", "mina@example.org", Password, "en");
        var (id, first) = Link("mina@example.org");

        // Asked for again at once, nothing is sent: a form is not a way to fill somebody's mailbox.
        store.Accounts.SendConfirmationAgain("Mina");
        Assert.Single(Mail("mina@example.org"));

        _clock.Now += VisitorAccounts.SendAgainAfter;
        store.Accounts.SendConfirmationAgain("mina@example.org");
        store.Accounts.SendConfirmationAgain("nobody@example.org");
        Assert.Equal(2, Mail("mina@example.org").Count);
        var (_, second) = Link("mina@example.org");

        // The new link takes the place of the old one, and ends a day after it was sent.
        Assert.NotEqual(first, second);
        Assert.False(store.Accounts.Confirm(id, first).Done);
        _clock.Now += VisitorAccounts.ConfirmationValidFor + TimeSpan.FromSeconds(1);
        Assert.False(store.Accounts.Confirm(id, second).Done);

        store.Accounts.SendConfirmationAgain("Mina");
        var (_, third) = Link("mina@example.org");
        Assert.True(store.Accounts.Confirm(id, third).Done);

        // A confirmed account is sent nothing more.
        _clock.Now += TimeSpan.FromHours(1);
        store.Accounts.SendConfirmationAgain("Mina");
        Assert.Equal(3, Mail("mina@example.org").Count);
    }

    [Fact]
    public async Task Where_an_invitation_is_needed_no_account_is_made_without_one_and_one_invitation_makes_one_account()
    {
        string invitation;
        using (var store = Open())
        {
            store.Accounts.Register("Mina", "mina@example.org", Password, "en");
            var (id, token) = Link("mina@example.org");
            Assert.True(store.Accounts.Confirm(id, token).Done);
            invitation = store.Invite(id)!;

            Assert.Equal(["invitation-needed"], store.Accounts.Register("Sara", "sara@example.org", Password, "en", invitationNeeded: true));
            Assert.Equal(["invitation-needed"], store.Accounts.Register("Sara", "sara@example.org", Password, "en", "no-such-invitation", invitationNeeded: true));
            Assert.Empty(Mail("sara@example.org"));
        }

        // Twelve forms with the same invitation at the same moment: one account.
        using var start = new ManualResetEventSlim();
        var asking = Enumerable.Range(0, 12).Select(number => Task.Run(() =>
        {
            using var store = Open();
            start.Wait();
            return store.Accounts.Register($"Guest {number}", $"guest-{number}@example.org", Password, "en", invitation, invitationNeeded: true);
        })).ToArray();
        start.Set();
        var made = await Task.WhenAll(asking);

        Assert.Single(made, answer => answer.IsEmpty);
        Assert.Equal(1, Enumerable.Range(0, 12).Count(number => Mail($"guest-{number}@example.org").Count == 1));
    }

    [Fact]
    public void Of_many_who_ask_for_the_same_name_or_the_same_address_at_the_same_moment_one_gets_it()
    {
        List<System.Collections.Immutable.ImmutableArray<string>> AllAtOnce(Func<PeopleStore, int, System.Collections.Immutable.ImmutableArray<string>> act)
        {
            using var start = new ManualResetEventSlim();
            var asking = Enumerable.Range(0, 12).Select(number => Task.Run(() =>
            {
                using var store = Open();
                start.Wait();
                return act(store, number);
            })).ToArray();
            start.Set();
            Task.WaitAll(asking);
            return [.. asking.Select(task => task.Result)];
        }

        // The same name with twelve addresses: one account, and eleven are told the name is taken.
        var names = AllAtOnce((store, number) => store.Accounts.Register("Mina", $"mina-{number}@example.org", Password, "en"));
        Assert.Single(names, made => made.IsEmpty);
        Assert.Equal(11, names.Count(made => made is ["taken"]));
        Assert.Equal(1, Enumerable.Range(0, 12).Count(number => Mail($"mina-{number}@example.org").Count == 1));

        // The same address with twelve names: everybody gets the same answer, and one account exists.
        var addresses = AllAtOnce((store, number) => store.Accounts.Register($"Sara {number}", "sara@example.org", Password, "en"));
        Assert.All(addresses, made => Assert.Empty(made));
        Assert.Single(Mail("sara@example.org"), mail => mail.Kind == "confirm-address");
    }
}
