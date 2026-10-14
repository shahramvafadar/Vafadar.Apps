using System.Globalization;
using System.Resources;
using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Plans;

/// <summary>Reviewed official-date vectors and the actual application presentation boundary.</summary>
[Trait("AT", "AT-273")]
public sealed class TargetHolidayTests
{
    [Theory]
    [InlineData("AT",13)] [InlineData("BE",10)] [InlineData("FR",11)] [InlineData("LI",14)]
    [InlineData("LU",11)] [InlineData("MC",13)] [InlineData("NL",11)] [InlineData("CH",1)]
    [InlineData("GB",6)] [InlineData("IE",10)] [InlineData("US",11)] [InlineData("CA",10)] [InlineData("AU",7)]
    public void Reviewed_country_year_has_its_exact_scoped_dates_and_no_duplicates(string code, int count)
    {
        var holidays = PublicHolidays.In(code,2026);
        Assert.Equal(count, holidays.Count);
        Assert.Equal(count, holidays.Select(h => h.Date).Distinct().Count());
        Assert.Equal(holidays.OrderBy(h => h.Date), holidays);
        Assert.All(holidays, h => Assert.False(h.IsApproximate));
        Assert.Equal(HolidayKnowledge.Reviewed, HolidayCalendars.Knowledge(code,2026));
        Assert.Equal(HolidayKnowledge.Unverified, HolidayCalendars.Knowledge(code,2028));
        Assert.Empty(PublicHolidays.In(code,2028));
    }

    [Theory]
    [InlineData("AT",2026,6,4)] [InlineData("FR",2027,5,6)] [InlineData("LU",2026,5,9)]
    [InlineData("NL",2027,3,29)] [InlineData("IE",2026,2,2)] [InlineData("IE",2027,2,1)]
    [InlineData("MC",2026,11,2)] [InlineData("MC",2027,8,16)] [InlineData("US",2026,7,3)]
    [InlineData("US",2027,12,31)] [InlineData("CA",2027,5,24)] [InlineData("AU",2027,12,28)]
    public void Official_statutory_or_published_date_is_recognized(string code,int year,int month,int day) =>
        Assert.True(PublicHolidays.IsHoliday(code,new DateOnly(year,month,day)));

    [Theory]
    [InlineData("AT",2026,4,3)] [InlineData("FR",2026,4,3)] [InlineData("CH",2026,12,25)]
    [InlineData("GB",2026,4,6)] [InlineData("GB",2026,6,15)] [InlineData("AU",2026,12,26)]
    [InlineData("AU",2027,4,25)] [InlineData("AU",2027,4,26)] [InlineData("AU",2027,1,4)]
    public void Unshared_regional_or_unpublished_substitute_is_not_invented(string code,int year,int month,int day) =>
        Assert.False(PublicHolidays.IsHoliday(code,new DateOnly(year,month,day)));

    [Fact]
    public void A_us_observed_new_year_date_is_checked_when_shifting_across_year_boundary()
    {
        var rule = new RecurrenceRule { HolidayRegion="US", WeekendDays=RecurrenceRule.MaskOf([DayOfWeek.Saturday,DayOfWeek.Sunday]), WeekendShift=WeekendShift.Before };
        Assert.Equal(new DateOnly(2027,12,30),rule.ApplyWeekend(new DateOnly(2028,1,1)));
    }

    [Fact]
    public void Every_target_currency_already_has_the_reviewed_two_minor_digits_without_amount_migration()
    {
        Assert.Equal(HolidayCalendars.All.Select(c => c.Region).Order(StringComparer.Ordinal),TargetMarkets.Countries);
        Assert.All(TargetMarkets.Countries,country => Assert.Equal(2,TargetMarkets.CurrencyFor(country)?.MinorDigits));
        Assert.Equal("CHF",TargetMarkets.CurrencyFor("LI")?.Code);
        Assert.Equal("CAD",TargetMarkets.CurrencyFor("CA")?.Code);
        Assert.Null(TargetMarkets.CurrencyFor("XX"));
    }

    [Fact]
    public void Editing_keeps_the_saved_calendar_and_weekend_mask_after_display_country_changes()
    {
        var original = new RecurrenceRule { HolidayRegion="IR", WeekendDays=RecurrenceRule.MaskOf([DayOfWeek.Friday]) };
        Assert.Equal("IR",HolidayCalendarText.RegionForDraft(original,"US"));
        Assert.Equal(original.WeekendDays,HolidayCalendarText.WeekendMaskForDraft(original,[DayOfWeek.Saturday,DayOfWeek.Sunday]));
        Assert.Equal("US",HolidayCalendarText.RegionForDraft(null,"us"));
        Assert.Null(HolidayCalendarText.RegionForDraft(null,"XX"));
    }

    [Theory]
    [InlineData("en")] [InlineData("fa")] [InlineData("de")] [InlineData("es")] [InlineData("fr")] [InlineData("it")]
    public void Actual_choice_and_provenance_text_are_translated_and_year_uncertainty_is_explicit(string language)
    {
        var text = new Translator();
        text.AddResources(new ResourceManager("Vafadar.Zanance.Core.Tests.Resources.AppResources",typeof(TargetHolidayTests).Assembly));
        text.SetCulture(CultureInfo.GetCultureInfo(language));
        var choice = HolidayCalendarText.Choice("CA",CultureInfo.GetCultureInfo(language),text);
        Assert.DoesNotContain("Holiday_Choice",choice);
        var known = HolidayCalendarText.Description("CA",2026,text);
        Assert.Contains("Government of Canada",known);
        Assert.DoesNotContain("Holiday_",known);
        Assert.NotEqual(known,HolidayCalendarText.Description("CA",2028,text));
        Assert.Contains(text["Holiday_YearUnverified"],HolidayCalendarText.Description("CA",2028,text));
    }
}
