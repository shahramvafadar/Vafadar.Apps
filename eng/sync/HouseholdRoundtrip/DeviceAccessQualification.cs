using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>Complete original existing-member device bootstrap, lost enrollment acknowledgement, actual fresh identity session, approved history and cold same-key finance.</summary>
internal static class DeviceAccessQualification
{
    /// <summary>Uses only the retained fictional cost owner and a fresh separately protected client, preserving every completed phase.</summary>
    internal static async Task RunAsync(string root, LocalSyncPeer peer, string mode, IServiceProvider service, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("Original own-device adoption invariant failed."); }
        var progressPath = Path.Combine(root, "own-device-adoption.json");
        var progress = File.Exists(progressPath) ? JsonSerializer.Deserialize<Progress>(await File.ReadAllTextAsync(progressPath, ct)) ?? throw new IOException() : null;
        if (progress?.Phase == 5 || progress?.Phase == 4 && mode != "device-access-cold") throw new IOException("Never replay completed device adoption.");
        var ownerRoot = Path.Combine(root, "recovered-owner"); var childRoot = Path.Combine(root, "self-enrolled-owner");
        using var owner = await ProtectedClient.OpenAsync(ownerRoot, peer, false, ct);
        using var child = await ProtectedClient.OpenAsync(childRoot, peer, !Directory.Exists(childRoot), ct);
        var tokens = new ProtectedSessions(ownerRoot, () =>
        { if (File.ReadAllText(Path.Combine(ownerRoot, "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); });
        using var transport = new HouseholdServiceTransport(peer, tokens);
        var identity = await transport.IdentityAsync(ct);
        var journal = owner.Services.GetRequiredService<HouseholdJournal>(); var workspace = owner.Services.GetRequiredService<HouseholdWorkspace>();
        var own = await journal.CaptureAsync(peer, identity.Id, ct) ?? throw new IOException();
        var childJournal = child.Services.GetRequiredService<HouseholdJournal>(); var childWorkspace = child.Services.GetRequiredService<HouseholdWorkspace>();
        async Task SaveAsync(int phase) { progress = progress! with { Phase = phase }; await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(progress), ct); }
        var packagePath = Path.Combine(root, "own-device-public-access.json");
        if (progress is null)
        {
            var package = await workspace.CreateDeviceAccessAsync(transport, own.Authority, identity.Id, ct);
            await File.WriteAllTextAsync(packagePath, JsonSerializer.Serialize(package, HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessPackage), ct);
            var rejected = false;
            try { _ = await childWorkspace.EnrollOwnDeviceAsync(transport, package, new('0', 64), ct); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected && await childJournal.CaptureAsync(peer, identity.Id, ct) is null && (await child.Store.GetEntriesAsync(cancellationToken: ct)).Count == 0);
            progress = new(0, identity.Id, null, Guid.Empty); await SaveAsync(0);
        }
        if (progress.Phase == 0)
        {
            Require(mode == "device-access-cut"); await SaveAsync(1);
            await File.WriteAllTextAsync(Path.Combine(root, "own-device-enrollment-cut.marker"), "Original own-device enrollment cut v1", ct);
            var package = JsonSerializer.Deserialize(await File.ReadAllTextAsync(packagePath, ct), HouseholdMetadataJsonContext.Default.HouseholdDeviceAccessPackage) ?? throw new IOException();
            _ = await childWorkspace.EnrollOwnDeviceAsync(transport, package,
                new Vafadar.Cryptography.PublicSigningRecipientIdentity(package.OwnerKeys.SigningKey, package.OwnerKeys.WrappingKey).Fingerprint, ct);
            throw new IOException("The real committed enrollment response must be cut before the local remote binding.");
        }
        var pending = await childJournal.CaptureAsync(peer, identity.Id, ct) ?? throw new IOException();
        if (progress.Phase == 1)
        {
            Require(mode == "device-access-resume" && pending.SpaceId == Guid.Empty && pending.DeviceId == Guid.Empty && pending.MembershipVersion == 0);
            progress = progress with { DeviceFingerprint = pending.DeviceFingerprint }; await SaveAsync(1);
            // This exact fixture identity has no retained password. Obtain one fresh real local
            // session through reset/outbox/completion, never by manufacturing a bearer or device grant.
            const string email = "household-native-cost-v2-owner@example.invalid";
            var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA!9";
            using var http = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
            using var requested = await http.PostAsJsonAsync("identity/recover", new IdentityInput(email, ""), ct); Require(requested.StatusCode == HttpStatusCode.Accepted);
            await using var scope = service.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            var user = await db.Users.SingleAsync(row => row.Email == email && row.EmailConfirmed && row.Id == identity.Id, ct);
            var challenge = await db.Challenges.AsNoTracking().Where(row => row.UserId == user.Id && row.Purpose == "reset" && !row.Consumed)
                .OrderByDescending(row => row.ExpiresAt).FirstAsync(ct);
            var mail = JsonSerializer.Deserialize<Vafadar.Zanance.Api.PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new IOException(); Require(mail.Recipient == email);
            using var completed = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, password), ct); Require(completed.StatusCode == HttpStatusCode.NoContent);
            try { Require((await transport.SignInAsync(email, password, ct)).Id == identity.Id); } finally { password = ""; }
            own = await workspace.RebindCurrentDeviceAsync(transport, own.Authority, ct); await SaveAsync(2);
            proof("real-new-fictional-owner-session-after-lost-device-enrollment-response-preserves-original-approved-owner-key");
        }
        if (progress.Phase == 2)
        {
            var retained = pending;
            pending = await childWorkspace.ResumeOwnDeviceAsync(transport, pending.Authority, ct);
            Require(pending.DeviceFingerprint == progress.DeviceFingerprint && retained.Authority == pending.Authority && pending.MembershipVersion == 0);
            var refused = false;
            try { _ = await transport.CheckpointAsync(pending.SpaceId, pending.DeviceId, ct); }
            catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.NotFound) { refused = true; }
            Require(refused && !(await childWorkspace.CompleteJoinAsync(transport, pending.Authority, "Fictitious same-identity device", ct)));
            Require((await child.Store.GetEntriesAsync(cancellationToken: ct)).Count == 0 && (await child.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct)).Costs.Count == 0);
            progress = progress with { DeviceId = pending.DeviceId }; await SaveAsync(3);
            proof("same-original-private-device-and-operation-resume-under-real-new-session-with-no-pending-financial-or-key-access");
        }
        if (progress.Phase == 3)
        {
            own = await owner.Services.GetRequiredService<HouseholdSyncCoordinator>().RefreshAuthorityAsync(transport, own.Authority, ct);
            var roster = (await journal.ReadCurrentMembershipAsync(own.Authority, ct)).Snapshot();
            if (!roster.Members.SelectMany(member => member.Devices).Any(device => device.Id == progress.DeviceId))
            {
                var request = (await transport.PendingDevicesAsync(own.SpaceId, own.DeviceId, ct)).Single(row => row.DeviceId == progress.DeviceId);
                await workspace.ApproveDeviceAsync(transport, own.Authority, own.MembershipVersion, request, pending.DeviceFingerprint, ct);
            }
            pending = await childWorkspace.ResumeOwnDeviceAsync(transport, pending.Authority, ct);
            Require(await childWorkspace.CompleteJoinAsync(transport, pending.Authority, "Fictitious same-identity device", ct));
            await SaveAsync(4);
            proof("original-device-access-anchor-and-later-explicit-owner-approval-adopt-full-original-encrypted-financial-history");
        }
        var current = await childJournal.CaptureAsync(peer, identity.Id, ct) ?? throw new IOException();
        if (mode == "device-access-cold")
        {
            Require(progress.Phase == 4 && current.DeviceFingerprint == progress.DeviceFingerprint && current.DeviceId == progress.DeviceId);
            current = await childWorkspace.RebindCurrentDeviceAsync(transport, current.Authority, ct);
        }
        var costs = await child.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
        Require(costs.CanWrite && costs.Totals.Single().NetCost == 1001 && costs.Costs.Count == 2 && costs.Allocations.Count == 4 && costs.Settlements.Count == 1 && (await child.Store.GetEntriesAsync(cancellationToken: ct)).Count == 0);
        // An actual owner-only operation opens this newly approved device's real root delegation.
        var delegated = await childWorkspace.CreateDeviceAccessAsync(transport, current.Authority, identity.Id, ct);
        Require(delegated.Kind == HouseholdSetupAnchors.DeviceAccess && current.DeviceFingerprint == progress.DeviceFingerprint);
        proof(mode == "device-access-cold" ? "cold-original-device-key-reopens-full-finance-and-current-owner-private-root-without-kit-or-pasted-package" :
            "newly-approved-device-receives-original-costs-shares-private-repayment-and-actual-owner-root-without-cash-reposting");
        if (mode == "device-access-cold") await SaveAsync(5);
    }

    private sealed record Progress(int Phase, string Identity, string? DeviceFingerprint, Guid DeviceId);
}
