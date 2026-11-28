using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>
/// A kind of record under its type (ADR 0029): a battle, a drought, a poet, a hero. The type says what a record
/// is and decides its profile; a kind says what sort of that it is, so that pages can mark and filter it. The
/// kinds are a list of the library (<c>kinds.json</c>), edited like a collection; a kind that is no longer
/// wanted is retired, never deleted, so that what was given it keeps its meaning.
/// </summary>
public sealed class Kind
{
    /// <summary>Gets the stable name of the kind, as records write it.</summary>
    public required string Key { get; init; }

    /// <summary>Gets what the kind is called.</summary>
    public required LocalizedText Label { get; init; }

    /// <summary>Gets what the kind means, in a sentence for who writes and who reads.</summary>
    public required LocalizedText Meaning { get; init; }

    /// <summary>Gets the types of entity the kind may be given to.</summary>
    public required ImmutableArray<EntityType> Types { get; init; }

    /// <summary>Gets the name of the drawing of the site that stands beside the kind.</summary>
    public required string Icon { get; init; }

    /// <summary>Gets the wider kind this one belongs to – a battle to a war – or nothing.</summary>
    public Kind? Parent { get; init; }

    /// <summary>Gets a value indicating whether the kind is no longer offered for new records; what has it keeps it.</summary>
    public bool Retired { get; init; }

    /// <summary>Gets the place of the kind in its list: kinds are offered in this order.</summary>
    public int Order { get; init; }

    /// <summary>Gets the kind and every wider kind above it, the kind itself first.</summary>
    public IEnumerable<Kind> Lineage
    {
        get
        {
            for (var kind = this; kind is not null; kind = kind.Parent)
            {
                yield return kind;
            }
        }
    }

    /// <summary>Returns whether the kind may be given to a record of a type.</summary>
    public bool Allows(EntityType type) => Types.Contains(type);

    /// <inheritdoc />
    public override string ToString() => Key;
}

/// <summary>The list of kinds of a library (ADR 0029), in the order of its file.</summary>
public sealed class KindList
{
    private readonly FrozenDictionary<string, Kind> _byKey;

    /// <summary>Initializes a new instance of the <see cref="KindList"/> class.</summary>
    /// <param name="kinds">The kinds, in their order; each key once.</param>
    public KindList(IEnumerable<Kind> kinds)
    {
        ArgumentNullException.ThrowIfNull(kinds);
        All = [.. kinds];
        _byKey = All.ToFrozenDictionary(kind => kind.Key, StringComparer.Ordinal);
    }

    /// <summary>Gets a list without kinds: what a content root without <c>kinds.json</c> has.</summary>
    public static KindList Empty { get; } = new([]);

    /// <summary>Gets every kind, in the order of the list.</summary>
    public ImmutableArray<Kind> All { get; }

    /// <summary>Returns the kind of that key, or <see langword="null"/> when the list has none.</summary>
    public Kind? Find(string key) => _byKey.GetValueOrDefault(key);
}

/// <summary>
/// How a page narrows what it shows by kind (ADR 0029): several kinds may be chosen, and a wider kind takes in
/// the kinds below it. The choice is part of the address – one value with commas between the keys.
/// </summary>
public static class KindFilter
{
    /// <summary>The name of the choice in the address of a page.</summary>
    public const string Parameter = "kinds";

    /// <summary>
    /// Reads the keys an address asks for: values of the parameter, each with commas between keys. Only keys
    /// among <paramref name="offered"/> are kept – a key that is no kind of the page is not asked for.
    /// </summary>
    /// <param name="asked">The values of the parameter in the address.</param>
    /// <param name="offered">The kinds the page offers.</param>
    public static ImmutableHashSet<string> Read(IEnumerable<string?>? asked, IEnumerable<Kind> offered)
    {
        ArgumentNullException.ThrowIfNull(offered);
        var known = offered.Select(kind => kind.Key).ToHashSet(StringComparer.Ordinal);
        return [.. (asked ?? [])
            .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(known.Contains)];
    }

    /// <summary>
    /// Returns the kinds a page offers for what it shows: every kind given to one of the records and each wider
    /// kind above it, in the order of the list. A kind nothing shown has is not offered.
    /// </summary>
    /// <param name="shown">The kinds of each record the page shows.</param>
    public static ImmutableArray<Kind> Offered(IEnumerable<ImmutableArray<Kind>> shown)
    {
        ArgumentNullException.ThrowIfNull(shown);
        return [.. shown.SelectMany(kinds => kinds).SelectMany(kind => kind.Lineage).DistinctBy(kind => kind.Key).OrderBy(kind => kind.Order).ThenBy(kind => kind.Key, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Returns whether a record with these kinds passes the choice: when nothing is chosen, or when one of its
    /// kinds or a wider kind above one of them is chosen.
    /// </summary>
    /// <param name="kinds">The kinds of the record.</param>
    /// <param name="chosen">The keys chosen.</param>
    public static bool Passes(ImmutableArray<Kind> kinds, IReadOnlySet<string> chosen)
    {
        ArgumentNullException.ThrowIfNull(chosen);
        return chosen.Count == 0 || kinds.SelectMany(kind => kind.Lineage).Any(kind => chosen.Contains(kind.Key));
    }

    /// <summary>Returns the value of the parameter for a choice, the keys in the order of the offered kinds; empty for no choice.</summary>
    /// <param name="chosen">The keys chosen.</param>
    /// <param name="offered">The kinds the page offers, in their order.</param>
    public static string Written(IEnumerable<string> chosen, IEnumerable<Kind> offered)
    {
        ArgumentNullException.ThrowIfNull(chosen);
        ArgumentNullException.ThrowIfNull(offered);
        var set = chosen.ToHashSet(StringComparer.Ordinal);
        return string.Join(',', offered.Select(kind => kind.Key).Where(set.Contains));
    }
}
