using System.Security.Cryptography;
using System.Text;

namespace Vafadar.Dastan.Studio.Site;

/// <summary>
/// What a form was made from, as a mark the form carries (step 13.2). When the form comes back and the store
/// holds something else by then, somebody changed it in between – and what was filled in is not kept over it
/// without the editor knowing.
/// </summary>
public static class Seen
{
    /// <summary>The name of the input that carries the mark.</summary>
    public const string Input = "seen";

    /// <summary>Returns the mark of what a form shows: the same parts give the same mark, anything else another.</summary>
    public static string Of(IEnumerable<string?> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        var text = new StringBuilder();
        foreach (var part in parts)
        {
            // The length before each part keeps two lists of parts from reading as the same text.
            text.Append(part?.Length ?? -1).Append(':').Append(part).Append('\n');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }

    /// <summary>Returns whether a form that was sent still stands on what the store holds.</summary>
    public static bool Holds(HttpContext context, string now)
    {
        ArgumentNullException.ThrowIfNull(context);
        return string.Equals(context.Request.Form[Input].ToString(), now, StringComparison.Ordinal);
    }
}
