using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Sharing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>Original two-identity ownership handoff with target-owned keys and real process cuts, retaining the existing financial fixture.</summary>
internal static class OwnershipQualification
{
    /// <summary>New cold process opens only the original target's committed root/epoch after its private handoff candidates were explicitly retired.</summary>
    internal static async Task ColdAsync(string root, LocalSyncPeer peer, Action<string> proof, CancellationToken ct)
    {
        var completed = Path.Combine(root, "ownership-cold.complete");
        var progress = JsonSerializer.Deserialize<Progress>(await File.ReadAllTextAsync(Path.Combine(root, "ownership-handoff.json"), ct)) ?? throw new IOException();
        if (!progress.Complete || File.Exists(completed)) throw new IOException("Never replay completed cold ownership acceptance.");
        var targetPath = Path.Combine(root, "recipient");
        using var target = await ProtectedClient.OpenAsync(targetPath, peer, false, ct);
        using var http = new HouseholdServiceTransport(peer, new ProtectedSessions(targetPath, () =>
        { if (File.ReadAllText(Path.Combine(targetPath, "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); }));
        var identity = await http.IdentityAsync(ct); var journal = target.Services.GetRequiredService<HouseholdJournal>();
        var original = await journal.CaptureAsync(peer, identity.Id, ct) ?? throw new IOException();
        var state = await target.Services.GetRequiredService<HouseholdWorkspace>().RebindCurrentDeviceAsync(http, original.Authority, ct);
        var membership = await journal.ReadCurrentMembershipAsync(state.Authority, ct);
        using var keys = await journal.ReadKeysAsync(state.Authority, ct);
        using var epoch = await journal.ReadEpochAsync(state.Authority, membership.Epoch, ct);
        if (membership.Snapshot().OwnerId != identity.Id || identity.Id == progress.OldOwner || keys.Owner?.Public != membership.Root ||
            membership.Epoch != progress.OldEpoch + 1 || epoch.KeyCommitment != membership.Snapshot().KeyCommitment ||
            await journal.ReadOwnershipAsync(state.Authority, ct) is not null || await FirstDeviceReceiveQualification.FinancialDigestAsync(target) != progress.TargetFinancial)
            throw new IOException("The committed cold ownership root, epoch or financial rows changed.");
        await using (var db = await target.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            db.Accounts.Add(new Account(Guid.NewGuid()) { Name = "Fictitious new owner rollback write", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) });
            if (await db.SaveChangesAsync(ct) != 1) throw new IOException();
            await transaction.RollbackAsync(ct);
        }
        if (await FirstDeviceReceiveQualification.FinancialDigestAsync(target) != progress.TargetFinancial) throw new IOException();
        await File.WriteAllTextAsync(completed, "Original committed ownership cold root and financial writer verified v1", ct);
        proof("cold-new-owner-opens-original-committed-root-and-epoch-after-handoff-finish-and-has-real-final-writer-authority-with-exact-financial-rollback");
    }

    /// <summary>Continues only the original retained operation; completed modes never replay money, membership or key generation.</summary>
    internal static async Task RunAsync(string root, LocalSyncPeer peer, IServiceProvider service, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("Original ownership handoff invariant failed."); }
        var path = Path.Combine(root, "ownership-handoff.json");
        var progress = File.Exists(path) ? JsonSerializer.Deserialize<Progress>(await File.ReadAllTextAsync(path, ct)) ?? throw new IOException() : null;
        if (progress?.Complete == true) throw new IOException("Never repeat completed original ownership acceptance.");
        var ownerPath = Path.Combine(root, "recovered-owner"); var targetPath = Path.Combine(root, "recipient");
        using var owner = await ProtectedClient.OpenAsync(ownerPath, peer, false, ct);
        using var target = await ProtectedClient.OpenAsync(targetPath, peer, false, ct);
        owner.Services.MigrateLocalDatabase<ZananceDbContext>(); target.Services.MigrateLocalDatabase<ZananceDbContext>();
        HouseholdServiceTransport Open(string clientRoot) => new(peer, new ProtectedSessions(clientRoot, () =>
        { if (File.ReadAllText(Path.Combine(clientRoot, "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); }));
        using var sourceHttp = Open(ownerPath); using var targetHttp = Open(targetPath);
        async Task<string> IdentityAsync(HouseholdServiceTransport http, string email)
        {
            try { return (await http.IdentityAsync(ct)).Id; }
            catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.Unauthorized)
            {
                // Renew only the explicitly marked fictitious account through the real
                // reset/outbox/sign-in endpoints. No bearer or role is manufactured.
                var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA!9";
                using var client = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
                using var request = await client.PostAsJsonAsync("identity/recover", new IdentityInput(email, ""), ct);
                Require(request.StatusCode == HttpStatusCode.Accepted);
                await using var scope = service.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                var user = await db.Users.SingleAsync(row => row.Email == email && row.EmailConfirmed, ct);
                var challenge = await db.Challenges.AsNoTracking().Where(row => row.UserId == user.Id && row.Purpose == "reset" && !row.Consumed)
                    .OrderByDescending(row => row.ExpiresAt).FirstAsync(ct);
                var mail = JsonSerializer.Deserialize<Vafadar.Zanance.Api.PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new IOException();
                Require(mail.Recipient == email);
                using var completed = await client.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, password), ct);
                Require(completed.StatusCode == HttpStatusCode.NoContent);
                try { var result = await http.SignInAsync(email, password, ct); Require(result.Id == user.Id); return result.Id; }
                finally { password = ""; }
            }
        }
        var sourceIdentity = await IdentityAsync(sourceHttp, "household-native-cost-v2-owner@example.invalid");
        var targetIdentity = await IdentityAsync(targetHttp, "household-native-cost-v2-recipient@example.invalid");
        var sourceJournal = owner.Services.GetRequiredService<HouseholdJournal>(); var targetJournal = target.Services.GetRequiredService<HouseholdJournal>();
        var sourceWorkspace = owner.Services.GetRequiredService<HouseholdWorkspace>(); var targetWorkspace = target.Services.GetRequiredService<HouseholdWorkspace>();
        var source = await sourceJournal.CaptureAsync(peer, sourceIdentity, ct) ?? throw new IOException();
        var recipient = await targetJournal.CaptureAsync(peer, targetIdentity, ct) ?? throw new IOException();
        try { source = await sourceWorkspace.RebindCurrentDeviceAsync(sourceHttp, source.Authority, ct); }
        catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.NotFound)
        {
            if (await sourceJournal.ReadOwnershipAsync(source.Authority, ct) is null) throw;
            proof("removed-original-owner-retains-only-own-private-possession-receipt-observation");
        }
        recipient = await targetWorkspace.RebindCurrentDeviceAsync(targetHttp, recipient.Authority, ct);
        async Task SaveAsync() => await File.WriteAllTextAsync(path, JsonSerializer.Serialize(progress), ct);
        if (progress is null)
        {
            Require((await target.Services.GetRequiredService<HouseholdSyncCoordinator>().SynchronizeAsync(targetHttp, recipient.Authority, ct)).Applied);
            var original = (await sourceJournal.ReadCurrentMembershipAsync(source.Authority, ct)).Snapshot();
            var offered = await sourceJournal.ReadOwnershipAsync(source.Authority, ct);
            if (offered is null) offered = await sourceWorkspace.BeginOwnershipAsync(sourceHttp, source.Authority, source.MembershipVersion, targetIdentity,
                recipient.DeviceId, recipient.DeviceFingerprint, HouseholdFormerOwnerRole.Leave, ct);
            else offered = await sourceWorkspace.AdvanceOwnershipAsync(sourceHttp, source.Authority, offered.OperationId!.Value, offered.Stage, ct);
            Require(offered.Stage == HouseholdOwnershipStage.Offered && offered.OperationId is not null);
            // An explicit decline returns the old room to its original authority without
            // creating or destroying target keys. Preserve its public terminal receipt.
            var canceled = await targetWorkspace.ReadOwnershipAsync(targetHttp, recipient.Authority, ct) ?? throw new IOException();
            canceled = await targetWorkspace.CancelOwnershipAsync(targetHttp, recipient.Authority, canceled.OperationId!.Value, ct);
            Require(canceled.Stage == HouseholdOwnershipStage.Canceled);
            await targetWorkspace.FinishOwnershipAsync(targetHttp, recipient.Authority, canceled.OperationId!.Value, ct);
            await sourceWorkspace.FinishOwnershipAsync(sourceHttp, source.Authority, canceled.OperationId!.Value, ct);
            source = await owner.Services.GetRequiredService<HouseholdSyncCoordinator>().RefreshAuthorityAsync(sourceHttp, source.Authority, ct);
            Require((await sourceJournal.ReadCurrentMembershipAsync(source.Authority, ct)).Snapshot().OwnerId == sourceIdentity);
            var actual = await sourceWorkspace.BeginOwnershipAsync(sourceHttp, source.Authority, source.MembershipVersion, targetIdentity,
                recipient.DeviceId, recipient.DeviceFingerprint, HouseholdFormerOwnerRole.Leave, ct);
            progress = new(actual.OperationId!.Value, original.OwnerId, original.KeyEpoch, original.OwnerKeys,
                await FirstDeviceReceiveQualification.FinancialDigestAsync(owner), await FirstDeviceReceiveQualification.FinancialDigestAsync(target), false);
            await SaveAsync(); proof("explicit-target-decline-keeps-old-owner-and-finance-before-original-new-handoff");
        }
        for (var step = 0; step < 8; step++)
        {
            var observed = await targetWorkspace.ReadOwnershipAsync(targetHttp, recipient.Authority, ct) ?? throw new IOException();
            Require(observed.OperationId == progress.Operation);
            if (observed.Stage == HouseholdOwnershipStage.Published) break;
            if (observed.Stage is HouseholdOwnershipStage.Offered or HouseholdOwnershipStage.Authorized)
                _ = await targetWorkspace.AdvanceOwnershipAsync(targetHttp, recipient.Authority, progress.Operation, observed.Stage, ct);
            else if (observed.Stage is HouseholdOwnershipStage.Prepared or HouseholdOwnershipStage.Accepted)
                _ = await sourceWorkspace.AdvanceOwnershipAsync(sourceHttp, source.Authority, progress.Operation, observed.Stage, ct);
            else throw new IOException("The original ownership handoff cannot be replaced or silently restarted.");
        }
        var final = await targetWorkspace.ReadOwnershipAsync(targetHttp, recipient.Authority, ct) ?? throw new IOException();
        Require(final.Stage == HouseholdOwnershipStage.Published);
        _ = await targetWorkspace.AdvanceOwnershipAsync(targetHttp, recipient.Authority, progress.Operation, HouseholdOwnershipStage.Published, ct);
        _ = await sourceWorkspace.AdvanceOwnershipAsync(sourceHttp, source.Authority, progress.Operation, HouseholdOwnershipStage.Published, ct);
        var membership = await targetJournal.ReadCurrentMembershipAsync(recipient.Authority, ct);
        Require(membership.Snapshot().OwnerId == targetIdentity && membership.Snapshot().PreviousOwnerId == progress.OldOwner && membership.Epoch == progress.OldEpoch + 1 &&
            membership.Snapshot().OwnerKeys != progress.OldRoot && !membership.Snapshot().Members.Any(member => member.IdentityId == progress.OldOwner));
        using (var keys = await targetJournal.ReadKeysAsync(recipient.Authority, ct)) Require(keys.Owner?.Public == membership.Root);
        var denied = false;
        try { _ = await sourceHttp.CheckpointAsync(source.SpaceId, source.DeviceId, ct); }
        catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.NotFound) { denied = true; }
        Require(denied);
        await using (var scope = service.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            Require(!await db.Set<HouseholdKeyGrantRow>().AnyAsync(row => row.SpaceId == source.SpaceId && row.Epoch == membership.Epoch && row.DeviceId == source.DeviceId, ct));
            Require(!await db.Set<HouseholdOwnerRootGrantRow>().AnyAsync(row => row.SpaceId == source.SpaceId && row.RootFingerprint == membership.Root.Fingerprint && row.DeviceId == source.DeviceId, ct));
        }
        Require(await FirstDeviceReceiveQualification.FinancialDigestAsync(owner) == progress.SourceFinancial &&
            await FirstDeviceReceiveQualification.FinancialDigestAsync(target) == progress.TargetFinancial);
        await targetWorkspace.FinishOwnershipAsync(targetHttp, recipient.Authority, progress.Operation, ct);
        await sourceWorkspace.FinishOwnershipAsync(sourceHttp, source.Authority, progress.Operation, ct);
        // Check actual final ordinary writer authority without retaining fictitious money.
        await using (var db = await owner.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            db.Accounts.Add(new Account(Guid.NewGuid()) { Name = "Fictitious removed owner write", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) });
            denied = false; try { await db.SaveChangesAsync(ct); } catch (HouseholdWriteRejectedException) { denied = true; }
            await transaction.RollbackAsync(ct); Require(denied);
        }
        progress = progress with { Complete = true }; await SaveAsync();
        proof("original-target-owned-root-epoch-two-sided-transfer-with-exact-lost-response-continuation-and-unchanged-financial-columns");
        proof("departed-old-owner-has-neither-new-epoch-nor-root-grant-and-cannot-checkpoint-or-write-original-ledger");
    }

    /// <summary>Public original operation and comparison digests only; no credential or private key is retained here.</summary>
    private sealed record Progress(Guid Operation, string OldOwner, long OldEpoch, HouseholdPublicKeys OldRoot, string SourceFinancial, string TargetFinancial, bool Complete);
}
