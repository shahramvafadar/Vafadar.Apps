namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// What an editor may do, another password proved by the present one, and when a password was tried
/// (steps 13.3 and 13.4).
/// </summary>
public sealed class EditorRolesTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string Another = "another long phrase";

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    private (int Exit, string Output) Run(string[] typed, params string[] arguments)
    {
        using var output = new StringWriter();
        var answers = new Queue<string>(typed);
        return (Vafadar.Dastan.Content.ContentCommands.Run([.. arguments, "--store", _copy.Database], output, new DateOnly(2026, 11, 17), _copy.Clock, _ => answers.Count > 0 ? answers.Dequeue() : null), output.ToString());
    }

    [Fact]
    public void An_account_may_do_everything_unless_it_is_made_with_roles_and_its_roles_can_be_changed()
    {
        using var store = _copy.Open();
        store.AddEditor("Mina", Password);
        store.AddEditor("Omid", Password, EditorRoles.Reviewer);

        var before = (store.RolesOf("Mina"), store.RolesOf("omid"), store.RolesOf("Nobody"));
        var changed = store.SetRoles("Mina", EditorRoles.Writer | EditorRoles.Publisher);
        var same = store.SetRoles("Mina", EditorRoles.Writer | EditorRoles.Publisher);
        var unknown = store.SetRoles("Nobody", EditorRoles.All);
        store.CloseEditor("Omid");

        Assert.Equal((EditorRoles.All, EditorRoles.Reviewer, EditorRoles.None), before);
        Assert.Equal((1, 0), (changed.Changed, same.Changed));
        Assert.Equal(EditorRoles.Writer | EditorRoles.Publisher, store.RolesOf("Mina"));
        Assert.Equal(EditorRoles.Writer | EditorRoles.Publisher, store.Editors().Single(editor => editor.Name == "Mina").Roles);
        Assert.False(unknown.Done);

        // A closed account may do nothing.
        Assert.Equal(EditorRoles.None, store.RolesOf("Omid"));
    }

    [Theory]
    [InlineData("writer", EditorRoles.Writer)]
    [InlineData("Reviewer, publisher", EditorRoles.Reviewer | EditorRoles.Publisher)]
    [InlineData("all", EditorRoles.All)]
    [InlineData("", EditorRoles.None)]
    public void Roles_are_written_as_their_names_with_commas_between_them(string written, EditorRoles roles)
    {
        Assert.Equal(roles, EditorialStore.ReadRoles(written));
        Assert.Equal(roles, EditorialStore.ReadRoles(EditorialStore.RolesText(roles)));
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("writer;reviewer")]
    [InlineData("7")]
    [InlineData("None")]
    public void What_is_no_role_is_not_read_as_one(string written)
    {
        Assert.Null(EditorialStore.ReadRoles(written));
    }

    [Fact]
    public void An_editor_changes_the_own_password_by_proving_the_present_one()
    {
        using var store = _copy.Open();
        store.AddEditor("Mina", Password);
        var session = store.SignIn("Mina", Password)!;

        var wrong = store.ChangePassword("Mina", "not the password", Another);
        var same = store.ChangePassword("Mina", Password, Password);
        var weak = store.ChangePassword("Mina", Password, "short");
        var held = store.Holds(session);
        var changed = store.ChangePassword("Mina", Password, Another);

        Assert.Equal(["the present password is not right."], wrong.Problems);
        Assert.Equal(["the new password is the present one."], same.Problems);
        Assert.False(weak.Done);
        Assert.True(held);
        Assert.True(changed.Done);

        // Whoever signed in with the old password is signed in no longer; the new one signs in.
        Assert.False(store.Holds(session));
        Assert.Null(store.SignIn("Mina", Password));
        Assert.NotNull(store.SignIn("Mina", Another));
    }

    [Fact]
    public void Wrong_present_passwords_count_like_wrong_ones_at_a_sign_in()
    {
        using var store = _copy.Open();
        store.AddEditor("Mina", Password);
        for (var attempt = 0; attempt < EditorialStore.FailuresAllowed; attempt++)
        {
            store.ChangePassword("Mina", "not the password", Another);
        }

        Assert.Null(store.SignIn("Mina", Password));
        Assert.NotNull(store.Editors().Single().LockedUntil);
    }

    [Fact]
    public void The_store_remembers_when_the_password_of_an_account_was_tried_and_whether_it_was_right()
    {
        using var store = _copy.Open();
        store.AddEditor("Mina", Password);
        store.SignIn("Mina", "not the password");
        _copy.Clock.Advance(TimeSpan.FromMinutes(1));
        store.SignIn("mina", Password);
        store.SignIn("Nobody", Password);

        var tries = store.SignIns("Mina");

        Assert.Equal([true, false], tries.Select(entry => entry.Succeeded));
        Assert.True(tries[0].At > tries[1].At);
        Assert.Empty(store.SignIns("Nobody"));
    }

    [Fact]
    public void Only_the_last_tries_of_an_account_are_kept()
    {
        using var store = _copy.Open();
        store.AddEditor("Mina", Password);
        for (var attempt = 0; attempt < EditorialStore.SignInsKept + 5; attempt++)
        {
            store.SignIn("Mina", Password);
        }

        Assert.Equal(EditorialStore.SignInsKept, store.SignIns("Mina", take: 1000).Length);
    }

    [Fact]
    public void The_commands_make_an_account_with_roles_change_them_and_list_them()
    {
        var (made, _) = Run([Password, Password], "store", "editor", "add", "Omid", "--roles", "reviewer");
        var (changed, said) = Run([], "store", "editor", "roles", "Omid", "writer,reviewer");
        var (none, saidNone) = Run([], "store", "editor", "roles", "Omid", "");
        var (refused, why) = Run([], "store", "editor", "roles", "Omid", "owner");
        var (badAdd, _) = Run([Password, Password], "store", "editor", "add", "Sara", "--roles", "owner");
        var (_, list) = Run([], "store", "editors");

        Assert.Equal(0, made);
        Assert.Equal((0, $"Omid: writer,reviewer.{Environment.NewLine}"), (changed, said));
        Assert.Equal((0, $"Omid: reads only.{Environment.NewLine}"), (none, saidNone));
        Assert.NotEqual(0, refused);
        Assert.Contains("roles are writer, reviewer and publisher", why, StringComparison.Ordinal);
        Assert.NotEqual(0, badAdd);
        Assert.Contains("Omid  since 2026-11-17  reads only", list, StringComparison.Ordinal);
        Assert.DoesNotContain("Sara", list, StringComparison.Ordinal);
    }
}
