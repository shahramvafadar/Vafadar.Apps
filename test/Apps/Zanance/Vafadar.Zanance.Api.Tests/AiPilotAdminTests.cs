using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Api.Sync;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Exercises the real operator forms over TLS against an isolated PostgreSQL database.</summary>
public sealed class AiPilotAdminTests
{
    private const string Panel = "/admin/analysis";
    private const string OperatorEmail = "fictional-operator@example.invalid";
    private const string MemberEmail = "fictional-member@example.invalid";
    private const string FixturePassword = "Fictional-Admin-Fixture-9!";
    private const string OperatorId = "fictional-ai-admin";

    private sealed class NoProviderTraffic : IHttpClientFactory
    {
        internal int Attempts;
        public HttpClient CreateClient(string name)
        {
            Interlocked.Increment(ref Attempts);
            throw new InvalidOperationException("Admin forms must not contact a model provider.");
        }
    }

    /// <summary>Verifies identity isolation, independent choices, stale form refusal and zero vendor requests.</summary>
    [Fact]
    public async Task Operator_can_view_switch_and_disable_each_purpose_in_the_private_panel()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_ADMIN_TEST_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated admin PostgreSQL fixture required."); return; }
        var dbSettings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", 55479, "zanance_ai_provider_pilot"), (dbSettings.Host, dbSettings.Port, dbSettings.Database));
        var root = Environment.GetEnvironmentVariable("ZANANCE_AI_ADMIN_TEST_ROOT")!;
        Assert.Equal("Zanance synthetic admin panel", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Service"] = connection, ["Service:Root"] = Path.Combine(root, "service"),
            ["Analysis:LocalQualification"] = "true", ["Analysis:ProviderPilot:Enabled"] = "true",
            ["Analysis:ProviderPilot:OperatorUserId"] = OperatorId,
            ["Analysis:ProviderPilot:openai:Project"] = "fixture-openai", ["Analysis:ProviderPilot:openai:ApiKey"] = "fixture-only",
            ["Analysis:ProviderPilot:gemini:Project"] = "fixture-gemini", ["Analysis:ProviderPilot:gemini:ApiKey"] = "fixture-only",
        });
        builder.AddZananceService();
        builder.Logging.ClearProviders();
        builder.Services.RemoveAll<IHostedService>();
        var noEgress = new NoProviderTraffic();
        builder.Services.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(noEgress));
        await using var app = builder.Build();
        await ServiceHost.MigrateAsync(app.Services);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
            foreach (var (id, email) in new[] { (OperatorId, OperatorEmail), ("fictional-ai-member", MemberEmail) })
                Assert.True((await users.CreateAsync(new ServiceUser { Id = id, Email = email, UserName = email, EmailConfirmed = true }, FixturePassword)).Succeeded);
        }
        app.UseZananceService(); app.MapZananceService();
        await app.StartAsync(ct);
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var fingerprint = app.Services.GetRequiredService<LocalSyncCertificate>().Fingerprint;
        HttpClient Browser() => new(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = (_, certificate, _, _) => certificate is not null &&
                Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(certificate.RawData)) == fingerprint,
        }) { BaseAddress = new Uri(address) };
        using var browser = Browser();
        using var member = Browser();
        using var anonymous = await browser.GetAsync(Panel, ct);
        Assert.Equal(Panel + "/login", anonymous.Headers.Location?.OriginalString);
        using var neverSignedIn = await Post(browser, Panel + "/selection", ("purpose", "report"), ("model", "gpt-6-luna"), ("revision", "0"));
        Assert.Equal(Panel + "/login", neverSignedIn.Headers.Location?.OriginalString);
        using var nonOperator = await SignIn(member, MemberEmail);
        Assert.Equal(HttpStatusCode.Unauthorized, nonOperator.StatusCode);
        using var authenticated = await SignIn(browser, OperatorEmail);
        Assert.Equal(Panel, authenticated.Headers.Location?.OriginalString);
        var cookie = Assert.Single(authenticated.Headers.GetValues("Set-Cookie"), value => value.StartsWith("__Host-ZananceAnalysisOperator=", StringComparison.Ordinal));
        Assert.Contains("; secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("; httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("; samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var page = await ReadPanel("Off", "Off");
        using var missingCsrf = await Post(browser, Panel + "/selection", ("purpose", "report"), ("model", "gpt-6-luna"), ("revision", "0"));
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        using var wrongModel = await Save(page, "report", "gemini-4-hydrogen");
        Assert.Equal(Panel + "?unavailable=1", wrongModel.Headers.Location?.OriginalString);
        var stalePage = page;
        await Change("report", "gpt-6-luna", "gpt-6-luna", "Off");
        using var conflict = await Save(stalePage, "report", "off");
        Assert.Equal(Panel + "?conflict=1", conflict.Headers.Location?.OriginalString);
        // The pre-report-change comparison form remains valid because its revision is independent.
        using var independent = await Save(stalePage, "comparison", "gpt-6.2-sol");
        Assert.Equal(Panel + "?saved=comparison", independent.Headers.Location?.OriginalString);
        page = await ReadPanel("gpt-6-luna", "gpt-6.2-sol");
        await Change("report", "gemini-3.5-flash-lite", "gemini-3.5-flash-lite", "gpt-6.2-sol");
        await Change("comparison", "gemini-4-hydrogen", "gemini-3.5-flash-lite", "gemini-4-hydrogen");
        await Change("report", "off", "Off", "gemini-4-hydrogen");
        await Change("comparison", "off", "Off", "Off");

        var factory = app.Services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            var audit = await db.Set<AiPilotRouteRow>().OrderBy(r => r.Id).ToArrayAsync(ct);
            Assert.Equal(new[] { "gpt-6-luna", "gpt-6.2-sol", "gemini-3.5-flash-lite", "gemini-4-hydrogen", null, null }, audit.Select(r => r.Model));
            Assert.Equal(AiPurpose.PeriodExplanation, audit[^2].Purpose);
            Assert.Equal(AiPurpose.PeriodComparison, audit[^1].Purpose);
            Assert.All(audit, row => Assert.Equal(OperatorId, row.Actor));
            Assert.Empty(await db.Set<AiRequestRow>().ToArrayAsync(ct));
            Assert.Empty(await db.Set<AiCreditRow>().ToArrayAsync(ct));
        }
        Assert.Equal(0, noEgress.Attempts);
        using var logout = await Post(browser, Panel + "/logout", ("__RequestVerificationToken", Token(page)));
        Assert.Equal(Panel + "/login", logout.Headers.Location?.OriginalString);
        using var afterLogout = await browser.GetAsync(Panel, ct);
        Assert.Equal(Panel + "/login", afterLogout.Headers.Location?.OriginalString);

        async Task<HttpResponseMessage> SignIn(HttpClient client, string email)
        {
            var login = await client.GetStringAsync(Panel + "/login", ct);
            return await Post(client, Panel + "/login", ("__RequestVerificationToken", Token(login)), ("email", email), ("password", FixturePassword));
        }
        async Task<string> ReadPanel(string report, string comparison)
        {
            using var reply = await browser.GetAsync(Panel, ct);
            reply.EnsureSuccessStatusCode();
            Assert.True(reply.Headers.CacheControl?.NoStore);
            Assert.Contains("frame-ancestors 'none'", Assert.Single(reply.Headers.GetValues("Content-Security-Policy")), StringComparison.Ordinal);
            var html = await reply.Content.ReadAsStringAsync(ct);
            Assert.Contains($"id=\"report-current\">{report}</strong>", html, StringComparison.Ordinal);
            Assert.Contains($"id=\"comparison-current\">{comparison}</strong>", html, StringComparison.Ordinal);
            return html;
        }
        Task<HttpResponseMessage> Save(string html, string purpose, string model) => Post(browser, Panel + "/selection",
            ("__RequestVerificationToken", Token(html)), ("purpose", purpose), ("model", model),
            ("revision", Capture(html, $"name=\"purpose\" value=\"{purpose}\">\\s*<input type=\"hidden\" name=\"revision\" value=\"([0-9]+)\"")));
        async Task Change(string purpose, string model, string report, string comparison)
        {
            using var saved = await Save(page, purpose, model);
            Assert.Equal(Panel + "?saved=" + purpose, saved.Headers.Location?.OriginalString);
            page = await ReadPanel(report, comparison);
        }
        async Task<HttpResponseMessage> Post(HttpClient client, string path, params (string Name, string Value)[] fields)
        {
            using var body = new FormUrlEncodedContent(fields.Select(field => new KeyValuePair<string, string>(field.Name, field.Value)));
            return await client.PostAsync(path, body, ct);
        }
    }

    private static string Token(string html) => Capture(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"");
    private static string Capture(string html, string expression)
    {
        var match = Regex.Match(html, expression, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        Assert.True(match.Success);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}
