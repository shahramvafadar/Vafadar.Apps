
namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// The accounts of the editors (step 11.7): a name and a password of which only a derived value is kept; signing
/// in, too many wrong passwords, another password, a closed account.
/// </summary>
public sealed class EditorAccountsTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    private EditorialStore With(string name = "Mina")
    {
        var store = _copy.Open();
        Assert.True(store.AddEditor(name, Password).Done);
        return store;
    }

    [Fact]
    public void An_editor_signs_in_with_the_name_in_any_case_and_the_password()
    {
        using var store = With();

        var session = store.SignIn(" mina ", Password);

        Assert.Equal("Mina", session?.Name);
        Assert.True(store.Holds(session!));
        var editor = Assert.Single(store.Editors());
        Assert.Equal(("Mina", false, null), (editor.Name, editor.Closed, editor.LockedUntil));
    }

    [Theory]
    [InlineData("Mina", "a long pass phrasE")]
    [InlineData("Mina", "")]
    [InlineData("Nobody", Password)]
    public void A_wrong_name_or_password_signs_nobody_in(string name, string password)
    {
        using var store = With();

        Assert.Null(store.SignIn(name, password));
    }

    [Fact]
    public void The_password_is_not_in_the_file_of_the_store()
    {
        using (With())
        {
        }

        string kept;
        using (var connection = new Npgsql.NpgsqlConnection(_copy.Database))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """select "PasswordHash" from identity."EditorAccounts" """;
            kept = (string)command.ExecuteScalar()!;
        }

        // What Identity keeps of a password (its third form): a mark, how it was derived – PBKDF2 with
        // HMAC-SHA-512 and as many rounds as the store asks for – a salt of its own, and the derived value.
        var derived = Convert.FromBase64String(kept);
        Assert.Equal(0x01, derived[0]);
        Assert.Equal(2u, System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(derived.AsSpan(1)));
        Assert.Equal((uint)EditorialStore.PasswordRounds, System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(derived.AsSpan(5)));
        Assert.DoesNotContain(Password, kept, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_editors_with_the_same_password_are_kept_with_different_values()
    {
        using var store = With();
        Assert.True(store.AddEditor("Omid", Password).Done);

        Assert.NotEqual(store.SignIn("Mina", Password)!.Stamp, store.SignIn("Omid", Password)!.Stamp);
        Assert.Equal(["Mina", "Omid"], store.Editors().Select(editor => editor.Name));
    }

    [Theory]
    [InlineData("Mina", Password, "MINA: the store already knows an editor of that name.", "MINA")]
    [InlineData("Omid", "too short", "a password has at least 12 characters.", "Omid")]
    [InlineData(" ", Password, "an editor has a name of 1 to 200 characters in one line.", " ")]
    public void An_account_needs_a_name_of_its_own_and_a_password_of_some_length(string _, string password, string expected, string name)
    {
        using var store = With();

        var result = store.AddEditor(name, password);

        Assert.Equal([expected], result.Problems);
        Assert.Single(store.Editors());
    }

    [Fact]
    public void After_too_many_wrong_passwords_the_account_takes_none_for_a_while()
    {
        using var store = With();
        for (var attempt = 0; attempt < EditorialStore.FailuresAllowed; attempt++)
        {
            Assert.Null(store.SignIn("Mina", "not the password"));
        }

        Assert.Null(store.SignIn("Mina", Password));
        Assert.NotNull(Assert.Single(store.Editors()).LockedUntil);

        _copy.Clock.Advance(EditorialStore.LockedFor + TimeSpan.FromSeconds(1));
        Assert.NotNull(store.SignIn("Mina", Password));
        Assert.Null(Assert.Single(store.Editors()).LockedUntil);
    }

    [Fact]
    public void A_right_password_starts_the_count_of_wrong_ones_again()
    {
        using var store = With();
        for (var round = 0; round < 3; round++)
        {
            for (var attempt = 0; attempt < EditorialStore.FailuresAllowed - 1; attempt++)
            {
                Assert.Null(store.SignIn("Mina", "not the password"));
            }

            Assert.NotNull(store.SignIn("Mina", Password));
        }
    }

    [Fact]
    public void Another_password_signs_out_whoever_signed_in_with_the_old_one()
    {
        using var store = With();
        var before = store.SignIn("Mina", Password)!;

        Assert.True(store.SetPassword("mina", "another long phrase").Done);

        Assert.False(store.Holds(before));
        Assert.Null(store.SignIn("Mina", Password));
        Assert.True(store.Holds(store.SignIn("Mina", "another long phrase")!));
        Assert.Equal(["a password has at least 12 characters."], store.SetPassword("Mina", "short").Problems);
        Assert.Equal(["Omid: the store knows no editor of that name."], store.SetPassword("Omid", Password).Problems);
    }

    [Fact]
    public void A_closed_account_signs_in_nowhere_until_it_is_opened_again()
    {
        using var store = With();
        var before = store.SignIn("Mina", Password)!;

        Assert.True(store.CloseEditor("Mina").Done);

        Assert.False(store.Holds(before));
        Assert.Null(store.SignIn("Mina", Password));
        Assert.True(Assert.Single(store.Editors()).Closed);

        Assert.Equal(1, store.CloseEditor("Mina", closed: false).Changed);
        Assert.Equal(0, store.CloseEditor("Mina", closed: false).Changed);
        Assert.NotNull(store.SignIn("Mina", Password));
        Assert.False(store.Holds(before));
    }

    // --- the commands ---

    private (int Exit, string Output) Run(string[] typed, params string[] arguments)
    {
        using var output = new StringWriter();
        var answers = new Queue<string>(typed);
        return (Vafadar.Dastan.Content.ContentCommands.Run([.. arguments, "--store", _copy.Database], output, new DateOnly(2026, 11, 17), _copy.Clock, _ => answers.Count > 0 ? answers.Dequeue() : null), output.ToString());
    }

    [Fact]
    public void The_commands_make_an_account_list_it_change_its_password_and_close_it()
    {
        Assert.Equal((0, $"Mina: the account was made.{Environment.NewLine}"), Run([Password, Password], "store", "editor", "add", "Mina"));
        Assert.Equal((0, $"1 editor.{Environment.NewLine}  Mina  since 2026-11-17  writer,reviewer,publisher{Environment.NewLine}"), Run([], "store", "editors"));

        Assert.Equal(0, Run(["another long phrase", "another long phrase"], "store", "editor", "password", "Mina").Exit);
        using (var store = _copy.Open())
        {
            Assert.Null(store.SignIn("Mina", Password));
            Assert.NotNull(store.SignIn("Mina", "another long phrase"));
        }

        Assert.Equal((0, $"Mina: the account is closed.{Environment.NewLine}"), Run([], "store", "editor", "close", "Mina"));
        Assert.Contains("Mina  since 2026-11-17  writer,reviewer,publisher  closed", Run([], "store", "editors").Output, StringComparison.Ordinal);
        Assert.Equal((0, $"Nothing had to be changed.{Environment.NewLine}"), Run([], "store", "editor", "close", "Mina"));
        Assert.Equal((0, $"Mina: the account is open.{Environment.NewLine}"), Run([], "store", "editor", "open", "Mina"));
    }

    [Fact]
    public void A_password_is_typed_twice_alike_and_never_a_word_of_the_command()
    {
        var differing = Run([Password, "another long phrase"], "store", "editor", "add", "Mina");
        var weak = Run(["short", "short"], "store", "editor", "add", "Mina");
        var asWord = Run([], "store", "editor", "add", "Mina", "--password", Password);

        Assert.Equal((1, $"The two passwords differ. Nothing was changed.{Environment.NewLine}"), differing);
        Assert.Equal(1, weak.Exit);
        Assert.StartsWith("a password has at least 12 characters.", weak.Output, StringComparison.Ordinal);
        Assert.NotEqual(0, asWord.Exit);
        using var store = _copy.Open();
        Assert.Empty(store.Editors());
    }

    [Fact]
    public void Where_nobody_can_be_asked_no_account_is_made()
    {
        using var output = new StringWriter();

        var exit = Vafadar.Dastan.Content.ContentCommands.Run(["store", "editor", "add", "Mina", "--store", _copy.Database], output, new DateOnly(2026, 11, 17), _copy.Clock);

        Assert.Equal(1, exit);
        Assert.Contains("run it in a terminal", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_store_made_before_the_accounts_is_brought_to_the_form_with_them()
    {
        using (var store = _copy.Filled())
        {
            Assert.Empty(store.Editors());
        }

        using var again = _copy.Open();
        Assert.True(again.AddEditor("Mina", Password).Done);
        Assert.Contains(WorkingCopy.Collection, again.Collections);
    }
}
