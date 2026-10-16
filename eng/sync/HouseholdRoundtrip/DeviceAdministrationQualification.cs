using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>Original pending-device preservation, owner approval, delegated-root retirement and exact lost-response continuation.</summary>
internal static class DeviceAdministrationQualification
{
    /// <summary>Uses only the retained original cost owner; the second fictional device has its independent existing private key.</summary>
    internal static async Task RunAsync(string root, LocalSyncPeer peer, string mode, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("The native owner-device administration invariant failed."); }
        var progressPath = Path.Combine(root, "owner-device-administration.json");
        var progress = File.Exists(progressPath) ? JsonSerializer.Deserialize<DeviceProgress>(await File.ReadAllTextAsync(progressPath, ct)) ?? throw new IOException() : null;
        if (progress?.Phase == 4 && mode != "device-admin-cold" || progress?.Phase == 5) throw new IOException("Do not replay completed device administration.");
        var ownerRoot = Path.Combine(root, "recovered-owner"); using var owner = await ProtectedClient.OpenAsync(ownerRoot, peer, false, ct);
        using var transport = new HouseholdServiceTransport(peer, new ProtectedSessions(ownerRoot, () =>
        { if (File.ReadAllText(Path.Combine(ownerRoot, "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); }));
        var identity = await transport.IdentityAsync(ct); var journal = owner.Services.GetRequiredService<HouseholdJournal>();
        var workspace = owner.Services.GetRequiredService<HouseholdWorkspace>(); var coordinator = owner.Services.GetRequiredService<HouseholdSyncCoordinator>();
        var state = await journal.CaptureAsync(peer, identity.Id, ct) ?? throw new IOException();
        var keyPath = Path.Combine(root, "owner-second-device.protected");
        if (progress is null)
        {
            if (File.Exists(keyPath)) throw new IOException("Retain the incomplete original private device frame for explicit continuation.");
            using var fresh = SigningRecipientIdentity.Create(); var frame = fresh.ExportForProtectedStorage();
            var wrapped = ProtectedData.Protect(frame, null, DataProtectionScope.CurrentUser);
            try { await File.WriteAllBytesAsync(keyPath, wrapped, ct); }
            finally { CryptographicOperations.ZeroMemory(frame); CryptographicOperations.ZeroMemory(wrapped); }
            progress = new(0, Guid.Empty, null, null, null, 0);
            await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(progress), ct);
        }
        var privateBytes = await File.ReadAllBytesAsync(keyPath, ct); byte[]? privateClear = null; SigningRecipientIdentity device;
        try { privateClear = ProtectedData.Unprotect(privateBytes, null, DataProtectionScope.CurrentUser); device = SigningRecipientIdentity.ImportFromProtectedStorage(privateClear); }
        finally { CryptographicOperations.ZeroMemory(privateBytes); if (privateClear is not null) CryptographicOperations.ZeroMemory(privateClear); }
        using var originalDevice = device;
        async Task SaveAsync(int phase) { progress = progress! with { Phase = phase }; await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(progress), ct); }
        var kitFile = Path.Combine(root, "owner-device-retirement-kit.zhr"); var passwordFile = Path.Combine(root, "owner-device-retirement-kit-password.protected");
        if (progress.Phase == 0)
        {
            state = await coordinator.RefreshAuthorityAsync(transport, state.Authority, ct);
            if (!File.Exists(kitFile))
            {
                if (File.Exists(passwordFile)) throw new IOException("Retain the original incomplete device-retirement kit export.");
                var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
                using var export = await workspace.ExportRecoveryKitAsync(transport, state.Authority, password, ct);
                var clear = Encoding.UTF8.GetBytes(password); var wrapped = ProtectedData.Protect(clear, null, DataProtectionScope.CurrentUser);
                try { await File.WriteAllBytesAsync(passwordFile, wrapped, ct); await File.WriteAllBytesAsync(kitFile, export.Content, ct); }
                finally { password = ""; CryptographicOperations.ZeroMemory(clear); CryptographicOperations.ZeroMemory(wrapped); }
                await workspace.ConfirmRecoveryExportAsync(transport, state.Authority, export.Fingerprint, ct);
            }
            state = await workspace.CompleteRecoveryEnrollmentAsync(transport, state.Authority, ct);
            var session = await transport.SessionAsync(ct);
            var enrollment = new HouseholdDeviceSessionStatement(1, state.SpaceId, Guid.Empty, Guid.NewGuid(), identity.Id, session.SessionId,
                new(device.Public.SigningKey, device.Public.WrappingKey), DateTimeOffset.UtcNow.AddMinutes(12).ToUnixTimeSeconds());
            var signed = StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(enrollment, HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement),
                device, "zanance-household-device-enrollment+jwt");
            progress = progress with { SignedEnrollment = signed }; await SaveAsync(1);
        }
        if (progress.Phase == 1)
        {
            var observed = await transport.EnrollDeviceAsync(state.SpaceId, new(new(device.Public.SigningKey, device.Public.WrappingKey), progress.SignedEnrollment!), ct);
            Require(!observed.Approved && !observed.Revoked && observed.Fingerprint == device.Public.Fingerprint && observed.IdentityId == identity.Id);
            progress = progress with { DeviceId = observed.DeviceId, EnrollmentMembershipVersion = state.MembershipVersion }; await SaveAsync(2);
            proof("original-second-owner-device-key-self-proven-and-retained-before-independent-owner-approval");
        }
        if (progress.Phase == 2)
        {
            state = await coordinator.RefreshAuthorityAsync(transport, state.Authority, ct);
            var roster = (await journal.ReadCurrentMembershipAsync(state.Authority, ct)).Snapshot();
            var other = roster.Members.Single(member => member.Role != HouseholdRole.Owner);
            var enrollmentVersion = progress.EnrollmentMembershipVersion;
            if (enrollmentVersion == 0)
            {
                // Continue the original assertion failure after its one committed role change.
                // The active certificate still pins the exact earlier kit-enrollment membership.
                enrollmentVersion = (await transport.RecoveryAsync(state.SpaceId, ct)).MembershipVersion;
                progress = progress with { EnrollmentMembershipVersion = enrollmentVersion }; await SaveAsync(2);
            }
            if (roster.Version == enrollmentVersion)
                await workspace.ChangeMemberAsync(transport, state.Authority, roster.Version, other.IdentityId,
                    other.Role == HouseholdRole.Editor ? HouseholdRole.Viewer : HouseholdRole.Editor, ct);
            else Require(roster.Version == enrollmentVersion + 1 && roster.Change == HouseholdMembershipChange.ChangeRole);
            state = await coordinator.RefreshAuthorityAsync(transport, state.Authority, ct);
            var pending = (await transport.PendingDevicesAsync(state.SpaceId, state.DeviceId, ct)).Single(row => row.DeviceId == progress.DeviceId);
            Require(pending.SignedPossession == progress.SignedEnrollment);
            var wrongFingerprint = false;
            try { await workspace.ApproveDeviceAsync(transport, state.Authority, state.MembershipVersion, pending, new('0', 64), ct); }
            catch (InvalidOperationException) { wrongFingerprint = true; }
            Require(wrongFingerprint && (await transport.CheckpointAsync(state.SpaceId, state.DeviceId, ct)).MembershipVersion == state.MembershipVersion);
            await workspace.ApproveDeviceAsync(transport, state.Authority, state.MembershipVersion, pending, device.Public.Fingerprint, ct);
            state = await coordinator.RefreshAuthorityAsync(transport, state.Authority, ct);
            var current = await journal.ReadCurrentMembershipAsync(state.Authority, ct);
            var grant = await transport.OwnerRootAsync(state.SpaceId, progress.DeviceId, ct);
            using var delegated = HouseholdRecoveryMaterial.OpenOwnerRoot(new(TimeProvider.System), current, current, grant, progress.DeviceId, device);
            Require(delegated.Public == current.Root && current.Snapshot().Members.Single(member => member.Role == HouseholdRole.Owner).Devices.Length == 2);
            var checkpoint = await transport.CheckpointAsync(state.SpaceId, state.DeviceId, ct);
            progress = progress with { PreviousRoot = new(current.Root.SigningKey, current.Root.WrappingKey), Delegation = grant, Cursor = checkpoint.Cursor };
            await SaveAsync(3); proof("pending-device-survives-unrelated-membership-change-and-original-fingerprint-approval-delivers-real-private-owner-root");
        }
        if (progress.Phase == 3)
        {
            if (await journal.ReadMembershipProposalAsync(state.Authority, ct) is not null)
                state = await workspace.ResumePendingAdministrationAsync(transport, state.Authority, ct);
            else
            {
                state = await coordinator.RefreshAuthorityAsync(transport, state.Authority, ct);
                if ((await journal.ReadCurrentMembershipAsync(state.Authority, ct)).Snapshot().Members.Single(member => member.Role == HouseholdRole.Owner).Devices.Any(row => row.Id == progress.DeviceId))
                {
                    await File.WriteAllTextAsync(Path.Combine(root, "owner-device-revoke-submitted.marker"), "Original owner-device retirement request v1", ct);
                    state = await workspace.RevokeDeviceAsync(transport, state.Authority, state.MembershipVersion, identity.Id, progress.DeviceId, device.Public.Fingerprint, ct);
                }
            }
            await SaveAsync(4);
        }
        state = await coordinator.RefreshAuthorityAsync(transport, state.Authority, ct);
        var after = await journal.ReadCurrentMembershipAsync(state.Authority, ct); var checkpointAfter = await transport.CheckpointAsync(state.SpaceId, state.DeviceId, ct);
        Require(after.Root.SigningKey != progress.PreviousRoot!.SigningKey && after.Root.WrappingKey != progress.PreviousRoot.WrappingKey &&
            checkpointAfter.Cursor == progress.Cursor && !after.Snapshot().Members.SelectMany(member => member.Devices).Any(row => row.Id == progress.DeviceId));
        foreach (var operation in new Func<Task>[] {
            async () => { _ = await transport.CheckpointAsync(state.SpaceId, progress.DeviceId, ct); },
            async () => { _ = await transport.OwnerRootAsync(state.SpaceId, progress.DeviceId, ct); },
            async () => { _ = await transport.GrantAsync(state.SpaceId, progress.DeviceId, after.Epoch, ct); } })
        {
            var refused = false; try { await operation(); } catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.NotFound) { refused = true; }
            Require(refused);
        }
        var encryptedKit = await File.ReadAllBytesAsync(kitFile, ct); var wrappedPassword = await File.ReadAllBytesAsync(passwordFile, ct); byte[]? passwordBytes = null;
        try
        {
            passwordBytes = ProtectedData.Unprotect(wrappedPassword, null, DataProtectionScope.CurrentUser);
            using var kit = HouseholdRecoveryKit.ImportEncrypted(encryptedKit, Encoding.UTF8.GetString(passwordBytes));
            var recovery = await transport.RecoveryAsync(state.SpaceId, ct); var history = await journal.ReadHistoryAsync(state.Authority, ct);
            var trusted = HouseholdTrustChain.RecoveryHistory(kit, history, recovery, identity.Id);
            using var recoveredRoot = HouseholdRecoveryMaterial.OpenRoot(new(TimeProvider.System), state.SpaceId, kit.EnrollmentId, after.Epoch, recovery.RootCiphertext, trusted.Current.Root, kit);
            Require(recoveredRoot.Public == after.Root);
            var last = await transport.RecoveryEpochsAsync(state.SpaceId, kit.EnrollmentId, after.Epoch - 1, after.Epoch, ct);
            Require(last.Epochs.Length == 1);
            using var archived = HouseholdRecoveryMaterial.OpenEpoch(new(TimeProvider.System), after.Snapshot(), after.Snapshot(), last.Epochs[0], kit);
            Require(archived.Epoch == after.Epoch);
        }
        finally { CryptographicOperations.ZeroMemory(encryptedKit); CryptographicOperations.ZeroMemory(wrappedPassword); if (passwordBytes is not null) CryptographicOperations.ZeroMemory(passwordBytes); }
        var costs = await owner.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
        Require(costs.CanWrite && costs.Costs.Count == 2 && costs.Allocations.Count == 4 && costs.Settlements.Count == 1 && costs.Totals.Single().NetCost == 1001);
        if (mode == "device-admin-cold") await SaveAsync(5);
        proof(mode == "device-admin-cold" ? "cold-original-owner-device-retirement-and-updated-kit-root-epoch-preserve-finance-and-deny-revoked-device-resources"
            : "original-owner-device-retirement-rotates-both-root-roles-and-kit-current-root-epoch-with-same-finance-and-no-new-request-on-resume");
    }

    private sealed record DeviceProgress(int Phase, Guid DeviceId, string? SignedEnrollment, HouseholdPublicKeys? PreviousRoot,
        HouseholdOwnerRootGrant? Delegation, long Cursor, long EnrollmentMembershipVersion = 0);
}
