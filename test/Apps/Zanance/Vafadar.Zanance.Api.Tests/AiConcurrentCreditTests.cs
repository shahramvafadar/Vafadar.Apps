using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Api;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Separate real worker stores exercise the PostgreSQL owner lock, without client-side HTTP serialization.</summary>
[Trait("AT", "AT-298")][Trait("Qualification", "AiLocal")]
public sealed class AiConcurrentCreditTests
{
    private sealed class Factory(string connection) : IDbContextFactory<ServiceDbContext>
    { public ServiceDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options); }
    private sealed class Provider(string root) : IAiProvider
    {
        internal bool Resolve;
        public AiProviderOffer? Offer(AiPurpose purpose) => new("fictitious-concurrent", "fixture-v1", 8);
        public async Task<AiProviderOutcome> BeginAsync(string operation, AiOffer offer, AiPayload payload, CancellationToken ct)
        {
            await using var proof = new FileStream(Path.Combine(root, operation + ".dispatched"), FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await proof.WriteAsync("original"u8.ToArray(), ct); return new(AiProviderState.Unknown);
        }
        public Task<AiProviderOutcome> StatusAsync(string operation, AiOffer offer, CancellationToken ct) => Task.FromResult(new AiProviderOutcome(Resolve ? AiProviderState.DefinitiveNoCharge : AiProviderState.Unknown));
    }
    [Fact]
    public async Task Separate_workers_cannot_reserve_the_same_available_units_twice()
    {
        var ct = TestContext.Current.CancellationToken; var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_QA_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit owned analysis PostgreSQL fixture required."); return; }
        var settings = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55478, settings.Port); Assert.Equal("zanance_ai_d298", settings.Database);
        var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_AI_QA_ROOT")!);
        Assert.Equal("Zanance fictional analysis d298", await File.ReadAllTextAsync(Path.Combine(parent, "fixture.marker"), ct));
        var root = Path.Combine(parent, "separate-workers"); Directory.CreateDirectory(root); var complete = Path.Combine(root, "complete.json");
        Assert.False(File.Exists(complete), "Do not replay completed concurrent worker qualification.");
        var factory = new Factory(connection); var provider = new Provider(root);
        var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "keys")));
        var first = new AiRequestStore(factory, provider, protection, TimeProvider.System); var second = new AiRequestStore(factory, provider, protection, TimeProvider.System);
        const string owner = "fictitious-ai-workers-d298";
        var sessionFile = Path.Combine(root, "session.txt");
        var session = File.Exists(sessionFile) ? Guid.Parse(await File.ReadAllTextAsync(sessionFile, ct)) : Guid.NewGuid();
        if (!File.Exists(sessionFile)) await File.WriteAllTextAsync(sessionFile, session.ToString("D"), ct);
        await using (var db = factory.CreateDbContext())
        {
            if (!await db.Users.AnyAsync(u => u.Id == owner, ct))
            {
                db.Users.Add(new ServiceUser { Id = owner, UserName = owner, SecurityStamp = Guid.NewGuid().ToString("N") });
                db.Sessions.Add(new DeviceSession { Id = session, UserId = owner, Generation = 1, ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) }); await db.SaveChangesAsync(ct);
            }
        }
        var actor = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, owner), new Claim(ServiceHost.SessionClaim, session.ToString("D")), new Claim(ServiceHost.GenerationClaim, "1") }, "fictitious-original-workers"));
        await first.GrantAsync(owner, "fictitious-original-worker-wallet", true, 10, ct);
        async Task<AiSubmission> Original(string name)
        {
            var path = Path.Combine(root, name + ".json"); if (File.Exists(path)) return JsonSerializer.Deserialize(await File.ReadAllTextAsync(path, ct), AiJson.Default.AiSubmission)!;
            var payload = new AiPayload(1, AiPurpose.PeriodExplanation, "de", new(new(2026, 10, 1), new(2026, 10, 17), 1, true, [new("EUR", 100, 0, 50, 0)], 0), null);
            var submission = new AiSubmission(Guid.NewGuid(), await first.OfferAsync(actor, payload.Purpose, ct), payload);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(submission, AiJson.Default.AiSubmission), ct); return submission;
        }
        var a = await Original("first"); var b = await Original("second"); var reserved = Path.Combine(root, "reserved.json");
        if (!File.Exists(reserved))
        {
            var results = await Task.WhenAll(first.SubmitAsync(actor, a, ct), second.SubmitAsync(actor, b, ct));
            Assert.Single(results, r => r.State == AiRequestState.Pending); Assert.Single(results, r => r.State == AiRequestState.NotAccepted);
            Assert.Equal(new AiBalance(2, 0, 8, 0), (await first.AvailabilityAsync(actor, ct)).Balance);
            await File.WriteAllTextAsync(reserved, JsonSerializer.Serialize(results.Single(r => r.State == AiRequestState.Pending).RequestId), ct);
        }
        provider.Resolve = true; var pending = JsonSerializer.Deserialize<Guid>(await File.ReadAllTextAsync(reserved, ct));
        Assert.Equal(AiRequestState.Refunded, (await second.StatusAsync(actor, pending, ct)).State);
        Assert.Equal(new AiBalance(10, 0, 0, 0), (await first.AvailabilityAsync(actor, ct)).Balance);
        Assert.Single(Directory.EnumerateFiles(root, "*.dispatched"));
        await File.WriteAllTextAsync(complete, "{\"SeparateWorkerOwnerLock\":true,\"ReserveEvents\":1,\"DefinitiveRefund\":true}", ct);
    }
    [Fact]
    public void Production_module_has_a_closed_default_provider_and_no_automatic_model_selection()
    {
        var services = new ServiceCollection(); AiModule.Add(services); using var scope = services.BuildServiceProvider();
        var provider = scope.GetRequiredService<IAiProvider>(); Assert.IsType<UnconfiguredAiProvider>(provider);
        Assert.Null(provider.Offer(AiPurpose.PeriodExplanation)); Assert.Null(provider.Offer(AiPurpose.PeriodComparison));
    }
}
