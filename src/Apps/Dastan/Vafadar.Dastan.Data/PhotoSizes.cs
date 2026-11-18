namespace Vafadar.Dastan.Data;

/// <summary>
/// The smaller size of a photograph of a visitor (D-056): one, beside the photograph, under a name made from
/// its name. The store of the visitors knows nothing of it – a photograph that has none is given as it is.
/// </summary>
public static class PhotoSizes
{
    /// <summary>The longer side of the smaller size, in pixels: enough for a card on a screen that draws two pixels for one.</summary>
    public const int SmallSide = 800;

    private const string Mark = "-small";

    /// <summary>Returns the name of the smaller size of a photograph, made from the name of its file.</summary>
    public static string SmallName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return $"{Path.GetFileNameWithoutExtension(name)}{Mark}{Path.GetExtension(name)}";
    }

    /// <summary>Returns the name of the photograph a smaller size belongs to, or nothing for a name that is not one of a smaller size.</summary>
    public static string? OfSmall(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var stem = Path.GetFileNameWithoutExtension(name);
        return stem.EndsWith(Mark, StringComparison.Ordinal) ? stem[..^Mark.Length] + Path.GetExtension(name) : null;
    }

    /// <summary>
    /// Makes the smaller size of every photograph of a folder that has none yet and is large enough to have one
    /// (step 18.3.1): for the photographs that were brought in before the site made sizes. Returns how many
    /// were made. A file that is no picture is left as it is.
    /// </summary>
    /// <param name="folder">The folder the photographs lie in.</param>
    public static int MakeMissing(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        var made = 0;
        foreach (var file in Directory.EnumerateFiles(folder).Order(StringComparer.Ordinal).ToList())
        {
            var name = Path.GetFileName(file);
            if (name[0] == '.' || OfSmall(name) is not null || File.Exists(Path.Combine(folder, SmallName(name))))
            {
                continue;
            }

            if (ImageSizes.Smaller(file, SmallSide, SmallName(name)) is not null)
            {
                made++;
            }
        }

        return made;
    }
}
