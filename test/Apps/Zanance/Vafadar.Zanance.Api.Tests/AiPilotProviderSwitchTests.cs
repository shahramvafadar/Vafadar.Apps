using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Exercises the original analysis store and durable operator selection against an isolated PostgreSQL fixture.</summary>
public sealed class AiPilotProviderSwitchTests
{
    private sealed class Factory(string connection) : IDbContextFactory<ServiceDbContext>
    {
        public ServiceDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options);
    }

    private sealed class Transport : HttpMessageHandler, IHttpClientFactory
    {
        public List<(HttpMethod Method, string Host, string Path)> Calls { get; } = [];
        internal bool LoseGeminiAcknowledgement { get; set; }
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var uri = request.RequestUri!;
            Calls.Add((request.Method, uri.Host, uri.AbsolutePath));
            return request.Method == HttpMethod.Post ? await BeginAsync(request, ct) : Status(request);
        }

        private async Task<HttpResponseMessage> BeginAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var input = await request.Content!.ReadAsStringAsync(ct);
            Assert.Contains("125000", input, StringComparison.Ordinal);
            Assert.Contains("Synthetic Zanance test", input, StringComparison.Ordinal);
            if (request.RequestUri!.Host == "api.openai.com")
                return Json("""{"id":"resp_fixture","status":"in_progress"}""");
            if (LoseGeminiAcknowledgement) throw new TimeoutException("Fictitious lost create receipt.");
            return Json("""{"id":"interaction_fixture","status":"in_progress"}""");
        }

        private static HttpResponseMessage Status(HttpRequestMessage request) => request.RequestUri!.Host == "api.openai.com"
            ? Json("""{"id":"resp_fixture","model":"gpt-6.2-sol","status":"completed","usage":{"input_tokens":110,"output_tokens":52},"output":[{"type":"message","content":[{"type":"output_text","text":"{\"net_expenses_minor\":38000,\"explanation\":\"The recorded net expense is 38000 minor units.\"}"}]}]}""")
            : Json("""{"id":"interaction_fixture","model":"gemini-4-hydrogen","status":"completed","usage":{"total_input_tokens":110,"total_output_tokens":39,"total_thought_tokens":5},"steps":[{"type":"model_output","content":[{"type":"text","text":"{\"net_expenses_minor\":38000,\"explanation\":\"The recorded net expense is 38000 minor units.\"}"}]}]}""");

        private static HttpResponseMessage Json(string content)
            => new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "application/json") };
    }

    /// <summary>Switches both ways and off, preserving the original route and request count across a cold store reconstruction.</summary>
    [Fact]
    public async Task Switch_retires_quotes_but_pending_requests_keep_their_original_vendor()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_PROVIDER_PILOT_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated provider-pilot PostgreSQL fixture required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host);
        Assert.Equal(55479, settings.Port);
        Assert.Equal("zanance_ai_provider_pilot", settings.Database);
        var factory = new Factory(connection);
        var now = new FakeTimeProvider(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));
        var owner = "fictional-provider-switch";
        var session = Guid.NewGuid();
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.MigrateAsync(ct);
            Assert.False(db.Database.HasPendingModelChanges());
            db.Users.Add(new ServiceUser { Id = owner, UserName = owner, SecurityStamp = "pilot-stamp" });
            db.Sessions.Add(new DeviceSession { Id = session, UserId = owner, Generation = 1, ExpiresAt = now.GetUtcNow().AddDays(1) });
            await db.SaveChangesAsync(ct);
        }
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner),
            new Claim(ServiceHost.SessionClaim, session.ToString("D")), new Claim(ServiceHost.GenerationClaim, "1")], "synthetic"));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Analysis:ProviderPilot:openai:ApiKey"] = "fixture-openai",
            ["Analysis:ProviderPilot:openai:Project"] = "proj_fixture",
            ["Analysis:ProviderPilot:gemini:ApiKey"] = "fixture-gemini",
            ["Analysis:ProviderPilot:gemini:Project"] = "fixture-gcp"
        }).Build();
        var protection = new EphemeralDataProtectionProvider();
        using var transport = new Transport();
        AiRequestStore FreshStore() => new(factory, new UnconfiguredAiProvider(), protection, now, new AiPilotProvider(config, transport, now));
        var store = FreshStore();
        async Task Select(string provider)
        {
            await using var db = factory.CreateDbContext();
            await new AiPilotProvider(config, transport, now).SelectAsync(db, provider, "synthetic-operator", ct);
        }
        async Task<AiSubmission> Quote(AiPayload payload) => new(Guid.NewGuid(), await store.OfferAsync(actor, payload.Purpose, ct), payload);

        var input = new AiPayload(1, AiPurpose.PeriodExplanation, "en",
            new(new(2026, 11, 1), new(2026, 11, 30), 1, true, [new("EUR", 125000, 0, 41000, 3000)], 0), null);
        Assert.False((await store.AvailabilityAsync(actor, ct)).Configured);
        await store.GrantAsync(owner, "synthetic-fixture-gift", false, 10, ct);
        await Select("openai");
        var retired = await Quote(input);
        var original = await Quote(input);
        Assert.Equal("openai", original.Offer.Provider);
        Assert.Equal(AiRequestState.Pending, (await store.SubmitAsync(actor, original, ct)).State);
        Assert.Single(transport.Calls);
        await Select("gemini");
        store = FreshStore();
        Assert.Equal("gemini", (await store.AvailabilityAsync(actor, ct)).Provider);
        Assert.Equal(AiRequestState.NotAccepted, (await store.SubmitAsync(actor, retired, ct)).State);
        var changed = input with { Language = "de" };
        Assert.Equal(AiRequestState.NotAccepted, (await store.SubmitAsync(actor, await Quote(changed), ct)).State);
        Assert.Single(transport.Calls);
        Assert.Equal(AiRequestState.Completed, (await store.StatusAsync(actor, original.RequestId, ct)).State);
        Assert.Equal((HttpMethod.Get, "api.openai.com", "/v1/responses/resp_fixture"), transport.Calls[1]);

        var gemini = await Quote(input);
        Assert.Equal(AiRequestState.Pending, (await store.SubmitAsync(actor, gemini, ct)).State);
        transport.LoseGeminiAcknowledgement = true;
        var lost = await Quote(input);
        Assert.Equal(AiRequestState.Pending, (await store.SubmitAsync(actor, lost, ct)).State);
        await Select("off");
        Assert.False((await store.AvailabilityAsync(actor, ct)).Configured);
        await Select("openai");
        Assert.Equal("openai", (await store.AvailabilityAsync(actor, ct)).Provider);
        store = FreshStore();
        Assert.Equal(AiRequestState.Completed, (await store.StatusAsync(actor, gemini.RequestId, ct)).State);
        Assert.Equal((HttpMethod.Get, "generativelanguage.googleapis.com", "/v1beta/interactions/interaction_fixture"), transport.Calls[4]);
        Assert.Equal(AiRequestState.Pending, (await store.SubmitAsync(actor, lost, ct)).State);
        Assert.Equal(5, transport.Calls.Count);
        Assert.Equal((7L, 1L), ((await store.AvailabilityAsync(actor, ct)).Balance.GiftedAvailable,
            (await store.AvailabilityAsync(actor, ct)).Balance.GiftedReserved));

        await using var verify = factory.CreateDbContext();
        Assert.Equal(new[] { "openai", "gemini", "off", "openai" },
            await verify.Set<AiPilotRouteRow>().OrderBy(r => r.Id).Select(r => r.Provider).ToArrayAsync(ct));
        var saved = await verify.Set<AiRequestRow>().SingleAsync(r => r.Id == gemini.RequestId, ct);
        Assert.Equal("interaction_fixture", saved.VendorOperationId);
        Assert.Equal(110, saved.VendorInputTokens);
        Assert.Equal(44, saved.VendorOutputTokens);
    }
}
