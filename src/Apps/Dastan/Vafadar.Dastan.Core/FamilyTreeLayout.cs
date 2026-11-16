using System.Collections.Immutable;

namespace Vafadar.Dastan.Core;

/// <summary>A member of a family tree with its place.</summary>
/// <param name="Entity">The member.</param>
/// <param name="Layer">The generation, counted from the oldest one of the tree: 0, 1, 2, …</param>
/// <param name="Place">
/// Where the member stands inside its generation, counted in boxes: two members of a generation are at least 1
/// apart. The page turns it into a length.
/// </param>
public sealed record TreeNode(Entity Entity, int Layer, double Place);

/// <summary>
/// A family laid out as a tree. Places are counts, not lengths: the layout says who stands in which generation and
/// where in it, the page decides how large a box is and whether generations run down the page or across it.
/// </summary>
/// <param name="Nodes">The members, generation by generation and inside one from its beginning.</param>
/// <param name="Parentage">The relations from a parent to a child.</param>
/// <param name="Partnerships">The relations between partners.</param>
/// <param name="Layers">The number of generations.</param>
/// <param name="Breadth">The room the widest generation needs, in boxes.</param>
/// <param name="Lines">The number of lines the longest name needs in a box.</param>
/// <param name="Crossings">How many pairs of lines from parents to children cross.</param>
public sealed record FamilyTree(
    ImmutableArray<TreeNode> Nodes,
    ImmutableArray<Edge> Parentage,
    ImmutableArray<Edge> Partnerships,
    int Layers,
    double Breadth,
    int Lines,
    int Crossings)
{
    /// <summary>Returns the place of a member.</summary>
    public TreeNode Node(EntityId id) => Nodes.First(node => node.Entity.Id == id);
}

/// <summary>
/// Lays out a <see cref="FamilyView"/> as a tree: generations as layers, partners beside each other, and inside a
/// generation an order that lets as few lines cross as it finds. It is a layout of what the view holds and adds
/// nothing: no member, no relation and no order of birth.
/// </summary>
public static class FamilyTreeLayout
{
    // Sweeps over the generations while looking for an order with fewer crossings, and while moving members
    // towards their parents and children. More change little in families of the size a page shows.
    private const int Sweeps = 6;

    /// <summary>Returns the tree of a family.</summary>
    /// <param name="view">The family.</param>
    /// <param name="text">Returns a text in the language of the page; the longest name decides how many lines a box needs.</param>
    /// <param name="charactersPerLine">How many characters of a name fit on a line of a box.</param>
    public static FamilyTree Of(FamilyView view, Func<LocalizedText, string> text, int charactersPerLine = 11)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfLessThan(charactersPerLine, 1);

        if (view.Members.IsEmpty)
        {
            return new FamilyTree([], view.Parentage, view.Partnerships, 0, 0, 0, 0);
        }

        // Generations become layers without gaps, in the order of the view.
        var rows = view.Members.Select(member => member.Generation).Distinct().Order().ToList();
        var layerOf = view.Members.ToDictionary(member => member.Entity.Id, member => rows.IndexOf(member.Generation));
        var parents = view.Members.ToDictionary(member => member.Entity.Id, _ => new List<EntityId>());
        var children = view.Members.ToDictionary(member => member.Entity.Id, _ => new List<EntityId>());
        foreach (var edge in view.Parentage)
        {
            parents[edge.To.Id].Add(edge.From.Id);
            children[edge.From.Id].Add(edge.To.Id);
        }

        var layers = Enumerable.Range(0, rows.Count)
            .Select(layer => Units(view, layerOf, layer))
            .ToList();

        // The order of the collection is the first candidate; a sweep is kept only when fewer lines cross.
        var best = Snapshot(layers);
        var fewest = Crossings(view, layerOf, Index(layers));
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

            var crossings = Crossings(view, layerOf, Index(layers));
            if (crossings < fewest)
            {
                (best, fewest) = (Snapshot(layers), crossings);
            }
        }

        var places = Places(best, parents, children);
        return new FamilyTree(
            [
                .. best.SelectMany((units, layer) => units.SelectMany(unit => unit))
                    .Select(id => new TreeNode(view.Members.First(member => member.Entity.Id == id).Entity, layerOf[id], places[id])),
            ],
            view.Parentage,
            view.Partnerships,
            rows.Count,
            places.Values.Max() + 1,
            view.Members.Max(member => TextEstimate.Lines(text(member.Entity.Label), charactersPerLine)),
            fewest);
    }

    // The members of a generation as units that stay together: partners stand beside each other, somebody with
    // several partners between them. The order is that of the view.
    private static List<List<EntityId>> Units(FamilyView view, Dictionary<EntityId, int> layerOf, int layer)
    {
        var members = view.Members.Where(member => layerOf[member.Entity.Id] == layer).Select(member => member.Entity.Id).ToList();
        var partners = members.ToDictionary(id => id, _ => new List<EntityId>());
        foreach (var edge in view.Partnerships.Where(edge => layerOf[edge.From.Id] == layer && layerOf[edge.To.Id] == layer && edge.From.Id != edge.To.Id))
        {
            partners[edge.From.Id].Add(edge.To.Id);
            partners[edge.To.Id].Add(edge.From.Id);
        }

        var units = new List<List<EntityId>>();
        var placed = new HashSet<EntityId>();
        foreach (var member in members.Where(member => !placed.Contains(member)))
        {
            // Everybody joined to the member by partnerships, found from the member outwards.
            var group = new List<EntityId> { member };
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
                var hub = group.MaxBy(id => partners[id].Count);
                var others = group.Where(id => id != hub).ToList();
                group = [.. others.Where((_, index) => index % 2 == 0), hub, .. others.Where((_, index) => index % 2 == 1)];
            }

            units.Add(group);
        }

        return units;
    }

    private static List<List<List<EntityId>>> Snapshot(List<List<List<EntityId>>> layers) =>
        [.. layers.Select(units => units.Select(unit => unit.ToList()).ToList())];

    // The position of every member inside its generation, counted from 0.
    private static Dictionary<EntityId, int> Index(List<List<List<EntityId>>> layers)
    {
        var index = new Dictionary<EntityId, int>();
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
    private static List<List<EntityId>> Ordered(List<List<EntityId>> units, Dictionary<EntityId, List<EntityId>> neighbours, Dictionary<EntityId, int> index)
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

        double? Middle(EntityId id) => neighbours[id].Count > 0 ? neighbours[id].Average(neighbour => index[neighbour]) : null;
    }

    // Two lines from parents to children cross when they join the same two generations and their ends stand in
    // opposite orders.
    private static int Crossings(FamilyView view, Dictionary<EntityId, int> layerOf, Dictionary<EntityId, int> index)
    {
        var lines = view.Parentage
            .Select(edge => (Layers: (layerOf[edge.From.Id], layerOf[edge.To.Id]), From: index[edge.From.Id], To: index[edge.To.Id]))
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
    private static Dictionary<EntityId, double> Places(
        List<List<List<EntityId>>> layers,
        Dictionary<EntityId, List<EntityId>> parents,
        Dictionary<EntityId, List<EntityId>> children)
    {
        var places = Index(layers).ToDictionary(entry => entry.Key, entry => (double)entry.Value);
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
        return places.ToDictionary(entry => entry.Key, entry => Math.Round(entry.Value - first, 4));
    }

    // Moves the units of one generation as near to where their neighbours stand as possible without changing
    // their order or letting two boxes share a place: the least squares solution, found by merging units that
    // would overlap into blocks that move together.
    private static void Settle(List<List<EntityId>> units, Dictionary<EntityId, List<EntityId>> neighbours, Dictionary<EntityId, double> places)
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
