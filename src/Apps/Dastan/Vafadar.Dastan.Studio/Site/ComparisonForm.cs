using System.Globalization;
using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Studio.Site;

/// <summary>
/// A comparison taken apart for forms (step 13.6b): what it says as a whole – its title, its frame, its sources
/// – and, one at a time, its lanes with their periods and events. Each part is filled in with the form of a
/// record; what was filled in is put back into the file of the comparison, which the readers then try as a whole.
/// </summary>
public static class ComparisonForm
{
    /// <summary>The group under which the fields of a comparison as a whole are named in a form.</summary>
    public const string Whole = "comparison";

    /// <summary>The group under which the fields of a lane are named in a form.</summary>
    public const string Lanes = "lanes";

    // What a comparison says as a whole, in the order of its file. Its lanes have pages of their own.
    private static readonly string[] Top = ["status", "note", "frame", "title", "summary", "show", "publication", "sources"];
    private static readonly string[] Lists = ["periods", "events"];

    /// <summary>Returns what the forms of a comparison need to know: its sources to point to, and the row a list begins with.</summary>
    public static FormContext Context(JsonObject comparison, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        JsonObject Statement() => new()
        {
            ["key"] = string.Empty, ["label"] = string.Empty, ["summary"] = string.Empty, ["time"] = null,
            ["sourceIds"] = new JsonArray(), ["reviewStatus"] = "proposed",
        };

        return new FormContext(
            [],
            new Dictionary<string, IReadOnlyList<FieldOption>>(StringComparer.Ordinal)
            {
                ["sources"] = [.. (comparison["sources"] as JsonArray ?? []).Select(source => new FieldOption((string)source!["id"]!, $"{(string?)source["title"]} ({(string?)source["id"]})"))],
            })
        {
            Shapes = new Dictionary<string, JsonObject>(StringComparer.Ordinal)
            {
                [$"{Whole}[].sources"] = new()
                {
                    ["id"] = string.Empty, ["title"] = string.Empty, ["url"] = string.Empty, ["kind"] = "scholarlyReference",
                    ["accessedAt"] = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                },
                [$"{Lanes}[].periods"] = Statement(),
                [$"{Lanes}[].events"] = Statement(),
            },
        };
    }

    /// <summary>Returns what the comparison says as a whole, as the record a form is made from.</summary>
    public static JsonObject WholeOf(JsonObject comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        var whole = new JsonObject(Top.Where(comparison.ContainsKey).Select(name => KeyValuePair.Create(name, comparison[name]?.DeepClone())));
        whole["sources"] ??= new JsonArray();
        return whole;
    }

    /// <summary>Returns the comparison with what it says as a whole as a form was filled in.</summary>
    public static JsonObject WithWhole(JsonObject comparison, JsonObject filled)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        ArgumentNullException.ThrowIfNull(filled);
        var changed = new JsonObject();
        foreach (var (name, value) in comparison)
        {
            if (!Top.Contains(name))
            {
                changed[name] = value?.DeepClone();
            }
            else if (filled[name] is { } now)
            {
                changed[name] = now.DeepClone();
            }
        }

        // A field the file did not have and the form filled in stands before the lanes.
        foreach (var name in Top.Where(name => !comparison.ContainsKey(name) && filled[name] is JsonValue or JsonObject or JsonArray { Count: > 0 }))
        {
            changed[name] = filled[name]!.DeepClone();
        }

        if (changed.ContainsKey("lanes"))
        {
            var lanes = changed["lanes"];
            changed.Remove("lanes");
            changed["lanes"] = lanes;
        }

        return changed;
    }

    /// <summary>Returns the lanes of a comparison by their key, with their name.</summary>
    public static IReadOnlyList<(string Key, string Label, int Periods, int Events)> LanesOf(JsonObject comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        return
        [
            .. (comparison["lanes"] as JsonArray ?? []).Select(lane => (
                (string)lane!["key"]!,
                (string?)lane["label"] ?? string.Empty,
                (lane["periods"] as JsonArray)?.Count ?? 0,
                (lane["events"] as JsonArray)?.Count ?? 0)),
        ];
    }

    /// <summary>Returns a lane as the record a form is made from – without its key, which names it for good – or nothing.</summary>
    public static JsonObject? Lane(JsonObject comparison, string key)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        var lane = (comparison["lanes"] as JsonArray ?? []).FirstOrDefault(lane => (string?)lane!["key"] == key)?.DeepClone().AsObject();
        if (lane is null)
        {
            return null;
        }

        lane.Remove("key");
        foreach (var list in Lists)
        {
            lane[list] ??= new JsonArray();
        }

        return lane;
    }

    /// <summary>Returns the record the form of a new lane starts with.</summary>
    public static JsonObject NewLane() => new() { ["label"] = string.Empty, ["periods"] = new JsonArray(), ["events"] = new JsonArray() };

    /// <summary>Returns the comparison with a lane as a form was filled in: in place of the lane of that key, or at the end.</summary>
    public static JsonObject WithLane(JsonObject comparison, string key, JsonObject filled)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        ArgumentNullException.ThrowIfNull(filled);
        var changed = comparison.DeepClone().AsObject();
        var lanes = changed["lanes"] as JsonArray;
        if (lanes is null)
        {
            changed["lanes"] = lanes = [];
        }

        var lane = new JsonObject { ["key"] = key };
        foreach (var (name, value) in filled)
        {
            lane[name] = value?.DeepClone();
        }

        // A list the form was given no row for is an empty list: the readers say whether a lane may be empty.
        foreach (var list in Lists)
        {
            lane[list] ??= new JsonArray();
        }

        var place = Enumerable.Range(0, lanes.Count).Where(index => (string?)lanes[index]!["key"] == key).Cast<int?>().FirstOrDefault();
        if (place is { } index)
        {
            lanes[index] = lane;
        }
        else
        {
            lanes.Add(lane);
        }

        return changed;
    }

    /// <summary>Returns the comparison without the lane of that key.</summary>
    public static JsonObject WithoutLane(JsonObject comparison, string key)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        var changed = comparison.DeepClone().AsObject();
        if (changed["lanes"] is JsonArray lanes && lanes.FirstOrDefault(lane => (string?)lane!["key"] == key) is { } gone)
        {
            lanes.Remove(gone);
        }

        return changed;
    }

    /// <summary>
    /// Returns the translations of a comparison without the texts of what the comparison no longer has: a lane, a
    /// period or an event that was taken out goes with the texts that translated it.
    /// </summary>
    public static Dictionary<string, string> Tidy(JsonObject comparison, IReadOnlyDictionary<string, string> translations)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        ArgumentNullException.ThrowIfNull(translations);
        var lanes = (comparison["lanes"] as JsonArray ?? []).Select(lane => lane!.AsObject()).ToList();
        var keys = lanes.Select(lane => (string?)lane["key"]).ToHashSet(StringComparer.Ordinal);
        var items = lanes.SelectMany(lane => Lists.SelectMany(list => lane[list] as JsonArray ?? [])).Select(item => (string?)item!["key"]).ToHashSet(StringComparer.Ordinal);
        var tidy = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (language, text) in translations)
        {
            var translation = JsonNode.Parse(text)!.AsObject();
            var changed = false;
            foreach (var (part, known) in new[] { ("lanes", keys), ("items", items) })
            {
                if (translation[part] is JsonObject named)
                {
                    foreach (var gone in named.Select(entry => entry.Key).Where(key => !known.Contains(key)).ToList())
                    {
                        named.Remove(gone);
                        changed = true;
                    }
                }
            }

            // A translation nothing was taken out of stays as it is written.
            tidy[language] = changed ? translation.ToJsonString() : text;
        }

        return tidy;
    }
}
