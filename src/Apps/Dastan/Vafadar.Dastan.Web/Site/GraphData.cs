using System.Text.Json.Serialization;
using Microsoft.Extensions.Localization;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.People;
using Vafadar.Dastan.Web.Resources;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// A graph as the layer in the browser gets it (step 19.8, ADR 0020): the contract of <c>DastanGraph</c>.
/// Nodes with the card of their profile, edges with an identity of their own, a label, a direction and where
/// their sources are. Nothing of it names the control that draws it, and nothing is in it that the page of
/// the same visitor would not show.
/// </summary>
/// <param name="Kind">What is drawn: <c>family</c> – generations in rows – or <c>connections</c>.</param>
/// <param name="Focus">The node the graph is about; nothing for a graph about no one node.</param>
/// <param name="Nodes">The nodes, in the order of the page.</param>
/// <param name="Edges">The edges, in the order of the page.</param>
/// <param name="More">Where the page offers to open further, as far as a graph says so.</param>
public sealed record GraphData(string Kind, string? Focus, IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> Edges, GraphMore? More = null);

/// <summary>A node of a graph: who or what it is, in the words of its card.</summary>
/// <param name="Id">The identifier of the node in this graph.</param>
/// <param name="Name">The name.</param>
/// <param name="Address">The address of the page of the node.</param>
/// <param name="Profile">The address of the profile of the node, for the panel.</param>
public sealed record GraphNode(string Id, string Name, string Address, string Profile)
{
    /// <summary>Gets the language of the name where it is not the language of the page.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NameLanguage { get; init; }

    /// <summary>Gets what the node is, in the words of the page.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Kind { get; init; }

    /// <summary>Gets the picture of the card, when this visitor may see one.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProfilePictureData? Picture { get; init; }

    /// <summary>Gets the lines of the card: up to three facts that are stated, each as the page writes it.</summary>
    public IReadOnlyList<string> Lines { get; init; } = [];

    /// <summary>Gets the row of the node in a graph of generations, counted from the top.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Row { get; init; }

    /// <summary>
    /// Gets where the node stands in its row, counted in cards from the beginning of the row. The rows and the
    /// places of a family are decided by the layout of Dastan – the same that draws the still picture of the
    /// page – and not by the control that draws the graph (ADR 0020).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Place { get; init; }

    /// <summary>Gets a value indicating whether the page offers to open the relations of the node.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Expandable { get; init; }
}

/// <summary>An edge of a graph: one relationship, kept apart from every other between the same two nodes.</summary>
/// <param name="Id">The identifier of the edge in this graph.</param>
/// <param name="From">The node it begins at.</param>
/// <param name="To">The node it ends at.</param>
/// <param name="Role">What it is for the drawing: <c>parent</c>, <c>adoptive-parent</c>, <c>guardian</c>, <c>partner</c>, or <c>relation</c>.</param>
/// <param name="Label">The words of the relationship, as the page writes them.</param>
/// <param name="Directed">Whether it reads from the first node to the second only.</param>
public sealed record GraphEdge(string Id, string From, string To, string Role, string Label, bool Directed)
{
    /// <summary>Gets the language of the label where it is not the language of the page.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LabelLanguage { get; init; }

    /// <summary>Gets the time the relationship holds for, as the page writes it.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? When { get; init; }

    /// <summary>Gets the address of what supports the relationship, where the page links to it.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Source { get; init; }

    /// <summary>Gets a value indicating whether the editors mark the relationship as disputed.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Disputed { get; init; }
}

/// <summary>Where a graph goes on beyond what was asked for.</summary>
/// <param name="Above">Whether there are ancestors the visitor may see that are not in the graph.</param>
/// <param name="Below">Whether there are descendants.</param>
/// <param name="Beside">Whether there are relatives beside the line.</param>
public sealed record GraphMore(bool Above, bool Below, bool Beside);

/// <summary>
/// The addresses that give the browser a graph (step 19.8, ADR 0020): the family of an entity and its
/// connections – each at the address of its page and <c>.json</c>, asked with what the page is asked with –
/// and the tree of an account, private and public. Each is answered by the views the page is drawn from, so
/// a graph never holds a node or an edge the page of the same visitor does not show.
/// </summary>
public static class GraphDataEndpoints
{
    /// <summary>Returns the family of an entity as a graph.</summary>
    public static GraphData Of(FamilyView family, EntityId around, Visitor visitor, DastanSite site, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(site);
        var placed = FamilyTreeLayout.Of(family, words => visitor.Text(words).Value).Nodes.ToDictionary(node => node.Entity.Id);
        return new(
            "family",
            around.Value,
            [.. family.Members.Select(member => Node(member.Entity, visitor, site, text) with { Row = placed[member.Entity.Id].Layer, Place = placed[member.Entity.Id].Place })],
            [
                .. family.Parentage.Select(edge => Edge(edge, "parent", directed: true, visitor, site, text)),
                .. family.Partnerships.Select(edge => Edge(edge, "partner", directed: false, visitor, site, text)),
            ],
            new(family.MoreAbove, family.MoreBelow, family.MoreBeside));
    }

    /// <summary>Returns the connections of an entity as a graph.</summary>
    public static GraphData Of(ConnectionsView view, Visitor visitor, DastanSite site, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(site);
        var expandable = view.Expandable.Select(entity => entity.Id).ToHashSet();
        return new(
            "connections",
            view.Focus.Id.Value,
            [.. view.Nodes.Select(entity => Node(entity, visitor, site, text) with { Expandable = expandable.Contains(entity.Id) })],
            [.. view.Edges.Select(edge => Edge(edge, "relation", directed: true, visitor, site, text))]);
    }

    /// <summary>Returns a family tree of visitors as a graph.</summary>
    /// <param name="tree">The tree, as who looks may see it.</param>
    /// <param name="address">The address of the tree, below which the pages of its people stand.</param>
    /// <param name="visitor">Who looks.</param>
    /// <param name="text">The words of the pages.</param>
    public static GraphData Of(TreeView tree, string address, Visitor visitor, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);
        var placed = (TreeEndpoints.Placement(tree)?.Nodes ?? []).ToDictionary(node => node.Id, StringComparer.Ordinal);
        return new(
            "family",
            null,
            [
                .. tree.People.Select(person =>
                {
                    var years = TreeEndpoints.Years(person, text, visitor);
                    return new GraphNode(person.Id, person.Name, $"{address}/person/{person.Id}", $"{address}/person/{person.Id}{ProfileDataEndpoints.Ending}")
                    {
                        Picture = person.Picture is { } picture
                            ? new($"{PhotoEndpoints.File}/{picture.Id}", null, picture.Caption.Length > 0 ? picture.Caption : text["Photos_Alt"])
                            : null,
                        Lines = years.Length > 0 ? [years] : [],
                        Row = placed.TryGetValue(person.Id, out var where) ? where.Layer : null,
                        Place = placed.TryGetValue(person.Id, out var along) ? along.Place : null,
                    };
                }),
            ],
            [
                .. tree.Links.Select(link =>
                {
                    var held = PeopleEndpoints.Held(new RelationshipTerms(Since: link.Since, Until: link.Until), text, visitor);
                    return new GraphEdge(
                        link.Number.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        link.From,
                        link.To,
                        link.Kind,
                        text[$"Tree_Link_{link.Kind}"],
                        PeopleStore.IsParentage(link.Kind))
                    {
                        When = held.Length > 0 ? held : null,
                    };
                }),
            ]);
    }

    /// <summary>Maps the addresses.</summary>
    public static void MapGraphData(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/{collection}/entity/{id}/family.json", (HttpContext context, Visitor visitor, DastanSite site, IStringLocalizer<Ui> text, string collection, string id, string? up, string? down, string? beside, string? family) =>
        {
            var known = collection == site.Collection && id.Length > 0 && !id.Any(char.IsWhiteSpace);
            var state = TreeState.Read(up, down, beside, family, null);
            var around = new EntityId(known ? id : "-");
            var view = !known || visitor.Projection is not { } projection ? null
                : state.Whole ? projection.Family(around) : projection.Family(around, state.Up, state.Down, state.Beside);
            return Answer(context, view is null ? null : Of(view, around, visitor, site, text));
        });

        endpoints.MapGet("/{collection}/entity/{id}/connections.json", (HttpContext context, Visitor visitor, DastanSite site, IStringLocalizer<Ui> text, string collection, string id, string? open) =>
        {
            static bool IsIdentifier(string written) => written.Length > 0 && !written.Any(char.IsWhiteSpace);
            var known = collection == site.Collection && IsIdentifier(id);
            var opened = (open ?? string.Empty).Split(',', StringSplitOptions.TrimEntries).Where(IsIdentifier).Select(one => new EntityId(one)).Distinct();
            var view = known ? visitor.Projection?.Connections(new EntityId(id), opened) : null;
            return Answer(context, view is null ? null : Of(view, visitor, site, text));
        });

        endpoints.MapGet(TreeEndpoints.Page + "/{tree}/graph.json", (HttpContext context, AccountDesk desk, Visitor visitor, IStringLocalizer<Ui> text, string tree) =>
        {
            TreeView? seen = null;
            if (desk.IsOpen && tree.Length <= 40 && AccountDesk.Session(context.User) is { } session)
            {
                using var store = desk.Open();
                seen = store.Tree(session.Id, tree);
            }

            return Answer(context, seen is null ? null : Of(seen, $"account/trees/{seen.Tree.Id}", visitor, text));
        });

        endpoints.MapGet(TreeEndpoints.Public + "/{tree}/graph.json", (HttpContext context, AccountDesk desk, Visitor visitor, IStringLocalizer<Ui> text, string tree) =>
        {
            TreeView? seen = null;
            if (desk.IsOpen && tree.Length <= 40)
            {
                using var store = desk.Open();
                seen = store.PublicTree(tree);
            }

            return Answer(context, seen is null ? null : Of(seen, $"trees/{seen.Tree.Id}", visitor, text));
        });
    }

    // Like the profile of a node: the answer depends on who asks, so nothing keeps it; and what is not there
    // is the status and no page.
    private static IResult Answer(HttpContext context, GraphData? graph)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Vary = "Cookie";
        context.Response.Headers["X-Robots-Tag"] = "noindex";
        if (graph is not null)
        {
            return Results.Json(graph);
        }

        if (context.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodePagesFeature>() is { } pages)
        {
            pages.Enabled = false;
        }

        return Results.NotFound();
    }

    // The card of an entity: its name, its kind, its picture and the first stated facts – from its profile.
    private static GraphNode Node(Entity entity, Visitor visitor, DastanSite site, IStringLocalizer<Ui> text)
    {
        var profile = ProfileDataEndpoints.Of(visitor.Projection!.Profile(entity.Id)!, visitor, site, text);
        var card = visitor.Projection.Profile(entity.Id)!.Card.Select(line => text[$"Fact_{line.Kind.Key}"].Value).ToHashSet(StringComparer.Ordinal);
        return new(entity.Id.Value, profile.Name, profile.Address, profile.Address + ProfileDataEndpoints.Ending)
        {
            NameLanguage = profile.NameLanguage,
            Kind = profile.Kind,
            Picture = profile.Picture,
            Lines = [.. profile.Facts.Where(fact => card.Contains(fact.Label)).Take(3).Select(fact => $"{fact.Label}: {string.Join(text["Tree_And"], fact.Values.Select(value => value.Text))}")],
        };
    }

    private static GraphEdge Edge(Edge edge, string role, bool directed, Visitor visitor, DastanSite site, IStringLocalizer<Ui> text)
    {
        var label = visitor.Text(edge.Relation.Label);
        return new(edge.Relation.Id.Value, edge.From.Id.Value, edge.To.Id.Value, role, label.Value, directed)
        {
            LabelLanguage = label.Language == visitor.Language.Tag ? null : label.Language,
            When = edge.Relation.Time is { } time ? TimeText.Of(time, visitor, text) : null,
            Source = EvidenceAddress.Of(site.Collection, edge.Relation.Id),
            Disputed = edge.Relation.Review == ReviewStatus.Disputed,
        };
    }
}
