using System.Globalization;

namespace Vafadar.Zanance.Core.Reminders;

/// <summary>An original occurrence identity; a null date names a contract's plan instead.</summary>
/// <param name="PlanId">Stored plan identity.</param>
/// <param name="OriginalDate">Original rule date, independent of effective due date or snooze time.</param>
public sealed record PlanReminderTarget(Guid PlanId, DateOnly? OriginalDate);

/// <summary>Bounded device-local notification routing metadata, never a permission or a portable selection.</summary>
public sealed class PlanReminderLink
{
    private const string Prefix = "plan-reminder-v1";
    private const int MaximumLength = 1500;

    private PlanReminderLink(string scope, PlanReminderTarget[] targets)
    {
        Scope = scope;
        Targets = Array.AsReadOnly(targets);
    }

    /// <summary>Gets an opaque identifier for the actual database file, not an authorization token.</summary>
    public string Scope { get; }

    /// <summary>Gets the original members of this notification; later edits never add guessed members.</summary>
    public IReadOnlyList<PlanReminderTarget> Targets { get; }

    /// <summary>Gets whether this link opens a contract's retained plan details.</summary>
    public bool IsContract => Targets[0].OriginalDate is null;

    /// <summary>Formats validated metadata for the existing native notification returning-data field.</summary>
    public static string Format(string scope, IEnumerable<PlanReminderTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        var members = targets.Take(ReminderPlanner.MaxPending + 1).ToArray();
        if (!Valid(scope, members)) throw new ArgumentException("Invalid plan reminder identity.");
        return Prefix + "|" + scope + "|" + string.Join(';', members.Select(t =>
            t.PlanId.ToString("N") + "@" + (t.OriginalDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-")));
    }

    /// <summary>Rejects malformed, oversized, duplicate and unscoped legacy payloads without guessing identities.</summary>
    public static PlanReminderLink? Parse(string? link)
    {
        if (link is null || link.Length > MaximumLength || link.Split('|') is not [Prefix, var scope, var body]) return null;
        var members = new List<PlanReminderTarget>();
        foreach (var item in body.Split(';'))
        {
            if (members.Count == ReminderPlanner.MaxPending || item.Split('@') is not [var id, var date]
                || !Guid.TryParseExact(id, "N", out var plan) || plan == Guid.Empty) return null;
            DateOnly? original = null;
            if (date != "-")
            {
                if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return null;
                original = parsed;
            }
            members.Add(new(plan, original));
        }
        return Valid(scope, members) ? new(scope, members.ToArray()) : null;
    }

    private static bool Valid(string scope, IReadOnlyList<PlanReminderTarget> members) =>
        scope is { Length: 64 } && scope.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f')
        && members.Count is > 0 and <= ReminderPlanner.MaxPending && members.All(t => t.PlanId != Guid.Empty)
        && members.Distinct().Count() == members.Count
        && (members.All(t => t.OriginalDate is not null) || members.Count == 1 && members[0].OriginalDate is null);
}
