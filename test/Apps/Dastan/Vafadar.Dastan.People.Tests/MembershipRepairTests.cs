using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;
using Npgsql;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// What two requests at the same moment could leave behind before every act took its locks (step 20.2,
/// ADR 0022): the command that puts it right, and the rules the database keeps by itself. What the store would
/// not write any more is written here past the store, as the old code could have left it.
/// </summary>
public sealed class MembershipRepairTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero));
    private readonly string _database;
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;

    public MembershipRepairTests()
    {
        _database = TestDatabase.For(Path.Combine(_folder, "people.db"));
        _store = PeopleStore.Open(_database, _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
        _dara = _store.Register("Dara", Password).Session!.Id;
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    // Writes past the store, as nothing of the site does.
    private void Write(string command, params (string Name, object Value)[] values)
    {
        using var connection = new NpgsqlConnection(_database);
        connection.Open();
        using var statement = new NpgsqlCommand(command, connection);
        foreach (var (name, value) in values)
        {
            statement.Parameters.AddWithValue(name, value);
        }

        statement.ExecuteNonQuery();
    }

    private void Friends(string from, string to, string toName)
    {
        Assert.Equal("asked", _store.Ask(from, toName, "friend"));
        Assert.True(_store.Answer(to, _store.Relationships(to).Single(relationship => relationship.State == RelationshipState.Requested && !relationship.AskedByMe).Id, accept: true));
    }

    [Fact]
    public void Who_is_shared_with_without_a_relationship_is_taken_out_and_everybody_else_stays()
    {
        Friends(_mina, _sara, "Sara");
        var circle = _store.AddCircle(_mina, "Close")!.Value;
        var tree = _store.AddTree(_mina, "The Rad family")!;
        Assert.True(_store.SetCircle(_mina, circle, [_sara]));
        Assert.True(_store.SetTreeMember(_mina, tree, _sara, TreeRole.Viewer));

        // Dara and Mina show nothing; the old code could have left Dara in both.
        Write("""INSERT INTO personal."CircleMembers" ("Circle", "Member") VALUES (@circle, @member)""", ("circle", circle), ("member", _dara));
        Write("""INSERT INTO personal."TreeMembers" ("Tree", "Account", "Role") VALUES (@tree, @account, 1)""", ("tree", tree), ("account", _dara));
        Assert.NotNull(_store.Tree(_dara, tree));

        // Only counted: nothing changes.
        Assert.Equal(new MembershipRepair(1, 1, []), _store.RepairMemberships(change: false) with { TreesWithALoop = [] });
        Assert.NotNull(_store.Tree(_dara, tree));

        var done = _store.RepairMemberships(change: true);

        Assert.Equal((1, 1), (done.CircleMembers, done.TreeMembers));
        Assert.Null(_store.Tree(_dara, tree));
        Assert.Equal(["Sara"], _store.Circles(_mina).Single().Members.Select(member => member.Name));
        Assert.Equal(["Sara"], _store.Tree(_mina, tree)!.Members.Select(member => member.Account.Name));
        Assert.NotNull(_store.Tree(_sara, tree));

        // A second run finds nothing.
        var again = _store.RepairMemberships(change: true);
        Assert.Equal((0, 0), (again.CircleMembers, again.TreeMembers));
    }

    [Fact]
    public void A_tree_in_which_somebody_is_their_own_ancestor_is_named_and_left_as_it_is()
    {
        var tree = _store.AddTree(_mina, "The Rad family")!;
        var sound = _store.AddTree(_mina, "The Kian family")!;
        var (a, b, c) = (_store.AddPerson(_mina, tree, "A", null, null, deceased: true)!, _store.AddPerson(_mina, tree, "B", null, null, deceased: true)!, _store.AddPerson(_mina, tree, "C", null, null, deceased: true)!);
        var (d, e) = (_store.AddPerson(_mina, sound, "D", null, null, deceased: true)!, _store.AddPerson(_mina, sound, "E", null, null, deceased: true)!);
        Assert.Equal("done", _store.Link(_mina, tree, "parent", a, b));
        Assert.Equal("done", _store.Link(_mina, tree, "parent", b, c));
        Assert.Equal("done", _store.Link(_mina, sound, "parent", d, e));
        Assert.Equal("loop", _store.Link(_mina, tree, "parent", c, a));

        // The third link, as two requests at the same moment could have written it.
        Write("""INSERT INTO personal."Kinships" ("Tree", "Kind", "From", "To") VALUES (@tree, 'parent', @from, @to)""", ("tree", tree), ("from", c), ("to", a));

        var found = _store.RepairMemberships(change: true);

        Assert.Equal([tree], found.TreesWithALoop);
        Assert.Equal(3, _store.Tree(_mina, tree)!.Links.Length);
    }

    [Fact]
    public void The_database_itself_refuses_a_link_to_oneself_and_a_second_link_between_the_same_two()
    {
        var tree = _store.AddTree(_mina, "The Rad family")!;
        var (a, b) = (_store.AddPerson(_mina, tree, "A", null, null, deceased: true)!, _store.AddPerson(_mina, tree, "B", null, null, deceased: true)!);
        Assert.Equal("done", _store.Link(_mina, tree, "parent", a, b));
        const string Add = """INSERT INTO personal."Kinships" ("Tree", "Kind", "From", "To") VALUES (@tree, @kind, @from, @to)""";

        var toOneself = Assert.Throws<PostgresException>(() => Write(Add, ("tree", tree), ("kind", "partner"), ("from", a), ("to", a)));
        var again = Assert.Throws<PostgresException>(() => Write(Add, ("tree", tree), ("kind", "parent"), ("from", a), ("to", b)));
        var theOtherWay = Assert.Throws<PostgresException>(() => Write(Add, ("tree", tree), ("kind", "partner"), ("from", b), ("to", a)));

        Assert.Equal(PostgresErrorCodes.CheckViolation, toOneself.SqlState);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, again.SqlState);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, theOtherWay.SqlState);
        Assert.Single(_store.Tree(_mina, tree)!.Links);
    }
}
