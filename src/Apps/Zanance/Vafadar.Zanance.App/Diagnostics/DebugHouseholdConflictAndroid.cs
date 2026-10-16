#if DEBUG && ANDROID
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original native cost and conflict pages on the already accepted fictitious second owner device; no synthetic financial authority.</summary>
internal static class DebugHouseholdConflictAndroid
{
    private const string Package = "pro.vafadar.zanance.householdlifecycle";
    private const string Profile = "67f06bf729fa4c1dabce42802f2ca8bd";
    private const string Identity = "df8f98b5-121e-412a-b996-b69b19808e35";
    private const string Baseline = "4f28c370feb93b0e131223bf6638e24d30f2b29b0e749f2cabaf35271ff25592";
    private const string Note = "Fictitious Android competing cost note";
    private static Func<string, Task>? navigate;
    private static bool changing;

    /// <summary>Changes only the independently owned review page; submissions and confirmations remain actual native controls.</summary>
    internal static async void ApplyMode(string? mode)
    {
        if (mode is not ("costs" or "household" or "review" or "history" or "compact-export" or "original") || changing || navigate is null || AppInfo.Current.PackageName != Package) return;
        changing = true;
        try { await navigate(mode); }
        catch (Exception error) when (error is not OutOfMemoryException)
        { await File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, "household-main-conflict-error.json"), JsonSerializer.Serialize(new { Phase = "navigation", Kind = error.GetType().Name, Process = Environment.ProcessId })); }
        finally { changing = false; }
    }

    /// <summary>Reuses exactly the retained approved device/profile and original actual financial graph.</summary>
    internal static bool StartIfRequested(App app, IServiceProvider services)
    {
        if (AppInfo.Current.PackageName != Package || Environment.GetEnvironmentVariable("VAFADAR_HOUSEHOLD_CONFLICT_REVIEW") != "1") return false;
        app.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), async () =>
        {
            var phase = "startup";
            try
            {
                await app.WaitForStartupAsync();
                var folder = FileSystem.AppDataDirectory;
                if (!folder.Contains(Package, StringComparison.Ordinal)) throw new IOException("Independent protected installation required.");
                var profiles = services.GetRequiredService<ProfileService>();
                var target = profiles.All.Single(item => item.Id == Profile && item.Name == "Fictitious household second owner device");
                if (profiles.Current.Id != Profile && !await profiles.SwitchReviewedAsync(profiles.ReadWorkReview(), target)) throw new IOException();
                var environment = services.GetRequiredService<IPersonalSyncEnvironment>();
                var saved = environment.ReadConnection();
                var peer = new LocalSyncPeer(saved.Address, saved.Fingerprint);
                var journal = services.GetRequiredService<HouseholdJournal>();
                var state = await journal.CaptureAsync(peer, Identity) ?? throw new IOException();
                if (state.SpaceId != Guid.Parse("208f06f3-08ff-4a68-b27b-bb4159f7e95d") || state.DeviceId != Guid.Parse("f4f4d6ec-40f4-4f6b-9772-5e4cf928f928") ||
                    state.DeviceFingerprint != "5aa2af161b888ed7c08fd50762d8509724bb45474eba298624859a33b049046f") throw new IOException("Original accepted device required.");
                var marker = Path.Combine(folder, "household-main-conflict-state.json");
                if (!File.Exists(marker))
                {
                    if (await DebugSnapshots.HouseholdFinancialFingerprintAsync(services) != Baseline) throw new IOException("Original complete unedited financial baseline required.");
                    await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(new { Profile, Baseline, state.SpaceId, state.DeviceId, state.DeviceFingerprint }));
                }
                else
                {
                    using var original = JsonDocument.Parse(await File.ReadAllTextAsync(marker));
                    if (original.RootElement.GetProperty("Profile").GetString() != Profile || original.RootElement.GetProperty("Baseline").GetString() != Baseline)
                        throw new IOException("Preserve original conflict fixture.");
                }
                HouseholdCostsViewModel? costs = null; HouseholdViewModel? household = null; PersonalSyncViewModel? review = null;
                var revision = 0; string currentMode = "";
                async Task ReportAsync()
                {
                    if (profiles.Current.Id != Profile) throw new IOException("Original profile retired.");
                    var localAccess = await journal.ReadAccessAsync(state.Authority);
                    var financial = await services.GetRequiredService<HouseholdCostStore>().ReadAsync();
                    var row = financial.Costs.Single(item => item.Id == Guid.Parse("790d83c4-8b2d-44f2-8997-a6c1b0211658"));
                    var accounts = await services.GetRequiredService<ZananceStore>().GetAccountsAsync();
                    var entries = await services.GetRequiredService<ZananceStore>().GetEntriesAsync();
                    var originalCash = accounts.Count == 1 && accounts[0].Id == Guid.Parse("0fe856b7-727d-44c2-bfc1-645f4d766be4") &&
                        accounts[0].CurrencyCode == "EUR" && accounts[0].OpeningBalance == 10000 && entries.Count == 1 &&
                        entries[0].Id == Guid.Parse("27cbcefc-ed04-4bce-92d1-265583ddd3d3") && entries[0].AccountId == accounts[0].Id &&
                        entries[0].Kind == EntryKind.Expense && entries[0].Amount == 450;
                    var packet = JsonSerializer.Serialize(new { Process = Environment.ProcessId, Profile, Mode = currentMode, state.SpaceId, state.DeviceId, state.DeviceFingerprint,
                        Ready = costs?.IsReady ?? household?.IsReady ?? review?.IsReady ?? false,
                        Busy = costs?.IsBusy ?? household?.IsBusy ?? review?.IsBusy ?? false,
                        HasError = costs?.Error is not null || household?.Error is not null || review?.Error is not null,
                        CostEditing = costs?.EditingCost, CostCanWrite = costs?.CanWrite, DraftPrepared = costs?.Note == Note,
                        OriginalCashLedgerPreserved = originalCash, OriginalCostAmountPreserved = row.Amount == 1001 && row.CurrencyCode == "KWD",
                        OriginalAllocationsPreserved = financial.Allocations.Where(item => item.CostId == row.Id).Select(item => item.Amount).Order().SequenceEqual(new long[] { 500, 501 }),
                        CostNoteIsAndroid = row.Note == Note, CostNoteIsWindows = row.Note == "Fictitious Windows competing cost note",
                        household?.CanSynchronize, household?.CanAdminister, household?.CanSetUp, household?.CanReadAudit, household?.IsOwner,
                        localAccess.CanWrite, BlockedBy = localAccess.BlockedBy?.ToString(), ReviewCount = review?.Conflicts.Count,
                        HouseholdReview = review?.IsHouseholdConnection, FinancialHash = await DebugSnapshots.HouseholdFinancialFingerprintAsync(services),
                        NativeCommandsAndConfirmationsRequired = true, PhoneOrWholeBatchAcceptance = false });
                    var path = Path.Combine(folder, "household-main-conflict-proof.json");
                    await File.WriteAllTextAsync(path + ".writing", packet); File.Move(path + ".writing", path, true);
                }
                void Observe()
                {
                    var current = ++revision;
                    app.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(400), async () =>
                    {
                        if (current != revision || costs?.IsBusy == true || household?.IsBusy == true || review?.IsBusy == true) return;
                        try { await ReportAsync(); }
                        catch (Exception error) when (error is not OutOfMemoryException)
                        { await File.WriteAllTextAsync(Path.Combine(folder, "household-main-conflict-error.json"), JsonSerializer.Serialize(new { Phase = "observation", Kind = error.GetType().Name, Process = Environment.ProcessId })); }
                    });
                }
                navigate = async mode =>
                {
                    if (costs?.IsBusy == true || household?.IsBusy == true || review?.IsBusy == true || costs?.EditingCost == true) throw new IOException("Retain the original active command/draft.");
                    if (mode == "original")
                    {
                        if (!await profiles.SwitchReviewedAsync(profiles.ReadWorkReview(), profiles.All.Single(item => item.IsMain))) throw new IOException("Original main switch refused.");
                        var originalHash = await DebugSnapshots.HouseholdFinancialFingerprintAsync(services);
                        if (originalHash != "a67218d075ae8a549e736d62db0e70afcc4072233ffe313ba13b82f04e6e52a8") throw new IOException("Original main financial data changed.");
                        await File.WriteAllTextAsync(Path.Combine(folder, "household-main-conflict-original-preserved.json"), JsonSerializer.Serialize(new
                        { Process = Environment.ProcessId, OriginalMainFinancialHash = originalHash, FinancialColumnsPreserved = true, NetworkRequestedForOriginal = false }));
                        app.Quit(); return;
                    }
                    if (mode == "compact-export")
                    {
                        var proofPath = Path.Combine(folder, "household-main-compact-export-proof.json");
                        if (File.Exists(proofPath)) throw new IOException("Never replay accepted compact export.");
                        var before = await DebugSnapshots.HouseholdFinancialFingerprintAsync(services);
                        if (before != "86686082ce55aee24df3b9957be54abed413d3296e9cd6c9280f04bbc0163ea1") throw new IOException();
                        using var output = new MemoryStream();
                        await services.GetServices<IBackupSource>().Single(source => source.Name == "database.sqlite").WriteAsync(output, default);
                        output.Position = 0;
                        await using var portable = await SqliteMemorySnapshot.OpenAsync(output, SqliteDatabaseBackupSource<ZananceDbContext>.MaximumProtectedSnapshotBytes);
                        await using (var command = portable.Connection.CreateCommand())
                        {
                            command.CommandText = "SELECT Amount,CurrencyCode FROM HouseholdCosts";
                            await using (var reader = await command.ExecuteReaderAsync())
                                if (!await reader.ReadAsync() || reader.GetInt64(0) != 1001 || reader.GetString(1) != "KWD" || await reader.ReadAsync()) throw new IOException();
                            command.CommandText = "SELECT Amount FROM HouseholdAllocations ORDER BY Amount";
                            await using (var reader = await command.ExecuteReaderAsync())
                                if (!await reader.ReadAsync() || reader.GetInt64(0) != 500 || !await reader.ReadAsync() || reader.GetInt64(0) != 501 || await reader.ReadAsync()) throw new IOException();
                            command.CommandText = "SELECT OpeningBalance,CurrencyCode FROM Accounts";
                            await using (var reader = await command.ExecuteReaderAsync())
                                if (!await reader.ReadAsync() || reader.GetInt64(0) != 10000 || reader.GetString(1) != "EUR" || await reader.ReadAsync()) throw new IOException();
                            command.CommandText = "SELECT COUNT(*),SUM(Amount) FROM Entries";
                            await using (var reader = await command.ExecuteReaderAsync())
                                if (!await reader.ReadAsync() || reader.GetInt64(0) != 1 || reader.GetInt64(1) != 450) throw new IOException();
                            command.CommandText = "SELECT name FROM sqlite_schema WHERE type='table' AND name LIKE 'HouseholdLocal%'";
                            var tables = new List<string>();
                            await using (var reader = await command.ExecuteReaderAsync())
                                while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
                            if (tables.Count != 11) throw new IOException();
                            foreach (var table in tables)
                            {
                                if (!table.All(char.IsAsciiLetterOrDigit)) throw new IOException();
                                command.CommandText = "SELECT COUNT(*) FROM \"" + table + "\"";
                                if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 0) throw new IOException();
                            }
                        }
                        if (before != await DebugSnapshots.HouseholdFinancialFingerprintAsync(services)) throw new IOException();
                        await File.WriteAllTextAsync(proofPath, JsonSerializer.Serialize(new { Process = Environment.ProcessId, Profile,
                            PortableBytes = output.Length, PrivateHouseholdTables = 11, PrivateRowsCopied = 0, FinancialHash = before,
                            OriginalCashAndCompleteCostPreserved = true, NoNetworkRequests = true, OriginalKeysAndCiphertextRetained = true,
                            PhoneOrReleaseAcceptance = false }));
                        return;
                    }
                    if (mode == "history")
                    {
                        var proofPath = Path.Combine(folder, "household-main-history-proof.json");
                        if (File.Exists(proofPath)) throw new IOException("Never replay the accepted original history observation.");
                        var before = await DebugSnapshots.HouseholdFinancialFingerprintAsync(services);
                        if (before != "86686082ce55aee24df3b9957be54abed413d3296e9cd6c9280f04bbc0163ea1") throw new IOException("Retain original accepted cost data.");
                        using var replica = await journal.ReadReplicaAsync(state.Authority);
                        var current = replica.Snapshot(); var seen = new Dictionary<SyncRecordKey, long>();
                        long cursor = 0; var pages = 0;
                        while (cursor < replica.FinancialCursor)
                        {
                            var page = await replica.FinancialFeedAsync(cursor, replica.FinancialCursor);
                            foreach (var row in page.Records)
                            {
                                if (row.Version != ++cursor) throw new IOException("Original financial sequence required.");
                                seen[row.Record] = row.Version;
                            }
                            if (page.NextCursor != cursor || page.Records.Count is < 1 or > 128) throw new IOException();
                            pages++;
                        }
                        if (seen.Count != current.Count || current.Any(row => seen.GetValueOrDefault(row.Key) != row.Value.Version) ||
                            await DebugSnapshots.HouseholdFinancialFingerprintAsync(services) != before) throw new IOException("Original current graph changed.");
                        await File.WriteAllTextAsync(proofPath, JsonSerializer.Serialize(new { Process = Environment.ProcessId, Profile,
                            replica.Cursor, replica.FinancialCursor, Pages = pages, CurrentRows = current.Count, FinancialHash = before,
                            NoNetworkRequests = true, OriginalKeysAndCiphertextRetained = true, PhoneOrReleaseAcceptance = false }));
                        return;
                    }
                    costs = null; household = null; review = null; currentMode = mode;
                    switch (mode)
                    {
                        case "costs":
                            var next = services.GetRequiredService<HouseholdCostsViewModel>(); costs = next;
                            next.PropertyChanged += (_, args) =>
                            {
                                if (args.PropertyName == nameof(next.EditingCost) && next.EditingCost)
                                    app.Dispatcher.Dispatch(() =>
                                    {
                                        // Fictitious form input only. The actual native Edit,
                                        // Save and confirmation still own every mutation.
                                        if (ReferenceEquals(costs, next) && next.EditingCost && next.Note.Length == 0 && next.Costs.Count == 1) next.Note = Note;
                                    });
                                Observe();
                            };
                            app.ShowHouseholdReview(new HouseholdCostsPage(next));
                            for (var wait = 0; wait < 160 && (!next.IsReady || next.IsBusy) && next.Error is null; wait++) await Task.Delay(50);
                            if (!next.IsReady && !next.IsBusy && next.Error is null) await next.ActivateAsync();
                            break;
                        case "household":
                            household = services.GetRequiredService<HouseholdViewModel>();
                            household.ReviewActionFailure = (kind, status, result) => File.WriteAllText(Path.Combine(folder, "household-main-conflict-action-failure.json"),
                                JsonSerializer.Serialize(new { Kind = kind, HttpStatus = status, HResult = result, Process = Environment.ProcessId }));
                            household.PropertyChanged += (_, _) => Observe();
                            app.ShowHouseholdReview(new HouseholdPage(household));
                            for (var wait = 0; wait < 160 && (!household.IsReady || household.IsBusy) && household.Error is null; wait++) await Task.Delay(50);
                            if (!household.IsReady && !household.IsBusy && household.Error is null) await household.ActivateAsync();
                            break;
                        case "review":
                            review = services.GetRequiredService<PersonalSyncViewModel>(); review.PropertyChanged += (_, _) => Observe();
                            app.ShowHouseholdReview(new PersonalSyncPage(review));
                            for (var wait = 0; wait < 160 && (!review.IsReady || review.IsBusy) && review.Error is null; wait++) await Task.Delay(50);
                            if (!review.IsReady && !review.IsBusy && review.Error is null) await review.ActivateAsync();
                            break;
                        default: throw new IOException();
                    }
                    await ReportAsync();
                };
                phase = "original-page"; await navigate("costs");
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            { await File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, "household-main-conflict-error.json"), JsonSerializer.Serialize(new { Phase = phase, Kind = error.GetType().Name, Process = Environment.ProcessId })); }
        });
        return true;
    }
}
#endif
