using System.Text.Json.Nodes;

namespace Vafadar.Zanance.App;

/// <summary>D-574: actual native facts captured after the original loaded Home is laid out and drawn.</summary>
internal sealed record LoadedColdStartFacts(int ExpectedPid, int ActualPid, int ExpectedUid, int ActualUid,
    bool ProcessWasAbsent, bool NativeColdActivity, bool NativeStartupComplete,
    long LaunchNanoseconds, long InitialFrameNanoseconds, long ReadyDrawNanoseconds, long ReadyFrameNanoseconds,
    bool OriginalHomeUncovered, bool OriginalPublicationCompleted, bool EnabledOriginalAdd,
    string BeforeBoot, string AfterBoot, bool ExactOwnedProfile, bool OriginalSourceMatched);

/// <summary>Separates the real loaded cold boundary from a fast first covered frame or navigation-only publication.</summary>
internal static class LoadedColdStartPolicy
{
    /// <summary>Admits only the original startup's one-time null-to-device display-unit copy and its timestamp.</summary>
    internal static bool IsOriginalDisplayUnitStartup(IReadOnlyList<string> columns, string before, string after,
        string legacyUnits, DateTimeOffset now) => IsOriginalDisplayUnitStartup(columns, before, after, legacyUnits, now, out _);

    /// <summary>Returns only a fixed refusal category, without leaking settings or row payloads.</summary>
    internal static bool IsOriginalDisplayUnitStartup(IReadOnlyList<string> columns, string before, string after,
        string legacyUnits, DateTimeOffset now, out string admission)
    {
        admission = "duplicate-columns";
        if (columns.Count != columns.Distinct(StringComparer.Ordinal).Count()) return false;
        var units = columns.ToList().IndexOf("DisplayUnits"); var updated = columns.ToList().IndexOf("UpdatedAt");
        admission = "missing-original-columns";
        if (units < 0 || updated < 0 || units == updated) return false;
        var original = JsonNode.Parse(before) as JsonArray; var current = JsonNode.Parse(after) as JsonArray;
        admission = "row-width-or-kind";
        if (original is null || current is null || original.Count != columns.Count || current.Count != columns.Count) return false;
        admission = "source-unit-already-set";
        if (original[units] is not null) return false;
        admission = "target-not-serialized-units";
        if (current[units] is not JsonValue unit || !unit.TryGetValue<string>(out var serialized)) return false;
        admission = "target-differs-from-original-device-legacy";
        if (serialized != legacyUnits) return false;
        // LocalDbContext's original converter stores DateTimeOffset as integer UTC ticks,
        // including zero for this retained workload. A formatted date string is not that source.
        static bool Timestamp(JsonNode? value, out long ticks)
        {
            ticks = 0;
            return value is JsonValue number && number.TryGetValue(out ticks)
                && ticks >= 0 && ticks <= DateTimeOffset.MaxValue.UtcTicks;
        }
        admission = "source-save-time-malformed";
        if (!Timestamp(original[updated], out var previous)) return false;
        admission = "target-save-time-malformed";
        if (!Timestamp(current[updated], out var saved)) return false;
        admission = "target-time-before-source";
        if (saved < previous) return false;
        admission = "target-time-after-current-device-clock";
        if (saved > now.UtcTicks) return false;
        original[units] = current[units]?.DeepClone(); original[updated] = current[updated]?.DeepClone();
        admission = JsonNode.DeepEquals(original, current) ? "original-startup-migration" : "other-original-column-changed";
        return admission == "original-startup-migration";
    }

    /// <summary>Admits only current-process, original-profile, complete native-source and monotonic draw evidence.</summary>
    internal static double RequireLoadedColdMilliseconds(LoadedColdStartFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.ExpectedPid <= 0 || facts.ActualPid != facts.ExpectedPid
            || facts.ExpectedUid != 10381 || facts.ActualUid != facts.ExpectedUid
            || !facts.ProcessWasAbsent || !facts.NativeColdActivity || !facts.NativeStartupComplete
            || !facts.OriginalHomeUncovered || !facts.OriginalPublicationCompleted || !facts.EnabledOriginalAdd
            || !facts.ExactOwnedProfile || !facts.OriginalSourceMatched
            || !Guid.TryParseExact(facts.BeforeBoot, "D", out var before) || before == Guid.Empty
            || !string.Equals(facts.BeforeBoot, facts.AfterBoot, StringComparison.Ordinal)
            || facts.LaunchNanoseconds <= 0 || facts.InitialFrameNanoseconds < facts.LaunchNanoseconds
            || facts.ReadyDrawNanoseconds < facts.InitialFrameNanoseconds
            || facts.ReadyFrameNanoseconds <= facts.ReadyDrawNanoseconds)
            throw new InvalidOperationException("Original loaded cold startup evidence is incomplete or belongs to another lifetime.");
        // Keep slow observations: the two-second target is evaluated on the result, never used to discard it.
        return (facts.ReadyFrameNanoseconds - facts.LaunchNanoseconds) / 1_000_000d;
    }
}
