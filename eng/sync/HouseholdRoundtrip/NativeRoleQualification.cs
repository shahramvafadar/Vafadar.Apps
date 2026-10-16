using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Cryptography;
using Vafadar.Data;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>Real accepted roles against independent DPAPI/SQLCipher clients and the original HTTPS/PostgreSQL service.</summary>
internal static class NativeRoleQualification
{
    /// <summary>Continues only the remaining real host-expiry/grace phase with the same original owner ledger.</summary>
    internal static async Task GraceAsync(IServiceProvider service, ProtectedClient owner, HouseholdSyncCoordinator coordinator,
        HouseholdServiceTransport transport, HouseholdJournalState state, Action<string> proof, CancellationToken ct)
    {
        long before;
        await using (var scope = service.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            var room = await db.Set<Vafadar.Zanance.Api.Sharing.HouseholdSpaceRow>().SingleAsync(row => row.Id == state.SpaceId, ct);
            before = room.Cursor; room.HostExpiresAt = TimeProvider.System.GetUtcNow().AddMinutes(-1); await db.SaveChangesAsync(ct);
        }
        Require((await transport.CheckpointAsync(state.SpaceId, state.DeviceId, ct)).Retention == HouseholdRetentionState.ReadOnlyGrace);
        Require((await coordinator.SynchronizeAsync(transport, state.Authority, ct)).Applied);
        var blocked = false;
        try { await owner.Store.SaveAccountAsync(new() { Name = "Refused grace owner", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) }, ct); }
        catch (HouseholdWriteRejectedException error) { blocked = error.Reason == HouseholdWriteBlockReason.Expired; }
        Require(blocked && (await owner.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1200);
        var settings = await owner.Store.GetSettingsAsync(ct); var financialValue = settings.MonthStartDay;
        settings.MonthStartDay = financialValue == 7 ? 8 : 7; blocked = false;
        try { owner.Store.SaveSettings(settings); }
        catch (HouseholdWriteRejectedException error) { blocked = error.Reason == HouseholdWriteBlockReason.Expired; }
        Require(blocked && (await owner.Store.GetSettingsAsync(ct)).MonthStartDay == financialValue);
        var factory = owner.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            blocked = false;
            try { await db.Database.ExecuteSqlRawAsync("UPDATE Settings SET EssentialEstimate=19", ct); }
            catch (Microsoft.Data.Sqlite.SqliteException) { blocked = true; }
            Require(blocked);
        }
        await using (var scope = service.CreateAsyncScope())
            Require((await scope.ServiceProvider.GetRequiredService<ServiceDbContext>().Set<Vafadar.Zanance.Api.Sharing.HouseholdSpaceRow>()
                .SingleAsync(row => row.Id == state.SpaceId, ct)).Cursor == before);
        proof("actual-host-expiry-grace-completes-original-read-only-financial-receive-with-no-remote-publication-and-refuses-owner-store-financial-settings-and-unarmed-SQL");
    }

    /// <summary>Qualifies current native write authority and read-only feed without modifying the retained large-stream fixture.</summary>
    internal static async Task RunAsync(string root, IServiceProvider service, LocalSyncPeer peer, ProtectedClient ownerClient,
        HouseholdJournal ownerJournal, HouseholdSyncCoordinator ownerCoordinator, HouseholdServiceTransport ownerTransport,
        HouseholdJournalState ownerState, SigningRecipientIdentity rootKey, HouseholdEpochMaterial firstEpoch,
        HouseholdMembershipStatement genesis, Action<string> proof, CancellationToken ct, bool revokeOnly = false)
    {
        var step = "recipient-enrollment";
        try
        {
            var recipientRoot = Path.Combine(root, "recipient"); var clock = new LocalClock();
            var fresh = !Directory.Exists(recipientRoot);
            using var recipient = await ProtectedClient.OpenAsync(recipientRoot, peer, fresh, ct, clock);
            using var transport = new HouseholdServiceTransport(peer, new ProtectedSessions(recipientRoot, () =>
            { if (!File.Exists(Path.Combine(recipientRoot, "client.marker"))) throw new IOException("Original recipient root required."); }));
            var checkpoint = await ownerTransport.CheckpointAsync(ownerState.SpaceId, ownerState.DeviceId, ct);
            HouseholdInvitationClaimResult claimResult = null!;
            var retainedIdentity = Directory.Exists(Path.Combine(recipientRoot, "keys")) && !fresh;
            if (retainedIdentity)
            {
                var actual = await transport.IdentityAsync(ct);
                var retainedJournal = new HouseholdJournal(recipient.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), clock);
                var retained = await retainedJournal.CaptureAsync(peer, actual.Id, ct) ?? throw new InvalidDataException();
                var retainedMembership = (await retainedJournal.ReadCurrentMembershipAsync(retained.Authority, ct)).Snapshot();
                using var retainedEpoch = await retainedJournal.ReadEpochAsync(retained.Authority, retainedMembership.KeyEpoch, ct);
                if (revokeOnly)
                {
                    await RevokeAsync(recipient, transport, retainedJournal, new(recipient.Sync, retainedJournal), retained, retainedMembership, retainedEpoch);
                    return;
                }
                await ContinueEditorAsync(recipient, transport, retainedJournal, new(recipient.Sync, retainedJournal), retained, clock, retainedMembership, retainedEpoch);
                return;
            }
            const string email = "household-native-roles-recipient@example.invalid";
            var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA!9";
            using (var http = LocalSyncHttp.CreateClient(peer, TimeProvider.System))
            {
                using var registration = await http.PostAsJsonAsync("identity/register", new IdentityInput(email, password), ct);
                Require(registration.StatusCode == HttpStatusCode.Accepted);
                await using var scope = service.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                var user = await db.Users.SingleAsync(row => row.Email == email, ct);
                var challenge = await db.Challenges.SingleAsync(row => row.UserId == user.Id && row.Purpose == "confirm", ct);
                var mail = JsonSerializer.Deserialize<PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new InvalidDataException();
                using var confirmation = await http.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), ct);
                Require(confirmation.StatusCode == HttpStatusCode.NoContent);
            }
            var identity = await transport.SignInAsync(email, password, ct); password = "";
            using var device = SigningRecipientIdentity.Create();
            var journal = new HouseholdJournal(recipient.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), clock);
            var coordinator = new HouseholdSyncCoordinator(recipient.Sync, journal);
            checkpoint = await ownerTransport.CheckpointAsync(ownerState.SpaceId, ownerState.DeviceId, ct);
            var capability = RandomNumberGenerator.GetBytes(32);
            string signedClaim; string signedInvitation;
            try
            {
                var invitation = new HouseholdInvitationStatement(1, ownerState.SpaceId, Guid.NewGuid(), Guid.NewGuid(),
                    checkpoint.MembershipVersion, checkpoint.MembershipHead, HouseholdRole.Viewer,
                    Convert.ToHexStringLower(SHA256.HashData(capability)), TimeProvider.System.GetUtcNow().AddHours(1).ToUnixTimeSeconds());
                signedInvitation = Sign(invitation, HouseholdMetadataJsonContext.Default.HouseholdInvitationStatement, rootKey, "zanance-household-invitation+jwt");
                _ = await ownerTransport.InviteAsync(ownerState.SpaceId, new(ownerState.DeviceId, signedInvitation), ct);
                var claim = new HouseholdInvitationClaimStatement(1, ownerState.SpaceId, invitation.InvitationId, Guid.NewGuid(), identity.Id,
                    HouseholdPublicationDigest.Ciphertext(signedInvitation), rootKey.Public.Fingerprint, Public(device));
                signedClaim = Sign(claim, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimStatement, device, "zanance-household-invitation-claim+jwt");
                claimResult = await transport.ClaimAsync(ownerState.SpaceId, invitation.InvitationId,
                    new(Convert.ToHexStringLower(capability), signedClaim, Public(device)), ct);
            }
            finally { CryptographicOperations.ZeroMemory(capability); }
            var draft = await journal.BeginDraftAsync(peer, identity.Id, rootKey.Public, false, device, null, signedInvitation, ct);
            await journal.BindRemoteAsync(draft.Authority, ownerState.SpaceId, claimResult.DeviceId, ct);
            using var secondEpoch = firstEpoch.Next();
            var current = genesis with { OperationId = Guid.NewGuid(), Change = HouseholdMembershipChange.AcceptMember,
                InvitationId = claimResult.InvitationId, ExpectedVersion = checkpoint.MembershipVersion, Version = checkpoint.MembershipVersion + 1,
                PreviousHash = checkpoint.MembershipHead, KeyEpoch = secondEpoch.Epoch, KeyCommitment = secondEpoch.KeyCommitment,
                AddressKeyCommitment = secondEpoch.AddressKeyCommitment,
                Members = [genesis.Members[0], new(identity.Id, HouseholdRole.Viewer, HouseholdPublicationDigest.Ciphertext(signedClaim),
                    [new(claimResult.DeviceId, Public(device))])] };
            await PublishAsync(current, secondEpoch, signedClaim, [firstEpoch]);
            await RefreshAsync(journal, transport, draft.Authority, claimResult.DeviceId);
            await RefreshAsync(ownerJournal, ownerTransport, ownerState.Authority, ownerState.DeviceId);
            step = "viewer-first-receive";
            await recipient.Sync.ConnectAsync(await recipient.Sync.ReviewAsync(ct), peer, identity.Id, claimResult.DeviceId,
                new(ownerState.SpaceId, "Fictitious native role household", HouseholdFinancialSchema.Hash, 0, false), ct,
                receiveFirstRun: true, transportKind: ProfileSynchronizationKind.Household);
            var account = new Account { Name = "Fictitious shared account", CurrencyCode = "EUR", OpeningBalance = 100_000, OpeningDate = new(2026, 10, 1) };
            await ownerClient.Store.SaveAccountAsync(account, ct);
            var entry = new LedgerEntry { AccountId = account.Id, Amount = 1200, Kind = EntryKind.Expense, Date = new(2026, 10, 16), Title = "Fictitious shared amount" };
            Require((await ownerClient.Store.SaveEntryAsync(entry, ct)).Succeeded);
            await ownerClient.Sync.ConnectAsync(await ownerClient.Sync.ReviewAsync(ct), peer, ownerState.Authority.IdentityId, ownerState.DeviceId,
                new(ownerState.SpaceId, "Fictitious native role household", HouseholdFinancialSchema.Hash, 0, true), ct, transportKind: ProfileSynchronizationKind.Household);
            Require((await ownerCoordinator.SynchronizeAsync(ownerTransport, ownerState.Authority, ct)).Applied);
            Require((await coordinator.SynchronizeAsync(transport, draft.Authority, ct)).Applied);
            Require((await recipient.Store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1200);
            await RefuseAsync(recipient, HouseholdWriteBlockReason.Viewer);
            var settings = await recipient.Store.GetSettingsAsync(ct); settings.ReviewReminderEnabled = !settings.ReviewReminderEnabled;
            recipient.Store.SaveSettings(settings);
            await using (var scope = service.CreateAsyncScope())
                Require(!await scope.ServiceProvider.GetRequiredService<ServiceDbContext>().Set<Vafadar.Zanance.Api.Sharing.HouseholdRoundRow>()
                    .AnyAsync(row => row.DeviceId == claimResult.DeviceId, ct));
            proof("actual-approved-viewer-receives-original-protected-finance-with-zero-server-write-rounds-and-cannot-write-through-store-or-direct-EF");

            await ContinueEditorAsync(recipient, transport, journal, coordinator, draft, clock, current, secondEpoch);

            async Task ContinueEditorAsync(ProtectedClient recipient, HouseholdServiceTransport transport, HouseholdJournal journal,
                HouseholdSyncCoordinator coordinator, HouseholdJournalState draft, LocalClock clock, HouseholdMembershipStatement current, HouseholdEpochMaterial secondEpoch)
            {
            step = "approved-editor-and-expiry";
            checkpoint = await ownerTransport.CheckpointAsync(ownerState.SpaceId, ownerState.DeviceId, ct);
            if (current.Version < checkpoint.MembershipVersion)
            {
                await RefreshAsync(journal, transport, draft.Authority, draft.DeviceId);
                current = (await journal.ReadCurrentMembershipAsync(draft.Authority, ct)).Snapshot();
            }
            if (current.Members[1].Role != HouseholdRole.Editor)
            {
                using var thirdEpoch = secondEpoch.Next();
                current = current with { OperationId = Guid.NewGuid(), Change = HouseholdMembershipChange.ChangeRole, InvitationId = null,
                    ExpectedVersion = current.Version, Version = current.Version + 1, PreviousHash = checkpoint.MembershipHead,
                    PublicationCursor = checkpoint.Cursor, PublicationHead = checkpoint.PublicationHead,
                    ValidUntilUnixSeconds = TimeProvider.System.GetUtcNow().AddMinutes(10).ToUnixTimeSeconds(),
                    KeyEpoch = thirdEpoch.Epoch, KeyCommitment = thirdEpoch.KeyCommitment, AddressKeyCommitment = thirdEpoch.AddressKeyCommitment,
                    Members = [current.Members[0], current.Members[1] with { Role = HouseholdRole.Editor }] };
                await ownerJournal.StageEpochAsync(ownerState.Authority, current.OperationId, thirdEpoch, ct);
                await PublishAsync(current, thirdEpoch, null, []);
                await RefreshAsync(journal, transport, draft.Authority, draft.DeviceId);
            }
            var added = new Account { Name = "Fictitious approved editor", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) };
            await recipient.Store.SaveAccountAsync(added, ct);
            Require((await coordinator.SynchronizeAsync(transport, draft.Authority, ct)).Applied);
            Require((await ownerCoordinator.SynchronizeAsync(ownerTransport, ownerState.Authority, ct)).Applied);
            proof("actual-owner-approved-editor-rotation-permits-native-write-and-original-encrypted-publication");

            var expiry = TimeProvider.System.GetUtcNow().AddMinutes(2);
            await using (var scope = service.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                var room = await db.Set<Vafadar.Zanance.Api.Sharing.HouseholdSpaceRow>().SingleAsync(row => row.Id == ownerState.SpaceId, ct);
                room.HostExpiresAt = expiry; await db.SaveChangesAsync(ct);
            }
            await RefreshAsync(journal, transport, draft.Authority, draft.DeviceId);
            var factory = recipient.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            await using (var db = await factory.CreateDbContextAsync(ct))
            await using (var tx = await db.Database.BeginTransactionAsync(ct))
            {
                var row = await db.Accounts.SingleAsync(row => row.Id == added.Id, ct); row.Name = "Must roll back at expiry";
                await db.SaveChangesAsync(ct); clock.Now = expiry.AddSeconds(1);
                var refused = false;
                try { await tx.CommitAsync(ct); } catch (HouseholdWriteRejectedException error) { refused = error.Reason == HouseholdWriteBlockReason.Expired; }
                Require(refused);
            }
            await RefuseAsync(recipient, HouseholdWriteBlockReason.Expired);
            clock.Now = null;
            await using (var db = await factory.CreateDbContextAsync(ct))
                Require((await db.Accounts.SingleAsync(row => row.Id == added.Id, ct)).Name == added.Name);
            clock.Now = TimeProvider.System.GetUtcNow().AddMinutes(-6); await RefuseAsync(recipient, HouseholdWriteBlockReason.Clock); clock.Now = null;
            proof("actual-borrowed-native-transaction-cannot-commit-after-expiry-and-clock-rollback-cannot-authorize-new-writes");

            }
            async Task RevokeAsync(ProtectedClient target, HouseholdServiceTransport channel, HouseholdJournal targetJournal,
                HouseholdSyncCoordinator targetCoordinator, HouseholdJournalState retained, HouseholdMembershipStatement current, HouseholdEpochMaterial priorEpoch)
            {
                step = "actual-scoped-revocation-with-original-pending-ciphertext";
                await using (var scope = service.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                    var room = await db.Set<Vafadar.Zanance.Api.Sharing.HouseholdSpaceRow>().SingleAsync(row => row.Id == ownerState.SpaceId, ct);
                    room.HostExpiresAt = TimeProvider.System.GetUtcNow().AddMinutes(10); await db.SaveChangesAsync(ct);
                }
                await RefreshAsync(targetJournal, channel, retained.Authority, retained.DeviceId);
                var account = new Account { Name = "Fictitious retained unpublished editor change", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) };
                await target.Store.SaveAccountAsync(account, ct);
                var pending = (await targetJournal.PrepareOriginalAsync(retained.Authority, peer, target.Sync, ct)).Attempt ?? throw new InvalidDataException();
                Require(pending.Parts.Count != 0 && !pending.Committed && pending.RemoteId is null);
                var digest = await Vafadar.Zanance.HouseholdQualification.RetainedCiphertext.HashAsync(targetJournal, retained.Authority, pending, ct);
                checkpoint = await ownerTransport.CheckpointAsync(ownerState.SpaceId, ownerState.DeviceId, ct);
                using var rotated = priorEpoch.Next();
                var next = current with { OperationId = Guid.NewGuid(), Change = HouseholdMembershipChange.RevokeMember, InvitationId = null,
                    ExpectedVersion = current.Version, Version = current.Version + 1, PreviousHash = checkpoint.MembershipHead,
                    PublicationCursor = checkpoint.Cursor, PublicationHead = checkpoint.PublicationHead,
                    ValidUntilUnixSeconds = TimeProvider.System.GetUtcNow().AddMinutes(10).ToUnixTimeSeconds(),
                    KeyEpoch = rotated.Epoch, KeyCommitment = rotated.KeyCommitment, AddressKeyCommitment = rotated.AddressKeyCommitment,
                    Members = [current.Members.Single(member => member.Role == HouseholdRole.Owner)] };
                await ownerJournal.StageEpochAsync(ownerState.Authority, next.OperationId, rotated, ct);
                var body = rotated.Grant(new(TimeProvider.System), next, rootKey, next.Members[0].Devices[0]);
                var proposal = new HouseholdPublishMembership(ownerState.DeviceId,
                    Sign(next, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement, rootKey, "zanance-household-membership+jwt"), null, null,
                    [new(rotated.Epoch, ownerState.DeviceId, body, HouseholdPublicationDigest.Ciphertext(body))]);
                await ownerJournal.StageMembershipProposalAsync(ownerState.Authority, proposal, ct);
                _ = await ownerTransport.PublishMembershipAsync(ownerState.SpaceId, proposal, ct);
                await RefreshAsync(ownerJournal, ownerTransport, ownerState.Authority, ownerState.DeviceId);
                await ownerJournal.PromoteStagedEpochAsync(ownerState.Authority, next.OperationId, ct);
                await ownerJournal.CompleteMembershipProposalAsync(ownerState.Authority, ct);
                var refused = false;
                try { await targetCoordinator.SynchronizeAsync(channel, retained.Authority, ct); }
                catch (PersonalSyncHttpException error) { refused = error.Status == HttpStatusCode.NotFound; }
                Require(refused);
                await RefuseAsync(target, HouseholdWriteBlockReason.RemoteRefused);
                var unchanged = await targetJournal.ReadAttemptAsync(retained.Authority, ct) ?? throw new InvalidDataException();
                Require(unchanged.Open.OperationId == pending.Open.OperationId && unchanged.Open.AttemptId == pending.Open.AttemptId &&
                    unchanged.Cutoff == pending.Cutoff && !unchanged.Committed &&
                    await Vafadar.Zanance.HouseholdQualification.RetainedCiphertext.HashAsync(targetJournal, retained.Authority, unchanged, ct) == digest);
                await using (var db = await target.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct))
                    Require(await db.Accounts.AnyAsync(row => row.Id == account.Id, ct));
                proof("actual-authenticated-revoked-device-404-closes-local-write-authority-and-retains-exact-unpublished-ciphertext-operation-cutoff-and-finance");
            }
            async Task PublishAsync(HouseholdMembershipStatement statement, HouseholdEpochMaterial epoch, string? acceptance, HouseholdEpochMaterial[] historical)
            {
                var grants = statement.Members.SelectMany(member => member.Devices).Select(deviceInfo =>
                { var body = epoch.Grant(new(TimeProvider.System), statement, rootKey, deviceInfo); return new HouseholdEncryptedGrant(epoch.Epoch, deviceInfo.Id, body, HouseholdPublicationDigest.Ciphertext(body)); }).ToList();
                foreach (var old in historical)
                {
                    var body = old.GrantHistorical(new(TimeProvider.System), statement, genesis, rootKey, statement.Members[1].Devices[0]);
                    grants.Add(new(old.Epoch, claimResult.DeviceId, body, HouseholdPublicationDigest.Ciphertext(body)));
                }
                var signed = Sign(statement, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement, rootKey, "zanance-household-membership+jwt");
                checkpoint = await ownerTransport.PublishMembershipAsync(ownerState.SpaceId, new(ownerState.DeviceId, signed, null, acceptance, grants.ToArray()), ct);
            }
            async Task RefreshAsync(HouseholdJournal target, HouseholdServiceTransport channel, HouseholdJournalAuthority authority, Guid deviceId)
            {
                _ = await target.RecordMembershipAsync(authority, await channel.MembershipAsync(ownerState.SpaceId, deviceId, 0, ct), ct);
                foreach (var epoch in await target.MissingEpochsAsync(authority, ct))
                    await target.RecordEpochGrantAsync(authority, await channel.GrantAsync(ownerState.SpaceId, deviceId, epoch, ct), ct);
                await target.RecordAccessCheckpointAsync(authority, await channel.CheckpointAsync(ownerState.SpaceId, deviceId, ct), ct);
            }
            async Task RefuseAsync(ProtectedClient target, HouseholdWriteBlockReason reason)
            {
                var storeRefused = false; var efRefused = false;
                try { await target.Store.SaveAccountAsync(new() { Name = "Refused fictional account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) }, ct); }
                catch (HouseholdWriteRejectedException error) { storeRefused = error.Reason == reason; }
                await using var db = await target.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
                db.Accounts.Add(new() { Name = "Refused direct fictional account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) });
                try { await db.SaveChangesAsync(ct); } catch (HouseholdWriteRejectedException error) { efRefused = error.Reason == reason; }
                Require(storeRefused && efRefused);
            }
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("NATIVE-ROLE-STAGE " + step);
            Console.Error.WriteLine(string.Join(" > ", new System.Diagnostics.StackTrace(error, true).GetFrames()
                .Where(frame => frame.GetMethod()?.DeclaringType?.FullName?.Contains(nameof(NativeRoleQualification), StringComparison.Ordinal) == true)
                .Select(frame => frame.GetMethod()!.Name + ":" + frame.GetFileLineNumber())));
            throw;
        }
    }

    private static void Require(bool condition) { if (!condition) throw new InvalidDataException("The actual native role invariant failed."); }
    private static HouseholdPublicKeys Public(SigningRecipientIdentity identity) => new(identity.Public.SigningKey, identity.Public.WrappingKey);
    private static string Sign<T>(T value, JsonTypeInfo<T> json, SigningRecipientIdentity identity, string type) =>
        StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(value, json), identity, type);
    /// <summary>Only the independent client's injected clock changes; no device/system setting is touched.</summary>
    private sealed class LocalClock : TimeProvider
    {
        /// <summary>Optional independent fixture time; null follows the unchanged system clock.</summary>
        internal DateTimeOffset? Now { get; set; }
        public override DateTimeOffset GetUtcNow() => Now ?? TimeProvider.System.GetUtcNow();
    }
}
