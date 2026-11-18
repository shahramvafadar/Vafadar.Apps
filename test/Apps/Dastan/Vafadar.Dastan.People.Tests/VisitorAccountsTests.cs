using Microsoft.Data.Sqlite;

namespace Vafadar.Dastan.People.Tests;

/// <summary>A clock a test moves.</summary>
internal sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>
/// The accounts of the visitors (step 16.1, D-048 to D-050 and D-055): anybody makes one with a name and a
/// password; signing in, too many wrong passwords, recovery codes, another password, closing and what is deleted.
/// </summary>
public sealed class VisitorAccountsTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));

    private string File => Path.Combine(_folder, "people.db");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private PeopleStore Open() => PeopleStore.Open(File, _clock);

    [Fact]
    public void Anybody_makes_an_account_and_is_signed_in_with_it()
    {
        using var store = Open();

        var made = store.Register("  Mina Rad ", Password);

        Assert.True(made.Done);
        Assert.Equal("Mina Rad", made.Session?.Name);
        Assert.True(store.Holds(made.Session!));
        Assert.Equal(PeopleStore.RecoveryCodeCount, made.RecoveryCodes.Length);
        var info = store.Info(made.Session!.Id);
        Assert.Equal(("Mina Rad", PeopleStore.RecoveryCodeCount, _clock.Now.UtcDateTime), (info?.Name, info?.RecoveryCodesLeft, info?.CreatedAt));
    }

    [Fact]
    public void A_name_in_another_script_makes_an_account_too()
    {
        using var store = Open();

        Assert.True(store.Register("مینا راد", Password).Done);
        Assert.Equal("مینا راد", store.SignIn("مینا راد", Password)?.Name);
    }

    [Theory]
    [InlineData("ab", Password, "name-length")]
    [InlineData("a name that is far too long to be the name of an account", Password, "name-length")]
    [InlineData("mina@example.org", Password, "name-letters")]
    [InlineData("mina  rad", Password, "name-letters")]
    [InlineData("<b>mina</b>", Password, "name-letters")]
    [InlineData("Mina", "short", "password-short")]
    public void An_account_needs_a_plain_name_and_a_long_password(string name, string password, string problem)
    {
        using var store = Open();

        var made = store.Register(name, password);

        Assert.Equal([problem], made.Problems);
        Assert.Null(made.Session);
    }

    [Fact]
    public void A_name_belongs_to_one_account_whatever_its_case()
    {
        using var store = Open();
        Assert.True(store.Register("Mina", Password).Done);

        Assert.Equal(["taken"], store.Register("MINA", "another long pass phrase").Problems);
    }

    [Fact]
    public void The_store_keeps_neither_the_password_nor_a_recovery_code_as_it_is()
    {
        string code;
        using (var store = Open())
        {
            code = store.Register("Mina", Password).RecoveryCodes[0];
        }

        using var connection = new SqliteConnection($"Data Source={File};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "select Password from Visitors union all select Code from RecoveryCodes";
        using var reader = command.ExecuteReader();
        var kept = new List<string>();
        while (reader.Read())
        {
            kept.Add(reader.GetString(0));
        }

        Assert.Equal(1 + PeopleStore.RecoveryCodeCount, kept.Count);
        Assert.StartsWith("pbkdf2-sha256$", kept[0], StringComparison.Ordinal);
        Assert.DoesNotContain(kept, text => text.Contains(Password, StringComparison.Ordinal) || text.Contains(code, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void A_visitor_signs_in_with_the_name_in_any_case_and_the_password()
    {
        using var store = Open();
        store.Register("Mina", Password);

        var session = store.SignIn(" mina ", Password);

        Assert.Equal("Mina", session?.Name);
        Assert.True(store.Holds(session!));
    }

    [Theory]
    [InlineData("Mina", "a long pass phrasE")]
    [InlineData("Mina", "")]
    [InlineData("Nobody", Password)]
    public void A_wrong_name_or_password_signs_nobody_in(string name, string password)
    {
        using var store = Open();
        store.Register("Mina", Password);

        Assert.Null(store.SignIn(name, password));
    }

    [Fact]
    public void After_too_many_wrong_passwords_the_account_takes_none_for_a_while()
    {
        using var store = Open();
        store.Register("Mina", Password);

        for (var attempt = 0; attempt < PeopleStore.FailuresAllowed; attempt++)
        {
            Assert.Null(store.SignIn("Mina", "a wrong pass phrase"));
        }

        Assert.Null(store.SignIn("Mina", Password));

        _clock.Now += PeopleStore.LockedFor + TimeSpan.FromSeconds(1);
        Assert.NotNull(store.SignIn("Mina", Password));
    }

    [Fact]
    public void A_right_password_forgets_the_wrong_ones_before_it()
    {
        using var store = Open();
        store.Register("Mina", Password);

        for (var round = 0; round < 3; round++)
        {
            for (var attempt = 0; attempt < PeopleStore.FailuresAllowed - 1; attempt++)
            {
                Assert.Null(store.SignIn("Mina", "a wrong pass phrase"));
            }

            Assert.NotNull(store.SignIn("Mina", Password));
        }
    }

    [Fact]
    public void Another_password_ends_every_sign_in_made_before()
    {
        using var store = Open();
        var first = store.Register("Mina", Password).Session!;
        var second = store.SignIn("Mina", Password)!;

        var changed = store.ChangePassword(first.Id, Password, "another long pass phrase");

        Assert.True(changed.Done);
        Assert.False(store.Holds(first));
        Assert.False(store.Holds(second));
        Assert.True(store.Holds(changed.Session!));
        Assert.Null(store.SignIn("Mina", Password));
        Assert.NotNull(store.SignIn("Mina", "another long pass phrase"));
    }

    [Theory]
    [InlineData("a wrong pass phrase", "another long pass phrase", "wrong")]
    [InlineData(Password, "short", "password-short")]
    public void Another_password_needs_the_present_one_and_a_long_new_one(string current, string password, string problem)
    {
        using var store = Open();
        var session = store.Register("Mina", Password).Session!;

        Assert.Equal([problem], store.ChangePassword(session.Id, current, password).Problems);
        Assert.True(store.Holds(session));
    }

    [Fact]
    public void A_recovery_code_gets_into_the_account_once()
    {
        using var store = Open();
        var made = store.Register("Mina", Password);
        var code = made.RecoveryCodes[2];

        // Typed by hand: small letters, spaces for the hyphens.
        var recovered = store.Recover("mina", code.ToLowerInvariant().Replace('-', ' '), "another long pass phrase");

        Assert.True(recovered.Done);
        Assert.False(store.Holds(made.Session!));
        Assert.True(store.Holds(recovered.Session!));
        Assert.NotNull(store.SignIn("Mina", "another long pass phrase"));
        Assert.Equal(PeopleStore.RecoveryCodeCount - 1, store.Info(made.Session!.Id)?.RecoveryCodesLeft);
        Assert.Equal(["wrong"], store.Recover("Mina", code, "a third long pass phrase").Problems);
    }

    [Fact]
    public void A_recovery_code_of_another_account_gets_into_none()
    {
        using var store = Open();
        store.Register("Mina", Password);
        var other = store.Register("Sara", Password).RecoveryCodes[0];

        Assert.Equal(["wrong"], store.Recover("Mina", other, "another long pass phrase").Problems);
        Assert.Equal(["wrong"], store.Recover("Nobody", other, "another long pass phrase").Problems);
    }

    [Fact]
    public void Recovery_codes_cannot_be_tried_without_end()
    {
        using var store = Open();
        var code = store.Register("Mina", Password).RecoveryCodes[0];

        for (var attempt = 0; attempt < PeopleStore.FailuresAllowed; attempt++)
        {
            Assert.False(store.Recover("Mina", "AAAA-AAAA-AAAA-AAAA", "another long pass phrase").Done);
        }

        Assert.Equal(["wrong"], store.Recover("Mina", code, "another long pass phrase").Problems);
        Assert.Null(store.SignIn("Mina", Password));

        _clock.Now += PeopleStore.LockedFor + TimeSpan.FromSeconds(1);
        Assert.True(store.Recover("Mina", code, "another long pass phrase").Done);
    }

    [Fact]
    public void New_recovery_codes_take_the_place_of_the_ones_before()
    {
        using var store = Open();
        var made = store.Register("Mina", Password);

        Assert.Equal(["wrong"], store.NewRecoveryCodes(made.Session!.Id, "a wrong pass phrase").Problems);
        var renewed = store.NewRecoveryCodes(made.Session.Id, Password);

        Assert.Equal(PeopleStore.RecoveryCodeCount, renewed.RecoveryCodes.Length);
        Assert.Empty(renewed.RecoveryCodes.Intersect(made.RecoveryCodes));
        Assert.Equal(PeopleStore.RecoveryCodeCount, store.Info(made.Session.Id)?.RecoveryCodesLeft);
        Assert.False(store.Recover("Mina", made.RecoveryCodes[0], "another long pass phrase").Done);
        Assert.True(store.Recover("Mina", renewed.RecoveryCodes[0], "another long pass phrase").Done);
    }

    [Fact]
    public void Recovery_codes_are_written_to_be_copied_by_hand()
    {
        using var store = Open();

        var codes = store.Register("Mina", Password).RecoveryCodes;

        Assert.All(codes, code => Assert.Matches("^[A-HJ-KM-NP-Z2-9]{4}(-[A-HJ-KM-NP-Z2-9]{4}){3}$", code));
        Assert.Equal(codes.Length, codes.Distinct().Count());
    }

    [Fact]
    public void A_closed_account_signs_nobody_in_and_ends_every_sign_in()
    {
        using var store = Open();
        var made = store.Register("Mina", Password);

        Assert.Equal(["wrong"], store.Close(made.Session!.Id, "a wrong pass phrase").Problems);
        Assert.True(store.Holds(made.Session));
        Assert.True(store.Close(made.Session.Id, Password).Done);

        Assert.False(store.Holds(made.Session));
        Assert.Null(store.SignIn("Mina", Password));
        Assert.Null(store.Info(made.Session.Id));
        Assert.False(store.Recover("Mina", made.RecoveryCodes[0], "another long pass phrase").Done);
        Assert.Equal(["taken"], store.Register("Mina", Password).Problems);
    }

    [Fact]
    public void A_closed_account_is_opened_again_while_it_is_kept()
    {
        using var store = Open();
        var made = store.Register("Mina", Password);
        store.Close(made.Session!.Id, Password);
        _clock.Now += PeopleStore.KeptAfterClosing - TimeSpan.FromHours(1);

        Assert.Null(store.Reopen("Mina", "a wrong pass phrase"));
        Assert.Equal(0, store.Purge());
        var again = store.Reopen("Mina", Password);

        Assert.Equal(made.Session.Id, again?.Id);
        Assert.True(store.Holds(again!));
        Assert.NotNull(store.SignIn("Mina", Password));
    }

    [Fact]
    public void An_open_account_is_not_opened_again()
    {
        using var store = Open();
        var made = store.Register("Mina", Password);

        Assert.Null(store.Reopen("Mina", Password));
        Assert.True(store.Holds(made.Session!));
    }

    [Fact]
    public void A_closed_account_is_deleted_for_good_after_thirty_days()
    {
        using var store = Open();
        var gone = store.Register("Mina", Password);
        var stays = store.Register("Sara", Password);
        store.Close(gone.Session!.Id, Password);
        _clock.Now += TimeSpan.FromDays(30) + TimeSpan.FromMinutes(1);

        Assert.Equal(1, store.Purge());
        Assert.Equal(0, store.Purge());

        Assert.Null(store.Reopen("Mina", Password));
        Assert.True(store.Holds(stays.Session!));
        Assert.Equal(PeopleStore.RecoveryCodeCount, Count("RecoveryCodes"));
        Assert.Equal(1, Count("Visitors"));

        // The name is free again, and it is another account.
        var another = store.Register("Mina", Password);
        Assert.True(another.Done);
        Assert.NotEqual(gone.Session.Id, another.Session!.Id);
    }

    [Fact]
    public void What_the_store_holds_is_there_when_it_is_opened_again()
    {
        VisitorSession session;
        using (var store = Open())
        {
            session = store.Register("Mina", Password).Session!;
        }

        using var again = Open();

        Assert.True(again.Holds(session));
        Assert.NotNull(again.SignIn("Mina", Password));
    }

    private long Count(string table)
    {
        using var connection = new SqliteConnection($"Data Source={File};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"select count(*) from {table}";
        return (long)command.ExecuteScalar()!;
    }
}
