using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// What who runs the site does about content that may not stay (row 18.8): a photograph is taken out of
/// sight, a tree is taken back from the public, an account is closed – each with a statement of reasons its
/// owner reads, and each can be lifted. And which terms an account said it read.
/// </summary>
public sealed class OperatorRestrictionsTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 19, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;

    public OperatorRestrictionsTests()
    {
        _store = PeopleStore.Open(Path.Combine(_folder, "people.db"), _clock);
        _mina = _store.Register("Mina", Password, terms: "2026-11-19").Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void A_restricted_photograph_is_seen_by_its_owner_only_until_the_restriction_is_lifted()
    {
        Assert.True(_store.AddPhoto(_mina, "photo-1", new PhotoFile("photo-1.jpg", "image/jpeg", 800, 600, 12345), "At the sea", null, ScopeKind.Public));
        Assert.NotNull(_store.Photo(null, "photo-1"));

        var number = _store.Restrict(RestrictionKind.Photo, "photo-1", "It shows a person who objected.", "Section 7 of the terms", noticed: true);

        Assert.NotNull(number);
        Assert.Null(_store.Photo(null, "photo-1"));
        Assert.Null(_store.Photo(_sara, "photo-1"));
        Assert.NotNull(_store.Photo(_mina, "photo-1"));
        var statement = Assert.Single(_store.Restrictions(_mina));
        Assert.Equal((number.Value, RestrictionKind.Photo, "photo-1", "It shows a person who objected.", "Section 7 of the terms", true), (statement.Number, statement.Kind, statement.Target, statement.Reason, statement.Ground, statement.Noticed));
        Assert.Empty(_store.Restrictions(_sara));
        Assert.Equal("Mina", Assert.Single(_store.AllRestrictions()).Account);

        Assert.True(_store.Lift(number.Value));
        Assert.False(_store.Lift(number.Value));
        Assert.NotNull(_store.Photo(null, "photo-1"));
        Assert.Empty(_store.Restrictions(_mina));
    }

    [Fact]
    public void A_restricted_tree_is_not_public_and_cannot_be_made_public()
    {
        var tree = _store.AddTree(_mina, "The Rad family")!;
        _store.AddPerson(_mina, tree, "Zal", 1870, 1950, deceased: true);
        Assert.True(_store.AskPublication(_mina, tree));
        Assert.True(_store.Publish(_mina, tree));
        Assert.NotNull(_store.PublicTree(tree));

        var number = _store.Restrict(RestrictionKind.Tree, tree, "It names a living person as deceased.", "Section 7 of the terms", noticed: false)!.Value;

        Assert.Null(_store.PublicTree(tree));
        Assert.NotNull(_store.Tree(_mina, tree));
        Assert.Contains(_store.Tree(_mina, tree)!.History, change => change.What == "operator-restricted" && change.By is null);

        Assert.True(_store.Lift(number));
        Assert.NotNull(_store.PublicTree(tree));
    }

    [Fact]
    public void An_account_the_operator_closed_signs_in_nowhere_and_tells_only_its_owner_why()
    {
        var number = _store.Restrict(RestrictionKind.Account, "mina", "Repeated breaches after two decisions.", "Section 10 of the terms", noticed: false)!.Value;

        Assert.Null(_store.SignIn("Mina", Password));
        Assert.Null(_store.ClosedByOperator("Mina", "not her pass phrase"));
        Assert.Null(_store.ClosedByOperator("Sara", Password));
        var statement = _store.ClosedByOperator("Mina", Password)!;
        Assert.Equal((number, RestrictionKind.Account, "Repeated breaches after two decisions."), (statement.Number, statement.Kind, statement.Reason));
        Assert.Equal(statement, _store.Restriction(number));

        // Signing in does not open it again, as it would an account its visitor closed.
        Assert.Null(_store.SignIn("Mina", Password));
        Assert.True(_store.Lift(number));
        Assert.NotNull(_store.SignIn("Mina", Password));
        Assert.Null(_store.ClosedByOperator("Mina", Password));
        Assert.Null(_store.Restriction(number));
    }

    [Theory]
    [InlineData(RestrictionKind.Photo, "no-such-photo", "A reason.", "A ground.")]
    [InlineData(RestrictionKind.Tree, "no-such-tree", "A reason.", "A ground.")]
    [InlineData(RestrictionKind.Account, "Nobody", "A reason.", "A ground.")]
    [InlineData(RestrictionKind.Account, "Mina", "", "A ground.")]
    [InlineData(RestrictionKind.Account, "Mina", "A reason.", " ")]
    public void Nothing_is_restricted_without_something_to_restrict_and_the_words_for_it(RestrictionKind kind, string target, string reason, string ground)
    {
        Assert.Null(_store.Restrict(kind, target, reason, ground, noticed: false));
        Assert.Empty(_store.AllRestrictions());
        Assert.NotNull(_store.SignIn("Mina", Password));
    }

    [Fact]
    public void The_operator_takes_the_record_of_a_person_out_of_a_tree_and_the_history_does_not_say_the_name()
    {
        var tree = _store.AddTree(_mina, "The Rad family")!;
        var zal = _store.AddPerson(_mina, tree, "Zal", 1870, 1950, deceased: true)!;
        var rudabeh = _store.AddPerson(_mina, tree, "Rudabeh Example", 1980, null, deceased: false)!;
        Assert.Equal("done", _store.Link(_mina, tree, "parent", zal, rudabeh));
        Assert.Equal(2, _store.PersonsOfTree(tree).Count);

        Assert.False(_store.RemovePersonByOperator(tree, "no-such-person"));
        Assert.True(_store.RemovePersonByOperator(tree, rudabeh));

        var view = _store.Tree(_mina, tree)!;
        Assert.Equal("Zal", Assert.Single(_store.PersonsOfTree(tree)).Name);
        Assert.Empty(view.Links);
        var line = Assert.Single(view.History, change => change.What == "operator-person-removed");
        Assert.Equal((null, string.Empty), (line.By, line.Subject));
    }

    [Fact]
    public void An_account_notes_which_terms_it_read_and_reads_newer_ones_by_its_own_act_only()
    {
        Assert.Equal("2026-11-19", _store.TermsOf(_mina));
        Assert.Null(_store.TermsOf(_sara));

        Assert.True(_store.AcceptTerms(_sara, "2027-01-01"));
        Assert.Equal("2027-01-01", _store.TermsOf(_sara));
        Assert.False(_store.AcceptTerms("no-such-account", "2027-01-01"));
        Assert.False(_store.AcceptTerms(_sara, new string('1', 21)));
        Assert.Equal("2026-11-19", _store.TermsOf(_mina));
    }
}
