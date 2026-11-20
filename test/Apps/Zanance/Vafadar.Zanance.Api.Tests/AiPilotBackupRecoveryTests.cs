using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
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

/// <summary>Opens only the retained fictional snapshot with the provider and analysis routes disabled.</summary>
[Trait("Qualification", "AiLocal")]
public sealed class AiPilotBackupRecoveryTests
{
    /// <summary>Verifies that restored data and protected results remain readable without replay or settlement.</summary>
    [Fact]
    public async Task Restored_synthetic_analysis_snapshot_keeps_results_and_holds_without_provider_egress()
    {
        var ct = TestContext.Current.CancellationToken;
        var connection = Environment.GetEnvironmentVariable("ZANANCE_AI_RESTORE_CONNECTION");
        if (connection is null) { Assert.Skip("Explicit isolated restored-archive fixture required."); return; }
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal(("127.0.0.1", 55488, "zanance_ai_restore_test"), (settings.Host, settings.Port, settings.Database));
        var source = Environment.GetEnvironmentVariable("ZANANCE_AI_RESTORE_FIXTURE")!;
        Assert.Equal("Zanance fixed synthetic multilingual live cohort",
            await File.ReadAllTextAsync(Path.Combine(source, "fixture.marker"), ct));
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(source, "cases.json"), ct));
        var pairs = manifest.RootElement.EnumerateArray().Select(item => (
            Id: item.GetProperty("RequestId").GetGuid(), Locale: item.GetProperty("Locale").GetString()!,
            Model: item.GetProperty("Model").GetString()!)).ToDictionary(item => item.Id);
        Assert.Equal(24, pairs.Count);

        var root = Environment.GetEnvironmentVariable("ZANANCE_AI_RESTORE_ROOT")!;
        Assert.Equal("Zanance isolated synthetic archive restore", await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), ct));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Integration",
            ApplicationName = typeof(ServiceHost).Assembly.FullName });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Service"] = connection,
            ["Service:Root"] = Path.Combine(root, "service"),
            ["Analysis:LocalQualification"] = "false",
            ["Analysis:ProviderPilot:Enabled"] = "false",
        });
        builder.AddZananceService();
        builder.WebHost.ConfigureKestrel(options => ServiceHost.ListenLocalTls(options, 0));
        builder.Logging.ClearProviders();
        builder.Services.RemoveAll<IHostedService>();
        var noEgress = new NoEgress();
        builder.Services.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(noEgress));
        await using var app = builder.Build();
        var factory = app.Services.GetRequiredService<IDbContextFactory<ServiceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(ct))
            Assert.NotEmpty(await db.Database.GetPendingMigrationsAsync(ct));
        await ServiceHost.MigrateAsync(app.Services);

        var protection = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root, "fixture-key-copy")),
            options => options.SetApplicationName("Zanance isolated locale evaluation"));
        app.UseZananceService();
        app.MapZananceService();
        await app.StartAsync(ct);
        Assert.Null(app.Services.GetService<AiPilotProvider>());
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var fingerprint = app.Services.GetRequiredService<LocalSyncCertificate>().Fingerprint;
        using var probe = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = (_, certificate, _, _) => certificate is not null &&
                Convert.ToHexStringLower(SHA256.HashData(certificate.RawData)) == fingerprint,
        }) { BaseAddress = new Uri(address) };
        using var healthy = await probe.GetAsync("/health/ready", ct);
        Assert.Equal(HttpStatusCode.OK, healthy.StatusCode);
        using var analysis = await probe.GetAsync("/analysis/availability", ct);
        Assert.Equal(HttpStatusCode.NotFound, analysis.StatusCode);
        using var admin = await probe.GetAsync("/admin/analysis", ct);
        Assert.Equal(HttpStatusCode.NotFound, admin.StatusCode);

        await using var audit = await factory.CreateDbContextAsync(ct);
        var requests = await audit.Set<AiRequestRow>().AsNoTracking().ToArrayAsync(ct);
        Assert.Equal(pairs.Keys.Order(), requests.Select(row => row.Id).Order());
        var offers = await audit.Set<AiOfferRow>().AsNoTracking().ToDictionaryAsync(row => row.Id, ct);
        foreach (var row in requests)
        {
            var expected = pairs[row.Id];
            Assert.Equal("fictional-live-locales-" + expected.Locale, row.UserId);
            Assert.Equal(expected.Model, offers[row.OfferId].Model);
            Assert.NotNull(row.VendorOperationId);
            var protector = protection.CreateProtector("zanance.analysis.v1", row.UserId, row.Id.ToString("N"));
            var payload = JsonSerializer.Deserialize(protector.Unprotect(row.ProtectedPayload), AiJson.Default.AiPayload)!;
            Assert.Equal(row.PayloadDigest, AiProtocol.Digest(payload));
            Assert.Equal(expected.Locale, payload.Language);
            if (row.State == AiRequestState.Pending)
            {
                Assert.Equal("gemini-4-hydrogen", expected.Model);
                Assert.Contains(expected.Locale, new[] { "fa", "it" });
                Assert.Null(row.ProtectedText);
                Assert.Null(row.PilotMeteredCostMicros);
                Assert.Equal(1L, row.GiftedReserved);
                continue;
            }
            Assert.Equal(AiRequestState.Completed, row.State);
            var text = protector.Unprotect(Assert.IsType<string>(row.ProtectedText));
            using var receipt = JsonDocument.Parse(await File.ReadAllTextAsync(
                Path.Combine(source, expected.Locale + "-" + expected.Model + "-terminal.json"), ct));
            Assert.True(text == receipt.RootElement.GetProperty("Text").GetString());
            Assert.Equal(receipt.RootElement.GetProperty("PilotMeteredCostMicros").GetInt64(), row.PilotMeteredCostMicros);
        }
        Assert.Equal(22, requests.Count(row => row.State == AiRequestState.Completed));
        Assert.Equal(2, requests.Count(row => row.State == AiRequestState.Pending));
        var ledger = audit.Set<AiCreditRow>();
        Assert.Equal(24, await ledger.CountAsync(row => row.Kind == AiCreditEvent.Reserve, ct));
        Assert.Equal(22, await ledger.CountAsync(row => row.Kind == AiCreditEvent.Settle, ct));
        Assert.Empty(await ledger.Where(row => row.Kind == AiCreditEvent.Refund).ToArrayAsync(ct));
        Assert.Equal(0, noEgress.Attempts);
    }

    private sealed class NoEgress : IHttpClientFactory
    {
        internal int Attempts { get; private set; }

        public HttpClient CreateClient(string name)
        {
            Attempts++;
            throw new InvalidOperationException("The isolated restore must not open an outbound client.");
        }
    }
}
