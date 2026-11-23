using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>A place people can have been at, as the chooser of a form and a page about it need it.</summary>
/// <param name="Reference">Which entity of which collection it is.</param>
/// <param name="Entity">The entity, as everybody may see it: its name, its kind, its other and earlier names, its facts.</param>
/// <param name="Collection">The subject of its collection – what tells two places of one name apart when nothing else does.</param>
/// <param name="Seat">Where it is – its seat, as the collection states it and everybody may see it – or nothing.</param>
public sealed record Institution(EntityReference Reference, Entity Entity, Entity Collection, Entity? Seat = null);

/// <summary>
/// The places of the published collections that people can have been at – universities, companies, clubs,
/// teams, other organisations (step 20.4, ADR 0023). It is read from what everybody may see of each
/// collection, and it is all the personal side of the site knows of the collections: a private statement can
/// point at one of these, and is told nothing else about them.
/// <para>
/// Finding is by name – the name, other names, earlier names, in every language the collection has them, with
/// the letters brought together as the search of the site does. That finds; it decides nothing: two entities
/// that share a name are two entries, and the visitor chooses one or none.
/// </para>
/// <para>
/// Whether an entity of one collection is the same place as an entity of another is said by the editors, in
/// the one way the collections have for it: both carry the same identifier of the same outside catalogue,
/// marked as exact (<see cref="ExternalIdentifier"/>, DOM-003). Nothing else makes two entities one – not a
/// name, not a place, not a kind.
/// </para>
/// </summary>
public sealed class Institutions
{
    private readonly ImmutableArray<(string Collection, Projection Projection)> _collections;
    private readonly Dictionary<EntityReference, Institution> _all = [];
    private readonly Dictionary<(string Issuer, string Value), List<EntityReference>> _byIdentifier = [];

    /// <summary>Creates the directory from what everybody may see of each collection.</summary>
    /// <param name="collections">The collections by their identifiers, each as a visitor who has read nothing sees it.</param>
    public Institutions(IEnumerable<(string Collection, Projection Projection)> collections)
    {
        ArgumentNullException.ThrowIfNull(collections);
        _collections = [.. collections];
        foreach (var (collection, projection) in _collections)
        {
            var subject = projection.Overview().Subject;
            foreach (var entity in projection.Entities.Where(entity => Is(entity.Type)))
            {
                var reference = new EntityReference(collection, entity.Id);
                var seat = projection.Profile(entity.Id)?.Essential.Concat(projection.Profile(entity.Id)!.More)
                    .Where(line => line.Kind == ProfileFacts.Seat)
                    .SelectMany(line => line.Stated)
                    .Select(fact => fact.Node is { } node ? projection.Find(node) : null)
                    .FirstOrDefault(place => place is not null);
                _all[reference] = new(reference, entity, subject, seat);
                foreach (var identifier in entity.Identifiers.Where(identifier => identifier.Match == IdentifierMatch.Exact))
                {
                    var key = (identifier.Issuer.Trim().ToUpperInvariant(), identifier.Value.Trim());
                    if (!_byIdentifier.TryGetValue(key, out var named))
                    {
                        _byIdentifier[key] = named = [];
                    }

                    named.Add(reference);
                }
            }
        }
    }

    /// <summary>A directory of nothing: a site without collections, or a test that needs none.</summary>
    public static Institutions None { get; } = new([]);

    /// <summary>
    /// Returns whether entities of a type are places people can have been at. A family, a dynasty and a people
    /// are groups too, and are not: nobody attends one.
    /// </summary>
    public static bool Is(EntityType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Types.Contains(type);
    }

    /// <summary>
    /// Gets the types of entity that are places people can have been at, in the order a form offers them: a
    /// visitor chooses among these when proposing a place to the editors (D-085).
    /// </summary>
    public static ImmutableArray<EntityType> Types { get; } =
        [EntityTypes.University, EntityTypes.Company, EntityTypes.Organisation, EntityTypes.SportsClub, EntityTypes.SportsTeam, EntityTypes.Government];

    /// <summary>Returns the place a reference names, or nothing: no such entity is published, or it is no such place.</summary>
    public Institution? Find(EntityReference? reference) => reference is not null && _all.TryGetValue(reference, out var found) ? found : null;

    /// <summary>
    /// Returns the places a visitor may mean by some words, the best first – at most <paramref name="most"/>.
    /// Places of one name in several collections are all there, each by itself.
    /// </summary>
    public ImmutableArray<Institution> Search(string? words, int most = 12)
    {
        if (string.IsNullOrWhiteSpace(words) || most <= 0)
        {
            return [];
        }

        var found = new List<Institution>();
        foreach (var (collection, projection) in _collections)
        {
            foreach (var hit in projection.Search(words, new SearchFilter(EntityNature.Group), most))
            {
                if (Find(new EntityReference(collection, hit.Entity.Id)) is { } institution && !found.Contains(institution))
                {
                    found.Add(institution);
                }
            }
        }

        return [.. found.Take(most)];
    }

    /// <summary>
    /// Returns a place with every entity the editors say is the same place in another collection: those that
    /// share an exact identifier of an outside catalogue with it, and those that share one with one of those.
    /// A reference that names no place of the directory is returned alone.
    /// </summary>
    public ImmutableArray<EntityReference> Same(EntityReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var same = new List<EntityReference> { reference };
        for (var next = 0; next < same.Count; next++)
        {
            if (!_all.TryGetValue(same[next], out var institution))
            {
                continue;
            }

            foreach (var identifier in institution.Entity.Identifiers.Where(identifier => identifier.Match == IdentifierMatch.Exact))
            {
                foreach (var other in _byIdentifier[(identifier.Issuer.Trim().ToUpperInvariant(), identifier.Value.Trim())])
                {
                    if (!same.Contains(other))
                    {
                        same.Add(other);
                    }
                }
            }
        }

        return [.. same];
    }
}
