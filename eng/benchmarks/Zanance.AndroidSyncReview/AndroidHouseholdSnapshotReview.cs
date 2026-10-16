#if HOUSEHOLD_SNAPSHOT_REVIEW
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Zanance.ProtectedSync;

namespace Vafadar.Zanance.App;

/// <summary>Only the new native portable allocation/authorization mechanism runs; no prior account, network, page or document scenario is repeated.</summary>
internal static class AndroidHouseholdSnapshotReview
{
    /// <summary>Runs the exact same native failure-cleanup case in an independent signed Release AOT/trimmed caller.</summary>
    internal static async Task RunAsync(App app, string run)
    {
        const string package = "pro.vafadar.zanance.householdsnapshotreview";
        var phase = "independent-owned-root";
        void Emit(string stage, string? error = null) => Android.Util.Log.Info("ZananceHouseholdSnapshot",
            JsonSerializer.Serialize(new HouseholdSnapshotReport(run, stage, Environment.ProcessId, error), HouseholdSnapshotJsonContext.Default.HouseholdSnapshotReport));
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            if (AppInfo.Current.PackageName != package || run.Length != 32 || run.Any(value => !char.IsAsciiHexDigit(value))) throw new IOException();
            var root = Path.Combine(FileSystem.AppDataDirectory, "household-snapshot-" + run);
            if (!root.Contains(package, StringComparison.Ordinal) || Directory.Exists(root)) throw new IOException("Preserve a completed or interrupted native case.");
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "fixture.marker"), "Fictitious bounded native portable snapshot v1", deadline.Token);
            phase = "native-allocation-and-authorization"; Emit("started");
            await HistoryQualification.NativeBoundAsync(root, _ => { }, deadline.Token);
            Android.Util.Log.Info("ZananceHouseholdSnapshot", await File.ReadAllTextAsync(Path.Combine(root, "history-native-bound.complete.json"), deadline.Token));
            Emit("passed"); app.Quit();
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { Emit(phase, error.GetType().Name); }
    }
}

/// <summary>Public process/phase only; no key, credential, database path or financial row enters device logs.</summary>
internal sealed record HouseholdSnapshotReport(string Run, string Stage, int Process, string? Error);

/// <summary>Explicit native report roots survive trimming without reflection serialization.</summary>
[JsonSerializable(typeof(HouseholdSnapshotReport))]
internal partial class HouseholdSnapshotJsonContext : JsonSerializerContext;
#endif
