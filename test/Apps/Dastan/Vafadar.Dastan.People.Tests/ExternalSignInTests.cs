using Microsoft.Data.Sqlite;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// Signing in through a provider (step 16.8, D-048): the provider says who it is, the visitor chooses the name,
/// and nothing the provider knows is kept – not even its identifier of the person.
/// </summary>
public sealed class ExternalSignInTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Subject = "1098765432109876543210";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;

    public ExternalSignInTests() => _store = PeopleStore.Open(File, _clock);

    private string File => Path.Combine(_folder, "people.db");

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Somebody_new_chooses_a_name_and_is_found_again_by_the_same_sign_in()
    {
        Assert.Null(_store.SignInWith("google", Subject));

        var made = _store.RegisterWith(" Mina ", "google", Subject);

        Assert.True(made.Done);
        Assert.Equal("Mina", made.Session?.Name);
        Assert.Equal(PeopleStore.RecoveryCodeCount, made.RecoveryCodes.Length);
        var again = _store.SignInWith("google", Subject);
        Assert.Equal(made.Session!.Id, again?.Id);
        Assert.True(_store.Holds(again!));

        // The same identifier at another provider is somebody else; so is another identifier.
        Assert.Null(_store.SignInWith("microsoft", Subject));
        Assert.Null(_store.SignInWith("google", Subject + "1"));
        var info = _store.Info(made.Session.Id)!;
        Assert.False(info.HasPassword);
        Assert.Equal(["google"], info.Providers);
    }

    [Theory]
    [InlineData("ab", "name-length")]
    [InlineData("mina@example.org", "name-letters")]
    [InlineData("Sara", "taken")]
    public void An_account_through_a_provider_needs_a_plain_name_nobody_has(string name, string problem)
    {
        _store.Register("Sara", Password);

        Assert.Equal([problem], _store.RegisterWith(name, "google", Subject).Problems);
        Assert.Null(_store.SignInWith("google", Subject));
    }

    [Fact]
    public void A_sign_in_of_a_provider_makes_one_account_and_no_second()
    {
        _store.RegisterWith("Mina", "google", Subject);

        Assert.Equal(["login-taken"], _store.RegisterWith("Another", "google", Subject).Problems);
        Assert.True(_store.RegisterWith("Another", "microsoft", Subject).Done);
    }

    [Fact]
    public void An_account_without_a_password_takes_none_until_its_visitor_sets_one()
    {
        var mina = _store.RegisterWith("Mina", "google", Subject).Session!;

        Assert.Null(_store.SignIn("Mina", string.Empty));
        Assert.Null(_store.SignIn("Mina", Password));
        Assert.Equal(["wrong"], _store.ChangePassword(mina.Id, string.Empty, Password).Problems);
        Assert.Equal(["wrong"], _store.Close(mina.Id, string.Empty).Problems);
        Assert.Null(_store.Export(mina.Id, string.Empty));

        Assert.Equal(["password-short"], _store.SetPassword(mina.Id, "short").Problems);
        var set = _store.SetPassword(mina.Id, Password);

        Assert.True(set.Done);
        Assert.False(_store.Holds(mina));
        Assert.True(_store.Holds(set.Session!));
        Assert.NotNull(_store.SignIn("Mina", Password));
        Assert.True(_store.Info(mina.Id)!.HasPassword);

        // Setting is for an account without a password only: with one, the present password is asked for.
        Assert.Equal(["wrong"], _store.SetPassword(mina.Id, "another long pass phrase").Problems);
        Assert.NotNull(_store.SignInWith("google", Subject));
    }

    [Fact]
    public void An_account_with_a_password_adds_a_provider_and_takes_it_away_again()
    {
        var mina = _store.Register("Mina", Password).Session!;
        var sara = _store.Register("Sara", Password).Session!;

        Assert.True(_store.Link(mina.Id, "google", Subject));
        Assert.Equal(mina.Id, _store.SignInWith("google", Subject)?.Id);

        // A sign-in belongs to one account, and an account has one sign-in of a provider.
        Assert.False(_store.Link(sara.Id, "google", Subject));
        Assert.False(_store.Link(mina.Id, "google", "another-identifier"));
        Assert.True(_store.Link(mina.Id, "microsoft", "another-identifier"));
        Assert.Equal(["google", "microsoft"], _store.Info(mina.Id)!.Providers);

        Assert.True(_store.Unlink(mina.Id, "google"));
        Assert.False(_store.Unlink(mina.Id, "google"));
        Assert.Null(_store.SignInWith("google", Subject));
        Assert.True(_store.Link(sara.Id, "google", Subject));
    }

    [Fact]
    public void The_only_way_into_an_account_is_not_taken_away()
    {
        var mina = _store.RegisterWith("Mina", "google", Subject).Session!;

        Assert.False(_store.Unlink(mina.Id, "google"));

        _store.Link(mina.Id, "microsoft", "another-identifier");
        Assert.True(_store.Unlink(mina.Id, "google"));
        Assert.False(_store.Unlink(mina.Id, "microsoft"));

        _store.SetPassword(mina.Id, Password);
        Assert.True(_store.Unlink(mina.Id, "microsoft"));
        Assert.Empty(_store.Info(mina.Id)!.Providers);
    }

    [Fact]
    public void A_closed_account_is_signed_in_through_no_provider_and_a_deleted_one_frees_the_sign_in()
    {
        var mina = _store.RegisterWith("Mina", "google", Subject).Session!;
        _store.SetPassword(mina.Id, Password);
        _store.Close(mina.Id, Password);

        Assert.Null(_store.SignInWith("google", Subject));
        Assert.Equal(["login-taken"], _store.RegisterWith("Another", "google", Subject).Problems);

        _clock.Now += PeopleStore.KeptAfterClosing + TimeSpan.FromMinutes(1);
        _store.Purge();
        Assert.True(_store.RegisterWith("Another", "google", Subject).Done);
    }

    [Fact]
    public void An_invitation_is_used_up_by_an_account_made_through_a_provider_too()
    {
        var mina = _store.Register("Mina", Password).Session!;
        var code = _store.Invite(mina.Id)!;

        Assert.True(_store.RegisterWith("Sara", "google", Subject, code).Done);

        Assert.False(_store.IsInvited(code));
    }

    [Fact]
    public void The_store_keeps_neither_the_identifier_of_the_provider_nor_anything_else_of_it()
    {
        var mina = _store.RegisterWith("Mina", "google", Subject).Session!;

        var kept = new List<string>();
        using (var connection = new SqliteConnection($"Data Source={File};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "select Provider || ' ' || Key || ' ' || VisitorId from ExternalLogins union all select Password from Visitors";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                kept.Add(reader.GetString(0));
            }
        }

        Assert.Equal(2, kept.Count);
        Assert.Matches($"^google [0-9A-F]{{64}} {mina.Id}$", kept[0]);
        Assert.DoesNotContain(kept, text => text.Contains(Subject, StringComparison.Ordinal));
        Assert.Equal(string.Empty, kept[1]);
        Assert.Equal("""["google"]""", _store.SetPassword(mina.Id, Password).Done ? _store.Export(mina.Id, Password)!["account"]!["signInThrough"]!.ToJsonString() : null);
    }
}
