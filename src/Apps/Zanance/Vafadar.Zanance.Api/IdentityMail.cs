using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Vafadar.Zanance.Api;

/// <summary>Original one-use identity envelope, never financial content or a marketing contact.</summary>
internal sealed record IdentityMail(Guid Id, string Purpose, string Recipient, string Token, int Format = 1, string? Language = null)
{
    /// <inheritdoc />
    public override string ToString() => "Protected identity delivery";
}

/// <summary>Acceptance by the selected sender; it does not claim delivery to the recipient's inbox.</summary>
internal interface IIdentityMailSender
{
    /// <summary>Nonsecret original provider/sender identity captured inside each protected challenge.</summary>
    string Authority { get; }
    /// <summary>Submits one immutable challenge with its original idempotency identity.</summary>
    Task SubmitAsync(IdentityMail mail, CancellationToken ct);
}

/// <summary>Bounded failure without provider bodies, recipient details, tokens or credential values.</summary>
internal sealed class IdentityMailUnavailable() : IOException("Identity email submission is unavailable.");

/// <summary>Explicit Brevo integration. No account, credential or request exists merely by constructing the service.</summary>
internal static class IdentityMailModule
{
    /// <summary>Validates nonsecret configuration before a public worker can start; migrations never send mail.</summary>
    internal static void Add(WebApplicationBuilder builder, bool publicWorker)
    {
        var provider = builder.Configuration["Service:Mail:Provider"];
        if (string.IsNullOrEmpty(provider))
        {
            if (publicWorker) throw new InvalidOperationException("Public identity requires a configured real email sender.");
            return;
        }
        if (provider != "brevo" || builder.Configuration.GetValue<bool>("Service:LocalMailbox"))
            throw new InvalidOperationException("Select one explicit identity email delivery mode.");
        var email = builder.Configuration["Service:Mail:SenderEmail"];
        var keyFile = builder.Configuration["Service:Mail:ApiKeyFile"];
        if (email is null || !MailAddress.TryCreate(email, out var address) || address.Address != email ||
            email.Length > 254 || keyFile is null || !Path.IsPathFullyQualified(keyFile))
            throw new InvalidOperationException("An exact verified sender address and absolute private API-key file are required.");
        if (publicWorker)
        {
            var secret = new FileInfo(keyFile);
            if (!secret.Exists || secret.LinkTarget is not null || secret.Length is < 1 or > 512 ||
                (secret.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("The dedicated private email credential file is unavailable.");
        }
        builder.Services.AddSingleton<IIdentityMailSender>(_ => new BrevoIdentityMailSender(email, keyFile));
    }
}

/// <summary>Fixed HTTPS transactional endpoint, runtime-only secret file and challenge-scoped duplicate protection.</summary>
internal sealed class BrevoIdentityMailSender : IIdentityMailSender, IDisposable
{
    private readonly string sender;
    private readonly string keyFile;
    private readonly HttpClient client;
    /// <inheritdoc />
    public string Authority => "brevo-v1:" + sender;

    /// <summary>Creates an independent client with redirects/cookies disabled and no HTTP header/body logging.</summary>
    internal BrevoIdentityMailSender(string sender, string keyFile, HttpMessageHandler? handler = null)
    {
        this.sender = sender; this.keyFile = keyFile;
        client = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
        { Timeout = TimeSpan.FromSeconds(20) };
    }

    /// <inheritdoc />
    public async Task SubmitAsync(IdentityMail mail, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (mail.Id == Guid.Empty || mail.Purpose is not ("confirm" or "reset") ||
            mail.Recipient is null || mail.Recipient.Length > 254 || !MailAddress.TryCreate(mail.Recipient, out var address) || address.Address != mail.Recipient ||
            string.IsNullOrEmpty(mail.Token) || mail.Token.Length > 16384 || mail.Format is not (1 or 2))
            throw new IdentityMailUnavailable();
        var caller = ct;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(20)); ct = deadline.Token;
        byte[]? body = null;
        try
        {
            // Only runtime code opens the owner-supplied dedicated secret. Never accept a redirected key file.
            if ((File.GetAttributes(keyFile) & FileAttributes.ReparsePoint) != 0) throw new IdentityMailUnavailable();
            await using var secret = new FileStream(keyFile, FileMode.Open, FileAccess.Read, FileShare.Read, 512, true);
            if (secret.Length is < 1 or > 512) throw new IdentityMailUnavailable();
            var bytes = new byte[checked((int)secret.Length)];
            string key;
            try
            {
                await secret.ReadExactlyAsync(bytes, ct);
                var start = bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble) ? Encoding.UTF8.Preamble.Length : 0;
                key = new UTF8Encoding(false, true).GetString(bytes, start, bytes.Length - start).TrimEnd('\r', '\n');
                if (key.Length is < 1 or > 256 || key.Any(c => c < 33 || c > 126)) throw new IdentityMailUnavailable();
            }
            finally { CryptographicOperations.ZeroMemory(bytes); }
            var content = IdentityMailText.Render(mail);
            body = JsonSerializer.SerializeToUtf8Bytes(new
            {
                sender = new { email = sender, name = "Zanance" }, subject = content.Subject,
                textContent = content.Text, htmlContent = content.Html,
                // The documented batch contract applies to one version/one recipient too. The key is never rotated
                // after an unknown transport outcome. Its 30-minute TTL exceeds the original 15-minute challenge.
                headers = new Dictionary<string, string> { ["idempotencyKey"] = mail.Id.ToString("D") },
                messageVersions = new[] { new { to = new[] { new { email = mail.Recipient } } } }
            });
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", key); request.Headers.Accept.ParseAdd("application/json");
            request.Content = new ByteArrayContent(body); request.Content.Headers.ContentType = new("application/json");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var bounded = new MemoryStream(); var buffer = new byte[1024];
            try
            {
                int length;
                while ((length = await stream.ReadAsync(buffer, ct)) != 0)
                {
                    if (bounded.Length + length > 8192) throw new IdentityMailUnavailable();
                    bounded.Write(buffer, 0, length);
                }
                using var json = JsonDocument.Parse(bounded.GetBuffer().AsMemory(0, checked((int)bounded.Length)));
                var root = json.RootElement;
                if (response.StatusCode == HttpStatusCode.Created &&
                    (root.TryGetProperty("messageId", out var id) && id.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(id.GetString()) ||
                     root.TryGetProperty("messageIds", out var ids) && ids.ValueKind == JsonValueKind.Array && ids.GetArrayLength() == 1 &&
                     ids[0].ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(ids[0].GetString()))) return;
                // Brevo's exact duplicate response acknowledges this same original batch, including recovery after
                // acceptance followed by process loss. Other 400s, timeouts and malformed receipts remain unaccepted.
                if (response.StatusCode == HttpStatusCode.BadRequest && root.TryGetProperty("code", out var code) &&
                    code.ValueKind == JsonValueKind.String && code.GetString() == "duplicate_parameter") return;
                throw new IdentityMailUnavailable();
            }
            finally { CryptographicOperations.ZeroMemory(buffer); CryptographicOperations.ZeroMemory(bounded.GetBuffer()); }
        }
        catch (OperationCanceledException) when (caller.IsCancellationRequested) { throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or HttpRequestException or
            OperationCanceledException or JsonException or DecoderFallbackException or FormatException or InvalidOperationException)
        { throw new IdentityMailUnavailable(); }
        finally { if (body is not null) CryptographicOperations.ZeroMemory(body); }
    }

    /// <inheritdoc />
    public void Dispose() => client.Dispose();
}

/// <summary>Original protected outbox decoding; no caller or provider can replace the challenge purpose/identity.</summary>
internal static class IdentityMailEnvelope
{
    /// <summary>Decodes only the original identity protector, retaining the stored challenge identifier and purpose.</summary>
    internal static IdentityMail Read(IdentityChallenge row, IDataProtectionProvider protection, string expectedAuthority)
    {
        var original = JsonSerializer.Deserialize<PrivateMail>(protection.CreateProtector("identity-mail-v1").Unprotect(row.ProtectedMail))
            ?? throw new IdentityMailUnavailable();
        // Enabling a real sender must never drain historical local qualification mail or borrow a replacement sender.
        if (original.DeliveryAuthority != expectedAuthority) throw new IdentityMailUnavailable();
        return new(row.Id, row.Purpose, original.Recipient, original.Token, original.Format, original.Language);
    }
}
