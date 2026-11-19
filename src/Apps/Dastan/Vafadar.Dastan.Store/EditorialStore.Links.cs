using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.Store;

/// <summary>
/// One record of a collection naming another: the end of a relation, a source a statement rests on, an entity
/// an event is about, what a media item shows, a stop of a thread. The store derives these rows from the
/// records whenever it keeps a collection (ADR 0015, D-081); the database refuses a row that names a record
/// the collection does not have, and a record cannot go while another still names it.
/// </summary>
public sealed class RecordLink
{
    /// <summary>Gets or sets the collection.</summary>
    public required string CollectionId { get; set; }

    /// <summary>Gets or sets the group of the record that names the other.</summary>
    public required string FromGroup { get; set; }

    /// <summary>Gets or sets the identifier of the record that names the other.</summary>
    public required string FromId { get; set; }

    /// <summary>Gets or sets what the other record is to this one, such as <c>from</c>, <c>to</c> or <c>source</c>.</summary>
    public required string Role { get; set; }

    /// <summary>Gets or sets the place of the link among the links of its role, in the order the record writes them.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the group of the record that is named.</summary>
    public required string ToGroup { get; set; }

    /// <summary>Gets or sets the identifier of the record that is named.</summary>
    public required string ToId { get; set; }
}

/// <summary>A record that names another, as the store answers who uses a record.</summary>
/// <param name="FromGroup">The group of the record that names.</param>
/// <param name="FromId">Its identifier.</param>
/// <param name="Role">What the named record is to it.</param>
/// <param name="ToGroup">The group of the record that is named.</param>
/// <param name="ToId">Its identifier.</param>
public sealed record RecordLinkInfo(string FromGroup, string FromId, string Role, string ToGroup, string ToId);

// The links between the records of a collection, as rows with foreign keys beside the records themselves.
public sealed partial class EditorialStore
{
    /// <summary>Returns the records of a collection that name a record, in the order of their groups and identifiers.</summary>
    public ImmutableArray<RecordLinkInfo> LinksTo(string collectionId, string group, string recordId) =>
    [
        .. _context.Links.AsNoTracking()
            .Where(link => link.CollectionId == collectionId && link.ToGroup == group && link.ToId == recordId)
            .OrderBy(link => link.FromGroup).ThenBy(link => link.FromId).ThenBy(link => link.Role).ThenBy(link => link.Ordinal)
            .Select(link => new RecordLinkInfo(link.FromGroup, link.FromId, link.Role, link.ToGroup, link.ToId)),
    ];

    /// <summary>Returns the records a record of a collection names, in the order the record writes them.</summary>
    public ImmutableArray<RecordLinkInfo> LinksFrom(string collectionId, string group, string recordId) =>
    [
        .. _context.Links.AsNoTracking()
            .Where(link => link.CollectionId == collectionId && link.FromGroup == group && link.FromId == recordId)
            .OrderBy(link => link.Role).ThenBy(link => link.Ordinal)
            .Select(link => new RecordLinkInfo(link.FromGroup, link.FromId, link.Role, link.ToGroup, link.ToId)),
    ];

    /// <summary>
    /// Says which records a record names, as the file of a collection writes it: the role, the place among the
    /// links of that role, and the group and identifier of the record that is named. A field that is not there,
    /// or is not an identifier, names nothing.
    /// </summary>
    internal static List<(string Role, int Ordinal, string ToGroup, string ToId)> LinksOf(string group, JsonObject record)
    {
        var links = new List<(string, int, string, string)>();

        void One(string role, JsonNode? value, string toGroup)
        {
            if (value is JsonValue written && written.TryGetValue<string>(out var id) && id.Length > 0)
            {
                links.Add((role, links.Count(link => link.Item1 == role), toGroup, id));
            }
        }

        void Many(string role, JsonNode? values, string toGroup)
        {
            foreach (var value in values as JsonArray ?? [])
            {
                One(role, value, toGroup);
            }
        }

        // Whatever a record is, it may say what it rests on.
        Many("source", record["sourceIds"], "sources");
        switch (group)
        {
            case "relations":
                One("from", record["from"], "entities");
                One("to", record["to"], "entities");
                if (record["scope"] is JsonObject scope)
                {
                    One("scope-story", scope["storyId"], "entities");
                    Many("scope-event", scope["eventIds"], "events");
                }

                break;
            case "events":
                One("story", record["storyId"], "entities");
                foreach (var association in record["entityAssociations"] as JsonArray ?? [])
                {
                    One("associated", (association as JsonObject)?["entityId"], "entities");
                }

                break;
            case "media":
                Many("depicted-entity", record["depictedEntityIds"], "entities");
                Many("depicted-event", record["depictedEventIds"], "events");
                Many("related-relation", record["relatedRelationIds"], "relations");
                break;
            case "threads":
                foreach (var stop in record["stops"] as JsonArray ?? [])
                {
                    One("stop-entity", (stop as JsonObject)?["entityId"], "entities");
                    One("stop-event", (stop as JsonObject)?["eventId"], "events");
                    One("stop-media", (stop as JsonObject)?["mediaId"], "media");
                }

                break;
        }

        return links;
    }

    // Brings the rows of the links to what the records of a collection say now. Called by Keep, in its step,
    // after the records themselves were brought to what the collection says.
    private void KeepLinks(string id, JsonObject root)
    {
        var present = new HashSet<(string, string)>();
        foreach (var group in Groups)
        {
            foreach (var record in root[group] as JsonArray ?? [])
            {
                present.Add((group, (string)record!["id"]!));
            }
        }

        var held = _context.Links.Where(link => link.CollectionId == id).ToDictionary(link => (link.FromGroup, link.FromId, link.Role, link.Ordinal));
        foreach (var group in Groups)
        {
            foreach (var record in root[group] as JsonArray ?? [])
            {
                var recordId = (string)record!["id"]!;
                foreach (var (role, ordinal, toGroup, toId) in LinksOf(group, record.AsObject()))
                {
                    if (!present.Contains((toGroup, toId)))
                    {
                        // The readers accepted the collection, so this is a field they do not read as a link.
                        throw new InvalidOperationException($"{group}/{recordId} names {toGroup}/{toId} as its {role}, and the collection has no such record.");
                    }

                    if (held.Remove((group, recordId, role, ordinal), out var link))
                    {
                        (link.ToGroup, link.ToId) = (toGroup, toId);
                    }
                    else
                    {
                        _context.Links.Add(new RecordLink { CollectionId = id, FromGroup = group, FromId = recordId, Role = role, Ordinal = ordinal, ToGroup = toGroup, ToId = toId });
                    }
                }
            }
        }

        _context.Links.RemoveRange(held.Values);
    }
}
