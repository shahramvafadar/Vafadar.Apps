namespace Vafadar.Localization.Formatting;

/// <summary>
/// Formats dates for display using the user's current language and calendar.
/// </summary>
/// <remarks>
/// Use this for everything shown to the user. Never use it for persistence or data exchange;
/// those always use <see cref="System.Globalization.CultureInfo.InvariantCulture"/> and the Gregorian calendar.
/// </remarks>
public interface IDateFormatter
{
    /// <summary>Formats a calendar date.</summary>
    string Format(DateOnly date, DateFormatStyle style = DateFormatStyle.Short);

    /// <summary>Formats the local date part of a point in time.</summary>
    string Format(DateTimeOffset value, DateFormatStyle style = DateFormatStyle.Short);
}
