using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Api;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Real sender serialization and refusal boundaries with fictional credentials and an in-memory HTTP peer only.</summary>
public sealed class IdentityMailTests
{
    /// <summary>One original envelope retains its request identity through an unknown return and exact duplicate acknowledgement.</summary>
    [Fact]
    public async Task Unknown_return_retries_the_identical_original_batch_then_accepts_only_the_exact_duplicate_code()
    {
        using var key = new FictionalKey(); var packets = new List<string>();
        using var sender = new BrevoIdentityMailSender("zanance@example.invalid", key.Path, new Peer(async (request, ct) =>
        {
            Assert.Equal("https://api.brevo.com/v3/smtp/email", request.RequestUri!.AbsoluteUri);
            Assert.Equal(HttpMethod.Post, request.Method); Assert.Single(request.Headers.GetValues("api-key"));
            var packet = await request.Content!.ReadAsStringAsync(ct); packets.Add(packet);
            if (packets.Count == 1) throw new HttpRequestException("Fictional unknown return.");
            return Response(400, "{\"code\":\"duplicate_parameter\"}");
        }));
        var original = new IdentityMail(Guid.NewGuid(), "reset", "recipient@example.invalid", "fictional-code<&>");
        await Assert.ThrowsAsync<IdentityMailUnavailable>(() => sender.SubmitAsync(original, TestContext.Current.CancellationToken));
        await sender.SubmitAsync(original, TestContext.Current.CancellationToken);
        Assert.Equal(2, packets.Count); Assert.Equal(packets[0], packets[1]);
        using var json = JsonDocument.Parse(packets[0]); var root = json.RootElement;
        Assert.Equal(original.Id.ToString("D"), root.GetProperty("headers").GetProperty("idempotencyKey").GetString());
        Assert.Single(root.GetProperty("messageVersions").EnumerateArray());
        Assert.Single(root.GetProperty("messageVersions")[0].GetProperty("to").EnumerateArray());
        Assert.Equal(original.Recipient, root.GetProperty("messageVersions")[0].GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Contains("Recover", root.GetProperty("subject").GetString());
        Assert.Contains("&lt;&amp;&gt;", root.GetProperty("htmlContent").GetString());
        Assert.DoesNotContain("attachment", packets[0]); Assert.DoesNotContain("bcc", packets[0]);
    }

    /// <summary>Provider acceptance requires its actual 201 receipt; a nonempty HTTP body alone is not delivery evidence.</summary>
    [Theory]
    [InlineData(201, "{\"messageIds\":[\"fictional-id\"]}", true)]
    [InlineData(201, "{\"messageId\":\"fictional-id\"}", true)]
    [InlineData(201, "{}", false)]
    [InlineData(201, "{\"messageIds\":[]}", false)]
    [InlineData(201, "{\"messageIds\":[\"\",\"other\"]}", false)]
    [InlineData(200, "{\"messageId\":\"fictional-id\"}", false)]
    [InlineData(302, "{}", false)]
    [InlineData(400, "{\"code\":\"invalid_parameter\"}", false)]
    [InlineData(401, "{\"code\":\"duplicate_parameter\"}", false)]
    [InlineData(429, "{}", false)]
    [InlineData(503, "untrusted provider body", false)]
    [InlineData(201, "[]", false)]
    public async Task Actual_response_status_and_receipt_control_acceptance(int status, string body, bool accepted)
    {
        using var key = new FictionalKey();
        using var sender = new BrevoIdentityMailSender("sender@example.invalid", key.Path,
            new Peer((_, _) => Task.FromResult(Response(status, body))));
        var operation = sender.SubmitAsync(new(Guid.NewGuid(), "confirm", "recipient@example.invalid", "fictional-code"), TestContext.Current.CancellationToken);
        if (accepted) await operation;
        else Assert.Equal("Identity email submission is unavailable.", (await Assert.ThrowsAsync<IdentityMailUnavailable>(() => operation)).Message);
    }

    /// <summary>Oversized provider responses cannot become unbounded logs or acknowledgement payloads.</summary>
    [Fact]
    public async Task Oversized_response_is_refused_without_exposing_content()
    {
        using var key = new FictionalKey();
        using var sender = new BrevoIdentityMailSender("sender@example.invalid", key.Path,
            new Peer((_, _) => Task.FromResult(Response(201, new string('x', 8193)))));
        var error = await Assert.ThrowsAsync<IdentityMailUnavailable>(() => sender.SubmitAsync(
            new(Guid.NewGuid(), "confirm", "recipient@example.invalid", "fictional-code"), TestContext.Current.CancellationToken));
        Assert.Null(error.InnerException);
    }

    /// <summary>Malformed original purposes fail before any credential or HTTP effect.</summary>
    [Fact]
    public async Task Foreign_purpose_is_refused_before_secret_read_or_http()
    {
        var calls = 0;
        using var sender = new BrevoIdentityMailSender("sender@example.invalid", "missing-file",
            new Peer((_, _) => { calls++; throw new InvalidOperationException(); }));
        await Assert.ThrowsAsync<IdentityMailUnavailable>(() => sender.SubmitAsync(
            new(Guid.NewGuid(), "marketing", "recipient@example.invalid", "fictional-code"), TestContext.Current.CancellationToken));
        Assert.Equal(0, calls);
    }

    /// <summary>Public identity cannot start with an absent sender; private qualification remains closed without one.</summary>
    [Theory]
    [InlineData(null, null, null, true)]
    [InlineData("smtp", "sender@example.invalid", "C:/private/key", false)]
    [InlineData("brevo", "Display <sender@example.invalid>", "C:/private/key", false)]
    [InlineData("brevo", "sender@example.invalid", "relative-key", false)]
    public void Incomplete_or_ambiguous_delivery_configuration_is_rejected(string? provider, string? sender, string? file, bool publicWorker)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["Service:Mail:Provider"] = provider, ["Service:Mail:SenderEmail"] = sender, ["Service:Mail:ApiKeyFile"] = file });
        Assert.Throws<InvalidOperationException>(() => IdentityMailModule.Add(builder, publicWorker));
    }

    /// <summary>Historical local payloads and another original sender never become externally deliverable.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("brevo-v1:other@example.invalid")]
    public void Historical_or_replaced_sender_envelope_is_refused(string? originalAuthority)
    {
        var protection = new EphemeralDataProtectionProvider();
        var row = new IdentityChallenge { Id = Guid.NewGuid(), Purpose = "confirm", ProtectedMail =
            protection.CreateProtector("identity-mail-v1").Protect(JsonSerializer.Serialize(
                new PrivateMail("recipient@example.invalid", "fictional-original", originalAuthority))) };
        Assert.Throws<IdentityMailUnavailable>(() => IdentityMailEnvelope.Read(row, protection, "brevo-v1:sender@example.invalid"));
    }

    /// <summary>Cancellation stops before even opening the private credential file.</summary>
    [Fact]
    public async Task Cancelled_original_does_not_read_a_key_or_submit_http()
    {
        using var cancelled = new CancellationTokenSource(); await cancelled.CancelAsync();
        using var sender = new BrevoIdentityMailSender("sender@example.invalid", "missing-file",
            new Peer((_, _) => throw new InvalidOperationException("HTTP must not run.")));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sender.SubmitAsync(
            new(Guid.NewGuid(), "confirm", "recipient@example.invalid", "fictional-code"), cancelled.Token));
    }

    /// <summary>The public worker needs a real mounted file, but construction never opens its secret contents or sends HTTP.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Public_sender_requires_the_dedicated_file_without_loading_its_contents_during_composition(bool exists)
    {
        using var key = new FictionalKey();
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Service:Mail:Provider"] = "brevo", ["Service:Mail:SenderEmail"] = "sender@example.invalid",
            ["Service:Mail:ApiKeyFile"] = exists ? key.Path : key.Path + ".missing"
        });
        if (!exists) Assert.Throws<InvalidOperationException>(() => IdentityMailModule.Add(builder, publicWorker: true));
        else
        {
            IdentityMailModule.Add(builder, publicWorker: true);
            using var services = builder.Services.BuildServiceProvider();
            Assert.Equal("brevo-v1:sender@example.invalid", services.GetRequiredService<IIdentityMailSender>().Authority);
        }
    }

    private static HttpResponseMessage Response(int status, string body) => new((HttpStatusCode)status)
    { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class Peer(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> run) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => run(request, ct);
    }

    private sealed class FictionalKey : IDisposable
    {
        internal string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "zanance-fictional-mail-" + Guid.NewGuid().ToString("N"));
        internal FictionalKey() => File.WriteAllText(Path, "fictional-not-a-real-provider-key");
        public void Dispose() => File.Delete(Path);
    }
}
