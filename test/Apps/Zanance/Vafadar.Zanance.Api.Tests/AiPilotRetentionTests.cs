using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Exercises the original retention claim and vendor receipts without real provider traffic.</summary>
[Trait("Qualification", "AiLocal")]
public sealed class AiPilotRetentionTests
{
    /// <summary>Claims each eligible deletion once and leaves failed receipts and local credits intact.</summary>
    [Fact]
    public async Task Original_synthetic_vendor_deletion_is_bound_to_the_frozen_route_and_receipt()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_RETENTION_TEST_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated retention PostgreSQL fixture required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", "zanance_ai_provider_pilot"), (settings.Host, settings.Database));
        Assert.Contains(settings.Port, new[] { 55479, 55483 });
        var factory = new Factory(connection);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Analysis:ProviderPilot:openai:ApiKey"] = "fictional-retention",
            ["Analysis:ProviderPilot:openai:Project"] = "fixture-retention-openai",
            ["Analysis:ProviderPilot:gemini:ApiKey"] = "fictional-retention",
            ["Analysis:ProviderPilot:gemini:Project"] = "fixture-retention-gemini",
        }).Build();
        using var transport = new Replies();
        var pilot = new AiPilotProvider(config, transport, clock);
        var retention = new AiPilotRetention(factory, pilot, clock);
        const string owner = "fictional-retention-owner";
        const string actor = "fictional-retention-operator";

        Guid openAi, gemini, foreign, missing, pending, contradictory, geminiForeign, geminiLegacy;
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.MigrateAsync(ct);
            Assert.False(db.Database.HasPendingModelChanges());
            db.Users.Add(new ServiceUser { Id = owner, UserName = owner, SecurityStamp = "fictional-retention-stamp" });
            await db.SaveChangesAsync(ct);

            var first = await SelectAsync("gpt-6-luna");
            openAi = Seed(first, "resp_fixture_retention");
            foreign = Seed(first, "resp_fixture_unmatched");
            contradictory = Seed(first, "resp_fixture_contradictory");
            transport.Operations.Add("resp_fixture_retention", Operation(openAi));
            transport.Operations.Add("resp_fixture_unmatched", Operation(foreign));
            transport.Operations.Add("resp_fixture_contradictory", Operation(foreign));

            string Operation(Guid request) => Convert.ToHexStringLower(SHA256.HashData(
                Encoding.UTF8.GetBytes($"zanance.analysis.v1\n{owner}\n{request:N}")));
            var next = await SelectAsync("gemini-3.5-flash-lite");
            gemini = Seed(next, "interaction_fixture_retention");
            missing = Seed(next, "interaction_fixture_missing");
            pending = Seed(next, "interaction_fixture_pending", AiRequestState.Pending);
            geminiForeign = Seed(next, "interaction_fixture_foreign");
            geminiLegacy = Seed(next, "interaction_fixture_legacy");
            transport.Operations.Add("interaction_fixture_retention", Operation(gemini));
            transport.Operations.Add("interaction_fixture_missing", Operation(missing));
            transport.Operations.Add("interaction_fixture_foreign", Operation(missing));
            transport.Operations.Add("interaction_fixture_legacy", null);
            await SelectAsync("off");
            await db.SaveChangesAsync(ct);

            async Task<AiPilotRouteRow> SelectAsync(string model)
            {
                await pilot.SelectModelAsync(db, AiPurpose.PeriodExplanation, model, actor, ct);
                return await db.Set<AiPilotRouteRow>().OrderByDescending(row => row.Id).FirstAsync(ct);
            }

            Guid Seed(AiPilotRouteRow route, string handle, AiRequestState state = AiRequestState.Completed)
            {
                var offer = new AiOfferRow { Id = Guid.NewGuid(), UserId = owner, Purpose = route.Purpose,
                    Provider = route.Provider, Model = route.Model!, PilotRouteId = route.Id,
                    MaximumUnits = 1, ExpiresAt = clock.GetUtcNow() };
                var request = new AiRequestRow { Id = Guid.NewGuid(), OfferId = offer.Id, UserId = owner, State = state,
                    CreatedAt = clock.GetUtcNow(), PayloadDigest = AiProtocol.Digest(AiPilotProvider.ReportFixture),
                    ProtectedPayload = "fixture payload", ProtectedText = state == AiRequestState.Completed ? "fixture output" : null,
                    VendorOperationId = handle, GiftedReserved = 1, ChargedUnits = state == AiRequestState.Completed ? 1 : 0,
                    PilotMeteredCostMicros = state == AiRequestState.Completed ? 31 : null };
                db.Add(offer);
                db.Add(request);
                db.Add(new AiCreditRow { Id = Guid.NewGuid(), UserId = owner, RequestId = request.Id,
                    CreatedAt = clock.GetUtcNow(), Kind = AiCreditEvent.Reserve, GiftedAvailable = -1, GiftedReserved = 1 });
                if (state == AiRequestState.Completed)
                    db.Add(new AiCreditRow { Id = Guid.NewGuid(), UserId = owner, RequestId = request.Id,
                        CreatedAt = clock.GetUtcNow(), Kind = AiCreditEvent.Settle, GiftedReserved = -1 });
                return request.Id;
            }
        }

        transport.BeforeSend = async handle =>
        {
            await using var db = factory.CreateDbContext();
            var request = await db.Set<AiRequestRow>().SingleAsync(item => item.VendorOperationId == handle, ct);
            var receipt = await db.Set<AiPilotDeletionRow>().SingleAsync(row => row.RequestId == request.Id, ct);
            Assert.Equal(clock.GetUtcNow(), receipt.StartedAt);
            Assert.Equal(actor, receipt.Actor);
            Assert.Null(receipt.ConfirmedAt);
        };
        Assert.Equal(AiPilotDeletionResult.Refused, await retention.DeleteAsync(pending, actor, ct));
        config["Analysis:ProviderPilot:openai:Project"] = "fixture-different-project";
        Assert.Equal(AiPilotDeletionResult.Refused, await retention.DeleteAsync(openAi, actor, ct));
        config["Analysis:ProviderPilot:openai:Project"] = "fixture-retention-openai";
        Assert.Empty(transport.Calls);

        Assert.Equal(AiPilotDeletionResult.Unconfirmed, await retention.DeleteAsync(contradictory, actor, ct));
        Assert.Empty(transport.Calls);
        Assert.Equal(AiPilotDeletionResult.Unconfirmed, await retention.DeleteAsync(contradictory, actor, ct));

        Assert.Equal(AiPilotDeletionResult.Unconfirmed, await retention.DeleteAsync(geminiForeign, actor, ct));
        Assert.Equal(AiPilotDeletionResult.Unconfirmed, await retention.DeleteAsync(geminiLegacy, actor, ct));
        Assert.Equal(AiPilotDeletionResult.Unconfirmed, await retention.DeleteAsync(geminiForeign, actor, ct));
        Assert.Empty(transport.Calls);

        var concurrent = await Task.WhenAll(retention.DeleteAsync(openAi, actor, ct), retention.DeleteAsync(openAi, actor, ct));
        Assert.Contains(AiPilotDeletionResult.Confirmed, concurrent);
        Assert.All(concurrent, result => Assert.NotEqual(AiPilotDeletionResult.Refused, result));
        Assert.Equal(AiPilotDeletionResult.Confirmed, await retention.DeleteAsync(openAi, actor, ct));
        Assert.Equal(AiPilotDeletionResult.Confirmed, await retention.DeleteAsync(gemini, actor, ct));
        Assert.Equal(AiPilotDeletionResult.Unconfirmed, await retention.DeleteAsync(foreign, actor, ct));
        Assert.Equal(AiPilotDeletionResult.Unconfirmed, await retention.DeleteAsync(missing, actor, ct));
        Assert.Equal(AiPilotDeletionResult.Unconfirmed, await retention.DeleteAsync(missing, actor, ct));
        Assert.Equal(new[] { "resp_fixture_retention", "interaction_fixture_retention", "resp_fixture_unmatched", "interaction_fixture_missing" }, transport.Calls);

        Assert.Equal(new[] { "resp_fixture_contradictory", "interaction_fixture_foreign", "interaction_fixture_legacy",
            "resp_fixture_retention", "interaction_fixture_retention", "resp_fixture_unmatched", "interaction_fixture_missing" }, transport.Reads);
        await using var audit = factory.CreateDbContext();
        var rows = await audit.Set<AiRequestRow>().ToDictionaryAsync(row => row.Id, ct);
        var receipts = await audit.Set<AiPilotDeletionRow>().ToDictionaryAsync(row => row.RequestId, ct);
        Assert.NotNull(receipts[openAi].ConfirmedAt);
        Assert.NotNull(receipts[gemini].ConfirmedAt);
        Assert.Null(receipts[foreign].ConfirmedAt);
        Assert.Null(receipts[contradictory].ConfirmedAt);
        Assert.Null(receipts[geminiForeign].ConfirmedAt);
        Assert.Null(receipts[geminiLegacy].ConfirmedAt);
        Assert.Null(receipts[missing].ConfirmedAt);
        Assert.DoesNotContain(pending, receipts.Keys);
        Assert.Null(rows[pending].ProtectedText);
        Assert.Equal(0L, rows[pending].ChargedUnits);
        Assert.All(rows.Values.Where(row => row.Id != pending), row =>
        {
            Assert.Equal("fixture payload", row.ProtectedPayload);
            Assert.Equal("fixture output", row.ProtectedText);
            Assert.Equal(31L, row.PilotMeteredCostMicros);
            Assert.Equal(1L, row.ChargedUnits);
        });
        var credits = audit.Set<AiCreditRow>();
        Assert.Equal(15, await credits.CountAsync(ct));
        Assert.Empty(await credits.Where(row => row.Kind == AiCreditEvent.Refund).ToArrayAsync(ct));
    }

    private sealed class Factory(string connection) : IDbContextFactory<ServiceDbContext>
    {
        public ServiceDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options);
    }

    private sealed class Replies : HttpMessageHandler, IHttpClientFactory
    {
        internal List<string> Calls { get; } = [];
        internal List<string> Reads { get; } = [];
        internal Dictionary<string, string?> Operations { get; } = [];
        internal Func<string, Task>? BeforeSend { get; set; }

        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Contains(request.Method, new[] { HttpMethod.Get, HttpMethod.Delete });
            var uri = request.RequestUri!;
            var handle = uri.Segments[^1];
            if (handle.StartsWith("resp_", StringComparison.Ordinal))
            {
                Assert.Equal("api.openai.com", uri.Host);
                Assert.Equal("/v1/responses/" + handle, uri.AbsolutePath);
                Assert.Equal("fixture-retention-openai", Assert.Single(request.Headers.GetValues("OpenAI-Project")));
            }
            else
            {
                Assert.Equal("generativelanguage.googleapis.com", uri.Host);
                Assert.Equal("/v1beta/interactions/" + handle, uri.AbsolutePath);
                Assert.True(request.Headers.Contains("x-goog-api-key"));
            }
            if (request.Method == HttpMethod.Get)
            {
                Reads.Add(handle);
                await BeforeSend!(handle);
                var operation = Operations[handle];
                if (uri.Host == "api.openai.com")
                    return Json(JsonSerializer.Serialize(new
                    {
                        id = handle, model = "gpt-6-luna", status = "completed",
                        metadata = new { zanance_operation = operation },
                    }));

                Assert.Equal("?include_input=true", uri.Query);
                var input = operation is null ? "Fictional older prompt." :
                    "Fictional prompt.\nIgnore this opaque operation reference and do not repeat it: " + operation;
                return Json(JsonSerializer.Serialize(new
                {
                    id = handle, model = "gemini-3.5-flash-lite", status = "completed",
                    steps = new[] { new { type = "user_input", status = "done", content = new[] { new { type = "text", text = input } } } },
                }));
            }

            Calls.Add(handle);
            await BeforeSend!(handle);
            return handle switch
            {
                "resp_fixture_retention" => Json("""{"id":"resp_fixture_retention","object":"response.deleted","deleted":true}"""),
                "resp_fixture_contradictory" => Json("""{"id":"resp_fixture_contradictory","object":"response.deleted","deleted":true}"""),
                "resp_fixture_unmatched" => Json("""{"id":"resp_other","object":"response.deleted","deleted":true}"""),
                "interaction_fixture_retention" or "interaction_fixture_foreign" or "interaction_fixture_legacy"
                    => new HttpResponseMessage(HttpStatusCode.NoContent),
                "interaction_fixture_missing" => new HttpResponseMessage(HttpStatusCode.NotFound),
                _ => throw new InvalidOperationException("Unexpected deletion."),
            };

            static HttpResponseMessage Json(string text) => new(HttpStatusCode.OK)
                { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        }
    }
}
