using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Npgsql;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Real HTTP fixture peers under the unchanged durable service request budget.</summary>
public sealed partial class BillingClientContinuationTests
{
    /// <summary>Independent HTTP fixtures have separate peers while one peer retains its exact budget across actual workers.</summary>
    [Fact]
    [Trait("AT", "AT-304")]
    public async Task Fictional_http_peers_are_isolated_while_one_peer_budget_is_shared_across_workers()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated billing PostgreSQL fixture required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("127.0.0.1", settings.Host); Assert.Equal(55475, settings.Port); Assert.Equal("zanance_billing_d295", settings.Database);
        var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("ZANANCE_BILLING_QA_ROOT")!);
        Assert.Equal("Zanance fictional billing d295", await File.ReadAllTextAsync(Path.Combine(parent, "fixture.marker"), ct));
        var root = Path.Combine(parent, "http-rate-isolation"); Directory.CreateDirectory(root);
        var complete = Path.Combine(root, "complete.json"); Assert.False(File.Exists(complete), "Do not replay a completed rate qualification.");
        var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(parent, "protection")));
        var clock = new Clock { Now = DateTimeOffset.UtcNow };
        async Task ExpectAsync(HttpClient http, HttpStatusCode expected)
        {
            using var reply = await http.GetAsync("/billing/time/" + Guid.NewGuid().ToString("D"), ct);
            Assert.Equal(expected, reply.StatusCode);
            if (expected == HttpStatusCode.TooManyRequests) Assert.Equal("60", Assert.Single(reply.Headers.GetValues("Retry-After")));
        }
        await using (var first = new BillingHttpHost(connection, parent, protection, new Provider(), "http-rate-worker-a", clock, "http-rate-peer-a"))
        {
            using var http = first.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            for (var count = 0; count < 30; count++) await ExpectAsync(http, HttpStatusCode.OK);
            await ExpectAsync(http, HttpStatusCode.TooManyRequests);
        }
        await using (var independent = new BillingHttpHost(connection, parent, protection, new Provider(), "http-rate-worker-independent", clock, "http-rate-peer-b"))
        {
            using var http = independent.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            await ExpectAsync(http, HttpStatusCode.OK);
        }
        await using (var following = new BillingHttpHost(connection, parent, protection, new Provider(), "http-rate-worker-b", clock, "http-rate-peer-a"))
        {
            using var http = following.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            await ExpectAsync(http, HttpStatusCode.TooManyRequests);
            clock.Now = clock.Now.AddMinutes(1);
            await ExpectAsync(http, HttpStatusCode.OK);
        }
        await File.WriteAllTextAsync(complete, "{\"IndependentPeers\":true,\"OriginalBudgetRetainedAcrossWorkers\":true,\"RetryAfterPreserved\":true,\"NextWindowAccepted\":true,\"ProductionLimiterChanged\":false}", ct);
    }
}
