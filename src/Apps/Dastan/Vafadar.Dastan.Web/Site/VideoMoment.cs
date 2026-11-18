using System.Globalization;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// A moment of a video in an address (step 14.6, MED-014): <c>?t=75</c>, <c>?t=1:15</c> or <c>?t=1:02:03</c>.
/// The page hands the moment to the player of the browser with the address of the file (<c>#t=75</c>, a media
/// fragment), so the video starts there without a script. An address is written by anybody: what is no time
/// is no moment, and the video starts at its beginning.
/// </summary>
public static class VideoMoment
{
    // A day: no video of a collection is longer, and a larger number is no time anybody meant.
    private const int Longest = 24 * 60 * 60;

    /// <summary>Returns the seconds from the beginning that an address names, or <see langword="null"/> when it names no moment.</summary>
    public static int? Read(string? written)
    {
        if (string.IsNullOrEmpty(written) || written.Length > 8)
        {
            return null;
        }

        var parts = written.Split(':');
        if (parts.Length > 3 || parts.Any(part => part.Length is 0 or > 5 || !part.All(char.IsAsciiDigit)))
        {
            return null;
        }

        var numbers = parts.Select(part => int.Parse(part, NumberStyles.None, CultureInfo.InvariantCulture)).ToArray();

        // Written with colons, minutes and seconds are below sixty; a bare number is seconds, however many.
        if (parts.Length > 1 && numbers.Skip(1).Any(number => number > 59))
        {
            return null;
        }

        var seconds = numbers.Aggregate(0L, (sum, number) => (sum * 60) + number);
        return seconds is > 0 and <= Longest ? (int)seconds : null;
    }

    /// <summary>Returns a moment as it is written in an address and read by a person: <c>1:15</c>, <c>1:02:03</c>.</summary>
    public static string Write(int seconds)
    {
        var (hours, minutes, rest) = (seconds / 3600, seconds / 60 % 60, seconds % 60);
        return hours > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{hours}:{minutes:00}:{rest:00}")
            : string.Create(CultureInfo.InvariantCulture, $"{minutes}:{rest:00}");
    }
}
