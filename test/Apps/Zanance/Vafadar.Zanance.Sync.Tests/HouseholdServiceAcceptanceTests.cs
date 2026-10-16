using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vafadar.Cryptography;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Sharing;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Sync.Tests;

/// <summary>Original HTTPS endpoints/PostgreSQL with independent real signing/recipient clients; no household financial/UI acceptance is inferred.</summary>
[Trait("AT", "AT-294")]
public sealed partial class HouseholdServiceAcceptanceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly string Zero = new('0', 64);

    /// <summary>Actual transfer requires old-root consent plus new-root acceptance; removed old owner cannot retain future financial/key authority.</summary>
    [Fact]
    public async Task Actual_ownership_transfer_requires_independent_new_root_consent_and_preserves_the_next_signed_generation()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_TEST_DB") ?? throw new InvalidOperationException("An isolated household database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_TRANSFER_TEST_ROOT") ?? throw new InvalidOperationException("An owned transfer qualification root is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_household_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" }); Assert.True(Path.IsPathFullyQualified(root)); Assert.False(Directory.Exists(root));
        Directory.CreateDirectory(root); await File.WriteAllTextAsync(Path.Combine(root, "fixture.marker"), "Zanance fictional household transfer qualification v1", Ct);
        await using (var schema = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options))
        { await schema.Database.MigrateAsync(Ct); Assert.False(schema.Database.HasPendingModelChanges()); }
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Service"] = runtime,
            ["Service:Root"] = Path.Combine(root, "service"), ["Service:Household:LocalQualification"] = "true" });
        builder.AddZananceService(); builder.Logging.ClearProviders(); builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build(); app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct));
            using var owner = LocalSyncHttp.CreateClient(peer, TimeProvider.System); using var target = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
            var fixtureId = Hash(root)[..12];
            var ownerId = await EnrollAsync(owner, app.Services, "household-transfer-owner-" + fixtureId + "@example.invalid");
            var targetId = await EnrollAsync(target, app.Services, "household-transfer-target-" + fixtureId + "@example.invalid");
            using var oldRoot = SigningRecipientIdentity.Create(); using var newRoot = SigningRecipientIdentity.Create();
            using var oldDevice = SigningRecipientIdentity.Create(); using var targetDevice = SigningRecipientIdentity.Create(); using var extraDevice = SigningRecipientIdentity.Create();
            using var epoch1 = HouseholdEpochMaterial.Create(); var crypto = new HouseholdCryptography(TimeProvider.System);
            var reservation = await PostAsync(owner, "households/", new HouseholdReserveSpace(Guid.NewGuid(), new('a', 64), Public(oldRoot), Public(oldDevice)),
                HouseholdMetadataJsonContext.Default.HouseholdReserveSpace, HouseholdMetadataJsonContext.Default.HouseholdSpaceReservation);
            var path = $"households/{reservation.SpaceId}";
            var genesis = Membership(reservation.SpaceId, oldRoot, ownerId, epoch1, 0, Zero, HouseholdMembershipChange.Genesis,
                [new(ownerId, HouseholdRole.Owner, Zero, [new(reservation.DeviceId, Public(oldDevice))])]);
            var initial = Publish(genesis, oldRoot, epoch1, crypto);
            var checkpoint = await PostAsync(owner, path + "/membership", initial, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership, HouseholdMetadataJsonContext.Default.HouseholdCheckpoint);
            var trusted = HouseholdTrustChain.Genesis(reservation.SpaceId, oldRoot.Public, new(initial.SignedStatement, null, null, HouseholdPublicationDigest.Grants(initial.Grants)));
            var capability = RandomNumberGenerator.GetBytes(32);
            var invitation = new HouseholdInvitationStatement(1, reservation.SpaceId, Guid.NewGuid(), Guid.NewGuid(), 1, checkpoint.MembershipHead,
                HouseholdRole.Editor, Convert.ToHexStringLower(SHA256.HashData(capability)), TimeProvider.System.GetUtcNow().AddHours(1).ToUnixTimeSeconds());
            var signedInvitation = Sign(invitation, HouseholdMetadataJsonContext.Default.HouseholdInvitationStatement, oldRoot, HouseholdMetadataProofs.InvitationType);
            using (var invite = await owner.PostAsJsonAsync(path + "/invitations", new HouseholdCreateInvitation(reservation.DeviceId, signedInvitation), Ct)) Assert.Equal(HttpStatusCode.OK, invite.StatusCode);
            var claim = new HouseholdInvitationClaimStatement(1, reservation.SpaceId, invitation.InvitationId, Guid.NewGuid(), targetId, Hash(signedInvitation), oldRoot.Public.Fingerprint, Public(targetDevice));
            var signedClaim = Sign(claim, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimStatement, targetDevice, HouseholdMetadataProofs.ClaimType);
            HouseholdInvitationClaimResult claimed;
            try { claimed = await PostAsync(target, path + $"/invitations/{invitation.InvitationId}/claim", new HouseholdClaimInvitation(Convert.ToHexStringLower(capability), signedClaim, Public(targetDevice)),
                HouseholdMetadataJsonContext.Default.HouseholdClaimInvitation, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimResult); }
            finally { CryptographicOperations.ZeroMemory(capability); }
            using var epoch2 = epoch1.Next();
            var targetMember = new HouseholdMemberDefinition(targetId, HouseholdRole.Editor, Hash(signedClaim), [new(claimed.DeviceId, Public(targetDevice))]);
            var accepted = Membership(reservation.SpaceId, oldRoot, ownerId, epoch2, 1, checkpoint.MembershipHead, HouseholdMembershipChange.AcceptMember,
                [genesis.Members[0], targetMember], invitation.InvitationId);
            var join = Publish(accepted, oldRoot, epoch2, crypto, signedClaim);
            checkpoint = await PostAsync(owner, path + "/membership", join, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership, HouseholdMetadataJsonContext.Default.HouseholdCheckpoint);
            trusted = HouseholdTrustChain.Advance(trusted, new(join.SignedStatement, null, signedClaim, HouseholdPublicationDigest.Grants(join.Grants)));
            using var targetMaterial = trusted.OpenGrant(crypto, join.Grants.Single(grant => grant.DeviceId == claimed.DeviceId).Ciphertext, claimed.DeviceId, targetDevice);
            // The target creates the new epoch/root; only its own recipient receives the new material.
            using var epoch3 = targetMaterial.Next();
            var transfer = Membership(reservation.SpaceId, oldRoot, ownerId, epoch3, 2, checkpoint.MembershipHead, HouseholdMembershipChange.TransferOwner,
                [targetMember with { Role = HouseholdRole.Owner }]) with { OwnerId = targetId, OwnerKeys = Public(newRoot) };
            var signedTransfer = Sign(transfer, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement, oldRoot, HouseholdMetadataProofs.MembershipType);
            var ciphertext = epoch3.Grant(crypto, transfer, newRoot, targetMember.Devices[0]);
            var grants = new[] { new HouseholdEncryptedGrant(3, claimed.DeviceId, ciphertext, HouseholdPublicationDigest.Ciphertext(ciphertext)) };
            var consent = new HouseholdOwnerAcceptanceStatement(1, reservation.SpaceId, transfer.OperationId, Hash(signedTransfer), HouseholdPublicationDigest.Grants(grants));
            var request = new HouseholdPublishMembership(reservation.DeviceId, signedTransfer, null, null, grants);
            using (var absent = await owner.PostAsJsonAsync(path + "/membership", request, Ct)) Assert.Equal(HttpStatusCode.Conflict, absent.StatusCode);
            var alteredConsent = Sign(consent with { GrantsHash = Zero }, HouseholdMetadataJsonContext.Default.HouseholdOwnerAcceptanceStatement, newRoot, HouseholdMetadataProofs.TransferType);
            using (var altered = await owner.PostAsJsonAsync(path + "/membership", request with { TransferAcceptance = alteredConsent }, Ct)) Assert.Equal(HttpStatusCode.Conflict, altered.StatusCode);
            var acceptance = Sign(consent, HouseholdMetadataJsonContext.Default.HouseholdOwnerAcceptanceStatement, newRoot, HouseholdMetadataProofs.TransferType);
            request = request with { TransferAcceptance = acceptance };
            var verifiedTransfer = HouseholdTrustChain.Advance(trusted, new(signedTransfer, acceptance, null, consent.GrantsHash));
            var rootGrants = new[] { HouseholdRecoveryMaterial.GrantOwnerRoot(crypto, verifiedTransfer, newRoot, claimed.DeviceId) };
            request = request with { OwnerRootGrants = rootGrants };
            // Even valid old/new-root signatures do not prove actual target-device consent.
            using (var bypass = await owner.PostAsJsonAsync(path + "/membership", request, Ct)) Assert.Equal(HttpStatusCode.Conflict, bypass.StatusCode);
            var offer = new HouseholdOwnershipOffer(1, reservation.SpaceId, transfer.OperationId, reservation.DeviceId, ownerId, claimed.DeviceId, targetId,
                HouseholdFormerOwnerRole.Leave, checkpoint.MembershipVersion, checkpoint.MembershipHead, checkpoint.Cursor, checkpoint.PublicationHead, transfer.ValidUntilUnixSeconds);
            var expiredOffer = offer with { OperationId = Guid.NewGuid(), ExpiresUnixSeconds = TimeProvider.System.GetUtcNow().AddSeconds(-1).ToUnixTimeSeconds() };
            var expired = await PostAsync(owner, path + "/ownership", new HouseholdOpenOwnership(reservation.DeviceId,
                Sign(expiredOffer, HouseholdMetadataJsonContext.Default.HouseholdOwnershipOffer, oldRoot, HouseholdOwnershipProtocol.OfferType)),
                HouseholdMetadataJsonContext.Default.HouseholdOpenOwnership, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation);
            Assert.Equal(HouseholdOwnershipStage.Expired, expired.Stage);
            var offered = await PostAsync(owner, path + "/ownership", new HouseholdOpenOwnership(reservation.DeviceId,
                Sign(offer, HouseholdMetadataJsonContext.Default.HouseholdOwnershipOffer, oldRoot, HouseholdOwnershipProtocol.OfferType)),
                HouseholdMetadataJsonContext.Default.HouseholdOpenOwnership, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation);
            Assert.Equal(HouseholdOwnershipStage.Offered, offered.Stage);
            var operationPath = path + "/ownership/" + transfer.OperationId;
            var preparation = new HouseholdOwnershipPreparation(transfer, grants, "");
            preparation = preparation with { SignedDeviceProof = Sign(new HouseholdOwnershipProof(1, reservation.SpaceId, transfer.OperationId, claimed.DeviceId,
                targetId, HouseholdOwnershipStage.Prepared, HouseholdOwnershipProtocol.PreparationDigest(preparation)), HouseholdMetadataJsonContext.Default.HouseholdOwnershipProof,
                targetDevice, HouseholdOwnershipProtocol.DeviceProofType) };
            using (var malformed = await target.PostAsJsonAsync(operationPath + "/prepare", new HouseholdPrepareOwnership(claimed.DeviceId,
                preparation with { Grants = [null!] }), Ct)) Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
            var prepared = await PostAsync(target, operationPath + "/prepare", new HouseholdPrepareOwnership(claimed.DeviceId, preparation),
                HouseholdMetadataJsonContext.Default.HouseholdPrepareOwnership, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation);
            Assert.Equal(HouseholdOwnershipStage.Prepared, prepared.Stage);
            var authorized = await PostAsync(owner, operationPath + "/authorize", new HouseholdAuthorizeOwnership(reservation.DeviceId, signedTransfer),
                HouseholdMetadataJsonContext.Default.HouseholdAuthorizeOwnership, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation);
            Assert.Equal(HouseholdOwnershipStage.Authorized, authorized.Stage);
            var targetAcceptance = new HouseholdOwnershipAcceptance(acceptance, rootGrants, "");
            var proof = new HouseholdOwnershipProof(1, reservation.SpaceId, transfer.OperationId, claimed.DeviceId, targetId, HouseholdOwnershipStage.Accepted,
                HouseholdOwnershipProtocol.AcceptanceDigest(targetAcceptance));
            using (var impersonated = await target.PostAsJsonAsync(operationPath + "/accept", new HouseholdAcceptOwnership(claimed.DeviceId,
                targetAcceptance with { SignedDeviceProof = Sign(proof, HouseholdMetadataJsonContext.Default.HouseholdOwnershipProof, oldDevice, HouseholdOwnershipProtocol.DeviceProofType) }), Ct))
                Assert.Equal(HttpStatusCode.BadRequest, impersonated.StatusCode);
            targetAcceptance = targetAcceptance with { SignedDeviceProof = Sign(proof, HouseholdMetadataJsonContext.Default.HouseholdOwnershipProof, targetDevice, HouseholdOwnershipProtocol.DeviceProofType) };
            var acceptedTransfer = await PostAsync(target, operationPath + "/accept", new HouseholdAcceptOwnership(claimed.DeviceId, targetAcceptance),
                HouseholdMetadataJsonContext.Default.HouseholdAcceptOwnership, HouseholdMetadataJsonContext.Default.HouseholdOwnershipObservation);
            Assert.Equal(HouseholdOwnershipStage.Accepted, acceptedTransfer.Stage);
            checkpoint = await PostAsync(owner, path + "/membership", request, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership, HouseholdMetadataJsonContext.Default.HouseholdCheckpoint);
            Assert.Equal(targetId, checkpoint.OwnerId);
            trusted = HouseholdTrustChain.Advance(trusted, new(signedTransfer, acceptance, null, consent.GrantsHash));
            Assert.Equal(newRoot.Public, trusted.Root);
            using (var removed = await owner.GetAsync(path + $"/grants/2?device={reservation.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.NotFound, removed.StatusCode);
            using (var oldRootReplay = await owner.PostAsJsonAsync(path + "/membership", request, Ct)) Assert.Equal(HttpStatusCode.NotFound, oldRootReplay.StatusCode);
            var session = await target.GetFromJsonAsync("households/session", HouseholdMetadataJsonContext.Default.HouseholdSessionIdentity, Ct); Assert.NotNull(session);
            var admission = new HouseholdDeviceSessionStatement(1, reservation.SpaceId, Guid.Empty, Guid.NewGuid(), targetId, session.SessionId, Public(extraDevice), TimeProvider.System.GetUtcNow().AddMinutes(10).ToUnixTimeSeconds());
            var pending = await PostAsync(target, path + "/devices", new HouseholdEnrollDevice(Public(extraDevice), Sign(admission,
                HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement, extraDevice, "zanance-household-device-enrollment+jwt")),
                HouseholdMetadataJsonContext.Default.HouseholdEnrollDevice, HouseholdMetadataJsonContext.Default.HouseholdDeviceObservation);
            using var epoch4 = epoch3.Next();
            var final = Membership(reservation.SpaceId, newRoot, targetId, epoch4, 3, checkpoint.MembershipHead, HouseholdMembershipChange.ApproveDevice,
                [transfer.Members[0] with { Devices = [..transfer.Members[0].Devices, new(pending.DeviceId, Public(extraDevice))] }]);
            var finalPublication = Publish(final, newRoot, epoch4, crypto);
            var verifiedNext = HouseholdTrustChain.Advance(trusted, new(finalPublication.SignedStatement, null, null, HouseholdPublicationDigest.Grants(finalPublication.Grants)));
            finalPublication = finalPublication with { OwnerRootGrants = final.Members[0].Devices.Select(device => HouseholdRecoveryMaterial.GrantOwnerRoot(crypto, verifiedNext, newRoot, device.Id)).ToArray() };
            checkpoint = await PostAsync(target, path + "/membership", finalPublication, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership, HouseholdMetadataJsonContext.Default.HouseholdCheckpoint);
            var history = await target.GetFromJsonAsync(path + $"/membership?device={claimed.DeviceId}&after=2", HouseholdMetadataJsonContext.Default.HouseholdMembershipProofArray, Ct);
            Assert.NotNull(history); Assert.Equal(2, history.Length);
            Assert.Equal(trusted.Hash, HouseholdTrustChain.Digest(history[0].SignedStatement));
            trusted = HouseholdTrustChain.Advance(trusted, history[1]); Assert.Equal(checkpoint.MembershipHead, trusted.Hash);
            using var material = trusted.OpenGrant(crypto, finalPublication.Grants.Single(grant => grant.DeviceId == pending.DeviceId).Ciphertext, pending.DeviceId, extraDevice);
            Assert.Equal(epoch4.KeyCommitment, material.KeyCommitment);
            await File.WriteAllTextAsync(Path.Combine(root, "completed-stages.txt"), "actual-old-root-consent-new-root-acceptance-old-owner-refusal-and-next-generation-continuity", Ct);
        }
        finally { await app.StopAsync(Ct); }
    }

    /// <summary>New login cannot approve a key; existing approved private possession may rebind, but revoked keys can never resurrect themselves.</summary>
    [Fact]
    public async Task Actual_new_device_needs_owner_rotation_and_session_rebind_needs_private_possession()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_TEST_DB") ?? throw new InvalidOperationException("An isolated household database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_DEVICE_TEST_ROOT") ?? throw new InvalidOperationException("An owned device qualification root is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_household_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" }); Assert.True(Path.IsPathFullyQualified(root)); Assert.False(Directory.Exists(root));
        Directory.CreateDirectory(root); await File.WriteAllTextAsync(Path.Combine(root, "fixture.marker"), "Zanance fictional household device qualification v1", Ct);
        await using (var schema = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options))
        {
            await schema.Database.MigrateAsync(Ct); Assert.False(schema.Database.HasPendingModelChanges());
            await schema.Database.ExecuteSqlRawAsync("GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO zanance_runtime", Ct);
        }
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Service"] = runtime,
            ["Service:Root"] = Path.Combine(root, "service"), ["Service:Household:LocalQualification"] = "true" });
        builder.AddZananceService(); builder.Logging.ClearProviders(); builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build(); app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct));
            using var owner = LocalSyncHttp.CreateClient(peer, TimeProvider.System); using var second = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
            using var third = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
            const string email = "household-device-owner@example.invalid";
            var ownerId = await EnrollAsync(owner, app.Services, email); Assert.Equal(ownerId, await EnrollAsync(second, app.Services, email));
            using var rootKey = SigningRecipientIdentity.Create(); using var firstKey = SigningRecipientIdentity.Create();
            using var secondKey = SigningRecipientIdentity.Create(); using var wrongKey = SigningRecipientIdentity.Create();
            using var firstEpoch = HouseholdEpochMaterial.Create(); var crypto = new HouseholdCryptography(TimeProvider.System);
            var reservation = await PostAsync(owner, "households/", new HouseholdReserveSpace(Guid.NewGuid(), new('a', 64), Public(rootKey), Public(firstKey)),
                HouseholdMetadataJsonContext.Default.HouseholdReserveSpace, HouseholdMetadataJsonContext.Default.HouseholdSpaceReservation);
            var path = $"households/{reservation.SpaceId}";
            var genesis = Membership(reservation.SpaceId, rootKey, ownerId, firstEpoch, 0, Zero, HouseholdMembershipChange.Genesis,
                [new(ownerId, HouseholdRole.Owner, Zero, [new(reservation.DeviceId, Public(firstKey))])]);
            var original = Publish(genesis, rootKey, firstEpoch, crypto);
            var checkpoint = await PostAsync(owner, path + "/membership", original, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership,
                HouseholdMetadataJsonContext.Default.HouseholdCheckpoint);
            var history = await owner.GetFromJsonAsync(path + $"/membership?device={reservation.DeviceId}&after=0",
                HouseholdMetadataJsonContext.Default.HouseholdMembershipProofArray, Ct);
            Assert.NotNull(history); var trusted = HouseholdTrustChain.Genesis(reservation.SpaceId, rootKey.Public, Assert.Single(history));
            Assert.Equal(checkpoint.MembershipHead, trusted.Hash);
            var session = await second.GetFromJsonAsync("households/session", HouseholdMetadataJsonContext.Default.HouseholdSessionIdentity, Ct);
            Assert.NotNull(session); Assert.Equal(ownerId, session.IdentityId);
            var admission = new HouseholdDeviceSessionStatement(1, reservation.SpaceId, Guid.Empty, Guid.NewGuid(), ownerId, session.SessionId,
                Public(secondKey), TimeProvider.System.GetUtcNow().AddMinutes(10).ToUnixTimeSeconds());
            var signedAdmission = Sign(admission, HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement, secondKey, "zanance-household-device-enrollment+jwt");
            var request = new HouseholdEnrollDevice(Public(secondKey), signedAdmission);
            var pending = await PostAsync(second, path + "/devices", request, HouseholdMetadataJsonContext.Default.HouseholdEnrollDevice,
                HouseholdMetadataJsonContext.Default.HouseholdDeviceObservation);
            Assert.False(pending.Approved);
            Assert.Equal(pending, await PostAsync(second, path + "/devices", request, HouseholdMetadataJsonContext.Default.HouseholdEnrollDevice,
                HouseholdMetadataJsonContext.Default.HouseholdDeviceObservation));
            using (var pendingRead = await second.GetAsync(path + $"/checkpoint?device={pending.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.NotFound, pendingRead.StatusCode);
            using var secondEpoch = firstEpoch.Next();
            var approved = Membership(reservation.SpaceId, rootKey, ownerId, secondEpoch, 1, checkpoint.MembershipHead, HouseholdMembershipChange.ApproveDevice,
                [genesis.Members[0] with { Devices = [..genesis.Members[0].Devices, new(pending.DeviceId, Public(secondKey))] }]);
            var approve = Publish(approved, rootKey, secondEpoch, crypto);
            checkpoint = await PostAsync(owner, path + "/membership", approve, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership,
                HouseholdMetadataJsonContext.Default.HouseholdCheckpoint);
            using (var approvedRead = await second.GetAsync(path + $"/checkpoint?device={pending.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.OK, approvedRead.StatusCode);
            history = await second.GetFromJsonAsync(path + $"/membership?device={pending.DeviceId}&after=1", HouseholdMetadataJsonContext.Default.HouseholdMembershipProofArray, Ct);
            Assert.NotNull(history); trusted = HouseholdTrustChain.Advance(trusted, Assert.Single(history));
            Assert.Equal(checkpoint.MembershipHead, trusted.Hash);
            Assert.Equal(ownerId, await EnrollAsync(third, app.Services, email));
            var freshSession = await third.GetFromJsonAsync("households/session", HouseholdMetadataJsonContext.Default.HouseholdSessionIdentity, Ct);
            Assert.NotNull(freshSession); Assert.NotEqual(session.SessionId, freshSession.SessionId);
            using (var loginOnly = await third.GetAsync(path + $"/grants/2?device={pending.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.NotFound, loginOnly.StatusCode);
            var possession = admission with { DeviceId = pending.DeviceId, SessionId = freshSession.SessionId, OperationId = Guid.NewGuid() };
            var wrong = new HouseholdRebindDevice(Sign(possession, HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement, wrongKey, "zanance-household-device-session+jwt"));
            using (var refused = await third.PostAsJsonAsync(path + $"/devices/{pending.DeviceId}/rebind", wrong, Ct)) Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
            var rebind = new HouseholdRebindDevice(Sign(possession, HouseholdMetadataJsonContext.Default.HouseholdDeviceSessionStatement, secondKey, "zanance-household-device-session+jwt"));
            var rebound = await PostAsync(third, path + $"/devices/{pending.DeviceId}/rebind", rebind, HouseholdMetadataJsonContext.Default.HouseholdRebindDevice,
                HouseholdMetadataJsonContext.Default.HouseholdDeviceObservation);
            Assert.True(rebound.Approved);
            Assert.Equal(rebound, await PostAsync(third, path + $"/devices/{pending.DeviceId}/rebind", rebind, HouseholdMetadataJsonContext.Default.HouseholdRebindDevice,
                HouseholdMetadataJsonContext.Default.HouseholdDeviceObservation));
            using (var oldSession = await second.GetAsync(path + $"/grants/2?device={pending.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.NotFound, oldSession.StatusCode);
            using (var replayOld = await second.PostAsJsonAsync(path + $"/devices/{pending.DeviceId}/rebind", rebind, Ct)) Assert.Equal(HttpStatusCode.Forbidden, replayOld.StatusCode);
            using var grantResponse = await third.GetAsync(path + $"/grants/2?device={pending.DeviceId}", Ct); Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode);
            var grant = await grantResponse.Content.ReadFromJsonAsync<HouseholdKeyGrantRow>(Ct); Assert.NotNull(grant);
            using var material = trusted.OpenGrant(crypto, grant.Ciphertext, pending.DeviceId, secondKey);
            Assert.Equal(secondEpoch.KeyCommitment, material.KeyCommitment);
            using var thirdEpoch = secondEpoch.Next();
            var revoked = Membership(reservation.SpaceId, rootKey, ownerId, thirdEpoch, 2, checkpoint.MembershipHead, HouseholdMembershipChange.RevokeDevice,
                [genesis.Members[0]]);
            _ = await PostAsync(owner, path + "/membership", Publish(revoked, rootKey, thirdEpoch, crypto),
                HouseholdMetadataJsonContext.Default.HouseholdPublishMembership, HouseholdMetadataJsonContext.Default.HouseholdCheckpoint);
            using (var finalRead = await third.GetAsync(path + $"/grants/2?device={pending.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.NotFound, finalRead.StatusCode);
            using (var resurrect = await third.PostAsJsonAsync(path + $"/devices/{pending.DeviceId}/rebind", rebind, Ct)) Assert.Equal(HttpStatusCode.NotFound, resurrect.StatusCode);
            await File.WriteAllTextAsync(Path.Combine(root, "completed-stages.txt"), "actual-pending-approval-rotation-proof-chain-rebind-old-session-refusal-and-revocation", Ct);
        }
        finally { await app.StopAsync(Ct); }
    }

    /// <summary>Two-sided enrollment, encrypted atomic CAS, lost acknowledgement, exact receipt/feed and immediate revocation share actual service transactions.</summary>
    [Fact]
    public async Task Actual_household_service_requires_consent_and_preserves_encrypted_atomic_publications_and_revocation()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_TEST_DB") ?? throw new InvalidOperationException("An isolated household PostgreSQL fixture is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_TEST_ROOT") ?? throw new InvalidOperationException("An independently owned household fixture root is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_household_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" }); Assert.True(Path.IsPathFullyQualified(root));
        const string marker = "Zanance fictional encrypted household service v1";
        if (Directory.Exists(root))
        {
            Assert.Equal(marker, await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
            Assert.False(File.Exists(Path.Combine(root, "completed-stages.txt")));
        }
        else { Directory.CreateDirectory(root); await File.WriteAllTextAsync(Path.Combine(root, "fixture.marker"), marker, Ct); }
        void Checkpoint(string stage) => File.AppendAllText(Path.Combine(root, "completed-stages.txt"), stage + Environment.NewLine);
        await using (var schema = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options))
        {
            await schema.Database.MigrateAsync(Ct); Assert.False(schema.Database.HasPendingModelChanges());
            Assert.False(await schema.Set<HouseholdSpaceRow>().AnyAsync(Ct));
            await schema.Database.ExecuteSqlRawAsync("GRANT USAGE ON SCHEMA public TO zanance_runtime; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO zanance_runtime; GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO zanance_runtime", Ct);
        }
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Service"] = runtime,
            ["Service:Root"] = Path.Combine(root, "service"), ["Service:Household:LocalQualification"] = "true" });
        builder.AddZananceService(); builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build();
        app.Use(async (http, next) =>
        {
            try { await next(http); }
            catch (Exception error)
            {
                // Only exception types/stacks and PostgreSQL's public SQLSTATE. Never log request,
                // exception messages, detail fields, credentials, capability values or parameters.
                var lines = new List<string>();
                for (Exception? current = error; current is not null; current = current.InnerException)
                { lines.Add(current.GetType().FullName!); lines.Add(current.StackTrace ?? ""); if (current is PostgresException postgres) lines.Add(postgres.SqlState); }
                await File.AppendAllLinesAsync(Path.Combine(root, "content-free-failure.txt"), lines, Ct);
                throw;
            }
        });
        var loseCommit = 0;
        app.Use(async (http, next) =>
        {
            if (!http.Request.Path.Value!.EndsWith("/commit", StringComparison.Ordinal) || Interlocked.CompareExchange(ref loseCommit, 0, 1) != 1)
            { await next(http); return; }
            var actual = http.Response.Body; await using var suppressed = new MemoryStream(); http.Response.Body = suppressed;
            try { await next(http); } finally { http.Response.Body = actual; }
            http.Abort();
        });
        app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct));
            using var owner = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
            using var editor = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
            using var viewer = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
            var ownerId = await EnrollAsync(owner, app.Services, "household-owner@example.invalid");
            var editorId = await EnrollAsync(editor, app.Services, "household-editor@example.invalid");
            var viewerId = await EnrollAsync(viewer, app.Services, "household-viewer@example.invalid");
            using var rootKey = SigningRecipientIdentity.Create(); using var ownerKey = SigningRecipientIdentity.Create();
            using var editorKey = SigningRecipientIdentity.Create(); using var viewerKey = SigningRecipientIdentity.Create();
            var crypto = new HouseholdCryptography(TimeProvider.System);
            var creation = new HouseholdReserveSpace(Guid.NewGuid(), new('a', 64), Public(rootKey), Public(ownerKey));
            var reservation = await PostAsync(owner, "households/", creation, HouseholdMetadataJsonContext.Default.HouseholdReserveSpace,
                HouseholdMetadataJsonContext.Default.HouseholdSpaceReservation);
            Assert.False(reservation.Initialized);
            Assert.Equal(reservation, await PostAsync(owner, "households/", creation, HouseholdMetadataJsonContext.Default.HouseholdReserveSpace,
                HouseholdMetadataJsonContext.Default.HouseholdSpaceReservation));
            var room = reservation.SpaceId; var path = $"households/{room}";
            using var epoch1 = HouseholdEpochMaterial.Create();
            var genesis = Membership(room, rootKey, ownerId, epoch1, 0, Zero, HouseholdMembershipChange.Genesis,
                [new(ownerId, HouseholdRole.Owner, Zero, [new(reservation.DeviceId, Public(ownerKey))])]);
            var publication = Publish(genesis, rootKey, epoch1, crypto);
            var checkpoint = await PostAsync(owner, path + "/membership", publication, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership,
                HouseholdMetadataJsonContext.Default.HouseholdCheckpoint);
            Assert.Equal(1, checkpoint.MembershipVersion); Assert.Equal(1, checkpoint.KeyEpoch);
            Assert.Equal(checkpoint, await PostAsync(owner, path + "/membership", publication, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership,
                HouseholdMetadataJsonContext.Default.HouseholdCheckpoint));
            using (var material = HouseholdEpochMaterial.OpenGrant(crypto, genesis, publication.Grants[0].Ciphertext, reservation.DeviceId, ownerKey))
                Assert.Equal(epoch1.KeyCommitment, material.KeyCommitment);
            Checkpoint("actual-owner-reservation-and-genesis-idempotent");

            async Task<(HouseholdInvitationClaimResult Result, string Proof)> Invite(HttpClient recipient, string identity, SigningRecipientIdentity deviceKey, HouseholdRole role)
            {
                var capability = RandomNumberGenerator.GetBytes(32);
                try
                {
                    var statement = new HouseholdInvitationStatement(1, room, Guid.NewGuid(), Guid.NewGuid(), checkpoint.MembershipVersion,
                        checkpoint.MembershipHead, role, Convert.ToHexStringLower(SHA256.HashData(capability)), TimeProvider.System.GetUtcNow().AddHours(1).ToUnixTimeSeconds());
                    var signedInvitation = Sign(statement, HouseholdMetadataJsonContext.Default.HouseholdInvitationStatement, rootKey, HouseholdMetadataProofs.InvitationType);
                    using var inviteResponse = await owner.PostAsJsonAsync(path + "/invitations", new HouseholdCreateInvitation(reservation.DeviceId, signedInvitation), Ct);
                    Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);
                    var claim = new HouseholdInvitationClaimStatement(1, room, statement.InvitationId, Guid.NewGuid(), identity,
                        Hash(signedInvitation), rootKey.Public.Fingerprint, Public(deviceKey));
                    var signedClaim = Sign(claim, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimStatement, deviceKey, HouseholdMetadataProofs.ClaimType);
                    var request = new HouseholdClaimInvitation(Convert.ToHexStringLower(capability), signedClaim, Public(deviceKey));
                    var result = await PostAsync(recipient, path + $"/invitations/{statement.InvitationId}/claim", request,
                        HouseholdMetadataJsonContext.Default.HouseholdClaimInvitation, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimResult);
                    Assert.False(result.Accepted);
                    Assert.Equal(result, await PostAsync(recipient, path + $"/invitations/{statement.InvitationId}/claim", request,
                        HouseholdMetadataJsonContext.Default.HouseholdClaimInvitation, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimResult));
                    using var pendingRead = await recipient.GetAsync(path + $"/checkpoint?device={result.DeviceId}", Ct);
                    Assert.Equal(HttpStatusCode.NotFound, pendingRead.StatusCode);
                    return (result, signedClaim);
                }
                finally { CryptographicOperations.ZeroMemory(capability); }
            }
            var editorClaim = await Invite(editor, editorId, editorKey, HouseholdRole.Editor);
            using var epoch2 = epoch1.Next();
            var withEditor = Membership(room, rootKey, ownerId, epoch2, 1, checkpoint.MembershipHead, HouseholdMembershipChange.AcceptMember,
                [genesis.Members[0], new(editorId, HouseholdRole.Editor, Hash(editorClaim.Proof), [new(editorClaim.Result.DeviceId, Public(editorKey))])], editorClaim.Result.InvitationId);
            var acceptEditor = Publish(withEditor, rootKey, epoch2, crypto, editorClaim.Proof);
            using (var missingConsent = await owner.PostAsJsonAsync(path + "/membership", acceptEditor with { SignedAcceptanceClaim = null }, Ct))
                Assert.Equal(HttpStatusCode.Conflict, missingConsent.StatusCode);
            using (var missingGrant = await owner.PostAsJsonAsync(path + "/membership", acceptEditor with { Grants = acceptEditor.Grants[..1] }, Ct))
                Assert.Equal(HttpStatusCode.Conflict, missingGrant.StatusCode);
            checkpoint = await PostAsync(owner, path + "/membership", acceptEditor, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership,
                HouseholdMetadataJsonContext.Default.HouseholdCheckpoint);
            var viewerClaim = await Invite(viewer, viewerId, viewerKey, HouseholdRole.Viewer);
            using var epoch3 = epoch2.Next();
            var withViewer = Membership(room, rootKey, ownerId, epoch3, 2, checkpoint.MembershipHead, HouseholdMembershipChange.AcceptMember,
                [..withEditor.Members, new(viewerId, HouseholdRole.Viewer, Hash(viewerClaim.Proof), [new(viewerClaim.Result.DeviceId, Public(viewerKey))])], viewerClaim.Result.InvitationId);
            var acceptViewer = Publish(withViewer, rootKey, epoch3, crypto, viewerClaim.Proof);
            checkpoint = await PostAsync(owner, path + "/membership", acceptViewer, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership,
                HouseholdMetadataJsonContext.Default.HouseholdCheckpoint);
            using var editorMaterial = HouseholdEpochMaterial.OpenGrant(crypto, withViewer,
                acceptViewer.Grants.Single(grant => grant.DeviceId == editorClaim.Result.DeviceId).Ciphertext, editorClaim.Result.DeviceId, editorKey);
            using var viewerMaterial = HouseholdEpochMaterial.OpenGrant(crypto, withViewer,
                acceptViewer.Grants.Single(grant => grant.DeviceId == viewerClaim.Result.DeviceId).Ciphertext, viewerClaim.Result.DeviceId, viewerKey);
            Checkpoint("actual-two-sided-editor-viewer-enrollment-and-recipient-grants");

            var op = Guid.NewGuid(); var attempt = Guid.NewGuid();
            var open = new HouseholdOpenRound(reservation.DeviceId, op, attempt, 0, checkpoint.MembershipVersion, epoch3.Epoch, creation.SchemaHash);
            using (var denied = await viewer.PostAsJsonAsync(path + "/rounds", open with { DeviceId = viewerClaim.Result.DeviceId }, Ct))
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            using (var crossDevice = await editor.PostAsJsonAsync(path + "/rounds", open, Ct)) Assert.Equal(HttpStatusCode.NotFound, crossDevice.StatusCode);
            var round = await PostAsync(owner, path + "/rounds", open, HouseholdMetadataJsonContext.Default.HouseholdOpenRound, HouseholdMetadataJsonContext.Default.HouseholdRoundHandle);
            var concurrentOp = Guid.NewGuid();
            var concurrentOpen = open with { DeviceId = editorClaim.Result.DeviceId, OperationId = concurrentOp, AttemptId = Guid.NewGuid() };
            var concurrent = await PostAsync(editor, path + "/rounds", concurrentOpen, HouseholdMetadataJsonContext.Default.HouseholdOpenRound, HouseholdMetadataJsonContext.Default.HouseholdRoundHandle);
            var clear = Encoding.UTF8.GetBytes("Fictitious household finance; no owner data.");
            var parts = new List<HouseholdOpaquePart>();
            for (var index = 0; index < 10; index++)
            {
                var ciphertext = epoch3.Encrypt(crypto, new(room, epoch3.Epoch, op, HouseholdProtectionPurpose.Financial, "members"), clear, ownerKey);
                var part = new HouseholdOpaquePart(index, epoch3.RecordAddress("Fictitious", index.ToString(System.Globalization.CultureInfo.InvariantCulture)), 0,
                    ciphertext, HouseholdPublicationDigest.Ciphertext(ciphertext));
                parts.Add(part);
                using var stored = await owner.PutAsJsonAsync(path + $"/rounds/{round.Id}/parts", part, Ct); Assert.Equal(HttpStatusCode.NoContent, stored.StatusCode);
            }
            using (var altered = await owner.PutAsJsonAsync(path + $"/rounds/{round.Id}/parts", parts[0] with { BaseVersion = 1 }, Ct))
                Assert.Equal(HttpStatusCode.Conflict, altered.StatusCode);
            var manifest = HouseholdPublicationDigest.Manifest(parts.Select(part => new HouseholdPartCommitment(part.Index, part.RecordId, part.BaseVersion, part.Digest)).ToArray());
            var commitProof = new HouseholdCommitStatement(1, room, reservation.DeviceId, op, attempt, 0, 1, checkpoint.MembershipVersion, epoch3.Epoch, Zero, 10, manifest);
            var commit = new HouseholdCommitRound(reservation.DeviceId, Sign(commitProof, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement, ownerKey, HouseholdMetadataProofs.CommitType));
            Interlocked.Exchange(ref loseCommit, 1);
            var dropped = await Record.ExceptionAsync(async () => { using var response = await owner.PostAsJsonAsync(path + $"/rounds/{round.Id}/commit", commit, Ct); });
            Assert.True(dropped is HttpRequestException or IOException or OperationCanceledException);
            var committed = await PostAsync(owner, path + $"/rounds/{round.Id}/commit", commit, HouseholdMetadataJsonContext.Default.HouseholdCommitRound,
                HouseholdMetadataJsonContext.Default.HouseholdRoundHandle);
            Assert.Equal(1, committed.CommittedCursor); Assert.Equal(10, committed.Parts);
            Assert.Equal(committed, await PostAsync(owner, path + $"/rounds/{round.Id}/commit", commit, HouseholdMetadataJsonContext.Default.HouseholdCommitRound,
                HouseholdMetadataJsonContext.Default.HouseholdRoundHandle));
            Checkpoint("actual-lost-commit-acknowledgement-resumed-one-atomic-cursor");
            var firstPage = await viewer.GetFromJsonAsync(path + $"/feed?device={viewerClaim.Result.DeviceId}&afterCursor=0&afterRecord=&through=1",
                HouseholdMetadataJsonContext.Default.HouseholdFeedPage, Ct);
            Assert.NotNull(firstPage); Assert.False(firstPage.Complete); Assert.Equal(8, firstPage.Events.Length);
            var secondPage = await viewer.GetFromJsonAsync(path + $"/feed?device={viewerClaim.Result.DeviceId}&afterCursor={firstPage.Next.Cursor}&afterRecord={firstPage.Next.RecordId}&through=1",
                HouseholdMetadataJsonContext.Default.HouseholdFeedPage, Ct);
            Assert.NotNull(secondPage); Assert.True(secondPage.Complete); Assert.Equal(2, secondPage.Events.Length);
            var receipt = await viewer.GetFromJsonAsync(path + $"/receipts/{round.Id}?device={viewerClaim.Result.DeviceId}",
                HouseholdMetadataJsonContext.Default.HouseholdPublicationReceipt, Ct);
            Assert.NotNull(receipt); Assert.Equal(manifest, HouseholdPublicationDigest.Manifest(receipt.Parts));
            var signedBytes = StandardJoseSignatures.Verify(receipt.SignedCommit, ownerKey.Public, HouseholdMetadataProofs.CommitType, HouseholdLimits.MembershipBytes);
            CryptographicOperations.ZeroMemory(signedBytes);
            foreach (var item in firstPage.Events.Concat(secondPage.Events))
            {
                var plaintext = viewerMaterial.Decrypt(crypto, new(room, 3, op, HouseholdProtectionPurpose.Financial, "members"), item.Ciphertext, ownerKey.Public);
                try { Assert.Equal(HashBytes(clear), HashBytes(plaintext)); } finally { CryptographicOperations.ZeroMemory(plaintext); }
                Assert.DoesNotContain("Fictitious", item.Ciphertext, StringComparison.Ordinal);
            }
            Checkpoint("actual-viewer-feed-exact-prefix-full-signed-manifest-and-client-decryption");

            var editorCipher = editorMaterial.Encrypt(crypto, new(room, 3, concurrentOp, HouseholdProtectionPurpose.Financial, "members"), clear, editorKey);
            var editorPart = new HouseholdOpaquePart(0, editorMaterial.RecordAddress("Fictitious", "editor"), 0, editorCipher, HouseholdPublicationDigest.Ciphertext(editorCipher));
            using (var stale = await editor.PutAsJsonAsync(path + $"/rounds/{concurrent.Id}/parts", editorPart, Ct)) Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            using (var abandon = await editor.DeleteAsync(path + $"/rounds/{concurrent.Id}", Ct)) Assert.Equal(HttpStatusCode.NoContent, abandon.StatusCode);
            var retryOpen = concurrentOpen with { AttemptId = Guid.NewGuid(), ExpectedCursor = 1 };
            var retry = await PostAsync(editor, path + "/rounds", retryOpen, HouseholdMetadataJsonContext.Default.HouseholdOpenRound, HouseholdMetadataJsonContext.Default.HouseholdRoundHandle);
            using (var part = await editor.PutAsJsonAsync(path + $"/rounds/{retry.Id}/parts", editorPart, Ct)) Assert.Equal(HttpStatusCode.NoContent, part.StatusCode);
            var receiptHead = Hash(commit.SignedCommit);
            var editorProof = new HouseholdCommitStatement(1, room, editorClaim.Result.DeviceId, concurrentOp, retryOpen.AttemptId, 1, 2, 3, 3, receiptHead, 1,
                HouseholdPublicationDigest.Manifest([new(0, editorPart.RecordId, 0, editorPart.Digest)]));
            var editorCommit = new HouseholdCommitRound(editorClaim.Result.DeviceId, Sign(editorProof, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement, editorKey, HouseholdMetadataProofs.CommitType));
            var editorCommitted = await PostAsync(editor, path + $"/rounds/{retry.Id}/commit", editorCommit,
                HouseholdMetadataJsonContext.Default.HouseholdCommitRound, HouseholdMetadataJsonContext.Default.HouseholdRoundHandle);
            Assert.Equal(2, editorCommitted.CommittedCursor);
            using (var duplicateLogical = await editor.PostAsJsonAsync(path + "/rounds", retryOpen with { AttemptId = Guid.NewGuid(), ExpectedCursor = 2 }, Ct))
                Assert.Equal(HttpStatusCode.Conflict, duplicateLogical.StatusCode);
            Checkpoint("actual-CAS-retry-preserves-logical-operation-and-commits-only-once");

            using (var leave = await editor.PostAsync(path + $"/leave?device={editorClaim.Result.DeviceId}", null, Ct)) Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);
            using (var deniedRead = await editor.GetAsync(path + $"/grants/3?device={editorClaim.Result.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.NotFound, deniedRead.StatusCode);
            using (var frozen = await owner.PostAsJsonAsync(path + "/rounds", open with { OperationId = Guid.NewGuid(), AttemptId = Guid.NewGuid(), ExpectedCursor = 2 }, Ct))
                Assert.Equal(HttpStatusCode.Conflict, frozen.StatusCode);
            using var epoch4 = epoch3.Next();
            var withoutEditor = Membership(room, rootKey, ownerId, epoch4, 3, checkpoint.MembershipHead, HouseholdMembershipChange.ConfirmDeparture,
                withViewer.Members.Where(member => member.IdentityId != editorId).ToArray()) with { PublicationCursor = 2, PublicationHead = Hash(editorCommit.SignedCommit) };
            var rotation = Publish(withoutEditor, rootKey, epoch4, crypto);
            checkpoint = await PostAsync(owner, path + "/membership", rotation, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership,
                HouseholdMetadataJsonContext.Default.HouseholdCheckpoint);
            Assert.Equal(4, checkpoint.KeyEpoch); Assert.False(checkpoint.RotationRequired);
            Assert.DoesNotContain(rotation.Grants, grant => grant.DeviceId == editorClaim.Result.DeviceId);
            var newCipher = epoch4.Encrypt(crypto, new(room, 4, withoutEditor.OperationId, HouseholdProtectionPurpose.Financial, "members"), clear, ownerKey);
            Assert.Throws<CryptographicException>(() => editorMaterial.Decrypt(crypto, new(room, 4, withoutEditor.OperationId,
                HouseholdProtectionPurpose.Financial, "members"), newCipher, ownerKey.Public));
            Checkpoint("actual-departure-revokes-all-resources-and-requires-fresh-recipient-rotation");
            await using (var inspect = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(runtime).Options))
            {
                Assert.Equal(2, await inspect.Set<HouseholdRoundRow>().CountAsync(row => row.SpaceId == room && row.CommittedCursor != null, Ct));
                Assert.Equal(11, await inspect.Set<HouseholdRecordRow>().CountAsync(row => row.SpaceId == room, Ct));
                Assert.Equal(4, await inspect.Set<HouseholdMembershipEventRow>().CountAsync(row => row.SpaceId == room, Ct));
                var space = await inspect.Set<HouseholdSpaceRow>().SingleAsync(row => row.Id == room, Ct);
                space.HostExpiresAt = TimeProvider.System.GetUtcNow().AddDays(-1); await inspect.SaveChangesAsync(Ct);
            }
            using (var graceRead = await viewer.GetAsync(path + $"/checkpoint?device={viewerClaim.Result.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.OK, graceRead.StatusCode);
            using (var graceWrite = await owner.PostAsJsonAsync(path + "/rounds", open with { OperationId = Guid.NewGuid(), AttemptId = Guid.NewGuid(), ExpectedCursor = 2, MembershipVersion = 4, KeyEpoch = 4 }, Ct))
                Assert.Equal(HttpStatusCode.Forbidden, graceWrite.StatusCode);
            await using (var retention = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(runtime).Options))
            {
                var space = await retention.Set<HouseholdSpaceRow>().SingleAsync(row => row.Id == room, Ct);
                space.HostExpiresAt = TimeProvider.System.GetUtcNow().AddDays(-31); await retention.SaveChangesAsync(Ct);
            }
            using (var retainedRead = await viewer.GetAsync(path + $"/checkpoint?device={viewerClaim.Result.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.Forbidden, retainedRead.StatusCode);
            Checkpoint("actual-verified-expiry-grace-read-only-and-retained-access-refusal");
            CryptographicOperations.ZeroMemory(clear);
        }
        finally { await app.StopAsync(Ct); }
    }

    /// <summary>Uses original registration/one-use confirmation/session endpoints; only synthetic example.invalid credentials exist.</summary>
    private static async Task<string> EnrollAsync(HttpClient client, IServiceProvider services, string email)
    {
        const string password = "Fictitious-Household-Password9!";
        string identity;
        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            var user = await db.Users.SingleOrDefaultAsync(row => row.Email == email, Ct);
            if (user is null)
            {
                using var registration = await client.PostAsJsonAsync("identity/register", new IdentityInput(email, password), Ct);
                Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);
                user = await db.Users.SingleAsync(row => row.Email == email, Ct);
            }
            identity = user.Id;
            if (!user.EmailConfirmed)
            {
                var challenge = await db.Challenges.AsNoTracking().SingleAsync(row => row.UserId == user.Id && row.Purpose == "confirm", Ct);
                var mail = JsonSerializer.Deserialize<PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail)) ?? throw new InvalidDataException("Fictional challenge unavailable.");
                using var confirmation = await client.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), Ct);
                Assert.Equal(HttpStatusCode.NoContent, confirmation.StatusCode);
            }
        }
        using var login = await client.PostAsJsonAsync("identity/login", new IdentityInput(email, password), Ct);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = await login.Content.ReadFromJsonAsync<JsonElement>(Ct);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.GetProperty("accessToken").GetString());
        return identity;
    }

    /// <summary>Reads exact typed service output without including raw tokens/private request content in assertion diagnostics.</summary>
    private static async Task<TOut> PostAsync<TIn, TOut>(HttpClient client, string path, TIn input, JsonTypeInfo<TIn> inputJson, JsonTypeInfo<TOut> outputJson)
    {
        using var response = await client.PostAsJsonAsync(path, input, inputJson, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync(outputJson, Ct) ?? throw new InvalidDataException("Expected household response unavailable.");
    }
    /// <summary>Creates actual client-side standard recipient grants rather than a service-side financial/key fixture.</summary>
    private static HouseholdPublishMembership Publish(HouseholdMembershipStatement membership, SigningRecipientIdentity root,
        HouseholdEpochMaterial epoch, HouseholdCryptography cryptography, string? claim = null)
    {
        var signed = Sign(membership, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement, root, HouseholdMetadataProofs.MembershipType);
        var grants = membership.Members.SelectMany(member => member.Devices).Select(device =>
        {
            var ciphertext = epoch.Grant(cryptography, membership, root, device);
            return new HouseholdEncryptedGrant(epoch.Epoch, device.Id, ciphertext, HouseholdPublicationDigest.Ciphertext(ciphertext));
        }).ToArray();
        return new(membership.Members.Single(member => member.Role == HouseholdRole.Owner).Devices[0].Id, signed, null, claim, grants);
    }
    /// <summary>Fictional proposed roster metadata; the original service validates/signature-checks every effect.</summary>
    private static HouseholdMembershipStatement Membership(Guid room, SigningRecipientIdentity root, string owner, HouseholdEpochMaterial epoch,
        long previous, string head, HouseholdMembershipChange change, HouseholdMemberDefinition[] members, Guid? invitation = null) =>
        new(1, room, Guid.NewGuid(), change, invitation, previous, previous + 1, head, owner, owner, Public(root), epoch.Epoch,
            epoch.KeyCommitment, epoch.AddressKeyCommitment, TimeProvider.System.GetUtcNow().AddMinutes(10).ToUnixTimeSeconds(), members);
    /// <summary>Public-only key DTO.</summary>
    private static HouseholdPublicKeys Public(SigningRecipientIdentity identity) => new(identity.Public.SigningKey, identity.Public.WrappingKey);
    /// <summary>Original fixed standard-signature implementation, not a copied cryptographic algorithm.</summary>
    private static string Sign<T>(T value, JsonTypeInfo<T> json, SigningRecipientIdentity key, string type) =>
        StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(value, json), key, type);
    /// <summary>Public metadata identity without printing a signed body/capability.</summary>
    private static string Hash(string value) => HashBytes(Encoding.UTF8.GetBytes(value));
    /// <summary>Content integrity assertion without exposing confidential assertion operands.</summary>
    private static string HashBytes(byte[] value) => Convert.ToHexStringLower(SHA256.HashData(value));
}
