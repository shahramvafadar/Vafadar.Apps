using System.Text.Json;

namespace Vafadar.Zanance.Core.Sync;

/// <summary>Returns an explicit conflict or a complete field merge; it never averages or guesses financial values.</summary>
public sealed record SyncRowMergeResult(JsonElement? Values, IReadOnlyList<string> Conflicts);

/// <summary>Compares a proposal with authentic historical/current versions, independently of clocks or transport retries.</summary>
public static class SyncRowMerge
{
    /// <summary>Merges disjoint edits; competing edits/deletes and restoration without the exact known deletion remain conflicts.</summary>
    public static SyncRowMergeResult Merge(JsonElement? baseline, JsonElement? current, JsonElement? proposed,
        bool currentIsTombstone = false, bool knownCurrentDeletion = false)
    {
        if (baseline is { } beforeRow) RequireObject(beforeRow);
        if (current is { } currentRow) RequireObject(currentRow);
        if (proposed is { } proposedRow) RequireObject(proposedRow);
        // An explicit local Undo against the exact authentic current deletion is different from
        // resurrecting a missing/stale historical record. Callers must prove that deletion version.
        if (currentIsTombstone && proposed is not null && !knownCurrentDeletion)
            return new(null, ["$deleted"]);
        if (Equal(current, proposed)) return new(Clone(current), []);
        if (Equal(baseline, current)) return new(Clone(proposed), []);
        if (Equal(baseline, proposed)) return new(Clone(current), []);
        if (baseline is null || current is null || proposed is null)
            return new(null, ["$record"]);
        var before = baseline.Value.EnumerateObject().ToDictionary(value => value.Name, value => value.Value, StringComparer.Ordinal);
        var remote = current.Value.EnumerateObject().ToDictionary(value => value.Name, value => value.Value, StringComparer.Ordinal);
        var local = proposed.Value.EnumerateObject().ToDictionary(value => value.Name, value => value.Value, StringComparer.Ordinal);
        // Schema negotiation requires complete records; missing fields cannot mean an implicit deletion/default.
        if (!before.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(remote.Keys) ||
            !before.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(local.Keys))
            return new(null, ["$schema"]);
        var result = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
        var conflicts = new List<string>();
        foreach (var name in before.Keys.Order(StringComparer.Ordinal))
        {
            if (JsonElement.DeepEquals(local[name], remote[name]) || JsonElement.DeepEquals(local[name], before[name]))
                result.Add(name, remote[name]);
            else if (JsonElement.DeepEquals(remote[name], before[name])) result.Add(name, local[name]);
            else conflicts.Add(name);
        }
        if (conflicts.Count != 0) return new(null, conflicts);
        // JsonDocument owns an independent completed value rather than a disposed request buffer.
        using var bytes = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bytes))
        {
            writer.WriteStartObject();
            foreach (var (name, value) in result) { writer.WritePropertyName(name); value.WriteTo(writer); }
            writer.WriteEndObject();
        }
        using var document = JsonDocument.Parse(bytes.ToArray());
        return new(document.RootElement.Clone(), []);
    }

    private static bool Equal(JsonElement? left, JsonElement? right) => left is null ? right is null
        : right is not null && JsonElement.DeepEquals(left.Value, right.Value);

    private static JsonElement? Clone(JsonElement? value) => value?.Clone();

    private static void RequireObject(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new ArgumentException("A complete sync row must be an object.");
    }
}
