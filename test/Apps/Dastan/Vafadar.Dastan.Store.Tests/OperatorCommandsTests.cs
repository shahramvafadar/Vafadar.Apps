using Vafadar.Dastan.Content;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.People;

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

    private string Notices => Path.Combine(_folder, "notices");

    private string People => Path.Combine(_folder, "people.db");

    public OperatorCommandsTests() => Directory.CreateDirectory(Notices);

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
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
        Assert.Equal(ContentCommands.NotUsable, Run("notices", Path.Combine(_folder, "no-such-folder")).Exit);
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

        var restricted = Run("people", "restrict", "tree", tree, "--people", People, "--reason", "It names a living person who objected.", "--ground", "Section 7 of the terms", "--noticed");
        Assert.Equal(ContentCommands.Done, restricted.Exit);
        Assert.Contains("restricted, decision no. 1.", restricted.Output, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.Done, Run("people", "restrict", "account", "Mina", "--people", People, "--reason", "Repeated breaches.", "--ground", "Section 10 of the terms").Exit);
        Assert.Equal(ContentCommands.NotUsable, Run("people", "restrict", "photo", "no-such-photo", "--people", People, "--reason", "A reason.", "--ground", "A ground.").Exit);
        Assert.Equal(ContentCommands.NotUsable, Run("people", "restrictions", "--people", Path.Combine(_folder, "no-such.db")).Exit);
        Assert.False(File.Exists(Path.Combine(_folder, "no-such.db")));

        var standing = Run("people", "restrictions", "--people", People).Output;
        Assert.Contains("2 restrictions stand.", standing, StringComparison.Ordinal);
        Assert.Contains($"1  2026-11-19  tree  {tree}  of Mina  (after a notice)", standing, StringComparison.Ordinal);
        Assert.Contains("reason: Repeated breaches.", standing, StringComparison.Ordinal);

        Assert.Contains($"{zal}  Zal", Run("people", "persons", tree, "--people", People).Output, StringComparison.Ordinal);
        Assert.Equal(ContentCommands.Done, Run("people", "person", "remove", tree, zal, "--people", People).Exit);
        Assert.Equal(ContentCommands.NotUsable, Run("people", "person", "remove", tree, zal, "--people", People).Exit);

        // An account that is not closed is not forgotten; one its visitor closed is, at once.
        Assert.Equal(ContentCommands.NotUsable, Run("people", "forget", "Nobody", "--people", People).Exit);
        Assert.Equal(ContentCommands.Done, Run("people", "forget", "Mina", "--people", People).Exit);
        using (var closed = PeopleStore.Open(People, _clock))
        {
            Assert.Equal(1, closed.Purge());
        }

        Assert.Equal(ContentCommands.NotUsable, Run("people", "lift", "2", "--people", People).Exit);
        Assert.Equal(ContentCommands.NotUnderstood, Run("people", "lift", "two", "--people", People).Exit);
        using var gone = PeopleStore.Open(People, _clock);
        Assert.Null(gone.SignIn("Mina", Password));
        Assert.Empty(gone.AllRestrictions());
    }
}
