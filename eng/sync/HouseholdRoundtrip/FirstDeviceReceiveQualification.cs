using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>One new actual receiver cuts after committing its real connection and retains first-receive/write gates across process loss.</summary>
internal static class FirstDeviceReceiveQualification
{
    /// <summary>Reuses only the retained fictional actual owner; no completed device/cost scenario is replayed.</summary>
    internal static async Task RunAsync(string root, LocalSyncPeer peer, string mode, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("Original first device reception invariant failed."); }
        var progressPath = Path.Combine(root, "first-device-reception.json");
        var progress = File.Exists(progressPath) ? JsonSerializer.Deserialize<Progress>(await File.ReadAllTextAsync(progressPath, ct)) ?? throw new IOException() : null;
        if (progress?.Phase == 3) throw new IOException("Never repeat completed first device reception.");
        var ownerRoot = Path.Combine(root, "recovered-owner"); var childRoot = Path.Combine(root, "first-receive-owner");
        using var owner = await ProtectedClient.OpenAsync(ownerRoot, peer, false, ct);
        using var child = await ProtectedClient.OpenAsync(childRoot, peer, !Directory.Exists(childRoot), ct);
        using var transport = new HouseholdServiceTransport(peer, new ProtectedSessions(ownerRoot, () =>
        { if (File.ReadAllText(Path.Combine(ownerRoot, "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); }));
        var identity = await transport.IdentityAsync(ct); var journal = owner.Services.GetRequiredService<HouseholdJournal>();
        var workspace = owner.Services.GetRequiredService<HouseholdWorkspace>(); var own = await journal.CaptureAsync(peer, identity.Id, ct) ?? throw new IOException();
        var localJournal = child.Services.GetRequiredService<HouseholdJournal>(); var localWorkspace = child.Services.GetRequiredService<HouseholdWorkspace>();
        async Task SaveAsync(int phase) { progress = progress! with { Phase = phase }; await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(progress), ct); }
        if (progress is null)
        {
            Require(mode == "first-device-receive-cut");
            var package = await workspace.CreateDeviceAccessAsync(transport, own.Authority, identity.Id, ct);
            var rootKey = new Vafadar.Cryptography.PublicSigningRecipientIdentity(package.OwnerKeys.SigningKey, package.OwnerKeys.WrappingKey);
            var requested = await localWorkspace.EnrollOwnDeviceAsync(transport, package, rootKey.Fingerprint, ct);
            progress = new(0, requested.DeviceFingerprint, requested.DeviceId); await SaveAsync(0);
        }
        var state = await localJournal.CaptureAsync(peer, identity.Id, ct) ?? throw new IOException();
        if (progress.Phase == 0)
        {
            own = await owner.Services.GetRequiredService<HouseholdSyncCoordinator>().RefreshAuthorityAsync(transport, own.Authority, ct);
            var roster = (await journal.ReadCurrentMembershipAsync(own.Authority, ct)).Snapshot();
            if (!roster.Members.SelectMany(member => member.Devices).Any(device => device.Id == progress.DeviceId))
            {
                var pending = (await transport.PendingDevicesAsync(own.SpaceId, own.DeviceId, ct)).Single(item => item.DeviceId == state.DeviceId);
                await workspace.ApproveDeviceAsync(transport, own.Authority, own.MembershipVersion, pending, state.DeviceFingerprint, ct);
            }
            state = await localWorkspace.RebindCurrentDeviceAsync(transport, state.Authority, ct);
            await DemandUnpublishedAsync();
            await child.Sync.ConnectAsync(await child.Sync.ReviewAsync(ct), peer, identity.Id, state.DeviceId,
                new(state.SpaceId, "Fictitious original first receiving device", HouseholdFinancialSchema.Hash, state.Cursor, false), ct,
                receiveFirstRun: true, transportKind: ProfileSynchronizationKind.Household);
            await SaveAsync(1);
            await DemandUnpublishedAsync();
            proof("actual-original-receive-connection-committed-with-financial-write-denied-before-first-apply-and-process-exit");
            Environment.Exit(37);
        }
        Require(mode == "first-device-receive-resume" && progress.Phase == 1);
        if (progress.SourceDigest is null)
        {
            // The first process ended before any recipient synchronization. Capture the
            // original source now, before resuming, including every allowlisted Settings column.
            progress = progress with { SourceDigest = await FinancialDigestAsync(owner) }; await SaveAsync(1);
        }
        state = await localWorkspace.RebindCurrentDeviceAsync(transport, state.Authority, ct);
        Require(state.DeviceFingerprint == progress.Fingerprint && state.DeviceId == progress.DeviceId && (await child.Sync.StatusAsync(ct)).LastSuccessAt is null);
        await DemandUnpublishedAsync();
        Require(await localWorkspace.CompleteJoinAsync(transport, state.Authority, "Fictitious original first receiving device", ct));
        var received = await child.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
        Require(received.CanWrite && received.Costs.Count == 2 && received.Allocations.Count == 4 && received.Settlements.Count == 1 &&
            received.Totals.Single().NetCost == 1001 && (await child.Store.GetEntriesAsync(cancellationToken: ct)).Count == 0 && (await child.Sync.StatusAsync(ct)).LastSuccessAt is not null);
        // No receiving-device defaults or empty baseline may have been posted into the source.
        Require((await owner.Services.GetRequiredService<HouseholdSyncCoordinator>().SynchronizeAsync(transport, own.Authority, ct)).Applied);
        var original = await owner.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
        Require(original.Costs.Count == 2 && original.Allocations.Count == 4 && original.Settlements.Count == 1 && original.Totals.Single().NetCost == 1001);
        Require(progress.SourceDigest == await FinancialDigestAsync(owner));
        await SaveAsync(3); proof("same-native-device-connection-resumes-receive-only-after-process-loss-and-unlocks-writes-only-after-original-atomic-financial-apply");

        async Task DemandUnpublishedAsync()
        {
            var access = await localJournal.ReadAccessAsync(state.Authority, ct);
            Require(!access.CanWrite && access.BlockedBy == HouseholdWriteBlockReason.Unpublished);
            await using var db = await child.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            db.Accounts.Add(new Account(Guid.NewGuid()) { Name = "Fictitious forbidden early receive write", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) });
            var denied = false;
            try { await db.SaveChangesAsync(ct); } catch (HouseholdWriteRejectedException error) when (error.Reason == HouseholdWriteBlockReason.Unpublished) { denied = true; }
            await transaction.RollbackAsync(ct); Require(denied);
        }
    }

    /// <summary>Independent raw native comparison of all shipped financial columns; no copied financial calculation or private metadata tables.</summary>
    internal static async Task<string> FinancialDigestAsync(ProtectedClient client)
    {
        await using var db = await client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync();
        await db.Database.OpenConnectionAsync(); using var command = db.Database.GetDbConnection().CreateCommand();
        var tables = new SortedDictionary<string, List<string>>();
        foreach (var table in HouseholdFinancialSchema.Manifest.Tables)
        {
            static string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";
            command.CommandText = "SELECT " + string.Join(",", table.Columns.Select(column => Quote(column.Name))) + " FROM " + Quote(table.Name);
            using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
            while (await reader.ReadAsync()) rows.Add(JsonSerializer.Serialize(Enumerable.Range(0, reader.FieldCount).Select(index => reader.IsDBNull(index) ? null : reader.GetValue(index)).ToArray()));
            rows.Sort(StringComparer.Ordinal); tables[table.Name] = rows;
        }
        var clear = JsonSerializer.SerializeToUtf8Bytes(tables);
        try { return Convert.ToHexStringLower(SHA256.HashData(clear)); }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    /// <summary>Public exact original recipient identity; never private keys, credentials or financial values.</summary>
    private sealed record Progress(int Phase, string Fingerprint, Guid DeviceId, string? SourceDigest = null);
}
