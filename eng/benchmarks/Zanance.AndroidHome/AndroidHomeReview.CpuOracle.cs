using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Small original presentation controls; no complete ledger graph crosses the preparation process boundary.</summary>
    private sealed record CpuOracleControls(IReadOnlyDictionary<string, long> Balances, CpuOracleRecent[] Recent,
        string Presentation, int PreparedProcessId);

    /// <summary>Original formatted recent-row identity, independent of native controls and mutable domain objects.</summary>
    private sealed record CpuOracleRecent(Guid Id, string Title, string AmountText, bool IsUnreviewed, string Subtitle);

    /// <summary>Builds original financial controls outside the measurement process, without a native publication or save.</summary>
    private static async Task<object> PrepareHomeCpuOracleAsync(App app, IServiceProvider services, string root,
        string comparison, string day, string sourceFingerprint, CancellationToken ct)
    {
        var payloadPath = Path.Combine(root, "cpu-oracle-" + comparison + ".json");
        var readyPath = Path.Combine(root, "cpu-oracle-ready-" + comparison + ".json");
        if (File.Exists(payloadPath) || File.Exists(readyPath))
            throw new ReviewCheckException("Never replace a prepared oracle or repeat its completed preparation.");
        var folder = Path.Combine(root, "tenfold"); RequireRoot(folder);
        var original = Path.Combine(folder, "fixture.db");
        using var data = new ServiceCollection().AddZananceData(original, protectionRequired: true).BuildServiceProvider();
        var location = data.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        var keys = new AndroidGenerationKeyStore(Path.Combine(folder, "keys"));
        SqlCipherProfileGeneration generation;
        using (var owner = await location.Access.AcquireExclusiveAsync(ct))
        {
            var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), () => RequireRoot(folder));
            var identity = (await identities.ReadAsync(ct)).Single();
            var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", identity.InitialCandidateKey, () => RequireRoot(folder));
            generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, "", original, history,
                _ => throw new ReviewCheckException("Oracle preparation requires an existing captured generation."), keys,
                new AndroidGenerationFileDurability(folder), owner, () => RequireRoot(folder), 256 * 1024 * 1024, "4.19.0", ct);
            generation.Publish(location, owner);
        }
        using (generation)
        {
            var local = services.GetRequiredService<ILocalizationService>();
            app.OpenReviewShell();
            await SetLanguageAsync(app, local, "en", ct);
            var today = DateOnly.ParseExact(day, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            using var scalar = new IsolatedHomeReadFactory(data.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), data.GetRequiredService<TimeProvider>());
            var scalarStore = new ZananceStore(scalar, data.GetRequiredService<TimeProvider>(), data.GetRequiredService<ICommercialWriteAccessSource>());
            var expected = await scalarStore.GetHomeSnapshotAsync(today, 0, ct);
            if (expected.Entries.Count != Workload.Tenfold.Entries || expected.QuickResources.Accounts.Count != Workload.Tenfold.Accounts
                || expected.Plans.Schedules.Count != Workload.Tenfold.Schedules)
                throw new ReviewCheckException("Prepared original workload shape differs.");
            var balances = LedgerCalculator.TotalBalances(expected.QuickResources.Accounts, expected.Entries, today);
            var presenter = new Presentation.EntryPresenter(expected.QuickResources.Accounts.ToDictionary(account => account.Id),
                new Presentation.CategoryLookup(expected.QuickResources.Categories, services.GetRequiredService<Translator>()),
                services.GetRequiredService<Translator>(), local.CurrentCulture);
            var recent = expected.Entries.Where(entry => entry.Date <= today)
                .OrderByDescending(entry => entry.Date).ThenByDescending(entry => entry.CreatedAt).Take(3)
                .Select(entry => { var row = presenter.Row(entry); return new CpuOracleRecent(row.Id, row.Title, row.AmountText, row.IsUnreviewed, row.Subtitle); }).ToArray();
            var reads = new WindowReadFactory(scalar);
            var store = new ZananceStore(reads, data.GetRequiredService<TimeProvider>(), data.GetRequiredService<ICommercialWriteAccessSource>());
            var vm = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store);
            string presentation;
            try
            {
                // Run the actual original projection, with no HomePage/native tree or copied financial algorithm.
                await vm.LoadAsync();
                if (!vm.Loading.IsReady || vm.Accounts.Count != Workload.Tenfold.Accounts || reads.SynchronousCaptures != 0)
                    throw new ReviewCheckException("Original oracle projection did not finish its covered read.");
                presentation = CapturePresentation(vm);
            }
            finally { vm.RetirePublication(); }
            if (scalar.CompleteScalarReads < 2 || scalar.DerivedTransportReads != 0)
                throw new ReviewCheckException("Independent financial controls must use the complete original scalar reader.");
            var payload = JsonSerializer.Serialize(new { Balances = balances, Recent = recent, Presentation = presentation,
                Entries = expected.Entries.Count, Accounts = expected.QuickResources.Accounts.Count, Schedules = expected.Plans.Schedules.Count });
            await WriteNewDurableCpuOracleAsync(root, payloadPath, payload, ct);
            var digest = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(payloadPath, ct)));
            var ready = new { Comparison = comparison, Day = day, SourceFingerprint = sourceFingerprint, PayloadSha256 = digest,
                DataModule = typeof(ZananceStore).Module.ModuleVersionId, CoreModule = typeof(LedgerCalculator).Module.ModuleVersionId,
                CallerModule = typeof(HomePage).Module.ModuleVersionId, PreparedProcessId = Environment.ProcessId,
                PreparedUid = Android.OS.Process.MyUid(), CapturedBeforeNativePublication = true, NativePublications = 0, FinancialCommands = 0,
                OriginalScalarLedgerControl = true, CompleteScalarReads = scalar.CompleteScalarReads, DerivedTransportReads = scalar.DerivedTransportReads };
            await WriteNewDurableCpuOracleAsync(root, readyPath, JsonSerializer.Serialize(ready), ct);
            return new { Comparison = comparison, CpuWindowOnly = true, SeparateCpuOracle = true, CpuOraclePrepared = true, PreparedProcessId = Environment.ProcessId,
                NativePublications = 0, FinancialCommands = 0, FullOriginalLedgerAndProjectionUsed = true, OriginalScalarLedgerControl = true,
                CompleteScalarReads = scalar.CompleteScalarReads, DerivedTransportReads = scalar.DerivedTransportReads, PayloadSha256 = digest };
        }
    }

    /// <summary>Durably publishes a new proof only; a failed write cannot overwrite an accepted oracle.</summary>
    private static async Task WriteNewDurableCpuOracleAsync(string root, string path, string json, CancellationToken ct)
    {
        using var parsed = JsonDocument.Parse(json);
        await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            await JsonSerializer.SerializeAsync(output, parsed.RootElement, cancellationToken: ct);
        await new AndroidGenerationFileDurability(root).SynchronizeFileAsync(path, ct);
    }

    /// <summary>Rejects stale source/code/day, same-process controls, another UID or incomplete preparation.</summary>
    private static bool IsolatedCpuOracleIdentityMatches(JsonElement packet, string comparison, string day, string sourceFingerprint,
        string payloadSha256, Guid dataModule, Guid coreModule, Guid callerModule, int processId, int uid) =>
            packet.TryGetProperty("OriginalScalarLedgerControl", out var originalScalar) && originalScalar.ValueKind == JsonValueKind.True
        && packet.GetProperty("CompleteScalarReads").GetInt32() >= 2 && packet.GetProperty("DerivedTransportReads").GetInt32() == 0
        && packet.GetProperty("Comparison").GetString() == comparison && packet.GetProperty("Day").GetString() == day
        && packet.GetProperty("SourceFingerprint").GetString() == sourceFingerprint && packet.GetProperty("PayloadSha256").GetString() == payloadSha256
        && packet.GetProperty("DataModule").GetGuid() == dataModule && packet.GetProperty("CoreModule").GetGuid() == coreModule
        && packet.GetProperty("CallerModule").GetGuid() == callerModule
        && packet.GetProperty("PreparedProcessId").GetInt32() > 0 && packet.GetProperty("PreparedProcessId").GetInt32() != processId
        && packet.GetProperty("PreparedUid").GetInt32() == uid && packet.GetProperty("CapturedBeforeNativePublication").GetBoolean()
        && packet.GetProperty("NativePublications").GetInt32() == 0 && packet.GetProperty("FinancialCommands").GetInt32() == 0;

    /// <summary>Loads only compact controls after exact binding checks; it never reads a complete ledger before timing.</summary>
    private static async Task<CpuOracleControls> ReadIsolatedCpuOracleAsync(string root, string comparison, string day, CancellationToken ct)
    {
        var payloadPath = Path.Combine(root, "cpu-oracle-" + comparison + ".json");
        if (new FileInfo(payloadPath).Length is <= 0 or > 2 * 1024 * 1024)
            throw new ReviewCheckException("A bounded compact oracle is required.");
        var readyPath = Path.Combine(root, "cpu-oracle-ready-" + comparison + ".json");
        if (new FileInfo(readyPath).Length is <= 0 or > 16 * 1024)
            throw new ReviewCheckException("A bounded original oracle identity is required.");
        var payload = await File.ReadAllBytesAsync(payloadPath, ct);
        using var ready = JsonDocument.Parse(await File.ReadAllTextAsync(readyPath, ct));
        var fingerprint = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(root, "source-byte-baseline-" + comparison + ".json"), ct)));
        if (!IsolatedCpuOracleIdentityMatches(ready.RootElement, comparison, day, fingerprint, Convert.ToHexString(SHA256.HashData(payload)),
            typeof(ZananceStore).Module.ModuleVersionId, typeof(LedgerCalculator).Module.ModuleVersionId, typeof(HomePage).Module.ModuleVersionId,
            Environment.ProcessId, Android.OS.Process.MyUid()))
            throw new ReviewCheckException("The original separate-process oracle cannot be rebound or re-prepared.");
        using var parsed = JsonDocument.Parse(payload); var packet = parsed.RootElement;
        if (packet.GetProperty("Entries").GetInt32() != Workload.Tenfold.Entries || packet.GetProperty("Accounts").GetInt32() != Workload.Tenfold.Accounts
            || packet.GetProperty("Schedules").GetInt32() != Workload.Tenfold.Schedules)
            throw new ReviewCheckException("Compact oracle shape differs.");
        var balances = packet.GetProperty("Balances").EnumerateObject().ToDictionary(item => item.Name, item => item.Value.GetInt64(), StringComparer.Ordinal);
        var recent = packet.GetProperty("Recent").EnumerateArray().Select(item => new CpuOracleRecent(item.GetProperty("Id").GetGuid(),
            item.GetProperty("Title").GetString()!, item.GetProperty("AmountText").GetString()!, item.GetProperty("IsUnreviewed").GetBoolean(), item.GetProperty("Subtitle").GetString()!)).ToArray();
        if (balances.Count is <= 0 or > 200 || recent.Length != 3 || recent.Select(item => item.Id).Distinct().Count() != 3)
            throw new ReviewCheckException("Compact financial controls are incomplete.");
        return new(balances, recent, packet.GetProperty("Presentation").GetString() ?? throw new ReviewCheckException("Missing original presentation."),
            ready.RootElement.GetProperty("PreparedProcessId").GetInt32());
    }
}
