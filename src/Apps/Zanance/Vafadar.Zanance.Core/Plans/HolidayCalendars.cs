namespace Vafadar.Zanance.Core.Plans;

/// <summary>Explicit national/federal/common scope; no payroll or bank-business-day assertion.</summary>
public enum HolidayCoverage
{
    /// <summary>Regular nationwide public dates; regional and workplace rules are excluded.</summary>
    Nationwide,
    /// <summary>Federal statutory dates, excluding provincial/cantonal holidays and workplace substitutes.</summary>
    Federal,
    /// <summary>US federal observed dates for the standard Monday-Friday schedule.</summary>
    FederalObserved,
    /// <summary>Only dates common to every constituent region, not a complete regional calendar.</summary>
    Common,
}

/// <summary>Whether a date comes from reviewed coverage, an approximate legacy rule, or unavailable coverage.</summary>
public enum HolidayKnowledge
{
    /// <summary>No reviewed dates for this country/year.</summary>
    Unverified,
    /// <summary>Reviewed regular scope, excluding extraordinary announcements and local employment rules.</summary>
    Reviewed,
    /// <summary>Legacy calculated dates can differ from lunar announcements.</summary>
    Approximate,
}

/// <summary>Provenance and bounded reviewed years of an offline calendar dataset.</summary>
/// <param name="Region">ISO country code independent of language/currency.</param>
/// <param name="Coverage">What this precise calendar includes.</param>
/// <param name="FirstYear">First reviewed Gregorian year.</param>
/// <param name="LastYear">Last reviewed Gregorian year.</param>
/// <param name="Source">Official source; no runtime network request.</param>
/// <param name="Publisher">Proper authority name for provenance.</param>
public sealed record HolidayCalendarInfo(string Region, HolidayCoverage Coverage, int FirstYear, int LastYear, string Source, string Publisher)
{
    /// <summary>Whether this dataset has reviewed dates for the requested year.</summary>
    public bool Covers(int year) => year >= FirstYear && year <= LastYear;
}

/// <summary>Extensible, source-qualified calendars; unreviewed years never become guessed empty calendars.</summary>
public static class HolidayCalendars
{
    private static readonly IReadOnlyDictionary<string, HolidayCalendarInfo> Catalog = new Dictionary<string, HolidayCalendarInfo>(StringComparer.OrdinalIgnoreCase)
    {
        ["DE"] = new("DE", HolidayCoverage.Nationwide, 2026, 2027, "https://www.bundesbank.de/de/startseite/hinweise-zur-abwicklung-des-unbaren-zahlungsverkehrs-bei-der-deutschen-bundesbank-im-zusammenhang-mit-bundesweiten-und-regionalen-feiertagen-671010", "Deutsche Bundesbank"),
        ["AT"] = new("AT", HolidayCoverage.Nationwide, 2026, 2027, "https://www.ris.bka.gv.at/NormDokument.wxe?Abfrage=Bundesnormen&Gesetzesnummer=10008541&Paragraf=7", "RIS"),
        ["BE"] = new("BE", HolidayCoverage.Nationwide, 2026, 2027, "https://emploi.belgique.be/fr/themes/jours-feries-et-conges/jours-feries", "SPF Emploi"),
        ["FR"] = new("FR", HolidayCoverage.Nationwide, 2026, 2027, "https://www.service-public.gouv.fr/particuliers/vosdroits/F2405", "Service Public"),
        ["LI"] = new("LI", HolidayCoverage.Nationwide, 2026, 2027, "https://www.gesetze.li/konso/1967.006", "Liechtensteinische Landesverwaltung"),
        ["LU"] = new("LU", HolidayCoverage.Nationwide, 2026, 2027, "https://itm.public.lu/fr/conditions-travail/conges/jours-feries.html", "ITM Luxembourg"),
        ["MC"] = new("MC", HolidayCoverage.Nationwide, 2026, 2027, "https://legimonaco.mc/tnc/loi/1966/02-18-798/", "Legimonaco"),
        ["NL"] = new("NL", HolidayCoverage.Nationwide, 2026, 2027, "https://www.government.nl/themes/work/working-hours/question-and-answer/work/public-holidays-in-the-netherlands", "Government.nl"),
        ["CH"] = new("CH", HolidayCoverage.Federal, 2026, 2027, "https://www.ch.ch/de/arbeit/arbeitszeit/ferien--feiertage-und-arbeitsabwesenheiten/", "ch.ch"),
        ["GB"] = new("GB", HolidayCoverage.Common, 2026, 2027, "https://www.gov.uk/bank-holidays.json", "GOV.UK"),
        ["IE"] = new("IE", HolidayCoverage.Nationwide, 2026, 2027, "https://www.workplacerelations.ie/en/what_you_should_know/public-holidays/", "Workplace Relations Commission"),
        ["US"] = new("US", HolidayCoverage.FederalObserved, 2026, 2027, "https://www.opm.gov/policy-data-oversight/pay-leave/federal-holidays/", "OPM"),
        ["CA"] = new("CA", HolidayCoverage.Federal, 2026, 2027, "https://www.canada.ca/en/services/jobs/workplace/federal-labour-standards/vacations-holidays.html", "Government of Canada"),
        ["AU"] = new("AU", HolidayCoverage.Common, 2026, 2027, "https://www.fairwork.gov.au/employment-conditions/public-holidays", "Fair Work Ombudsman"),
    };

    /// <summary>Immutable definitions; adding a country does not require a stored model change.</summary>
    public static IReadOnlyList<HolidayCalendarInfo> All { get; } = [.. Catalog.Values.OrderBy(c => c.Region, StringComparer.Ordinal)];
    /// <summary>Gets provenance, or null when no reviewed dataset is available.</summary>
    public static HolidayCalendarInfo? Get(string? region) => region is not null && Catalog.TryGetValue(region, out var value) ? value : null;
    /// <summary>Review date of the published source/current statutory-rule snapshot.</summary>
    public static DateOnly ReviewedOn { get; } = new(2026, 10, 14);

    /// <summary>Returns an explicit uncertainty state, independently of whether any date matched a holiday.</summary>
    public static HolidayKnowledge Knowledge(string? region, int year) => PublicHolidays.HasApproximateDates(region)
        ? (year is >= 623 and <= 9998 ? HolidayKnowledge.Approximate : HolidayKnowledge.Unverified) : Get(region)?.Covers(year) == true ? HolidayKnowledge.Reviewed : HolidayKnowledge.Unverified;

    /// <summary>Returns only the explicitly reviewed regular dates within the declared scope and years.</summary>
    internal static IReadOnlyList<PublicHoliday> GetDates(string region, int year)
    {
        if (Get(region) is not { } info || !info.Covers(year)) return [];
        var easter = PublicHolidays.EasterSunday(year);
        List<PublicHoliday> dates = [];
        void Fixed(int month, int day, string key) => dates.Add(new(new DateOnly(year, month, day), key, false));
        void Relative(int offset, string key) => dates.Add(new(easter.AddDays(offset), key, false));
        void Monday(int month, int occurrence, string key) => dates.Add(new(NthWeekday(year, month, DayOfWeek.Monday, occurrence), key, false));
        switch (region.ToUpperInvariant())
        {
            case "AT":
                Fixed(1,1,"NewYear"); Fixed(1,6,"Epiphany"); Relative(1,"EasterMonday"); Fixed(5,1,"LabourDay");
                Relative(39,"Ascension"); Relative(50,"WhitMonday"); Relative(60,"CorpusChristi"); Fixed(8,15,"Assumption");
                Fixed(10,26,"AustriaNational"); Fixed(11,1,"AllSaints"); Fixed(12,8,"ImmaculateConception"); Fixed(12,25,"Christmas"); Fixed(12,26,"SecondChristmas"); break;
            case "BE":
                Fixed(1,1,"NewYear"); Relative(1,"EasterMonday"); Fixed(5,1,"LabourDay"); Relative(39,"Ascension"); Relative(50,"WhitMonday");
                Fixed(7,21,"BelgiumNational"); Fixed(8,15,"Assumption"); Fixed(11,1,"AllSaints"); Fixed(11,11,"Armistice"); Fixed(12,25,"Christmas"); break;
            case "FR":
                Fixed(1,1,"NewYear"); Relative(1,"EasterMonday"); Fixed(5,1,"LabourDay"); Fixed(5,8,"VictoryEurope"); Relative(39,"Ascension"); Relative(50,"WhitMonday");
                Fixed(7,14,"FranceNational"); Fixed(8,15,"Assumption"); Fixed(11,1,"AllSaints"); Fixed(11,11,"Armistice"); Fixed(12,25,"Christmas"); break;
            case "LI":
                Fixed(1,1,"NewYear"); Fixed(1,6,"Epiphany"); Fixed(3,19,"SaintJoseph"); Relative(1,"EasterMonday"); Fixed(5,1,"LabourDay");
                Relative(39,"Ascension"); Relative(50,"WhitMonday"); Relative(60,"CorpusChristi"); Fixed(8,15,"LiechtensteinNational");
                Fixed(9,8,"NativityMary"); Fixed(11,1,"AllSaints"); Fixed(12,8,"ImmaculateConception"); Fixed(12,25,"Christmas"); Fixed(12,26,"SecondChristmas"); break;
            case "LU":
                Fixed(1,1,"NewYear"); Relative(1,"EasterMonday"); Fixed(5,1,"LabourDay"); Fixed(5,9,"EuropeDay"); Relative(39,"Ascension"); Relative(50,"WhitMonday");
                Fixed(6,23,"LuxembourgNational"); Fixed(8,15,"Assumption"); Fixed(11,1,"AllSaints"); Fixed(12,25,"Christmas"); Fixed(12,26,"SecondChristmas"); break;
            case "MC":
                Fixed(1,1,"NewYear"); Fixed(1,27,"SaintDevote"); Relative(1,"EasterMonday"); Fixed(5,1,"LabourDay"); Relative(39,"Ascension");
                Relative(50,"WhitMonday"); Relative(60,"CorpusChristi"); Fixed(8,15,"Assumption"); Fixed(11,1,"AllSaints"); Fixed(11,19,"MonacoNational");
                Fixed(12,8,"ImmaculateConception"); Fixed(12,25,"Christmas");
                foreach (var holiday in dates.ToArray().Where(h => h.Date.DayOfWeek == DayOfWeek.Sunday))
                    dates.Add(holiday with { Date = holiday.Date.AddDays(1) }); break;
            case "NL":
                Fixed(1,1,"NewYear"); Relative(-2,"GoodFriday"); Relative(0,"EasterSunday"); Relative(1,"EasterMonday");
                dates.Add(new(new DateOnly(year,4,27).DayOfWeek == DayOfWeek.Sunday ? new DateOnly(year,4,26) : new DateOnly(year,4,27), "KingsDay", false));
                Fixed(5,5,"LiberationDay"); Relative(39,"Ascension"); Relative(49,"WhitSunday"); Relative(50,"WhitMonday"); Fixed(12,25,"Christmas"); Fixed(12,26,"SecondChristmas"); break;
            case "CH": Fixed(8,1,"SwissNational"); break;
            case "GB":
                Fixed(1,1,"NewYear"); Relative(-2,"GoodFriday"); Monday(5,1,"EarlyMay"); Monday(5,-1,"SpringHoliday");
                dates.Add(new(new DateOnly(year,12,year == 2026 ? 25 : 27),"Christmas",false));
                Fixed(12,28,"BoxingDay"); break;
            case "IE":
                Fixed(1,1,"NewYear"); dates.Add(new(new DateOnly(year,2,1).DayOfWeek == DayOfWeek.Friday ? new DateOnly(year,2,1) : NthWeekday(year,2,DayOfWeek.Monday,1),"SaintBrigid",false));
                Fixed(3,17,"SaintPatrick"); Relative(1,"EasterMonday"); Monday(5,1,"MayHoliday"); Monday(6,1,"JuneHoliday");
                Monday(8,1,"AugustHoliday"); Monday(10,-1,"OctoberHoliday"); Fixed(12,25,"Christmas"); Fixed(12,26,"SaintStephen"); break;
            case "US":
                AddUsObserved(dates,year); break;
            case "CA":
                Fixed(1,1,"NewYear"); Relative(-2,"GoodFriday");
                var victoria = new DateOnly(year,5,24); while (victoria.DayOfWeek != DayOfWeek.Monday) victoria = victoria.AddDays(-1);
                dates.Add(new(victoria,"VictoriaDay",false)); Fixed(7,1,"CanadaDay"); Monday(9,1,"LabourDay"); Fixed(9,30,"TruthReconciliation");
                Monday(10,2,"Thanksgiving"); Fixed(11,11,"Remembrance"); Fixed(12,25,"Christmas"); Fixed(12,26,"BoxingDay"); break;
            case "AU":
                Fixed(1,1,"NewYear"); Fixed(1,26,"AustraliaDay"); Relative(-2,"GoodFriday"); Relative(1,"EasterMonday"); if (year == 2026) Fixed(4,25,"AnzacDay");
                Fixed(12,25,"Christmas");
                if (year == 2026) Fixed(12,28,"BoxingObserved");
                else { Fixed(12,27,"ChristmasObserved"); Fixed(12,28,"BoxingObserved"); } break;
        }
        return [.. dates.Where(h => h.Date.Year == year).OrderBy(h => h.Date).DistinctBy(h => h.Date)];
    }

    private static DateOnly NthWeekday(int year, int month, DayOfWeek day, int occurrence)
    {
        var date = occurrence > 0 ? new DateOnly(year,month,1) : new DateOnly(year,month,DateTime.DaysInMonth(year,month));
        var step = occurrence > 0 ? 1 : -1;
        while (date.DayOfWeek != day) date = date.AddDays(step);
        return date.AddDays(7 * step * (Math.Abs(occurrence) - 1));
    }

    private static void AddUsObserved(List<PublicHoliday> dates, int year)
    {
        void Observed(int sourceYear, int month, int day, string key)
        {
            var date = new DateOnly(sourceYear,month,day);
            if (date.DayOfWeek == DayOfWeek.Saturday) date = date.AddDays(-1);
            else if (date.DayOfWeek == DayOfWeek.Sunday) date = date.AddDays(1);
            if (date.Year == year) dates.Add(new(date,key,false));
        }
        Observed(year,1,1,"NewYear"); Observed(year+1,1,1,"NewYear");
        dates.Add(new(NthWeekday(year,1,DayOfWeek.Monday,3),"MartinLutherKing",false));
        dates.Add(new(NthWeekday(year,2,DayOfWeek.Monday,3),"WashingtonBirthday",false));
        dates.Add(new(NthWeekday(year,5,DayOfWeek.Monday,-1),"MemorialDay",false));
        Observed(year,6,19,"Juneteenth"); Observed(year,7,4,"IndependenceDay");
        dates.Add(new(NthWeekday(year,9,DayOfWeek.Monday,1),"LabourDay",false));
        dates.Add(new(NthWeekday(year,10,DayOfWeek.Monday,2),"ColumbusDay",false));
        Observed(year,11,11,"VeteransDay"); dates.Add(new(NthWeekday(year,11,DayOfWeek.Thursday,4),"Thanksgiving",false));
        Observed(year,12,25,"Christmas");
    }
}
