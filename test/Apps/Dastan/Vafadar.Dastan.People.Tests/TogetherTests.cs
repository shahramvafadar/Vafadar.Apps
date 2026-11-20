using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// Acts on the same accounts or the same tree that arrive together (step 20.2, ADR 0022). Each act has a store
/// and a connection of its own. Two acts are held at the place where each has decided and not yet written:
/// without a lock both stand there at once, and what they then write breaks a rule; with the locks the second
/// cannot come so far before the first is done, and what is left is what one after the other would leave.
/// </summary>
public sealed class TogetherTests : IDisposable
{
    private const string Password = "a long pass phrase";

    // How long the first act waits at its place for the second. With the locks the second never comes while
    // the first waits, so this is waited once in such a test; it is long enough for a connection to be opened.
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(2);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero));
    private readonly string _database;
    private readonly string _mina;
    private readonly string _sara;

    public TogetherTests()
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

    // Mina asks, Sara accepts; returns the identifier of the relationship.
    private string Shown(PeopleStore store, string kind)
    {
        Assert.Equal("asked", store.Ask(_mina, "Sara", kind));
        var id = store.Relationships(_sara).Single(relationship => relationship.State == RelationshipState.Requested && !relationship.AskedByMe).Id;
        Assert.True(store.Answer(_sara, id, accept: true));
        return id;
    }

    /// <summary>
    /// Runs two acts, each with a store of its own, and holds each at the named place until the other stands at
    /// its place too or the time is over. Returns what the acts answered and whether they stood there together.
    /// With <paramref name="secondWritesLast"/> the second act, once both stood at their places, goes on only
    /// when the first is done: the order in which two acts without a lock would do the most harm.
    /// </summary>
    private (T First, T Second, bool Together) Both<T>(string firstPlace, Func<PeopleStore, T> first, string secondPlace, Func<PeopleStore, T> second, bool secondWritesLast = false)
    {
        using var arrived = new CountdownEvent(2);
        using var firstDone = new ManualResetEventSlim();
        var together = 0;
        T Run(string place, Func<PeopleStore, T> act, bool last)
        {
            using var store = Open();
            var held = false;
            store.Reached = at =>
            {
                if (at == place && !held)
                {
                    held = true;
                    arrived.Signal();
                    if (arrived.Wait(Wait))
                    {
                        Interlocked.Increment(ref together);
                    }

                    if (last)
                    {
                        firstDone.Wait(Wait);
                    }
                }
            };
            return act(store);
        }

        var one = Task.Run(() =>
        {
            try
            {
                return Run(firstPlace, first, last: false);
            }
            finally
            {
                firstDone.Set();
            }
        });
        var other = Task.Run(() => Run(secondPlace, second, secondWritesLast));
        Task.WaitAll(one, other);
        return (one.Result, other.Result, together == 2);
    }

    // What the rule of the memberships asks: Sara is in a circle or a tree of Mina's only while the two show
    // an accepted relationship.
    private void AssertMembershipsFollowRelationships(long circle, string tree)
    {
        using var store = Open();
        var known = store.Relationships(_mina).Any(relationship => relationship.State == RelationshipState.Accepted);
        var inCircle = store.Circles(_mina).Single(candidate => candidate.Number == circle).Members.Any(member => member.Id == _sara);
        var inTree = store.Tree(_mina, tree)!.Members.Any(member => member.Account.Id == _sara);
        Assert.True(known || !inCircle, "Sara is in a circle of Mina's and the two show no relationship.");
        Assert.True(known || !inTree, "Sara is a member of a tree of Mina's and the two show no relationship.");
        Assert.Equal(known && inTree, store.Tree(_sara, tree) is not null);
    }

    private (long Circle, string Tree) Shared(PeopleStore store, bool with)
    {
        var circle = store.AddCircle(_mina, "Close")!.Value;
        var tree = store.AddTree(_mina, "The Rad family")!;
        if (with)
        {
            Assert.True(store.SetCircle(_mina, circle, [_sara]));
            Assert.True(store.SetTreeMember(_mina, tree, _sara, TreeRole.Viewer));
        }

        return (circle, tree);
    }

    [Fact]
    public void Two_last_relationships_that_end_together_leave_no_membership_behind()
    {
        string friend, neighbour;
        long circle;
        string tree;
        using (var store = Open())
        {
            (friend, neighbour) = (Shown(store, "friend"), Shown(store, "neighbour"));
            (circle, tree) = Shared(store, with: true);
        }

        var (first, second, _) = Both("relationship", store => store.End(_mina, friend), "relationship", store => store.End(_sara, neighbour));

        Assert.True(first && second);
        using var after = Open();
        Assert.Empty(after.Relationships(_mina));
        Assert.Empty(after.Circles(_mina).Single(candidate => candidate.Number == circle).Members);
        Assert.Empty(after.Tree(_mina, tree)!.Members);
        Assert.Null(after.Tree(_sara, tree));
        Assert.DoesNotContain(after.Trees(_sara), shared => shared.Id == tree);
    }

    [Fact]
    public void One_relationship_that_ends_while_another_stays_keeps_what_was_shared()
    {
        using var store = Open();
        var (friend, _) = (Shown(store, "friend"), Shown(store, "neighbour"));
        var (circle, tree) = Shared(store, with: true);

        Assert.True(store.End(_mina, friend));

        Assert.Single(store.Circles(_mina).Single(candidate => candidate.Number == circle).Members);
        Assert.NotNull(store.Tree(_sara, tree));
    }

    [Theory]
    [InlineData("end", "tree")]
    [InlineData("end", "circle")]
    [InlineData("block", "tree")]
    [InlineData("block", "circle")]
    public void Sharing_with_somebody_while_the_last_relationship_goes_never_shares_with_a_stranger(string going, string sharing)
    {
        string friend;
        long circle;
        string tree;
        using (var store = Open())
        {
            friend = Shown(store, "friend");
            (circle, tree) = Shared(store, with: false);
        }

        Both(
            going == "end" ? "relationship" : "block",
            store => going == "end" ? store.End(_sara, friend) : store.Block(_sara, _mina),
            sharing == "tree" ? "tree-member" : "circle",
            store => sharing == "tree" ? store.SetTreeMember(_mina, tree, _sara, TreeRole.Editor) : store.SetCircle(_mina, circle, [_sara]),
            secondWritesLast: true);

        AssertMembershipsFollowRelationships(circle, tree);
        using var after = Open();
        Assert.Empty(after.Relationships(_mina));
    }

    [Fact]
    public void Agreeing_to_another_name_while_the_relationship_ends_leaves_one_of_the_two_outcomes()
    {
        string friend, change;
        long circle;
        string tree;
        using (var store = Open())
        {
            friend = Shown(store, "friend");
            (circle, tree) = Shared(store, with: true);
            Assert.Equal("asked", store.AskChange(_mina, friend, "close-friend"));
            change = store.Relationships(_sara).Single(relationship => relationship.State == RelationshipState.Requested).Id;
        }

        var (agreed, ended, _) = Both("relationship", store => store.Answer(_sara, change, accept: true), "relationship", store => store.End(_mina, friend));

        using var after = Open();
        var left = after.Relationships(_mina);
        if (agreed && !ended)
        {
            // The new one took the place of the old one before the end came; the end found nothing to end.
            Assert.Equal("close-friend", Assert.Single(left).Kind);
            Assert.Equal(RelationshipState.Accepted, left[0].State);
        }
        else
        {
            // The end came first and took the request with it.
            Assert.True(ended && !agreed, $"agreed: {agreed}, ended: {ended}");
            Assert.Empty(left);
        }

        AssertMembershipsFollowRelationships(circle, tree);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Two_links_that_would_close_a_loop_together_are_not_both_made(bool longWay)
    {
        string tree, a, b, c, d;
        using (var store = Open())
        {
            tree = store.AddTree(_mina, "The Rad family")!;
            (a, b) = (store.AddPerson(_mina, tree, "A", null, null, deceased: true)!, store.AddPerson(_mina, tree, "B", null, null, deceased: true)!);
            (c, d) = (store.AddPerson(_mina, tree, "C", null, null, deceased: true)!, store.AddPerson(_mina, tree, "D", null, null, deceased: true)!);
            if (longWay)
            {
                // A is a parent of B and C of D; B a parent of C and D a parent of A together would close it.
                Assert.Equal("done", store.Link(_mina, tree, "parent", a, b));
                Assert.Equal("done", store.Link(_mina, tree, "parent", c, d));
            }
        }

        var (first, second, _) = longWay
            ? Both("link", store => store.Link(_mina, tree, "parent", b, c), "link", store => store.Link(_mina, tree, "parent", d, a))
            : Both("link", store => store.Link(_mina, tree, "parent", a, b), "link", store => store.Link(_mina, tree, "parent", b, a));

        Assert.Single(new[] { first, second }, answer => answer == "done");
        Assert.Contains(longWay ? "loop" : "exists", new[] { first, second });
        using var after = Open();
        Assert.Equal(longWay ? 3 : 1, after.Tree(_mina, tree)!.Links.Length);
    }

    [Fact]
    public void Links_in_two_trees_do_not_wait_for_each_other()
    {
        string one, other, a, b, c, d;
        using (var store = Open())
        {
            (one, other) = (store.AddTree(_mina, "The Rad family")!, store.AddTree(_sara, "The Kian family")!);
            (a, b) = (store.AddPerson(_mina, one, "A", null, null, deceased: true)!, store.AddPerson(_mina, one, "B", null, null, deceased: true)!);
            (c, d) = (store.AddPerson(_sara, other, "C", null, null, deceased: true)!, store.AddPerson(_sara, other, "D", null, null, deceased: true)!);
        }

        var (first, second, together) = Both("link", store => store.Link(_mina, one, "parent", a, b), "link", store => store.Link(_sara, other, "parent", c, d));

        Assert.Equal(("done", "done"), (first, second));
        Assert.True(together, "A link in one tree waited for a link in another.");
    }

    [Fact]
    public async Task An_act_inside_another_holds_its_locks_or_is_refused()
    {
        using var store = Open();
        using var other = Open();

        // Further down the order: the lock is taken, and nobody else gets it until the outer act is done.
        Task<bool>? elsewhere = null;
        var waited = store.Guarded([PeopleStore.OfAccount(_mina)], () => store.Guarded(["tree:made-up"], () =>
        {
            elsewhere = Task.Run(() => other.Guarded(["tree:made-up"], () => true));
            return !elsewhere.Wait(TimeSpan.FromMilliseconds(500));
        }));
        Assert.True(waited, "A lock asked for inside another act was not held.");

        // When the outer act is done the lock is free, and who waited for it gets it.
        Assert.True(await elsewhere!.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        // Back up the order: two acts could wait for each other, so it is refused, and nothing is written.
        var (later, earlier) = string.CompareOrdinal(_mina, _sara) > 0 ? (_mina, _sara) : (_sara, _mina);
        Assert.Throws<InvalidOperationException>(() => store.Guarded([PeopleStore.OfAccount(later)], () =>
        {
            Assert.NotNull(store.AddCircle(later, "Written before the refusal"));
            return store.Guarded([PeopleStore.OfAccount(earlier)], () => true);
        }));
        Assert.Empty(other.Circles(later));
    }
}
