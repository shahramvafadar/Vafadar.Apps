using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>A theme as one collection has it, as far as a reader has read that collection.</summary>
/// <param name="Collection">The identifier of the collection.</param>
/// <param name="Subject">What the collection is about: the name it is called by.</param>
/// <param name="Theme">The theme, an entity of that collection.</param>
/// <param name="Items">How many images, videos and references to works the reader may see under it there.</param>
public sealed record ThemeOf(string Collection, Entity Subject, Entity Theme, int Items);

/// <summary>
/// A theme of the site: one theme of one collection, or the same theme in several (D-083, delivery plan 25.4).
/// </summary>
/// <param name="In">The collections that have it, in the order of the site; never empty.</param>
public sealed record SharedTheme(ImmutableArray<ThemeOf> In)
{
    /// <summary>Gets a value indicating whether the editors say that several collections have this theme.</summary>
    public bool IsShared => In.Length > 1;

    /// <summary>Gets how much stands under the theme in all its collections.</summary>
    public int Items => In.Sum(one => one.Items);

    /// <summary>Returns the theme as a collection has it, or the first.</summary>
    /// <param name="collection">The collection a visitor came from, or nothing.</param>
    public ThemeOf From(string? collection) => In.FirstOrDefault(one => one.Collection == collection) ?? In[0];
}

/// <summary>
/// The themes of a site across its collections. Whether a theme of one collection is the same theme as one of
/// another is said by the editors, in the one way the collections have for it (ADR 0023, ADR 0027): both carry
/// the same identifier of the same outside catalogue, marked as exact. Nothing else makes two themes one – not
/// a name, not what stands under them. A theme is there only as far as the reader has read its collection, and
/// only with something under it: what a later stage holds is neither named nor counted.
/// </summary>
public static class SharedThemes
{
    /// <summary>Returns the themes of the collections, the shared ones once, in the order of the site.</summary>
    /// <param name="collections">The collections by their identifiers, each as this reader may see it.</param>
    public static ImmutableArray<SharedTheme> Of(IEnumerable<(string Collection, Projection Projection)> collections)
    {
        ArgumentNullException.ThrowIfNull(collections);

        // Every theme a reader may see, in the order of the site, with the identifiers that say what it is.
        var themes = new List<(ThemeOf Theme, List<(string Issuer, string Value)> Keys)>();
        foreach (var (collection, projection) in collections)
        {
            var subject = projection.Overview().Subject;
            foreach (var counted in projection.Themes())
            {
                themes.Add((new ThemeOf(collection, subject, counted.Theme, counted.Items),
                [
                    .. counted.Theme.Identifiers
                        .Where(identifier => identifier.Match == IdentifierMatch.Exact)
                        .Select(identifier => (identifier.Issuer.Trim().ToUpperInvariant(), identifier.Value.Trim())),
                ]));
            }
        }

        // What an identifier ties together, and what is tied through that: themes that share one are of one
        // set, and so are themes that each share one with a third. A set is the same whatever the order of the
        // collections, of the themes and of the identifiers of a theme.
        var set = Enumerable.Range(0, themes.Count).ToArray();
        int Of(int index)
        {
            while (set[index] != index)
            {
                index = set[index] = set[set[index]];
            }

            return index;
        }

        var first = new Dictionary<(string Issuer, string Value), int>();
        for (var index = 0; index < themes.Count; index++)
        {
            foreach (var key in themes[index].Keys)
            {
                if (first.TryGetValue(key, out var other))
                {
                    // The earlier of the two stands for the set, so that a set stands where its first theme does.
                    var (one, two) = (Of(index), Of(other));
                    set[Math.Max(one, two)] = Math.Min(one, two);
                }
                else
                {
                    first[key] = index;
                }
            }
        }

        // A set is one theme of the site – unless it holds two themes of one collection. A collection has a
        // theme once; a set that says otherwise contradicts itself (one collection tells two themes apart that
        // another calls one), and then nothing of it is joined: each of its themes stands by itself, until the
        // editors say which belongs to which.
        return
        [
            .. Enumerable.Range(0, themes.Count)
                .GroupBy(Of)
                .OrderBy(group => group.Key)
                .SelectMany(group =>
                {
                    var members = group.Select(index => themes[index].Theme).ToList();
                    return members.Select(member => member.Collection).Distinct(StringComparer.Ordinal).Count() == members.Count
                        ? [new SharedTheme([.. members])]
                        : members.Select(member => new SharedTheme([member]));
                }),
        ];
    }

    /// <summary>Returns the theme of the site that a theme of a collection is, or nothing: the reader may not see it, or nothing stands under it.</summary>
    /// <param name="themes">The themes of the site.</param>
    /// <param name="collection">The collection.</param>
    /// <param name="theme">The theme of that collection.</param>
    public static SharedTheme? Find(this ImmutableArray<SharedTheme> themes, string collection, EntityId theme) =>
        themes.FirstOrDefault(shared => shared.In.Any(one => one.Collection == collection && one.Theme.Id == theme));
}
