using System.Text.Json;

namespace Vafadar.Finance.Core.Reminders;

/// <summary>How long a reminder is snoozed.</summary>
public enum SnoozeChoice
{
    /// <summary>One hour later.</summary>
    OneHour,

    /// <summary>The same time tomorrow.</summary>
    Tomorrow,
}

/// <summary>A reminder the user snoozed; it repeats the original text and link at <see cref="NotifyAt"/>.</summary>
/// <param name="Id">Notification id, derived from the original reminder.</param>
/// <param name="Link">Where a tap leads, as for the original reminder.</param>
/// <param name="Title">Title of the original reminder (already generic unless details are allowed).</param>
/// <param name="Body">Text of the original reminder.</param>
/// <param name="NotifyAt">Local time of the repeated reminder.</param>
public sealed record SnoozedReminder(int Id, string Link, string Title, string Body, DateTime NotifyAt);

/// <summary>
/// Snoozing repeats a reminder later and nothing else: the due date of the occurrence never changes, and a snooze ends
/// as soon as the occurrence is settled, skipped or its plan ends (REM-04, AT-35, AT-36).
/// </summary>
public static class ReminderSnoozes
{
    /// <summary>At most this many snoozes are kept.</summary>
    public const int MaxSnoozes = 20;

    /// <summary>
    /// Returns the id of the snoozed copy. Snoozing a snooze keeps the same id, so there is never more than one copy.
    /// </summary>
    public static int IdFor(int reminderId) => reminderId | 0x1000_0001;

    /// <summary>Returns when a reminder snoozed at <paramref name="now"/> fires again.</summary>
    public static DateTime NotifyAt(SnoozeChoice choice, DateTime now) => choice switch
    {
        SnoozeChoice.Tomorrow => now.AddDays(1),
        _ => now.AddHours(1),
    };

    /// <summary>Adds or replaces a snooze (a snooze of a snooze keeps one entry).</summary>
    public static IReadOnlyList<SnoozedReminder> Add(IEnumerable<SnoozedReminder> snoozes, SnoozedReminder snooze)
    {
        ArgumentNullException.ThrowIfNull(snoozes);
        ArgumentNullException.ThrowIfNull(snooze);
        return [.. snoozes.Where(s => s.Id != snooze.Id).Append(snooze).OrderBy(s => s.NotifyAt).TakeLast(MaxSnoozes)];
    }

    /// <summary>Keeps the snoozes that are still in the future and whose occurrence is still open.</summary>
    public static IReadOnlyList<SnoozedReminder> Keep(IEnumerable<SnoozedReminder> snoozes, DateTime now, Func<string, bool> isStillOpen)
    {
        ArgumentNullException.ThrowIfNull(snoozes);
        ArgumentNullException.ThrowIfNull(isStillOpen);
        return [.. snoozes.Where(s => s.NotifyAt > now && isStillOpen(s.Link)).OrderBy(s => s.NotifyAt)];
    }

    /// <summary>Serializes snoozes for local preferences.</summary>
    public static string Serialize(IEnumerable<SnoozedReminder> snoozes) => JsonSerializer.Serialize(snoozes.ToArray());

    /// <summary>Reads snoozes; damaged or missing data gives an empty list rather than an error.</summary>
    public static IReadOnlyList<SnoozedReminder> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<SnoozedReminder[]>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
