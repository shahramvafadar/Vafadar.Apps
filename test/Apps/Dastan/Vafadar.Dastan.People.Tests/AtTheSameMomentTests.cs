using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// The rules of the store that count before they add, tried with requests that arrive at the same moment
/// (step 19.3, ADR 0015): each request has a connection of its own, as each request of the site has, and all
/// begin together. However many ask, the store ends with what its rule allows and not one more.
/// </summary>
public sealed class AtTheSameMomentTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const int Askers = 12;

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 19, 9, 0, 0, TimeSpan.Zero));
    private readonly string _database;
    private readonly string _mina;
    private readonly string _sara;

    public AtTheSameMomentTests()
    {
        _database = TestDatabase.For(Path.Combine(_folder, "people.db"));
        using var store = Open();
        _mina = store.Register("Mina", Password).Session!.Id;
        _sara = store.Register("Sara", Password).Session!.Id;
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private PeopleStore Open() => PeopleStore.Open(_database, _clock);

    // Runs an act once per asker, each with a store of its own, all let go at the same moment.
    private List<T> AllAtOnce<T>(Func<PeopleStore, int, T> act)
    {
        using var start = new ManualResetEventSlim();
        var asking = Enumerable.Range(0, Askers).Select(number => Task.Run(() =>
        {
            using var store = Open();
            start.Wait();
            return act(store, number);
        })).ToArray();
        start.Set();
        Task.WaitAll(asking);
        return [.. asking.Select(task => task.Result)];
    }

    [Fact]
    public void An_account_gets_its_last_tree_once_however_many_ask_for_one()
    {
        using (var store = Open())
        {
            for (var number = 0; number < PeopleStore.MostTrees - 1; number++)
            {
                Assert.NotNull(store.AddTree(_mina, $"Tree {number}"));
            }
        }

        var made = AllAtOnce((store, number) => store.AddTree(_mina, $"Last tree {number}"));

        Assert.Single(made, id => id is not null);
        using var after = Open();
        Assert.Equal(PeopleStore.MostTrees, after.Trees(_mina).Count);
    }

    [Fact]
    public void An_account_gets_its_last_circle_and_its_last_album_once()
    {
        using (var store = Open())
        {
            for (var number = 0; number < PeopleStore.MostCircles - 1; number++)
            {
                Assert.NotNull(store.AddCircle(_mina, $"Circle {number}"));
            }

            for (var number = 0; number < PeopleStore.MostAlbums - 1; number++)
            {
                Assert.NotNull(store.AddAlbum(_mina, $"Album {number}"));
            }
        }

        Assert.Single(AllAtOnce((store, number) => store.AddCircle(_mina, $"Last circle {number}")), circle => circle is not null);
        Assert.Single(AllAtOnce((store, number) => store.AddAlbum(_mina, $"Last album {number}")), album => album is not null);

        // The same name asked for by everybody at once is one circle, not twelve.
        using (var store = Open())
        {
            Assert.True(store.RemoveCircle(_mina, store.Circles(_mina)[0].Number));
        }

        Assert.Single(AllAtOnce((store, _) => store.AddCircle(_mina, "The one name")), circle => circle is not null);
    }

    [Fact]
    public void An_account_has_no_more_open_invitations_than_it_may_however_many_are_asked_for()
    {
        using (var store = Open())
        {
            for (var number = 0; number < PeopleStore.OpenInvitations - 1; number++)
            {
                Assert.NotNull(store.Invite(_mina));
            }
        }

        Assert.Single(AllAtOnce((store, _) => store.Invite(_mina)), code => code is not null);
    }

    [Fact]
    public void A_tree_gets_its_last_record_once_and_a_photograph_its_last_place_once()
    {
        string tree;
        using (var store = Open())
        {
            tree = store.AddTree(_mina, "The Rad family")!;
            for (var number = 0; number < PeopleStore.MostPeople - 1; number++)
            {
                Assert.NotNull(store.AddPerson(_mina, tree, $"Person {number}", null, null, deceased: true));
            }

            for (var number = 0; number < PeopleStore.MostPhotos - 1; number++)
            {
                Assert.True(store.AddPhoto(_mina, $"photo-{number}", new PhotoFile($"photo-{number}.jpg", "image/jpeg", 8, 8, 100), string.Empty, null, ScopeKind.Private));
            }
        }

        Assert.Single(AllAtOnce((store, number) => store.AddPerson(_mina, tree, $"Last person {number}", null, null, deceased: true)), id => id is not null);
        Assert.Single(AllAtOnce((store, number) => store.AddPhoto(_mina, $"last-{number}", new PhotoFile($"last-{number}.jpg", "image/jpeg", 8, 8, 100), string.Empty, null, ScopeKind.Private)), kept => kept);
    }

    [Fact]
    public void Two_accounts_that_ask_each_other_at_the_same_moment_have_one_request_between_them()
    {
        // Half of the askers are Mina asking Sara, the other half Sara asking Mina.
        var answers = AllAtOnce((store, number) => number % 2 == 0 ? store.Ask(_mina, "Sara", "friend") : store.Ask(_sara, "Mina", "friend"));

        Assert.Single(answers, answer => answer == "asked");
        using var after = Open();
        Assert.Single(after.Relationships(_mina));
        Assert.Single(after.Relationships(_sara));
    }

    [Fact]
    public void An_account_says_once_that_a_record_is_them_however_often_it_is_sent()
    {
        string tree, record;
        using (var store = Open())
        {
            tree = store.AddTree(_mina, "The Rad family")!;
            record = store.AddPerson(_mina, tree, "Sara Rad", 1990, null, deceased: false)!;
            Assert.Equal("asked", store.Ask(_mina, "Sara", "family"));
            Assert.True(store.Answer(_sara, store.Relationships(_sara).Single().Id, accept: true));
            Assert.True(store.SetTreeMember(_mina, tree, _sara, TreeRole.Viewer));
        }

        var answers = AllAtOnce((store, _) => store.Claim(_sara, tree, record));

        Assert.Single(answers, answer => answer == "claimed");
        using var after = Open();
        Assert.Single(after.Tree(_mina, tree)!.Claims);
    }
}
