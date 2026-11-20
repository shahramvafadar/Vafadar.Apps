namespace Vafadar.Zanance.Core.Reminders;

/// <summary>Targets the current review of the same local source that scheduled the reminder.</summary>
public sealed class PeriodReviewReminderLink
{
    private const string Prefix = "review-reminder-v1|";
    private PeriodReviewReminderLink(string scope) => Scope = scope;

    /// <summary>Gets the opaque local file identity, without its path.</summary>
    public string Scope { get; }

    /// <summary>Formats the original source identity for the native notification.</summary>
    public static string Format(string scope)
    {
        if (!ValidScope(scope)) throw new ArgumentException("Invalid period-review reminder scope.", nameof(scope));
        return Prefix + scope;
    }

    /// <summary>Parses only the exact scoped notification format.</summary>
    public static PeriodReviewReminderLink? Parse(string? link) =>
        link?.Length == Prefix.Length + 64 && link.StartsWith(Prefix, StringComparison.Ordinal) &&
        ValidScope(link[Prefix.Length..]) ? new(link[Prefix.Length..]) : null;

    private static bool ValidScope(string scope) => scope.Length == 64 &&
        scope.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
