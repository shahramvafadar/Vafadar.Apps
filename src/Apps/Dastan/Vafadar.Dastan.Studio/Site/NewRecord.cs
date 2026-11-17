using System.Globalization;
using System.Text.Json.Nodes;
using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Studio.Site;

/// <summary>
/// A record that is not there yet, as the form of a new record starts with it (step 13.5): the fields a record
/// of its kind has, empty or with what is the usual value. It is no content: every text is to be filled in.
/// </summary>
public static class NewRecord
{
    /// <summary>The groups a new record of which starts as a form. An image or a video is brought in with its file.</summary>
    public static IReadOnlyList<string> Groups { get; } = ["sources", "entities", "relations", "events"];

    /// <summary>Returns the record a form for a new record of the group starts with, or nothing for a group without one.</summary>
    public static JsonObject? Of(string group, EditorialStore store, string collection, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(store);
        const string Pending = "source_backed_editorial_review_pending";
        var story = store.Records(collection, "entities")
            .Select(entity => store.Record(collection, "entities", entity.Id)!.Body)
            .FirstOrDefault(entity => (string?)entity["type"] == "NarrativeArc")?["id"]?.GetValue<string>();
        return group switch
        {
            "sources" => new JsonObject
            {
                ["id"] = string.Empty, ["title"] = string.Empty, ["url"] = string.Empty, ["kind"] = "scholarly_reference",
                ["accessedAt"] = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), ["passageLocator"] = string.Empty, ["spoilerLevel"] = 0,
            },
            "entities" => new JsonObject
            {
                ["id"] = string.Empty, ["type"] = "Person", ["label"] = string.Empty, ["summary"] = string.Empty,
                ["sourceIds"] = new JsonArray(), ["spoilerLevel"] = 0,
            },
            "relations" => new JsonObject
            {
                ["id"] = string.Empty, ["from"] = string.Empty, ["to"] = string.Empty, ["type"] = "memberOf", ["directionality"] = "directed",
                ["label"] = string.Empty, ["sourceIds"] = new JsonArray(), ["spoilerLevel"] = 0, ["passageLocator"] = string.Empty, ["reviewStatus"] = Pending,
                ["scope"] = story is null
                    ? new JsonObject { ["kind"] = "outside_the_story", ["eventIds"] = new JsonArray() }
                    : new JsonObject { ["kind"] = "outside_the_story", ["storyId"] = story, ["eventIds"] = new JsonArray() },
            },
            "events" => new JsonObject
            {
                ["id"] = string.Empty, ["label"] = string.Empty, ["summary"] = string.Empty,
                ["order"] = store.Records(collection, "events").Length + 1, ["orderBasis"] = "narrative",
                ["sourceIds"] = new JsonArray(), ["spoilerLevel"] = 0,
                ["temporalExpression"] = new JsonObject { ["kind"] = "narrative_position" }, ["reviewStatus"] = Pending,
                ["entityAssociations"] = new JsonArray(new JsonObject { ["entityId"] = string.Empty, ["role"] = string.Empty, ["presence"] = "acts_in_selected_scenes" }),
            },
            _ => null,
        };
    }
}
