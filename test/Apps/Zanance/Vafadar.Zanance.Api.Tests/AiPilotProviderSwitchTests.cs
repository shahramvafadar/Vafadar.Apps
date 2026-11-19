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
[Trait("Qualification", "AiLocal")]
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
        internal Func<Task>? InspectBeforeCreate { get; set; }
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var uri = request.RequestUri!;
            Calls.Add((request.Method, uri.Host, uri.AbsolutePath));
            return request.Method == HttpMethod.Post ? await BeginAsync(request, ct) : Status(request);
        }

        private async Task<HttpResponseMessage> BeginAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (InspectBeforeCreate is { } observe) await observe();
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

    /// <summary>Exercises request, user and shared project cost admission under actual PostgreSQL concurrency.</summary>
    [Fact]
    public async Task Pilot_cost_budget_reserves_at_acceptance_and_rejects_concurrent_overrun()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_PROVIDER_PILOT_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated provider-pilot PostgreSQL fixture required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", 55479, "zanance_ai_provider_pilot"), (settings.Host, settings.Port, settings.Database));
        var factory = new Factory(connection);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Analysis:ProviderPilot:openai:ApiKey"] = "fictional-budget-test",
            ["Analysis:ProviderPilot:openai:Project"] = "fixture-budget-project",
            ["Analysis:ProviderPilot:Budget:RequestMicros"] = "460",
            ["Analysis:ProviderPilot:Budget:UserMicros"] = "461",
            ["Analysis:ProviderPilot:Budget:ProjectMicros"] = "922",
        }).Build();
        using var transport = new Transport();
        var pilot = new AiPilotProvider(config, transport, clock);
        var store = new AiRequestStore(factory, new UnconfiguredAiProvider(), new EphemeralDataProtectionProvider(), clock, pilot);
        var owners = new[] { "fictional-budget-one", "fictional-budget-two", "fictional-budget-three" };
        var actors = new List<ClaimsPrincipal>();
        var payload = AiPilotProvider.ReportFixture;
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.MigrateAsync(ct);
            Assert.False(db.Database.HasPendingModelChanges());
            foreach (var owner in owners)
            {
                var session = Guid.NewGuid();
                db.Users.Add(new ServiceUser { Id = owner, UserName = owner, SecurityStamp = "fictional-budget-stamp" });
                db.Sessions.Add(new DeviceSession { Id = session, UserId = owner, Generation = 1, ExpiresAt = clock.GetUtcNow().AddDays(1) });
                actors.Add(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner),
                    new Claim(ServiceHost.SessionClaim, session.ToString("D")), new Claim(ServiceHost.GenerationClaim, "1")], "synthetic")));
            }
            await db.SaveChangesAsync(ct);
            await pilot.SelectModelAsync(db, payload.Purpose, "gpt-6-luna", "fictional-budget-manager", ct);
        }
        foreach (var owner in owners) await store.GrantAsync(owner, "fictional-budget-credit-" + owner, false, 3, ct);
        async Task<AiSubmission> Quote(int index) => new(Guid.NewGuid(), await store.OfferAsync(actors[index], payload.Purpose, ct), payload);

        var perRequest = await Quote(0);
        Assert.Equal(AiRequestState.NotAccepted, (await store.SubmitAsync(actors[0], perRequest, ct)).State);
        Assert.Empty(transport.Calls);
        config["Analysis:ProviderPilot:Budget:RequestMicros"] = "461";
        var first = await Quote(0);
        Assert.Equal(AiRequestState.Pending, (await store.SubmitAsync(actors[0], first, ct)).State);
        var perUser = await Quote(0);
        Assert.Equal(AiRequestState.NotAccepted, (await store.SubmitAsync(actors[0], perUser, ct)).State);
        Assert.Single(transport.Calls);

        var second = await Quote(1);
        var third = await Quote(2);
        var contenders = await Task.WhenAll(store.SubmitAsync(actors[1], second, ct), store.SubmitAsync(actors[2], third, ct));
        Assert.Single(contenders, result => result.State == AiRequestState.Pending);
        Assert.Single(contenders, result => result.State == AiRequestState.NotAccepted);
        Assert.Equal(2, transport.Calls.Count);
        Assert.All(transport.Calls, call => Assert.Equal(HttpMethod.Post, call.Method));
        await using var verification = factory.CreateDbContext();
        var rows = await verification.Set<AiRequestRow>().ToArrayAsync(ct);
        Assert.Equal(5, rows.Length);
        Assert.All(rows.Where(row => row.State == AiRequestState.Pending), row => Assert.Equal(461, row.PilotCostCeilingMicros));
        Assert.All(rows.Where(row => row.State == AiRequestState.NotAccepted), row => Assert.Null(row.PilotCostCeilingMicros));
        Assert.Equal(922, rows.Sum(row => row.PilotCostCeilingMicros));
        Assert.Equal(2, await verification.Set<AiCreditRow>().CountAsync(row => row.Kind == AiCreditEvent.Reserve, ct));
    }

    /// <summary>Two fresh stores recover only one never-claimed operation and preserve uncertain legacy and claimed work.</summary>
    [Fact]
    public async Task Pilot_recovers_only_never_claimed_create_after_restart()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_PROVIDER_PILOT_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated provider-pilot PostgreSQL fixture required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", 55479, "zanance_ai_provider_pilot"), (settings.Host, settings.Port, settings.Database));
        var factory = new Factory(connection);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Analysis:ProviderPilot:gemini:ApiKey"] = "fictional-dispatch-fixture",
            ["Analysis:ProviderPilot:gemini:Project"] = "fictional-dispatch-project",
        }).Build();
        using var transport = new Transport { LoseGeminiAcknowledgement = true };
        var pilot = new AiPilotProvider(config, transport, clock);
        const string owner = "fictional-dispatch-owner";
        var queued = Guid.NewGuid();
        var claimed = Guid.NewGuid();
        var legacy = Guid.NewGuid();
        var payload = AiPilotProvider.ReportFixture;
        var session = Guid.NewGuid();
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.MigrateAsync(ct);
            Assert.False(db.Database.HasPendingModelChanges());
            db.Users.Add(new ServiceUser { Id = owner, UserName = owner, SecurityStamp = "fictional-dispatch-stamp" });
            db.Sessions.Add(new DeviceSession { Id = session, UserId = owner, Generation = 1, ExpiresAt = clock.GetUtcNow().AddYears(1) });
            await db.SaveChangesAsync(ct);
            await pilot.SelectModelAsync(db, payload.Purpose, "gemini-3.5-flash-lite", "fictional-dispatch-operator", ct);
            var original = await db.Set<AiPilotRouteRow>().SingleAsync(ct);
            await pilot.SelectModelAsync(db, payload.Purpose, "off", "fictional-dispatch-operator", ct);
            db.Add(new AiCreditRow { Id = Guid.NewGuid(), UserId = owner, GrantKey = new string('b', 64),
                Kind = AiCreditEvent.GiftedGrant, GiftedAvailable = 3, CreatedAt = clock.GetUtcNow().AddDays(-3) });
            Seed(queued, AiPilotDispatchStage.Queued);
            Seed(claimed, AiPilotDispatchStage.Claimed);
            Seed(legacy, null);
            await db.SaveChangesAsync(ct);

            void Seed(Guid id, AiPilotDispatchStage? stage)
            {
                var offer = new AiOfferRow { Id = Guid.NewGuid(), UserId = owner, Provider = original.Provider, Model = original.Model!,
                    Purpose = payload.Purpose, PilotRouteId = original.Id, MaximumUnits = 1, ExpiresAt = clock.GetUtcNow().AddDays(-1) };
                db.AddRange(offer, new AiRequestRow { Id = id, UserId = owner, OfferId = offer.Id,
                    PayloadDigest = AiProtocol.Digest(payload), ProtectedPayload = "fictional-protected-aggregate", CreatedAt = clock.GetUtcNow().AddDays(-2),
                    State = AiRequestState.Pending, GiftedReserved = 1, PilotCostCeilingMicros = 1895,
                    PilotDispatchStage = stage, PilotDispatchClaimedAt = stage == AiPilotDispatchStage.Claimed ? clock.GetUtcNow().AddDays(-2) : null },
                    new AiCreditRow { Id = Guid.NewGuid(), UserId = owner, RequestId = id, Kind = AiCreditEvent.Reserve,
                        GiftedAvailable = -1, GiftedReserved = 1, CreatedAt = clock.GetUtcNow().AddDays(-2) });
            }
        }

        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner),
            new Claim(ServiceHost.SessionClaim, session.ToString("D")), new Claim(ServiceHost.GenerationClaim, "1")], "synthetic"));
        var protection = new EphemeralDataProtectionProvider();
        AiRequestStore FreshStore() => new(factory, new UnconfiguredAiProvider(), protection, clock, new AiPilotProvider(config, transport, clock));
        Assert.Equal(AiRequestState.Pending, (await FreshStore().StatusAsync(actor, queued, ct)).State);
        Assert.Empty(transport.Calls);
        transport.InspectBeforeCreate = async () =>
        {
            await using var db = factory.CreateDbContext();
            var saved = await db.Set<AiRequestRow>().SingleAsync(row => row.Id == queued, ct);
            Assert.Equal(AiPilotDispatchStage.Claimed, saved.PilotDispatchStage);
            Assert.Equal(clock.GetUtcNow(), saved.PilotDispatchClaimedAt);
            Assert.Null(saved.VendorOperationId);
        };
        await Task.WhenAll(FreshStore().DrainPilotDispatchesAsync(ct), FreshStore().DrainPilotDispatchesAsync(ct));
        Assert.Equal((HttpMethod.Post, "generativelanguage.googleapis.com", "/v1beta/interactions"), Assert.Single(transport.Calls));

        clock.Advance(TimeSpan.FromDays(30));
        await FreshStore().DrainPilotDispatchesAsync(ct);
        Assert.Single(transport.Calls);
        await using var observed = factory.CreateDbContext();
        var rows = await observed.Set<AiRequestRow>().ToDictionaryAsync(row => row.Id, ct);
        Assert.Equal(AiPilotDispatchStage.Claimed, rows[queued].PilotDispatchStage);
        Assert.Equal(AiPilotDispatchStage.Claimed, rows[claimed].PilotDispatchStage);
        Assert.Null(rows[legacy].PilotDispatchStage);
        Assert.All(rows.Values, row => { Assert.Equal(AiRequestState.Pending, row.State); Assert.Null(row.VendorOperationId); });
        Assert.Equal(4, await observed.Set<AiCreditRow>().CountAsync(ct));
    }

    /// <summary>Separates confirmed vendor usage from customer settlement for unpublishable original output.</summary>
    [Fact]
    public async Task Pilot_records_terminal_usage_without_settling_unusable_output()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_PROVIDER_PILOT_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated provider-pilot PostgreSQL fixture required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", 55479, "zanance_ai_provider_pilot"), (settings.Host, settings.Port, settings.Database));
        var factory = new Factory(connection);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        const string owner = "fictional-metered-pilot";
        var session = Guid.NewGuid();
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.MigrateAsync(ct);
            Assert.False(db.Database.HasPendingModelChanges());
            db.Users.Add(new ServiceUser { Id = owner, UserName = owner, SecurityStamp = "fictional-metered-stamp" });
            db.Sessions.Add(new DeviceSession { Id = session, UserId = owner, Generation = 1, ExpiresAt = clock.GetUtcNow().AddDays(1) });
            await db.SaveChangesAsync(ct);
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Analysis:ProviderPilot:openai:ApiKey"] = "fictional-metering-only",
            ["Analysis:ProviderPilot:openai:Project"] = "fixture-metering-openai",
            ["Analysis:ProviderPilot:gemini:ApiKey"] = "fictional-metering-only",
            ["Analysis:ProviderPilot:gemini:Project"] = "fixture-metering-gemini",
        }).Build();
        using var transport = new MeteringTransport();
        var pilot = new AiPilotProvider(config, transport, clock);
        var store = new AiRequestStore(factory, new UnconfiguredAiProvider(), new EphemeralDataProtectionProvider(), clock, pilot);
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner),
            new Claim(ServiceHost.SessionClaim, session.ToString("D")), new Claim(ServiceHost.GenerationClaim, "1")], "synthetic"));
        var payload = AiPilotProvider.ReportFixture;
        await store.GrantAsync(owner, "fictional-metered-credits", false, 2, ct);
        var report = await SelectAndQuote("gpt-6-luna");
        var rejected = await store.SubmitAsync(actor, report, ct);
        Assert.Equal(AiRequestState.Pending, rejected.State);
        Assert.Null(rejected.Text);
        await ExpectUsage(report.RequestId, (100, 50, 25, 20, 35));
        var alternative = await SelectAndQuote("gemini-3.5-flash-lite");
        var overLimit = await store.SubmitAsync(actor, alternative, ct);
        Assert.Equal(AiRequestState.Pending, overLimit.State);
        Assert.Null(overLimit.Text);
        await ExpectUsage(alternative.RequestId, (48, 520, 8, 20, 1315));

        var balance = (await store.AvailabilityAsync(actor, ct)).Balance;
        Assert.Equal(new AiBalance(0, 0, 0, 2), balance);
        clock.Advance(TimeSpan.FromSeconds(3));
        var conflicting = await store.StatusAsync(actor, report.RequestId, ct);
        Assert.Equal(AiRequestState.Pending, conflicting.State);
        await ExpectUsage(report.RequestId, (100, 50, 25, 20, 35));
        clock.Advance(TimeSpan.FromSeconds(3));
        var resolved = await store.StatusAsync(actor, report.RequestId, ct);
        Assert.Equal(AiRequestState.Completed, resolved.State);
        Assert.Equal(1L, resolved.ChargedUnits);
        await ExpectUsage(report.RequestId, (100, 50, 25, 20, 35));
        Assert.Equal(new AiBalance(0, 0, 0, 1), (await store.AvailabilityAsync(actor, ct)).Balance);
        Assert.Equal(2, transport.Creates);
        Assert.Equal(2, transport.Reads);
        await using var audit = factory.CreateDbContext();
        var settlement = Assert.Single(await audit.Set<AiCreditRow>().Where(row => row.Kind == AiCreditEvent.Settle).ToArrayAsync(ct));
        Assert.Equal(report.RequestId, settlement.RequestId);
        Assert.Empty(await audit.Set<AiCreditRow>().Where(row => row.Kind == AiCreditEvent.Refund).ToArrayAsync(ct));

        async Task<AiSubmission> SelectAndQuote(string model)
        {
            await using var db = factory.CreateDbContext();
            await pilot.SelectModelAsync(db, payload.Purpose, model, "fictional-metering-operator", ct);
            return new(Guid.NewGuid(), await store.OfferAsync(actor, payload.Purpose, ct), payload);
        }
        async Task ExpectUsage(Guid id, (int Input, int Output, int Cached, int Thinking, long Cost) expected)
        {
            await using var db = factory.CreateDbContext();
            var row = await db.Set<AiRequestRow>().SingleAsync(item => item.Id == id, ct);
            Assert.Equal(expected, (row.VendorInputTokens!.Value, row.VendorOutputTokens!.Value, row.VendorCachedInputTokens!.Value,
                row.VendorThinkingTokens!.Value, row.PilotMeteredCostMicros!.Value));
            Assert.Equal(AiPilotUsage.Tariff, row.PilotMeteredTariff);
        }
    }

    /// <summary>Preserves an authenticated terminal vendor failure independently of usage or credit movement.</summary>
    [Fact]
    public async Task Pilot_reports_original_failure_without_guessing_usage_or_releasing_credit()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_TERMINAL_TEST_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated terminal-status PostgreSQL fixture required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", 55480, "zanance_ai_terminal_test"), (settings.Host, settings.Port, settings.Database));
        var factory = new Factory(connection);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        const string owner = "fictional-terminal-status";
        var session = Guid.NewGuid();
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.MigrateAsync(ct);
            Assert.False(db.Database.HasPendingModelChanges());
            db.Users.Add(new ServiceUser { Id = owner, UserName = owner, SecurityStamp = "fictional-terminal-stamp" });
            db.Sessions.Add(new DeviceSession { Id = session, UserId = owner, Generation = 1, ExpiresAt = clock.GetUtcNow().AddDays(1) });
            await db.SaveChangesAsync(ct);
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Analysis:ProviderPilot:gemini:ApiKey"] = "fictional-terminal-only",
            ["Analysis:ProviderPilot:gemini:Project"] = "fixture-terminal-gemini",
        }).Build();
        using var transport = new TerminalStatusTransport();
        var pilot = new AiPilotProvider(config, transport, clock);
        var store = new AiRequestStore(factory, new UnconfiguredAiProvider(), new EphemeralDataProtectionProvider(), clock, pilot);
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner),
            new Claim(ServiceHost.SessionClaim, session.ToString("D")), new Claim(ServiceHost.GenerationClaim, "1")], "synthetic"));
        var payload = AiPilotProvider.ReportFixture;
        await store.GrantAsync(owner, "fictional-terminal-credit", false, 1, ct);
        await using (var db = factory.CreateDbContext())
            await pilot.SelectModelAsync(db, payload.Purpose, "gemini-3.5-flash-lite", "fixture-terminal-operator", ct);
        var id = Guid.NewGuid();
        var result = await store.SubmitAsync(actor, new(id, await store.OfferAsync(actor, payload.Purpose, ct), payload), ct);
        Assert.Equal(AiRequestState.Pending, result.State);
        Assert.Null(await RecordedStatus());

        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(AiRequestState.Pending, await store.CheckPilotStatusAsync("fixture-terminal-operator", id, ct));
        Assert.Null(await RecordedStatus());

        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(AiRequestState.Pending, await store.CheckPilotStatusAsync("fixture-terminal-operator", id, ct));
        Assert.Equal(AiPilotVendorStatus.Failed, await RecordedStatus());

        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(AiRequestState.Pending, await store.CheckPilotStatusAsync("fixture-terminal-operator", id, ct));
        Assert.Equal(AiPilotVendorStatus.Failed, await RecordedStatus());

        Assert.Equal(new AiBalance(0, 0, 0, 1), (await store.AvailabilityAsync(actor, ct)).Balance);
        await using var audit = factory.CreateDbContext();
        Assert.Empty(await audit.Set<AiCreditRow>().Where(row => row.Kind == AiCreditEvent.Settle || row.Kind == AiCreditEvent.Refund).ToArrayAsync(ct));
        Assert.Equal(1, transport.Creates);
        Assert.Equal(3, transport.Reads);

        async Task<AiPilotVendorStatus?> RecordedStatus()
        {
            await using var db = factory.CreateDbContext();
            var row = await db.Set<AiRequestRow>().SingleAsync(item => item.Id == id, ct);
            Assert.Null(row.PilotMeteredCostMicros);
            Assert.Null(row.ProtectedText);
            Assert.Equal("interaction_terminal", row.VendorOperationId);
            return row.PilotVendorStatus;
        }
    }

    private sealed class TerminalStatusTransport : HttpMessageHandler, IHttpClientFactory
    {
        internal int Creates;
        internal int Reads;

        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post)
            {
                Creates++;
                return Task.FromResult(Json("""{"id":"interaction_terminal","status":"in_progress","model":"gemini-3.5-flash-lite"}"""));
            }
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://generativelanguage.googleapis.com/v1beta/interactions/interaction_terminal", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(Json(++Reads switch
            {
                1 => """{"id":"interaction_other","status":"failed","model":"gemini-3.5-flash-lite"}""",
                2 => """{"id":"interaction_terminal","status":"failed","model":"gemini-3.5-flash-lite"}""",
                _ => """{"id":"interaction_terminal","status":"completed","model":"gemini-3.5-flash-lite","usage":{"total_input_tokens":48,"total_output_tokens":50},"steps":[{"type":"model_output","content":[{"type":"text","text":"{\"net_expenses_minor\":38000,\"explanation\":\"Refunds reduce recorded expenses.\"}"}]}]}""",
            }));

            static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
                { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class MeteringTransport : HttpMessageHandler, IHttpClientFactory
    {
        internal int Creates;
        internal int Reads;
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post)
            {
                Creates++;
                return Task.FromResult(request.RequestUri!.Host == "api.openai.com"
                    ? OpenAi(100, 76000)
                    : Json("""{"id":"interaction_metered","status":"completed","model":"gemini-3.5-flash-lite","usage":{"total_input_tokens":48,"total_output_tokens":500,"total_thought_tokens":20,"total_cached_tokens":8},"steps":[{"type":"model_output","content":[{"type":"text","text":"{\"net_expenses_minor\":38000,\"explanation\":\"Recorded expenses less refunds are the shown net amount.\"}"}]}]}"""));
            }
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://api.openai.com/v1/responses/resp_metered", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(OpenAi(++Reads == 1 ? 101 : 100, 38000));
        }

        private static HttpResponseMessage OpenAi(int input, int value)
        {
            var text = System.Text.Json.JsonSerializer.Serialize(new
            {
                net_expenses_minor = value,
                explanation = "Recorded expenses less refunds are the shown net amount.",
            });
            return new(HttpStatusCode.OK) { Content = System.Net.Http.Json.JsonContent.Create(new
            {
                id = "resp_metered", status = "completed", model = "gpt-6-luna",
                usage = new { input_tokens = input, output_tokens = 50, input_tokens_details = new { cached_tokens = 25 },
                    output_tokens_details = new { reasoning_tokens = 20 } },
                output = new[] { new { type = "message", content = new[] { new { type = "output_text", text } } } },
            }) };
        }

        private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
            { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }

    /// <summary>Exercises the fixed, independently scoped multilingual corpus through both adapters.</summary>
    [Fact]
    public async Task Pilot_localizes_only_the_fixed_synthetic_corpus_for_both_adapters()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_PROVIDER_PILOT_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated provider-pilot PostgreSQL fixture required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", 55479, "zanance_ai_provider_pilot"), (settings.Host, settings.Port, settings.Database));
        var factory = new Factory(connection);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Analysis:ProviderPilot:openai:ApiKey"] = "fictional-locale",
            ["Analysis:ProviderPilot:openai:Project"] = "fixture-locale-openai",
            ["Analysis:ProviderPilot:gemini:ApiKey"] = "fictional-locale",
            ["Analysis:ProviderPilot:gemini:Project"] = "fixture-locale-gemini",
        }).Build();
        using var transport = new LocaleCorpusTransport();
        var pilot = new AiPilotProvider(config, transport, clock);
        var store = new AiRequestStore(factory, new UnconfiguredAiProvider(), new EphemeralDataProtectionProvider(), clock, pilot);
        var fixtures = AiPilotLocaleFixtures.All.OrderBy(item => item.Payload.Language, StringComparer.Ordinal)
            .ThenBy(item => item.Payload.Purpose).ToArray();
        Assert.Equal(12, fixtures.Length);
        Assert.Equal(12, fixtures.Select(item => AiProtocol.Digest(item.Payload)).Distinct().Count());
        var actors = new Dictionary<string, ClaimsPrincipal>(StringComparer.Ordinal);
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.MigrateAsync(ct);
            foreach (var locale in fixtures.Select(item => item.Payload.Language).Distinct(StringComparer.Ordinal))
            {
                var owner = "fictional-locale-" + locale;
                var session = Guid.NewGuid();
                db.Users.Add(new ServiceUser { Id = owner, UserName = owner, SecurityStamp = "fictional-locale-stamp" });
                db.Sessions.Add(new DeviceSession { Id = session, UserId = owner, Generation = 1, ExpiresAt = clock.GetUtcNow().AddDays(1) });
                actors.Add(locale, new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner),
                    new Claim(ServiceHost.SessionClaim, session.ToString("D")), new Claim(ServiceHost.GenerationClaim, "1")], "synthetic")));
            }
            await db.SaveChangesAsync(ct);
            foreach (var locale in actors.Keys)
                await store.GrantAsync("fictional-locale-" + locale, "fictional-locale-credit-" + locale, false, 5, ct);
        }

        foreach (var item in fixtures)
        {
            var payload = item.Payload;
            var actor = actors[payload.Language];
            var comparison = payload.Purpose == AiPurpose.PeriodComparison;
            if (comparison)
            {
                Assert.Equal(0, item.Facts["eur_spending_change"]);
                Assert.Equal(2000, item.Facts["usd_spending_change"]);
                Assert.Equal(-12000, item.Facts["eur_cash_flow_change"]);
                Assert.Equal(5000, item.Facts["usd_cash_flow_change"]);
                Assert.False(Assert.IsType<bool>(item.Facts["can_combine"]));
            }
            else Assert.Equal(0, item.Facts["net_expenses_minor"]);
            var modified = payload with { Current = payload.Current with { UnreviewedCount = 1 } };
            Assert.False(AiPilotProvider.IsSynthetic(payload.Purpose, AiProtocol.Digest(modified)));
            foreach (var model in AiPilotProvider.Models(payload.Purpose))
            {
                var result = await Run(item, actor, model);
                Assert.Equal(AiRequestState.Completed, result.State);
                Assert.Equal(transport.Explanation, result.Text);
                Assert.Equal(1L, result.ChargedUnits);
            }
        }

        var sample = Assert.Single(fixtures, item => item.Payload.Language == "fa" && item.Payload.Purpose == AiPurpose.PeriodComparison);
        transport.CombineCurrencies = true;
        var refused = await Run(sample, actors["fa"], "gemini-4-hydrogen");
        Assert.Equal(AiRequestState.Pending, refused.State);
        Assert.Null(refused.Text);
        Assert.Equal(25, transport.Creates);
        await using var audit = factory.CreateDbContext();
        Assert.Equal(24, await audit.Set<AiCreditRow>().CountAsync(row => row.Kind == AiCreditEvent.Settle, ct));

        async Task<AiRequestResult> Run(AiPilotLocaleFixtures.Fixture fixture, ClaimsPrincipal actor, string model)
        {
            await using var db = factory.CreateDbContext();
            await pilot.SelectModelAsync(db, fixture.Payload.Purpose, model, "fictional-locale-operator", ct);
            transport.Fixture = fixture;
            transport.Model = model;
            var offer = await store.OfferAsync(actor, fixture.Payload.Purpose, ct);
            Assert.Equal(model, offer.Model);
            return await store.SubmitAsync(actor, new(Guid.NewGuid(), offer, fixture.Payload), ct);
        }
    }

    private sealed class LocaleCorpusTransport : HttpMessageHandler, IHttpClientFactory
    {
        private static readonly IReadOnlyDictionary<string, (string Name, string Empty, string Comparison)> Locales =
            new Dictionary<string, (string, string, string)>
        {
            ["en"] = ("English", "No activity was recorded in the supplied period.", "Refunds reduce net expenses in each currency separately."),
            ["fa"] = ("Persian", "در بازهٔ ارائه‌شده فعالیتی ثبت نشده است.", "بازپرداخت‌ها هزینهٔ خالص هر ارز را جداگانه کاهش می‌دهند."),
            ["de"] = ("German", "Im angegebenen Zeitraum wurde keine Aktivität erfasst.", "Erstattungen verringern die Nettoausgaben je Währung."),
            ["es"] = ("Spanish", "No se registró actividad durante el período indicado.", "Los reembolsos reducen el gasto neto por separado en cada moneda."),
            ["fr"] = ("French", "Aucune activité n’a été enregistrée pendant la période indiquée.", "Les remboursements réduisent les dépenses nettes séparément pour chaque devise."),
            ["it"] = ("Italian", "Non è stata registrata alcuna attività nel periodo indicato.", "I rimborsi riducono le spese nette separatamente per ciascuna valuta."),
        };

        internal AiPilotLocaleFixtures.Fixture Fixture { get; set; } = null!;
        internal string Model { get; set; } = "";
        internal string Explanation { get; private set; } = "";
        internal int Creates;
        internal bool CombineCurrencies;
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            using var body = await System.Text.Json.JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(ct), cancellationToken: ct);
            Assert.Equal(Model, body.RootElement.GetProperty("model").GetString());
            var input = body.RootElement.GetProperty("input").GetString();
            var language = Locales[Fixture.Payload.Language];
            Assert.Equal(Fixture.Prompt, input);
            Assert.Contains($"in {language.Name}.", input, StringComparison.Ordinal);
            Explanation = Fixture.Payload.Purpose == AiPurpose.PeriodComparison ? language.Comparison : language.Empty;
            var output = new Dictionary<string, object>(Fixture.Facts) { ["explanation"] = Explanation };
            if (CombineCurrencies) output["can_combine"] = true;
            var text = System.Text.Json.JsonSerializer.Serialize(output);
            var sequence = ++Creates;
            var openAi = Model.StartsWith("gpt-", StringComparison.Ordinal);
            object reply = openAi
                ? new { id = $"resp_locale_{sequence}", status = "completed", model = Model, usage = new { input_tokens = 50, output_tokens = 30 },
                    output = new[] { new { type = "message", content = new[] { new { type = "output_text", text } } } } }
                : new { id = $"interaction_locale_{sequence}", status = "completed", model = Model,
                    usage = new { total_input_tokens = 50, total_output_tokens = 30, total_thought_tokens = 10 },
                    steps = new[] { new { type = "model_output", content = new[] { new { type = "text", text } } } } };
            return new(HttpStatusCode.OK) { Content = System.Net.Http.Json.JsonContent.Create(reply) };
        }
    }
}
