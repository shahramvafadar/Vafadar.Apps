using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Api;
using Vafadar.Data;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>Retains an actual unaccepted member claim across an unrelated roster publication before original admission and removal.</summary>
internal static class PendingMemberQualification
{
    /// <summary>Uses the existing native owner and an independently protected fictional recipient, without replacing prior test data or keys.</summary>
    internal static async Task RunAsync(string root, LocalSyncPeer peer, IServiceProvider service, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("The original pending-member preservation invariant failed."); }
        var progressPath = Path.Combine(root, "pending-member-preservation.json");
        var progress = File.Exists(progressPath) ? JsonSerializer.Deserialize<Progress>(await File.ReadAllTextAsync(progressPath, ct)) ?? throw new IOException() : null;
        if (progress?.Phase == 6) throw new IOException("Do not replay completed pending-member qualification.");
        var ownerRoot = Path.Combine(root, "recovered-owner"); using var owner = await ProtectedClient.OpenAsync(ownerRoot, peer, false, ct);
        using var local = new HouseholdServiceTransport(peer, new ProtectedSessions(ownerRoot, () =>
        { if (File.ReadAllText(Path.Combine(ownerRoot, "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); }));
        var journal = owner.Services.GetRequiredService<HouseholdJournal>(); var coordinator = owner.Services.GetRequiredService<HouseholdSyncCoordinator>();
        var workspace = owner.Services.GetRequiredService<HouseholdWorkspace>();
        var state = await journal.CaptureAsync(peer, (await local.IdentityAsync(ct)).Id, ct) ?? throw new IOException();
        state = await coordinator.RefreshAuthorityAsync(local, state.Authority, ct);
        var childRoot = Path.Combine(root, "pending-member-recipient"); var fresh = progress is null;
        using var child = await ProtectedClient.OpenAsync(childRoot, peer, fresh, ct);
        child.Services.MigrateLocalDatabase<ZananceDbContext>();
        using var remote = new HouseholdServiceTransport(peer, new ProtectedSessions(childRoot, () =>
        { if (File.ReadAllText(Path.Combine(childRoot, "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); }));
        async Task SaveAsync(int phase) { progress = progress! with { Phase = phase }; await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(progress), ct); }
        if (fresh)
        {
            const string email = "household-native-pending-member@example.invalid";
            var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA!9";
            using var http = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
            using var registered = await http.PostAsJsonAsync("identity/register", new IdentityInput(email, password), ct); Require(registered.StatusCode == HttpStatusCode.Accepted);
            await using var scope = service.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            var user = await db.Users.SingleAsync(row => row.Email == email, ct);
            var challenge = await db.Challenges.AsNoTracking().SingleAsync(row => row.UserId == user.Id && row.Purpose == "confirm", ct);
            var mail = JsonSerializer.Deserialize<Vafadar.Zanance.Api.PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new IOException();
            using var confirmed = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), ct);
            Require(confirmed.StatusCode == HttpStatusCode.NoContent);
            try { Require((await remote.SignInAsync(email, password, ct)).Id == user.Id); } finally { password = ""; }
            progress = new(0, user.Id, Guid.Empty, Guid.Empty, null, 0); await SaveAsync(0);
        }
        var childJournal = child.Services.GetRequiredService<HouseholdJournal>(); var childWorkspace = child.Services.GetRequiredService<HouseholdWorkspace>();
        if (progress!.Phase == 0)
        {
            var invitation = await workspace.InviteAsync(local, state.Authority, HouseholdRole.Viewer, ct);
            var current = await journal.ReadCurrentMembershipAsync(state.Authority, ct);
            var own = await childWorkspace.ClaimAsync(remote, invitation, current.Root.Fingerprint, ct);
            // The initial pinned root of a recovered owner can precede its current root. Use the
            // independently read original invitation keys, never a substituted service directory.
            Require(own.Authority.IdentityId == progress.Identity);
            var pending = (await local.PendingClaimsAsync(state.SpaceId, state.DeviceId, ct)).Single(row => row.DeviceId == own.DeviceId);
            var statement = Vafadar.Cryptography.StandardJoseSignatures.VerifyJson(invitation.SignedInvitation, current.Root,
                "zanance-household-invitation+jwt", HouseholdLimits.MembershipBytes, HouseholdMetadataJsonContext.Default.HouseholdInvitationStatement);
            progress = progress with { DeviceId = own.DeviceId, InvitationOperation = statement.OperationId,
                OriginalClaim = pending.SignedClaim, MembershipBeforeChange = state.MembershipVersion,
                InvitationRoot = invitation.OwnerKeys, InvitationNotice = await journal.ReadInvitationNoticeAsync(state.Authority, ct) }; await SaveAsync(1);
        }
        if (progress.Phase == 1)
        {
            state = await coordinator.RefreshAuthorityAsync(local, state.Authority, ct);
            var roster = (await journal.ReadCurrentMembershipAsync(state.Authority, ct)).Snapshot();
            if (roster.Version == progress.MembershipBeforeChange)
            {
                var another = roster.Members.Single(member => member.Role != HouseholdRole.Owner);
                await workspace.ChangeMemberAsync(local, state.Authority, roster.Version, another.IdentityId,
                    another.Role == HouseholdRole.Viewer ? HouseholdRole.Editor : HouseholdRole.Viewer, ct);
            }
            else Require(roster.Version == progress.MembershipBeforeChange + 1 && roster.Change == HouseholdMembershipChange.ChangeRole);
            state = await coordinator.RefreshAuthorityAsync(local, state.Authority, ct);
            var pending = (await local.PendingClaimsAsync(state.SpaceId, state.DeviceId, ct)).Single(row => row.DeviceId == progress.DeviceId && row.IdentityId == progress.Identity);
            Require(pending.SignedClaim == progress.OriginalClaim);
            var fingerprint = new Vafadar.Cryptography.PublicSigningRecipientIdentity(pending.DeviceKeys.SigningKey, pending.DeviceKeys.WrappingKey).Fingerprint;
            await workspace.ApproveAsync(local, state.Authority, pending, fingerprint, ct); await SaveAsync(2);
            proof("original-unaccepted-member-and-claim-device-survive-unrelated-role-change-before-exact-original-owner-approval");
        }
        var childState = await childJournal.CaptureAsync(peer, progress.Identity, ct) ?? throw new IOException();
        if (progress.Phase == 2)
        {
            Require(await childWorkspace.CompleteJoinAsync(remote, childState.Authority, "Fictitious pending-consent viewer", ct));
            var snapshot = await child.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
            Require(!snapshot.CanWrite && snapshot.Costs.Count == 2 && snapshot.Allocations.Count == 4 && snapshot.Settlements.Count == 1 && snapshot.Totals.Single().NetCost == 1001);
            await SaveAsync(3); proof("same-native-pending-recipient-admitted-after-owner-root-retirement-and-receives-original-encrypted-finance-as-read-only-viewer");
        }
        if (progress.Phase == 3)
        {
            state = await coordinator.RefreshAuthorityAsync(local, state.Authority, ct);
            var before = await journal.ReadCurrentMembershipAsync(state.Authority, ct);
            if (before.Root.SigningKey == progress.InvitationRoot!.SigningKey)
            {
                var kit = await local.RecoveryAsync(state.SpaceId, ct);
                state = await workspace.RevokeRecoveryAsync(local, state.Authority, kit.EnrollmentId, state.MembershipVersion, ct);
            }
            var after = await journal.ReadCurrentMembershipAsync(state.Authority, ct);
            Require(after.Root.SigningKey != progress.InvitationRoot.SigningKey && after.Root.WrappingKey != progress.InvitationRoot.WrappingKey);
            var notice = await journal.ReadInvitationNoticeAsync(state.Authority, ct);
            if (notice is not null)
            {
                Require(notice == progress.InvitationNotice);
                var staleNoticeRefused = false;
                try { await workspace.FinishPendingInvitationAsync(local, state.Authority, notice with { FrameDigest = new('0', 64) }, ct); }
                catch (InvalidOperationException) { staleNoticeRefused = true; }
                Require(staleNoticeRefused && await journal.ReadInvitationNoticeAsync(state.Authority, ct) == notice);
                await workspace.FinishPendingInvitationAsync(local, state.Authority, notice, ct);
            }
            Require(await journal.ReadInvitationNoticeAsync(state.Authority, ct) is null);
            Require((await child.Services.GetRequiredService<HouseholdSyncCoordinator>().SynchronizeAsync(remote, childState.Authority, ct)).Applied);
            await SaveAsync(4); proof("exact-original-private-invitation-finished-after-root-retirement-without-old-clipboard-or-resubmission-and-stale-digest-refused");
        }
        if (progress.Phase == 4)
        {
            state = await coordinator.RefreshAuthorityAsync(local, state.Authority, ct);
            await workspace.ChangeMemberAsync(local, state.Authority, state.MembershipVersion, progress.Identity, null, ct);
            await SaveAsync(5);
        }
        var denied = false;
        try { _ = await remote.CheckpointAsync(childState.SpaceId, childState.DeviceId, ct); }
        catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.NotFound) { denied = true; }
        Require(denied);
        var currentCosts = await owner.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
        Require(currentCosts.CanWrite && currentCosts.Totals.Single().NetCost == 1001 && currentCosts.Costs.Count == 2 && currentCosts.Allocations.Count == 4);
        await SaveAsync(6); proof("only-new-fictional-member-explicitly-removed-after-regression-with-original-household-finance-preserved");
    }

    private sealed record Progress(int Phase, string Identity, Guid DeviceId, Guid InvitationOperation, string? OriginalClaim, long MembershipBeforeChange,
        HouseholdPublicKeys? InvitationRoot = null, HouseholdInvitationNotice? InvitationNotice = null);
}
