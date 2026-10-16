using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.ProtectedSync;

/// <summary>Actual original workspace kit enrollment over retained fictitious native keys/finance and the real local service.</summary>
internal static class RecoveryQualification
{
    /// <summary>Exercises the real ordinary membership path while recovery is enabled, including the unchanged recipient's complete verified root-transition history.</summary>
    internal static async Task AppendAsync(string root, HouseholdServiceTransport originalTransport, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("The active-kit membership append invariant failed."); }
        const string marker = "active-kit-ordinary-role-change-appends-original-epoch-and-existing-viewer-verifies-recovered-owner-root-without-private-delegation";
        if (File.ReadLines(Path.Combine(root, "checkpoints.txt")).Contains(marker)) throw new InvalidOperationException("Do not repeat completed active-kit append.");
        var peer = originalTransport.Peer; var clientRoot = Path.Combine(root, "recovered-owner");
        using var client = await ProtectedClient.OpenAsync(clientRoot, peer, false, ct);
        using var transport = new HouseholdServiceTransport(peer, new ProtectedSessions(clientRoot, () =>
        { if (!File.Exists(Path.Combine(clientRoot, "client.marker"))) throw new IOException("Original recovered native client required."); }));
        var identity = await transport.IdentityAsync(ct); var journal = client.Services.GetRequiredService<HouseholdJournal>();
        var state = await journal.CaptureAsync(peer, identity.Id, ct) ?? throw new InvalidDataException();
        state = await client.Services.GetRequiredService<HouseholdSyncCoordinator>().RefreshAuthorityAsync(transport, state.Authority, ct);
        var roster = (await journal.ReadCurrentMembershipAsync(state.Authority, ct)).Snapshot();
        var recipientIdentity = roster.Members.Single(member => member.Role != HouseholdRole.Owner).IdentityId;
        var workspace = client.Services.GetRequiredService<HouseholdWorkspace>();
        if (await journal.ReadMembershipProposalAsync(state.Authority, ct) is not null)
            state = await workspace.ResumePendingAdministrationAsync(transport, state.Authority, ct);
        else if (roster.Members.Single(member => member.IdentityId == recipientIdentity).Role != HouseholdRole.Viewer)
            await workspace.ChangeMemberAsync(transport, state.Authority, roster.Version, recipientIdentity, HouseholdRole.Viewer, ct);
        var observed = await transport.RecoveryAsync(state.SpaceId, ct);
        var last = await transport.RecoveryEpochsAsync(state.SpaceId, observed.EnrollmentId, observed.ThroughEpoch - 1, observed.ThroughEpoch, ct);
        Require(last.Epochs.Length == 1 && last.Epochs[0].Epoch == observed.ThroughEpoch && last.Complete);
        var recipientRoot = Path.Combine(root, "recipient"); using var recipient = await ProtectedClient.OpenAsync(recipientRoot, peer, false, ct);
        recipient.Services.MigrateLocalDatabase<Vafadar.Zanance.Data.ZananceDbContext>();
        using var remote = new HouseholdServiceTransport(peer, new ProtectedSessions(recipientRoot, () =>
        { if (!File.Exists(Path.Combine(recipientRoot, "client.marker"))) throw new IOException("Original recipient required."); }));
        var recipientJournal = recipient.Services.GetRequiredService<HouseholdJournal>();
        var own = await recipientJournal.CaptureAsync(peer, (await remote.IdentityAsync(ct)).Id, ct) ?? throw new InvalidDataException();
        Require((await recipient.Services.GetRequiredService<HouseholdSyncCoordinator>().SynchronizeAsync(remote, own.Authority, ct)).Applied);
        var current = await recipientJournal.ReadCurrentMembershipAsync(own.Authority, ct);
        Require(current.Snapshot().Members.Single(member => member.IdentityId == recipientIdentity).Role == HouseholdRole.Viewer);
        var refused = false;
        try { _ = await remote.OwnerRootAsync(state.SpaceId, own.DeviceId, ct); }
        catch (Vafadar.Zanance.Data.Sync.PersonalSyncHttpException error) when (error.Status == System.Net.HttpStatusCode.NotFound) { refused = true; }
        Require(refused);
        var snapshot = await recipient.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
        Require(snapshot.Costs.Count == 2 && snapshot.Totals.Single().NetCost == 1001 && snapshot.Costs.Any(row => row.Title.EndsWith(" reviewed in original app", StringComparison.Ordinal)));
        proof(marker);
    }

    /// <summary>Continues only the recovered native device after a real cold reopen, optionally retiring the separately held kit with another exact root transition.</summary>
    internal static async Task ColdAsync(string root, HouseholdServiceTransport originalTransport, bool revoke,
        Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("The recovered native cold/revocation invariant failed."); }
        var marker = revoke ? "native-kit-revocation-rotates-both-owner-roles-preserves-finance-and-refuses-old-kit-discovery" :
            "cold-original-recovered-native-device-keeps-own-device-root-and-all-original-cost-history-without-kit-import";
        if (File.ReadLines(Path.Combine(root, "checkpoints.txt")).Contains(marker)) throw new InvalidOperationException("Do not repeat completed recovery phase.");
        var clientRoot = Path.Combine(root, "recovered-owner"); var peer = originalTransport.Peer;
        using var client = await ProtectedClient.OpenAsync(clientRoot, peer, false, ct);
        using var transport = new HouseholdServiceTransport(peer, new ProtectedSessions(clientRoot, () =>
        { if (!File.Exists(Path.Combine(clientRoot, "client.marker"))) throw new IOException("Original native recovered client required."); }));
        var identity = await transport.IdentityAsync(ct); var journal = client.Services.GetRequiredService<HouseholdJournal>();
        var state = await journal.CaptureAsync(peer, identity.Id, ct) ?? throw new InvalidDataException();
        var coordinator = client.Services.GetRequiredService<HouseholdSyncCoordinator>();
        state = await coordinator.RefreshAuthorityAsync(transport, state.Authority, ct);
        using var beforeKeys = await journal.ReadKeysAsync(state.Authority, ct);
        var beforeRoot = beforeKeys.Owner?.Public ?? throw new InvalidDataException();
        var before = await transport.CheckpointAsync(state.SpaceId, state.DeviceId, ct);
        if (revoke)
        {
            var observed = await transport.RecoveryAsync(state.SpaceId, ct);
            state = await client.Services.GetRequiredService<HouseholdWorkspace>().RevokeRecoveryAsync(transport, state.Authority,
                observed.EnrollmentId, state.MembershipVersion, ct);
            using var afterKeys = await journal.ReadKeysAsync(state.Authority, ct);
            var afterRoot = afterKeys.Owner?.Public ?? throw new InvalidDataException();
            Require(beforeRoot.SigningKey != afterRoot.SigningKey && beforeRoot.WrappingKey != afterRoot.WrappingKey);
            var after = await transport.CheckpointAsync(state.SpaceId, state.DeviceId, ct);
            Require(after.Cursor == before.Cursor && after.PublicationHead == before.PublicationHead && after.MembershipVersion == before.MembershipVersion + 1);
            var refused = false;
            try { _ = await transport.RecoveryAsync(state.SpaceId, ct); }
            catch (Vafadar.Zanance.Data.Sync.PersonalSyncHttpException error) when (error.Status == System.Net.HttpStatusCode.NotFound) { refused = true; }
            Require(refused);
            await journal.RecordOwnerRootAsync(state.Authority, await transport.OwnerRootAsync(state.SpaceId, state.DeviceId, ct), ct);
        }
        var snapshot = await client.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
        Require(snapshot.Contacts.Count == 2 && snapshot.Costs.Count == 2 && snapshot.Allocations.Count == 4 && snapshot.Settlements.Count == 1 &&
            snapshot.Totals.Single().NetCost == 1001 && snapshot.Costs.Any(row => row.Title.EndsWith(" reviewed in original app", StringComparison.Ordinal)));
        proof(marker);
    }

    /// <summary>Uses only the separately exported kit and an actual fictitious identity session on a new protected client, never the old device/root private keys.</summary>
    internal static async Task RecoverAsync(string root, ProtectedClient source, HouseholdServiceTransport sourceTransport,
        Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("The new native owner recovery invariant failed."); }
        bool Done(string marker) => File.ReadLines(Path.Combine(root, "checkpoints.txt")).Contains(marker);
        const string completed = "native-new-device-kit-recovery-retires-old-owner-device-and-restores-complete-original-cost-finance";
        if (Done(completed)) throw new InvalidOperationException("Do not repeat completed owner recovery.");
        var peer = sourceTransport.Peer; var sourceJournal = source.Services.GetRequiredService<HouseholdJournal>();
        var sourceIdentity = await sourceTransport.IdentityAsync(ct); var original = await sourceJournal.CaptureAsync(peer, sourceIdentity.Id, ct) ?? throw new InvalidDataException();
        const string originalSave = "original-app-cost-save-published-once-before-independent-owner-device-loss-recovery";
        if (!Done(originalSave))
        {
            Require((await source.Services.GetRequiredService<HouseholdSyncCoordinator>().SynchronizeAsync(sourceTransport, original.Authority, ct)).Applied);
            proof(originalSave);
        }
        var recoveredRoot = Path.Combine(root, "recovered-owner"); var fresh = !Directory.Exists(recoveredRoot);
        using var recovered = await ProtectedClient.OpenAsync(recoveredRoot, peer, fresh, ct);
        recovered.Services.MigrateLocalDatabase<Vafadar.Zanance.Data.ZananceDbContext>();
        void Validate() { if (!File.Exists(Path.Combine(recoveredRoot, "client.marker"))) throw new IOException("Original recovered fixture required."); }
        var sessions = new ProtectedSessions(recoveredRoot, Validate);
        if (fresh)
        {
            var originalSessions = new ProtectedSessions(Path.Combine(root, "client"), () =>
            { if (!File.Exists(Path.Combine(root, "client", "client.marker"))) throw new IOException("Original fictional session owner required."); });
            var session = await originalSessions.ReadAsync(peer.BindingKey("session"), ct) ?? throw new InvalidDataException();
            await sessions.WriteAsync(peer.BindingKey("session"), session, ct);
        }
        using var transport = new HouseholdServiceTransport(peer, sessions);
        var encrypted = await File.ReadAllBytesAsync(Path.Combine(root, "fictitious-household-recovery.zhr"), ct);
        var wrapped = await File.ReadAllBytesAsync(Path.Combine(root, "recovery-test-password.dpapi"), ct);
        var password = ProtectedData.Unprotect(wrapped, null, DataProtectionScope.CurrentUser);
        try
        {
            using var kit = HouseholdRecoveryKit.ImportEncrypted(encrypted, Encoding.UTF8.GetString(password));
            var workspace = recovered.Services.GetRequiredService<HouseholdWorkspace>();
            var state = await workspace.BeginOwnerRecoveryAsync(transport, kit, ct);
            state = await workspace.CompleteOwnerRecoveryAsync(transport, state.Authority, "Fictitious recovered household", ct);
            var after = await transport.CheckpointAsync(state.SpaceId, state.DeviceId, ct);
            Require(after.OwnerId == sourceIdentity.Id && state.DeviceId != original.DeviceId);
            var costs = await recovered.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
            Require(costs.Contacts.Count == 2 && costs.Costs.Count == 2 && costs.Allocations.Count == 4 && costs.Settlements.Count == 1 &&
                costs.Totals.Single().NetCost == 1001 && costs.Costs.Any(row => row.Title.EndsWith(" reviewed in original app", StringComparison.Ordinal)));
            await using (var db = await recovered.Services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<Vafadar.Zanance.Data.ZananceDbContext>>().CreateDbContextAsync(ct))
                Require(!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Entries, ct));
            var refused = false;
            try { _ = await sourceTransport.CheckpointAsync(original.SpaceId, original.DeviceId, ct); }
            catch (Vafadar.Zanance.Data.Sync.PersonalSyncHttpException error) when (error.Status == System.Net.HttpStatusCode.NotFound) { refused = true; }
            Require(refused);
            using var keys = await recovered.Services.GetRequiredService<HouseholdJournal>().ReadKeysAsync(state.Authority, ct);
            var ownerRoot = keys.Owner ?? throw new InvalidDataException();
            await File.WriteAllTextAsync(Path.Combine(root, "recovered-owner-public-proof.json"), JsonSerializer.Serialize(new
            { state.SpaceId, state.DeviceId, state.MembershipVersion, after.KeyEpoch, after.Cursor, RootFingerprint = ownerRoot.Public.Fingerprint }), ct);
            proof(completed);
        }
        finally { CryptographicOperations.ZeroMemory(encrypted); CryptographicOperations.ZeroMemory(wrapped); CryptographicOperations.ZeroMemory(password); }
    }

    internal static async Task EnrollAsync(string root, ProtectedClient client, HouseholdServiceTransport transport,
        Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("The retained native recovery invariant failed."); }
        const string completed = "native-original-kit-root-rotation-complete-with-same-ciphertext-prefix-and-unchanged-financial-history";
        if (File.ReadLines(Path.Combine(root, "checkpoints.txt")).Contains(completed)) throw new InvalidOperationException("Do not repeat completed enrollment.");
        var workspace = client.Services.GetRequiredService<HouseholdWorkspace>(); var journal = client.Services.GetRequiredService<HouseholdJournal>();
        var identity = await transport.IdentityAsync(ct); var state = await journal.CaptureAsync(transport.Peer, identity.Id, ct) ?? throw new InvalidDataException();
        var file = Path.Combine(root, "fictitious-household-recovery.zhr"); var passwordFile = Path.Combine(root, "recovery-test-password.dpapi");
        if (!File.Exists(file))
        {
            if (File.Exists(passwordFile)) throw new IOException("Preserve the retained partial native export for explicit continuation.");
            var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
            using var export = await workspace.ExportRecoveryKitAsync(transport, state.Authority, password, ct);
            var clear = Encoding.UTF8.GetBytes(password); var wrapped = ProtectedData.Protect(clear, null, DataProtectionScope.CurrentUser);
            try
            {
                await using (var stream = new FileStream(passwordFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { await stream.WriteAsync(wrapped, ct); stream.Flush(true); }
                await using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { await stream.WriteAsync(export.Content, ct); stream.Flush(true); }
                await workspace.ConfirmRecoveryExportAsync(transport, state.Authority, export.Fingerprint, ct);
                proof("native-original-password-encrypted-independent-kit-export-confirmed-before-any-membership-change");
            }
            finally { password = ""; CryptographicOperations.ZeroMemory(clear); CryptographicOperations.ZeroMemory(wrapped); }
        }
        var before = await transport.CheckpointAsync(state.SpaceId, state.DeviceId, ct);
        state = await workspace.CompleteRecoveryEnrollmentAsync(transport, state.Authority, ct);
        var after = await transport.CheckpointAsync(state.SpaceId, state.DeviceId, ct);
        Require(after.Cursor == before.Cursor && after.PublicationHead == before.PublicationHead && after.MembershipVersion == before.MembershipVersion + 1);
        var encrypted = await File.ReadAllBytesAsync(file, ct); var protectedPassword = await File.ReadAllBytesAsync(passwordFile, ct);
        var bytes = ProtectedData.Unprotect(protectedPassword, null, DataProtectionScope.CurrentUser);
        try
        {
            using var kit = HouseholdRecoveryKit.ImportEncrypted(encrypted, Encoding.UTF8.GetString(bytes));
            var observed = await transport.RecoveryAsync(state.SpaceId, ct); var proofs = new List<HouseholdMembershipProof>();
            while (proofs.Count < after.MembershipVersion)
            { var page = await transport.RecoveryHistoryAsync(state.SpaceId, proofs.Count, ct); Require(page.Length is > 0 and <= 8); proofs.AddRange(page); }
            var trusted = HouseholdTrustChain.RecoveryHistory(kit, proofs, observed, identity.Id);
            using var rootKey = HouseholdRecoveryMaterial.OpenRoot(new(TimeProvider.System), state.SpaceId, kit.EnrollmentId,
                trusted.States[checked((int)observed.MembershipVersion - 1)].Epoch, observed.RootCiphertext, trusted.Current.Root, kit);
            Require(rootKey.Public == trusted.Current.Root);
            long cursor = 0;
            while (cursor < observed.ThroughEpoch)
            {
                var page = await transport.RecoveryEpochsAsync(state.SpaceId, kit.EnrollmentId, cursor, observed.ThroughEpoch, ct);
                Require(page.EnrollmentId == kit.EnrollmentId && page.Epochs.Length is > 0 and <= HouseholdRecoveryBounds.PageSize);
                foreach (var grant in page.Epochs)
                {
                    Require(grant.Epoch == cursor + 1);
                    var granting = trusted.States.Single(row => row.Version == grant.MembershipVersion).Snapshot();
                    var origin = trusted.States.Single(row => row.Epoch == grant.Epoch).Snapshot();
                    using var recovered = HouseholdRecoveryMaterial.OpenEpoch(new(TimeProvider.System), granting, origin, grant, kit);
                    using var original = await journal.ReadEpochAsync(state.Authority, grant.Epoch, ct);
                    Require(recovered.KeyCommitment == original.KeyCommitment && recovered.RecordAddress("Entries", "fictitious") == original.RecordAddress("Entries", "fictitious"));
                    cursor = grant.Epoch;
                }
                Require(page.NextEpoch == cursor && page.Complete == (cursor == observed.ThroughEpoch));
            }
            var snapshot = await client.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
            Require(snapshot.Contacts.Count == 2 && snapshot.Costs.Count == 2 && snapshot.Allocations.Count == 4 && snapshot.Settlements.Count == 1 && snapshot.Totals.Single().NetCost == 1001);
            await File.WriteAllTextAsync(Path.Combine(root, "recovery-enrollment-public-proof.json"), JsonSerializer.Serialize(new
            { observed.EnrollmentId, MembershipVersion = after.MembershipVersion, Epoch = after.KeyEpoch, after.Cursor,
                RootFingerprint = trusted.Current.Root.Fingerprint, KitFingerprint = kit.Public.Fingerprint }), ct);
            proof(completed);
        }
        finally { CryptographicOperations.ZeroMemory(encrypted); CryptographicOperations.ZeroMemory(protectedPassword); CryptographicOperations.ZeroMemory(bytes); }
    }
}
