using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Checks the original response binding on both sides of a transaction interruption.</summary>
[Trait("Qualification", "AiLocal")]
public sealed class AiPilotRecoveryTransactionTests
{
    /// <summary>A failed database write rolls back binding and an older partial binding can resume by its original ID.</summary>
    [Fact]
    public async Task Notice_binding_rolls_back_with_settlement_and_recovers_an_existing_handle()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_RECOVERY_TX_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated recovery database required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", "zanance_ai_provider_pilot"), (settings.Host, settings.Database));
        Assert.Contains(settings.Port, new[] { 55479, 55486 });
        var factory = new Factory(connection);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        var protection = new EphemeralDataProtectionProvider();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Analysis:ProviderPilot:openai:ApiKey"] = "fictional-transaction-key",
            ["Analysis:ProviderPilot:openai:Project"] = "fixture-notice-transaction",
            ["Analysis:ProviderPilot:openai:WebhookSecret"] = "fixture-signed-inbox-only",
        }).Build();
        const string owner = "fictional-recovery-tx-ä-owner";
        var lost = Guid.NewGuid();
        var earlier = Guid.NewGuid();
        var otherRequests = Enumerable.Range(0, 21).Select(_ => Guid.NewGuid()).ToArray();
        using var transport = new Replies(Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes($"zanance.analysis.v1\n{owner}\n{lost:N}"))));
        var pilot = new AiPilotProvider(config, transport, clock);
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.MigrateAsync(ct);
            db.Users.Add(new ServiceUser { Id = owner, UserName = owner, SecurityStamp = "fictional-tx-stamp" });
            await db.SaveChangesAsync(ct);
            await pilot.SelectModelAsync(db, AiPurpose.PeriodExplanation, "gpt-6-luna", "fictional-tx-operator", ct);
            var route = await db.Set<AiPilotRouteRow>().SingleAsync(ct);
            Seed(lost, null);
            Seed(earlier, "resp_fixture_earlier");
            db.AddRange(
                new AiPilotCompletionNoticeRow { Provider = "openai", ResponseId = "resp_fixture_lost", DeliveryId = "fixture-lost",
                    ReceivedAt = clock.GetUtcNow().AddSeconds(-2) },
                new AiPilotCompletionNoticeRow { Provider = "openai", ResponseId = "resp_fixture_timeout", DeliveryId = "fixture-timeout",
                    ReceivedAt = clock.GetUtcNow().AddSeconds(-1) },
                new AiPilotCompletionNoticeRow { Provider = "openai", ResponseId = "resp_fixture_earlier", DeliveryId = "fixture-earlier",
                    ReceivedAt = clock.GetUtcNow() });
            for (var i = 0; i < 2; i++)
            {
                var otherOwner = $"fictional-recovery-other-{i}";
                db.Users.Add(new ServiceUser { Id = otherOwner, UserName = otherOwner, SecurityStamp = "fictional-other-stamp" });
                foreach (var request in otherRequests.Skip(i * 11).Take(11))
                    Seed(request, null, otherOwner);
            }
            await db.SaveChangesAsync(ct);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION fixture_reject_original_settlement() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NEW."Kind" = 3 THEN
                        RAISE EXCEPTION 'Fictional settlement interruption' USING ERRCODE = '40001';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER "FixtureSettlementInterrupt" BEFORE INSERT ON "AiCredits"
                FOR EACH ROW EXECUTE FUNCTION fixture_reject_original_settlement();
                """, ct);

            void Seed(Guid id, string? handle, string requestOwner = owner)
            {
                var offer = new AiOfferRow { Id = Guid.NewGuid(), UserId = requestOwner, Provider = route.Provider,
                    Model = route.Model!, Purpose = route.Purpose, PilotRouteId = route.Id, MaximumUnits = 1,
                    ExpiresAt = clock.GetUtcNow().AddHours(1) };
                db.AddRange(offer, new AiRequestRow { Id = id, UserId = requestOwner, OfferId = offer.Id,
                    CreatedAt = clock.GetUtcNow(), State = AiRequestState.Pending, GiftedReserved = 1,
                    VendorOperationId = handle, PayloadDigest = AiProtocol.Digest(AiPilotProvider.ReportFixture),
                    ProtectedPayload = "fictional-protected", PilotDispatchStage = AiPilotDispatchStage.Claimed,
                    PilotDispatchClaimedAt = clock.GetUtcNow() });
            }
        }

        var inbox = new AiPilotCompletionNotices(config, factory, NewStore(), clock);
        await inbox.DrainAsync(ct);
        Assert.Equal(new[] { "resp_fixture_lost", "resp_fixture_timeout", "resp_fixture_earlier" }, transport.Calls);
        await using (var db = factory.CreateDbContext())
        {
            var rolledBack = await db.Set<AiRequestRow>().SingleAsync(row => row.Id == lost, ct);
            Assert.Equal(AiRequestState.Pending, rolledBack.State);
            Assert.Null(rolledBack.VendorOperationId);
            Assert.Null(rolledBack.PilotVendorStatus);
            Assert.Null(rolledBack.PilotMeteredCostMicros);
            Assert.Empty(await db.Set<AiCreditRow>().ToArrayAsync(ct));
            var notices = await db.Set<AiPilotCompletionNoticeRow>().ToDictionaryAsync(item => item.ResponseId, ct);
            Assert.All(notices.Values, notice => Assert.Equal(1, notice.Attempts));
            Assert.False(notices["resp_fixture_lost"].Handled);
            Assert.False(notices["resp_fixture_timeout"].Handled);
            Assert.True(notices["resp_fixture_earlier"].Handled);
            await db.Database.ExecuteSqlRawAsync("""
                DROP TRIGGER "FixtureSettlementInterrupt" ON "AiCredits";
                DROP FUNCTION fixture_reject_original_settlement();
                """, ct);
        }

        var restarted = new AiPilotCompletionNotices(config, factory, NewStore(), clock);
        await restarted.DrainAsync(ct);
        Assert.Equal(3, transport.Calls.Count);
        clock.Advance(TimeSpan.FromMinutes(1));
        await restarted.DrainAsync(ct);
        await restarted.DrainAsync(ct);
        Assert.Equal(new[] { "resp_fixture_lost", "resp_fixture_timeout", "resp_fixture_earlier", "resp_fixture_lost", "resp_fixture_timeout" }, transport.Calls);
        await using var audit = factory.CreateDbContext();
        var finalNotices = await audit.Set<AiPilotCompletionNoticeRow>().ToDictionaryAsync(item => item.ResponseId, ct);
        Assert.True(finalNotices["resp_fixture_lost"].Handled);
        Assert.Equal(2, finalNotices["resp_fixture_lost"].Attempts);
        Assert.False(finalNotices["resp_fixture_timeout"].Handled);
        Assert.Equal(2, finalNotices["resp_fixture_timeout"].Attempts);
        var requests = await audit.Set<AiRequestRow>().ToDictionaryAsync(row => row.Id, ct);
        Assert.Equal(AiRequestState.Completed, requests[lost].State);
        Assert.Equal("resp_fixture_lost", requests[lost].VendorOperationId);
        Assert.Equal(AiPilotVendorStatus.Completed, requests[lost].PilotVendorStatus);
        Assert.Equal(AiRequestState.Pending, requests[earlier].State);
        Assert.Equal("resp_fixture_earlier", requests[earlier].VendorOperationId);
        Assert.Equal(AiPilotVendorStatus.Failed, requests[earlier].PilotVendorStatus);
        Assert.Null(requests[earlier].PilotMeteredCostMicros);
        Assert.All(otherRequests, request =>
        {
            Assert.Equal(AiRequestState.Pending, requests[request].State);
            Assert.Null(requests[request].VendorOperationId);
            Assert.Null(requests[request].PilotVendorStatus);
        });
        var settlement = Assert.Single(await audit.Set<AiCreditRow>().ToArrayAsync(ct));
        Assert.Equal((lost, AiCreditEvent.Settle), (settlement.RequestId!.Value, settlement.Kind));

        AiRequestStore NewStore() => new(factory, new UnconfiguredAiProvider(), protection, clock, pilot);
    }

    private sealed class Factory(string connection) : IDbContextFactory<ServiceDbContext>
    {
        public ServiceDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options);
    }

    private sealed class Replies(string correlation) : HttpMessageHandler, IHttpClientFactory
    {
        internal List<string> Calls { get; } = [];

        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            var handle = request.RequestUri!.Segments[^1];
            Assert.Equal("https://api.openai.com/v1/responses/" + handle, request.RequestUri.AbsoluteUri);
            Assert.Equal("fixture-notice-transaction", Assert.Single(request.Headers.GetValues("OpenAI-Project")));
            Calls.Add(handle);
            if (handle == "resp_fixture_timeout")
                throw new TaskCanceledException("Fictional HTTP client timeout.");
            var body = handle switch
            {
                "resp_fixture_lost" => JsonSerializer.Serialize(new
                {
                    id = handle, status = "completed", model = "gpt-6-luna",
                    metadata = new { zanance_operation = correlation }, usage = new { input_tokens = 17, output_tokens = 11 },
                    output = new[] { new { type = "message", content = new[] { new { type = "output_text",
                        text = """{"net_expenses_minor":38000,"explanation":"Refunds reduce recorded expenses."}""" } } } },
                }),
                "resp_fixture_earlier" => """{"id":"resp_fixture_earlier","status":"failed","model":"gpt-6-luna"}""",
                _ => throw new InvalidOperationException("Unexpected original response lookup."),
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
