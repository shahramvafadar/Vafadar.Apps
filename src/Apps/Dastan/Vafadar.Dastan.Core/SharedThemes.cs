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
        var groups = new List<List<ThemeOf>>();
        var seen = new List<ThemeOf>();
        var byIdentifier = new Dictionary<(string Issuer, string Value), List<ThemeOf>>();
        foreach (var (collection, projection) in collections)
        {
            var subject = projection.Overview().Subject;
            foreach (var counted in projection.Themes())
            {
                var theme = new ThemeOf(collection, subject, counted.Theme, counted.Items);
                var keys = counted.Theme.Identifiers
                    .Where(identifier => identifier.Match == IdentifierMatch.Exact)
                    .Select(identifier => (identifier.Issuer.Trim().ToUpperInvariant(), identifier.Value.Trim()))
                    .ToList();

                // The groups this theme belongs to by one of its identifiers become one group, with it – as far
                // as that leaves every collection in the group once. Two themes of one collection are two themes
                // (a collection has a theme once), also when a theme of another collection carries the
                // identifiers of both: that theme then joins the first of them, and the two stay apart.
                var joined = keys.Where(byIdentifier.ContainsKey).Select(key => byIdentifier[key]).Distinct()
                    .Where(found => found.All(one => one.Collection != collection)).ToList();
                var group = joined.Count == 0 ? [] : joined[0];
                if (joined.Count == 0)
                {
                    groups.Add(group);
                }

                foreach (var other in joined.Skip(1).Where(other => !other.Any(one => group.Any(kept => kept.Collection == one.Collection))).ToList())
                {
                    group.AddRange(other);
                    groups.Remove(other);
                    foreach (var key in byIdentifier.Where(pair => ReferenceEquals(pair.Value, other)).Select(pair => pair.Key).ToList())
                    {
                        byIdentifier[key] = group;
                    }
                }

                group.Add(theme);
                seen.Add(theme);
                foreach (var key in keys.Where(key => !byIdentifier.ContainsKey(key)))
                {
                    byIdentifier[key] = group;
                }
            }
        }

        // Joining groups does not change the order of the site: a theme stands where its first collection has it.
        return
        [
            .. groups
                .Select(group => group.OrderBy(seen.IndexOf).ToList())
                .OrderBy(group => seen.IndexOf(group[0]))
                .Select(group => new SharedTheme([.. group])),
        ];
    }

    /// <summary>Returns the theme of the site that a theme of a collection is, or nothing: the reader may not see it, or nothing stands under it.</summary>
    /// <param name="themes">The themes of the site.</param>
    /// <param name="collection">The collection.</param>
    /// <param name="theme">The theme of that collection.</param>
    public static SharedTheme? Find(this ImmutableArray<SharedTheme> themes, string collection, EntityId theme) =>
        themes.FirstOrDefault(shared => shared.In.Any(one => one.Collection == collection && one.Theme.Id == theme));
}
