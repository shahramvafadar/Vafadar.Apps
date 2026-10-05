using Vafadar.Core.Dates;

namespace Vafadar.Core.Tests.Dates;

public sealed class LunarHijriTests
{
    [Fact]
    public void Ramadan_1446_follows_the_official_Umm_al_Qura_calendar()
    {
        // Saudi Arabia announced 1 Ramadan 1446 on 1 March 2025 and Eid al-Fitr (1 Shawwal) on 30 March 2025; the tabular
        // calendar would start Ramadan a day earlier.
        Assert.Equal(new DateOnly(2025, 3, 1), LunarHijri.ToDate(1446, 9, 1));
        Assert.Equal(29, LunarHijri.DaysInMonth(1446, 9));
        Assert.Equal(new DateOnly(2025, 3, 30), LunarHijri.ToDate(1446, 10, 1));
        Assert.Equal((1446, 9, 1), LunarHijri.Parts(new DateOnly(2025, 3, 1)));
    }

    [Fact]
    public void Months_have_29_or_30_days_and_a_year_354_or_355()
    {
        var days = Enumerable.Range(1, 12).Select(month => LunarHijri.DaysInMonth(1446, month)).ToList();

        Assert.All(days, d => Assert.InRange(d, 29, 30));
        Assert.InRange(days.Sum(), 354, 355);
    }

    [Theory]
    [InlineData(1900, 4, 30)]
    [InlineData(2026, 10, 5)]
    [InlineData(2077, 11, 16)]
    [InlineData(1800, 1, 1)]
    [InlineData(2100, 1, 1)]
    public void Every_date_converts_to_a_Hijri_day_and_back(int year, int month, int day)
    {
        // Inside 1318–1500 AH Umm al-Qura, outside the tabular calendar: dates never fail and never shift.
        var date = new DateOnly(year, month, day);
        var (hijriYear, hijriMonth, hijriDay) = LunarHijri.Parts(date);

        Assert.Equal(date, LunarHijri.ToDate(hijriYear, hijriMonth, hijriDay));
        Assert.True(LunarHijri.Supports(date));
    }

    [Fact]
    public void The_Umm_al_Qura_years_are_the_ones_dotnet_covers()
    {
        Assert.Equal(new DateOnly(1900, 4, 30), LunarHijri.ToDate(LunarHijri.FirstUmAlQuraYear, 1, 1));
        Assert.Equal(new DateOnly(2077, 11, 16), LunarHijri.ToDate(LunarHijri.LastUmAlQuraYear, 12, LunarHijri.DaysInMonth(LunarHijri.LastUmAlQuraYear, 12)));
    }
}
