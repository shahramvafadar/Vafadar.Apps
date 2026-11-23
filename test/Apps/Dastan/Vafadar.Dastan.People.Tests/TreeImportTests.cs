using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// A family tree written from a file (delivery plan, 25.2): the file is read whole or not at all, the tree is
/// written whole or not at all, by the rules every tree keeps, and nothing is added to a tree that is there.
/// All names are made up.
/// </summary>
public sealed class TreeImportTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private const string Family = """
        {
          "format": "dastan-tree-0.1",
          "name": "The Rad family",
          "people": [
            { "key": "sara", "name": "Sara Rad", "born": 1990 },
            { "key": "nima", "name": "Nima Rad" },
            { "key": "zal", "name": "Zal Rad", "born": 1960 },
            { "key": "rudabeh", "name": "Rudabeh Rad", "born": 1962, "deceased": false },
            { "key": "sam", "name": "Sam Rad", "born": 1930, "died": 2001 },
            { "key": "sam-2", "name": "Sam Rad", "deceased": true },
            { "key": "tara", "name": "Tara Rad", "born": 2015 }
          ],
          "parents": [
            { "parent": "zal", "child": "sara" },
            { "parent": "rudabeh", "child": "sara" },
            { "parent": "zal", "child": "nima" },
            { "parent": "rudabeh", "child": "nima" },
            { "parent": "sam", "child": "zal" },
            { "parent": "sam-2", "child": "rudabeh" },
            { "parent": "sara", "child": "tara", "kind": "adoptive-parent" }
          ],
          "partners": [ ["zal", "rudabeh"] ]
        }
        """;

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 23, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;

    public TreeImportTests()
    {
        _store = PeopleStore.Open(TestDatabase.For(Path.Combine(_folder, "people.db")), _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static TreeFile Read(string json)
    {
        var (tree, problems) = TreeFile.Read(json);
        Assert.True(tree is not null, string.Join('\n', problems));
        return tree!;
    }

    private static string Problems(string json)
    {
        var (tree, problems) = TreeFile.Read(json);
        Assert.Null(tree);
        Assert.NotEmpty(problems);
        return string.Join('\n', problems);
    }

    [Fact]
    public void AFileOfThreeGenerationsIsReadAsItIsWritten()
    {
        var tree = Read(Family);

        Assert.Equal("The Rad family", tree.Name);
        Assert.Equal(["sara", "nima", "zal", "rudabeh", "sam", "sam-2", "tara"], tree.People.Select(person => person.Key));
        Assert.Equal(8, tree.Links.Length);
        Assert.Equal(new TreeFilePerson("sam", "Sam Rad", 1930, 2001, Deceased: true), tree.People[4]);
        Assert.False(tree.People[3].Deceased);
        Assert.Equal(new TreeFileLink("adoptive-parent", "sara", "tara"), tree.Links[6]);
        Assert.Equal(new TreeFileLink("partner", "zal", "rudabeh"), tree.Links[7]);
    }

    [Theory]
    [InlineData("not json", "not valid JSON")]
    [InlineData("[]", "not a JSON object")]
    [InlineData("""{ "format": "dastan-tree-9", "name": "A", "people": [ { "key": "a", "name": "A" } ] }""", "this program reads dastan-tree-0.1")]
    [InlineData("""{ "format": "dastan-tree-0.1", "people": [ { "key": "a", "name": "A" } ] }""", "the tree has no name")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [] }""", "the tree has nobody")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A" }""", "people: the list is missing")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a", "name": "A" }, { "key": "a", "name": "B" } ] }""", "the key a is used twice")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a" } ] }""", "the person has no name")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "name": "A" } ] }""", "the person has no key")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a", "name": "A", "born": "1990-03-01" } ] }""", "a year, never a day")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a", "name": "A", "born": 1990, "died": 1980 } ] }""", "died before they were born")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a", "name": "A", "died": 1980, "deceased": false } ] }""", "said not to have died")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a", "name": "A", "birthPlace": "A town" } ] }""", "the field birthPlace is not one a tree file has")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "note": "x", "people": [ { "key": "a", "name": "A" } ] }""", "the field note is not one a tree file has")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a", "name": "A", "name": "B" } ] }""", "the field name is said 2 times")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a", "name": "A" } ], "parents": [ { "parent": "a", "child": "b" } ] }""", "there is no person with the key b")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a", "name": "A" } ], "parents": [ { "parent": "a", "child": "a" } ] }""", "a is linked with themself")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a", "name": "A" }, { "key": "b", "name": "B" } ], "parents": [ { "parent": "a", "child": "b", "kind": "friend" } ] }""", "one of parent, adoptive-parent and guardian")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a", "name": "A" }, { "key": "b", "name": "B" } ], "parents": [ { "parent": "a", "child": "b" } ], "partners": [ ["b", "a"] ] }""", "linked already")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a", "name": "A" }, { "key": "b", "name": "B" } ], "partners": [ ["a"] ] }""", "a pair of two keys")]
    [InlineData("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a", "name": "A" }, { "key": "b", "name": "B" }, { "key": "c", "name": "C" } ], "parents": [ { "parent": "a", "child": "b" }, { "parent": "b", "child": "c" }, { "parent": "c", "child": "a" } ] }""", "would be their own ancestor")]
    public void AFileThatIsNotUnderstoodWholeIsNotReadAtAll(string json, string problem)
    {
        Assert.Contains(problem, Problems(json), StringComparison.Ordinal);
    }

    [Fact]
    public void EveryProblemOfAFileIsSaidAtOnce()
    {
        var problems = Problems("""{ "format": "dastan-tree-0.1", "name": "A", "people": [ { "key": "a" }, { "key": "a", "name": "B", "born": 0 } ] }""");

        Assert.Contains("people[0]: the person has no name", problems, StringComparison.Ordinal);
        Assert.Contains("people[1]: the key a is used twice", problems, StringComparison.Ordinal);
        Assert.Contains("people[1]: born is a year", problems, StringComparison.Ordinal);
    }

    [Fact]
    public void ATreeIsWrittenIntoTheAccountAndReadBack()
    {
        var came = _store.ImportTree("mina", Read(Family));

        Assert.Null(came.Problem);
        Assert.Equal((7, 8), (came.People, came.Links));
        var tree = _store.Tree(_mina, came.Tree!)!;
        Assert.Equal("The Rad family", tree.Tree.Name);
        Assert.Equal(7, tree.People.Length);
        Assert.Equal(8, tree.Links.Length);
        Assert.Equal(2, tree.People.Count(person => person.Name == "Sam Rad"));

        var sara = tree.People.Single(person => person.Name == "Sara Rad");
        var parents = tree.Links.Where(link => link.Kind == "parent" && link.To == sara.Id).Select(link => tree.Person(link.From).Name).Order();
        Assert.Equal(["Rudabeh Rad", "Zal Rad"], parents);
        Assert.Single(tree.Links, link => link.Kind == "adoptive-parent");
        Assert.Single(tree.Links, link => link.Kind == "partner");

        // The tree is the account's own and nobody else's, and no record of it is said to be an account.
        Assert.Single(_store.Trees(_mina));
        Assert.Empty(tree.Members);
        Assert.All(tree.People, person => Assert.Null(person.Account));
    }

    [Fact]
    public void ASecondRunWritesNothingTwice()
    {
        Assert.NotNull(_store.ImportTree("Mina", Read(Family)).Tree);

        var again = _store.ImportTree("Mina", Read(Family));

        Assert.Null(again.Tree);
        Assert.Contains("has a tree of this name already", again.Problem, StringComparison.Ordinal);
        Assert.Single(_store.Trees(_mina));
    }

    [Fact]
    public void AnAccountThatIsNotThereGetsNoTreeAndIsNotMade()
    {
        var came = _store.ImportTree("Nobody", Read(Family));

        Assert.Null(came.Tree);
        Assert.Contains("there is no open account of this name", came.Problem, StringComparison.Ordinal);
    }

    [Fact]
    public void ATreeThatCannotBeWrittenWholeWritesNothing()
    {
        // The last record has a name no record can have: what was written before it is taken back.
        var file = Read(Family) with { Name = "A tree that fails" };
        file = file with { People = file.People.Add(new TreeFilePerson("last", new string('x', 500), null, null, false)) };

        var came = _store.ImportTree("Mina", file);

        Assert.Null(came.Tree);
        Assert.Contains("last: the record cannot be written", came.Problem, StringComparison.Ordinal);
        Assert.Empty(_store.Trees(_mina));
    }

    [Fact]
    public void AnAccountWithAsManyTreesAsItMayHaveGetsNoMore()
    {
        for (var number = 0; number < PeopleStore.MostTrees; number++)
        {
            Assert.NotNull(_store.AddTree(_mina, $"Tree {number}"));
        }

        var came = _store.ImportTree("Mina", Read(Family));

        Assert.Null(came.Tree);
        Assert.Contains("as many trees as an account may", came.Problem, StringComparison.Ordinal);
        Assert.Equal(PeopleStore.MostTrees, _store.Trees(_mina).Count);
    }
}
