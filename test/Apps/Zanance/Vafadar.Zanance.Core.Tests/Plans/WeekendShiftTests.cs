using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Tests.Plans;

public sealed class WeekendShiftTests
{
    private static readonly int SaturdaySunday = RecurrenceRule.MaskOf([DayOfWeek.Saturday, DayOfWeek.Sunday]);

    [Theory]
    [InlineData(WeekendShift.Before, "2027-05-01", "2027-04-30")]
    [InlineData(WeekendShift.After, "2027-05-01", "2027-05-03")]
    [InlineData(WeekendShift.None, "2027-05-01", "2027-05-01")]
    [InlineData(WeekendShift.Before, "2027-05-03", "2027-05-03")]
    public void A_weekend_due_date_moves_to_the_nearest_working_day(WeekendShift shift, string date, string expected)
    {
        var rule = new RecurrenceRule { WeekendShift = shift, WeekendDays = SaturdaySunday };

        Assert.Equal(DateOnly.Parse(expected), rule.ApplyWeekend(DateOnly.Parse(date)));
    }

    [Fact]
    public void A_friday_weekend_moves_to_thursday_and_an_all_weekend_mask_never_loops()
    {
        var iran = new RecurrenceRule { WeekendShift = WeekendShift.Before, WeekendDays = RecurrenceRule.MaskOf([DayOfWeek.Friday]) };
        Assert.Equal(new DateOnly(2027, 4, 29), iran.ApplyWeekend(new DateOnly(2027, 4, 30)));

        var broken = new RecurrenceRule { WeekendShift = WeekendShift.After, WeekendDays = 0x7F };
        Assert.Equal(new DateOnly(2027, 5, 1), broken.ApplyWeekend(new DateOnly(2027, 5, 1)));
    }

    [Fact]
    public void Occurrences_keep_their_identity_while_their_due_date_moves()
    {
        var plan = new Schedule
        {
            Name = "Rent",
            Kind = EntryKind.Expense,
            AccountId = Guid.CreateVersion7(),
            Amount = 95_000,
            Rule = new RecurrenceRule { Frequency = Frequency.Monthly, Start = new DateOnly(2027, 4, 1), WeekendShift = WeekendShift.Before, WeekendDays = SaturdaySunday },
        };

        var may = Occurrences.Between(plan, [], new DateOnly(2027, 4, 25), new DateOnly(2027, 5, 10), new DateOnly(2027, 4, 1)).Single();

        Assert.Equal(new DateOnly(2027, 5, 1), may.OriginalDate);
        Assert.Equal(new DateOnly(2027, 4, 30), may.DueDate);
    }
}