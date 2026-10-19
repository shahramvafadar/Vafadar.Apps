using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies the original remaining surfaces at minimum width without replaying completed larger-width cases.</summary>
    private static async Task<object> ReviewNarrowSurfacesAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        var settingsRun = Platform.CurrentActivity?.Intent?.GetStringExtra("review_settings_run");
        if (settingsRun is null || !System.Text.RegularExpressions.Regex.IsMatch(settingsRun, "^[0-9a-f]{32}$"))
            throw new ReviewCheckException("An explicit retained Settings run is required.");
        var settingsRoot = Path.Combine(Path.GetDirectoryName(root)!, settingsRun);
        if (!File.Exists(Path.Combine(root, "financial-tables.db"))
            || !File.Exists(Path.Combine(settingsRoot, "settings-text.db"))
            || !File.Exists(Path.Combine(settingsRoot, "settings-text-prepared.marker"))
            || RedirectedAncestors(root).Length != 0 || RedirectedAncestors(settingsRoot).Length != 0)
            throw new ReviewCheckException("Original nonredirected financial and Settings fixtures are required; never reseed.");
        var sourceMvid = typeof(AndroidHomeReview).Module.ModuleVersionId;
        var dataMvid = typeof(ZananceStore).Module.ModuleVersionId;
        var results = new List<JsonElement>();
        foreach (var group in new[] { "tables", "legends", "budget-titles", "home-legends", "plan-rows", "ledger-rows", "settings" })
        foreach (var context in new[] { "en-Light", "fa-Light", "de-Light", "en-Dark", "fa-Dark", "de-Dark" })
        {
            RequireNarrowSurfaceConfiguration();
            var fixtureRoot = group == "settings" ? settingsRoot : root;
            var inputHash = await SurfaceInputHashAsync(fixtureRoot, group == "settings", ct);
            var casePhase = "narrow-" + group + "-" + context;
            var receipt = Path.Combine(root, casePhase + "-" + comparison + "-result.json");
            if (File.Exists(receipt))
            {
                using var retained = JsonDocument.Parse(await File.ReadAllTextAsync(receipt, ct));
                var packet = retained.RootElement;
                if (packet.GetProperty("Comparison").GetString() != comparison || packet.GetProperty("Group").GetString() != group
                    || packet.GetProperty("Context").GetString() != context || packet.GetProperty("WidthDp").GetInt32() != 360
                    || packet.GetProperty("OsFontScale").GetDouble() != 2 || packet.GetProperty("SourceMvid").GetGuid() != sourceMvid
                    || packet.GetProperty("DataMvid").GetGuid() != dataMvid || packet.GetProperty("InputHash").GetString() != inputHash
                    || !packet.GetProperty("Passed").GetBoolean() || !packet.GetProperty("OriginalStoredInputsUnchanged").GetBoolean())
                    throw new ReviewCheckException("Retained narrow case differs from its original source, input or successful observation.");
                results.Add(packet.Clone());
                continue;
            }
            Android.Util.Log.Info("ZananceHomeStage", comparison + "|" + casePhase);
            // D-366: each case uses the original page reader; only the unqualified context is selected.
            var result = group == "settings"
                ? await ReviewSettingsTextAsync(app, services, settingsRoot, comparison, ct, context)
                : await ReviewFinancialTablesAsync(app, services, root, comparison, ct,
                    legendsOnly: group == "legends", budgetTitlesOnly: group == "budget-titles",
                    homeLegendsOnly: group == "home-legends", planRowsOnly: group == "plan-rows",
                    ledgerRowsOnly: group == "ledger-rows", onlyContext: context);
            RequireNarrowSurfaceConfiguration();
            RequireHomeWindowFocus();
            var unchanged = inputHash == await SurfaceInputHashAsync(fixtureRoot, group == "settings", ct);
            var value = JsonSerializer.SerializeToElement(result);
            var observations = value.GetProperty("Observations");
            var parts = context.Split('-');
            var passed = unchanged && value.GetProperty("OriginalWindowFocusRequired").GetBoolean()
                && observations.GetArrayLength() == 1
                && observations[0].GetProperty("Language").GetString() == parts[0]
                && observations[0].GetProperty("Theme").GetString() == parts[1];
            if (group == "settings")
                passed &= value.GetProperty("StoredInputsUnchanged").GetBoolean()
                    && observations[0].GetProperty("Failures").GetArrayLength() == 0
                    && observations[0].GetProperty("OriginalPickerCancelRetainsSelection").GetBoolean();
            else passed &= value.GetProperty("OriginalStoredInputsUnchanged").GetBoolean();
            var report = new { Comparison = comparison, Group = group, Context = context, WidthDp = 360, OsFontScale = 2,
                SourceMvid = sourceMvid, DataMvid = dataMvid, InputHash = inputHash, OriginalStoredInputsUnchanged = unchanged,
                Passed = passed, Result = value };
            // A failed observation is retained too; it cannot silently become a completed case on continuation.
            await WriteReportAsync(root, casePhase, report, ct);
            if (!passed) throw new ReviewCheckException("Original narrow surface observation failed; preserve its exact receipt.");
            results.Add(JsonSerializer.SerializeToElement(report));
        }
        return new { Comparison = comparison, SourceMvid = sourceMvid, DataMvid = dataMvid, WidthDp = 360, OsFontScale = 2,
            OriginalStoredInputsUnchanged = true, Prior412DpCasesReplayed = false, MainPackageTouched = false,
            FinancialSaveInvoked = false, SystemSettingsWrittenByCaller = false, Cases = results };
    }

    /// <summary>Refuses accidental context narrowing or unknown language/theme combinations.</summary>
    private static void ValidateSurfaceContext(string? context)
    {
        if (context is not null && context is not ("en-Light" or "fa-Light" or "de-Light" or "en-Dark" or "fa-Dark" or "de-Dark"))
            throw new ReviewCheckException("One explicit original surface context is required.");
    }

    /// <summary>Reads the actual Activity configuration before every new or retained minimum-width case.</summary>
    private static void RequireNarrowSurfaceConfiguration()
    {
        var configuration = Platform.CurrentActivity?.Resources?.Configuration;
        if (configuration is null || Math.Abs(configuration.FontScale - 2f) > .001f || configuration.ScreenWidthDp != 360)
            throw new ReviewCheckException("Original narrow surfaces require actual OS scale2.0 at360dp.");
    }

    /// <summary>Binds each retained case to complete original fixture inputs rather than a success flag alone.</summary>
    private static async Task<string> SurfaceInputHashAsync(string root, bool settings, CancellationToken ct)
    {
        var path = Path.Combine(root, settings ? "settings-text.db" : "financial-tables.db");
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        var store = fixture.GetRequiredService<ZananceStore>();
        var original = JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
            Entries = await store.GetEntriesAsync(cancellationToken: ct), Settings = await store.GetSettingsAsync(ct),
            Budgets = await store.GetBudgetsAsync(ct) });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(original)));
    }
}
