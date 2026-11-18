using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// A copy of one's own data (step 16.7, D-055): everything the store holds about an account, proved by its
/// password – and nothing that is a secret or belongs to somebody else.
/// </summary>
public sealed class OwnDataTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;

    public OwnDataTests()
    {
        _store = PeopleStore.Open(File, _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
    }

    private string File => Path.Combine(_folder, "people.db");

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private string Friends()
    {
        _store.Ask(_mina, "Sara", "family");
        var id = _store.Relationships(_sara)[0].Id;
        _store.Answer(_sara, id, accept: true);
        return id;
    }

    [Fact]
    public void A_wrong_password_and_a_closed_account_get_no_copy()
    {
        Assert.Null(_store.Export(_mina, "a wrong pass phrase"));
        Assert.Null(_store.Export("no-such-account", Password));
        Assert.NotNull(_store.Export(_mina, Password));

        _store.Close(_mina, Password);
        Assert.Null(_store.Export(_mina, Password));
    }

    [Fact]
    public void The_copy_of_a_new_account_says_who_it_is_and_that_it_has_nothing()
    {
        var copy = _store.Export(_mina, Password)!;

        Assert.Equal(("dastan-account", PeopleStore.ExportVersion, "2026-11-18T09:00:00Z"), ((string?)copy["format"], (int?)copy["version"], (string?)copy["madeAt"]));
        Assert.Equal(("Mina", "2026-11-18T09:00:00Z", PeopleStore.RecoveryCodeCount), ((string?)copy["account"]!["name"], (string?)copy["account"]!["createdAt"], (int?)copy["account"]!["recoveryCodesLeft"]));
        foreach (var part in new[] { "readingPositions", "list", "invitations", "relationships", "circles", "trees", "treesSharedWithMe" })
        {
            Assert.Empty(copy[part]!.AsArray());
        }
    }

    [Fact]
    public void The_copy_holds_everything_that_belongs_to_the_account()
    {
        _store.SetPosition(_mina, "story-rostam-sohrab", 3);
        _store.Keep(_mina, new KeptPage("story-rostam-sohrab", "entity", "character-rostam"));
        var code = _store.Invite(_mina)!;
        var relationship = Friends();
        var circle = _store.AddCircle(_mina, "Close")!.Value;
        _store.SetCircle(_mina, circle, [_sara]);
        _store.Show(_mina, relationship, ScopeKind.Circle, circle);
        var tree = _store.AddTree(_mina, "The Rad family")!;
        var zal = _store.AddPerson(_mina, tree, "Zal", 1270, null, deceased: true)!;
        var rostam = _store.AddPerson(_mina, tree, "Rostam", null, null, deceased: false)!;
        _store.Link(_mina, tree, "parent", zal, rostam);
        _store.SetTreeMember(_mina, tree, _sara, TreeRole.Editor);

        var copy = _store.Export(_mina, Password)!;

        Assert.Equal("""[{"collection":"story-rostam-sohrab","stage":3}]""", copy["readingPositions"]!.ToJsonString());
        Assert.Equal("""[{"collection":"story-rostam-sohrab","kind":"entity","id":"character-rostam"}]""", copy["list"]!.ToJsonString());
        Assert.Equal($$"""[{"code":"{{code}}","lastDay":"2026-12-02","state":"open"}]""", copy["invitations"]!.ToJsonString());
        Assert.Equal("""[{"with":"Sara","kind":"family","state":"accepted","askedByMe":true,"shownTo":"Close"}]""", copy["relationships"]!.ToJsonString());
        Assert.Equal("""[{"name":"Close","members":["Sara"]}]""", copy["circles"]!.ToJsonString());
        var written = Assert.Single(copy["trees"]!.AsArray())!;
        Assert.Equal("The Rad family", (string?)written["name"]);
        Assert.Equal("""[{"name":"Rostam","born":null,"died":null,"deceased":false,"account":null},{"name":"Zal","born":1270,"died":null,"deceased":true,"account":null}]""", written["people"]!.ToJsonString());
        Assert.Equal("""[{"kind":"parent","from":"Zal","to":"Rostam"}]""", written["links"]!.ToJsonString());
        Assert.Equal("""[{"account":"Sara","role":"editor"}]""", written["sharedWith"]!.ToJsonString());
        Assert.Equal(5, written["history"]!.AsArray().Count);
        Assert.Equal(("Mina", "tree-made", "The Rad family"), ((string?)written["history"]![4]!["by"], (string?)written["history"]![4]!["what"], (string?)written["history"]![4]!["subject"]));
    }

    [Fact]
    public void A_tree_of_somebody_else_is_named_and_not_copied()
    {
        Friends();
        var tree = _store.AddTree(_mina, "The Rad family")!;
        var record = _store.AddPerson(_mina, tree, "Sara Rad", null, null, deceased: false)!;
        _store.AddPerson(_mina, tree, "Zal", null, null, deceased: false);
        _store.SetTreeMember(_mina, tree, _sara, TreeRole.Viewer);
        _store.Claim(_sara, tree, record);

        var waiting = _store.Export(_sara, Password)!;
        Assert.Equal("""[{"name":"The Rad family","owner":"Mina","role":"viewer","recordThatIsMe":null,"recordIClaim":"Sara Rad"}]""", waiting["treesSharedWithMe"]!.ToJsonString());

        _store.ReviewClaim(_mina, tree, _store.Tree(_mina, tree)!.Claims[0].Number, confirm: true);
        var copy = _store.Export(_sara, Password)!;

        Assert.Empty(copy["trees"]!.AsArray());
        Assert.Equal("""[{"name":"The Rad family","owner":"Mina","role":"viewer","recordThatIsMe":"Sara Rad","recordIClaim":null}]""", copy["treesSharedWithMe"]!.ToJsonString());
        Assert.DoesNotContain("Zal", copy.ToJsonString(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_copy_holds_no_secret_and_no_identifier_of_anybody()
    {
        var codes = _store.Register("Dara", Password).RecoveryCodes;
        Friends();
        var tree = _store.AddTree(_mina, "The Rad family")!;
        _store.AddPerson(_mina, tree, "Zal", null, null, deceased: false);

        var copy = _store.Export(_mina, Password)!.ToJsonString();

        var kept = new List<string>();
        using (var connection = new SqliteConnection($"Data Source={File};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "select Password from Visitors union all select Stamp from Visitors union all select Code from RecoveryCodes union all select Id from Visitors union all select Id from Trees union all select Id from Persons";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                kept.Add(reader.GetString(0));
            }
        }

        Assert.NotEmpty(kept);
        Assert.DoesNotContain(kept, secret => copy.Contains(secret, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(Password, copy, StringComparison.Ordinal);
        Assert.DoesNotContain(codes, code => copy.Contains(code, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("Dara", copy, StringComparison.Ordinal);
        Assert.IsType<JsonObject>(JsonNode.Parse(copy));
    }
}
