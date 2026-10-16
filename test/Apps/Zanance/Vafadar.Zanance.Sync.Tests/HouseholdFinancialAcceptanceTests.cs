using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
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

namespace Vafadar.Zanance.Sync.Tests;

public sealed partial class HouseholdServiceAcceptanceTests
{
    private sealed class HouseholdFixtureSessions : IPersonalSyncTokenStore
    {
        private readonly Dictionary<string, PersonalSyncSession> sessions = [];
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct) => Task.FromResult(sessions.GetValueOrDefault(peer));
        public Task WriteAsync(string peer, PersonalSyncSession session, CancellationToken ct) { sessions[peer] = session; return Task.CompletedTask; }
        public Task RemoveAsync(string peer, CancellationToken ct) { sessions.Remove(peer); return Task.CompletedTask; }
    }

    /// <summary>Original SQLite capture/apply, exact multi-currency money, refunds and a multi-piece attachment cross actual HTTPS only inside client ciphertext.</summary>
    [Fact]
    public async Task Actual_original_financial_capture_and_apply_use_encrypted_household_transport_without_readable_fallback()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_TEST_DB") ?? throw new InvalidOperationException("An isolated household database is required.");
        var root = Environment.GetEnvironmentVariable("ZANANCE_HOUSEHOLD_FINANCIAL_TEST_ROOT") ?? throw new InvalidOperationException("An owned financial qualification root is required.");
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_household_[a-z0-9_]+$", settings.Database ?? ""); Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" });
        Assert.True(Path.IsPathFullyQualified(root)); Assert.False(Directory.Exists(root)); Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "fixture.marker"), "Zanance fictional encrypted original-finance qualification v1", Ct);
        void Stage(string value) => File.AppendAllText(Path.Combine(root, "completed-stages.txt"), value + Environment.NewLine);
        await using (var schema = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options))
        {
            await schema.Database.MigrateAsync(Ct); Assert.False(schema.Database.HasPendingModelChanges());
            await schema.Database.ExecuteSqlRawAsync("GRANT USAGE ON SCHEMA public TO zanance_runtime; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO zanance_runtime; GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO zanance_runtime", Ct);
        }
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "zanance_runtime" }.ConnectionString;
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Service"] = runtime,
            ["Service:Root"] = Path.Combine(root, "service"), ["Service:Household:LocalQualification"] = "true" });
        builder.AddZananceService(); builder.Logging.ClearProviders(); builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        await using var app = builder.Build(); var readableFinancialRequests = 0;
        app.Use(async (http, next) =>
        {
            if (http.Request.Path.StartsWithSegments("/sync/profiles")) Interlocked.Increment(ref readableFinancialRequests);
            await next(http);
        });
        app.UseZananceService(); app.MapZananceService(); await app.StartAsync(Ct);
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var peer = new LocalSyncPeer(address, await File.ReadAllTextAsync(Path.Combine(root, "service", "local-tls.fingerprint"), Ct));
            using var enrollment = LocalSyncHttp.CreateClient(peer, TimeProvider.System);
            var ownerId = await EnrollAsync(enrollment, app.Services, "household-financial-owner@example.invalid");
            var memberId = await EnrollAsync(enrollment, app.Services, "household-financial-member@example.invalid");
            var ownerSessions = new HouseholdFixtureSessions();
            using var ownerTransport = new HouseholdServiceTransport(peer, ownerSessions);
            using var memberTransport = new HouseholdServiceTransport(peer, new HouseholdFixtureSessions());
            Assert.Equal(ownerId, (await ownerTransport.SignInAsync("household-financial-owner@example.invalid", "Fictitious-Household-Password9!", Ct)).Id);
            Assert.Equal(memberId, (await memberTransport.SignInAsync("household-financial-member@example.invalid", "Fictitious-Household-Password9!", Ct)).Id);
            using var rootKey = SigningRecipientIdentity.Create(); using var ownerKey = SigningRecipientIdentity.Create(); using var memberKey = SigningRecipientIdentity.Create();
            using var epoch1 = HouseholdEpochMaterial.Create(); var crypto = new HouseholdCryptography(TimeProvider.System);
            var reservation = await ownerTransport.ReserveAsync(new(Guid.NewGuid(), HouseholdFinancialSchema.Hash, Public(rootKey), Public(ownerKey)), Ct);
            var room = reservation.SpaceId;
            var genesis = Membership(room, rootKey, ownerId, epoch1, 0, Zero, HouseholdMembershipChange.Genesis,
                [new(ownerId, HouseholdRole.Owner, Zero, [new(reservation.DeviceId, Public(ownerKey))])]);
            var initial = Publish(genesis, rootKey, epoch1, crypto);
            var checkpoint = await ownerTransport.PublishMembershipAsync(room, initial, Ct);
            var genesisTrusted = HouseholdTrustChain.Genesis(room, rootKey.Public, new(initial.SignedStatement, null, null, HouseholdPublicationDigest.Grants(initial.Grants)));
            using var source = new ServiceCollection().AddSingleton(TimeProvider.System).AddZananceData(Path.Combine(root, "source.sqlite")).BuildServiceProvider();
            source.MigrateLocalDatabase<ZananceDbContext>(); var finances = source.GetRequiredService<ZananceStore>(); var sourceSync = source.GetRequiredService<PersonalSyncStore>();
            await finances.GetSettingsAsync(Ct);
            var cash = new Account { Name = "Fictitious shared EUR", CurrencyCode = "EUR", OpeningBalance = 100_000, OpeningDate = new(2026, 10, 1) };
            var foreign = new Account { Name = "Fictitious shared KWD", CurrencyCode = "KWD", OpeningBalance = 123_456, OpeningDate = cash.OpeningDate };
            await finances.SaveAccountAsync(cash, Ct); await finances.SaveAccountAsync(foreign, Ct);
            var date = new DateOnly(2026, 10, 16);
            var expense = new LedgerEntry { AccountId = cash.Id, Kind = EntryKind.Expense, Amount = 1234, Date = date, Title = "Fictitious encrypted household purchase" };
            var transfer = new LedgerEntry { AccountId = cash.Id, ToAccountId = foreign.Id, Kind = EntryKind.Transfer, Amount = 1001, ToAmount = 347, Date = date, Title = "Fictitious explicit currency transfer" };
            var refund = new LedgerEntry { AccountId = cash.Id, Kind = EntryKind.Refund, Amount = 500, RefundOfId = expense.Id, Date = date, Title = "Fictitious linked refund" };
            Assert.True((await finances.SaveEntryAsync(expense, Ct)).Succeeded); Assert.True((await finances.SaveEntryAsync(transfer, Ct)).Succeeded); Assert.True((await finances.SaveEntryAsync(refund, Ct)).Succeeded);
            var originalBytes = RandomNumberGenerator.GetBytes(2 * 1024 * 1024 + 140_123); var blobHash = HashBytes(originalBytes);
            var attachment = new EntryAttachment { EntryId = expense.Id, FileName = "fictitious-bounded-receipt.pdf", ContentType = "application/pdf", Data = originalBytes };
            await finances.AddAttachmentAsync(attachment, Ct);
            await sourceSync.ConnectAsync(await sourceSync.ReviewAsync(Ct), peer, ownerId, reservation.DeviceId,
                new(room, "Fictitious household", HouseholdFinancialSchema.Hash, 0, true), Ct, transportKind: ProfileSynchronizationKind.Household);
            using (var readable = new PersonalSyncTransport(peer, ownerSessions))
                await Assert.ThrowsAsync<InvalidOperationException>(() => source.GetRequiredService<PersonalSyncCoordinator>().SynchronizeAsync(readable, Ct));
            Assert.Equal(0, readableFinancialRequests);
            var sourceWork = await sourceSync.CaptureRoundAsync(peer, ownerId, Ct, ProfileSynchronizationKind.Household);
            using var ownerReplica = new HouseholdFinancialReplica(room); using var recipientReplica = new HouseholdFinancialReplica(room);
            var first = ownerReplica.Prepare(genesisTrusted, epoch1, reservation.DeviceId, ownerKey, sourceWork.Operation, Guid.NewGuid(),
                sourceWork.Parts.SelectMany(part => part.Rows).ToArray(), sourceWork.Resolutions, new Dictionary<string, byte[]> { [blobHash] = originalBytes }, crypto);
            Assert.True(first.Plan.Succeeded); Assert.True(first.Parts.Count > 16);
            var firstRound = await UploadAsync(ownerTransport, room, first);
            var firstEvents = await ReadWholePublicationAsync(ownerTransport, room, reservation.DeviceId, 0, 1);
            var firstReceipt = await ownerTransport.ReceiptAsync(room, reservation.DeviceId, firstRound.Id, Ct);
            ownerReplica.Apply(genesisTrusted, epoch1, firstReceipt, firstEvents, crypto);
            await ApplyOriginalAsync(sourceSync, sourceWork, ownerReplica, firstRound.Id);
            Stage("original-source-capture-sealed-uploaded-verified-and-original-apply-acknowledged");

            var capability = RandomNumberGenerator.GetBytes(32); HouseholdInvitationClaimResult claimed; string signedClaim; string signedInvitation;
            var invitationId = Guid.NewGuid();
            try
            {
                var invitation = new HouseholdInvitationStatement(1, room, invitationId, Guid.NewGuid(), 1, checkpoint.MembershipHead, HouseholdRole.Editor,
                    HashBytes(capability), TimeProvider.System.GetUtcNow().AddHours(1).ToUnixTimeSeconds());
                signedInvitation = Sign(invitation, HouseholdMetadataJsonContext.Default.HouseholdInvitationStatement, rootKey, "zanance-household-invitation+jwt");
                Assert.Equal(invitationId, (await ownerTransport.InviteAsync(room, new(reservation.DeviceId, signedInvitation), Ct)).InvitationId);
                var claim = new HouseholdInvitationClaimStatement(1, room, invitationId, Guid.NewGuid(), memberId, Hash(signedInvitation), rootKey.Public.Fingerprint, Public(memberKey));
                signedClaim = Sign(claim, HouseholdMetadataJsonContext.Default.HouseholdInvitationClaimStatement, memberKey, "zanance-household-invitation-claim+jwt");
                claimed = await memberTransport.ClaimAsync(room, invitationId, new(Convert.ToHexStringLower(capability), signedClaim, Public(memberKey)), Ct);
            }
            finally { CryptographicOperations.ZeroMemory(capability); }
            using var epoch2 = epoch1.Next();
            var memberDefinition = new HouseholdMemberDefinition(memberId, HouseholdRole.Editor, Hash(signedClaim), [new(claimed.DeviceId, Public(memberKey))]);
            var joined = Membership(room, rootKey, ownerId, epoch2, 1, checkpoint.MembershipHead, HouseholdMembershipChange.AcceptMember,
                [genesis.Members[0], memberDefinition], invitationId) with { PublicationCursor = ownerReplica.Cursor, PublicationHead = ownerReplica.PublicationHead };
            var admission = Publish(joined, rootKey, epoch2, crypto, signedClaim);
            var oldCipher = epoch1.GrantHistorical(crypto, joined, genesis, rootKey, memberDefinition.Devices[0]);
            admission = admission with { Grants = [..admission.Grants, new(1, claimed.DeviceId, oldCipher, HouseholdPublicationDigest.Ciphertext(oldCipher))] };
            var staleSigned = Sign(joined with { PublicationCursor = 0, PublicationHead = Zero }, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement,
                rootKey, "zanance-household-membership+jwt");
            var staleBoundary = await Assert.ThrowsAsync<PersonalSyncHttpException>(() => ownerTransport.PublishMembershipAsync(room,
                admission with { SignedStatement = staleSigned }, Ct));
            Assert.Equal(HttpStatusCode.Conflict, staleBoundary.Status);
            checkpoint = await ownerTransport.PublishMembershipAsync(room, admission, Ct);
            var history = await memberTransport.MembershipAsync(room, claimed.DeviceId, 0, Ct);
            var memberTrusted = HouseholdTrustChain.Join(rootKey.Public, signedInvitation, history, memberId, claimed.DeviceId, memberKey.Public);
            var oldGrant = await memberTransport.GrantAsync(room, claimed.DeviceId, 1, Ct);
            using var historicalKey = memberTrusted.OpenHistoricalGrant(crypto, genesisTrusted, oldGrant.Ciphertext, claimed.DeviceId, memberKey);
            var currentGrant = await memberTransport.GrantAsync(room, claimed.DeviceId, 2, Ct);
            using var memberEpoch = memberTrusted.OpenGrant(crypto, currentGrant.Ciphertext, claimed.DeviceId, memberKey);
            var memberEvents = await ReadWholePublicationAsync(memberTransport, room, claimed.DeviceId, 0, 1);
            recipientReplica.Apply(genesisTrusted, historicalKey, await memberTransport.ReceiptAsync(room, claimed.DeviceId, firstRound.Id, Ct), memberEvents, crypto, memberTrusted);
            using var destination = new ServiceCollection().AddSingleton(TimeProvider.System).AddZananceData(Path.Combine(root, "destination.sqlite")).BuildServiceProvider();
            destination.MigrateLocalDatabase<ZananceDbContext>(); var destinationFinances = destination.GetRequiredService<ZananceStore>();
            var destinationSync = destination.GetRequiredService<PersonalSyncStore>(); await destinationFinances.GetSettingsAsync(Ct);
            await destinationSync.ConnectAsync(await destinationSync.ReviewAsync(Ct), peer, memberId, claimed.DeviceId,
                new(room, "Fictitious household", HouseholdFinancialSchema.Hash, recipientReplica.FinancialCursor, false), Ct,
                receiveFirstRun: true, transportKind: ProfileSynchronizationKind.Household);
            var receiveWork = await destinationSync.CaptureRoundAsync(peer, memberId, Ct, ProfileSynchronizationKind.Household);
            await ApplyOriginalAsync(destinationSync, receiveWork, recipientReplica, receiveWork.Operation);
            var entries = await destinationFinances.GetEntriesAsync(cancellationToken: Ct);
            Assert.Equal(3, entries.Count); Assert.Equal(1234, entries.Single(row => row.Id == expense.Id).Amount);
            Assert.Equal(347, entries.Single(row => row.Id == transfer.Id).ToAmount); Assert.Equal(expense.Id, entries.Single(row => row.Id == refund.Id).RefundOfId);
            Assert.Equal(123_456, (await destinationFinances.GetAccountsAsync(cancellationToken: Ct)).Single(row => row.Id == foreign.Id).OpeningBalance);
            var received = await destinationFinances.GetAttachmentAsync(attachment.Id, Ct); Assert.NotNull(received); Assert.Equal(blobHash, HashBytes(received.Data));
            Stage("new-member-pinned-bootstrap-historical-key-original-money-refund-transfer-and-multimegabyte-attachment-applied");

            expense.Amount = 1300; Assert.True((await finances.SaveEntryAsync(expense, Ct)).Succeeded);
            var competing = entries.Single(row => row.Id == expense.Id); competing.Amount = 1400; Assert.True((await destinationFinances.SaveEntryAsync(competing, Ct)).Succeeded);
            var ownerWork = await sourceSync.CaptureRoundAsync(peer, ownerId, Ct, ProfileSynchronizationKind.Household);
            var memberWork = await destinationSync.CaptureRoundAsync(peer, memberId, Ct, ProfileSynchronizationKind.Household);
            var next = ownerReplica.Prepare(memberTrusted, epoch2, reservation.DeviceId, ownerKey, ownerWork.Operation, Guid.NewGuid(),
                ownerWork.Parts.SelectMany(part => part.Rows).ToArray(), ownerWork.Resolutions, new Dictionary<string, byte[]>(), crypto);
            Assert.True(next.Plan.Succeeded); var nextRound = await UploadAsync(ownerTransport, room, next);
            var nextEvents = await ReadWholePublicationAsync(memberTransport, room, claimed.DeviceId, 1, 2);
            var nextReceipt = await memberTransport.ReceiptAsync(room, claimed.DeviceId, nextRound.Id, Ct);
            ownerReplica.Apply(memberTrusted, epoch2, nextReceipt, nextEvents, crypto); recipientReplica.Apply(memberTrusted, memberEpoch, nextReceipt, nextEvents, crypto);
            await ApplyOriginalAsync(sourceSync, ownerWork, ownerReplica, nextRound.Id);
            var refused = recipientReplica.Prepare(memberTrusted, memberEpoch, claimed.DeviceId, memberKey, memberWork.Operation, Guid.NewGuid(),
                memberWork.Parts.SelectMany(part => part.Rows).ToArray(), memberWork.Resolutions, new Dictionary<string, byte[]>(), crypto);
            Assert.False(refused.Plan.Succeeded); Assert.Empty(refused.Parts); Assert.Contains(Assert.Single(refused.Plan.Conflicts).Fields, field => field == "Amount");
            await destinationSync.RecordCommitAsync(memberWork, new(false, recipientReplica.FinancialCursor, refused.Plan.Conflicts), Ct);
            Assert.Equal(1400, (await destinationFinances.GetEntriesAsync(cancellationToken: Ct)).Single(row => row.Id == expense.Id).Amount);
            var review = Assert.Single(await destinationSync.ConflictsAsync(Ct)); await destinationSync.ResolveAsync(review, keepLocalProposal: false, Ct);
            var reviewed = await destinationSync.CaptureRoundAsync(peer, memberId, Ct, ProfileSynchronizationKind.Household);
            var selected = recipientReplica.Prepare(memberTrusted, memberEpoch, claimed.DeviceId, memberKey, reviewed.Operation, Guid.NewGuid(),
                reviewed.Parts.SelectMany(part => part.Rows).ToArray(), reviewed.Resolutions, new Dictionary<string, byte[]>(), crypto);
            Assert.True(selected.Plan.Succeeded); Assert.Empty(selected.Parts);
            await ApplyOriginalAsync(destinationSync, reviewed, recipientReplica, reviewed.Operation);
            Assert.Equal(1300, (await destinationFinances.GetEntriesAsync(cancellationToken: Ct)).Single(row => row.Id == expense.Id).Amount);
            Assert.Equal(3, (await destinationFinances.GetEntriesAsync(cancellationToken: Ct)).Count);
            Assert.Equal(0, readableFinancialRequests);
            Stage("original-explicit-money-conflict-no-partial-write-no-invented-second-payment-and-no-readable-http-fallback");
            CryptographicOperations.ZeroMemory(originalBytes);
        }
        finally { await app.StopAsync(Ct); }
    }

    /// <summary>Fixture orchestrates only original production typed calls; complete ciphertext is the only financial service body.</summary>
    private static async Task<HouseholdRoundHandle> UploadAsync(HouseholdServiceTransport transport, Guid room, HouseholdPreparedPublication publication)
    {
        var handle = await transport.OpenAsync(room, publication.Open, Ct);
        foreach (var part in publication.Parts) await transport.PutAsync(room, handle.Id, part, Ct);
        return await transport.CommitAsync(room, handle.Id, publication.Commit, Ct);
    }

    /// <summary>Retains the complete original atomic encrypted transaction before verifying/applying any financial packet.</summary>
    private static async Task<HouseholdOpaqueEvent[]> ReadWholePublicationAsync(HouseholdServiceTransport transport, Guid room, Guid device, long after, long through)
    {
        var result = new List<HouseholdOpaqueEvent>(); var position = new HouseholdFeedPosition(after, after == 0 ? "" : new('f', 64));
        while (true)
        {
            var page = await transport.FeedAsync(room, device, position, through, Ct); result.AddRange(page.Events);
            if (page.Complete) return result.ToArray();
            Assert.NotEqual(position, page.Next); position = page.Next;
        }
    }

    /// <summary>Original transactional known-baseline/conflict/attachment/apply code writes the actual SQLite destination; no duplicate financial writer is tested.</summary>
    private static async Task ApplyOriginalAsync(PersonalSyncStore store, SyncRoundWork work, HouseholdFinancialReplica replica, Guid receipt)
    {
        await store.RecordRemoteAsync(work, work.Remote ?? receipt, work.Parts.Length, Ct);
        await store.RecordCommitAsync(work, new(true, replica.FinancialCursor, []), Ct);
        var cursor = work.Cursor;
        while (cursor < replica.FinancialCursor)
        { var page = replica.FinancialFeed(cursor, replica.FinancialCursor); await store.StageFeedAsync(work, page, Ct); cursor = page.NextCursor; }
        foreach (var blob in await store.IncomingBlobsAsync(work, Ct))
            foreach (var index in await store.MissingBlobPiecesAsync(work, blob, Ct))
            {
                var bytes = replica.ReadAttachmentPiece(blob.Hash, index);
                try { await store.StageBlobAsync(work, blob, index, bytes, Ct); } finally { CryptographicOperations.ZeroMemory(bytes); }
            }
        Assert.True(await store.ApplyFeedAsync(work, Ct));
    }
}
