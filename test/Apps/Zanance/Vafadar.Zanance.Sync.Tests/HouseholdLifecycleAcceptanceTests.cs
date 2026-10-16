using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vafadar.Cryptography;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Sharing;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Sync.Tests;

public sealed partial class HouseholdServiceAcceptanceTests
{
    /// <summary>Actual HTTPS authorization, lost deletion acknowledgement, database rollback, scoped retirement and elapsed retention use the original service.</summary>
    [Fact]
    public async Task Permanent_room_deletion_survives_database_rollback_and_retention_never_serves_expired_ciphertext()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_TEST_DB") ?? throw new InvalidOperationException("An isolated lifecycle database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_LIFECYCLE_TEST_ROOT") ?? throw new InvalidOperationException("An owned lifecycle root is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("zanance_identity_household_lifecycle_d294", settings.Database);
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" }); Assert.True(Path.IsPathFullyQualified(root)); Assert.False(Directory.Exists(root));
        Directory.CreateDirectory(root); await File.WriteAllTextAsync(Path.Combine(root, "fixture.marker"), "Fictional household lifecycle HTTPS qualification v1", Ct);
        await using (var schema = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options))
        {
            await schema.Database.MigrateAsync(Ct); Assert.False(schema.Database.HasPendingModelChanges());
            await schema.Database.ExecuteSqlRawAsync("GRANT USAGE ON SCHEMA public TO zanance_runtime; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO zanance_runtime; GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO zanance_runtime", Ct);
        }
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Service"] = runtime,
            ["Service:Root"] = Path.Combine(root, "service"), ["Service:Household:LocalQualification"] = "true" });
        builder.AddZananceService(); builder.Logging.ClearProviders(); builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        // Run the actual retirement worker explicitly at the reviewed boundaries, without a background race.
        foreach (var descriptor in builder.Services.Where(row => row.ServiceType == typeof(IHostedService) && row.ImplementationType == typeof(PrivateMaintenance)).ToArray()) builder.Services.Remove(descriptor);
        builder.Services.Replace(ServiceDescriptor.Singleton<IHouseholdDeletionLedger>(services => new LostDeletionAcknowledgement(
            new HouseholdDeletionLedger(services.GetRequiredService<ServiceStorage>()))));
        await using var app = builder.Build(); app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct));
            using var owner = LocalSyncHttp.CreateClient(peer, TimeProvider.System); using var stranger = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
            var suffix = Hash(root)[..12];
            var ownerId = await EnrollAsync(owner, app.Services, "household-lifecycle-owner-" + suffix + "@example.invalid");
            var strangerId = await EnrollAsync(stranger, app.Services, "household-lifecycle-other-" + suffix + "@example.invalid");
            using var signing = SigningRecipientIdentity.Create(); using var device = SigningRecipientIdentity.Create(); using var epoch = HouseholdEpochMaterial.Create();
            var crypto = new HouseholdCryptography(TimeProvider.System);
            async Task<(HouseholdSpaceReservation Room, HouseholdCheckpoint Checkpoint, HouseholdPublishMembership Publication)> CreateAsync(HttpClient client, string identity)
            {
                var room = await PostAsync(client, "households/", new HouseholdReserveSpace(Guid.NewGuid(), new('a', 64), Public(signing), Public(device)),
                    HouseholdMetadataJsonContext.Default.HouseholdReserveSpace, HouseholdMetadataJsonContext.Default.HouseholdSpaceReservation);
                var genesis = Membership(room.SpaceId, signing, identity, epoch, 0, Zero, HouseholdMembershipChange.Genesis,
                    [new(identity, HouseholdRole.Owner, Zero, [new(room.DeviceId, Public(device))])]);
                var publication = Publish(genesis, signing, epoch, crypto);
                var checkpoint = await PostAsync(client, $"households/{room.SpaceId}/membership", publication,
                    HouseholdMetadataJsonContext.Default.HouseholdPublishMembership, HouseholdMetadataJsonContext.Default.HouseholdCheckpoint);
                return (room, checkpoint, publication);
            }
            var deleted = await CreateAsync(owner, ownerId); var control = await CreateAsync(stranger, strangerId);
            var path = $"households/{deleted.Room.SpaceId}"; var original = deleted.Checkpoint;
            var audit = await owner.GetFromJsonAsync(path + $"/audit?device={deleted.Room.DeviceId}&after=0&limit=1", HouseholdMetadataJsonContext.Default.HouseholdAuditPage, Ct);
            Assert.NotNull(audit); Assert.Single(audit.Items); Assert.True(audit.Complete); Assert.Equal(1, audit.Through);
            Assert.Equal(deleted.Publication.SignedStatement, audit.Items[0].Evidence.Membership!.SignedStatement);
            _ = await PostAsync(owner, path + "/membership", deleted.Publication, HouseholdMetadataJsonContext.Default.HouseholdPublishMembership, HouseholdMetadataJsonContext.Default.HouseholdCheckpoint);
            var auditRetry = await owner.GetFromJsonAsync(path + $"/audit?device={deleted.Room.DeviceId}&after=1&through=1&limit=1", HouseholdMetadataJsonContext.Default.HouseholdAuditPage, Ct);
            Assert.NotNull(auditRetry); Assert.Empty(auditRetry.Items); Assert.Equal(audit.Through, auditRetry.Through);
            using (var foreignAudit = await stranger.GetAsync(path + $"/audit?device={deleted.Room.DeviceId}&after=0", Ct)) Assert.Equal(HttpStatusCode.NotFound, foreignAudit.StatusCode);
            var declaration = new HouseholdDeleteStatement(1, deleted.Room.SpaceId, Guid.NewGuid(), ownerId, deleted.Room.DeviceId,
                original.MembershipVersion, original.MembershipHead, original.Cursor, original.PublicationHead, TimeProvider.System.GetUtcNow().AddMinutes(5).ToUnixTimeSeconds());
            var signed = Sign(declaration, HouseholdMetadataJsonContext.Default.HouseholdDeleteStatement, signing, HouseholdLifecycleProtocol.DeleteType);
            var request = new HouseholdDeleteRequest(deleted.Room.DeviceId, signed);
            using (var crossOwner = await stranger.PostAsJsonAsync(path + "/deletion", request, HouseholdMetadataJsonContext.Default.HouseholdDeleteRequest, Ct)) Assert.Equal(HttpStatusCode.NotFound, crossOwner.StatusCode);
            var changed = request with { SignedDeclaration = Sign(declaration with { MembershipHead = Zero }, HouseholdMetadataJsonContext.Default.HouseholdDeleteStatement, signing, HouseholdLifecycleProtocol.DeleteType) };
            using (var stale = await owner.PostAsJsonAsync(path + "/deletion", changed, HouseholdMetadataJsonContext.Default.HouseholdDeleteRequest, Ct)) Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            var ledger = (LostDeletionAcknowledgement)app.Services.GetRequiredService<IHouseholdDeletionLedger>(); ledger.CutNext = true;
            using (var lost = await owner.PostAsJsonAsync(path + "/deletion", request, HouseholdMetadataJsonContext.Default.HouseholdDeleteRequest, Ct)) Assert.Equal(HttpStatusCode.ServiceUnavailable, lost.StatusCode);
            var factory = app.Services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
            await using (var db = await factory.CreateDbContextAsync(Ct)) Assert.False((await db.Set<HouseholdSpaceRow>().AsNoTracking().SingleAsync(row => row.Id == deleted.Room.SpaceId, Ct)).Deleted);
            Assert.NotNull(await ledger.ReadAsync(deleted.Room.SpaceId, Ct));
            using (var blocked = await owner.GetAsync(path + $"/grants/1?device={deleted.Room.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.Gone, blocked.StatusCode);
            var receipt = await PostAsync(owner, path + "/deletion", request, HouseholdMetadataJsonContext.Default.HouseholdDeleteRequest, HouseholdMetadataJsonContext.Default.HouseholdDeletionReceipt);
            Assert.Equal(declaration.OperationId, receipt.OperationId); Assert.False(receipt.StorageRetired);
            using (var replacement = await owner.PostAsJsonAsync(path + "/deletion", changed, HouseholdMetadataJsonContext.Default.HouseholdDeleteRequest, Ct)) Assert.Equal(HttpStatusCode.Conflict, replacement.StatusCode);
            await app.Services.GetRequiredService<HouseholdRetirement>().RunOnceAsync(Ct);
            var retired = await owner.GetFromJsonAsync(path + $"/deletion/{declaration.OperationId}", HouseholdMetadataJsonContext.Default.HouseholdDeletionReceipt, Ct);
            Assert.NotNull(retired); Assert.True(retired.StorageRetired); Assert.Equal(receipt.DeclaredAtUnixSeconds, retired.DeclaredAtUnixSeconds);
            using (var foreignReceipt = await stranger.GetAsync(path + $"/deletion/{declaration.OperationId}", Ct)) Assert.Equal(HttpStatusCode.NotFound, foreignReceipt.StatusCode);
            await using (var db = await factory.CreateDbContextAsync(Ct))
            {
                Assert.False(await db.Set<HouseholdMemberRow>().AnyAsync(row => row.SpaceId == deleted.Room.SpaceId, Ct));
                Assert.False(await db.Set<HouseholdDeviceRow>().AnyAsync(row => row.SpaceId == deleted.Room.SpaceId, Ct));
                Assert.False(await db.Set<HouseholdKeyGrantRow>().AnyAsync(row => row.SpaceId == deleted.Room.SpaceId, Ct));
                Assert.False(await db.Set<HouseholdAuditRow>().AnyAsync(row => row.SpaceId == deleted.Room.SpaceId, Ct));
                Assert.Equal(1, await db.Set<HouseholdAuditRow>().CountAsync(row => row.SpaceId == control.Room.SpaceId, Ct));
            }
            // The actual technical host bound is one active room per identity. Exercise
            // subsequent hosting only after the first room has been permanently retired.
            var expired = await CreateAsync(owner, ownerId);
            var expiredPath = $"households/{expired.Room.SpaceId}";
            async Task SetExpiryAsync(int days)
            {
                await using var db = await factory.CreateDbContextAsync(Ct); var expiry = TimeProvider.System.GetUtcNow().AddDays(-days);
                await db.Set<HouseholdSpaceRow>().Where(row => row.Id == expired.Room.SpaceId).ExecuteUpdateAsync(update => update.SetProperty(row => row.HostExpiresAt, expiry), Ct);
            }
            await SetExpiryAsync(29);
            using (var grace = await owner.GetAsync(expiredPath + $"/grants/1?device={expired.Room.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.OK, grace.StatusCode);
            await SetExpiryAsync(30);
            using (var retained = await owner.GetAsync(expiredPath + $"/grants/1?device={expired.Room.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.Forbidden, retained.StatusCode);
            var status = await owner.GetFromJsonAsync(expiredPath + $"/lifecycle?device={expired.Room.DeviceId}", HouseholdMetadataJsonContext.Default.HouseholdLifecycleStatus, Ct);
            Assert.NotNull(status); Assert.Equal(HouseholdRetentionState.Retained, status.Retention);
            await SetExpiryAsync(120);
            using (var due = await owner.GetAsync(expiredPath + $"/grants/1?device={expired.Room.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.Gone, due.StatusCode);
            await app.Services.GetRequiredService<HouseholdRetirement>().RunOnceAsync(Ct);
            Assert.Equal(HouseholdDeletionReason.RetentionEnded, (await ledger.ReadAsync(expired.Room.SpaceId, Ct))!.Reason);
            // Restoring an older database's reversible flags cannot override the independently retained permanent intent.
            await using (var db = await factory.CreateDbContextAsync(Ct))
                await db.Set<HouseholdSpaceRow>().Where(row => row.Id == expired.Room.SpaceId).ExecuteUpdateAsync(update => update.SetProperty(row => row.Deleted, false)
                    .SetProperty(row => row.StorageRetired, false).SetProperty(row => row.HostExpiresAt, (DateTimeOffset?)null), Ct);
            await app.Services.GetRequiredService<HouseholdRetirement>().RunOnceAsync(Ct);
            await using (var db = await factory.CreateDbContextAsync(Ct))
            {
                var restored = await db.Set<HouseholdSpaceRow>().AsNoTracking().SingleAsync(row => row.Id == expired.Room.SpaceId, Ct);
                Assert.True(restored.Deleted && restored.StorageRetired);
            }
            using (var preserved = await stranger.GetAsync($"households/{control.Room.SpaceId}/grants/1?device={control.Room.DeviceId}", Ct)) Assert.Equal(HttpStatusCode.OK, preserved.StatusCode);
            await File.WriteAllTextAsync(Path.Combine(root, "completed-stages.txt"), "actual-https-audit-owner-deletion-lost-response-rollback-retirement-grace-retention-marker-replay-and-unrelated-preservation; not-process-or-full-snapshot-restore-acceptance", Ct);
        }
        finally { await app.StopAsync(Ct); }
    }

    /// <summary>Only this synthetic effect port loses one acknowledgement after the actual immutable file publication.</summary>
    private sealed class LostDeletionAcknowledgement(IHouseholdDeletionLedger original) : IHouseholdDeletionLedger
    {
        /// <summary>Explicit one-shot request failure; the original marker and PostgreSQL rollback remain real.</summary>
        internal bool CutNext { get; set; }
        /// <inheritdoc />
        public Task<HouseholdDeletionMarker?> ReadAsync(Guid space, CancellationToken ct) => original.ReadAsync(space, ct);
        /// <inheritdoc />
        public async Task<HouseholdDeletionMarker> RecordAsync(HouseholdDeletionMarker marker, CancellationToken ct)
        {
            var retained = await original.RecordAsync(marker, ct);
            if (CutNext) { CutNext = false; throw new IOException("Fictional acknowledgement loss after durable marker publication."); }
            return retained;
        }
        /// <inheritdoc />
        public HouseholdDeletionInspection Inspect(int offset, int maximum) => original.Inspect(offset, maximum);
    }
}
