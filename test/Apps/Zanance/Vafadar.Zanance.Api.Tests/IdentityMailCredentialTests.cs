using System.Net;
using System.Text;
using Vafadar.Zanance.Api;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Real runtime file/stream boundaries with fictional key text and no provider network.</summary>
public sealed class IdentityMailCredentialTests
{
    /// <summary>Ordinary UTF-8 editor files work and a same-account key rotation cannot reuse another request's cached header.</summary>
    [Fact]
    public async Task Runtime_utf8_key_file_accepts_bom_and_reloads_only_for_the_next_original_request()
    {
        var path = Path.Combine(Path.GetTempPath(), "zanance-fictional-bom-" + Guid.NewGuid().ToString("N"));
        var ct = TestContext.Current.CancellationToken;
        await File.WriteAllBytesAsync(path, [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes("fictional-first-key\r\n")], ct);
        try
        {
            var headers = new List<string>();
            using var sender = new BrevoIdentityMailSender("sender@example.invalid", path, new Peer((request, _) =>
            {
                headers.Add(Assert.Single(request.Headers.GetValues("api-key")));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                { Content = new StringContent("{\"messageIds\":[\"fictional-receipt\"]}") });
            }));
            await sender.SubmitAsync(new(Guid.NewGuid(), "confirm", "recipient@example.invalid", "fictional-code", 2, "it"), ct);
            await File.WriteAllTextAsync(path, "fictional-second-key", ct);
            await sender.SubmitAsync(new(Guid.NewGuid(), "reset", "recipient@example.invalid", "fictional-next-code", 2, "fr"), ct);
            Assert.Equal(new[] { "fictional-first-key", "fictional-second-key" }, headers);
        }
        finally { File.Delete(path); }
    }

    /// <summary>The total deadline covers a stalled response body after successful headers, not only the send-to-headers phase.</summary>
    [Fact]
    public async Task Stalled_provider_response_body_expires_without_acknowledgement_or_sensitive_exception()
    {
        var path = Path.Combine(Path.GetTempPath(), "zanance-fictional-timeout-" + Guid.NewGuid().ToString("N"));
        var ct = TestContext.Current.CancellationToken;
        await File.WriteAllTextAsync(path, "fictional-not-a-real-key", ct);
        try
        {
            using var sender = new BrevoIdentityMailSender("sender@example.invalid", path, new Peer((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created) { Content = new StreamContent(new Stalled()) })));
            var error = await Assert.ThrowsAsync<IdentityMailUnavailable>(() => sender.SubmitAsync(
                new(Guid.NewGuid(), "confirm", "recipient@example.invalid", "fictional-code", 2, "en"), ct).WaitAsync(TimeSpan.FromSeconds(30), ct));
            Assert.Null(error.InnerException); Assert.DoesNotContain("fictional", error.Message);
        }
        finally { File.Delete(path); }
    }

    private sealed class Peer(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct);
    }
    private sealed class Stalled : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); return 0; }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
