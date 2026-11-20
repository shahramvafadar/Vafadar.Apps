using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// Accounts and the providers they can be signed in through (step 19.4, ADR 0016): a first sign-in makes an
/// account with the address the provider confirmed; an address that has an account makes no second one;
/// linking is an act of the signed-in visitor; and an account never loses its last way in. Test data.
/// </summary>
public sealed class AccountProvidersTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-providers-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 19, 9, 0, 0, TimeSpan.Zero));
    private readonly string _database;
    private readonly PeopleStore _store;

    public AccountProvidersTests()
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

    private long Count(string sql)
    {
        using var connection = new Npgsql.NpgsqlConnection(_database);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)command.ExecuteScalar()!;
    }

    // Confirms the address of an account with the link that was mailed to it.
    private bool Confirm(string address)
    {
        using var connection = new Npgsql.NpgsqlConnection(_database);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """select "Text" from delivery."Mail" where "To" = @to and "Kind" = 'confirm-address' order by "CreatedAt" desc limit 1""";
        command.Parameters.AddWithValue("to", address);
        var link = System.Text.RegularExpressions.Regex.Match((string)command.ExecuteScalar()!, @"confirm\?id=([0-9a-f]+)&token=([0-9a-f]+)");
        return link.Success && _store.Accounts.Confirm(link.Groups[1].Value, link.Groups[2].Value).Done;
    }

    [Fact]
    public void A_first_sign_in_through_a_provider_makes_an_account_with_the_address_the_provider_confirmed()
    {
        Assert.Null(_store.Accounts.SignInWith("google", "subject-1"));

        var made = _store.Accounts.RegisterWith("Mina", "google", "subject-1", "mina@example.org", "fa", terms: "2026-11-19", proven: true);

        Assert.True(made.Done);
        Assert.Equal(PeopleStore.RecoveryCodeCount, made.RecoveryCodes.Length);
        Assert.True(_store.Holds(made.Session!));
        var info = _store.Accounts.Info(made.Session!.Id)!;
        Assert.Equal(("Mina", "mina@example.org", false), (info.Name, info.Address, info.HasPassword));
        Assert.Equal(["google"], info.Providers);
        Assert.Equal(("mina@example.org", true), _store.Accounts.Address(info.Id));
        Assert.Equal(made.Session, _store.Accounts.SignInWith("google", "subject-1"));

        // It has no password, so no password signs it in – and no mail was needed to confirm what the provider confirmed.
        Assert.Equal(SignInOutcome.Refused, _store.Accounts.SignIn("Mina", Password).Outcome);
        Assert.Equal(0L, Count("""select count(*) from delivery."Mail" """));

        // What the provider said about who this is, is not kept as it was said.
        Assert.Equal(0L, Count("""select count(*) from identity."VisitorLogins" where "ProviderKey" like '%subject-1%'"""));
    }

    [Fact]
    public void What_cannot_be_such_an_account_is_said()
    {
        Assert.True(_store.Accounts.RegisterWith("Mina", "google", "subject-1", "mina@example.org", "en", proven: true).Done);

        Assert.Equal(["login-taken"], _store.Accounts.RegisterWith("Sara", "google", "subject-1", "sara@example.org", "en", proven: true).Problems);
        Assert.Equal(["address-has-account"], _store.Accounts.RegisterWith("Sara", "microsoft", "subject-9", "MINA@example.org", "en", proven: true).Problems);
        Assert.Equal(["taken"], _store.Accounts.RegisterWith("mina", "google", "subject-2", "sara@example.org", "en", proven: true).Problems);
        Assert.Equal(["name-length"], _store.Accounts.RegisterWith("Sa", "google", "subject-2", "sara@example.org", "en", proven: true).Problems);
        Assert.Equal(["mail"], _store.Accounts.RegisterWith("Sara", "google", "subject-2", "nothing", "en", proven: true).Problems);
        Assert.Equal(["invitation-needed"], _store.Accounts.RegisterWith("Sara", "google", "subject-2", "sara@example.org", "en", invitationNeeded: true, proven: true).Problems);
        Assert.Equal(1L, Count("""select count(*) from identity."VisitorAccounts" """));
    }

    [Fact]
    public void An_address_that_has_an_account_is_reached_by_signing_in_to_it_and_linking_the_provider_there()
    {
        Assert.Empty(_store.Accounts.Register("Mina", "mina@example.org", Password, "en"));
        Assert.True(Confirm("mina@example.org"));
        var account = _store.Accounts.Proves("Mina", Password)!;

        // The provider confirms the same address: that signs nobody in and makes nothing.
        Assert.Null(_store.Accounts.SignInWith("google", "subject-1"));
        Assert.Equal(["address-has-account"], _store.Accounts.RegisterWith("Mina Two", "google", "subject-1", "mina@example.org", "en", proven: true).Problems);

        // Signed in to the account, its visitor links the provider; from then on it signs in.
        Assert.True(_store.Accounts.Link(account, "google", "subject-1"));
        Assert.False(_store.Accounts.Link(account, "google", "subject-2"));
        Assert.False(_store.Accounts.Link("nobody", "microsoft", "subject-3"));
        Assert.Equal(account, _store.Accounts.SignInWith("google", "subject-1")!.Id);

        // A sign-in belongs to one account.
        Assert.True(_store.Accounts.RegisterWith("Sara", "microsoft", "subject-7", "sara@example.org", "en", proven: true).Done);
        Assert.False(_store.Accounts.Link(account, "microsoft", "subject-7"));
    }

    [Fact]
    public void An_account_never_loses_its_last_way_in()
    {
        var made = _store.Accounts.RegisterWith("Mina", "google", "subject-1", "mina@example.org", "en", proven: true);
        var id = made.Session!.Id;

        // Its only provider stays while it has no password.
        Assert.False(_store.Accounts.Unlink(id, "google"));
        Assert.False(_store.Accounts.Unlink(id, "microsoft"));

        // With a second provider, one can go.
        Assert.True(_store.Accounts.Link(id, "microsoft", "subject-2"));
        Assert.True(_store.Accounts.Unlink(id, "google"));
        Assert.False(_store.Accounts.Unlink(id, "microsoft"));

        // With a password, the last provider can go too – and the password is set once, then changed.
        Assert.Equal(["password-short"], _store.Accounts.SetPassword(id, "short").Problems);
        var set = _store.Accounts.SetPassword(id, Password);
        Assert.True(set.Done);
        Assert.False(_store.Holds(made.Session));
        Assert.True(_store.Holds(set.Session!));
        Assert.Equal(["wrong"], _store.Accounts.SetPassword(id, "another long pass phrase").Problems);
        Assert.True(_store.Accounts.Unlink(id, "microsoft"));
        Assert.Equal(SignInOutcome.SignedIn, _store.Accounts.SignIn("mina@example.org", Password).Outcome);
        Assert.Null(_store.Accounts.SignInWith("microsoft", "subject-2"));
    }

    [Fact]
    public async Task Of_many_first_sign_ins_of_the_same_person_at_the_same_moment_one_makes_the_account()
    {
        using var start = new ManualResetEventSlim();
        var asking = Enumerable.Range(0, 12).Select(number => Task.Run(() =>
        {
            using var store = PeopleStore.Open(_database, _clock, "https://dastan.test");
            start.Wait();
            return store.Accounts.RegisterWith($"Mina {number}", "google", "subject-1", "mina@example.org", "en", proven: true);
        })).ToArray();
        start.Set();
        var made = await Task.WhenAll(asking);

        Assert.Single(made, result => result.Done);
        Assert.Equal(1L, Count("""select count(*) from identity."VisitorAccounts" """));
        Assert.Equal(1L, Count("""select count(*) from identity."VisitorLogins" """));
        Assert.Equal(1L, Count("""select count(*) from personal."Visitors" """));
    }
}
