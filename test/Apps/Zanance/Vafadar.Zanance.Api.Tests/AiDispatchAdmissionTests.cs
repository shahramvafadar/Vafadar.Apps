using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Checks the original private journal and pinned transport against a local synthetic service.</summary>
public sealed class AiDispatchAdmissionTests
{
    private const string Owner = "fictitious-analysis-owner";

    /// <summary>An absent status permits replay only after the original source check was durably accepted.</summary>
    [Theory]
    [InlineData("rejected")]
    [InlineData("interrupted")]
    [InlineData("legacy")]
    [InlineData("legacy-accepted")]
    public async Task Analysis_continuation_respects_durable_source_admission(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        using var rsa = RSA.Create(2048);
        var certificateRequest = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddIpAddress(IPAddress.Loopback);
        certificateRequest.CertificateExtensions.Add(names.Build());
        certificateRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, false));
        using var generated = certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(15));
        // Match the local service's Schannel-compatible import without installing global trust.
        var flags = OperatingSystem.IsWindows() ? X509KeyStorageFlags.UserKeySet : X509KeyStorageFlags.EphemeralKeySet;
        var pfx = generated.Export(X509ContentType.Pfx);
        using var certificate = X509CertificateLoader.LoadPkcs12(pfx, null, flags);
        CryptographicOperations.ZeroMemory(pfx);
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0, endpoint => endpoint.UseHttps(certificate)));
        await using var server = builder.Build();
        int posts = 0, statusReads = 0, closes = 0;
        AiSubmission? delivered = null, accepted = null;
        server.MapPost("/analysis/requests/{id:guid}/status", (Guid id) =>
        {
            Interlocked.Increment(ref statusReads);
            if (accepted is { } existing && existing.RequestId == id)
                return Results.Json(new AiRequestResult(id, AiProtocol.Digest(existing.Payload), AiRequestState.Completed, 1,
                    "Synthetic result.", new(0, 0, 0, 0)), AiJson.Default.AiRequestResult);
            return Results.NotFound();
        });
        server.MapPost("/analysis/requests/{id:guid}/close", (Guid id, AiCloseRequest request) =>
        {
            Interlocked.Increment(ref closes);
            return Results.Json(new AiRequestResult(id, request.PayloadDigest, AiRequestState.NotAccepted, 0, null, new(0, 0, 0, 0)),
                AiJson.Default.AiRequestResult);
        });
        server.MapPost("/analysis/requests", async (HttpContext context) =>
        {
            var submission = await JsonSerializer.DeserializeAsync(context.Request.Body, AiJson.Default.AiSubmission, context.RequestAborted);
            if (Interlocked.Increment(ref posts) == 1 && scenario == "interrupted")
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            delivered = submission!;
            return Results.Json(new AiRequestResult(delivered.RequestId, AiProtocol.Digest(delivered.Payload),
                AiRequestState.NotAccepted, 0, null, new(0, 0, 0, 0)), AiJson.Default.AiRequestResult);
        });
        await server.StartAsync(ct);
        using var transport = new PersonalSyncTransport(new LocalSyncPeer(server.Urls.Single(),
            Convert.ToHexStringLower(SHA256.HashData(certificate.RawData))), new SyntheticSession());
        var journal = new MemoryJournal();
        var scope = Path.GetFullPath("fictitious-analysis-" + Guid.NewGuid().ToString("N"));
        var client = new AiClient(transport, journal, Owner, scope, () => { });
        var offer = new AiOffer(Guid.NewGuid(), "fixture", "fixture-model", AiPurpose.PeriodExplanation, 8, DateTimeOffset.UtcNow.AddMinutes(5));
        var payload = new AiPayload(1, offer.Purpose, "en",
            new(new(2026, 10, 1), new(2026, 10, 2), 1, true, [new("EUR", 0, 0, 0, 0)], 0), null);

        if (scenario is "legacy" or "legacy-accepted")
        {
            var originalSubmission = new AiSubmission(Guid.NewGuid(), offer, payload);
            if (scenario == "legacy-accepted") accepted = originalSubmission;
            journal.Value = JsonSerializer.Serialize(new AiClientRecord(1, originalSubmission, null), AiClientJson.Default.AiClientRecord);
        }
        else if (scenario == "rejected")
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.SubmitAsync(offer, payload, () => throw new InvalidOperationException("Original source changed."), ct));
        else
            await Assert.ThrowsAsync<PersonalSyncHttpException>(() => client.SubmitAsync(offer, payload, () => Task.CompletedTask, ct));
        var original = Assert.IsType<AiClientRecord>(await client.ReadAsync(ct));
        var resumed = new AiClient(transport, journal, Owner, scope, () => { });

        if (scenario == "interrupted")
        {
            Assert.True(original.DispatchPermitted);
            var statusOnly = original with { DispatchPermitted = null };
            await Assert.ThrowsAsync<AiSourceReviewRequiredException>(() => resumed.ContinueAsync(statusOnly, ct));
            Assert.Equal(1, posts);
            Assert.Equal(original.Submission.RequestId, (await resumed.ContinueAsync(original, ct)).RequestId);
            Assert.NotNull(delivered);
            Assert.Equal(original.Submission.RequestId, delivered.RequestId);
            Assert.Equal(AiProtocol.Digest(original.Submission.Payload), AiProtocol.Digest(delivered.Payload));
            Assert.Equal(original.Submission.Offer, delivered.Offer);
            Assert.Equal(2, posts);
        }
        else if (scenario == "legacy-accepted")
        {
            Assert.Null(original.DispatchPermitted);
            var result = await resumed.ContinueAsync(ct);
            Assert.Equal(original.Submission.RequestId, result.RequestId);
            Assert.Equal(AiRequestState.Completed, result.State);
            Assert.Equal(0, posts);
        }
        else
        {
            Assert.Equal(scenario == "legacy" ? (bool?)null : false, original.DispatchPermitted);
            await Assert.ThrowsAsync<AiSourceReviewRequiredException>(() => resumed.ContinueAsync(ct));
            Assert.Equal(0, posts);
            Assert.Null((await resumed.ReadAsync(ct))!.Result);
            Assert.Equal(AiRequestState.NotAccepted, (await resumed.CloseAsync(ct)).State);
            Assert.Equal(1, closes);
        }
        Assert.Equal(scenario == "interrupted" ? 2 : 1, statusReads);
    }

    /// <summary>A replaced journal entry is refused before a status, closure or aggregate request.</summary>
    [Theory]
    [InlineData(false, "id")]
    [InlineData(true, "id")]
    [InlineData(false, "offer")]
    [InlineData(false, "payload")]
    public async Task Original_analysis_confirmation_cannot_act_on_a_replaced_record(bool close, string changed)
    {
        var ct = TestContext.Current.CancellationToken;
        var journal = new MemoryJournal();
        using var transport = new PersonalSyncTransport(new LocalSyncPeer("https://localhost:55555", new string('f', 64)), new NoSessions());
        var client = new AiClient(transport, journal, Owner, Path.GetFullPath(Guid.NewGuid().ToString("N")), () => { });
        var offer = new AiOffer(Guid.NewGuid(), "fixture", "fixture-model", AiPurpose.PeriodExplanation, 8, DateTimeOffset.UtcNow.AddMinutes(5));
        var payload = new AiPayload(1, offer.Purpose, "en",
            new(new(2026, 10, 1), new(2026, 10, 2), 1, true, [new("EUR", 0, 0, 0, 0)], 0), null);
        var expected = new AiClientRecord(1, new(Guid.NewGuid(), offer, payload), null) { DispatchPermitted = true };
        var modified = changed switch
        {
            "id" => expected.Submission with { RequestId = Guid.NewGuid() },
            "offer" => expected.Submission with { Offer = offer with { Model = "other-fixture-model" } },
            "payload" => expected.Submission with { Payload = payload with { Language = "fa" } },
            _ => throw new ArgumentOutOfRangeException(nameof(changed)),
        };
        journal.Value = JsonSerializer.Serialize(expected with { Submission = modified }, AiClientJson.Default.AiClientRecord);

        var originalJournal = journal.Value;
        await Assert.ThrowsAsync<InvalidDataException>(() => close ? client.CloseAsync(expected, ct) : client.ContinueAsync(expected, ct));
        Assert.Equal(originalJournal, journal.Value);
    }

    private sealed class MemoryJournal : IAiPrivateStorage
    {
        internal string? Value;
        public Task<string?> ReadAsync(string scope, CancellationToken ct) => Task.FromResult(Value);
        public Task WriteAsync(string scope, string value, CancellationToken ct) { Value = value; return Task.CompletedTask; }
    }

    private sealed class SyntheticSession : IPersonalSyncTokenStore
    {
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct) => Task.FromResult<PersonalSyncSession?>(
            new() { UserId = Owner, AccessToken = "synthetic-fixture", RefreshToken = "synthetic-fixture", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5) });
        public Task WriteAsync(string peer, PersonalSyncSession value, CancellationToken ct) => throw new InvalidOperationException();
        public Task RemoveAsync(string peer, CancellationToken ct) => throw new InvalidOperationException();
    }

    private sealed class NoSessions : IPersonalSyncTokenStore
    {
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct) => throw new InvalidOperationException("Unexpected transport access.");
        public Task WriteAsync(string peer, PersonalSyncSession value, CancellationToken ct) => throw new InvalidOperationException();
        public Task RemoveAsync(string peer, CancellationToken ct) => throw new InvalidOperationException();
    }
}
