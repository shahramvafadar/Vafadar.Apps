using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Time.Testing;
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
[Trait("Qualification", "AiLocal")]
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

    private sealed class RetainedStatusReplies : HttpMessageHandler, IHttpClientFactory
    {
        internal int Attempts;
        internal const string Unresolved = "fixture-unresolved-reference";
        internal const string Resolved = "fixture-resolved-reference";

        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Null(request.Content);
            var id = request.RequestUri!.Segments[^1];
            Assert.Equal($"https://generativelanguage.googleapis.com/v1beta/interactions/{id}", request.RequestUri.AbsoluteUri);
            Assert.Contains(id, new[] { Unresolved, Resolved });
            Interlocked.Increment(ref Attempts);
            if (id == Unresolved) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id, status = "completed", model = "gemini-4-hydrogen",
                    usage = new { total_input_tokens = 17, total_output_tokens = 11, total_thought_tokens = 5 },
                    steps = new[] { new { type = "model_output", content = new[] { new { type = "text",
                        text = """{"spending_change_minor":13000,"contributing_rows":1,"explanation":"The recorded category accounts for the observed increase."}""" } } } }
                })
            });
        }
    }

    /// <summary>Verifies identity isolation, independent choices, stale form refusal and zero vendor requests.</summary>
    [Fact]
    public Task Operator_can_view_switch_and_disable_each_purpose_in_the_private_panel() => RunPanelAsync(false);

    /// <summary>Verifies the bounded pending view without replaying accepted model selections.</summary>
    [Fact]
    public Task Operator_sees_only_bounded_original_pending_metadata() => RunPanelAsync(true);

    /// <summary>Verifies the real authenticated status action against a synthetic retained operation.</summary>
    [Fact]
    public Task Operator_checks_the_original_reference_with_durable_limits() => RunPanelAsync(false, exerciseStatus: true);

    private static async Task RunPanelAsync(bool pendingOnly, bool exerciseStatus = false)
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
        using var replies = exerciseStatus ? new RetainedStatusReplies() : null;
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));
        builder.Services.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>((IHttpClientFactory?)replies ?? noEgress));
        if (exerciseStatus) builder.Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));
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
        if (replies is not null)
        {
            using var statusBrowser = Browser();
            using var login = await SignIn(statusBrowser, OperatorEmail);
            Assert.Equal(Panel, login.Headers.Location?.OriginalString);
            await VerifyStatusChecksAsync(app.Services, statusBrowser, replies, clock, ct);
            return;
        }
        if (pendingOnly)
        {
            using var pendingBrowser = Browser();
            using var anonymousQueue = await pendingBrowser.GetAsync(Panel + "/pending", ct);
            Assert.Equal(Panel + "/login", anonymousQueue.Headers.Location?.OriginalString);
            using var login = await SignIn(pendingBrowser, OperatorEmail);
            Assert.Equal(Panel, login.Headers.Location?.OriginalString);
            Assert.Contains("No pending pilot requests.", await pendingBrowser.GetStringAsync(Panel + "/pending", ct), StringComparison.Ordinal);
            await VerifyPendingAsync(app.Services, pendingBrowser, ct);
            Assert.Equal(0, noEgress.Attempts);
            return;
        }

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

    private static async Task VerifyPendingAsync(IServiceProvider services, HttpClient browser, CancellationToken ct)
    {
        const string memberId = "fictional-ai-member";
        var factory = services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
        await using var db = await factory.CreateDbContextAsync(ct);
        var now = new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
        var original = new AiPilotRouteRow { Provider = "gemini", Purpose = AiPurpose.PeriodComparison,
            Model = "gemini-4-hydrogen", Revision = "fictional-first", Actor = OperatorId, SelectedAt = now };
        db.Add(original);
        await db.SaveChangesAsync(ct);
        db.Add(new AiPilotRouteRow { Provider = "off", Purpose = original.Purpose, Revision = "fictional-off",
            Actor = OperatorId, SelectedAt = now.AddMinutes(1) });
        var pending = Enumerable.Range(0, 52).Select(index => AddRequest(AiRequestState.Pending, original.Id, index)).ToArray();
        var terminal = AddRequest(AiRequestState.Completed, original.Id, -2);
        var ordinary = AddRequest(AiRequestState.Pending, null, -1);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        using var reply = await browser.GetAsync(Panel + "/pending", ct);
        reply.EnsureSuccessStatusCode();
        Assert.True(reply.Headers.CacheControl?.NoStore);
        var html = await reply.Content.ReadAsStringAsync(ct);
        Assert.Contains("Showing the oldest 50 requests.", html, StringComparison.Ordinal);
        Assert.Equal(51, Regex.Matches(html, "<tr>", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)).Count);
        Assert.Contains("gemini / gemini-4-hydrogen", html, StringComparison.Ordinal);
        Assert.Contains("2026-11-19 12:02:00", html, StringComparison.Ordinal);
        Assert.Contains(">Recorded</td>", html, StringComparison.Ordinal);
        Assert.Contains(">Missing</td>", html, StringComparison.Ordinal);
        Assert.Contains(pending[0].Id.ToString("D"), html, StringComparison.Ordinal);
        Assert.Contains(pending[49].Id.ToString("D"), html, StringComparison.Ordinal);
        Assert.True(html.IndexOf(pending[0].Id.ToString("D"), StringComparison.Ordinal) <
            html.IndexOf(pending[49].Id.ToString("D"), StringComparison.Ordinal));
        Assert.DoesNotContain(pending[50].Id.ToString("D"), html, StringComparison.Ordinal);
        Assert.DoesNotContain(terminal.Id.ToString("D"), html, StringComparison.Ordinal);
        Assert.DoesNotContain(ordinary.Id.ToString("D"), html, StringComparison.Ordinal);
        foreach (var hidden in new[] { memberId, "fictional-protected-aggregate", "fictional-protected-result", "fictional-vendor-reference", new string('a', 64) })
            Assert.DoesNotContain(hidden, html, StringComparison.Ordinal);
        var unchanged = await db.Set<AiRequestRow>().SingleAsync(row => row.Id == pending[0].Id, ct);
        Assert.Equal(pending[0].StatusCheckedAt, unchanged.StatusCheckedAt);
        Assert.Equal(AiRequestState.Pending, unchanged.State);
        Assert.Equal(54, await db.Set<AiRequestRow>().CountAsync(ct));
        Assert.Equal(2, await db.Set<AiPilotRouteRow>().CountAsync(ct));
        Assert.Empty(await db.Set<AiCreditRow>().ToArrayAsync(ct));

        AiRequestRow AddRequest(AiRequestState state, long? route, int index)
        {
            var offer = new AiOfferRow { Id = Guid.NewGuid(), UserId = memberId, Purpose = AiPurpose.PeriodComparison,
                Provider = "gemini", Model = "gemini-4-hydrogen", MaximumUnits = 1, PilotRouteId = route, ExpiresAt = now.AddHours(1) };
            var request = new AiRequestRow { Id = Guid.NewGuid(), UserId = memberId, OfferId = offer.Id, State = state,
                CreatedAt = now.AddSeconds(index), StatusCheckedAt = index == 0 ? now.AddMinutes(2) : null,
                VendorOperationId = index == 0 ? "fictional-vendor-reference" : null, PayloadDigest = new('a', 64),
                GiftedReserved = 1, ChargedUnits = state == AiRequestState.Completed ? 1 : 0,
                ProtectedPayload = "fictional-protected-aggregate", ProtectedText = state == AiRequestState.Completed ? "fictional-protected-result" : null };
            db.Add(offer);
            db.Add(request);
            return request;
        }
    }

    private static async Task VerifyStatusChecksAsync(IServiceProvider services, HttpClient browser, RetainedStatusReplies replies,
        FakeTimeProvider clock, CancellationToken ct)
    {
        const string member = "fictional-ai-member";
        var purpose = AiPurpose.PeriodComparison;
        var pilot = services.GetRequiredService<AiPilotProvider>();
        var factory = services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
        await using var db = await factory.CreateDbContextAsync(ct);
        await pilot.SelectModelAsync(db, purpose, "gemini-4-hydrogen", OperatorId, ct);
        var original = await db.Set<AiPilotRouteRow>().SingleAsync(ct);
        await pilot.SelectModelAsync(db, purpose, "off", OperatorId, ct);
        var unresolved = Seed(RetainedStatusReplies.Unresolved);
        var resolved = Seed(RetainedStatusReplies.Resolved);
        var missing = Seed(null);
        var unsupported = Seed("fixture-unsupported", new string('a', 64));
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var page = await browser.GetStringAsync(Panel + "/pending", ct);
        var token = Token(page);
        var start = clock.GetUtcNow();
        using (var refusal = await Check(unresolved, includeToken: false))
            Assert.Equal(HttpStatusCode.BadRequest, refusal.StatusCode);
        await Expect(missing, "unavailable");
        await Expect(unsupported, "unavailable");
        Assert.Equal(0, replies.Attempts);
        var concurrent = await Task.WhenAll(Check(unresolved), Check(unresolved));
        Assert.Equal(new[] { "pending", "unavailable" }, concurrent.Select(reply => State(reply)).Order(StringComparer.Ordinal));
        foreach (var reply in concurrent) reply.Dispose();
        Assert.Equal(1, replies.Attempts);
        clock.Advance(TimeSpan.FromSeconds(59));
        await Expect(unresolved, "unavailable");
        clock.Advance(TimeSpan.FromSeconds(1));
        await Expect(unresolved, "pending");
        clock.Advance(TimeSpan.FromMinutes(1));
        await Expect(unresolved, "pending");
        clock.Advance(TimeSpan.FromMinutes(1));
        await Expect(unresolved, "unavailable");
        Assert.Equal(3, replies.Attempts);
        await Expect(resolved, "resolved");
        await Expect(resolved, "unavailable");
        Assert.Equal(4, replies.Attempts);

        var audit = await db.Set<AiPilotStatusCheckRow>().Where(row => row.RequestId == unresolved).OrderBy(row => row.Attempt).ToArrayAsync(ct);
        Assert.Equal(new[] { 1, 2, 3 }, audit.Select(row => row.Attempt));
        Assert.Equal(new[] { start, start.AddMinutes(1), start.AddMinutes(2) }, audit.Select(row => row.CheckedAt));
        Assert.Equal(4, await db.Set<AiPilotStatusCheckRow>().CountAsync(ct));
        Assert.All(await db.Set<AiPilotStatusCheckRow>().ToListAsync(ct), row => Assert.Equal(OperatorId, row.Actor));
        var originalRequest = await db.Set<AiRequestRow>().SingleAsync(row => row.Id == unresolved, ct);
        Assert.Equal(AiRequestState.Pending, originalRequest.State);
        Assert.Equal(RetainedStatusReplies.Unresolved, originalRequest.VendorOperationId);
        var completed = await db.Set<AiRequestRow>().SingleAsync(row => row.Id == resolved, ct);
        Assert.Equal(AiRequestState.Completed, completed.State);
        Assert.Equal((17, 16), (completed.VendorInputTokens!.Value, completed.VendorOutputTokens!.Value));
        Assert.Equal(1L, completed.ChargedUnits);
        var settlement = Assert.Single(await db.Set<AiCreditRow>().Where(row => row.Kind != AiCreditEvent.Reserve).ToArrayAsync(ct));
        Assert.Equal(resolved, settlement.RequestId);
        Assert.Equal(AiCreditEvent.Settle, settlement.Kind);
        var remaining = await browser.GetStringAsync(Panel + "/pending", ct);
        Assert.DoesNotContain(resolved.ToString("D"), remaining, StringComparison.Ordinal);
        Assert.Contains("Check limit reached", remaining, StringComparison.Ordinal);

        Guid Seed(string? handle, string? digest = null)
        {
            var offer = new AiOfferRow { Id = Guid.NewGuid(), UserId = member, Purpose = purpose, Provider = original.Provider,
                Model = original.Model!, MaximumUnits = 1, PilotRouteId = original.Id, ExpiresAt = clock.GetUtcNow().AddHours(1) };
            var request = new AiRequestRow { Id = Guid.NewGuid(), UserId = member, State = AiRequestState.Pending, OfferId = offer.Id,
                GiftedReserved = 1, CreatedAt = clock.GetUtcNow(), VendorOperationId = handle,
                PayloadDigest = digest ?? AiProtocol.Digest(AiPilotProvider.SpendingQuestionFixture), ProtectedPayload = "fictional-protected" };
            db.AddRange(offer, request, new AiCreditRow { Id = Guid.NewGuid(), UserId = member, RequestId = request.Id,
                Kind = AiCreditEvent.Reserve, GiftedAvailable = -1, GiftedReserved = 1, CreatedAt = clock.GetUtcNow() });
            return request.Id;
        }
        async Task<HttpResponseMessage> Check(Guid request, bool includeToken = true)
        {
            var fields = new Dictionary<string, string> { ["request"] = request.ToString("D") };
            if (includeToken) fields["__RequestVerificationToken"] = token;
            using var form = new FormUrlEncodedContent(fields);
            return await browser.PostAsync(Panel + "/pending/check", form, ct);
        }
        static string State(HttpResponseMessage reply) => reply.Headers.Location!.OriginalString.Split('=')[^1];
        async Task Expect(Guid request, string state)
        {
            using var reply = await Check(request);
            Assert.Equal(HttpStatusCode.Redirect, reply.StatusCode);
            Assert.Equal(state, State(reply));
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
