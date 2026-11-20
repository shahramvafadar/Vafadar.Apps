using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
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

    private sealed class RecoveryReplies(string provider, bool failed) : HttpMessageHandler, IHttpClientFactory
    {
        internal int Calls;
        internal const string Matched = "resp_fixture_op_matched";
        internal const string Foreign = "resp_fixture_op_foreign";
        internal string Correlation { get; set; } = "";
        internal int CreateCalls;
        private string? geminiInput;
        internal static readonly byte[] SigningKey = Enumerable.Repeat((byte)23, 32).ToArray();

        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (provider == "gemini") return await GeminiAsync(request, cancellationToken);
            Interlocked.Increment(ref Calls);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Null(request.Content);
            Assert.Equal("fixture-openai", Assert.Single(request.Headers.GetValues("OpenAI-Project")));
            var id = request.RequestUri!.Segments[^1];
            Assert.Contains(id, new[] { Matched, Foreign });
            Assert.Equal($"https://api.openai.com/v1/responses/{id}", request.RequestUri.AbsoluteUri);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id, status = failed ? "failed" : "completed", model = "gpt-6-luna",
                    usage = failed ? null : new { input_tokens = 17, output_tokens = 11 },
                    metadata = new { zanance_operation = id == Matched ? Correlation : new string('f', 64) },
                    output = new[] { new { type = "message", content = new[] { new { type = "output_text",
                        text = """{"net_expenses_minor":38000,"explanation":"Recorded expenses less refunds are the shown net amount."}""" } } } }
                })
            };
        }
        private async Task<HttpResponseMessage> GeminiAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.Equal("fixture-only", Assert.Single(request.Headers.GetValues("x-goog-api-key")));
            var uri = request.RequestUri!;
            if (request.Method == HttpMethod.Post)
            {
                Interlocked.Increment(ref CreateCalls);
                Assert.Equal("https://generativelanguage.googleapis.com/v1beta/interactions", uri.AbsoluteUri);
                using var body = await System.Text.Json.JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(ct), cancellationToken: ct);
                geminiInput = body.RootElement.GetProperty("input").GetString();
                Assert.EndsWith(Correlation, geminiInput, StringComparison.Ordinal);
                throw new TimeoutException("Fictional missing interaction acknowledgement.");
            }
            Interlocked.Increment(ref Calls);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Null(request.Content);
            var id = uri.Segments[^1];
            Assert.Contains(id, new[] { Matched, Foreign });
            Assert.Equal($"https://generativelanguage.googleapis.com/v1beta/interactions/{id}?include_input=true", uri.AbsoluteUri);
            var expectedInput = Assert.IsType<string>(geminiInput);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id, status = failed ? "failed" : "completed", model = "gemini-3.5-flash-lite",
                    usage = failed ? null : new { total_input_tokens = 48, total_output_tokens = 11, total_thought_tokens = 3 },
                    steps = new[]
                    {
                        new { type = "user_input", status = "done", content = new[] { new { type = "text",
                            text = id == Matched ? expectedInput : expectedInput[..^64] + new string('f', 64) } } },
                        new { type = "model_output", status = "done", content = new[] { new { type = "text",
                            text = """{"net_expenses_minor":38000,"explanation":"Recorded expenses less refunds are the shown net amount."}""" } } },
                    },
                })
            };
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

    /// <summary>Verifies signed receipt, authenticated lost-ID retrieval and durable duplicate refusal.</summary>
    [Fact]
    public Task OpenAi_notice_recovers_one_matching_claim_and_rejects_foreign_and_duplicate_results() =>
        RunPanelAsync(false, recoveryProvider: "openai");

    /// <summary>Uses the signed notice and initial provider input to recover one original Gemini create.</summary>
    [Fact]
    public Task Gemini_notice_recovers_only_the_original_input_reference() => RunPanelAsync(false, recoveryProvider: "gemini");

    /// <summary>Recovers a failed operation with a lost initial handle without charging or resending.</summary>
    [Theory]
    [InlineData("openai")]
    [InlineData("gemini")]
    public Task Failure_notice_recovers_only_the_original_failed_operation(string provider) =>
        RunPanelAsync(false, recoveryProvider: provider, recoveryFailure: true);

    /// <summary>Validates provider-specific signing-key overlap, expiry and revocation over the original receiver.</summary>
    [Fact]
    public Task Webhook_rotation_respects_provider_and_explicit_utc_overlap() => RunPanelAsync(false, exerciseRotation: true);

    /// <summary>Verifies the private operations view with an exact aggregate and no provider access.</summary>
    [Fact]
    public Task Operator_operations_shows_local_aggregate_without_accessing_providers() => RunPanelAsync(false, exerciseOperations: true);

    /// <summary>Verifies active capacity independently of retained completion and exhaustion receipts.</summary>
    [Fact]
    public Task Webhook_capacity_counts_only_notices_that_can_still_be_checked() => RunPanelAsync(false, exerciseCapacity: true);

    private static async Task RunPanelAsync(bool pendingOnly, bool exerciseStatus = false, string? recoveryProvider = null,
        bool exerciseRotation = false, bool exerciseOperations = false, bool recoveryFailure = false, bool exerciseCapacity = false)
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable(recoveryFailure
            ? "ZANANCE_AI_FAILURE_NOTICE_CONNECTION_" + recoveryProvider!.ToUpperInvariant()
            : "ZANANCE_AI_ADMIN_TEST_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated admin PostgreSQL fixture required."); return; }
        var dbSettings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", "zanance_ai_provider_pilot"), (dbSettings.Host, dbSettings.Database));
        var expectedPort = recoveryFailure ? (recoveryProvider == "openai" ? 55484 : 55485) : 55479;
        Assert.True(dbSettings.Port == expectedPort || pendingOnly && dbSettings.Port == 55480 || exerciseCapacity && dbSettings.Port == 55488);
        var rootKey = recoveryFailure ? "ZANANCE_AI_FAILURE_NOTICE_ROOT_" + recoveryProvider!.ToUpperInvariant() : "ZANANCE_AI_ADMIN_TEST_ROOT";
        var root = Environment.GetEnvironmentVariable(rootKey)!;
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
        using var recovery = recoveryProvider is null ? null : new RecoveryReplies(recoveryProvider, recoveryFailure);
        if (recovery is not null)
            builder.Configuration[AiPilotCompletionNotices.SecretSetting(recoveryProvider!)] = "whsec_" + Convert.ToBase64String(RecoveryReplies.SigningKey);
        if (exerciseRotation || exerciseCapacity)
        {
            builder.Configuration[AiPilotCompletionNotices.SecretSetting("openai")] = Secret(31);
            builder.Configuration[AiPilotCompletionNotices.SecretSetting("gemini")] = Secret(32);
        }
        builder.AddZananceService();
        builder.Logging.ClearProviders();
        builder.Services.RemoveAll<IHostedService>();
        var noEgress = new NoProviderTraffic();
        using var replies = exerciseStatus ? new RetainedStatusReplies() : null;
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));
        builder.Services.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>((IHttpClientFactory?)replies ?? (IHttpClientFactory?)recovery ?? noEgress));
        if (exerciseStatus || recovery is not null || exerciseRotation || exerciseCapacity) builder.Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));
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
        if (exerciseOperations)
        {
            using var view = Browser();
            using var anonymousOperations = await view.GetAsync(Panel + "/operations", ct);
            Assert.Equal(Panel + "/login", anonymousOperations.Headers.Location?.OriginalString);
            using var signedIn = await SignIn(view, OperatorEmail);
            Assert.Equal(Panel, signedIn.Headers.Location?.OriginalString);
            await VerifyOperationsAsync(app.Services, app.Configuration, view, ct);
            Assert.Equal(0, noEgress.Attempts);
            return;
        }
        if (exerciseCapacity)
        {
            using var receiver = Browser();
            await VerifyCapacityAsync(app.Services, receiver, clock, ct);
            Assert.Equal(0, noEgress.Attempts);
            return;
        }
        if (exerciseRotation)
        {
            using var receiver = Browser();
            await VerifyRotationAsync(app.Services, app.Configuration, receiver, clock, ct);
            Assert.Equal(0, noEgress.Attempts);
            return;
        }
        if (recovery is not null)
        {
            using var callback = Browser();
            await VerifyRecoveryAsync(app.Services, callback, recovery, recoveryProvider!, recoveryFailure, clock, ct);
            return;
        }
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
        Assert.Contains(">Recorded</td><td>—</td><td>Failed</td><td>—</td>", html, StringComparison.Ordinal);
        Assert.Contains(">Missing</td><td>401 Authentication rejected</td><td>—</td><td>—</td>", html, StringComparison.Ordinal);
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
                PilotVendorStatus = index == 0 ? AiPilotVendorStatus.Failed : null,
                PilotCreateRefusal = index == 1 ? AiPilotCreateRefusal.AuthenticationRejected : null,
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

    private static async Task VerifyRecoveryAsync(IServiceProvider services, HttpClient callback,
        RecoveryReplies replies, string provider, bool failed, FakeTimeProvider clock, CancellationToken ct)
    {
        const string owner = "fictional-ai-member";
        var pending = Guid.NewGuid();
        var other = Guid.NewGuid();
        var payload = AiPilotProvider.ReportFixture;
        var factory = services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
        var pilot = services.GetRequiredService<AiPilotProvider>();
        await using var db = await factory.CreateDbContextAsync(ct);
        await pilot.SelectModelAsync(db, payload.Purpose, provider == "openai" ? "gpt-6-luna" : "gemini-3.5-flash-lite", OperatorId, ct);
        var original = await db.Set<AiPilotRouteRow>().SingleAsync(ct);
        await pilot.SelectModelAsync(db, payload.Purpose, "off", OperatorId, ct);
        Seed(pending, provider == "gemini" ? AiPilotDispatchStage.Queued : AiPilotDispatchStage.Claimed);
        Seed(other, AiPilotDispatchStage.Claimed);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        replies.Correlation = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"zanance.analysis.v1\n{owner}\n{pending:N}")));
        if (provider == "gemini") await services.GetRequiredService<AiRequestStore>().DrainPilotDispatchesAsync(ct);
        Assert.Equal(provider == "gemini" ? 1 : 0, replies.CreateCalls);
        var now = clock.GetUtcNow();
        await Send("fixture-delivery-stale", RecoveryReplies.Matched, now.AddMinutes(-6), HttpStatusCode.BadRequest);
        await Send("fixture-delivery-altered", RecoveryReplies.Matched, now, HttpStatusCode.BadRequest, alter: true);
        Assert.Empty(await db.Set<AiPilotCompletionNoticeRow>().ToArrayAsync(ct));
        await Send("fixture-delivery-original", RecoveryReplies.Matched, now, HttpStatusCode.NoContent);
        await Send("fixture-delivery-original", RecoveryReplies.Matched, now, HttpStatusCode.NoContent);
        await Send("fixture-delivery-original", RecoveryReplies.Foreign, now, HttpStatusCode.NoContent);
        await Send("fixture-delivery-duplicate", RecoveryReplies.Matched, now, HttpStatusCode.NoContent);
        await Send("fixture-delivery-foreign", RecoveryReplies.Foreign, now, HttpStatusCode.NoContent);
        Assert.Equal(2, await db.Set<AiPilotCompletionNoticeRow>().CountAsync(ct));
        Assert.Equal(0, replies.Calls);

        var inbox = services.GetRequiredService<AiPilotCompletionNotices>();
        await Task.WhenAll(inbox.DrainAsync(ct), inbox.DrainAsync(ct));
        Assert.Equal(2, replies.Calls);
        var accepted = await db.Set<AiRequestRow>().SingleAsync(row => row.Id == pending, ct);
        Assert.Equal(failed ? (AiRequestState.Pending, RecoveryReplies.Matched, 0L) : (AiRequestState.Completed, RecoveryReplies.Matched, 1L),
            (accepted.State, accepted.VendorOperationId, accepted.ChargedUnits));
        if (failed)
        {
            Assert.Equal(AiPilotVendorStatus.Failed, accepted.PilotVendorStatus);
            Assert.Null(accepted.ProtectedText);
            Assert.Null(accepted.PilotMeteredCostMicros);
            Assert.Equal(1L, accepted.GiftedReserved);
        }
        var untouched = await db.Set<AiRequestRow>().SingleAsync(row => row.Id == other, ct);
        Assert.Equal(AiRequestState.Pending, untouched.State);
        Assert.Null(untouched.VendorOperationId);
        var movements = await db.Set<AiCreditRow>().ToArrayAsync(ct);
        if (failed) Assert.Empty(movements);
        else
        {
            var settlement = Assert.Single(movements);
            Assert.Equal(AiCreditEvent.Settle, settlement.Kind);
            Assert.Equal(pending, settlement.RequestId);
        }
        Assert.All(await db.Set<AiPilotCompletionNoticeRow>().ToArrayAsync(ct), row => { Assert.Equal(1, row.Attempts); Assert.True(row.Handled); });

        clock.Advance(TimeSpan.FromMinutes(10));
        await Send("fixture-delivery-new-signature", RecoveryReplies.Matched, clock.GetUtcNow(), HttpStatusCode.NoContent);
        await inbox.DrainAsync(ct);
        Assert.Equal(2, replies.Calls);

        void Seed(Guid requestId, AiPilotDispatchStage stage)
        {
            var offer = new AiOfferRow { Id = Guid.NewGuid(), UserId = owner, Purpose = payload.Purpose,
                Provider = original.Provider, Model = original.Model!, MaximumUnits = 1, PilotRouteId = original.Id,
                ExpiresAt = clock.GetUtcNow().AddHours(1) };
            db.AddRange(offer, new AiRequestRow { Id = requestId, UserId = owner, OfferId = offer.Id,
                State = AiRequestState.Pending, CreatedAt = clock.GetUtcNow(), GiftedReserved = 1,
                PayloadDigest = AiProtocol.Digest(payload), ProtectedPayload = "fictional-protected",
                PilotDispatchStage = stage, PilotDispatchClaimedAt = stage == AiPilotDispatchStage.Claimed ? clock.GetUtcNow() : null });
        }
        async Task Send(string delivery, string id, DateTimeOffset timestamp, HttpStatusCode status, bool alter = false)
        {
            var stamp = timestamp.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
            var resource = provider == "openai" ? "response" : "interaction";
            var body = System.Text.Json.JsonSerializer.Serialize(new { type = resource + (failed ? ".failed" : ".completed"), data = new { id } });
            var signature = Convert.ToBase64String(HMACSHA256.HashData(RecoveryReplies.SigningKey,
                Encoding.UTF8.GetBytes($"{delivery}.{stamp}.{body}")));
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/analysis/pilot/{provider}/completions")
            {
                Content = new StringContent(alter ? body + " " : body, Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("webhook-id", delivery);
            request.Headers.Add("webhook-timestamp", stamp);
            request.Headers.Add("webhook-signature", "v1," + signature);
            using var response = await callback.SendAsync(request, ct);
            Assert.Equal(status, response.StatusCode);
        }
    }

    private static async Task VerifyCapacityAsync(IServiceProvider services, HttpClient receiver, FakeTimeProvider clock, CancellationToken ct)
    {
        var factory = services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
        await using (var seed = await factory.CreateDbContextAsync(ct))
        {
            seed.AddRange(Enumerable.Range(0, 255).Select(index => new AiPilotCompletionNoticeRow
            {
                Provider = "openai", ResponseId = $"resp_capacity_{index}", DeliveryId = $"fixture-capacity-{index}",
                ReceivedAt = clock.GetUtcNow().AddDays(-1),
                Handled = index < 64, Attempts = index is >= 64 and < 128 ? AiPilotCompletionNotices.MaximumLookups : 0,
                CheckedAt = index is >= 64 and < 128 ? clock.GetUtcNow().AddDays(-1) : null,
            }));
            await seed.SaveChangesAsync(ct);
        }

        await Deliver("openai", 255, 31, HttpStatusCode.NoContent);
        await Deliver("openai", 256, 31, HttpStatusCode.ServiceUnavailable);
        await Deliver("openai", 0, 31, HttpStatusCode.NoContent);
        await Deliver("openai", 64, 31, HttpStatusCode.NoContent);
        await Deliver("gemini", 256, 32, HttpStatusCode.NoContent);

        await using var audit = await factory.CreateDbContextAsync(ct);
        var rows = await audit.Set<AiPilotCompletionNoticeRow>().ToArrayAsync(ct);
        Assert.Equal(128, rows.Count(row => row.Provider == "openai" && !row.Handled && row.Attempts < AiPilotCompletionNotices.MaximumLookups));
        Assert.Equal(64, rows.Count(row => row.Provider == "openai" && row.Handled));
        Assert.Equal(64, rows.Count(row => row.Provider == "openai" && row.Attempts == AiPilotCompletionNotices.MaximumLookups));
        Assert.Single(rows, row => row.Provider == "gemini");
        Assert.Empty(await audit.Set<AiRequestRow>().ToArrayAsync(ct));
        Assert.Empty(await audit.Set<AiCreditRow>().ToArrayAsync(ct));

        async Task Deliver(string provider, int index, byte signer, HttpStatusCode expected)
        {
            var stamp = clock.GetUtcNow().ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
            var delivery = $"fixture-capacity-{index}";
            var body = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = provider == "openai" ? "response.completed" : "interaction.completed",
                data = new { id = $"resp_capacity_{index}" },
            });
            var signature = Convert.ToBase64String(HMACSHA256.HashData(SigningBytes(signer),
                Encoding.UTF8.GetBytes($"{delivery}.{stamp}.{body}")));
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/analysis/pilot/{provider}/completions")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("webhook-id", delivery);
            request.Headers.Add("webhook-timestamp", stamp);
            request.Headers.Add("webhook-signature", "v1," + signature);
            using var response = await receiver.SendAsync(request, ct);
            Assert.Equal(expected, response.StatusCode);
        }
    }

    private static async Task VerifyRotationAsync(IServiceProvider services, IConfiguration configuration, HttpClient receiver,
        FakeTimeProvider clock, CancellationToken ct)
    {
        const string openai = "openai";
        const string gemini = "gemini";
        var start = clock.GetUtcNow().AddMinutes(1);
        var end = start.AddMinutes(2);
        Overlap(openai, 21, start, end);
        Overlap(gemini, 22, start, end);
        await Deliver(openai, "early", HttpStatusCode.BadRequest, 21);
        await Deliver(openai, "without-overlap", HttpStatusCode.NoContent, 31);
        clock.Advance(TimeSpan.FromMinutes(1));
        await Deliver(openai, "rotated", HttpStatusCode.NoContent, 21);
        await Deliver(openai, "rotated", HttpStatusCode.NoContent, 21, 31);
        await Deliver(gemini, "wrong-provider", HttpStatusCode.BadRequest, 21);
        await Deliver(gemini, "rotated", HttpStatusCode.NoContent, 22, 32);
        Overlap(gemini, 22, start, start.AddHours(25));
        await Deliver(gemini, "excessive", HttpStatusCode.BadRequest, 22);
        Overlap(gemini, 22, start, end);
        configuration["Analysis:ProviderPilot:gemini:PreviousWebhookSecretUntilUtc"] = null;
        await Deliver(gemini, "no-deadline", HttpStatusCode.BadRequest, 22);
        Overlap(gemini, 22, start, end);
        configuration[AiPilotCompletionNotices.SecretSetting(gemini)] = "invalid-fixture-secret";
        await Deliver(gemini, "invalid-current", HttpStatusCode.BadRequest, 22);
        configuration[AiPilotCompletionNotices.SecretSetting(gemini)] = Secret(32);
        configuration["Analysis:ProviderPilot:gemini:PreviousWebhookSecret"] = null;
        await Deliver(gemini, "revoked", HttpStatusCode.BadRequest, 22);
        await Deliver(gemini, "revoked", HttpStatusCode.NoContent, 32);
        clock.Advance(TimeSpan.FromMinutes(2));
        await Deliver(openai, "expired", HttpStatusCode.BadRequest, 21);
        await Deliver(openai, "expired", HttpStatusCode.NoContent, 31);

        await using var db = await services.GetRequiredService<IDbContextFactory<ServiceDbContext>>().CreateDbContextAsync(ct);
        var inbox = await db.Set<AiPilotCompletionNoticeRow>().ToArrayAsync(ct);
        Assert.Equal(3, inbox.Count(row => row.Provider == openai));
        Assert.Equal(2, inbox.Count(row => row.Provider == gemini));
        Assert.Equal(5, inbox.Length);
        Assert.All(inbox, row => { Assert.Equal(0, row.Attempts); Assert.False(row.Handled); });
        Assert.Empty(await db.Set<AiRequestRow>().ToArrayAsync(ct));
        Assert.Empty(await db.Set<AiCreditRow>().ToArrayAsync(ct));

        void Overlap(string provider, byte old, DateTimeOffset from, DateTimeOffset until)
        {
            var section = $"Analysis:ProviderPilot:{provider}";
            configuration[$"{section}:PreviousWebhookSecret"] = Secret(old);
            configuration[$"{section}:PreviousWebhookSecretFromUtc"] = Format(from);
            configuration[$"{section}:PreviousWebhookSecretUntilUtc"] = Format(until);
        }
        async Task Deliver(string provider, string caseId, HttpStatusCode expected, params byte[] signers)
        {
            var stamp = clock.GetUtcNow().ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
            var delivery = "fixture-delivery-" + caseId;
            var body = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = provider == openai ? "response.completed" : "interaction.completed",
                data = new { id = "resp_fixture_" + caseId },
            });
            var signed = Encoding.UTF8.GetBytes($"{delivery}.{stamp}.{body}");
            var signatures = string.Join(' ', signers.Select(value => "v1," +
                Convert.ToBase64String(HMACSHA256.HashData(SigningBytes(value), signed))));
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/analysis/pilot/{provider}/completions")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("webhook-id", delivery);
            request.Headers.Add("webhook-timestamp", stamp);
            request.Headers.Add("webhook-signature", signatures);
            using var response = await receiver.SendAsync(request, ct);
            Assert.Equal(expected, response.StatusCode);
        }

        static string Format(DateTimeOffset time) =>
            time.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static byte[] SigningBytes(byte value) => Enumerable.Repeat(value, 32).ToArray();

    private static string Secret(byte value) => "whsec_" + Convert.ToBase64String(SigningBytes(value));

    private static async Task VerifyOperationsAsync(IServiceProvider services, IConfiguration settings, HttpClient client, CancellationToken ct)
    {
        var factory = services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
        await using var db = await factory.CreateDbContextAsync(ct);
        const string owner = "fictional-ai-member";
        var now = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        var route = new AiPilotRouteRow { Provider = "openai", Purpose = AiPurpose.PeriodExplanation, Model = "gpt-6-luna",
            Revision = "fixture-operations-revision", Actor = OperatorId, SelectedAt = now };
        db.Add(route);
        await db.SaveChangesAsync(ct);
        Seed(AiRequestState.Pending, route.Id, 461, null, AiPilotDispatchStage.Queued, null);
        Seed(AiRequestState.Pending, route.Id, null, null, null, null);
        Seed(AiRequestState.Pending, route.Id, 461, 35, AiPilotDispatchStage.Claimed, "resp_fixture_private");
        Seed(AiRequestState.Completed, route.Id, 461, 0, AiPilotDispatchStage.Claimed, "resp_fixture_completed");
        Seed(AiRequestState.NotAccepted, route.Id, null, null, null, null);
        Seed(AiRequestState.Pending, null, null, null, null, null);
        db.AddRange(new AiPilotCompletionNoticeRow { Provider = "openai", ResponseId = "resp_fixture_inbox_open",
                DeliveryId = "fixture-delivery-open", ReceivedAt = now },
            new AiPilotCompletionNoticeRow { Provider = "openai", ResponseId = "resp_fixture_inbox_exhausted",
                DeliveryId = "fixture-delivery-exhausted", ReceivedAt = now, Attempts = 3 },
            new AiPilotCompletionNoticeRow { Provider = "openai", ResponseId = "resp_fixture_inbox_handled",
                DeliveryId = "fixture-delivery-handled", ReceivedAt = now, Attempts = 1, Handled = true });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        settings["Analysis:ProviderPilot:Budget:ProjectMicros"] = "123000";
        using var response = await client.GetAsync(Panel + "/operations", ct);
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.CacheControl?.NoStore);
        var html = await response.Content.ReadAsStringAsync(ct);
        var begin = html.IndexOf("<h2 id=\"openai-operations\">", StringComparison.Ordinal);
        var end = html.IndexOf("</section>", begin, StringComparison.Ordinal);
        var openAi = html.Substring(begin, end - begin);
        Assert.Contains("<dt>Accepted requests</dt><dd>4</dd>", openAi, StringComparison.Ordinal);
        Assert.Contains("<dt>Committed vendor ceiling (USD)</dt><dd>0.035175</dd>", openAi, StringComparison.Ordinal);
        Assert.Contains("<dt>Observed Standard estimate (USD)</dt><dd>0.000035</dd>", openAi, StringComparison.Ordinal);
        Assert.Contains("<dt>Requests with usage evidence</dt><dd>2</dd>", openAi, StringComparison.Ordinal);
        Assert.Contains("<dt>Waiting to send</dt><dd>1</dd>", openAi, StringComparison.Ordinal);
        Assert.Contains("<dt>Pending with a vendor reference</dt><dd>1</dd>", openAi, StringComparison.Ordinal);
        Assert.Contains("<dt>Pending without a vendor reference after dispatch may have started</dt><dd>1</dd>", openAi, StringComparison.Ordinal);
        Assert.Contains("<dt>Open completion notices</dt><dd>1</dd>", openAi, StringComparison.Ordinal);
        Assert.Contains("<dt>Notices that reached the lookup limit</dt><dd>1</dd>", openAi, StringComparison.Ordinal);
        Assert.Contains("<dt>Per provider including prior project revisions</dt><dd>0.123000</dd>", html, StringComparison.Ordinal);
        Assert.Contains("<dt>Observed Standard estimate (USD)</dt><dd>Unavailable</dd>", html[(end + 10)..], StringComparison.Ordinal);
        foreach (var prohibited in new[] { owner, "resp_fixture_", "fixture-sensitive-payload", "fixture-sensitive-output", "fixture-only" })
            Assert.DoesNotContain(prohibited, html, StringComparison.Ordinal);

        settings["Analysis:ProviderPilot:Budget:ProjectMicros"] = "invalid-budget";
        Assert.Contains("<dt>Per provider including prior project revisions</dt><dd>Invalid setting</dd>",
            await client.GetStringAsync(Panel + "/operations", ct), StringComparison.Ordinal);
        Assert.Equal(6, await db.Set<AiRequestRow>().CountAsync(ct));
        Assert.Equal(3, await db.Set<AiPilotCompletionNoticeRow>().CountAsync(ct));
        Assert.Empty(await db.Set<AiCreditRow>().ToArrayAsync(ct));

        void Seed(AiRequestState state, long? routeId, long? ceiling, long? estimate, AiPilotDispatchStage? stage, string? handle)
        {
            var completed = state == AiRequestState.Completed;
            var refused = state == AiRequestState.NotAccepted;
            var offer = new AiOfferRow { Id = Guid.NewGuid(), UserId = owner, Purpose = route.Purpose, Provider = route.Provider,
                Model = route.Model!, MaximumUnits = 1, PilotRouteId = routeId, ExpiresAt = now.AddHours(1) };
            db.AddRange(offer, new AiRequestRow { Id = Guid.NewGuid(), UserId = owner, OfferId = offer.Id, State = state,
                CreatedAt = now, GiftedReserved = refused ? 0 : 1, ChargedUnits = completed ? 1 : 0, PayloadDigest = new('c', 64),
                ProtectedPayload = "fixture-sensitive-payload", ProtectedText = completed ? "fixture-sensitive-output" : null,
                PilotCostCeilingMicros = ceiling, PilotMeteredCostMicros = estimate, VendorOperationId = handle, PilotDispatchStage = stage });
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
