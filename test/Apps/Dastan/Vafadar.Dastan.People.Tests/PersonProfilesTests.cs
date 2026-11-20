using Vafadar.Dastan.Core.Personal;
using Vafadar.Dastan.Tests;
using Vafadar.Temporal;

namespace Vafadar.Dastan.People.Tests;

/// <summary>
/// The profile of a person of a family tree (step 19.5, ADR 0017, PRIV-003): a record states times, not bare
/// years; where somebody was born, a few words and a picture are kept of somebody who died, of somebody who
/// says so themself and of a child whose parent decides – and of nobody else who lives.
/// </summary>
public sealed class PersonProfilesTests : IDisposable
{
    private const string Password = "a long pass phrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dastan-people-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero));
    private readonly PeopleStore _store;
    private readonly string _mina;
    private readonly string _sara;
    private readonly string _dara;
    private readonly string _tree;

    public PersonProfilesTests()
    {
        _store = PeopleStore.Open(TestDatabase.For(Path.Combine(_folder, "people.db")), _clock);
        _mina = _store.Register("Mina", Password).Session!.Id;
        _sara = _store.Register("Sara", Password).Session!.Id;
        _dara = _store.Register("Dara", Password).Session!.Id;
        _tree = _store.AddTree(_mina, "The Rad family")!;
        Friends(_mina, _sara, "Sara");
        Friends(_mina, _dara, "Dara");
        _store.SetTreeMember(_mina, _tree, _sara, TreeRole.Viewer);
        _store.SetTreeMember(_mina, _tree, _dara, TreeRole.Editor);
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private void Friends(string from, string to, string toName)
    {
        Assert.Equal("asked", _store.Ask(from, toName, "family"));
        Assert.True(_store.Answer(to, _store.Relationships(to).Single(relationship => relationship.Other.Id == from).Id, accept: true));
    }

    private static Occurrence Day(int year, int month, int day) => new(CalendarPosition.OfDay(CalendarSystem.Gregorian, Era.CommonEra, year, month, day));

    private TreePerson Seen(string viewer, string person) => _store.Tree(viewer, _tree)!.Person(person);

    private string Photo(string owner, string id, ScopeKind shown)
    {
        Assert.True(_store.AddPhoto(owner, id, new PhotoFile($"{id}.jpg", "image/jpeg", 800, 600, 12345), "A portrait", null, shown));
        return id;
    }

    // Sara says the record is her, and Mina – who made the record – confirms it.
    private string Herself()
    {
        var record = _store.AddPerson(_mina, _tree, "Sara Rad", 1990, null, deceased: false)!;
        Assert.Equal("claimed", _store.Claim(_sara, _tree, record));
        Assert.True(_store.ReviewClaim(_mina, _tree, _store.Tree(_mina, _tree)!.Claims.Single().Number, confirm: true));
        return record;
    }

    [Fact]
    public void A_record_states_a_time_with_what_is_uncertain_about_it()
    {
        var about = new Occurrence(CalendarPosition.OfYear(CalendarSystem.SolarHijri, Era.SolarHijri, 1250).Approximately());
        var between = new OccurrenceWindow(CalendarPosition.OfYear(CalendarSystem.SolarHijri, Era.SolarHijri, 1318), CalendarPosition.OfYear(CalendarSystem.SolarHijri, Era.SolarHijri, 1320));

        var zal = _store.AddPerson(_mina, _tree, "Zal", about, between, deceased: false)!;
        var sam = _store.AddPerson(_mina, _tree, "Sam", new UnknownTime(), Day(1900, 3, 21), deceased: true)!;

        // A time comes back as it was stated: in its calendar, about or between, and "not known" as that.
        Assert.Equal((about, between, true), (Seen(_sara, zal).Born, Seen(_sara, zal).Died, Seen(_sara, zal).Deceased));
        Assert.Equal((new UnknownTime(), Day(1900, 3, 21)), (Seen(_sara, sam).Born, Seen(_sara, sam).Died));
    }

    [Fact]
    public void Nobody_dies_before_they_are_born_in_whatever_calendar_the_times_are_written()
    {
        var born = new Occurrence(CalendarPosition.OfYear(CalendarSystem.SolarHijri, Era.SolarHijri, 1300));

        // 1300 of the solar calendar is 1921 or 1922 of the common era.
        Assert.Null(_store.AddPerson(_mina, _tree, "Zal", born, TestPersons.Year(1900), deceased: true));
        Assert.NotNull(_store.AddPerson(_mina, _tree, "Zal", born, TestPersons.Year(1921), deceased: true));

        // In one calendar the month and the day count where both times say them.
        Assert.Null(_store.AddPerson(_mina, _tree, "Sam", Day(1900, 5, 2), Day(1900, 5, 1), deceased: true));
        Assert.NotNull(_store.AddPerson(_mina, _tree, "Sam", Day(1900, 5, 2), TestPersons.Year(1900), deceased: true));

        // A time of another shape – something that lasted – is no time of a birth.
        var lasted = new Period(PeriodStart.At(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, 1900)), PeriodEnd.Ongoing);
        Assert.Null(_store.AddPerson(_mina, _tree, "Sam", lasted, null, deceased: true));
    }

    [Fact]
    public void The_day_somebody_who_lives_was_born_is_theirs_to_say()
    {
        var record = Herself();

        // Of somebody who lives, a record made or changed by somebody else says the year.
        Assert.Null(_store.AddPerson(_mina, _tree, "Somebody living", Day(1985, 5, 1), null, deceased: false));
        Assert.False(_store.ChangePerson(_dara, _tree, record, "Sara Rad", Day(1990, 2, 3), null, deceased: false));
        Assert.NotNull(_store.AddPerson(_mina, _tree, "Somebody who died", Day(1885, 5, 1), null, deceased: true));

        // The account the record is may say the day – once it may change the tree at all.
        _store.SetTreeMember(_mina, _tree, _sara, TreeRole.Editor);
        Assert.True(_store.ChangePerson(_sara, _tree, record, "Sara Rad", Day(1990, 2, 3), null, deceased: false));
        Assert.Equal(Day(1990, 2, 3), Seen(_mina, record).Born);

        // Somebody else who changes the name leaves the day as it stands, and cannot move it.
        Assert.True(_store.ChangePerson(_dara, _tree, record, "Sara R.", Day(1990, 2, 3), null, deceased: false));
        Assert.False(_store.ChangePerson(_dara, _tree, record, "Sara R.", Day(1990, 2, 4), null, deceased: false));
    }

    [Fact]
    public void Of_somebody_who_lives_and_was_not_asked_nothing_else_is_kept()
    {
        var living = _store.AddPerson(_mina, _tree, "Somebody living", 1980, null, deceased: false)!;

        Assert.Equal("not-asked", _store.DescribePerson(_mina, _tree, living, "Zabol", null, "Likes horses.", null));

        var seen = Seen(_mina, living);
        Assert.True(seen.NotAsked);
        Assert.False(seen.MayDescribe);
        Assert.Equal((null, null, null), (seen.BirthPlace, seen.About, seen.Picture));
    }

    [Fact]
    public void Of_somebody_who_died_who_may_change_the_tree_says_what_the_family_knows()
    {
        var zal = _store.AddPerson(_mina, _tree, "Zal", 1870, 1950, deceased: true)!;
        var portrait = Photo(_dara, "portrait-of-zal", ScopeKind.Private);

        // Who may only see the tree does not write in it.
        Assert.Equal("refused", _store.DescribePerson(_sara, _tree, zal, "Zabol", null, null, null));
        Assert.Equal("done", _store.DescribePerson(_dara, _tree, zal, " Zabol ", "Kabul", "He raised Rostam.\r\nHe had white hair.", portrait));

        var seen = Seen(_sara, zal);
        Assert.Equal(("Zabol", "Kabul", "He raised Rostam.\nHe had white hair."), (seen.BirthPlace, seen.DeathPlace, seen.About));
        Assert.False(seen.NotAsked);
        Assert.False(seen.MayDescribe);
        Assert.True(Seen(_mina, zal).MayDescribe);
        Assert.Equal("person-described", _store.Tree(_mina, _tree)!.History[0].What);
    }

    [Fact]
    public void A_picture_is_one_of_the_photographs_of_who_chose_it_and_is_seen_by_who_may_see_that_photograph()
    {
        var zal = _store.AddPerson(_mina, _tree, "Zal", 1870, 1950, deceased: true)!;
        var portrait = Photo(_dara, "portrait-of-zal", ScopeKind.Private);

        // Nobody makes somebody else's photograph the picture of a person, and no photograph that is not there.
        Assert.Equal("picture", _store.DescribePerson(_mina, _tree, zal, null, null, null, portrait));
        Assert.Equal("picture", _store.DescribePerson(_dara, _tree, zal, null, null, null, "no-such-photograph"));
        Assert.Equal("done", _store.DescribePerson(_dara, _tree, zal, null, null, null, portrait));

        // The photograph is Dara's alone: the tree shows it to Dara and to nobody else.
        Assert.Equal(portrait, Seen(_dara, zal).Picture?.Id);
        Assert.Null(Seen(_mina, zal).Picture);

        // Somebody else who writes about Zal leaves the picture where it is by sending it back.
        Assert.Equal("done", _store.DescribePerson(_mina, _tree, zal, "Zabol", null, null, portrait));

        // Shown to everybody, it is the picture for everybody who sees the tree.
        Assert.True(_store.SharePhoto(_dara, portrait, ScopeKind.Public));
        Assert.Equal(portrait, Seen(_sara, zal).Picture?.Id);

        // Removed, it is no picture any more – the record does not break.
        Assert.NotNull(_store.RemovePhoto(_dara, portrait));
        Assert.Null(Seen(_dara, zal).Picture);
    }

    [Fact]
    public void Somebody_who_says_the_record_is_them_says_the_rest_themself_and_takes_it_along_when_they_go()
    {
        var record = Herself();

        // The record is an account: nobody writes about that person but the person – not even the owner.
        Assert.Equal("refused", _store.DescribePerson(_mina, _tree, record, "Shiraz", null, null, null));
        Assert.Equal("done", _store.DescribePerson(_sara, _tree, record, "Shiraz", null, "I draw trees.", null));
        Assert.Equal(("Shiraz", "I draw trees."), (Seen(_dara, record).BirthPlace, Seen(_dara, record).About));
        Assert.True(Seen(_sara, record).MayDescribe);
        Assert.False(Seen(_mina, record).MayDescribe);

        // She is not the record after all: what she said about herself is not kept of a person nobody asked.
        Assert.True(_store.Unclaim(_sara, _tree, record));
        var after = Seen(_mina, record);
        Assert.True(after.NotAsked);
        Assert.Equal((null, null), (after.BirthPlace, after.About));
    }

    [Fact]
    public void What_is_kept_about_a_child_is_decided_by_an_account_that_is_its_parent_in_the_tree()
    {
        var mother = Herself();
        var child = _store.AddPerson(_mina, _tree, "Little Rad", 2015, null, deceased: false)!;
        var grown = _store.AddPerson(_mina, _tree, "Grown Rad", 2005, null, deceased: false)!;

        // Until the tree says whose child it is, nobody decides – not the owner either.
        Assert.Equal("refused", _store.DescribePerson(_mina, _tree, child, "Tehran", null, null, null));
        Assert.Equal("refused", _store.DescribePerson(_sara, _tree, child, "Tehran", null, null, null));
        Assert.False(Seen(_mina, child).NotAsked);

        Assert.Equal("done", _store.Link(_mina, _tree, "parent", mother, child));
        Assert.Equal("done", _store.Link(_mina, _tree, "parent", mother, grown));

        Assert.Equal("done", _store.DescribePerson(_sara, _tree, child, "Tehran", null, "Plays the violin.", null));
        Assert.Equal("refused", _store.DescribePerson(_mina, _tree, child, "Tabriz", null, null, null));
        Assert.Equal("Tehran", Seen(_dara, child).BirthPlace);
        Assert.True(Seen(_sara, child).MayDescribe);

        // Somebody of twenty-one is no child: nobody but they themselves says more.
        Assert.Equal("not-asked", _store.DescribePerson(_sara, _tree, grown, "Tehran", null, null, null));

        // Years pass: the child is grown, and what a parent wrote is not shown as theirs to keep.
        _clock.Now = _clock.Now.AddYears(10);
        var later = Seen(_sara, child);
        Assert.True(later.NotAsked);
        Assert.False(later.MayDescribe);
        Assert.Null(later.BirthPlace);
    }

    [Theory]
    [InlineData(201, 10, "long")]
    [InlineData(10, 1001, "long")]
    [InlineData(200, 1000, "done")]
    public void A_place_and_the_words_about_a_person_have_a_length(int place, int words, string answer)
    {
        var zal = _store.AddPerson(_mina, _tree, "Zal", 1870, 1950, deceased: true)!;

        Assert.Equal(answer, _store.DescribePerson(_mina, _tree, zal, new string('a', place), null, new string('b', words), null));
        Assert.Equal("long", _store.DescribePerson(_mina, _tree, zal, "Two\nlines", null, null, null));
    }

    [Fact]
    public void A_tree_made_public_shows_the_picture_of_a_person_only_when_that_photograph_is_public()
    {
        var zal = _store.AddPerson(_mina, _tree, "Zal", 1870, 1950, deceased: true)!;
        var portrait = Photo(_mina, "portrait-of-zal", ScopeKind.Private);
        Assert.Equal("done", _store.DescribePerson(_mina, _tree, zal, "Zabol", null, "He raised Rostam.", portrait));
        Assert.True(_store.AskPublication(_mina, _tree));
        Assert.True(_store.Publish(_mina, _tree));

        var shown = _store.PublicTree(_tree)!.Person(zal);
        Assert.Equal(("Zabol", "He raised Rostam.", false), (shown.BirthPlace, shown.About, shown.MayDescribe));
        Assert.Null(shown.Picture);

        Assert.True(_store.SharePhoto(_mina, portrait, ScopeKind.Public));
        Assert.Equal(portrait, _store.PublicTree(_tree)!.Person(zal).Picture?.Id);
    }

    [Fact]
    public void The_copy_of_my_data_says_the_times_and_the_profile_of_the_people_of_my_trees()
    {
        var zal = _store.AddPerson(_mina, _tree, "Zal", new Occurrence(CalendarPosition.OfYear(CalendarSystem.SolarHijri, Era.SolarHijri, 1250).Approximately()), null, deceased: true)!;
        Assert.Equal("done", _store.DescribePerson(_mina, _tree, zal, "Zabol", null, "He raised Rostam.", null));

        var person = Assert.Single(Assert.Single(_store.Export(_mina, Password)!["trees"]!.AsArray())!["people"]!.AsArray())!;

        Assert.Equal(("Zabol", "He raised Rostam.", false), ((string?)person["bornAt"], (string?)person["about"], (bool)person["hasPicture"]!));
        Assert.Equal(TemporalJson.Serialize(Seen(_mina, zal).Born!), person["born"]!.ToJsonString());
    }
}
