using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>A member of a tree that is to be laid out: who, in which generation, and the name a box has to hold.</summary>
/// <param name="Id">What identifies the member within the tree.</param>
/// <param name="Generation">The row of the member: smaller for parents than for their children. Rows need not be without gaps.</param>
/// <param name="Label">The name as the page shows it; the longest one decides how many lines a box needs.</param>
public sealed record TreeShapeMember(string Id, int Generation, string Label);

/// <summary>A link of a tree that is to be laid out, from one member to another.</summary>
/// <param name="From">The parent, or one partner.</param>
/// <param name="To">The child, or the other partner.</param>
public sealed record TreeShapeLink(string From, string To);

/// <summary>
/// A family as a layout sees it: members with their generations, lines from parents to children and lines
/// between partners. It says nothing about what a member is – an entity of a collection, the record of a person
/// in somebody's own tree – so that one layout serves every family the site draws.
/// </summary>
/// <param name="Members">The members, in the order in which they are to stand when nothing says otherwise.</param>
/// <param name="Parentage">The links from a parent to a child.</param>
/// <param name="Partnerships">The links between partners.</param>
public sealed record TreeShape(ImmutableArray<TreeShapeMember> Members, ImmutableArray<TreeShapeLink> Parentage, ImmutableArray<TreeShapeLink> Partnerships)
{
    /// <summary>
    /// Returns a generation for every member from the links alone: somebody without a parent stands in row 0,
    /// a child one below the lowest of its parents, and partners in the same row – the lower of the two, with
    /// everybody below them moved along. Links to members that are not named are passed over.
    /// </summary>
    public static ImmutableDictionary<string, int> Generations(IEnumerable<string> members, IEnumerable<TreeShapeLink> parentage, IEnumerable<TreeShapeLink> partnerships)
    {
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(parentage);
        ArgumentNullException.ThrowIfNull(partnerships);
        var rows = members.Distinct(StringComparer.Ordinal).ToDictionary(member => member, _ => 0, StringComparer.Ordinal);
        var down = parentage.Where(link => rows.ContainsKey(link.From) && rows.ContainsKey(link.To) && link.From != link.To).ToList();
        var beside = partnerships.Where(link => rows.ContainsKey(link.From) && rows.ContainsKey(link.To) && link.From != link.To).ToList();

        // Rows only ever grow, and no row can be lower than the number of members: a family in which somebody is
        // their own ancestor – which a tree does not allow – ends there instead of running for ever.
        for (var changed = true; changed;)
        {
            changed = false;
            foreach (var link in down.Where(link => rows[link.To] <= rows[link.From] && rows[link.From] < rows.Count))
            {
                rows[link.To] = rows[link.From] + 1;
                changed = true;
            }

            foreach (var link in beside.Where(link => rows[link.From] != rows[link.To]))
            {
                rows[link.From] = rows[link.To] = Math.Max(rows[link.From], rows[link.To]);
                changed = true;
            }
        }

        return rows.ToImmutableDictionary(StringComparer.Ordinal);
    }
}

/// <summary>A member of a tree with its place.</summary>
/// <param name="Id">The member.</param>
/// <param name="Layer">The generation, counted from the oldest one of the tree: 0, 1, 2, …</param>
/// <param name="Place">Where the member stands inside its generation, counted in boxes.</param>
public sealed record PlacedMember(string Id, int Layer, double Place);

/// <summary>How the line from a parent to a child runs (see <see cref="TreeRoute"/>).</summary>
/// <param name="From">The parent.</param>
/// <param name="To">The child.</param>
/// <param name="Port">Where the line leaves the box of the parent, as a share of the side of the box (0 to 1).</param>
/// <param name="Track">Where the line crosses over to the child, as a share of the room between two generations, counted from the child (0 to 1).</param>
public sealed record PlacedRoute(string From, string To, double Port, double Track);

/// <summary>A tree laid out: places are counts, not lengths (see <see cref="FamilyTree"/>).</summary>
/// <param name="Nodes">The members, generation by generation and inside one from its beginning.</param>
/// <param name="Parentage">The links from a parent to a child, each with the way its line runs, in the order of the shape.</param>
/// <param name="Partnerships">The links between partners.</param>
/// <param name="Layers">The number of generations.</param>
/// <param name="Breadth">The room the widest generation needs, in boxes.</param>
/// <param name="Lines">The number of lines the longest name needs in a box.</param>
/// <param name="Crossings">How many pairs of lines from parents to children cross.</param>
public sealed record TreePlacement(
    ImmutableArray<PlacedMember> Nodes,
    ImmutableArray<PlacedRoute> Parentage,
    ImmutableArray<TreeShapeLink> Partnerships,
    int Layers,
    double Breadth,
    int Lines,
    int Crossings)
{
    /// <summary>Returns the place of a member.</summary>
    public PlacedMember Node(string id) => Nodes.First(node => node.Id == id);
}

/// <summary>
/// Lays out a <see cref="TreeShape"/>: generations as layers, partners beside each other, and inside a
/// generation an order that lets as few lines cross as it finds. It is a layout of what the shape holds and adds
/// nothing: no member, no link and no order of birth.
/// </summary>
public static class TreeShapeLayout
{
    // Sweeps over the generations while looking for an order with fewer crossings, and while moving members
    // towards their parents and children. More change little in families of the size a page shows.
    private const int Sweeps = 6;

    /// <summary>Returns the placement of a shape.</summary>
    /// <param name="shape">The family.</param>
    /// <param name="charactersPerLine">How many characters of a name fit on a line of a box.</param>
    public static TreePlacement Of(TreeShape shape, int charactersPerLine = 11)
    {
        ArgumentNullException.ThrowIfNull(shape);
        ArgumentOutOfRangeException.ThrowIfLessThan(charactersPerLine, 1);

        if (shape.Members.IsEmpty)
        {
            return new TreePlacement([], [], shape.Partnerships, 0, 0, 0, 0);
        }

        // A member is named once, and a link to somebody who is not named is passed over – as
        // TreeShape.Generations does: a tree whose store lost a person is drawn without the link, not refused.
        var named = shape.Members.Select(member => member.Id).ToHashSet(StringComparer.Ordinal);
        shape = new TreeShape(
            [.. shape.Members.DistinctBy(member => member.Id, StringComparer.Ordinal)],
            [.. shape.Parentage.Where(link => named.Contains(link.From) && named.Contains(link.To))],
            [.. shape.Partnerships.Where(link => named.Contains(link.From) && named.Contains(link.To))]);

        // Generations become layers without gaps, in the order of the shape.
        var rows = shape.Members.Select(member => member.Generation).Distinct().Order().ToList();
        var layerOf = shape.Members.ToDictionary(member => member.Id, member => rows.IndexOf(member.Generation), StringComparer.Ordinal);
        var parents = shape.Members.ToDictionary(member => member.Id, _ => new List<string>(), StringComparer.Ordinal);
        var children = shape.Members.ToDictionary(member => member.Id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var link in shape.Parentage)
        {
            parents[link.To].Add(link.From);
            children[link.From].Add(link.To);
        }

        var layers = Enumerable.Range(0, rows.Count)
            .Select(layer => Units(shape, layerOf, layer))
            .ToList();

        // The order of the shape is the first candidate; a sweep is kept only when fewer lines cross.
        var best = Snapshot(layers);
        var fewest = Crossings(shape, layerOf, Index(layers));
        for (var sweep = 0; sweep < Sweeps && fewest > 0; sweep++)
        {
            for (var layer = 1; layer < layers.Count; layer++)
            {
                layers[layer] = Ordered(layers[layer], parents, Index(layers));
            }

            for (var layer = layers.Count - 2; layer >= 0; layer--)
            {
                layers[layer] = Ordered(layers[layer], children, Index(layers));
            }

            var crossings = Crossings(shape, layerOf, Index(layers));
            if (crossings < fewest)
            {
                (best, fewest) = (Snapshot(layers), crossings);
            }
        }

        var places = Places(best, parents, children);
        return new TreePlacement(
            [.. best.SelectMany((units, layer) => units.SelectMany(unit => unit)).Select(id => new PlacedMember(id, layerOf[id], places[id]))],
            Routes(shape, layerOf, places, parents),
            shape.Partnerships,
            rows.Count,
            places.Values.Max() + 1,
            shape.Members.Max(member => TextEstimate.Lines(member.Label, charactersPerLine)),
            fewest);
    }

    // The way of every line from a parent to a child. A family is the children of one set of parents in one
    // generation. In the room above a generation every family has a track of its own, and a parent of several
    // families a port for each – both in the order in which the children stand, so that the lines of two families
    // lie beside each other and not on each other.
    private static ImmutableArray<PlacedRoute> Routes(
        TreeShape shape,
        Dictionary<string, int> layerOf,
        Dictionary<string, double> places,
        Dictionary<string, List<string>> parents)
    {
        var families = shape.Parentage
            .Select(link => link.To)
            .Distinct()
            .GroupBy(child => (Layer: layerOf[child], Parents: string.Join('\n', parents[child].Order(StringComparer.Ordinal))))
            .Select(family => (family.Key.Layer, family.Key.Parents, Children: family.ToList(), Middle: family.Average(child => places[child])))
            .OrderBy(family => family.Middle)
            .ThenBy(family => family.Parents, StringComparer.Ordinal)
            .ToList();

        var track = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var layer in families.GroupBy(family => family.Layer))
        {
            var above = layer.ToList();
            for (var index = 0; index < above.Count; index++)
            {
                foreach (var child in above[index].Children)
                {
                    track[child] = (index + 1.0) / (above.Count + 1);
                }
            }
        }

        // The families of a parent, in the order of where their children stand.
        var port = new Dictionary<(string Parent, string Child), double>();
        foreach (var parent in shape.Parentage.Select(link => link.From).Distinct())
        {
            var own = families.Where(family => family.Children.Any(child => parents[child].Contains(parent))).ToList();
            for (var index = 0; index < own.Count; index++)
            {
                foreach (var child in own[index].Children)
                {
                    port[(parent, child)] = (index + 1.0) / (own.Count + 1);
                }
            }
        }

        return [.. shape.Parentage.Select(link => new PlacedRoute(link.From, link.To, port[(link.From, link.To)], track[link.To]))];
    }

    // The members of a generation as units that stay together: partners stand beside each other, somebody with
    // several partners between them. The order is that of the shape.
    private static List<List<string>> Units(TreeShape shape, Dictionary<string, int> layerOf, int layer)
    {
        var members = shape.Members.Where(member => layerOf[member.Id] == layer).Select(member => member.Id).ToList();
        var partners = members.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var link in shape.Partnerships.Where(link => layerOf[link.From] == layer && layerOf[link.To] == layer && link.From != link.To))
        {
            partners[link.From].Add(link.To);
            partners[link.To].Add(link.From);
        }

        var units = new List<List<string>>();
        var placed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in members.Where(member => !placed.Contains(member)))
        {
            // Everybody joined to the member by partnerships, found from the member outwards.
            var group = new List<string> { member };
            placed.Add(member);
            for (var index = 0; index < group.Count; index++)
            {
                foreach (var partner in partners[group[index]].Where(placed.Add))
                {
                    group.Add(partner);
                }
            }

            if (group.Count > 2)
            {
                // The one with the most partners stands in the middle, the others on both sides in turn.
                var hub = group.MaxBy(id => partners[id].Count)!;
                var others = group.Where(id => id != hub).ToList();
                group = [.. others.Where((_, index) => index % 2 == 0), hub, .. others.Where((_, index) => index % 2 == 1)];
            }

            units.Add(group);
        }

        return units;
    }

    private static List<List<List<string>>> Snapshot(List<List<List<string>>> layers) =>
        [.. layers.Select(units => units.Select(unit => unit.ToList()).ToList())];

    // The position of every member inside its generation, counted from 0.
    private static Dictionary<string, int> Index(List<List<List<string>>> layers)
    {
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var units in layers)
        {
            var position = 0;
            foreach (var id in units.SelectMany(unit => unit))
            {
                index[id] = position++;
            }
        }

        return index;
    }

    // A generation ordered by where the neighbours of its units stand: each unit moves towards the middle of the
    // parents (or the children) of its members. A unit without such neighbours keeps its place.
    private static List<List<string>> Ordered(List<List<string>> units, Dictionary<string, List<string>> neighbours, Dictionary<string, int> index)
    {
        var ordered = units
            .Select(unit =>
            {
                var pair = unit.Count == 2 && Middle(unit[0]) is { } first && Middle(unit[1]) is { } second && second < first
                    ? [unit[1], unit[0]]
                    : unit;
                var near = pair.SelectMany(id => neighbours[id]).Select(id => (double)index[id]).ToList();
                return (Unit: pair, Middle: near.Count > 0 ? near.Average() : pair.Average(id => index[id]));
            })
            .ToList();

        // A stable sort: units with the same middle keep their order, so the result does not depend on chance.
        return [.. ordered.Select((entry, position) => (entry, position)).OrderBy(item => item.entry.Middle).ThenBy(item => item.position).Select(item => item.entry.Unit)];

        double? Middle(string id) => neighbours[id].Count > 0 ? neighbours[id].Average(neighbour => index[neighbour]) : null;
    }

    // Two lines from parents to children cross when they join the same two generations and their ends stand in
    // opposite orders.
    private static int Crossings(TreeShape shape, Dictionary<string, int> layerOf, Dictionary<string, int> index)
    {
        var lines = shape.Parentage
            .Select(link => (Layers: (layerOf[link.From], layerOf[link.To]), From: index[link.From], To: index[link.To]))
            .ToList();
        var crossings = 0;
        for (var first = 0; first < lines.Count; first++)
        {
            for (var second = first + 1; second < lines.Count; second++)
            {
                if (lines[first].Layers == lines[second].Layers
                    && (lines[first].From - lines[second].From) * (lines[first].To - lines[second].To) < 0)
                {
                    crossings++;
                }
            }
        }

        return crossings;
    }

    // Where the members stand: at first one box after the other, then moved towards the middle of their parents
    // and of their children as far as the order and the room of a box allow.
    private static Dictionary<string, double> Places(
        List<List<List<string>>> layers,
        Dictionary<string, List<string>> parents,
        Dictionary<string, List<string>> children)
    {
        var places = Index(layers).ToDictionary(entry => entry.Key, entry => (double)entry.Value, StringComparer.Ordinal);
        for (var sweep = 0; sweep < Sweeps; sweep++)
        {
            for (var layer = 1; layer < layers.Count; layer++)
            {
                Settle(layers[layer], parents, places);
            }

            // The last movement is the one from below: parents end up above the middle of their children.
            for (var layer = layers.Count - 2; layer >= 0; layer--)
            {
                Settle(layers[layer], children, places);
            }
        }

        var first = places.Values.Min();
        return places.ToDictionary(entry => entry.Key, entry => Math.Round(entry.Value - first, 4), StringComparer.Ordinal);
    }

    // Moves the units of one generation as near to where their neighbours stand as possible without changing
    // their order or letting two boxes share a place: the least squares solution, found by merging units that
    // would overlap into blocks that move together.
    private static void Settle(List<List<string>> units, Dictionary<string, List<string>> neighbours, Dictionary<string, double> places)
    {
        // A unit wants its members above the middle of their neighbours; "before" is the room of the units to its left.
        var wanted = new List<double>(units.Count);
        var before = 0;
        foreach (var unit in units)
        {
            var wishes = unit
                .Select((id, offset) => neighbours[id].Count > 0 ? neighbours[id].Average(neighbour => places[neighbour]) - offset : (double?)null)
                .OfType<double>()
                .ToList();
            wanted.Add((wishes.Count > 0 ? wishes.Average() : places[unit[0]]) - before);
            before += unit.Count;
        }

        // Blocks of units that move together: (sum of wishes, number of units).
        var blocks = new List<(double Sum, int Count)>();
        foreach (var wish in wanted)
        {
            blocks.Add((wish, 1));
            while (blocks.Count > 1 && blocks[^2].Sum / blocks[^2].Count > blocks[^1].Sum / blocks[^1].Count)
            {
                blocks[^2] = (blocks[^2].Sum + blocks[^1].Sum, blocks[^2].Count + blocks[^1].Count);
                blocks.RemoveAt(blocks.Count - 1);
            }
        }

        var index = 0;
        before = 0;
        foreach (var (sum, count) in blocks)
        {
            for (var member = 0; member < count; member++)
            {
                var unit = units[index++];
                for (var offset = 0; offset < unit.Count; offset++)
                {
                    places[unit[offset]] = (sum / count) + before + offset;
                }

                before += unit.Count;
            }
        }
    }
}
