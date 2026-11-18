using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The coasts, rivers and lakes the site draws under the places of a collection (D-057). They come with the
/// site – files of Natural Earth, public domain, kept in the program – and are read once, when a page first
/// asks for them. No request leaves the site for a map, and no border of a country is among them.
/// </summary>
public static class CoastData
{
    private static readonly Lazy<Coasts> Coasts = new(() => Read("coasts"));
    private static readonly Lazy<Coasts> RiversAndLakes = new(() => Read("waters"));

    /// <summary>Gets the coasts of the earth.</summary>
    public static Coasts OfTheEarth => Coasts.Value;

    /// <summary>Gets the rivers of the earth and the shores of its lakes.</summary>
    public static Coasts Waters => RiversAndLakes.Value;

    private static Coasts Read(string name)
    {
        using var file = typeof(CoastData).Assembly.GetManifestResourceStream($"Vafadar.Dastan.Web.Data.{name}.txt")
            ?? throw new InvalidOperationException($"The file {name} is not in the program.");
        using var text = new StreamReader(file);
        return Vafadar.Dastan.Core.Coasts.Read(text);
    }
}
