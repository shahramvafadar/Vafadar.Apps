using System.Globalization;
using Vafadar.Web;

namespace Vafadar.Dastan.Studio.Site;

/// <summary>What an editor fills in to record the review of a statement.</summary>
public sealed class ReviewInput
{
    /// <summary>Gets or sets who reviews.</summary>
    public string? By { get; set; }

    /// <summary>Gets or sets the statement: the identifier of a relation or an event, the key of a dated statement.</summary>
    public string? Statement { get; set; }

    /// <summary>Gets or sets what the review found: <c>reviewed</c>, <c>disputed</c> or <c>withdrawn</c>.</summary>
    public string? As { get; set; }

    /// <summary>Gets or sets the day of the review, as <c>yyyy-MM-dd</c>.</summary>
    public string? On { get; set; }

    /// <summary>Gets or sets the note of the review.</summary>
    public string? Note { get; set; }
}

/// <summary>What an editor fills in to put a record into a collection.</summary>
public sealed class ChangeInput
{
    /// <summary>Gets or sets who changes the record.</summary>
    public string? By { get; set; }

    /// <summary>Gets or sets the record as it is written in the file of a collection.</summary>
    public string? Json { get; set; }

    /// <summary>Gets or sets why.</summary>
    public string? Reason { get; set; }
}

/// <summary>What an editor fills in to take a record out of a collection.</summary>
public sealed class RemoveInput
{
    /// <summary>Gets or sets who takes it out.</summary>
    public string? By { get; set; }

    /// <summary>Gets or sets why.</summary>
    public string? Reason { get; set; }
}

/// <summary>What an editor fills in to publish the next revision.</summary>
public sealed class PublishInput
{
    /// <summary>Gets or sets who publishes.</summary>
    public string? By { get; set; }

    /// <summary>Gets or sets the day the revision is published, as <c>yyyy-MM-dd</c>.</summary>
    public string? On { get; set; }

    /// <summary>Gets or sets what the editors say about the revision.</summary>
    public string? Note { get; set; }
}

/// <summary>
/// The name an editor gave with the last act, kept in a cookie of the tool so that the next form starts with it.
/// It is a convenience, not an identity: the tool does not know who an editor is (ADR 0011).
/// </summary>
public static class EditorName
{
    /// <summary>The name of the cookie.</summary>
    public const string CookieName = "dastan.studio.editor";

    /// <summary>Returns the name of the last act, or nothing.</summary>
    public static string? Read(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var kept = context.Request.Cookies[CookieName];
        return string.IsNullOrWhiteSpace(kept) ? null : Uri.UnescapeDataString(kept);
    }

    /// <summary>Keeps the name for the next form.</summary>
    public static void Keep(HttpContext context, string name)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        context.Response.Cookies.Append(CookieName, Uri.EscapeDataString(name.Trim()), PreferenceCookie.Options(context));
    }
}

/// <summary>What the forms of the tool share.</summary>
public static class ActInput
{
    /// <summary>Reads a day a form was given, as the browser writes it.</summary>
    public static DateOnly? Day(string? text) =>
        DateOnly.TryParseExact(text?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) ? day : null;

    /// <summary>Writes a day for a form.</summary>
    public static string Day(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Returns a text a form was given, without the space around it; nothing when it is empty.</summary>
    public static string? Text(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
