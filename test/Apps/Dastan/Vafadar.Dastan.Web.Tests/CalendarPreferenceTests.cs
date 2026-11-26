using Vafadar.Dastan.Web.Site;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The calendar a visitor reads dates in beside the Gregorian one (D-100): what is stored is Gregorian and is
/// always shown; the same date in the calendar the visitor chose stands beside it, said to be converted; a year
/// lies in two years of a Hijri calendar and both are said. Asked of the library: the Shahnameh was finished
/// in 1010 CE.
/// </summary>
public sealed class CalendarPreferenceTests
{
    private const string Work = "work-shahnameh/entity/work-shahnameh";

    // The visitor chooses as a person does: by the form in the settings of a page.
    private static Task<Page> Choose(Browser visitor, string calendar) => visitor.Post(CalendarPreference.Address, "/en/", ("calendar", calendar), ("returnUrl", "/en/"));

    [Fact]
    public async Task A_page_in_English_shows_Gregorian_dates_alone_until_the_visitor_chooses_a_calendar()
    {
        if (LibrarySite.Collections.Count == 0)
        {
            return;
        }

        using var site = new LibrarySite();
        var visitor = site.Visitor();
        var plain = (await visitor.Get($"/en/{Work}")).Main;
        Assert.Contains("1010", plain, StringComparison.Ordinal);
        Assert.DoesNotContain("converted", plain, StringComparison.Ordinal);

        await Choose(visitor, "solar-hijri");
        var solar = (await visitor.Get($"/en/{Work}")).Main;
        Assert.Matches("1010[^()]*\\(converted: 388 or 389 [^()]*\\)", solar);

        // "About" a year stays "about" both, said once.
        Assert.Matches("about 977[^()]*\\(converted: about 355 or 356 [^()]*\\)", solar);

        await Choose(visitor, "lunar-hijri");
        var lunar = (await visitor.Get($"/en/{Work}")).Main;
        Assert.Matches("1010[^()]*\\(converted: 400 or 401 [^()]*\\)", lunar);
    }

    [Fact]
    public async Task A_page_in_Persian_reads_in_the_Solar_Hijri_calendar_until_the_visitor_chooses_another()
    {
        if (LibrarySite.Collections.Count == 0)
        {
            return;
        }

        using var site = new LibrarySite();
        var visitor = site.Visitor();
        var byLanguage = (await visitor.Get($"/fa/{Work}")).Main;
        Assert.Matches("۱۰۱۰[^()]*\\(تبدیل‌شده: ۳۸۸ یا ۳۸۹ [^()]*\\)", byLanguage);

        await Choose(visitor, "gregorian");
        var gregorian = (await visitor.Get($"/fa/{Work}")).Main;
        Assert.Contains("۱۰۱۰", gregorian, StringComparison.Ordinal);
        Assert.DoesNotContain("تبدیل‌شده", gregorian, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_lies_before_a_calendar_stands_in_the_Gregorian_one_alone()
    {
        if (LibrarySite.Collections.Count == 0)
        {
            return;
        }

        // Ancient Iran ends about 100 CE: nothing of it has a Hijri year, whatever the visitor chose.
        using var site = new LibrarySite();
        var visitor = site.Visitor();
        await Choose(visitor, "lunar-hijri");
        var page = (await visitor.Get("/en/subject-ancient-iran/entity/person-cyrus-ii")).Main;

        Assert.Contains("BCE", page, StringComparison.Ordinal);
        Assert.DoesNotContain("converted", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_choice_is_made_in_the_settings_of_every_page_and_remembered()
    {
        using var site = new DemoSite();
        var visitor = site.Visitor();
        var page = await visitor.Get("/en/");
        Assert.Contains($"action=\"{CalendarPreference.Address}\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("name=\"calendar\" value=\"gregorian\" aria-pressed=\"true\"", page.Html, StringComparison.Ordinal);
        Assert.Contains("name=\"calendar\" value=\"lunar-hijri\" aria-pressed=\"false\"", page.Html, StringComparison.Ordinal);

        await visitor.Post(CalendarPreference.Address, "/en/", ("calendar", "lunar-hijri"), ("returnUrl", "/en/"));
        Assert.Equal("lunar-hijri", visitor.Cookies[CalendarPreference.CookieName]);
        Assert.Contains("name=\"calendar\" value=\"lunar-hijri\" aria-pressed=\"true\"", (await visitor.Get("/en/")).Html, StringComparison.Ordinal);

        // The Persian page reads in the Solar Hijri calendar by itself.
        using var other = new DemoSite();
        Assert.Contains("name=\"calendar\" value=\"solar-hijri\" aria-pressed=\"true\"", (await other.Visitor().Get("/fa/")).Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_calendar_nobody_knows_is_refused()
    {
        using var site = new DemoSite();
        var visitor = site.Visitor();
        await Choose(visitor, "mayan");

        Assert.DoesNotContain(CalendarPreference.CookieName, visitor.Cookies.Keys);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    public void The_page_of_what_is_stored_on_a_device_names_the_choice(string language)
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "src", "Apps", "Dastan", "Vafadar.Dastan.Web", "Legal", $"cookies.{language}.txt"));

        Assert.Contains($"\"{CalendarPreference.CookieName}\"", text, StringComparison.Ordinal);
    }
}
