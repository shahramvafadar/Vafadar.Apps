using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Core.Personal;
using Vafadar.Temporal;

namespace Vafadar.Dastan.People;

/// <summary>What an account says about a place it was at: a school, a university, an employer, a team.</summary>
/// <param name="Organisation">The name of the place, in words.</param>
/// <param name="Role">What the person was there – "student of history", "typesetter" – or nothing.</param>
/// <param name="Since">From when, when that is stated.</param>
/// <param name="Until">Until when, when that is stated.</param>
/// <param name="Graduated">When the person graduated, when one is recorded. A statement of its own, not the end of the attendance.</param>
/// <param name="Institution">
/// The entity of a collection the place is, when the account chose one (step 20.4, ADR 0023). Without it the
/// words stand alone – allowed, and found by nobody who asks for an entity.
/// </param>
public sealed record AffiliationTerms(string Organisation, string? Role = null, TemporalExpression? Since = null, TemporalExpression? Until = null, TemporalExpression? Graduated = null, EntityReference? Institution = null);

/// <summary>An affiliation as its account sees it.</summary>
/// <param name="Number">The number of the affiliation in the store.</param>
/// <param name="Terms">What it says.</param>
/// <param name="Findable">Who sees it on the page of the account and may find the account through the place.</param>
/// <param name="FindableCircle">For a circle: which one.</param>
/// <param name="Dates">Who sees when – and so may find the account by a year.</param>
/// <param name="DatesCircle">For a circle: which one.</param>
public sealed record AffiliationInfo(long Number, AffiliationTerms Terms, ScopeKind Findable, long? FindableCircle, ScopeKind Dates, long? DatesCircle);

/// <summary>An affiliation as a page about its account shows it to a viewer: the dates only when that viewer may see them.</summary>
/// <param name="Terms">What the viewer may read of it.</param>
/// <param name="DatesShown">Whether the viewer may see when.</param>
public sealed record ShownAffiliation(AffiliationTerms Terms, bool DatesShown);

/// <summary>Somebody found through a place.</summary>
/// <param name="Person">The account.</param>
/// <param name="Role">What the person was there, when it is stated.</param>
/// <param name="Holding">For a search with a year: whether the records say so, or only leave it open.</param>
/// <param name="Shown">The affiliation as the viewer may read it.</param>
public sealed record FoundThrough(PersonRef Person, string? Role, Holding Holding, ShownAffiliation Shown);

/// <content>
/// Affiliations (step 19.6, ADR 0018, PRIV-010 to PRIV-012): where an account was – a school, an employer –
/// what it was there and when; and the choice to be found through the place. The name of an account, the
/// affiliation and its dates are allowed separately: an affiliation is shown and found by who its account
/// names, and found by a year only by who may also see the dates. The questions are answered by the rules of
/// step 15.2 (<see cref="AffiliationSearch"/>); being found together makes nobody a classmate or a friend.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>How many affiliations an account can state.</summary>
    public const int MostAffiliations = 40;

    /// <summary>The greatest length of the name of a place, in characters.</summary>
    public const int LongestOrganisation = 160;

    /// <summary>The greatest length of what somebody was at a place, in characters.</summary>
    public const int LongestRole = 120;

    /// <summary>
    /// States an affiliation. It is shown to nobody else until its account says so. Returns its number, or
    /// nothing when it says something impossible – no name, a text that is too long, a time that is none or an
    /// end before the beginning – when the account has stated exactly this already, or has as many as it may.
    /// <para>
    /// Somebody is at one place more than once: a stretch at a place is a statement of its own (step 20.4).
    /// "Exactly this" is the same place, the same role and the same times – a form that is sent twice makes
    /// one statement. Two stretches with different times are two, and what lies between them is no attendance.
    /// </para>
    /// </summary>
    public long? AddAffiliation(string id, AffiliationTerms terms) => Guarded([OfAccount(id)], () =>
    {
        if (!IsOpen(id) || Read(terms) is not { } read
            || _context.Affiliations.Count(stored => stored.Account == id) >= MostAffiliations
            || Stated(id, read, terms, except: null))
        {
            return (long?)null;
        }

        var stored = new StoredAffiliation { Account = id, Organisation = read.Organisation, OrganisationKey = read.Key, CreatedAt = Now };
        Write(stored, read, terms);
        _context.Affiliations.Add(stored);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        return stored.Number;
    });

    /// <summary>Changes what an affiliation says. Who may see it stays as it was chosen.</summary>
    public bool ChangeAffiliation(string id, long number, AffiliationTerms terms) => Guarded([OfAccount(id)], () =>
    {
        var stored = _context.Affiliations.SingleOrDefault(candidate => candidate.Number == number && candidate.Account == id);
        if (stored is null || Read(terms) is not { } read || Stated(id, read, terms, except: number))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        (stored.Organisation, stored.OrganisationKey) = (read.Organisation, read.Key);
        Write(stored, read, terms);
        Save();
        return true;
    });

    // Whether the account states exactly this already: the same place – the same entity or, where none is
    // named on either side, the same words – with the same role and the same times.
    private bool Stated(string id, (string Organisation, string Key, string? Role) read, AffiliationTerms terms, long? except)
    {
        var (institution, since, until, graduated) = (terms.Institution?.Write(), PersonTime.Write(terms.Since), PersonTime.Write(terms.Until), PersonTime.Write(terms.Graduated));
        return _context.Affiliations.AsNoTracking().Any(other => other.Account == id && other.Number != except
            && other.Institution == institution && (institution != null || other.OrganisationKey == read.Key)
            && other.Role == read.Role && other.Since == since && other.Until == until && other.Graduated == graduated);
    }

    /// <summary>Removes an affiliation.</summary>
    public bool RemoveAffiliation(string id, long number)
    {
        var stored = _context.Affiliations.SingleOrDefault(candidate => candidate.Number == number && candidate.Account == id);
        if (stored is null)
        {
            return false;
        }

        _context.Affiliations.Remove(stored);
        Save();
        return true;
    }

    /// <summary>
    /// Says who sees an affiliation and may find its account through the place, and who sees when. The dates
    /// are never shown to more people than the affiliation: a wider audience for them is cut down to it.
    /// </summary>
    public bool ShowAffiliation(string id, long number, ScopeKind findable, long? findableCircle, ScopeKind dates, long? datesCircle)
    {
        var stored = _context.Affiliations.SingleOrDefault(candidate => candidate.Number == number && candidate.Account == id);
        if (stored is null || !IsOpen(id) || !Enum.IsDefined(findable) || !Enum.IsDefined(dates) || !OwnsCircle(id, findable, findableCircle) || !OwnsCircle(id, dates, datesCircle))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        (stored.FindableScope, stored.FindableCircle) = ((int)findable, findable == ScopeKind.Circle ? findableCircle : null);

        // Dates without the affiliation would be dates of nothing: they follow it where they would go further.
        var narrower = dates == ScopeKind.Private || findable == ScopeKind.Public || (dates == ScopeKind.Circle && findable == ScopeKind.Circle && datesCircle == findableCircle);
        (stored.DatesScope, stored.DatesCircle) = narrower ? ((int)dates, dates == ScopeKind.Circle ? datesCircle : null) : (stored.FindableScope, stored.FindableCircle);
        Save();
        return true;
    }

    /// <summary>Returns the affiliations of an account as it sees them itself: by the name of the place.</summary>
    public IReadOnlyList<AffiliationInfo> Affiliations(string id) =>
    [
        .. _context.Affiliations.AsNoTracking().Where(stored => stored.Account == id).ToList()
            .Select(stored => new AffiliationInfo(stored.Number, Terms(stored), (ScopeKind)stored.FindableScope, stored.FindableCircle, (ScopeKind)stored.DatesScope, stored.DatesCircle))
            .OrderBy(affiliation => affiliation.Terms.Organisation, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(affiliation => affiliation.Number),
    ];

    /// <summary>
    /// Returns who a viewer may find through a place: the open accounts that state it and let this viewer find
    /// them there – and, asked with a year, let this viewer see when. The name of the place is compared as it
    /// is written, without case and outer spaces. Authorization comes first (PRIV-012): an affiliation the
    /// viewer may not find is not asked the question, so neither the list nor its length says anything about
    /// it. An account that blocks the viewer, one the viewer blocks and the viewer itself are not in the list.
    /// </summary>
    /// <param name="viewer">The account that asks.</param>
    /// <param name="organisation">The name of the place.</param>
    /// <param name="year">The year that is asked about, or nothing for any time.</param>
    /// <param name="question">What the year is asked of the attendance.</param>
    public IReadOnlyList<FoundThrough> FindThrough(string viewer, string organisation, YearOfEra? year = null, AttendanceQuestion question = AttendanceQuestion.AttendedDuring)
    {
        ArgumentNullException.ThrowIfNull(organisation);
        var key = Key(organisation.Trim());
        if (!IsOpen(viewer) || key.Length == 0)
        {
            return [];
        }

        return Found(viewer, _context.Affiliations.AsNoTracking().Where(candidate => candidate.OrganisationKey == key && candidate.Account != viewer).ToList(), InWords, year, question);
    }

    // What the rules are told a place is when it was only written in words: they ask for an entity, and the
    // words of an account are none. Nothing is found by this name – it stands for "the place of these words".
    private static readonly EntityId InWords = new("place-in-words");

    /// <summary>
    /// Returns who a viewer may find through a place that is an entity of a collection (step 20.4, ADR 0023):
    /// the open accounts whose affiliation points at it, under the same rules as
    /// <see cref="FindThrough(string, string, YearOfEra?, AttendanceQuestion)"/>. An affiliation that only names
    /// the place in words is not among them, however alike the words are: a name does not make a place.
    /// </summary>
    /// <param name="viewer">The account that asks.</param>
    /// <param name="place">
    /// The entity, with every entity the editors say is the same one in another collection. The store does
    /// not know the collections: who asks says which references are one place.
    /// </param>
    /// <param name="year">The year that is asked about, or nothing for any time.</param>
    /// <param name="question">What the year is asked of the attendance.</param>
    public IReadOnlyList<FoundThrough> FindThrough(string viewer, IReadOnlyCollection<EntityReference> place, YearOfEra? year = null, AttendanceQuestion question = AttendanceQuestion.AttendedDuring)
    {
        ArgumentNullException.ThrowIfNull(place);
        if (!IsOpen(viewer) || place.Count == 0)
        {
            return [];
        }

        var written = place.Select(reference => reference.Write()).Distinct(StringComparer.Ordinal).ToList();
        return Found(viewer, _context.Affiliations.AsNoTracking().Where(candidate => candidate.Institution != null && written.Contains(candidate.Institution) && candidate.Account != viewer).ToList(), place.First().Entity, year, question);
    }

    // Who of those that state a place a viewer may find there, and with which answer to a year.
    private List<FoundThrough> Found(string viewer, List<StoredAffiliation> stored, EntityId place, YearOfEra? year, AttendanceQuestion question)
    {
        var apart = _context.Blocks.AsNoTracking().Where(block => block.By == viewer || block.Blocked == viewer).AsEnumerable().Select(block => block.By == viewer ? block.Blocked : block.By).ToHashSet(StringComparer.Ordinal);
        var names = Names(stored.Select(candidate => candidate.Account));
        var asked = new AccountId(viewer);
        var open = stored.Where(candidate => names.ContainsKey(candidate.Account) && !apart.Contains(candidate.Account)).ToList();

        // One record of the rules for each affiliation, with its times as the rules can count them.
        Affiliation Rule(StoredAffiliation candidate, Func<TemporalExpression?, TemporalExpression?> counted) => new(
            new PersonId(candidate.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new AccountId(candidate.Account),
            place,
            Attended(counted(PersonTime.Read(candidate.Since)), counted(PersonTime.Read(candidate.Until))),
            counted(PersonTime.Read(candidate.Graduated)),
            Scope(candidate.Account, (ScopeKind)candidate.FindableScope, candidate.FindableCircle),
            Scope(candidate.Account, (ScopeKind)candidate.DatesScope, candidate.DatesCircle));

        Holding Answer(StoredAffiliation candidate)
        {
            if (year is not { } inYear)
            {
                return AffiliationSearch.Find([Rule(candidate, time => time)], place, asked).Count > 0 ? Holding.Yes : Holding.No;
            }

            // The rules count years of the common era. An affiliation written in the calendar the year is
            // asked in is compared in that calendar, year against year – exactly. One written in another
            // calendar is brought over as what it can be: a year of the Solar Hijri calendar lies in two years
            // of the common era, so the answer may be "possibly" – never a guess that reads like a fact.
            var asksIn = inYear.Era is Era.SolarHijri or Era.BeforeSolarHijri ? CalendarSystem.SolarHijri : CalendarSystem.Gregorian;
            var same = PersonTime.Calendar(PersonTime.Read(candidate.Since), PersonTime.Read(candidate.Until), PersonTime.Read(candidate.Graduated)) == asksIn;
            var rule = Rule(candidate, same ? PersonTime.AsYears : PersonTime.InCommonEra);
            YearOfEra[] years = same || asksIn == CalendarSystem.Gregorian
                ? [same ? new YearOfEra(Era.CommonEra, inYear.Number) : inYear]
                : [new YearOfEra(Era.CommonEra, inYear.Number + 621), new YearOfEra(Era.CommonEra, inYear.Number + 622)];
            var answers = years.Select(one => AffiliationSearch.Find([rule], place, one, question, asked).People.Select(person => person.Holding).DefaultIfEmpty(Holding.No).First()).ToList();
            return answers.All(answer => answer == Holding.Yes) ? Holding.Yes : answers.Any(answer => answer != Holding.No) ? Holding.Possibly : Holding.No;
        }

        return
        [
            .. open
                .Select(candidate => (Holding: Answer(candidate), Stored: candidate))
                .Where(person => person.Holding != Holding.No)
                .Select(person => new FoundThrough(names[person.Stored.Account], person.Stored.Role, person.Holding, Shown(person.Stored, asked)))
                .OrderBy(person => person.Person.Name, StringComparer.CurrentCultureIgnoreCase),
        ];
    }

    // The affiliations of an account a viewer may see on its page.
    private ImmutableArray<ShownAffiliation> AffiliationsShown(AccountId? viewer, string id) =>
    [
        .. _context.Affiliations.AsNoTracking().Where(stored => stored.Account == id).ToList()
            .Where(stored => Scope(id, (ScopeKind)stored.FindableScope, stored.FindableCircle).Allows(viewer))
            .Select(stored => Shown(stored, viewer))
            .OrderBy(shown => shown.Terms.Organisation, StringComparer.CurrentCultureIgnoreCase),
    ];

    // An affiliation as a viewer may read it: without its dates where those are not for this viewer.
    private ShownAffiliation Shown(StoredAffiliation stored, AccountId? viewer)
    {
        var dates = Scope(stored.Account, (ScopeKind)stored.DatesScope, stored.DatesCircle).Allows(viewer);
        var terms = Terms(stored);
        return new(dates ? terms : terms with { Since = null, Until = null, Graduated = null }, dates);
    }

    private static AffiliationTerms Terms(StoredAffiliation stored) =>
        new(stored.Organisation, stored.Role, PersonTime.Read(stored.Since), PersonTime.Read(stored.Until), PersonTime.Read(stored.Graduated), EntityReference.Read(stored.Institution));

    // The attendance as the rules ask for it: something that lasted, from what is stated of its beginning to
    // what is stated of its end; a time that is not stated at all answers no year.
    private static TemporalExpression Attended(TemporalExpression? since, TemporalExpression? until)
    {
        if (PersonTime.First(since) is null && PersonTime.Last(until) is null)
        {
            return new UnknownTime();
        }

        try
        {
            var start = PersonTime.First(since) is not { } earliest ? PeriodStart.Unknown
                : earliest == PersonTime.Last(since) ? PeriodStart.At(earliest) : PeriodStart.Between(earliest, PersonTime.Last(since)!);
            var end = PersonTime.Last(until) is not { } latest ? PeriodEnd.Unknown
                : latest == PersonTime.First(until) ? PeriodEnd.At(latest) : PeriodEnd.Between(PersonTime.First(until)!, latest);
            return new Period(start, end);
        }
        catch (ArgumentException)
        {
            // Two times the scale of the rules cannot put in one order: nothing is said about any year.
            return new UnknownTime();
        }
    }

    private (string Organisation, string Key, string? Role)? Read(AffiliationTerms? terms)
    {
        if (terms is null)
        {
            return null;
        }

        var (organisation, role) = (Line(terms.Organisation, LongestOrganisation), Line(terms.Role, LongestRole));
        return organisation.Value is null || organisation.Refused || role.Refused
            || !PersonTime.IsOne(terms.Since) || !PersonTime.IsOne(terms.Until) || !PersonTime.IsOne(terms.Graduated)
            || terms.Since is UnknownTime || terms.Until is UnknownTime || terms.Graduated is UnknownTime
            || !PersonTime.Lived(terms.Since, terms.Until) || !PersonTime.Lived(terms.Since, terms.Graduated)
            ? null
            : (organisation.Value, Key(organisation.Value), role.Value);
    }

    private static void Write(StoredAffiliation stored, (string Organisation, string Key, string? Role) read, AffiliationTerms terms) =>
        (stored.Role, stored.Since, stored.Until, stored.Graduated, stored.Institution) = (read.Role, PersonTime.Write(terms.Since), PersonTime.Write(terms.Until), PersonTime.Write(terms.Graduated), terms.Institution?.Write());
}
