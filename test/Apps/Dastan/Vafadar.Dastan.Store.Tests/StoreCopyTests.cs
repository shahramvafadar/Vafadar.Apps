using Vafadar.Dastan.Content;

namespace Vafadar.Dastan.Store.Tests;

/// <summary>
/// A copy of the store (step 11.9): the whole store as it is at a moment, written while the store is in use, to
/// be kept elsewhere and put in the place of the file of the store when that is lost.
/// </summary>
public sealed class StoreCopyTests : IDisposable
{
    private const string Collection = WorkingCopy.Collection;

    private readonly WorkingCopy _copy = new();

    public void Dispose() => _copy.Dispose();

    private string Target => Path.Combine(_copy.Folder, "copies", "editors-copy.db");

    [Fact]
    public void A_copy_holds_the_collections_the_history_and_the_accounts_of_its_moment()
    {
        using var store = _copy.Filled();
        Assert.True(store.AddEditor("Mina", "a long pass phrase").Done);
        var changes = store.History(Collection).Length;

        Assert.Null(store.CopyTo(Target));

        // What happens to the store afterwards is not in the copy.
        Assert.True(store.Remove(Collection, "relation-18", "an editor", "Not told in the selected part.").Done);
        using var copied = EditorialStore.Open(Target, _copy.Clock);
        Assert.Equal([Collection], copied.Collections);
        Assert.Equal(changes, copied.History(Collection).Length);
        Assert.NotNull(copied.Record(Collection, "relations", "relation-18"));
        Assert.NotNull(copied.SignIn("Mina", "a long pass phrase"));
        Assert.Equal(store.Export(Collection)!.Json.Length > 0, copied.Export(Collection)!.Json.Length > 0);
    }

    [Fact]
    public void A_copy_is_never_written_over_a_file()
    {
        using var store = _copy.Filled();
        Assert.Null(store.CopyTo(Target));
        var written = File.GetLastWriteTimeUtc(Target);

        var again = store.CopyTo(Target);

        Assert.Contains("a copy is never written over a file", again, StringComparison.Ordinal);
        Assert.Equal(written, File.GetLastWriteTimeUtc(Target));
    }

    [Fact]
    public void The_command_writes_a_copy_and_makes_no_store_where_there_is_none()
    {
        using (_copy.Filled())
        {
        }

        using var output = new StringWriter();
        var done = ContentCommands.Run(["store", "copy", "--store", _copy.Database, "--out", Target], output, new DateOnly(2026, 11, 17), _copy.Clock);
        var absent = Path.Combine(_copy.Folder, "no-such-store.db");
        var none = ContentCommands.Run(["store", "copy", "--store", absent, "--out", Target + ".2"], output, new DateOnly(2026, 11, 17), _copy.Clock);

        Assert.Equal(0, done);
        Assert.Contains("a copy of the store", output.ToString(), StringComparison.Ordinal);
        Assert.True(new FileInfo(Target).Length > 0);
        Assert.Equal(1, none);
        Assert.False(File.Exists(absent));
        Assert.False(File.Exists(Target + ".2"));
    }
}
