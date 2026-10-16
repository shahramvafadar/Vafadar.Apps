using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>A new accepted device receives newer saved finance before an original ownership transfer, retaining the former owner as a viewer.</summary>
internal static class OwnershipCatchupQualification
{
    /// <summary>Uses the existing actual owner and a distinct newly invited protected profile; completed financial/key cases are never reset.</summary>
    internal static async Task RunAsync(string root, LocalSyncPeer peer, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("Original ownership catch-up invariant failed."); }
        var progressPath = Path.Combine(root, "ownership-catchup.json");
        var progress = File.Exists(progressPath) ? JsonSerializer.Deserialize<Progress>(await File.ReadAllTextAsync(progressPath, ct)) ?? throw new IOException() : new(0);
        if (progress.Phase == 8) throw new IOException("Never replay completed ownership catch-up.");
        var ownerPath = Path.Combine(root, "recipient"); var targetPath = Path.Combine(root, "returning-owner");
        var sessionPath = Path.Combine(root, "recovered-owner");
        using var owner = await ProtectedClient.OpenAsync(ownerPath, peer, false, ct);
        using var target = await ProtectedClient.OpenAsync(targetPath, peer, !Directory.Exists(targetPath), ct);
        owner.Services.MigrateLocalDatabase<ZananceDbContext>(); target.Services.MigrateLocalDatabase<ZananceDbContext>();
        HouseholdServiceTransport Open(string path) => new(peer, new ProtectedSessions(path, () =>
        { if (File.ReadAllText(Path.Combine(path, "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); }));
        using var ownerHttp = Open(ownerPath); using var targetHttp = Open(sessionPath);
        var ownerIdentity = await ownerHttp.IdentityAsync(ct); var targetIdentity = await targetHttp.IdentityAsync(ct);
        var ownerJournal = owner.Services.GetRequiredService<HouseholdJournal>(); var targetJournal = target.Services.GetRequiredService<HouseholdJournal>();
        var ownerWorkspace = owner.Services.GetRequiredService<HouseholdWorkspace>(); var targetWorkspace = target.Services.GetRequiredService<HouseholdWorkspace>();
        var ownerCoordinator = owner.Services.GetRequiredService<HouseholdSyncCoordinator>(); var targetCoordinator = target.Services.GetRequiredService<HouseholdSyncCoordinator>();
        var own = await ownerJournal.CaptureAsync(peer, ownerIdentity.Id, ct) ?? throw new IOException();
        own = await ownerWorkspace.RebindCurrentDeviceAsync(ownerHttp, own.Authority, ct);
        async Task SaveAsync(int phase) { progress = progress with { Phase = phase }; await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(progress), ct); }
        if (progress.Phase == 0)
        {
            var package = await ownerWorkspace.InviteAsync(ownerHttp, own.Authority, HouseholdRole.Editor, ct);
            var requested = await targetWorkspace.ClaimAsync(targetHttp, package,
                new Vafadar.Cryptography.PublicSigningRecipientIdentity(package.OwnerKeys.SigningKey, package.OwnerKeys.WrappingKey).Fingerprint, ct);
            Require(requested.Authority.IdentityId == targetIdentity.Id && requested.DeviceFingerprint != own.DeviceFingerprint);
            progress = progress with { TargetDevice = requested.DeviceId }; await SaveAsync(1);
        }
        var recipient = await targetJournal.CaptureAsync(peer, targetIdentity.Id, ct) ?? throw new IOException();
        if (progress.Phase == 1)
        {
            var roster = (await ownerJournal.ReadCurrentMembershipAsync(own.Authority, ct)).Snapshot();
            if (!roster.Members.SelectMany(member => member.Devices).Any(device => device.Id == recipient.DeviceId))
            {
                var pending = (await ownerHttp.PendingClaimsAsync(own.SpaceId, own.DeviceId, ct)).Single(item => item.DeviceId == recipient.DeviceId);
                await ownerWorkspace.ApproveAsync(ownerHttp, own.Authority, pending, recipient.DeviceFingerprint, ct);
            }
            if ((await target.Sync.StatusAsync(ct)).LastSuccessAt is null)
                Require(await targetWorkspace.CompleteJoinAsync(targetHttp, recipient.Authority, "Fictitious returning owner separate device", ct));
            var invitation = await ownerJournal.ReadInvitationNoticeAsync(own.Authority, ct);
            if (invitation is not null) await ownerWorkspace.FinishPendingInvitationAsync(ownerHttp, own.Authority, invitation, ct);
            var cost = (await owner.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct)).Costs.OrderBy(item => item.Id).First();
            progress = progress with { Cost = cost.Id, OriginalName = cost.Title, PublishedName = cost.Title + " catch-up",
                FinancialDigest = await FirstDeviceReceiveQualification.FinancialDigestAsync(owner) };
            await SaveAsync(2); proof("new-explicitly-invited-returning-identity-uses-separate-original-protected-device-and-complete-finance");
        }
        if (progress.Phase == 2)
        {
            var costs = owner.Services.GetRequiredService<HouseholdCostStore>(); var review = await costs.ReadAsync(ct); var cost = review.Costs.Single(item => item.Id == progress.Cost);
            if (cost.Title == progress.OriginalName)
            { cost.Title = progress.PublishedName!; await costs.SaveCostAsync(review, cost, review.Allocations.Where(row => row.CostId == cost.Id).ToArray(), ct); }
            else Require(cost.Title == progress.PublishedName);
            Require((await ownerCoordinator.SynchronizeAsync(ownerHttp, own.Authority, ct)).Applied);
            Require((await target.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct)).Costs.Single(item => item.Id == progress.Cost).Title == progress.OriginalName);
            await SaveAsync(3); proof("original-owner-published-new-saved-cost-title-while-real-target-ledger-remained-offline");
        }
        if (progress.Phase == 3)
        {
            own = await ownerCoordinator.RefreshAuthorityAsync(ownerHttp, own.Authority, ct);
            var original = await ownerJournal.ReadOwnershipAsync(own.Authority, ct);
            if (original is null) original = await ownerWorkspace.BeginOwnershipAsync(ownerHttp, own.Authority, own.MembershipVersion,
                targetIdentity.Id, recipient.DeviceId, recipient.DeviceFingerprint, HouseholdFormerOwnerRole.Viewer, ct);
            progress = progress with { Operation = original.OperationId!.Value }; await SaveAsync(4);
        }
        if (progress.Phase == 4)
        {
            var observed = await targetWorkspace.ReadOwnershipAsync(targetHttp, recipient.Authority, ct) ?? throw new IOException();
            if (observed.Stage == HouseholdOwnershipStage.Offered)
                observed = await targetWorkspace.AdvanceOwnershipAsync(targetHttp, recipient.Authority, progress.Operation, observed.Stage, ct);
            Require(observed.Stage == HouseholdOwnershipStage.Prepared && observed.Preparation is not null);
            progress = progress with { PreparationDigest = HouseholdOwnershipProtocol.PreparationDigest(observed.Preparation!) }; await SaveAsync(5);
            var applied = (await target.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct)).Costs.Single(item => item.Id == progress.Cost).Title == progress.PublishedName;
            await File.WriteAllTextAsync(Path.Combine(root, "ownership-catchup-original-observation.json"), JsonSerializer.Serialize(new
            { TargetKeysPrepared = true, NewSavedFinancialRowsApplied = applied, OriginalOperationAndCandidatesRetained = true }), ct);
            if (!applied) throw new InvalidDataException("Ownership target prepared keys before receiving already published actual financial rows; retain this original operation.");
        }
        if (progress.Phase == 5)
        {
            // Continuation of the original counterexample is implemented with the production
            // incoming-only path; no replacement root, epoch, invitation or financial copy.
            var original = await targetWorkspace.CatchUpOwnershipAsync(targetHttp, recipient.Authority, progress.Operation, ct);
            Require(original.Stage == HouseholdOwnershipStage.Prepared && original.Preparation is not null &&
                HouseholdOwnershipProtocol.PreparationDigest(original.Preparation) == progress.PreparationDigest &&
                (await target.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct)).Costs.Single(item => item.Id == progress.Cost).Title == progress.PublishedName);
            await SaveAsync(6); proof("same-original-target-candidate-and-operation-adopt-frozen-published-finance-without-any-outgoing-proposal");
        }
        if (progress.Phase == 6)
        {
            for (var step = 0; step < 4; step++)
            {
                var observed = await targetWorkspace.ReadOwnershipAsync(targetHttp, recipient.Authority, ct) ?? throw new IOException();
                if (observed.Stage == HouseholdOwnershipStage.Published) break;
                if (observed.Stage == HouseholdOwnershipStage.Prepared || observed.Stage == HouseholdOwnershipStage.Accepted)
                    _ = await ownerWorkspace.AdvanceOwnershipAsync(ownerHttp, own.Authority, progress.Operation, observed.Stage, ct);
                else if (observed.Stage == HouseholdOwnershipStage.Authorized)
                    _ = await targetWorkspace.AdvanceOwnershipAsync(targetHttp, recipient.Authority, progress.Operation, observed.Stage, ct);
                else throw new IOException("Continue the same original handoff; do not replace expired or canceled requests.");
            }
            await targetWorkspace.FinishOwnershipAsync(targetHttp, recipient.Authority, progress.Operation, ct);
            await ownerWorkspace.FinishOwnershipAsync(ownerHttp, own.Authority, progress.Operation, ct);
            await SaveAsync(7);
        }
        if (progress.Phase == 7)
        {
            var membership = await targetJournal.ReadCurrentMembershipAsync(recipient.Authority, ct);
            Require(membership.Snapshot().OwnerId == targetIdentity.Id && membership.Snapshot().Members.Single(member => member.IdentityId == ownerIdentity.Id).Role == HouseholdRole.Viewer);
            using (var oldKeys = await ownerJournal.ReadKeysAsync(own.Authority, ct)) Require(oldKeys.Owner is null);
            var ownerAccess = await ownerJournal.ReadAccessAsync(own.Authority, ct); Require(!ownerAccess.CanWrite && ownerAccess.BlockedBy == HouseholdWriteBlockReason.Viewer);
            var costs = target.Services.GetRequiredService<HouseholdCostStore>(); var review = await costs.ReadAsync(ct); var cost = review.Costs.Single(item => item.Id == progress.Cost);
            if (cost.Title == progress.PublishedName)
            { cost.Title = progress.OriginalName!; await costs.SaveCostAsync(review, cost, review.Allocations.Where(row => row.CostId == cost.Id).ToArray(), ct); }
            else Require(cost.Title == progress.OriginalName);
            Require((await targetCoordinator.SynchronizeAsync(targetHttp, recipient.Authority, ct)).Applied);
            Require((await ownerCoordinator.SynchronizeAsync(ownerHttp, own.Authority, ct)).Applied);
            Require(await FirstDeviceReceiveQualification.FinancialDigestAsync(owner) == progress.FinancialDigest &&
                await FirstDeviceReceiveQualification.FinancialDigestAsync(target) == progress.FinancialDigest);
            await SaveAsync(8); proof("actual-retained-former-owner-is-view-only-with-no-new-private-root-and-complete-original-financial-columns-restored-on-both-devices");
        }
    }

    /// <summary>Only public original device/operation and fictitious comparison labels/digests, never credentials or private root/epoch frames.</summary>
    private sealed record Progress(int Phase, Guid TargetDevice = default, Guid Cost = default, string? OriginalName = null,
        string? PublishedName = null, string? FinancialDigest = null, Guid Operation = default, string? PreparationDigest = null);
}
