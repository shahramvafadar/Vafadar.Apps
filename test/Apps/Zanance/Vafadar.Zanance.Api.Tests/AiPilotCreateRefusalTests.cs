using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Exercises content-free create refusal evidence against the original reservation and dispatch guards.</summary>
[Trait("Qualification", "AiLocal")]
public sealed class AiPilotCreateRefusalTests
{
    /// <summary>Captures bounded create status without inventing an original handle or replaying a charge.</summary>
    [Fact]
    public async Task Create_refusal_records_only_bounded_HTTP_status_and_keeps_original_reservation()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_CREATE_REFUSAL_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated refusal database required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", "zanance_ai_provider_pilot"), (settings.Host, settings.Database));
        Assert.Contains(settings.Port, new[] { 55479, 55487 });
        const string owner = "fictional-create-refusal";
        var session = Guid.NewGuid();
        var factory = new Factory(connection);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.MigrateAsync(ct);
            Assert.False(db.Database.HasPendingModelChanges());
            db.Users.Add(new ServiceUser { Id = owner, UserName = owner, SecurityStamp = "fictional-refusal-stamp" });
            db.Sessions.Add(new DeviceSession { Id = session, UserId = owner, Generation = 1, ExpiresAt = clock.GetUtcNow().AddHours(1) });
            await db.SaveChangesAsync(ct);
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Analysis:ProviderPilot:openai:ApiKey"] = "fictional-refusal-only",
            ["Analysis:ProviderPilot:openai:Project"] = "fixture-refusal-openai",
            ["Analysis:ProviderPilot:gemini:ApiKey"] = "fictional-refusal-only",
            ["Analysis:ProviderPilot:gemini:Project"] = "fixture-refusal-gemini",
        }).Build();
        const string openAi = "gpt-6-luna";
        const string gemini = "gemini-3.5-flash-lite";
        var cases = new (string Model, HttpStatusCode Status, AiPilotCreateRefusal? Expected)[]
        {
            (openAi, HttpStatusCode.BadRequest, AiPilotCreateRefusal.RequestRejected),
            (openAi, HttpStatusCode.Unauthorized, AiPilotCreateRefusal.AuthenticationRejected),
            (gemini, HttpStatusCode.Forbidden, AiPilotCreateRefusal.AccessDenied),
            (gemini, HttpStatusCode.NotFound, AiPilotCreateRefusal.ResourceUnavailable),
            (openAi, HttpStatusCode.TooManyRequests, AiPilotCreateRefusal.RateLimited),
            (gemini, HttpStatusCode.ServiceUnavailable, null),
        };
        using var replies = new Replies(cases.Select(item =>
            (item.Model == openAi ? "api.openai.com" : "generativelanguage.googleapis.com", item.Status)));
        var pilot = new AiPilotProvider(config, replies, clock);
        var store = new AiRequestStore(factory, new UnconfiguredAiProvider(), new EphemeralDataProtectionProvider(), clock, pilot);
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, owner),
            new Claim(ServiceHost.SessionClaim, session.ToString("D")), new Claim(ServiceHost.GenerationClaim, "1")], "synthetic"));
        var payload = AiPilotProvider.ReportFixture;
        await store.GrantAsync(owner, "fictional-create-refusal-credit", false, cases.Length, ct);
        await using (var db = factory.CreateDbContext())
            await Assert.ThrowsAsync<InvalidOperationException>(() => pilot.SelectModelAsync(db, payload.Purpose,
                "fictional-unknown-model", owner, ct));
        Assert.Equal(0, replies.Calls);

        var admitted = new List<(Guid Id, AiPilotCreateRefusal? Refusal)>();
        foreach (var item in cases)
        {
            await using (var db = factory.CreateDbContext())
                await pilot.SelectModelAsync(db, payload.Purpose, item.Model, owner, ct);
            var packet = new AiSubmission(Guid.NewGuid(), await store.OfferAsync(actor, payload.Purpose, ct), payload);
            Assert.Equal(AiRequestState.Pending, (await store.SubmitAsync(actor, packet, ct)).State);
            Assert.Equal(AiRequestState.Pending, (await store.SubmitAsync(actor, packet, ct)).State);
            admitted.Add((packet.RequestId, item.Expected));
        }
        await store.DrainPilotDispatchesAsync(ct);
        Assert.Equal(cases.Length, replies.Calls);
        Assert.Equal(new AiBalance(0, 0, 0, cases.Length), (await store.AvailabilityAsync(actor, ct)).Balance);
        await using var audit = factory.CreateDbContext();
        var requests = await audit.Set<AiRequestRow>().ToDictionaryAsync(row => row.Id, ct);
        foreach (var (id, refusal) in admitted)
        {
            var request = requests[id];
            Assert.Equal(refusal, request.PilotCreateRefusal);
            Assert.Equal(AiPilotDispatchStage.Claimed, request.PilotDispatchStage);
            Assert.Null(request.VendorOperationId);
            Assert.Null(request.PilotVendorStatus);
            Assert.Null(request.PilotMeteredCostMicros);
            Assert.Null(request.ProtectedText);
        }
        var movements = await audit.Set<AiCreditRow>().ToArrayAsync(ct);
        Assert.Equal(cases.Length, movements.Count(row => row.Kind == AiCreditEvent.Reserve));
        Assert.DoesNotContain(movements, row => row.Kind is AiCreditEvent.Refund or AiCreditEvent.Settle);
    }

    private sealed class Factory(string connection) : IDbContextFactory<ServiceDbContext>
    {
        public ServiceDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options);
    }

    private sealed class Replies(IEnumerable<(string Host, HttpStatusCode Status)> cases) : HttpMessageHandler, IHttpClientFactory
    {
        private readonly Queue<(string Host, HttpStatusCode Status)> pending = new(cases);
        internal int Calls { get; private set; }

        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.True(pending.TryDequeue(out var next));
            Assert.Equal(next.Host, request.RequestUri!.Host);
            return Task.FromResult(new HttpResponseMessage(next.Status)
                { Content = new StringContent("Fictional untrusted provider diagnostic body") });
        }
    }
}
