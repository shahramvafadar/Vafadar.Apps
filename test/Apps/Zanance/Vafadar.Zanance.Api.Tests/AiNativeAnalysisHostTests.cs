using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
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

/// <summary>Hosts the isolated pinned service for the original Android analysis transport review.</summary>
[Trait("Qualification", "AiLocal")]
public sealed class AiNativeAnalysisHostTests
{
    /// <summary>Accepts exactly two fictional requests across the original server model switch.</summary>
    [Fact]
    public async Task Isolated_analysis_host_settles_two_original_native_requests()
    {
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_NATIVE_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated native review required."); return; }
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        limit.CancelAfter(TimeSpan.FromMinutes(6));
        var ct = limit.Token;
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", 55482, "zanance_ai_provider_pilot"), (settings.Host, settings.Port, settings.Database));
        var root = Environment.GetEnvironmentVariable("ZANANCE_AI_NATIVE_ROOT")!;
        Assert.Equal("Zanance isolated native analysis", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        var run = await File.ReadAllTextAsync(Path.Combine(root, "run.txt"), ct);
        Assert.True(run.Length == 32 && run.All(char.IsAsciiHexDigitLower));
        using (File.Open(Path.Combine(root, "native-host-started"), FileMode.CreateNew)) { }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration", ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Service"] = connection,
            ["Service:Root"] = Path.Combine(root, "service"),
            ["Analysis:LocalQualification"] = "true",
            ["Analysis:ProviderPilot:Enabled"] = "true",
            ["Analysis:ProviderPilot:openai:ApiKey"] = "fictional-native-transport",
            ["Analysis:ProviderPilot:openai:Project"] = "fictional-native-openai",
            ["Analysis:ProviderPilot:gemini:ApiKey"] = "fictional-native-transport",
            ["Analysis:ProviderPilot:gemini:Project"] = "fictional-native-gemini",
        });
        using var replies = new NativeReplies();
        builder.AddZananceService();
        builder.Logging.ClearProviders();
        builder.Services.RemoveAll<IHostedService>();
        builder.Services.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(replies));

        await using var app = builder.Build();
        await ServiceHost.MigrateAsync(app.Services);
        app.UseZananceService();
        app.MapZananceService();
        var factory = app.Services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
        var pilot = app.Services.GetRequiredService<AiPilotProvider>();
        var store = app.Services.GetRequiredService<AiRequestStore>();
        string owner;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ServiceUser>>();
            var email = "android-analysis-" + run + "@example.invalid";
            var user = new ServiceUser { Email = email, UserName = email, EmailConfirmed = true };
            Assert.True((await users.CreateAsync(user, "Fictitious!" + run + "aA9")).Succeeded);
            owner = user.Id;
        }
        await store.GrantAsync(owner, "native-analysis-" + run, false, 2, ct);
        await SelectAsync("gpt-6-luna");
        await app.StartAsync(ct);
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var uri = new Uri(address);
        Assert.True(uri.IsLoopback);
        await File.WriteAllLinesAsync(Path.Combine(root, "ready.txt"),
            [uri.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                app.Services.GetRequiredService<LocalSyncCertificate>().Fingerprint], ct);

        var first = await ObserveAsync(1);
        Assert.Equal("resp_native_analysis", Assert.Single(first).VendorOperationId);
        await SelectAsync("gemini-3.5-flash-lite");
        var completed = await ObserveAsync(2);
        Assert.Contains(completed, row => row.VendorOperationId == "interaction_native_analysis");
        Assert.All(completed, row => Assert.True(row.ChargedUnits == 1 && row.GiftedReserved == 1));
        await using var verify = await factory.CreateDbContextAsync(ct);
        Assert.Equal(2, await verify.Set<AiCreditRow>().CountAsync(row => row.UserId == owner && row.Kind == AiCreditEvent.Reserve, ct));
        Assert.Equal(2, await verify.Set<AiCreditRow>().CountAsync(row => row.UserId == owner && row.Kind == AiCreditEvent.Settle, ct));
        Assert.Equal(1, replies.OpenAiCreates);
        Assert.Equal(1, replies.GeminiCreates);
        await File.WriteAllTextAsync(Path.Combine(root, "host-complete.txt"), "Two original synthetic settlements", ct);

        async Task SelectAsync(string model)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            await pilot.SelectModelAsync(db, AiPurpose.PeriodExplanation, model, "fictional-native-operator", ct);
        }

        async Task<AiRequestRow[]> ObserveAsync(int expected)
        {
            while (true)
            {
                await using var db = await factory.CreateDbContextAsync(ct);
                var rows = await db.Set<AiRequestRow>().AsNoTracking().Where(row => row.UserId == owner).ToArrayAsync(ct);
                Assert.All(rows, row => Assert.NotEqual(AiRequestState.NotAccepted, row.State));
                if (rows.Count(row => row.State == AiRequestState.Completed) == expected) return rows;
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }
    }

    private sealed class NativeReplies : HttpMessageHandler, IHttpClientFactory
    {
        internal int OpenAiCreates;
        internal int GeminiCreates;

        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            var body = request.RequestUri!.AbsoluteUri switch
            {
                "https://api.openai.com/v1/responses" => OpenAi(),
                "https://generativelanguage.googleapis.com/v1beta/interactions" => Gemini(),
                _ => throw new InvalidOperationException("Unexpected isolated analysis destination."),
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }

        private string OpenAi()
        {
            Assert.Equal(1, Interlocked.Increment(ref OpenAiCreates));
            return """{"id":"resp_native_analysis","status":"completed","model":"gpt-6-luna","usage":{"input_tokens":30,"output_tokens":30},"output":[{"type":"message","content":[{"type":"output_text","text":"{\"net_expenses_minor\":0,\"explanation\":\"The selected period has no recorded activity.\"}"}]}]}""";
        }

        private string Gemini()
        {
            Assert.Equal(1, Interlocked.Increment(ref GeminiCreates));
            return """{"id":"interaction_native_analysis","status":"completed","model":"gemini-3.5-flash-lite","usage":{"total_input_tokens":30,"total_output_tokens":30},"steps":[{"type":"model_output","content":[{"type":"text","text":"{\"net_expenses_minor\":0,\"explanation\":\"The selected period has no recorded activity.\"}"}]}]}""";
        }
    }
}
