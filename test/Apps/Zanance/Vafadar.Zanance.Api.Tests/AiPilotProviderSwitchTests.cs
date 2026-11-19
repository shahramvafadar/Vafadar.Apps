using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>Qualifies the independent model selections through the original PostgreSQL request store and real vendor transport.</summary>
    [Fact]
    public async Task Purpose_routes_use_the_selected_model_and_keep_original_pending_operations()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_PROVIDER_PILOT_LIVE_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated live synthetic pilot required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host);
        Assert.Equal(55479, settings.Port);
        Assert.Equal("zanance_ai_provider_pilot", settings.Database);
        var factory = new Factory(connection);
        var hydrogenOnly = Environment.GetEnvironmentVariable("ZANANCE_AI_PROVIDER_PILOT_HYDROGEN_ONLY") == "1";
        var owner = hydrogenOnly ? "fictional-live-hydrogen-output-budget" : "fictional-live-purpose-routing";
        var session = Guid.NewGuid();
        var resume = Environment.GetEnvironmentVariable("ZANANCE_AI_PROVIDER_PILOT_RESUME") == "1";
        var now = TimeProvider.System;
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.MigrateAsync(ct);
            Assert.False(db.Database.HasPendingModelChanges());
            if (resume)
                session = await db.Sessions.Where(s => s.UserId == owner).Select(s => s.Id).SingleAsync(ct);
            else
            {
                db.Users.Add(new ServiceUser { Id = owner, UserName = owner, SecurityStamp = "live-pilot-stamp" });
                db.Sessions.Add(new DeviceSession { Id = session, UserId = owner, Generation = 1, ExpiresAt = now.GetUtcNow().AddDays(1) });
                await db.SaveChangesAsync(ct);
            }
        }
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner),
            new Claim(ServiceHost.SessionClaim, session.ToString("D")), new Claim(ServiceHost.GenerationClaim, "1")], "synthetic"));
        var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var services = new ServiceCollection();
        AiModule.Add(services, pilotEnabled: true);
        await using var built = services.BuildServiceProvider();
        var pilot = new AiPilotProvider(config, built.GetRequiredService<IHttpClientFactory>(), now);
        var protection = new EphemeralDataProtectionProvider();
        AiRequestStore FreshStore() => new(factory, new UnconfiguredAiProvider(), protection, now, pilot);
        var store = FreshStore();
        async Task VerifyCompletion(Guid request)
        {
            var status = await store.StatusAsync(actor, request, ct);
            for (var attempts = 0; status.State == AiRequestState.Pending && attempts < 30; attempts++)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                status = await store.StatusAsync(actor, request, ct);
            }
            Assert.Equal(AiRequestState.Completed, status.State);
            Assert.Equal(1, status.ChargedUnits);
            await using var db = factory.CreateDbContext();
            var saved = await db.Set<AiRequestRow>().SingleAsync(r => r.Id == request, ct);
            Assert.NotNull(saved.VendorOperationId);
            Assert.True(saved.VendorInputTokens > 0);
            Assert.True(saved.VendorOutputTokens > 0);
        }
        if (resume)
        {
            // Only pending original handles may resume. No new work or credit is created on this path.
            await using var db = factory.CreateDbContext();
            var requests = db.Set<AiRequestRow>().Where(r => r.UserId == owner && r.State != AiRequestState.NotAccepted);
            Assert.Equal(4, await requests.CountAsync(ct));
            var pending = await requests.Where(r => r.State == AiRequestState.Pending).ToArrayAsync(ct);
            foreach (var request in pending)
            {
                Assert.NotNull(request.VendorOperationId);
                await VerifyCompletion(request.Id);
            }
            Assert.Equal(4, await requests.CountAsync(r => r.State == AiRequestState.Completed, ct));
            Assert.Equal(new AiBalance(0, 6, 0, 0), (await store.AvailabilityAsync(actor, ct)).Balance);
            return;
        }
        async Task Select(AiPurpose purpose, string model)
        {
            await using var db = factory.CreateDbContext();
            await pilot.SelectModelAsync(db, purpose, model, "synthetic-live-operator", ct);
        }
        async Task<AiSubmission> Quote(AiPayload payload) => new(Guid.NewGuid(), await store.OfferAsync(actor, payload.Purpose, ct), payload);
        var report = AiPilotProvider.ReportFixture;
        var comparison = AiPilotProvider.ComparisonFixture;
        if (hydrogenOnly)
        {
            await store.GrantAsync(owner, "synthetic-hydrogen-output-budget-credit", false, 1, ct);
            await Select(comparison.Purpose, "gemini-4-hydrogen");
            var request = await Quote(comparison);
            Assert.Equal("gemini-4-hydrogen", request.Offer.Model);
            Assert.NotEqual(AiRequestState.NotAccepted, (await store.SubmitAsync(actor, request, ct)).State);
            await Select(comparison.Purpose, "off");
            await VerifyCompletion(request.RequestId);
            Assert.Equal(new AiBalance(0, 0, 0, 0), (await store.AvailabilityAsync(actor, ct)).Balance);
            return;
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => Select(report.Purpose, "gpt-6.2-sol"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Select(comparison.Purpose, "gemini-3.5-flash-lite"));
        await store.GrantAsync(owner, "synthetic-live-purpose-credit", false, 10, ct);
        await Select(report.Purpose, "gpt-6-luna");
        await Select(comparison.Purpose, "gpt-6.2-sol");
        var staleReport = await Quote(report);
        var staleComparison = await Quote(comparison);
        var lunar = await Quote(report);
        Assert.Equal("gpt-6-luna", lunar.Offer.Model);
        Assert.NotEqual(AiRequestState.NotAccepted, (await store.SubmitAsync(actor, lunar, ct)).State);
        await Select(report.Purpose, "gemini-3.5-flash-lite");
        Assert.Equal(AiRequestState.NotAccepted, (await store.SubmitAsync(actor, staleReport, ct)).State);
        Assert.Equal(AiRequestState.NotAccepted, (await store.SubmitAsync(actor, await Quote(report with { Language = "de" }), ct)).State);
        // The report switch must not retire the earlier comparison offer.
        Assert.Equal("gpt-6.2-sol", staleComparison.Offer.Model);
        Assert.NotEqual(AiRequestState.NotAccepted, (await store.SubmitAsync(actor, staleComparison, ct)).State);
        await Select(comparison.Purpose, "gemini-4-hydrogen");
        var flash = await Quote(report);
        var hydrogen = await Quote(comparison);
        Assert.Equal("gemini-3.5-flash-lite", flash.Offer.Model);
        Assert.Equal("gemini-4-hydrogen", hydrogen.Offer.Model);
        Assert.NotEqual(AiRequestState.NotAccepted, (await store.SubmitAsync(actor, flash, ct)).State);
        Assert.NotEqual(AiRequestState.NotAccepted, (await store.SubmitAsync(actor, hydrogen, ct)).State);
        await Select(report.Purpose, "off");
        Assert.False((await store.AvailabilityAsync(actor, ct)).Configured);
        Assert.Equal("gemini-4-hydrogen", (await Quote(comparison)).Offer.Model);
        await Select(comparison.Purpose, "off");
        await Assert.ThrowsAsync<AiRefusal>(() => Quote(comparison));

        store = FreshStore();
        foreach (var original in new[] { lunar, staleComparison, flash, hydrogen })
            await VerifyCompletion(original.RequestId);
        var balance = (await store.AvailabilityAsync(actor, ct)).Balance;
        Assert.Equal(new AiBalance(0, 6, 0, 0), balance);
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
