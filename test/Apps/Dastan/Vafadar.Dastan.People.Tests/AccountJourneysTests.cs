using System.Text.RegularExpressions;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// What an account does after it is one (step 19.4, ADR 0016): another password, a forgotten one, another
/// address, signing out everywhere, recovery codes, closing and opening again. What changes how an account is
/// entered signs it out everywhere and is told to its address. Test data.
/// </summary>
public sealed partial class AccountJourneysTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Another = "another long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-journeys-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 19, 9, 0, 0, TimeSpan.Zero));
    private readonly string _database;
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly System.Collections.Immutable.ImmutableArray<string> _codes;

    public AccountJourneysTests()
    {
        _database = TestDatabase.For(Path.Combine(_folder, "people.db"));
        _store = PeopleStore.Open(_database, _clock, "https://dastan.test");
        Assert.Empty(_store.Accounts.Register("Mina", "mina@example.org", Password, "en"));
        var (id, token) = Link("mina@example.org", "confirm");
        _codes = _store.Accounts.Confirm(id, token).RecoveryCodes;
        _mina = id;
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private VisitorSession SignedIn(string password = Password) => _store.Accounts.SignIn("Mina", password).Session!;

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

    // The identifier and the token of the newest link of a page (confirm, reset, address) mailed to an address.
    private (string Id, string Token) Link(string to, string page)
    {
        var link = Mail(to).Select(mail => LinkForm().Match(mail.Text)).Last(match => match.Success && match.Groups[1].Value == page);
        return (link.Groups[2].Value, link.Groups[3].Value);
    }

    [GeneratedRegex(@"https://dastan\.test/(?:en|fa)/account/(confirm|reset|address)\?id=([0-9a-f]+)&token=([0-9a-f]+)")]
    private static partial Regex LinkForm();

    [Fact]
    public void An_account_knows_its_address_and_how_it_is_entered()
    {
        var info = _store.Accounts.Info(_mina)!;

        Assert.Equal(("Mina", "mina@example.org", true, PeopleStore.RecoveryCodeCount), (info.Name, info.Address, info.HasPassword, info.RecoveryCodesLeft));
        Assert.Empty(info.Providers);
        Assert.Null(_store.Accounts.Info("nobody"));
    }

    [Fact]
    public void Another_password_is_proved_by_the_one_before_signs_out_everywhere_and_is_told_to_the_address()
    {
        var before = SignedIn();

        Assert.Equal(["wrong"], _store.Accounts.ChangePassword(_mina, "not the pass phrase", Another).Problems);
        Assert.Equal(["password-short"], _store.Accounts.ChangePassword(_mina, Password, "short").Problems);
        Assert.True(_store.Holds(before));

        var changed = _store.Accounts.ChangePassword(_mina, Password, Another);
        Assert.True(changed.Done);
        Assert.True(_store.Holds(changed.Session!));
        Assert.False(_store.Holds(before));
        Assert.Equal(SignInOutcome.Refused, _store.Accounts.SignIn("Mina", Password).Outcome);
        Assert.Equal(SignInOutcome.SignedIn, _store.Accounts.SignIn("Mina", Another).Outcome);
        Assert.Single(Mail("mina@example.org"), mail => mail.Kind == "password-changed");
    }

    [Fact]
    public void A_forgotten_password_is_set_anew_through_a_link_that_is_mailed_used_once_and_ends_after_an_hour()
    {
        var before = SignedIn();

        // Who asks is told nothing; the mail goes to the address, also when the name was written.
        _store.Accounts.ForgotPassword("nobody@example.org");
        _store.Accounts.ForgotPassword("Mina");
        _store.Accounts.ForgotPassword("mina@example.org");
        Assert.Single(Mail("mina@example.org"), mail => mail.Kind == "reset-password");
        var (id, token) = Link("mina@example.org", "reset");

        Assert.Equal(["wrong"], _store.Accounts.ResetPassword(id, new string('0', 64), Another).Problems);
        Assert.Equal(["password-short"], _store.Accounts.ResetPassword(id, token, "short").Problems);
        var reset = _store.Accounts.ResetPassword(id, token, Another);
        Assert.True(reset.Done);
        Assert.False(_store.Holds(before));
        Assert.True(_store.Holds(reset.Session!));
        Assert.Equal(SignInOutcome.SignedIn, _store.Accounts.SignIn("Mina", Another).Outcome);
        Assert.Equal(["wrong"], _store.Accounts.ResetPassword(id, token, Password).Problems);
        Assert.Single(Mail("mina@example.org"), mail => mail.Kind == "password-changed");

        // A link that is older than an hour sets nothing.
        _clock.Now += VisitorAccounts.SendAgainAfter;
        _store.Accounts.ForgotPassword("Mina");
        var (_, late) = Link("mina@example.org", "reset");
        _clock.Now += VisitorAccounts.ResetValidFor + TimeSpan.FromSeconds(1);
        Assert.Equal(["wrong"], _store.Accounts.ResetPassword(id, late, Password).Problems);
    }

    [Fact]
    public void A_new_password_through_the_link_ends_the_pause_after_wrong_passwords()
    {
        for (var attempt = 0; attempt < PeopleStore.FailuresAllowed; attempt++)
        {
            _store.Accounts.SignIn("Mina", "not the pass phrase");
        }

        Assert.Equal(SignInOutcome.Refused, _store.Accounts.SignIn("Mina", Password).Outcome);
        _store.Accounts.ForgotPassword("Mina");
        var (id, token) = Link("mina@example.org", "reset");

        Assert.True(_store.Accounts.ResetPassword(id, token, Another).Done);
        Assert.Equal(SignInOutcome.SignedIn, _store.Accounts.SignIn("Mina", Another).Outcome);
    }

    [Fact]
    public void Another_address_is_the_address_only_once_the_link_mailed_there_is_opened_and_the_old_one_is_told()
    {
        var before = SignedIn();

        Assert.Equal(["wrong"], _store.Accounts.ChangeAddress(_mina, "not the pass phrase", "mina@elsewhere.example"));
        Assert.Equal(["mail"], _store.Accounts.ChangeAddress(_mina, Password, "elsewhere"));
        Assert.Equal(["same"], _store.Accounts.ChangeAddress(_mina, Password, "MINA@example.org"));
        Assert.Empty(_store.Accounts.ChangeAddress(_mina, Password, "mina@elsewhere.example"));

        // The link goes to the new address, the old one hears that it was asked for, and nothing has changed.
        Assert.Single(Mail("mina@elsewhere.example"), mail => mail.Kind == "confirm-new-address");
        Assert.Single(Mail("mina@example.org"), mail => mail.Kind == "address-change-asked");
        Assert.Equal("mina@example.org", _store.Accounts.Info(_mina)!.Address);
        Assert.Equal(SignInOutcome.SignedIn, _store.Accounts.SignIn("mina@example.org", Password).Outcome);

        var (id, token) = Link("mina@elsewhere.example", "address");
        Assert.Null(_store.Accounts.ConfirmNewAddress(id, new string('0', 64)));
        var session = _store.Accounts.ConfirmNewAddress(id, token);
        Assert.NotNull(session);
        Assert.Null(_store.Accounts.ConfirmNewAddress(id, token));
        Assert.Equal("mina@elsewhere.example", _store.Accounts.Info(_mina)!.Address);
        Assert.False(_store.Holds(before));
        Assert.True(_store.Holds(session));
        Assert.Equal(SignInOutcome.Refused, _store.Accounts.SignIn("mina@example.org", Password).Outcome);
        Assert.Equal(SignInOutcome.SignedIn, _store.Accounts.SignIn("mina@elsewhere.example", Password).Outcome);
        Assert.Single(Mail("mina@example.org"), mail => mail.Kind == "address-changed");
    }

    [Fact]
    public void An_address_another_account_has_is_asked_for_in_vain_and_only_who_has_it_learns_of_the_try()
    {
        Assert.Empty(_store.Accounts.Register("Sara", "sara@example.org", Password, "fa"));

        Assert.Empty(_store.Accounts.ChangeAddress(_mina, Password, "sara@example.org"));

        Assert.DoesNotContain(Mail("sara@example.org"), mail => mail.Kind == "confirm-new-address");
        Assert.Single(Mail("sara@example.org"), mail => mail.Kind == "address-in-use");
        Assert.DoesNotContain(Mail("mina@example.org"), mail => mail.Kind == "address-change-asked");
        Assert.Equal("mina@example.org", _store.Accounts.Info(_mina)!.Address);
    }

    [Fact]
    public void A_recovery_code_sets_a_new_password_once_and_wrong_codes_pause_the_account()
    {
        Assert.Equal(["wrong"], _store.Accounts.Recover("Mina", "AAAA-AAAA-AAAA-AAAA", Another).Problems);
        var recovered = _store.Accounts.Recover("mina@example.org", _codes[0].ToLowerInvariant(), Another);
        Assert.True(recovered.Done);
        Assert.True(_store.Holds(recovered.Session!));
        Assert.Equal(["wrong"], _store.Accounts.Recover("Mina", _codes[0], Password).Problems);
        Assert.Equal(PeopleStore.RecoveryCodeCount - 1, _store.Accounts.Info(_mina)!.RecoveryCodesLeft);

        // New codes take the place of the old ones.
        var fresh = _store.Accounts.NewRecoveryCodes(_mina, Another);
        Assert.Equal(PeopleStore.RecoveryCodeCount, fresh.RecoveryCodes.Length);
        Assert.Equal(["wrong"], _store.Accounts.NewRecoveryCodes(_mina, Password).Problems);
        Assert.Equal(["wrong"], _store.Accounts.Recover("Mina", _codes[1], Password).Problems);

        // Codes cannot be tried without end: after five wrong ones not even a right one is taken for a while.
        for (var attempt = 0; attempt < PeopleStore.FailuresAllowed; attempt++)
        {
            _store.Accounts.Recover("Mina", "AAAA-AAAA-AAAA-AAAA", Password);
        }

        Assert.Equal(["wrong"], _store.Accounts.Recover("Mina", fresh.RecoveryCodes[0], Password).Problems);
        _clock.Now += PeopleStore.LockedFor + TimeSpan.FromSeconds(1);
        Assert.True(_store.Accounts.Recover("Mina", fresh.RecoveryCodes[0], Password).Done);
    }

    [Fact]
    public void Signing_out_everywhere_ends_every_session_but_the_one_that_asked()
    {
        var phone = SignedIn();
        var desk = SignedIn();

        var kept = _store.Accounts.SignOutEverywhere(_mina);

        Assert.False(_store.Holds(phone));
        Assert.False(_store.Holds(desk));
        Assert.True(_store.Holds(kept!));
        Assert.Null(_store.Accounts.SignOutEverywhere("nobody"));
    }

    [Fact]
    public void A_closed_account_is_signed_out_can_be_opened_again_for_a_while_and_then_is_gone_with_its_address()
    {
        var before = SignedIn();
        Assert.Equal(["wrong"], _store.Accounts.Close(_mina, "not the pass phrase").Problems);
        Assert.True(_store.Accounts.Close(_mina, Password).Done);

        Assert.False(_store.Holds(before));
        Assert.Equal(SignInOutcome.Refused, _store.Accounts.SignIn("Mina", Password).Outcome);
        Assert.Null(_store.Accounts.Info(_mina));
        Assert.Single(Mail("mina@example.org"), mail => mail.Kind == "account-closed");

        // Thought over: opened again with the name and the password.
        Assert.Null(_store.Accounts.Reopen("Mina", "not the pass phrase"));
        var back = _store.Accounts.Reopen("mina@example.org", Password);
        Assert.True(_store.Holds(back!));
        Assert.Equal(SignInOutcome.SignedIn, _store.Accounts.SignIn("Mina", Password).Outcome);

        // Closed and left closed: after the time it is kept, the account, its address and its name are free.
        Assert.True(_store.Accounts.Close(_mina, Password).Done);
        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromDays(1);
        Assert.Equal(1, _store.Purge());
        Assert.Null(_store.Accounts.Reopen("Mina", Password));
        Assert.Null(_store.Accounts.Address(_mina));
        Assert.Empty(_store.Accounts.Register("Mina", "mina@example.org", Password, "en"));
        Assert.Equal(2, Mail("mina@example.org").Count(mail => mail.Kind == "confirm-address"));
    }

    [Fact]
    public void A_name_and_a_password_prove_an_account_also_a_closed_one_and_wrong_ones_are_counted()
    {
        Assert.Equal(_mina, _store.Accounts.Proves("Mina", Password));
        Assert.Null(_store.Accounts.Proves("Mina", "not the pass phrase"));
        Assert.Null(_store.Accounts.Proves("Nobody", Password));
        Assert.True(_store.Accounts.Close(_mina, Password).Done);
        Assert.Equal(_mina, _store.Accounts.Proves("mina@example.org", Password));

        for (var attempt = 0; attempt < PeopleStore.FailuresAllowed; attempt++)
        {
            _store.Accounts.Proves("Mina", "not the pass phrase");
        }

        Assert.Null(_store.Accounts.Proves("Mina", Password));
    }
}
