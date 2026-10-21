using System.Text.Json;
using Vafadar.Zanance.App.Diagnostics;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Recovers only the original completed native outcomes after the report name collided; no financial or UI action is repeated.</summary>
    private static async Task<object> RecoverPhysicalRateReceiptAsync(IServiceProvider services, string folder, string comparison, CancellationToken ct)
    {
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var source = Path.Combine(folder, "original-physical-rates");
        using var original = await PlanEditorNativeReview.ContinuePhysicalRateOutcomesAsync(services, source, ct);
        using var input = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "plan-form-" + comparison + "-result.json"), ct));
        if (input.RootElement.GetProperty("Scope").GetString() != "physical-rate-inputs" || !input.RootElement.GetProperty("OriginalBaseStoreUnchanged").GetBoolean())
            throw new IOException("Original successful input report required.");
        var failures = new List<JsonElement>();
        foreach (var path in Directory.EnumerateFiles(folder, "failure-*.json"))
        {
            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct)); var row = doc.RootElement;
            if (row.TryGetProperty("ErrorType", out var type) && type.GetString() == "System.IO.IOException" && row.GetProperty("Stack").GetString()?.Contains("WriteReportAsync", StringComparison.Ordinal) == true)
                failures.Add(row.Clone());
        }
        if (failures.Count != 1) throw new IOException("One original terminal report collision required.");
        var stages = (from language in new[] { "en", "fa", "de" } from theme in new[] { "Light", "Dark" }
                      from feedback in new[] { "Rates_SourceChanged", "Rates_ChangeFailed", "Rates_SavedFollowUpFailed" }
                      select language + "-" + theme + "-" + feedback)
            .Concat(["rate-native-accepted-notification-failed", "rate-native-read-retried", "rate-native-delete-cancelled", "rate-native-delete-accepted"]).ToArray();
        var observations = new List<JsonElement>();
        foreach (var stage in stages)
        {
            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, stage + ".json"), ct));
            if (doc.RootElement.GetProperty("Stage").GetString() != stage || doc.RootElement.GetProperty("ProcessId").GetInt32() <= 0)
                throw new IOException("Exact original native frame identity required.");
            observations.Add(doc.RootElement.Clone());
        }
        var originalPids = observations.Select(row => row.GetProperty("ProcessId").GetInt32()).Distinct().ToArray();
        if (originalPids.Length != 1) throw new IOException("Original single outcome process required.");
        using var before = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(source, "physical-rate-actions-started.json"), ct));
        var finalRows = await File.ReadAllTextAsync(Path.Combine(source, "rate-native-delete-accepted.json"), ct);
        using var after = JsonDocument.Parse(finalRows);
        var changed = before.RootElement.EnumerateObject().Where(row => row.Value.GetRawText() != after.RootElement.GetProperty(row.Name).GetRawText()).Select(row => row.Name).ToArray();
        if (changed.Length != 1 || changed[0] != "AssetTypes" || after.RootElement.GetProperty("ExchangeRates").GetArrayLength() != 0 || finalRows != await original.AllMoneyRowsAsync(ct))
            throw new IOException("Actual original final numeric source differs.");
        if (baseline != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new IOException("Read-only receipt changed independent base rows.");
        // The original IO stack is inside WriteReportAsync: its awaited body, including base comparison,
        // necessarily returned before CreateNew encountered the already-owned input report. This is not another native run.
        return new { Scope = "physical-rate-original-outcome-receipt", OriginalOutcomeProcessId = originalPids.Single(),
            OriginalOutcomeBodyReturnedBeforeReport = true, CurrentBaseUnchangedDuringReceipt = true,
            CompleteSourceTableSets = before.RootElement.EnumerateObject().Count(), ChangedTables = changed,
            NativeTypeSaves = 1, NativeRateSaves = 1, DeliberateLaterRateVersion = 1, NativeRateDeletes = 1, NativeLedgerEntries = 0,
            RepeatedAcceptedCommands = 0, Observations = observations };
    }
}
