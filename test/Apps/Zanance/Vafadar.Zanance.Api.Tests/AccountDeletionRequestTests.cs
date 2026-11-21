using System.Net;
using System.Data.Common;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Api.Billing;
using Vafadar.Zanance.Api.Rates;
using Vafadar.Zanance.Api.Sharing;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Authenticated account deletion intake retains one original receipt and never applies deletion itself.</summary>
public sealed class AccountDeletionRequestTests
{
    [Fact]
    public async Task Pending_account_deletion_refunds_only_an_exact_never_dispatched_reservation()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_dispatch_fixture";
        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "analysis-dispatch-retirement"));
        await using var services = host.Services.CreateAsyncScope();
        var factory = services.ServiceProvider.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.Database.MigrateAsync(ct);
        var protection = new EphemeralDataProtectionProvider();
        var time = TimeProvider.System;
        var now = time.GetUtcNow();
        var target = new ServiceUser { UserName = "dispatch-deletion-target@example.invalid" };
        var other = new ServiceUser { UserName = "dispatch-deletion-other@example.invalid" };
        db.Users.AddRange(target, other);
        var targetActor = Actor(target);
        var otherActor = Actor(other);
        await db.SaveChangesAsync(ct);

        var provider = new NoProviderTraffic();
        var store = new AiRequestStore(factory, provider, protection, time);
        var offer = await store.OfferAsync(targetActor, AiPurpose.PeriodExplanation, ct);
        var payload = AiPilotProvider.ReportFixture;
        var submission = new AiSubmission(Guid.NewGuid(), offer, payload);
        var http = new NoProviderHttp();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Analysis:ProviderPilot:openai:ApiKey"] = "fictional-deletion-test-key",
            ["Analysis:ProviderPilot:openai:Project"] = "fixture-deletion-gate",
        }).Build();
        var pilot = new AiPilotProvider(config, http, time);
        await pilot.SelectModelAsync(db, AiPurpose.PeriodExplanation, "gpt-6-luna", "fictional-operator", ct);
        var route = await db.Set<AiPilotRouteRow>().SingleAsync(ct);
        var alreadyQueuedOffer = new AiOfferRow { Id = Guid.NewGuid(), UserId = target.Id, PilotRouteId = route.Id,
            Provider = route.Provider, Model = route.Model!, Purpose = route.Purpose, MaximumUnits = 1, ExpiresAt = now.AddMinutes(5) };
        var alreadyQueued = new AiRequestRow { Id = Guid.NewGuid(), UserId = target.Id, OfferId = alreadyQueuedOffer.Id,
            CreatedAt = now, State = AiRequestState.Pending, GiftedReserved = 1, PilotDispatchStage = AiPilotDispatchStage.Queued,
            PilotCostCeilingMicros = 100, PayloadDigest = AiProtocol.Digest(payload), ProtectedPayload = "Fictional pending payload" };
        var originalReceipt = Guid.NewGuid();
        db.AddRange(alreadyQueuedOffer, alreadyQueued, new AccountDeletionRequest { UserId = target.Id, ReceiptId = originalReceipt, RequestedAt = now });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        Assert.False((await store.AvailabilityAsync(targetActor, ct)).Configured);
        Assert.True((await store.AvailabilityAsync(otherActor, ct)).Configured);
        Assert.Equal("AiAccountDeletionPending",
            (await Assert.ThrowsAsync<AiRefusal>(() => store.OfferAsync(targetActor, offer.Purpose, ct))).Message);
        Assert.Equal("AiAccountDeletionPending",
            (await Assert.ThrowsAsync<AiRefusal>(() => store.SubmitAsync(targetActor, submission, ct))).Message);
        await new AiRequestStore(factory, provider, protection, time, pilot).DrainPilotDispatchesAsync(ct);
        var preserved = await db.Set<AiRequestRow>().AsNoTracking().SingleAsync(ct);
        Assert.Equal(alreadyQueued.Id, preserved.Id);
        Assert.Equal(AiPilotDispatchStage.Queued, preserved.PilotDispatchStage);
        Assert.Equal(AiRequestState.Pending, preserved.State);
        Assert.Empty(await db.Set<AiCreditRow>().ToListAsync(ct));
        Assert.Equal(0, http.Calls);

        var cancellableOffer = new AiOfferRow { Id = Guid.NewGuid(), UserId = target.Id, PilotRouteId = route.Id,
            Provider = route.Provider, Model = route.Model!, Purpose = route.Purpose, MaximumUnits = 2, ExpiresAt = now.AddMinutes(5) };
        var cancellable = new AiRequestRow { Id = Guid.NewGuid(), UserId = target.Id, OfferId = cancellableOffer.Id,
            CreatedAt = now, State = AiRequestState.Pending, GiftedReserved = 2, PilotDispatchStage = AiPilotDispatchStage.Queued,
            PilotCostCeilingMicros = 100, PayloadDigest = AiProtocol.Digest(payload), ProtectedPayload = "Fictional reserved payload" };
        db.AddRange(cancellableOffer, cancellable, new AiCreditRow { Id = Guid.NewGuid(), UserId = target.Id, RequestId = cancellable.Id,
            Kind = AiCreditEvent.Reserve, GiftedAvailable = -2, GiftedReserved = 2, CreatedAt = now });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        Assert.Equal(AccountAnalysisRetirementResult.Pending,
            await AccountAnalysisRetirement.RunAsync(db, originalReceipt, time, ct, releaseUnclaimedReservations: false));
        Assert.False(await db.Set<AiCreditRow>().AnyAsync(row => row.Kind == AiCreditEvent.Refund, ct));
        Assert.Equal(AccountAnalysisRetirementResult.Pending, await AccountAnalysisRetirement.RunAsync(db, originalReceipt, time, ct));
        Assert.Equal(AiRequestState.Refunded, await db.Set<AiRequestRow>().Where(row => row.Id == cancellable.Id).Select(row => row.State).SingleAsync(ct));
        Assert.Equal(AiRequestState.Pending, await db.Set<AiRequestRow>().Where(row => row.Id == alreadyQueued.Id).Select(row => row.State).SingleAsync(ct));
        var refund = await db.Set<AiCreditRow>().AsNoTracking().SingleAsync(row => row.Kind == AiCreditEvent.Refund, ct);
        Assert.Equal(cancellable.Id, refund.RequestId);
        Assert.Equal(2, refund.GiftedAvailable);
        Assert.Equal(-2, refund.GiftedReserved);
        Assert.Equal(AccountAnalysisRetirementResult.Pending, await AccountAnalysisRetirement.RunAsync(db, originalReceipt, time, ct));
        Assert.Equal(refund.Id, (await db.Set<AiCreditRow>().AsNoTracking().SingleAsync(row => row.Kind == AiCreditEvent.Refund, ct)).Id);
        Assert.Equal(2, await db.Set<AiCreditRow>().CountAsync(ct));
        Assert.Equal(0, http.Calls);

        ClaimsPrincipal Actor(ServiceUser owner)
        {
            var session = new DeviceSession { Id = Guid.NewGuid(), UserId = owner.Id, ExpiresAt = now.AddHours(1) };
            db.Sessions.Add(session);
            return new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.NameIdentifier, owner.Id), new(ServiceHost.SessionClaim, session.Id.ToString("D")),
                new(ServiceHost.GenerationClaim, "0"),
            }, "Fictional"));
        }
    }

    private sealed class NoProviderTraffic : IAiProvider
    {
        public AiProviderOffer? Offer(AiPurpose purpose) => new("fixture", "fixture-model", 1);
        public Task<AiProviderOutcome> BeginAsync(string operation, AiOffer offer, AiPayload payload, CancellationToken ct)
            => throw new InvalidOperationException("Provider creation is forbidden in this fixture.");
        public Task<AiProviderOutcome> StatusAsync(string operation, AiOffer offer, CancellationToken ct)
            => throw new InvalidOperationException("Provider status is forbidden in this fixture.");
    }

    private sealed class NoProviderHttp : IHttpClientFactory
    {
        public int Calls { get; private set; }
        public HttpClient CreateClient(string name)
        {
            Calls++;
            throw new InvalidOperationException("External transport is forbidden in this fixture.");
        }
    }

    [Fact]
    public async Task Account_erasure_releases_only_verified_retired_rooms_and_preserves_restore_markers()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_household_fixture";

        await using var host = new IdentityHost(connection.ConnectionString,
            Path.Combine(root, "household-erasure-" + Guid.NewGuid().ToString("N")));
        await using var services = host.Services.CreateAsyncScope();
        var db = services.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var storage = services.ServiceProvider.GetRequiredService<ServiceStorage>();
        var ledger = services.ServiceProvider.GetRequiredService<IHouseholdDeletionLedger>();
        await db.Database.MigrateAsync(ct);
        var target = new ServiceUser { UserName = "household-erasure-target@example.invalid" };
        var other = new ServiceUser { UserName = "household-erasure-other@example.invalid" };
        var receipt = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var retired = new HouseholdSpaceRow { Id = Guid.NewGuid(), OwnerId = target.Id, CreationOperationId = Guid.NewGuid(),
            CreatedAt = now, Deleted = true };
        var active = new HouseholdSpaceRow { Id = Guid.NewGuid(), OwnerId = target.Id, CreationOperationId = Guid.NewGuid(),
            CreatedAt = now, HostExpiresAt = now.AddDays(5) };
        db.Users.AddRange(target, other);
        db.AccountDeletionRequests.Add(new() { UserId = target.Id, ReceiptId = receipt, RequestedAt = now });
        db.AddRange(retired, active);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var marker = new HouseholdDeletionMarker(1, retired.Id, Guid.NewGuid(), HouseholdDeletionLedger.Actor(target.Id),
            new string('a', 64), now.ToUnixTimeSeconds(), HouseholdDeletionReason.Owner);
        await ledger.RecordAsync(marker, ct);
        await services.ServiceProvider.GetRequiredService<HouseholdRetirement>().RunOnceAsync(ct);
        Assert.True((await db.Set<HouseholdSpaceRow>().AsNoTracking().SingleAsync(row => row.Id == retired.Id, ct)).StorageRetired);

        var blocked = await AccountIdentityErasure.CompleteAsync(db, storage, TimeProvider.System, receipt, ct);
        Assert.Equal(new(AccountIdentityErasureStatus.DependenciesRemain, AccountDeletionDependencies.Household), blocked);
        Assert.Equal(2, await db.Set<HouseholdSpaceRow>().CountAsync(ct));
        Assert.Equal(marker, await ledger.ReadAsync(retired.Id, ct));

        // Model the independently reviewed transfer outcome without replaying the app's ownership protocol.
        await db.Set<HouseholdSpaceRow>().Where(row => row.Id == active.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.OwnerId, other.Id), ct);
        Assert.Equal(AccountIdentityErasureStatus.Completed,
            (await AccountIdentityErasure.CompleteAsync(db, storage, TimeProvider.System, receipt, ct)).Status);
        var current = await db.Set<HouseholdSpaceRow>().AsNoTracking().SingleAsync(ct);
        Assert.Equal(active.Id, current.Id);
        Assert.Equal(other.Id, current.OwnerId);
        Assert.False(current.Deleted);
        Assert.Equal(marker, await ledger.ReadAsync(retired.Id, ct));
        Assert.Equal(other.Id, (await db.Users.AsNoTracking().SingleAsync(ct)).Id);
    }

    [Fact]
    public async Task Local_analysis_retirement_removes_only_refused_requests_without_provider_or_credit_effects()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_analysis_fixture";

        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "analysis-retirement-service"));
        await using var services = host.Services.CreateAsyncScope();
        var db = services.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var storage = services.ServiceProvider.GetRequiredService<ServiceStorage>();
        await db.Database.MigrateAsync(ct);
        var target = new ServiceUser { UserName = "analysis-retirement-target@example.invalid" };
        var other = new ServiceUser { UserName = "analysis-retirement-other@example.invalid" };
        var receipt = Guid.NewGuid();
        var otherReceipt = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Users.AddRange(target, other);
        db.AccountDeletionRequests.AddRange(new() { UserId = target.Id, ReceiptId = receipt, RequestedAt = now },
            new() { UserId = other.Id, ReceiptId = otherReceipt, RequestedAt = now });
        var refusedOffer = Offer(target);
        var pendingOffer = Offer(target);
        var unused = Offer(target);
        var unrelated = Offer(other);
        var refused = new AiRequestRow { Id = Guid.NewGuid(), OfferId = refusedOffer.Id, UserId = target.Id,
            CreatedAt = now, PayloadDigest = new string('a', 64), ProtectedPayload = "Fictional refused payload", State = AiRequestState.NotAccepted };
        var pending = new AiRequestRow { Id = Guid.NewGuid(), OfferId = pendingOffer.Id, UserId = target.Id,
            CreatedAt = now, PayloadDigest = new string('b', 64), ProtectedPayload = "Fictional pending payload",
            GiftedReserved = 1, VendorOperationId = "fictional-unresolved-response", State = AiRequestState.Pending };
        db.AddRange(refusedOffer, pendingOffer, unused, unrelated, refused, pending);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        Assert.Equal(AccountAnalysisRetirementResult.UnknownReceipt, await AccountAnalysisRetirement.RunAsync(db, Guid.NewGuid(), TimeProvider.System, ct));
        Assert.Equal(4, await db.Set<AiOfferRow>().CountAsync(ct));
        Assert.Equal(AccountAnalysisRetirementResult.Pending, await AccountAnalysisRetirement.RunAsync(db, receipt, TimeProvider.System, ct));
        var retained = await db.Set<AiRequestRow>().AsNoTracking().SingleAsync(ct);
        Assert.Equal(pending.Id, retained.Id);
        Assert.Equal(AiRequestState.Pending, retained.State);
        Assert.Equal(pending.ProtectedPayload, retained.ProtectedPayload);
        Assert.Equal(pending.VendorOperationId, retained.VendorOperationId);
        Assert.True(await db.Set<AiOfferRow>().AnyAsync(row => row.Id == unrelated.Id, ct));
        Assert.Equal(2, await db.Set<AiOfferRow>().CountAsync(ct));
        Assert.Empty(await db.Set<AiCreditRow>().ToListAsync(ct));
        var result = await AccountIdentityErasure.CompleteAsync(db, storage, TimeProvider.System, receipt, ct);
        Assert.Equal(new(AccountIdentityErasureStatus.DependenciesRemain, AccountDeletionDependencies.Analysis), result);
        Assert.Equal(AccountAnalysisRetirementResult.Retired, await AccountAnalysisRetirement.RunAsync(db, otherReceipt, TimeProvider.System, ct));
        Assert.Equal(pendingOffer.Id, (await db.Set<AiOfferRow>().SingleAsync(ct)).Id);

        AiOfferRow Offer(ServiceUser owner) => new() { Id = Guid.NewGuid(), UserId = owner.Id, MaximumUnits = 1, ExpiresAt = now.AddMinutes(5) };
    }

    [Fact]
    public async Task Restore_reconciliation_binds_a_pre_request_snapshot_to_the_original_account()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        var fixtureRoot = Path.Combine(root, "restore-fence-" + Guid.NewGuid().ToString("N"));
        var currentRoot = Directory.CreateDirectory(Path.Combine(fixtureRoot, "current")).FullName;
        var restoredRoot = Directory.CreateDirectory(Path.Combine(fixtureRoot, "restored")).FullName;
        var currentStorage = new ServiceStorage { Root = currentRoot, LocalMailbox = false };
        var restoredStorage = new ServiceStorage { Root = restoredRoot, LocalMailbox = false };
        connection.Database = "zanance_delete_fence_current_fixture";
        await using var current = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection.ConnectionString).Options);
        connection.Database = "zanance_delete_fence_restored_fixture";
        await using var restored = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection.ConnectionString).Options);
        var receipt = Guid.NewGuid();
        var identity = Guid.NewGuid().ToString("D");
        foreach (var db in new[] { current, restored })
        {
            await db.Database.MigrateAsync(ct);
            db.Users.Add(new() { Id = identity, UserName = "restore-fence-original@example.invalid" });
            if (ReferenceEquals(db, current))
                db.AccountDeletionRequests.Add(new() { UserId = identity, ReceiptId = receipt, RequestedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
        var recreated = new PrivateObject { Id = Guid.NewGuid(), UserId = identity, CreatedAt = DateTimeOffset.UtcNow };
        restored.Objects.Add(recreated);
        await restored.SaveChangesAsync(ct);
        restored.ChangeTracker.Clear();
        Directory.CreateDirectory(Path.Combine(restoredRoot, "objects"));
        var restoredFile = restoredStorage.ObjectPath(recreated.Id);
        await File.WriteAllBytesAsync(restoredFile, new byte[] { 3, 4, 5 }, ct);

        var result = await AccountIdentityErasure.CompleteAsync(current, currentStorage, TimeProvider.System, receipt, ct);
        Assert.Equal(AccountIdentityErasureStatus.Completed, result.Status);
        await AccountDeletionRestoreFence.VerifyDatabaseAsync(current, currentStorage, ct);
        Assert.Equal(1, await AccountDeletionRestoreFence.MergeAsync(currentRoot, restoredRoot, ct));
        var refusal = await Assert.ThrowsAsync<IOException>(() => AccountDeletionRestoreFence.VerifyDatabaseAsync(restored, restoredStorage, ct));
        Assert.Contains("completion is missing", refusal.Message, StringComparison.Ordinal);
        await using var oldHost = new IdentityHost(connection.ConnectionString, restoredRoot);
        var startup = Record.Exception(() => oldHost.CreateClient().Dispose());
        Assert.NotNull(startup);
        Assert.Contains("completion is missing", startup.ToString(), StringComparison.Ordinal);
        Assert.True(await restored.Users.AnyAsync(row => row.Id == identity, ct));
        Assert.Empty(await restored.Set<AccountDeletionCompletion>().ToListAsync(ct));
        var copied = Directory.EnumerateFiles(Path.Combine(restoredRoot, "account-deletions")).Single();
        var marker = await File.ReadAllTextAsync(copied, ct);
        Assert.StartsWith($"2\n{receipt:D}\n", marker, StringComparison.Ordinal);
        Assert.DoesNotContain(identity, marker, StringComparison.Ordinal);
        var conflictingRoot = Directory.CreateDirectory(Path.Combine(fixtureRoot, "conflicting")).FullName;
        var legacyPath = Path.Combine(Directory.CreateDirectory(Path.Combine(conflictingRoot, "account-deletions")).FullName,
            receipt.ToString("N") + ".receipt");
        await File.WriteAllTextAsync(legacyPath, receipt.ToString("D"), ct);
        await Assert.ThrowsAsync<IOException>(() => AccountDeletionRestoreFence.MergeAsync(currentRoot, conflictingRoot, ct));
        Assert.Equal(receipt.ToString("D"), await File.ReadAllTextAsync(legacyPath, ct));
        Assert.True(File.Exists(restoredFile));

        var unrelated = new ServiceUser { UserName = "restore-unrelated@example.invalid" };
        restored.Users.Add(unrelated);
        restored.AccountDeletionRequests.Add(new() { UserId = unrelated.Id, ReceiptId = receipt, RequestedAt = DateTimeOffset.UtcNow });
        await restored.SaveChangesAsync(ct);
        restored.ChangeTracker.Clear();
        Assert.Equal(1, await AccountDeletionRestoreFence.ReconcileAsync(restored, restoredStorage, TimeProvider.System, ct));
        Assert.True(File.Exists(restoredFile));
        Assert.Equal(2, await restored.Users.CountAsync(ct));
        await restored.AccountDeletionRequests.Where(row => row.ReceiptId == receipt).ExecuteDeleteAsync(ct);

        Assert.Equal(0, await AccountDeletionRestoreFence.ReconcileAsync(restored, restoredStorage, TimeProvider.System, ct));
        Assert.False(File.Exists(restoredFile));
        Assert.Equal(unrelated.Id, (await restored.Users.AsNoTracking().SingleAsync(ct)).Id);
        Assert.Equal(receipt, (await restored.Set<AccountDeletionCompletion>().SingleAsync(ct)).ReceiptId);
        await using (var repairedHost = new IdentityHost(connection.ConnectionString, restoredRoot))
        using (var repaired = repairedHost.CreateClient(new() { BaseAddress = new Uri("https://localhost") }))
        using (var response = await repaired.GetAsync("/health/live", ct))
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var unknown = Guid.NewGuid();
        await File.WriteAllTextAsync(Path.Combine(restoredRoot, "account-deletions", unknown.ToString("N") + ".receipt"), unknown.ToString("D"), ct);
        Assert.Equal(1, await AccountDeletionRestoreFence.ReconcileAsync(restored, restoredStorage, TimeProvider.System, ct));
        Assert.True(await restored.Users.AnyAsync(row => row.Id == unrelated.Id, ct));
        Assert.False(await restored.Set<AccountDeletionCompletion>().AnyAsync(row => row.ReceiptId == unknown, ct));
    }

    [Fact]
    public async Task Account_identity_mail_retires_local_copies_and_keeps_an_unconfirmed_claim_pending()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_mail_fixture";

        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "identity-mail-retirement-service"));
        await using var services = host.Services.CreateAsyncScope();
        var db = services.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var storage = services.ServiceProvider.GetRequiredService<ServiceStorage>();
        await db.Database.MigrateAsync(ct);
        var target = new ServiceUser { UserName = "mail-retirement-target@example.invalid" };
        var other = new ServiceUser { UserName = "mail-retirement-other@example.invalid" };
        var receipt = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var original = new IdentityChallenge { Id = Guid.NewGuid(), UserId = target.Id, Purpose = "reset", ExpiresAt = now.AddMinutes(5) };
        var claimed = new IdentityChallenge { Id = Guid.NewGuid(), UserId = target.Id, Purpose = "confirm", ExpiresAt = now.AddMinutes(5),
            LeaseOwner = Guid.NewGuid(), LeaseUntil = now.AddMinutes(1) };
        var unaffected = new IdentityChallenge { Id = Guid.NewGuid(), UserId = other.Id, Purpose = "reset", ExpiresAt = now.AddMinutes(5) };
        db.Users.AddRange(target, other);
        db.AccountDeletionRequests.Add(new() { UserId = target.Id, ReceiptId = receipt, RequestedAt = now });
        db.Challenges.AddRange(original, claimed, unaffected);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        var mailbox = Path.Combine(storage.Root, "mailbox");
        var originalPath = Path.Combine(mailbox, original.Id.ToString("N"));
        var temporaryPath = originalPath + "." + Guid.NewGuid().ToString("N") + ".pending";
        var unownedName = originalPath + ".unrelated.pending";
        var otherPath = Path.Combine(mailbox, unaffected.Id.ToString("N"));
        await File.WriteAllTextAsync(originalPath, "Fictional original", ct);
        await File.WriteAllTextAsync(temporaryPath, "Fictional pending", ct);
        await File.WriteAllTextAsync(unownedName, "Fictional unrelated filename", ct);
        await File.WriteAllTextAsync(otherPath, "Fictional other account", ct);

        Assert.Equal(AccountIdentityMailRetirementResult.Pending, await AccountIdentityMailRetirement.RunAsync(db, storage, receipt, ct));
        Assert.False(File.Exists(originalPath));
        Assert.False(File.Exists(temporaryPath));
        Assert.True(File.Exists(unownedName));
        Assert.Equal("Fictional other account", await File.ReadAllTextAsync(otherPath, ct));
        var retained = await db.Challenges.AsNoTracking().SingleAsync(row => row.UserId == target.Id, ct);
        Assert.Equal(claimed.Id, retained.Id);
        Assert.True(retained.Consumed);
        Assert.False(retained.Delivered);
        Assert.False((await db.Challenges.AsNoTracking().SingleAsync(row => row.UserId == other.Id, ct)).Consumed);
        var finalizer = await AccountIdentityErasure.CompleteAsync(db, storage, TimeProvider.System, receipt, ct);
        Assert.Equal(new(AccountIdentityErasureStatus.DependenciesRemain, AccountDeletionDependencies.IdentityMail), finalizer);

        var expired = new IdentityChallenge { Id = Guid.NewGuid(), UserId = target.Id, Purpose = "reset", Consumed = true,
            ExpiresAt = now.AddDays(-2), LeaseOwner = Guid.NewGuid(), LeaseUntil = now.AddDays(-2) };
        db.Challenges.Add(expired);
        await db.SaveChangesAsync(ct);
        var expiredPending = Path.Combine(mailbox, expired.Id.ToString("N") + "." + expired.LeaseOwner.Value.ToString("N") + ".pending");
        await File.WriteAllTextAsync(expiredPending, "Fictional expired private file", ct);
        using (var maintenance = ActivatorUtilities.CreateInstance<PrivateMaintenance>(host.Services))
            await maintenance.RunOnceAsync(ct);
        Assert.False(File.Exists(expiredPending));
        Assert.False(await db.Challenges.AnyAsync(row => row.Id == expired.Id, ct));
        Assert.True(await db.Challenges.AnyAsync(row => row.Id == claimed.Id, ct));

        // Simulates confirmation of the original claimed delivery, with no real sender or repeat delivery.
        await db.Challenges.Where(row => row.Id == claimed.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.Delivered, true), ct);
        Assert.Equal(AccountIdentityMailRetirementResult.Retired, await AccountIdentityMailRetirement.RunAsync(db, storage, receipt, ct));
        Assert.Equal(unaffected.Id, (await db.Challenges.AsNoTracking().SingleAsync(ct)).Id);
        Assert.Equal(receipt, (await db.AccountDeletionRequests.SingleAsync(ct)).ReceiptId);
    }

    [Fact]
    public async Task Identity_erasure_refuses_unresolved_files_and_revokes_only_the_original_service_account()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_identity_fixture";

        var uploadWait = new OwnerUploadWait();
        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "identity-erasure-service"), uploadWait);
        await using var services = host.Services.CreateAsyncScope();
        var db = services.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var storage = services.ServiceProvider.GetRequiredService<ServiceStorage>();
        var time = services.ServiceProvider.GetRequiredService<TimeProvider>();
        var users = services.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
        await db.Database.MigrateAsync("20261121021759_AccountSyncFileRetirement", ct);
        const string email = "identity-erasure-target@example.invalid";
        const string password = "Synthetic-identity-erasure9!";
        var target = new ServiceUser { UserName = email, Email = email, EmailConfirmed = true };
        var other = new ServiceUser { UserName = "identity-erasure-other@example.invalid" };
        Assert.True((await users.CreateAsync(target, password)).Succeeded);
        db.Users.Add(other);
        var now = DateTimeOffset.UtcNow;
        var receipt = Guid.NewGuid();
        var retired = new PrivateObject { Id = Guid.NewGuid(), UserId = target.Id, CreatedAt = now, Deleted = true, StorageRetired = true };
        var unaffected = new PrivateObject { Id = Guid.NewGuid(), UserId = other.Id, CreatedAt = now };
        db.Objects.AddRange(retired, unaffected);
        db.AccountDeletionRequests.Add(new() { UserId = target.Id, ReceiptId = receipt, RequestedAt = now });
        db.Sessions.Add(new() { Id = Guid.NewGuid(), UserId = other.Id, ExpiresAt = now.AddDays(1) });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        await db.Database.MigrateAsync(ct);
        Assert.False(db.Database.HasPendingModelChanges());

        using var app = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var login = await app.PostAsJsonAsync("/identity/login", new IdentityInput(email, password), ct);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var grant = JsonDocument.Parse(await login.Content.ReadAsStringAsync(ct));
        app.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", grant.RootElement.GetProperty("accessToken").GetString());
        var known = await app.GetFromJsonAsync<AccountDeletionReceipt>("/identity/deletion-request", ct);
        Assert.Equal(receipt, known?.ReceiptId);

        // The database claims retirement, but a restored copy still exists and must prevent final erasure.
        var file = storage.ObjectPath(retired.Id);
        await File.WriteAllBytesAsync(file, new byte[] { 1, 2, 3 }, ct);
        var refused = await AccountIdentityErasure.CompleteAsync(db, storage, time, receipt, ct);
        Assert.Equal(new(AccountIdentityErasureStatus.DependenciesRemain, AccountDeletionDependencies.PrivateObjects), refused);
        Assert.Equal(2, await db.Users.CountAsync(ct));
        Assert.Empty(await db.Set<AccountDeletionCompletion>().ToListAsync(ct));
        Assert.True(File.Exists(file));
        File.Delete(file);

        // The upload already passed HTTP authentication, but is paused before taking the owner lock.
        uploadWait.Arm();
        using var content = new ByteArrayContent(new byte[] { 71, 82, 93 });
        content.Headers.ContentType = new("application/octet-stream");
        var pendingUpload = app.PostAsync("/objects/", content, ct);
        AccountIdentityErasureResult result;
        try
        {
            await uploadWait.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
            result = await AccountIdentityErasure.CompleteAsync(db, storage, time, receipt, ct);
        }
        finally { uploadWait.Release.TrySetResult(); }
        using var refusedUpload = await pendingUpload;
        Assert.Equal(HttpStatusCode.Unauthorized, refusedUpload.StatusCode);
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(storage.Root, "objects")));
        Assert.Equal(AccountIdentityErasureStatus.Completed, result.Status);
        Assert.Equal(other.Id, (await db.Users.AsNoTracking().SingleAsync(ct)).Id);
        Assert.Equal(other.Id, (await db.Sessions.AsNoTracking().SingleAsync(ct)).UserId);
        Assert.Equal(unaffected.Id, (await db.Objects.AsNoTracking().SingleAsync(ct)).Id);
        Assert.Empty(await db.AccountDeletionRequests.ToListAsync(ct));
        var completion = await db.Set<AccountDeletionCompletion>().AsNoTracking().SingleAsync(ct);
        Assert.Equal(receipt, completion.ReceiptId);
        Assert.Equal(result, await AccountIdentityErasure.CompleteAsync(db, storage, time, receipt, ct));
        Assert.Equal(AccountIdentityErasureStatus.UnknownReceipt,
            (await AccountIdentityErasure.CompleteAsync(db, storage, time, Guid.NewGuid(), ct)).Status);
        Assert.Equal(completion.CompletedAt, (await db.Set<AccountDeletionCompletion>().AsNoTracking().SingleAsync(ct)).CompletedAt);
        using var retiredSession = await app.GetAsync("/identity/deletion-request", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, retiredSession.StatusCode);
    }

    [Fact]
    public async Task Personal_sync_retirement_cascades_original_rows_and_resumes_exact_queued_pieces()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_sync_fixture";
        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "sync-retirement-service"));
        await using var services = host.Services.CreateAsyncScope();
        var db = services.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var storage = services.ServiceProvider.GetRequiredService<ServiceStorage>();
        await db.Database.MigrateAsync("20261121013816_AccountDeletionRequests", ct);
        var target = new ServiceUser { UserName = "sync-retirement-target@example.invalid" };
        var other = new ServiceUser { UserName = "sync-retirement-other@example.invalid" };
        var receipt = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Users.AddRange(target, other);
        db.AccountDeletionRequests.Add(new() { UserId = target.Id, ReceiptId = receipt, RequestedAt = now });
        var targetPiece = AddProfile(target);
        var otherPiece = AddProfile(other);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        await db.Database.MigrateAsync(ct);
        Assert.False(db.Database.HasPendingModelChanges());

        var pieceRoot = Directory.CreateDirectory(Path.Combine(storage.Root, "sync-pieces")).FullName;
        var targetPath = Path.Combine(pieceRoot, targetPiece.FileId.ToString("N"));
        var otherPath = Path.Combine(pieceRoot, otherPiece.FileId.ToString("N"));
        Directory.CreateDirectory(targetPath);
        await File.WriteAllBytesAsync(otherPath, new byte[] { 5, 6, 7 }, ct);
        try
        {
            Assert.Equal(AccountPersonalSyncRetirementResult.UnknownReceipt,
                await AccountPersonalSyncRetirement.RunAsync(db, storage, Guid.NewGuid(), ct));
            Assert.Equal(2, await db.Set<SyncBlobPieceRow>().CountAsync(ct));
            Assert.Equal(AccountPersonalSyncRetirementResult.Pending,
                await AccountPersonalSyncRetirement.RunAsync(db, storage, receipt, ct));
            Assert.Equal(otherPiece.FileId, (await db.Set<SyncBlobPieceRow>().SingleAsync(ct)).FileId);
            Assert.Equal(other.Id, (await db.Set<SyncDeviceRow>().SingleAsync(ct)).UserId);
            Assert.Equal(otherPiece.SpaceId, (await db.Set<PersonalMarketSourceRow>().SingleAsync(ct)).ProfileId);
            var pending = await db.Set<AccountSyncFileRetirement>().SingleAsync(ct);
            Assert.Equal(receipt, pending.ReceiptId);
            Assert.Equal(targetPiece.FileId, pending.FileId);
            Assert.Equal(AccountDeletionDependencies.PersonalSync, await AccountDeletionInventory.InspectAsync(db, receipt, ct));
            Assert.True(Directory.Exists(targetPath));
        }
        finally { Directory.Delete(targetPath); }

        await File.WriteAllBytesAsync(targetPath, new byte[] { 1, 2, 3 }, ct);
        Assert.Equal(AccountPersonalSyncRetirementResult.Retired,
            await AccountPersonalSyncRetirement.RunAsync(db, storage, receipt, ct));
        Assert.False(File.Exists(targetPath));
        Assert.Equal(new byte[] { 5, 6, 7 }, await File.ReadAllBytesAsync(otherPath, ct));
        Assert.Empty(await db.Set<AccountSyncFileRetirement>().ToListAsync(ct));
        Assert.Equal(2, await db.Users.CountAsync(ct));
        Assert.Equal(receipt, (await db.AccountDeletionRequests.SingleAsync(ct)).ReceiptId);

        SyncBlobPieceRow AddProfile(ServiceUser owner)
        {
            var id = Guid.NewGuid();
            var device = new SyncDeviceRow { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), UserId = owner.Id };
            var hash = new string('a', 64);
            var piece = new SyncBlobPieceRow { SpaceId = id, BlobHash = hash, FileId = Guid.NewGuid(), Length = 3, Hash = hash };
            db.AddRange(device,
                new SyncSpaceRow { Id = id, UserId = owner.Id, BootstrapDeviceId = device.Id },
                new SyncBlobRow { SpaceId = id, Hash = hash, Length = 3, Parts = 1 },
                piece,
                new PersonalMarketSourceRow { ProfileId = id, DocumentId = Guid.NewGuid(), Digest = hash,
                    ProtectedDocument = "Fictional protected payload", Bytes = 3 });
            return piece;
        }
    }

    [Fact]
    public async Task Account_objects_remain_hidden_after_io_failure_and_resume_only_the_original_retirement()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_objects_fixture";

        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "object-retirement-service"));
        await using var fixture = host.Services.CreateAsyncScope();
        var db = fixture.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var storage = fixture.ServiceProvider.GetRequiredService<ServiceStorage>();
        await db.Database.MigrateAsync(ct);
        var target = new ServiceUser { UserName = "objects-retirement-target@example.invalid" };
        var other = new ServiceUser { UserName = "objects-retirement-other@example.invalid" };
        var receipt = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var original = new PrivateObject { Id = Guid.NewGuid(), UserId = target.Id, CreatedAt = now };
        var blocked = new PrivateObject { Id = Guid.NewGuid(), UserId = target.Id, CreatedAt = now.AddSeconds(1) };
        var unrelated = new PrivateObject { Id = Guid.NewGuid(), UserId = other.Id, CreatedAt = now };
        db.Users.AddRange(target, other);
        db.AccountDeletionRequests.Add(new() { UserId = target.Id, ReceiptId = receipt, RequestedAt = now });
        db.Objects.AddRange(original, blocked, unrelated);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        await File.WriteAllBytesAsync(storage.ObjectPath(original.Id), new byte[] { 1, 2, 3 }, ct);
        await File.WriteAllBytesAsync(storage.ObjectPath(unrelated.Id), new byte[] { 4, 5, 6 }, ct);
        var blockedPath = storage.ObjectPath(blocked.Id);
        Directory.CreateDirectory(blockedPath);
        try
        {
            Assert.Equal(AccountPrivateObjectRetirementResult.UnknownReceipt,
                await AccountPrivateObjectRetirement.RunAsync(db, storage, Guid.NewGuid(), ct));
            Assert.Equal(3, await db.Objects.CountAsync(row => !row.Deleted, ct));
            Assert.Equal(AccountPrivateObjectRetirementResult.Pending,
                await AccountPrivateObjectRetirement.RunAsync(db, storage, receipt, ct));
            Assert.True(await db.Objects.Where(row => row.UserId == target.Id).AllAsync(row => row.Deleted, ct));
            Assert.True((await db.Objects.AsNoTracking().SingleAsync(row => row.Id == original.Id, ct)).StorageRetired);
            Assert.False((await db.Objects.AsNoTracking().SingleAsync(row => row.Id == blocked.Id, ct)).StorageRetired);
            Assert.False(File.Exists(storage.ObjectPath(original.Id)));
        }
        finally { Directory.Delete(blockedPath); }
        // The blocked directory stood in for an interrupted file operation. The real original file can now retire.
        await File.WriteAllBytesAsync(blockedPath, new byte[] { 7, 8, 9 }, ct);
        Assert.Equal(AccountPrivateObjectRetirementResult.Retired,
            await AccountPrivateObjectRetirement.RunAsync(db, storage, receipt, ct));
        Assert.False(File.Exists(blockedPath));
        Assert.True(await db.Objects.Where(row => row.UserId == target.Id).AllAsync(row => row.Deleted && row.StorageRetired, ct));
        Assert.False((await db.Objects.AsNoTracking().SingleAsync(row => row.Id == unrelated.Id, ct)).Deleted);
        Assert.Equal(new byte[] { 4, 5, 6 }, await File.ReadAllBytesAsync(storage.ObjectPath(unrelated.Id), ct));
        Assert.Equal(2, await db.Users.CountAsync(ct));
        Assert.Equal(receipt, (await db.AccountDeletionRequests.SingleAsync(ct)).ReceiptId);
    }

    [Fact]
    public async Task Deletion_inventory_scopes_each_record_group_to_the_original_receipt_without_mutation()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_inventory_fixture";

        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "inventory-service"));
        await using var fixture = host.Services.CreateAsyncScope();
        var db = fixture.ServiceProvider.GetRequiredService<ServiceDbContext>();
        await db.Database.MigrateAsync(ct);
        var target = new ServiceUser { UserName = "inventory-target@example.invalid" };
        var other = new ServiceUser { UserName = "inventory-empty@example.invalid" };
        var sharedOwner = new ServiceUser { UserName = "inventory-current-household-owner@example.invalid" };
        var targetReceipt = Guid.NewGuid();
        var otherReceipt = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Users.AddRange(target, other, sharedOwner);
        db.AccountDeletionRequests.AddRange(
            new() { UserId = target.Id, ReceiptId = targetReceipt, RequestedAt = now },
            new() { UserId = other.Id, ReceiptId = otherReceipt, RequestedAt = now });
        var space = new HouseholdSpaceRow { Id = Guid.NewGuid(), OwnerId = sharedOwner.Id };
        db.AddRange(
            new DeviceSession { Id = Guid.NewGuid(), UserId = target.Id, ExpiresAt = now.AddDays(1) },
            new IdentityChallenge { Id = Guid.NewGuid(), UserId = target.Id, Purpose = "reset", ExpiresAt = now.AddMinutes(5) },
            new PrivateObject { Id = Guid.NewGuid(), UserId = target.Id, CreatedAt = now },
            new SyncSpaceRow { Id = Guid.NewGuid(), UserId = target.Id },
            space,
            new HouseholdMembershipEventRow { SpaceId = space.Id, Version = 1, ActorId = target.Id, CreatedAt = now },
            new BillingCustomerRow { Id = Guid.NewGuid(), AccountToken = Guid.NewGuid(), CreatedAt = now,
                RegistrationIdentityHash = BillingAccountBinding.Identity(target.Id) },
            new AiOfferRow { Id = Guid.NewGuid(), UserId = target.Id, MaximumUnits = 1, ExpiresAt = now.AddMinutes(10) });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        var all = AccountDeletionDependencies.Sessions | AccountDeletionDependencies.IdentityMail |
            AccountDeletionDependencies.PrivateObjects | AccountDeletionDependencies.PersonalSync |
            AccountDeletionDependencies.Household | AccountDeletionDependencies.Billing | AccountDeletionDependencies.Analysis;
        Assert.Equal(all, await AccountDeletionInventory.InspectAsync(db, targetReceipt, ct));
        Assert.Equal(AccountDeletionDependencies.None, await AccountDeletionInventory.InspectAsync(db, otherReceipt, ct));
        Assert.Null(await AccountDeletionInventory.InspectAsync(db, Guid.NewGuid(), ct));
        Assert.Equal(3, await db.Users.CountAsync(ct));
        Assert.Equal(2, await db.AccountDeletionRequests.CountAsync(ct));
        Assert.False((await db.Sessions.SingleAsync(ct)).Revoked);
        Assert.False((await db.Objects.SingleAsync(ct)).Deleted);
        Assert.False((await db.Set<HouseholdSpaceRow>().SingleAsync(ct)).Deleted);
    }

    [Fact]
    public async Task Web_deletion_requires_fresh_proof_and_shares_the_pending_receipt_with_the_app()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        connection.Database = "zanance_delete_web_fixture";

        await using var host = new IdentityHost(connection.ConnectionString, Path.Combine(root, "web-service"));
        await using var fixture = host.Services.CreateAsyncScope();
        var db = fixture.ServiceProvider.GetRequiredService<ServiceDbContext>();
        await db.Database.MigrateAsync(ct);
        var users = fixture.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
        const string email = "fictional-web-deletion@example.invalid";
        const string password = "Synthetic-web-deletion9!";
        var owner = new ServiceUser { UserName = email, Email = email, EmailConfirmed = true };
        Assert.True((await users.CreateAsync(owner, password)).Succeeded);

        const string page = "/identity/delete-account";
        using var browser = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var firstPage = await browser.GetAsync(page, ct);
        Assert.Equal(HttpStatusCode.OK, firstPage.StatusCode);
        Assert.True(firstPage.Headers.CacheControl?.NoStore);
        Assert.Contains("frame-ancestors 'none'", firstPage.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
        var html = await firstPage.Content.ReadAsStringAsync(ct);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"", RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1)).Groups[1].Value;
        Assert.NotEmpty(token);
        using var rejectedCsrf = await browser.PostAsync(page, Form(email, password, null), ct);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedCsrf.StatusCode);
        using var wrong = await browser.PostAsync(page, Form(email, "Incorrect-fictional9!", token), ct);
        using var unknown = await browser.PostAsync(page, Form("unknown@example.invalid", "Incorrect-fictional9!", token), ct);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(wrong.StatusCode, unknown.StatusCode);
        Assert.Empty(await db.AccountDeletionRequests.ToListAsync(ct));

        using var accepted = await browser.PostAsync(page, Form(email, password, token), ct);
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        Assert.True(accepted.Headers.CacheControl?.NoStore);
        var pending = await db.AccountDeletionRequests.AsNoTracking().SingleAsync(ct);
        var result = await accepted.Content.ReadAsStringAsync(ct);
        Assert.Contains(pending.ReceiptId.ToString("D"), result, StringComparison.Ordinal);
        Assert.DoesNotContain(email, result, StringComparison.Ordinal);
        Assert.DoesNotContain(password, result, StringComparison.Ordinal);
        Assert.Equal(owner.Id, pending.UserId);
        Assert.Empty(await db.Sessions.ToListAsync(ct));

        using var app = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var login = await app.PostAsJsonAsync("/identity/login", new IdentityInput(email, password), ct);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var grant = JsonDocument.Parse(await login.Content.ReadAsStringAsync(ct));
        app.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", grant.RootElement.GetProperty("accessToken").GetString());
        var receipt = await app.GetFromJsonAsync<AccountDeletionReceipt>("/identity/deletion-request", ct);
        Assert.Equal(new AccountDeletionReceipt(pending.ReceiptId, pending.RequestedAt), receipt);

        static FormUrlEncodedContent Form(string email, string password, string? token)
        {
            var fields = new Dictionary<string, string> { ["email"] = email, ["password"] = password };
            if (token is not null) fields["__RequestVerificationToken"] = token;
            return new(fields);
        }
    }

    /// <summary>The additive schema preserves existing accounts and accepts only their own reauthenticated request.</summary>
    [Fact]
    public async Task Deletion_request_survives_concurrent_retry_and_keeps_other_accounts_separate()
    {
        var configured = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_DB");
        Assert.SkipWhen(configured is null, "Requires the fresh fictional deletion request database.");
        var connection = new NpgsqlConnectionStringBuilder(configured!);
        Assert.Equal("127.0.0.1", connection.Host);
        Assert.Equal(55489, connection.Port);
        Assert.Equal("zanance_delete_request_fixture", connection.Database);
        var root = Environment.GetEnvironmentVariable("ZANANCE_DELETE_REQUEST_FIXTURE_ROOT");
        Assert.False(string.IsNullOrWhiteSpace(root));
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("Zanance fictional deletion request fixture", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));

        await using var host = new IdentityHost(configured!, Path.Combine(root, "service"));
        await using var fixture = host.Services.CreateAsyncScope();
        var db = fixture.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var users = fixture.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
        const string previous = "20261120003818_AiPilotCreateRefusal";
        await db.Database.MigrateAsync(previous, ct);
        const string password = "Fictional-Deletion-Request9!";
        var owner = await SeedAsync("request-owner@example.invalid");
        var other = await SeedAsync("request-other@example.invalid");
        await db.Database.MigrateAsync(ct);
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(2, await db.Users.CountAsync(ct));
        Assert.Empty(await db.AccountDeletionRequests.ToListAsync(ct));

        using var unauthenticated = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var ownerHttp = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var otherHttp = host.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        const string route = "/identity/deletion-request";
        using var noSession = await unauthenticated.PostAsJsonAsync(route, new AccountDeletionInput(password), ct);
        Assert.Equal(HttpStatusCode.Unauthorized, noSession.StatusCode);
        await LoginAsync(ownerHttp, owner);
        await LoginAsync(otherHttp, other);
        using var originalStatus = await ownerHttp.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.NoContent, originalStatus.StatusCode);
        using var wrongPassword = await ownerHttp.PostAsJsonAsync(route, new AccountDeletionInput("Fictional-Incorrect-Password9!"), ct);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Empty(await db.AccountDeletionRequests.ToListAsync(ct));

        var results = await Task.WhenAll(RequestAsync(), RequestAsync());
        Assert.Equal(results[0], results[1]);
        Assert.NotEqual(Guid.Empty, results[0].ReceiptId);
        using var status = await ownerHttp.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.True(status.Headers.CacheControl?.NoStore);
        Assert.Equal(results[0], await status.Content.ReadFromJsonAsync<AccountDeletionReceipt>(ct));
        using var otherStatus = await otherHttp.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.NoContent, otherStatus.StatusCode);
        var recorded = Assert.Single(await db.AccountDeletionRequests.AsNoTracking().ToListAsync(ct));
        Assert.Equal(owner.Id, recorded.UserId);
        Assert.Equal(results[0].ReceiptId, recorded.ReceiptId);
        Assert.Equal(results[0].RequestedAt, recorded.RequestedAt);
        Assert.Equal(2, await db.Users.CountAsync(ct));
        Assert.Equal(2, await db.Sessions.CountAsync(row => !row.Revoked, ct));
        Assert.Empty(await db.Challenges.ToListAsync(ct));

        async Task<ServiceUser> SeedAsync(string email)
        {
            var user = new ServiceUser { UserName = email, Email = email, EmailConfirmed = true };
            Assert.True((await users.CreateAsync(user, password)).Succeeded);
            return user;
        }

        async Task LoginAsync(HttpClient http, ServiceUser user)
        {
            using var response = await http.PostAsJsonAsync("/identity/login", new IdentityInput(user.Email!, password), ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var grant = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", grant.GetProperty("accessToken").GetString());
        }

        async Task<AccountDeletionReceipt> RequestAsync()
        {
            using var response = await ownerHttp.PostAsJsonAsync(route, new AccountDeletionInput(password), ct);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
            return Assert.IsType<AccountDeletionReceipt>(await response.Content.ReadFromJsonAsync<AccountDeletionReceipt>(ct));
        }
    }

    private sealed class OwnerUploadWait : DbCommandInterceptor
    {
        private int armed;
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal void Arm() => Interlocked.Exchange(ref armed, 1);

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("SELECT pg_advisory_xact_lock(hashtextextended(", StringComparison.Ordinal) &&
                Interlocked.Exchange(ref armed, 0) == 1)
            {
                Entered.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }

    private sealed class IdentityHost(string connection, string root, OwnerUploadWait? uploadWait = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Integration");
            builder.UseSetting("ConnectionStrings:Service", connection);
            builder.UseSetting("Service:Root", root);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                if (uploadWait is not null)
                    services.ConfigureDbContext<ServiceDbContext>(options => options.AddInterceptors(uploadWait));
            });
        }
    }
}
