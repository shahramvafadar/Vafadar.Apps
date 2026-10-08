using System.Security.Cryptography;
using System.Text;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Reports;

namespace Vafadar.Zanance.Core.Reminders;

/// <summary>A reminder for a closed financial month; it never marks review steps as finished.</summary>
/// <param name="Id">Stable positive notification id.</param>
/// <param name="NotifyAt">Future device-local delivery time.</param>
/// <param name="PeriodFirst">First date of the financial month to review.</param>
public sealed record ReviewReminder(int Id, DateTime NotifyAt, DateOnly PeriodFirst);

/// <summary>Plans an optional reminder at 09:00 on the day after each financial month closes (ZCR-LOC-03).</summary>
public static class ReviewReminderPlanner
{
    /// <summary>Returns future reminders in the shared horizon, excluding empty and finished periods.</summary>
    public static IReadOnlyList<ReviewReminder> Plan(bool enabled, string? progress, DateTime now,
        PeriodCalendar calendar, int startDay, DateOnly? firstData)
    {
        if (!enabled || firstData is null) { return []; }
        var today = DateOnly.FromDateTime(now);
        var until = now.AddDays(ReminderPlanner.HorizonDays);
        var (year, month) = PeriodMath.MonthOf(today, calendar, startDay);
        var result = new List<ReviewReminder>();
        while (PeriodMath.IsSupportedYear(year, calendar))
        {
            var boundary = PeriodMath.ToDate(year, month, 1, calendar).AddDays(Math.Clamp(startDay, 1, PeriodMath.MaxStartDay) - 1);
            var at = boundary.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Local);
            if (at > until) { break; }
            // Resume never catches up missed alerts. On the boundary morning the review is already due.
            if (at > now && PeriodReview.Due(progress, boundary, calendar, startDay, firstData) is { } due)
            {
                var periodFirst = PeriodMath.MonthRange(due.Year, due.Month, calendar, startDay).First;
                result.Add(new ReviewReminder(StableId(calendar, periodFirst, boundary), at, periodFirst));
            }
            (year, month) = PeriodMath.Next(year, month);
        }
        return result.Take(ReminderPlanner.MaxPending).ToList();
    }

    private static int StableId(PeriodCalendar calendar, DateOnly periodFirst, DateOnly boundary)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(FormattableString.Invariant($"review:{calendar}:{periodFirst.DayNumber}:{boundary.DayNumber}")), hash);
        return (BitConverter.ToInt32(hash) & int.MaxValue) | 1;
    }
}
