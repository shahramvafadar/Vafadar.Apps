using System.Text.Json.Nodes;

namespace Vafadar.Dastan.Store;

/// <summary>
/// Two entities that turn out to be one (step 13.6, DOM-003). Merging is an act of an editor with a reason,
/// never something the store concludes from a likeness of names: one entity is kept, the other goes into it,
/// and every record that named the other names the kept one from then on. Every record the merge touches gets
/// its change in the history – with the record as it was before, so that a mistaken merge can be read back and
/// corrected.
/// </summary>
public sealed partial class EditorialStore
{
    /// <summary>Merges one entity of a collection into another.</summary>
    /// <param name="collectionId">The collection.</param>
    /// <param name="keepId">The entity that stays.</param>
    /// <param name="mergedId">The entity that goes into it and is then no record any more.</param>
    /// <param name="by">Who merges.</param>
    /// <param name="reason">Why the two are one.</param>
    public ActResult MergeEntities(string collectionId, string keepId, string mergedId, string by, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keepId);
        ArgumentException.ThrowIfNullOrWhiteSpace(mergedId);
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (keepId == mergedId)
        {
            return Refused("an entity is not merged into itself.");
        }

        return Act(collectionId, by, $"{reason.Trim()} ({mergedId} merged into {keepId})", (root, translations) =>
        {
            var entities = root["entities"]!.AsArray();
            JsonObject? Find(string id) => entities.Select(entity => entity!.AsObject()).FirstOrDefault(entity => (string?)entity["id"] == id);
            if (Find(keepId) is not { } keep)
            {
                return $"{keepId}: the collection has no entity of that name.";
            }

            if (Find(mergedId) is not { } merged)
            {
                return $"{mergedId}: the collection has no entity of that name.";
            }

            // What the merged entity was called stays findable: its name becomes another name of the kept one.
            var names = keep["names"] as JsonArray ?? [];
            void Named(string? name, string language)
            {
                if (!string.IsNullOrWhiteSpace(name)
                    && name != (string?)keep["label"]
                    && !(language == "fa" && name == (string?)keep["labelFa"])
                    && !names.Any(other => (string?)other!["name"] == name && (string?)other["language"] == language))
                {
                    names.Add(new JsonObject { ["name"] = name, ["language"] = language, ["kind"] = "alternative" });
                }
            }

            // The profile (ADR 0017): the kept entity keeps its picture and its facts, and takes from the merged
            // one what it does not state itself – with the words of a fact in every language.
            if (keep["picture"] is null && merged["picture"] is { } picture)
            {
                keep["picture"] = picture.DeepClone();
            }

            var facts = keep["facts"] as JsonArray ?? [];
            var taken = new List<string>();
            foreach (var fact in (merged["facts"] as JsonArray ?? []).Select(fact => fact!.AsObject()))
            {
                var kind = (string?)fact["fact"];
                var other = (string?)fact["entityId"];
                if (other == keepId || facts.Any(held => (string?)held!["fact"] == kind && (other is null || (string?)held["entityId"] == other)))
                {
                    continue;
                }

                facts.Add(fact.DeepClone());
                if (fact["text"] is not null && kind is not null)
                {
                    taken.Add(kind);
                }
            }

            if (facts.Count > 0 && facts.Parent is null)
            {
                keep["facts"] = facts;
            }

            foreach (var translation in translations.Values)
            {
                if ((translation["entities"] as JsonObject)?[mergedId]?["facts"] is not JsonObject words)
                {
                    continue;
                }

                foreach (var kind in taken.Where(kind => words[kind] is not null))
                {
                    var entitiesOf = translation["entities"]!.AsObject();
                    var kept = entitiesOf[keepId] as JsonObject ?? (JsonObject)(entitiesOf[keepId] = new JsonObject())!;
                    var keptWords = kept["facts"] as JsonObject ?? (JsonObject)(kept["facts"] = new JsonObject())!;
                    keptWords[kind] ??= words[kind]!.DeepClone();
                }
            }

            Named((string?)merged["label"], Language(root));
            Named((string?)merged["labelFa"], "fa");
            foreach (var (language, translation) in translations)
            {
                var translated = (string?)(translation["entities"] as JsonObject)?[mergedId]?["label"];
                if (translated != (string?)(translation["entities"] as JsonObject)?[keepId]?["label"])
                {
                    Named(translated, language);
                }

                (translation["entities"] as JsonObject)?.Remove(mergedId);
            }

            foreach (var other in merged["names"] as JsonArray ?? [])
            {
                if (!names.Any(name => (string?)name!["name"] == (string?)other!["name"] && (string?)name["language"] == (string?)other["language"]))
                {
                    names.Add(other!.DeepClone());
                }
            }

            if (names.Count > 0 && names.Parent is null)
            {
                keep["names"] = names;
            }

            // What was stated about the merged entity is stated about the kept one.
            Moved(keep, merged, "dated", static (_, _) => false);
            Moved(keep, merged, "identifiers", static (one, other) => (string?)one["issuer"] == (string?)other["issuer"] && (string?)one["value"] == (string?)other["value"]);
            var sources = keep["sourceIds"]!.AsArray();
            foreach (var source in (merged["sourceIds"] as JsonArray ?? []).Select(source => (string)source!).Where(source => !sources.Any(held => (string)held! == source)).ToList())
            {
                sources.Add(source);
            }

            entities.Remove(merged);

            // A fact of any profile that named the merged entity names the kept one.
            foreach (var entity in entities.Select(entity => entity!.AsObject()))
            {
                foreach (var fact in (entity["facts"] as JsonArray ?? []).Select(fact => fact!.AsObject()).Where(fact => (string?)fact["entityId"] == mergedId))
                {
                    if ((string?)entity["id"] == keepId)
                    {
                        return $"{keepId}, the fact {(string?)fact["fact"]}: after the merge the fact would name {keepId} itself; change the fact or take it out first.";
                    }

                    fact["entityId"] = keepId;
                }
            }

            // Every record that named the merged entity names the kept one.
            foreach (var relation in root["relations"]!.AsArray().Select(relation => relation!.AsObject()))
            {
                foreach (var end in new[] { "from", "to" })
                {
                    if ((string?)relation[end] == mergedId)
                    {
                        relation[end] = keepId;
                    }
                }

                if ((string?)relation["from"] == (string?)relation["to"])
                {
                    return $"{(string?)relation["id"]}: after the merge the relation would connect {keepId} with itself; change the relation or take it out first.";
                }

                if (relation["scope"] is JsonObject scope && (string?)scope["storyId"] == mergedId)
                {
                    scope["storyId"] = keepId;
                }
            }

            foreach (var storyEvent in root["events"]!.AsArray().Select(storyEvent => storyEvent!.AsObject()))
            {
                var parts = storyEvent["entityAssociations"] as JsonArray ?? [];
                if (parts.Any(part => (string?)part!["entityId"] == mergedId))
                {
                    if (parts.Any(part => (string?)part!["entityId"] == keepId))
                    {
                        return $"{(string?)storyEvent["id"]}: both entities take part in this step; say first which part the one entity has.";
                    }

                    foreach (var part in parts.Where(part => (string?)part!["entityId"] == mergedId))
                    {
                        part!["entityId"] = keepId;
                    }

                    // The roles of the step in the other languages follow who they are about.
                    foreach (var translation in translations.Values)
                    {
                        if ((translation["events"] as JsonObject)?[(string)storyEvent["id"]!]?["roles"] is JsonObject roles && roles[mergedId] is { } role)
                        {
                            roles.Remove(mergedId);
                            roles[keepId] = role;
                        }
                    }
                }

                if ((string?)storyEvent["storyId"] == mergedId)
                {
                    storyEvent["storyId"] = keepId;
                }
            }

            foreach (var item in root["media"]!.AsArray().Select(item => item!.AsObject()))
            {
                if (item["depictedEntityIds"] is JsonArray depicted && depicted.Any(id => (string?)id == mergedId))
                {
                    item["depictedEntityIds"] = new JsonArray([.. depicted.Select(id => (string)id! == mergedId ? keepId : (string)id!).Distinct(StringComparer.Ordinal).Select(id => (JsonNode?)JsonValue.Create(id))]);
                }
            }

            // A thread that stops at the merged entity stops at the kept one; a thread that stopped at both
            // stops there once, at the first of the two places.
            foreach (var thread in root["threads"]!.AsArray().Select(thread => thread!.AsObject()))
            {
                if (thread["stops"] is not JsonArray stops || !stops.Any(stop => (string?)stop?["entityId"] == mergedId))
                {
                    continue;
                }

                var kept = new JsonArray();
                var once = false;
                foreach (var stop in stops.Select(stop => stop!.AsObject()))
                {
                    var here = (string?)stop["entityId"];
                    if (here == mergedId || here == keepId)
                    {
                        if (once)
                        {
                            continue;
                        }

                        once = true;
                        var copy = (JsonObject)stop.DeepClone();
                        copy["entityId"] = keepId;
                        kept.Add(copy);
                    }
                    else
                    {
                        kept.Add(stop.DeepClone());
                    }
                }

                thread["stops"] = kept;
                foreach (var translation in translations.Values)
                {
                    if ((translation["threads"] as JsonObject)?[(string)thread["id"]!]?["notes"] is JsonObject notes && notes[mergedId] is { } note)
                    {
                        notes.Remove(mergedId);
                        notes[keepId] ??= note.DeepClone();
                    }
                }
            }

            if ((string?)root["subjectId"] == mergedId)
            {
                root["subjectId"] = keepId;
            }

            if (root["editionContext"] is JsonObject edition && (string?)edition["workId"] == mergedId)
            {
                edition["workId"] = keepId;
            }

            return null;
        });
    }

    // Moves the parts of a list of one entity to the same list of another, except those the other has already.
    private static void Moved(JsonObject keep, JsonObject merged, string list, Func<JsonObject, JsonObject, bool> same)
    {
        if (merged[list] is not JsonArray { Count: > 0 } parts)
        {
            return;
        }

        var held = keep[list] as JsonArray;
        if (held is null)
        {
            keep[list] = held = [];
        }

        foreach (var part in parts.Select(part => part!.AsObject()).ToList())
        {
            if (!held.Any(other => same(other!.AsObject(), part)))
            {
                held.Add(part.DeepClone());
            }
        }
    }

    // The language the file of a collection is written in, as a language tag.
    private static string Language(JsonObject root) => ((string?)root["editionContext"]?["summaryLanguage"] ?? (string?)root["language"]) switch
    {
        null or "English" => "en",
        "Persian" => "fa",
        var tag => tag,
    };
}
