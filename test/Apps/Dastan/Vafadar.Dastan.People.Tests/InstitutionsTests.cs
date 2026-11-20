using Vafadar.Dastan.Core;
using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;
using Vafadar.Temporal;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// A place an account was at as an entity of a collection, and a time there as a stretch of its own (step
/// 20.4, ADR 0023): an affiliation can point at an entity; the words stay and are allowed alone; a name never
/// makes two places one; somebody is at one place twice, with a gap between that stays a gap. The places are
/// made up.
/// </summary>
public sealed class InstitutionsTests : IDisposable
{
    private const string Password = "a long pass phrase";

    // Two universities of one name in two towns, and the first of them as another collection knows it.
    private static readonly EntityReference Kabul = new("universities", new EntityId("org-school-kabul"));
    private static readonly EntityReference Herat = new("universities", new EntityId("org-school-herat"));
    private static readonly EntityReference KabulElsewhere = new("history-of-kabul", new EntityId("org-school"));

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero));
    private readonly string _database;
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;

    public InstitutionsTests()
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

    private static Occurrence Year(int year) => new(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, year));

    private static YearOfEra In(int year) => new(Era.CommonEra, year);

    [Fact]
    public void Somebody_is_at_one_place_twice_and_the_gap_between_stays_a_gap()
    {
        var first = _store.AddAffiliation(_mina, new("The School", "teacher", Year(1998), Year(2002)));
        var second = _store.AddAffiliation(_mina, new("The School", "teacher", Year(2010), Year(2014)));

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(2, _store.Affiliations(_mina).Count);

        // The same statement twice is one statement.
        Assert.Null(_store.AddAffiliation(_mina, new(" the school ", "teacher", Year(1998), Year(2002))));
        Assert.False(_store.ChangeAffiliation(_mina, second!.Value, new("The School", "teacher", Year(1998), Year(2002))));

        Assert.True(_store.ShowAffiliation(_mina, first!.Value, ScopeKind.Public, null, ScopeKind.Public, null));
        Assert.True(_store.ShowAffiliation(_mina, second.Value, ScopeKind.Public, null, ScopeKind.Public, null));

        // Asked for a year of either stretch she is found; asked for a year between, she is not – nothing
        // makes one long attendance of two.
        Assert.Single(_store.FindThrough(_sara, "The School", In(2000)));
        Assert.Single(_store.FindThrough(_sara, "The School", In(2012)));
        Assert.Empty(_store.FindThrough(_sara, "The School", In(2006)));
    }

    private long At(string account, string words, EntityReference? place, int? since = null, int? until = null, ScopeKind findable = ScopeKind.Public, ScopeKind dates = ScopeKind.Public, string? role = null)
    {
        var number = _store.AddAffiliation(account, new(words, role, since is { } from ? Year(from) : null, until is { } to ? Year(to) : null, Institution: place))!.Value;
        Assert.True(_store.ShowAffiliation(account, number, findable, null, dates, null));
        return number;
    }

    private string[] Found(string viewer, YearOfEra? year, params EntityReference[] place) =>
        [.. _store.FindThrough(viewer, place, year).Select(found => $"{found.Person.Name}:{found.Holding}")];

    [Fact]
    public void A_place_is_found_by_what_it_is_and_never_by_what_it_is_called()
    {
        // Two schools of one name in two towns; Mina was at one, Sara at the other, Dara wrote the name only.
        At(_mina, "The School", Kabul, 1998, 2002);
        At(_sara, "The School", Herat, 1998, 2002);
        At(_dara, "The School", null, 1998, 2002);

        using var viewer = PeopleStore.Open(_database, _clock);
        var asks = viewer.Register("Asker", Password).Session!.Id;

        Assert.Equal(["Mina:Yes"], Found(asks, null, Kabul));
        Assert.Equal(["Sara:Yes"], Found(asks, null, Herat));
        Assert.Empty(Found(asks, null, KabulElsewhere));
        Assert.Empty(_store.FindThrough(asks, Array.Empty<EntityReference>()));

        // The words find all three, as words do; they decide nothing about which school it was.
        Assert.Equal(["Dara", "Mina", "Sara"], _store.FindThrough(asks, "the school").Select(found => found.Person.Name));

        // What the affiliation says comes back with the entity it points at, and the words it was written in.
        var mine = Assert.Single(_store.Affiliations(_mina));
        Assert.Equal((Kabul, "The School"), (mine.Terms.Institution, mine.Terms.Organisation));
        Assert.Null(Assert.Single(_store.Affiliations(_dara)).Terms.Institution);
    }

    [Fact]
    public void An_entity_the_editors_call_the_same_in_another_collection_is_one_place_and_a_new_name_changes_nothing()
    {
        At(_mina, "The School", Kabul, 1998, 2002);
        At(_sara, "Kabul School of Letters", KabulElsewhere, 2000, 2004);

        // Asked for one entity, only who points at it; asked with what the editors say is the same, both.
        Assert.Equal(["Sara:Yes"], Found(_dara, null, KabulElsewhere));
        Assert.Equal(["Mina:Yes", "Sara:Yes"], Found(_dara, null, Kabul, KabulElsewhere));
        Assert.Equal(["Mina:Yes"], Found(_dara, In(1999), Kabul, KabulElsewhere));

        // The place is given another name – in the collection, and on the affiliation: found as before.
        var number = _store.Affiliations(_mina).Single().Number;
        Assert.True(_store.ChangeAffiliation(_mina, number, new("Kabul University", null, Year(1998), Year(2002), Institution: Kabul)));
        Assert.Equal(["Mina:Yes"], Found(_dara, null, Kabul));
        Assert.Empty(_store.FindThrough(_dara, "The School"));
    }

    [Fact]
    public void Two_stretches_at_one_entity_are_two_and_only_the_same_statement_is_one()
    {
        var first = At(_mina, "The School", Kabul, 1998, 2002, role: "teacher");
        var second = At(_mina, "The School", Kabul, 2010, 2014, role: "teacher");
        Assert.NotEqual(first, second);

        // The same entity, role and times once more – in other words – is the same statement.
        Assert.Null(_store.AddAffiliation(_mina, new("Kabul University", "teacher", Year(1998), Year(2002), Institution: Kabul)));

        // The same words, role and times at another entity, or at no entity, is another statement.
        Assert.NotNull(_store.AddAffiliation(_mina, new("The School", "teacher", Year(1998), Year(2002), Institution: Herat)));
        Assert.NotNull(_store.AddAffiliation(_mina, new("The School", "teacher", Year(1998), Year(2002))));

        Assert.Equal(["Mina:Yes"], Found(_sara, In(2000), Kabul));
        Assert.Equal(["Mina:Yes"], Found(_sara, In(2012), Kabul));
        Assert.Empty(Found(_sara, In(2006), Kabul));
    }

    [Fact]
    public void That_a_place_is_public_makes_nobodys_time_there_public()
    {
        At(_mina, "The School", Kabul, 1998, 2002, findable: ScopeKind.Private);
        At(_sara, "The School", Kabul, 1998, 2002, findable: ScopeKind.Public, dates: ScopeKind.Private);
        using var asker = PeopleStore.Open(_database, _clock);
        var asks = asker.Register("Asker", Password).Session!.Id;

        // Mina did not choose to be found; Sara is found, but not by a year, and her dates are not read.
        Assert.Equal(["Sara:Yes"], Found(asks, null, Kabul));
        Assert.Empty(Found(asks, In(2000), Kabul));
        var shown = Assert.Single(_store.FindThrough(asks, [Kabul])).Shown;
        Assert.False(shown.DatesShown);
        Assert.Null(shown.Terms.Since);

        // Blocked in either direction, nobody finds the other; a closed account is found by nobody.
        Assert.True(_store.Block(_sara, asks));
        Assert.Empty(Found(asks, null, Kabul));
        Assert.True(_store.Unblock(_sara, asks));
        Assert.True(_store.Close(_sara, Password).Done);
        Assert.Empty(Found(asks, null, Kabul));
    }

    [Fact]
    public void Colleagues_at_two_places_of_one_name_are_two_relationships_and_another_place_asks_the_other_side_again()
    {
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "colleague", new(Context: "The School", Place: Kabul)));
        var first = _store.Relationships(_sara).Single().Id;
        Assert.True(_store.Answer(_sara, first, accept: true));

        // The same kind at the same entity is there already, whatever it is called and whoever asks.
        Assert.Equal("exists", _store.Ask(_sara, "Mina", "colleague", new(Context: "Kabul University", Place: Kabul)));

        // The same words at another entity, and the same words without an entity, are other relationships.
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "colleague", new(Context: "The School", Place: Herat)));
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "colleague", new(Context: "The School")));

        // Pointing an accepted relationship at another entity changes what it says: the other side is asked,
        // and until it agrees the relationship holds as it was accepted.
        Assert.Equal("same", _store.AskChange(_mina, first, "colleague", new(Context: "The School", Place: Kabul)));
        Assert.Equal("asked", _store.AskChange(_mina, first, "colleague", new(Context: "The School", Place: KabulElsewhere)));
        var held = _store.Relationships(_mina).Single(relationship => relationship.Id == first);
        Assert.Equal((RelationshipState.Accepted, Kabul), (held.State, held.Terms.Place));

        var change = _store.Relationships(_sara).Single(relationship => relationship.Replaces == first).Id;
        Assert.True(_store.Answer(_sara, change, accept: true));
        Assert.Equal(KabulElsewhere, _store.Relationships(_mina).Single(relationship => relationship.Id == change).Terms.Place);
        Assert.DoesNotContain(_store.Relationships(_mina), relationship => relationship.Id == first);
    }

    [Fact]
    public void What_was_stated_before_places_could_be_entities_is_kept_as_it_was()
    {
        // The store as the version before left it, with an affiliation and a relationship written then.
        using (var context = new PeopleContext(PeopleStore.Options(_database)))
        {
            Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>(context).Migrate("20261120220630_AddressProof");
        }

        Write($"""
            insert into personal."Affiliations" ("Account", "Organisation", "OrganisationKey", "Role", "Since", "Until", "FindableScope", "DatesScope", "CreatedAt")
            values ('{_mina}', 'The School', 'THE SCHOOL', 'teacher', '{PersonTime.Write(Year(1998))}', '{PersonTime.Write(Year(2002))}', {(int)ScopeKind.Public}, {(int)ScopeKind.Private}, now());
            insert into personal."Relationships" ("Id", "From", "To", "Kind", "Reversed", "Context", "ContextKey", "State", "FromScope", "ToScope", "CreatedAt", "ChangedAt")
            values ('before', '{_mina}', '{_sara}', 'colleague', false, 'The School', 'THE SCHOOL', {(int)RelationshipState.Accepted}, 0, 0, now(), now());
            """);

        PeopleStore.Migrate(_database);

        var kept = Assert.Single(_store.Affiliations(_mina));
        Assert.Equal(("The School", "teacher", Year(1998), Year(2002), (EntityReference?)null), (kept.Terms.Organisation, kept.Terms.Role, kept.Terms.Since, kept.Terms.Until, kept.Terms.Institution));
        Assert.Equal((ScopeKind.Public, ScopeKind.Private), (kept.Findable, kept.Dates));
        var relationship = Assert.Single(_store.Relationships(_mina));
        Assert.Equal(("before", "colleague", "The School", (EntityReference?)null, RelationshipState.Accepted), (relationship.Id, relationship.Kind, relationship.Terms.Context, relationship.Terms.Place, relationship.State));

        // Nothing was pointed at an entity by itself: the words find it, an entity does not.
        Assert.Single(_store.FindThrough(_sara, "The School"));
        Assert.Empty(_store.FindThrough(_sara, [Kabul]));

        // And it can be given its entity afterwards, keeping everything else.
        Assert.True(_store.ChangeAffiliation(_mina, kept.Number, kept.Terms with { Institution = Kabul }));
        Assert.Single(_store.FindThrough(_sara, [Kabul]));
    }

    private void Write(string sql)
    {
        using var connection = new Npgsql.NpgsqlConnection(_database);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
