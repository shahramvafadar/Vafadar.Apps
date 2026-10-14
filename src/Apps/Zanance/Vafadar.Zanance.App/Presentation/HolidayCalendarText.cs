using System.Globalization;
using Vafadar.Localization;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>One truthful holiday scope/provenance description for onboarding, Settings and new Plan drafts.</summary>
internal static class HolidayCalendarText
{
    /// <summary>A saved rule retains its own country even after the device's display country changes.</summary>
    public static string? RegionForDraft(RecurrenceRule? existing, string? deviceRegion) => existing?.HolidayRegion
        ?? (PublicHolidays.IsSupported(deviceRegion) ? deviceRegion?.ToUpperInvariant() : null);

    /// <summary>Country preference changes cannot rewrite the weekend mask captured by a saved rule.</summary>
    public static int WeekendMaskForDraft(RecurrenceRule? existing, IEnumerable<DayOfWeek> defaults) =>
        existing?.WeekendDays ?? RecurrenceRule.MaskOf(defaults);

    /// <summary>Country caption states a partial/federal scope directly in the choice.</summary>
    public static string Choice(string code, CultureInfo culture, Translator translator)
    {
        var country = Regions.DisplayName(code, culture);
        return HolidayCalendars.Get(code)?.Coverage switch
        {
            HolidayCoverage.Federal or HolidayCoverage.FederalObserved => translator.Format("Holiday_ChoiceFederal", country),
            HolidayCoverage.Common => translator.Format("Holiday_ChoiceCommon", country),
            _ => country,
        };
    }

    /// <summary>Shows the source, review boundary and uncertainty without asserting a bank/workplace closure.</summary>
    public static string Description(string? code, int year, Translator translator)
    {
        if (PublicHolidays.HasApproximateDates(code)) return translator["Regional_HolidaysIran"];
        if (HolidayCalendars.Get(code) is not { } info) return translator["Regional_HolidaysHint"];
        var scope = translator["Holiday_Coverage" + info.Coverage];
        var years = translator.Format("Holiday_ReviewedYears", Vafadar.Localization.Formatting.NativeDigits.Apply(info.FirstYear.ToString(CultureInfo.InvariantCulture)),
            Vafadar.Localization.Formatting.NativeDigits.Apply(info.LastYear.ToString(CultureInfo.InvariantCulture)), info.Publisher);
        return scope + " " + years + (!info.Covers(year) ? " " + translator["Holiday_YearUnverified"] : "");
    }
}
