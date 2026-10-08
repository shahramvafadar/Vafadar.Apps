using System.Resources;
using Vafadar.Core.Settings;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Plans;

/// <summary>The editor's sentence and actual date preview use the same recurrence calendar (D-65).</summary>
[Trait("AT", "AT-72")]
public sealed class PlanEditorTextTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("it")]
    public void Monthly_preview_keeps_the_start_day_and_uses_the_rule_calendar_in_every_language(string language)
    {
        var (text, dates) = Create(language, CalendarSystem.Persian);
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new(2026, 10, 20), Calendar = PeriodCalendar.Gregorian };
        var preview = text.UpcomingDates(rule, rule.Start);
        Assert.Equal(6, preview.Count);
        Assert.Equal(dates.Format(new DateOnly(2026, 11, 20), DateFormatStyle.Long, CalendarSystem.Gregorian), preview[1]);
        Assert.NotEqual(dates.Format(new DateOnly(2026, 11, 20), DateFormatStyle.Long), preview[1]);
        Assert.Contains(preview[0], text.EditorSummary(rule), StringComparison.Ordinal);
        Assert.DoesNotContain("Plan_", text.EditorSummary(rule), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PeriodCalendar.Gregorian)]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public void First_due_date_and_count_are_consistent_across_calendars(PeriodCalendar calendar)
    {
        var (text, dates) = Create("en", CalendarSystem.Gregorian);
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Calendar = calendar, Start = new(2026, 10, 20), End = EndKind.AfterCount, Count = 3 };
        var preview = text.UpcomingDates(rule, rule.Start);
        Assert.Equal(3, preview.Count);
        Assert.Equal(dates.Format(rule.Start, DateFormatStyle.Long, Calendars.ToDisplay(calendar)), preview[0]);
        Assert.Contains(preview[0], text.EditorSummary(rule), StringComparison.Ordinal);
    }

    [Fact]
    public void Month_end_explanation_and_preview_preserve_the_31_anchor()
    {
        var (text, dates) = Create("en", CalendarSystem.Gregorian);
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new(2027, 1, 31) };
        var preview = text.UpcomingDates(rule, rule.Start);
        Assert.Contains("Return to day 31", text.ShortMonthHint(rule), StringComparison.Ordinal);
        Assert.Equal(dates.Format(new DateOnly(2027, 2, 28), DateFormatStyle.Long), preview[1]);
        Assert.Equal(dates.Format(new DateOnly(2027, 3, 31), DateFormatStyle.Long), preview[2]);
        rule.MissingDay = MissingDayPolicy.Skip;
        Assert.Contains("Skip months", text.ShortMonthHint(rule), StringComparison.Ordinal);
        rule.DayRule = MonthDayRule.LastDayOfMonth;
        Assert.Null(text.ShortMonthHint(rule));
    }

    [Fact]
    public void Invalid_custom_repeat_and_exhausted_plan_have_explicit_preview_states()
    {
        var (text, _) = Create("en", CalendarSystem.Gregorian);
        var rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new(2026, 10, 20), Interval = 0 };
        Assert.Single(text.UpcomingDates(rule, rule.Start));
        Assert.Equal(text.EditorSummary(rule), text.UpcomingDates(rule, rule.Start)[0]);
        var ended = new RecurrenceRule { Frequency = Frequency.Monthly, Start = rule.Start, Interval = 1, End = EndKind.AfterCount, Count = 1 };
        Assert.Single(text.UpcomingDates(ended, ended.Start.AddYears(1)));
        Assert.NotEqual(text.UpcomingDates(ended, ended.Start)[0], text.UpcomingDates(ended, ended.Start.AddYears(1))[0]);
    }

    private static (PlanText Text, DateFormatter Dates) Create(string language, CalendarSystem displayCalendar)
    {
        var translator = new Translator();
        translator.AddResources(new ResourceManager("Vafadar.Zanance.Core.Tests.Resources.AppResources", typeof(PlanEditorTextTests).Assembly));
        var localization = new LocalizationService(new LocalizationOptions(), new InMemorySettingsStore(), translator);
        localization.Initialize();
        localization.SetLanguage(AppLanguages.All.Single(x => x.CultureName == language));
        localization.SetCalendar(displayCalendar);
        var dates = new DateFormatter(localization);
        return (new PlanText(translator, dates, localization), dates);
    }
}
