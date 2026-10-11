using System.Globalization;

namespace Vafadar.Zanance.Core.Reminders;

/// <summary>One original goal and its saved contribution rule date, independent of native delivery time.</summary>
/// <param name="GoalId">Original stored goal identity.</param>
/// <param name="OriginalDate">Original saved contribution rule date.</param>
public sealed record GoalReminderTarget(Guid GoalId, DateOnly OriginalDate);

/// <summary>Bounded original notification members tied to an opaque actual-file identity, never financial rights.</summary>
public sealed class GoalReminderLink
{
    private const string Prefix = "goal-reminder-v1";
    private const int MaximumLength = 1500;
    private GoalReminderLink(string scope, GoalReminderTarget[] targets)
    {
        Scope = scope; Targets = Array.AsReadOnly(targets);
    }
    /// <summary>Gets the actual file's opaque routing identity, not a path or an authorization token.</summary>
    public string Scope { get; }
    /// <summary>Gets only this notification's original goal/date members, without guessing later additions.</summary>
    public IReadOnlyList<GoalReminderTarget> Targets { get; }
    /// <summary>Formats a validated member list for the existing native returning-data field.</summary>
    public static string Format(string scope, IEnumerable<GoalReminderTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        var members = targets.Take(ReminderPlanner.MaxPending + 1).ToArray();
        if (!Valid(scope, members)) throw new ArgumentException("Invalid goal reminder identity.");
        return Prefix + "|" + scope + "|" + string.Join(';', members.Select(target => target.GoalId.ToString("N")
            + "@" + target.OriginalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
    }
    /// <summary>Rejects malformed, oversized, duplicate and unscoped legacy metadata without guessing a profile.</summary>
    public static GoalReminderLink? Parse(string? link)
    {
        if (link is null || link.Length > MaximumLength || link.Split('|') is not [Prefix, var scope, var body]) return null;
        var members = new List<GoalReminderTarget>();
        foreach (var item in body.Split(';'))
        {
            if (members.Count == ReminderPlanner.MaxPending || item.Split('@') is not [var id, var date]
                || !Guid.TryParseExact(id, "N", out var goal) || goal == Guid.Empty
                || !DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var original)) return null;
            members.Add(new(goal, original));
        }
        return Valid(scope, members) ? new(scope, members.ToArray()) : null;
    }
    private static bool Valid(string scope, IReadOnlyList<GoalReminderTarget> members) =>
        scope is { Length: 64 } && scope.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f')
        && members.Count is > 0 and <= ReminderPlanner.MaxPending && members.All(target => target.GoalId != Guid.Empty)
        && members.Distinct().Count() == members.Count;
}
