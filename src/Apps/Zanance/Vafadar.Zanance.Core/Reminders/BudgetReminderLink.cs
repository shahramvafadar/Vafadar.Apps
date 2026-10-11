namespace Vafadar.Zanance.Core.Reminders;

/// <summary>One original saved budget period tied to its actual profile file; never a financial authorization.</summary>
public sealed class BudgetReminderLink
{
    private const string Prefix = "budget-reminder-v1";
    private BudgetReminderLink(string scope, Guid budgetId) { Scope = scope; BudgetId = budgetId; }
    /// <summary>Gets the opaque actual-file identity, without a path or credentials.</summary>
    public string Scope { get; }
    /// <summary>Gets the original stored period, including its original currency and calendar.</summary>
    public Guid BudgetId { get; }
    /// <summary>Formats only a validated original identity for the native returning-data field.</summary>
    public static string Format(string scope, Guid budgetId)
    {
        if (!Valid(scope) || budgetId == Guid.Empty) throw new ArgumentException("Invalid budget reminder identity.");
        return Prefix + "|" + scope + "|" + budgetId.ToString("N");
    }
    /// <summary>Rejects malformed, oversized and unscoped payloads instead of guessing a profile or budget.</summary>
    public static BudgetReminderLink? Parse(string? link) =>
        link is { Length: 116 } && link.Split('|') is [Prefix, var scope, var id] && Valid(scope)
        && Guid.TryParseExact(id, "N", out var budgetId) && budgetId != Guid.Empty ? new(scope, budgetId) : null;
    private static bool Valid(string? scope) => scope is { Length: 64 }
        && scope.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
