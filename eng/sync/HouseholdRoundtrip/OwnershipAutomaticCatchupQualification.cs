using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>Fresh offered-stage automatic financial reception plus explicit former-owner editor access, without repeating the earlier retained-candidate case.</summary>
internal static class OwnershipAutomaticCatchupQualification
{
    /// <summary>Reuses both now-approved original identities/devices and restores only its own fictitious cost-title edit through real financial Save/sync.</summary>
    internal static async Task RunAsync(string root, LocalSyncPeer peer, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("Original automatic ownership reception invariant failed."); }
        var path = Path.Combine(root, "ownership-automatic-reception.json");
        var progress = File.Exists(path) ? JsonSerializer.Deserialize<Progress>(await File.ReadAllTextAsync(path, ct)) ?? throw new IOException() : new(0);
        if (progress.Phase == 5) throw new IOException("Never repeat completed automatic reception.");
        var priorPath = Path.Combine(root, "ownership-catchup.json");
        if (!File.Exists(priorPath)) throw new IOException("Original preceding transfer must finish first.");
        using var preceding = JsonDocument.Parse(await File.ReadAllTextAsync(priorPath, ct));
        if (preceding.RootElement.GetProperty("Phase").GetInt32() != 8)
            throw new IOException("Original preceding transfer must finish first.");
        var sourcePath = Path.Combine(root, "returning-owner"); var targetPath = Path.Combine(root, "recipient");
        using var source = await ProtectedClient.OpenAsync(sourcePath, peer, false, ct); using var target = await ProtectedClient.OpenAsync(targetPath, peer, false, ct);
        HouseholdServiceTransport Open(string clientRoot) => new(peer, new ProtectedSessions(clientRoot, () =>
        { if (File.ReadAllText(Path.Combine(clientRoot, "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); }));
        using var sourceHttp = Open(Path.Combine(root, "recovered-owner")); using var targetHttp = Open(targetPath);
        var sourceIdentity = await sourceHttp.IdentityAsync(ct); var targetIdentity = await targetHttp.IdentityAsync(ct);
        var sourceJournal = source.Services.GetRequiredService<HouseholdJournal>(); var targetJournal = target.Services.GetRequiredService<HouseholdJournal>();
        var sourceWorkspace = source.Services.GetRequiredService<HouseholdWorkspace>(); var targetWorkspace = target.Services.GetRequiredService<HouseholdWorkspace>();
        var sourceCoordinator = source.Services.GetRequiredService<HouseholdSyncCoordinator>(); var targetCoordinator = target.Services.GetRequiredService<HouseholdSyncCoordinator>();
        var own = await sourceJournal.CaptureAsync(peer, sourceIdentity.Id, ct) ?? throw new IOException();
        var recipient = await targetJournal.CaptureAsync(peer, targetIdentity.Id, ct) ?? throw new IOException();
        own = await sourceWorkspace.RebindCurrentDeviceAsync(sourceHttp, own.Authority, ct);
        recipient = await targetWorkspace.RebindCurrentDeviceAsync(targetHttp, recipient.Authority, ct);
        async Task SaveAsync(int phase) { progress = progress with { Phase = phase }; await File.WriteAllTextAsync(path, JsonSerializer.Serialize(progress), ct); }
        async Task EditAsync(ProtectedClient client, string title)
        {
            var store = client.Services.GetRequiredService<HouseholdCostStore>(); var review = await store.ReadAsync(ct); var cost = review.Costs.Single(item => item.Id == progress.Cost);
            cost.Title = title; await store.SaveCostAsync(review, cost, review.Allocations.Where(row => row.CostId == cost.Id).ToArray(), ct);
        }
        if (progress.Phase == 0)
        {
            var cost = (await source.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct)).Costs.OrderBy(item => item.Id).First();
            progress = progress with { Cost = cost.Id, Original = cost.Title, Published = cost.Title + " automatic receive",
                FinancialDigest = await FirstDeviceReceiveQualification.FinancialDigestAsync(source) }; await SaveAsync(1);
        }
        if (progress.Phase == 1)
        {
            var current = (await source.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct)).Costs.Single(item => item.Id == progress.Cost);
            if (current.Title == progress.Original) await EditAsync(source, progress.Published!); else Require(current.Title == progress.Published);
            Require((await sourceCoordinator.SynchronizeAsync(sourceHttp, own.Authority, ct)).Applied);
            Require((await target.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct)).Costs.Single(item => item.Id == progress.Cost).Title == progress.Original);
            own = await sourceCoordinator.RefreshAuthorityAsync(sourceHttp, own.Authority, ct);
            var offered = await sourceJournal.ReadOwnershipAsync(own.Authority, ct) ?? await sourceWorkspace.BeginOwnershipAsync(sourceHttp, own.Authority,
                own.MembershipVersion, targetIdentity.Id, recipient.DeviceId, recipient.DeviceFingerprint, HouseholdFormerOwnerRole.Editor, ct);
            progress = progress with { Operation = offered.OperationId!.Value }; await SaveAsync(2);
        }
        if (progress.Phase == 2)
        {
            var observed = await targetWorkspace.ReadOwnershipAsync(targetHttp, recipient.Authority, ct) ?? throw new IOException();
            Require(observed.Stage is HouseholdOwnershipStage.Offered or HouseholdOwnershipStage.Prepared);
            var denied = false;
            if (observed.Stage == HouseholdOwnershipStage.Offered)
            {
            try { _ = await targetWorkspace.CatchUpOwnershipAsync(targetHttp, recipient.Authority, Guid.NewGuid(), ct); }
            catch (InvalidOperationException) { denied = true; }
            Require(denied);
            denied = false;
            try { _ = await targetCoordinator.SynchronizeAsync(targetHttp, recipient.Authority, ct); }
            catch (HouseholdWriteRejectedException error) when (error.Reason == HouseholdWriteBlockReason.Rotation) { denied = true; }
            Require(denied && (await target.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct)).Costs.Single(item => item.Id == progress.Cost).Title == progress.Original);
            }
            var before = await targetHttp.CheckpointAsync(recipient.SpaceId, recipient.DeviceId, ct);
            if (observed.Stage == HouseholdOwnershipStage.Offered)
                observed = await targetWorkspace.AdvanceOwnershipAsync(targetHttp, recipient.Authority, progress.Operation, HouseholdOwnershipStage.Offered, ct);
            var after = await targetHttp.CheckpointAsync(recipient.SpaceId, recipient.DeviceId, ct);
            Require(observed.Stage == HouseholdOwnershipStage.Prepared && observed.Preparation is not null && before.Cursor == after.Cursor && before.PublicationHead == after.PublicationHead &&
                (await target.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct)).Costs.Single(item => item.Id == progress.Cost).Title == progress.Published);
            var appliedAt = (await target.Sync.StatusAsync(ct)).LastSuccessAt;
            var originalCandidate = HouseholdOwnershipProtocol.PreparationDigest(observed.Preparation!);
            var repeated = await targetWorkspace.CatchUpOwnershipAsync(targetHttp, recipient.Authority, progress.Operation, ct);
            Require(repeated.Preparation is not null && HouseholdOwnershipProtocol.PreparationDigest(repeated.Preparation) == originalCandidate &&
                (await target.Sync.StatusAsync(ct)).LastSuccessAt == appliedAt && await targetJournal.ReadAttemptAsync(recipient.Authority, ct) is null);
            await using (var db = await target.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
            {
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                denied = false;
                var row = await db.HouseholdCosts.SingleAsync(item => item.Id == progress.Cost, ct); row.Title += " refused";
                try { await db.SaveChangesAsync(ct); }
                catch (HouseholdWriteRejectedException error) when (error.Reason == HouseholdWriteBlockReason.Rotation) { denied = true; }
                await transaction.RollbackAsync(ct); Require(denied);
            }
            await SaveAsync(3); proof("fresh-target-continues-offer-only-after-real-financial-adoption-with-no-remote-publication-and-no-reusable-normal-writer-grant");
            proof("wrong-operation-and-ordinary-rotation-sync-refused-with-original-pending-round-preserved");
        }
        if (progress.Phase == 3)
        {
            for (var step = 0; step < 4; step++)
            {
                var observed = await targetWorkspace.ReadOwnershipAsync(targetHttp, recipient.Authority, ct) ?? throw new IOException();
                if (observed.Stage == HouseholdOwnershipStage.Published) break;
                if (observed.Stage is HouseholdOwnershipStage.Prepared or HouseholdOwnershipStage.Accepted)
                    _ = await sourceWorkspace.AdvanceOwnershipAsync(sourceHttp, own.Authority, progress.Operation, observed.Stage, ct);
                else if (observed.Stage == HouseholdOwnershipStage.Authorized)
                    _ = await targetWorkspace.AdvanceOwnershipAsync(targetHttp, recipient.Authority, progress.Operation, observed.Stage, ct);
                else throw new IOException("Do not replace the original ownership operation.");
            }
            await targetWorkspace.FinishOwnershipAsync(targetHttp, recipient.Authority, progress.Operation, ct);
            await sourceWorkspace.FinishOwnershipAsync(sourceHttp, own.Authority, progress.Operation, ct); await SaveAsync(4);
        }
        if (progress.Phase == 4)
        {
            var membership = await sourceJournal.ReadCurrentMembershipAsync(own.Authority, ct);
            Require(membership.Snapshot().OwnerId == targetIdentity.Id && membership.Snapshot().Members.Single(member => member.IdentityId == sourceIdentity.Id).Role == HouseholdRole.Editor);
            using (var keys = await sourceJournal.ReadKeysAsync(own.Authority, ct)) Require(keys.Owner is null);
            Require((await sourceJournal.ReadAccessAsync(own.Authority, ct)).CanWrite);
            var current = (await source.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct)).Costs.Single(item => item.Id == progress.Cost);
            if (current.Title == progress.Published) await EditAsync(source, progress.Original!); else Require(current.Title == progress.Original);
            Require((await sourceCoordinator.SynchronizeAsync(sourceHttp, own.Authority, ct)).Applied);
            Require((await targetCoordinator.SynchronizeAsync(targetHttp, recipient.Authority, ct)).Applied);
            Require(await FirstDeviceReceiveQualification.FinancialDigestAsync(source) == progress.FinancialDigest && await FirstDeviceReceiveQualification.FinancialDigestAsync(target) == progress.FinancialDigest);
            await SaveAsync(5); proof("former-owner-retained-as-explicit-editor-writes-through-new-epoch-without-new-private-root-and-both-original-financial-digests-restored");
        }
    }

    /// <summary>Public operation and fictitious comparison metadata only.</summary>
    private sealed record Progress(int Phase, Guid Cost = default, string? Original = null, string? Published = null, string? FinancialDigest = null, Guid Operation = default);
}
