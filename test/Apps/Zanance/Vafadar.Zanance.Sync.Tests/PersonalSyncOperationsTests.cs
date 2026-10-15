using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Sync.Tests;

public sealed partial class PersonalSyncAcceptanceTests
{
    /// <summary>Real owner-wide budgets, immutable replay and cross-worker collection across actual publication commit/rollback.</summary>
    [Fact]
    public async Task Owner_staging_bounds_reclaim_abandoned_work_and_collection_preserves_inflight_pieces()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_DB") ?? throw new InvalidOperationException("An isolated service database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_SYNC_TEST_ROOT") ?? throw new InvalidOperationException("The retained fictional service root is required.");
        await PrepareStandaloneFixtureAsync(connection, root);
        var database = new NpgsqlConnectionStringBuilder(connection); Assert.True(database.Host is "127.0.0.1" or "localhost");
        Assert.StartsWith("zanance_identity_sync_", database.Database); database.Username = "zanance_runtime";
        var scenario = Guid.NewGuid().ToString("N"); var proof = Path.Combine(root, "operations-" + scenario + ".txt");
        void Checkpoint(string name) => File.AppendAllText(proof, name + Environment.NewLine);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Service"] = database.ConnectionString, ["Service:Root"] = Path.Combine(root, "service"),
            ["Service:Sync:PendingRoundsPerOwner"] = "2", ["Service:Sync:StagedBytesPerOwner"] = "4096", ["Service:Sync:BlobBytesPerOwner"] = "8192",
        });
        builder.AddZananceService(); builder.Logging.ClearProviders(); builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        // Only this fixture drives explicit worker passes to observe the exact live publication barrier.
        builder.Services.Remove(builder.Services.Single(item => item.ServiceType == typeof(IHostedService) && item.ImplementationType == typeof(PrivateMaintenance)));
        await using var app = builder.Build(); app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct));
            var email = "operations-" + scenario + "@example.invalid"; var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA9!";
            using (var enrollment = LocalSyncHttp.CreateClient(peer, TimeProvider.System))
            {
                using var registration = await enrollment.PostAsJsonAsync("identity/register", new IdentityInput(email, password), Ct); Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);
                await using var scope = app.Services.CreateAsyncScope(); var service = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                var user = await service.Users.SingleAsync(value => value.Email == email, Ct);
                var challenge = await service.Challenges.AsNoTracking().SingleAsync(value => value.UserId == user.Id && value.Purpose == "confirm", Ct);
                var mail = JsonSerializer.Deserialize<PrivateMail>(scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("identity-mail-v1").Unprotect(challenge.ProtectedMail))!;
                using var confirmation = await enrollment.PostAsJsonAsync("identity/complete", new ChallengeInput(challenge.Id, mail.Token, null), Ct); Assert.Equal(HttpStatusCode.NoContent, confirmation.StatusCode);
            }
            using var client = new Client(Path.Combine(root, "client-operations-" + scenario + ".sqlite"), peer);
            var owner = await client.Transport.SignInAsync(email, password, Ct);
            var device = await client.Transport.RegisterDeviceAsync("Fictitious operations device", Ct);
            var profiles = new List<SyncProfile>();
            for (var index = 0; index < 3; index++) profiles.Add(await client.Transport.CreateProfileAsync(new(device.Id, Guid.NewGuid(), "Fictitious operations " + index, SyncSchema.Hash), Ct));
            var opA = Guid.NewGuid(); var opB = Guid.NewGuid(); var opC = Guid.NewGuid();
            var roundA = await client.Transport.OpenAsync(profiles[0].Id, new(device.Id, opA, SyncSchema.Hash), Ct);
            var roundB = await client.Transport.OpenAsync(profiles[1].Id, new(device.Id, opB, SyncSchema.Hash), Ct);
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await Assert.ThrowsAsync<PersonalSyncHttpException>(() => client.Transport.OpenAsync(profiles[2].Id, new(device.Id, opC, SyncSchema.Hash), Ct))).Status);
            Assert.Equal(roundA.Id, (await client.Transport.OpenAsync(profiles[0].Id, new(device.Id, opA, SyncSchema.Hash), Ct)).Id);
            var account = new Account { Name = "Fictitious bounded original account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 15) };
            await client.Finances.SaveAccountAsync(account, Ct);
            await client.Sync.ConnectAsync(await client.Sync.ReviewAsync(Ct), peer, owner.Id, device.Id, profiles[0], Ct);
            SyncProposal proposal;
            await using (var local = await client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            {
                await local.Database.OpenConnectionAsync(Ct); await using var command = local.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT RecordKey,Body FROM SyncOutbox WHERE TableName='Accounts' AND Body IS NOT NULL ORDER BY Sequence DESC LIMIT 1";
                await using var reader = await command.ExecuteReaderAsync(Ct); Assert.True(await reader.ReadAsync(Ct));
                proposal = new(new("Accounts", reader.GetString(0)), 0, JsonSerializer.Deserialize<JsonElement>(reader.GetString(1)));
            }
            var parts = new List<SyncRoundPart>(); long acceptedBytes = 0; var refused = false;
            for (var index = 0; index < 32; index++)
            {
                var part = new SyncRoundPart(index, [proposal]); var bytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(part, PersonalSyncJsonContext.Default.SyncRoundPart));
                if (acceptedBytes + bytes > 4096)
                {
                    Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await Assert.ThrowsAsync<PersonalSyncHttpException>(() => client.Transport.PutPartAsync(profiles[0].Id, device.Id, roundA.Id, part, Ct))).Status);
                    refused = true; break;
                }
                await client.Transport.PutPartAsync(profiles[0].Id, device.Id, roundA.Id, part, Ct); parts.Add(part); acceptedBytes += bytes;
            }
            Assert.True(refused); Assert.NotEmpty(parts);
            // Exact retry still succeeds at the cap; another space cannot obtain a fresh budget for the same owner.
            await client.Transport.PutPartAsync(profiles[0].Id, device.Id, roundA.Id, parts[0], Ct);
            var second = new SyncRoundPart(0, [proposal]);
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await Assert.ThrowsAsync<PersonalSyncHttpException>(() => client.Transport.PutPartAsync(profiles[1].Id, device.Id, roundB.Id, second, Ct))).Status);
            await client.Transport.AbandonAsync(profiles[0].Id, device.Id, roundA.Id, Ct);
            Assert.Equal(HttpStatusCode.Gone, (await Assert.ThrowsAsync<PersonalSyncHttpException>(() => client.Transport.OpenAsync(profiles[0].Id, new(device.Id, opA, SyncSchema.Hash), Ct))).Status);
            var roundC = await client.Transport.OpenAsync(profiles[2].Id, new(device.Id, opC, SyncSchema.Hash), Ct);
            await client.Transport.PutPartAsync(profiles[1].Id, device.Id, roundB.Id, second, Ct);
            var committed = await client.Transport.CommitAsync(profiles[1].Id, device.Id, roundB.Id,
                new(1, 1, SyncProtocolDigest.Round([SyncProtocolDigest.Part(second)]), []), Ct);
            Assert.True(committed.Committed);
            Checkpoint("owner-wide-pending-and-byte-bounds-preserve-replay-and-abandon-reclaims-only-staging");

            var dataA = RandomNumberGenerator.GetBytes(4096); var dataB = RandomNumberGenerator.GetBytes(2048); var dataC = RandomNumberGenerator.GetBytes(2048);
            var hashA = Convert.ToHexStringLower(SHA256.HashData(dataA)); var hashB = Convert.ToHexStringLower(SHA256.HashData(dataB)); var hashC = Convert.ToHexStringLower(SHA256.HashData(dataC));
            await client.Transport.DeclareBlobAsync(profiles[0].Id, device.Id, hashA, dataA.Length, Ct);
            await client.Transport.DeclareBlobAsync(profiles[1].Id, device.Id, hashB, dataB.Length, Ct);
            await client.Transport.DeclareBlobAsync(profiles[2].Id, device.Id, hashC, dataC.Length, Ct);
            await client.Transport.DeclareBlobAsync(profiles[0].Id, device.Id, hashA, dataA.Length, Ct);
            foreach (var length in new long[] { 0, Core.Ledger.EntryAttachment.MaxBytes + 1, 1024 })
                Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await Assert.ThrowsAsync<PersonalSyncHttpException>(() => client.Transport.DeclareBlobAsync(profiles[2].Id, device.Id, new('f', 64), length, Ct))).Status);
            await client.Transport.PutBlobAsync(profiles[0].Id, device.Id, hashA, 0, dataA, Ct); await client.Transport.CompleteBlobAsync(profiles[0].Id, device.Id, hashA, Ct);
            var storage = app.Services.GetRequiredService<ServiceStorage>(); var pieceRoot = Path.Combine(storage.Root, "sync-pieces");
            var maintenance = new PrivateMaintenance(app.Services.GetRequiredService<IServiceScopeFactory>(), storage, TimeProvider.System, app.Lifetime);
            var factory = app.Services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
            await using (var original = await factory.CreateDbContextAsync(Ct))
            {
                var piece = await original.Set<SyncBlobPieceRow>().SingleAsync(value => value.SpaceId == profiles[0].Id && value.BlobHash == hashA, Ct);
                File.SetLastWriteTimeUtc(Path.Combine(pieceRoot, piece.FileId.ToString("N")), DateTime.UtcNow.AddDays(-2));
            }
            var orphan = Path.Combine(pieceRoot, Guid.NewGuid().ToString("N")); var fresh = Path.Combine(pieceRoot, Guid.NewGuid().ToString("N"));
            var unknown = Path.Combine(pieceRoot, "fictitious-unknown-" + scenario + ".keep");
            await File.WriteAllBytesAsync(orphan, [1, 2, 3], Ct); File.SetLastWriteTimeUtc(orphan, DateTime.UtcNow.AddDays(-2));
            await File.WriteAllBytesAsync(fresh, [4, 5, 6], Ct); await File.WriteAllBytesAsync(unknown, [7, 8, 9], Ct);
            await maintenance.RunOnceAsync(Ct); Assert.False(File.Exists(orphan)); Assert.True(File.Exists(fresh)); Assert.True(File.Exists(unknown));
            Assert.Equal(dataA, await client.Transport.ReadBlobAsync(profiles[0].Id, device.Id, hashA, 0, Ct));
            Checkpoint("collection-removes-only-aged-unmanifested-server-names-and-retains-original-readable-piece");

            foreach (var rollback in new[] { false, true })
            {
                var profile = profiles[rollback ? 2 : 1]; var hash = rollback ? hashC : hashB; var bytes = rollback ? dataC : dataB;
                await using var pending = await factory.CreateDbContextAsync(Ct);
                var actualDevice = await pending.Set<SyncDeviceRow>().AsNoTracking().SingleAsync(value => value.Id == device.Id, Ct);
                var session = await pending.Sessions.AsNoTracking().SingleAsync(value => value.Id == actualDevice.SessionId, Ct);
                var principal = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, owner.Id), new(ServiceHost.SessionClaim, session.Id.ToString("D")),
                    new(ServiceHost.GenerationClaim, session.Generation.ToString(System.Globalization.CultureInfo.InvariantCulture))], "fixture"));
                var scope = await SyncRequestScope.OpenAsync(principal, pending, TimeProvider.System, Ct); Assert.NotNull(scope);
                await using (scope)
                {
                    Assert.NotNull(await scope.DeviceAsync(device.Id, Ct)); var space = await scope.SpaceAsync(profile.Id, device.Id, Ct); Assert.NotNull(space);
                    var blob = await pending.Set<SyncBlobRow>().SingleAsync(value => value.SpaceId == profile.Id && value.Hash == hash, Ct);
                    await new SyncBlobStore(pending, storage, app.Services.GetRequiredService<IDataProtectionProvider>()).PutAsync(scope, space, blob, 0, bytes, Ct);
                    var piece = Assert.Single(pending.ChangeTracker.Entries<SyncBlobPieceRow>()).Entity;
                    var path = Path.Combine(pieceRoot, piece.FileId.ToString("N")); File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-2));
                    var collecting = maintenance.RunOnceAsync(Ct);
                    var blocked = false;
                    for (var attempt = 0; attempt < 50 && !collecting.IsCompleted; attempt++)
                    {
                        await using var observer = await factory.CreateDbContextAsync(Ct); await observer.Database.OpenConnectionAsync(Ct);
                        await using var query = observer.Database.GetDbConnection().CreateCommand();
                        query.CommandText = "SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE datname=current_database() AND lower(wait_event)='advisory' AND query LIKE '%pg_advisory_xact_lock(1685220206, 1)%')";
                        if (await query.ExecuteScalarAsync(Ct) is true) { blocked = true; break; }
                        await Task.Delay(100, Ct);
                    }
                    Assert.True(blocked); Assert.True(File.Exists(path));
                    if (!rollback) await scope.CommitAsync(Ct);
                    await scope.DisposeAsync(); // Actual rollback/commit releases the original shared publication gate.
                    await collecting;
                    Assert.Equal(!rollback, File.Exists(path));
                }
                if (!rollback)
                {
                    await client.Transport.CompleteBlobAsync(profile.Id, device.Id, hash, Ct);
                    Assert.Equal(bytes, await client.Transport.ReadBlobAsync(profile.Id, device.Id, hash, 0, Ct));
                }
                Checkpoint(rollback ? "actual-uncommitted-piece-rollback-allows-aged-orphan-collection" : "actual-publication-commit-blocks-collection-until-manifest-is-durable");
            }
            var interrupted = await client.Transport.OpenAsync(profiles[0].Id, new(device.Id, Guid.NewGuid(), SyncSchema.Hash), Ct);
            await client.Transport.PutPartAsync(profiles[0].Id, device.Id, interrupted.Id, second, Ct);
            Guid expiringSession;
            await using (var expiring = await factory.CreateDbContextAsync(Ct))
            {
                expiringSession = await expiring.Set<SyncDeviceRow>().Where(value => value.Id == device.Id).Select(value => value.SessionId).SingleAsync(Ct);
                // Expire only this independent fictional session; never advance the machine clock or another fixture's session.
                await expiring.Sessions.Where(value => value.Id == expiringSession && value.UserId == owner.Id)
                    .ExecuteUpdateAsync(update => update.SetProperty(value => value.ExpiresAt, TimeProvider.System.GetUtcNow().AddSeconds(-1)), Ct);
            }
            await using (var renewing = await factory.CreateDbContextAsync(Ct))
            {
                await using var renewal = await renewing.Database.BeginTransactionAsync(Ct);
                await SyncRequestScope.LockOwnerAsync(renewing, owner.Id, Ct);
                var waiting = maintenance.RunOnceAsync(Ct); var blocked = false;
                for (var attempt = 0; attempt < 50 && !waiting.IsCompleted; attempt++)
                {
                    await using var observer = await factory.CreateDbContextAsync(Ct); await observer.Database.OpenConnectionAsync(Ct);
                    await using var query = observer.Database.GetDbConnection().CreateCommand();
                    query.CommandText = "SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE datname=current_database() AND lower(wait_event)='advisory' AND query LIKE '%pg_advisory_xact_lock(hashtextextended%')";
                    if (await query.ExecuteScalarAsync(Ct) is true) { blocked = true; break; }
                    await Task.Delay(100, Ct);
                }
                Assert.True(blocked);
                // Publish a valid session-row renewal under the actual owner lock while the worker waits.
                // This isolates the worker's post-lock authority recheck, not framework refresh-token acceptance.
                await renewing.Sessions.Where(value => value.Id == expiringSession && value.UserId == owner.Id)
                    .ExecuteUpdateAsync(update => update.SetProperty(value => value.ExpiresAt, TimeProvider.System.GetUtcNow().AddHours(1)), Ct);
                await renewal.CommitAsync(Ct); await waiting;
                Assert.False((await renewing.Set<SyncDeviceRow>().AsNoTracking().SingleAsync(value => value.Id == device.Id, Ct)).Revoked);
                Assert.False((await renewing.Set<SyncRoundRow>().AsNoTracking().SingleAsync(value => value.Id == interrupted.Id, Ct)).Abandoned);
                Assert.True(await renewing.Set<SyncPartRow>().AnyAsync(value => value.RoundId == interrupted.Id, Ct));
                await renewing.Sessions.Where(value => value.Id == expiringSession && value.UserId == owner.Id)
                    .ExecuteUpdateAsync(update => update.SetProperty(value => value.ExpiresAt, TimeProvider.System.GetUtcNow().AddSeconds(-1)), Ct);
            }
            Checkpoint("worker-rechecks-renewed-session-after-actual-owner-lock-wait-with-pending-parts-intact");
            await maintenance.RunOnceAsync(Ct);
            await using (var retired = await factory.CreateDbContextAsync(Ct))
            {
                Assert.True((await retired.Set<SyncDeviceRow>().SingleAsync(value => value.Id == device.Id, Ct)).Revoked);
                Assert.True((await retired.Set<SyncRoundRow>().SingleAsync(value => value.Id == interrupted.Id, Ct)).Abandoned);
                Assert.True((await retired.Set<SyncRoundRow>().SingleAsync(value => value.Id == roundC.Id, Ct)).Abandoned);
                Assert.False(await retired.Set<SyncPartRow>().AnyAsync(value => value.RoundId == interrupted.Id, Ct));
                var receipt = await retired.Set<SyncRoundRow>().SingleAsync(value => value.Id == roundB.Id, Ct);
                Assert.True(receipt.Committed); Assert.False(receipt.Abandoned); Assert.Equal(committed.Cursor, receipt.CommitCursor);
                Assert.Equal(SyncProtocolDigest.Part(second), (await retired.Set<SyncPartRow>().SingleAsync(value => value.RoundId == roundB.Id, Ct)).Hash);
                Assert.Single(await retired.Set<SyncRecordRow>().Where(value => value.SpaceId == profiles[1].Id).ToArrayAsync(Ct));
                Assert.Single(await retired.Set<SyncEventRow>().Where(value => value.SpaceId == profiles[1].Id).ToArrayAsync(Ct));
            }
            Assert.Equal(HttpStatusCode.Unauthorized, (await Assert.ThrowsAsync<PersonalSyncHttpException>(() => client.Transport.IdentityAsync(Ct))).Status);
            Checkpoint("expired-device-reclaims-only-unpublished-staging-under-owner-lock-and-keeps-committed-receipt-history");
            await client.Transport.SignInAsync(email, password, Ct); password = "";
            var replacement = await client.Transport.RegisterDeviceAsync("Fictitious replacement operations device", Ct);
            var revoke = await client.Transport.OpenAsync(profiles[1].Id, new(replacement.Id, Guid.NewGuid(), SyncSchema.Hash), Ct);
            await client.Transport.PutPartAsync(profiles[1].Id, replacement.Id, revoke.Id, second, Ct);
            await client.Transport.RevokeDeviceAsync(replacement.Id, Ct);
            await using (var revoked = await factory.CreateDbContextAsync(Ct))
            {
                Assert.True((await revoked.Set<SyncRoundRow>().SingleAsync(value => value.Id == revoke.Id, Ct)).Abandoned);
                Assert.False(await revoked.Set<SyncPartRow>().AnyAsync(value => value.RoundId == revoke.Id, Ct));
                Assert.True((await revoked.Set<SyncRoundRow>().SingleAsync(value => value.Id == roundB.Id, Ct)).Committed);
            }
            Checkpoint("explicit-device-revocation-atomically-retires-only-its-unpublished-staging");
            Assert.Single(await client.Finances.GetAccountsAsync(cancellationToken: Ct)); Assert.Empty(await client.Finances.GetEntriesAsync(cancellationToken: Ct));
            Checkpoint("complete-with-original-finances-and-operation-tombstones-preserved");
        }
        finally { await app.StopAsync(CancellationToken.None); }
    }
}
