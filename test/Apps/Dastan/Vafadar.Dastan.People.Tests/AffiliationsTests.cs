using Vafadar.Dastan.Core;
using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;
using Vafadar.Temporal;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// Affiliations and being found through them (step 19.6, ADR 0018, PRIV-010 to PRIV-012): an account says
/// where it was, what it was there and when; who sees that and may find the account through the place is its
/// own choice, apart for the affiliation and for its dates; and what a viewer may not find says nothing – not
/// by a line, a count or an absence.
/// </summary>
public sealed class AffiliationsTests : IDisposable
{
    private const string Password = "a long pass phrase";
    private const string School = "School of Kabul";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;

    public AffiliationsTests()
    {
        _store = PeopleStore.Open(TestDatabase.For(Path.Combine(_folder, "people.db")), _clock);
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

    private long At(string account, string place, int? since = null, int? until = null, int? graduated = null, string? role = null, ScopeKind findable = ScopeKind.Public, ScopeKind dates = ScopeKind.Public)
    {
        var number = _store.AddAffiliation(account, new(place, role, since is { } from ? Year(from) : null, until is { } to ? Year(to) : null, graduated is { } done ? Year(done) : null))!.Value;
        Assert.True(_store.ShowAffiliation(account, number, findable, null, dates, null));
        return number;
    }

    private string[] Found(string viewer, string place, YearOfEra? year = null, AttendanceQuestion question = AttendanceQuestion.AttendedDuring) =>
        [.. _store.FindThrough(viewer, place, year, question).Select(found => found.Person.Name)];

    [Fact]
    public void An_affiliation_is_shown_to_nobody_and_found_by_nobody_until_its_account_says_so()
    {
        var number = _store.AddAffiliation(_mina, new(" School of Kabul ", "student", Year(1998), Year(2002)))!.Value;

        var mine = Assert.Single(_store.Affiliations(_mina));
        Assert.Equal((School, "student", ScopeKind.Private, ScopeKind.Private), (mine.Terms.Organisation, mine.Terms.Role, mine.Findable, mine.Dates));
        Assert.Empty(Found(_sara, School));
        Assert.Empty(_store.Profile(_sara, _mina)!.Affiliations);
        Assert.Single(_store.Profile(_mina, _mina)!.Affiliations);

        Assert.True(_store.ShowAffiliation(_mina, number, ScopeKind.Public, null, ScopeKind.Private, null));

        // Found through the place – however its name is written – and shown on the page, without the dates.
        Assert.Equal(["Mina"], Found(_sara, "school of kabul"));
        var shown = Assert.Single(_store.Profile(null, _mina)!.Affiliations);
        Assert.Equal((School, "student", null, false), (shown.Terms.Organisation, shown.Terms.Role, shown.Terms.Since, shown.DatesShown));
        Assert.Equal(Year(1998), Assert.Single(_store.Profile(_mina, _mina)!.Affiliations).Terms.Since);
    }

    [Fact]
    public void A_name_that_may_be_found_is_no_consent_to_be_found_by_a_year()
    {
        At(_mina, School, 1998, 2002, dates: ScopeKind.Private);
        At(_dara, School, 1999, 2003);

        Assert.Equal(["Dara", "Mina"], Found(_sara, School));

        // Asked with a year, only who lets this viewer see the dates is asked at all: Mina is not in the
        // list for a year she was there, and so the list says nothing about when she was.
        Assert.Equal(["Dara"], Found(_sara, School, In(2000)));
        Assert.Empty(Found(_sara, School, In(1998)));
        Assert.Empty(Found(_sara, School, In(2010)));
    }

    [Fact]
    public void Each_question_about_a_year_has_its_own_meaning()
    {
        At(_mina, School, 1998, 2002, graduated: 2002);
        At(_dara, School, 2000, 2004);

        Assert.Equal(["Dara", "Mina"], Found(_sara, School, In(2001)));
        Assert.Equal(["Mina"], Found(_sara, School, In(1998), AttendanceQuestion.EnteredIn));
        Assert.Equal(["Dara"], Found(_sara, School, In(2000), AttendanceQuestion.EnteredIn));
        Assert.Equal(["Mina"], Found(_sara, School, In(2002), AttendanceQuestion.GraduatedIn));

        // An end is not a graduation: Dara left in 2004 and no record says that he graduated.
        Assert.Empty(Found(_sara, School, In(2004), AttendanceQuestion.GraduatedIn));
    }

    [Fact]
    public void A_year_is_compared_in_the_calendar_it_is_asked_in_and_across_calendars_says_possibly()
    {
        static Occurrence Solar(int year) => new(CalendarPosition.OfYear(CalendarSystem.SolarHijri, Era.SolarHijri, year));
        var mina = _store.AddAffiliation(_mina, new(School, null, Solar(1377), Solar(1381), Solar(1381)))!.Value;
        Assert.True(_store.ShowAffiliation(_mina, mina, ScopeKind.Public, null, ScopeKind.Public, null));
        At(_dara, School, 1998, 2002);

        // Asked in the Solar Hijri calendar: Mina's years are compared as they are written – exactly.
        var found = _store.FindThrough(_sara, School, new YearOfEra(Era.SolarHijri, 1379)).ToDictionary(person => person.Person.Name, person => person.Holding);
        Assert.Equal(Holding.Yes, found["Mina"]);
        Assert.Equal(["Mina"], _store.FindThrough(_sara, School, new YearOfEra(Era.SolarHijri, 1377), AttendanceQuestion.EnteredIn).Where(person => person.Holding == Holding.Yes).Select(person => person.Person.Name));
        Assert.Equal(["Mina"], Found(_sara, School, new YearOfEra(Era.SolarHijri, 1381), AttendanceQuestion.GraduatedIn));
        Assert.DoesNotContain("Mina", Found(_sara, School, new YearOfEra(Era.SolarHijri, 1382)));

        // Dara wrote years of the common era: 1379 lies in 2000 and 2001, where he was – a certain yes; 1376
        // lies in 1997 and 1998, of which he was there in one – possibly, and never a plain yes.
        Assert.Equal(Holding.Yes, found["Dara"]);
        Assert.Equal(Holding.Possibly, _store.FindThrough(_sara, School, new YearOfEra(Era.SolarHijri, 1376)).Single(person => person.Person.Name == "Dara").Holding);
        Assert.DoesNotContain("Dara", Found(_sara, School, new YearOfEra(Era.SolarHijri, 1385)));

        // Asked in the common era, Mina's Solar Hijri years are what they can be: 2000 is inside for certain,
        // 1998 is where her first year begins – possibly.
        Assert.Equal(Holding.Yes, _store.FindThrough(_sara, School, In(2000)).Single(person => person.Person.Name == "Mina").Holding);
        Assert.Equal(Holding.Possibly, _store.FindThrough(_sara, School, In(1998)).Single(person => person.Person.Name == "Mina").Holding);
        Assert.DoesNotContain("Mina", Found(_sara, School, In(1997)));
    }

    [Fact]
    public void A_circle_finds_who_shows_an_affiliation_to_it_and_nobody_else_does()
    {
        Assert.Equal("asked", _store.Ask(_mina, "Sara", "friend"));
        Assert.True(_store.Answer(_sara, _store.Relationships(_sara).Single().Id, accept: true));
        var circle = _store.AddCircle(_mina, "Close")!.Value;
        _store.SetCircle(_mina, circle, [_sara]);
        var number = _store.AddAffiliation(_mina, new(School, null, Year(1998), Year(2002)))!.Value;

        // Somebody else's circle is no audience of mine.
        Assert.False(_store.ShowAffiliation(_dara, number, ScopeKind.Public, null, ScopeKind.Public, null));
        Assert.True(_store.ShowAffiliation(_mina, number, ScopeKind.Circle, circle, ScopeKind.Circle, circle));

        Assert.Equal(["Mina"], Found(_sara, School, In(2000)));
        Assert.Empty(Found(_dara, School));
        Assert.Empty(_store.Profile(_dara, _mina)!.Affiliations);
        Assert.Empty(_store.Profile(null, _mina)!.Affiliations);

        // The circle is removed: what was shown to it is shown to its account only – never to more.
        Assert.True(_store.RemoveCircle(_mina, circle));
        Assert.Empty(Found(_sara, School));
        Assert.Equal((ScopeKind.Private, ScopeKind.Private), (_store.Affiliations(_mina).Single().Findable, _store.Affiliations(_mina).Single().Dates));
    }

    [Fact]
    public void The_dates_are_never_shown_to_more_people_than_the_affiliation()
    {
        var number = _store.AddAffiliation(_mina, new(School, null, Year(1998), Year(2002)))!.Value;

        Assert.True(_store.ShowAffiliation(_mina, number, ScopeKind.Private, null, ScopeKind.Public, null));

        var mine = _store.Affiliations(_mina).Single();
        Assert.Equal((ScopeKind.Private, ScopeKind.Private), (mine.Findable, mine.Dates));
        Assert.Empty(Found(_sara, School, In(2000)));
    }

    [Fact]
    public void Who_blocks_or_is_blocked_is_not_found_and_nobody_finds_themself()
    {
        At(_mina, School, 1998, 2002);
        At(_sara, School, 1998, 2002);
        At(_dara, School, 1998, 2002);

        Assert.Equal(["Dara", "Sara"], Found(_mina, School, In(2000)));
        Assert.True(_store.Block(_sara, _mina));

        Assert.Equal(["Dara"], Found(_mina, School, In(2000)));
        Assert.Equal(["Dara"], Found(_sara, School));
        Assert.Equal(["Mina", "Sara"], Found(_dara, School));

        // An account that is closed is found by nobody.
        Assert.True(_store.Close(_dara, Password).Done);
        Assert.Empty(Found(_mina, School));
    }

    [Theory]
    [InlineData("", null, 1998, 2002, null)]
    [InlineData("School of Kabul", null, 2002, 1998, null)]
    [InlineData("School of Kabul", null, 1998, 2002, 1990)]
    [InlineData("Two\nlines", null, 1998, 2002, null)]
    public void An_affiliation_that_says_something_impossible_is_not_kept(string place, string? role, int since, int until, int? graduated)
    {
        Assert.Null(_store.AddAffiliation(_mina, new(place, role, Year(since), Year(until), graduated is { } done ? Year(done) : null)));
        Assert.Null(_store.AddAffiliation(_mina, new(new string('a', 161))));
        Assert.Null(_store.AddAffiliation(_mina, new(School, new string('a', 121))));
        Assert.Empty(_store.Affiliations(_mina));
    }

    [Fact]
    public void An_affiliation_is_changed_and_removed_by_its_account_only_and_stated_once()
    {
        var number = At(_mina, School, 1998, 2002, role: "student");

        // The same place, role and times once more is the same statement, however the place is written; another
        // time at the same place in the same role is a stretch of its own (step 20.4, InstitutionsTests).
        Assert.Null(_store.AddAffiliation(_mina, new("school of kabul", "student", Year(1998), Year(2002))));
        Assert.NotNull(_store.AddAffiliation(_mina, new(School, "teacher", Year(2010))));
        Assert.False(_store.ChangeAffiliation(_sara, number, new("Somewhere else")));
        Assert.False(_store.RemoveAffiliation(_sara, number));
        Assert.False(_store.ChangeAffiliation(_mina, number, new(School, "teacher", Year(2010))));

        Assert.True(_store.ChangeAffiliation(_mina, number, new("Kabul Books", "typesetter", Year(2003))));

        // Who may find it stays as it was chosen; the place it is found through is the new one.
        Assert.Empty(Found(_sara, School));
        var found = Assert.Single(_store.FindThrough(_sara, "Kabul Books"));
        Assert.Equal(("Mina", "typesetter", Year(2003), true), (found.Person.Name, found.Role, found.Shown.Terms.Since, found.Shown.DatesShown));

        Assert.True(_store.RemoveAffiliation(_mina, number));
        Assert.Empty(Found(_sara, "Kabul Books"));
    }

    [Fact]
    public void The_copy_of_my_data_says_where_i_was_and_who_may_find_me_there()
    {
        At(_mina, School, 1998, 2002, role: "student", dates: ScopeKind.Private);

        var written = Assert.Single(_store.Export(_mina, Password)!["affiliations"]!.AsArray())!;

        Assert.Equal((School, "student", "public", "private"), ((string?)written["place"], (string?)written["as"], (string?)written["foundBy"], (string?)written["datesSeenBy"]));
        Assert.Equal(TemporalJson.Serialize(Year(1998)), written["since"]!.ToJsonString());
    }
}
