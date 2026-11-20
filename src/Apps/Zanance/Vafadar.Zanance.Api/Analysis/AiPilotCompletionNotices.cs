using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Api.Analysis;

/// <summary>Authenticates completion notices and queues bounded reads for the isolated provider projects.</summary>
internal sealed class AiPilotCompletionNotices(IConfiguration configuration, IDbContextFactory<ServiceDbContext> factory,
    AiRequestStore store, TimeProvider time)
{
    private const int MaximumBodyBytes = 8192;
    private const int MaximumNotices = 128;
    /// <summary>Number of authenticated status lookups allowed per signed notice.</summary>
    internal const int MaximumLookups = 3;
    /// <summary>Configuration key for the explicitly registered provider signing secret.</summary>
    internal static string SecretSetting(string provider) => $"Analysis:ProviderPilot:{provider}:WebhookSecret";

    /// <summary>Maps the receiver only for an explicitly configured, isolated pilot.</summary>
    internal static void Map(WebApplication app)
    {
        if (app.Services.GetService<AiPilotCompletionNotices>() is null) return;
        Register("openai");
        Register("gemini");

        void Register(string provider)
        {
            if (string.IsNullOrWhiteSpace(app.Configuration[SecretSetting(provider)])) return;
            app.MapPost($"/analysis/pilot/{provider}/completions",
                (HttpContext http, AiPilotCompletionNotices inbox, CancellationToken ct) => inbox.ReceiveAsync(http, provider, ct));
        }
    }

    private async Task<IResult> ReceiveAsync(HttpContext http, string provider, CancellationToken ct)
    {
        var request = http.Request;
        if (!request.IsHttps || configuration[SecretSetting(provider)] is not { } secret) return Results.NotFound();
        if (request.ContentLength > MaximumBodyBytes) return Results.StatusCode(413);
        var bytes = new byte[MaximumBodyBytes + 1];
        try
        {
            var count = await request.Body.ReadAtLeastAsync(bytes, bytes.Length, false, ct);
            if (count > MaximumBodyBytes) return Results.StatusCode(413);
            var body = bytes.AsMemory(0, count);
            var delivery = Header(request, "webhook-id");
            var timestamp = Header(request, "webhook-timestamp");
            var signature = Header(request, "webhook-signature");
            if (!Verify(provider, secret, delivery, timestamp, signature, body.Span))
                return Results.BadRequest();
            try
            {
                using var document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 12 });
                var json = document.RootElement;
                var type = Text(json, "type");
                var supported = provider == "openai"
                    ? type is "response.completed" or "response.failed"
                    : type is "interaction.completed" or "interaction.failed";
                if (!supported) return Results.NoContent();
                if (!json.TryGetProperty("data", out var data) || Text(data, "id") is not { } response ||
                    !IsIdentifier(response) || provider == "openai" && !response.StartsWith("resp_", StringComparison.Ordinal))
                    return Results.BadRequest();
                return await QueueAsync(provider, response, delivery!, ct) ? Results.NoContent() : Results.StatusCode(503);
            }
            catch (JsonException) { return Results.BadRequest(); }
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    private async Task<bool> QueueAsync(string provider, string response, string delivery, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Serialize the small inbox limit and retain one row for either duplicate key.
        await AiPilotProvider.LockAsync(db, ct);
        var queue = db.Set<AiPilotCompletionNoticeRow>();
        if (await queue.AnyAsync(item => item.Provider == provider && (item.ResponseId == response || item.DeliveryId == delivery), ct)) return true;
        if (await queue.CountAsync(item => item.Provider == provider, ct) >= MaximumNotices) return false;
        db.Add(new AiPilotCompletionNoticeRow { Provider = provider, ResponseId = response, DeliveryId = delivery, ReceivedAt = time.GetUtcNow() });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    /// <summary>Claims a limited status-only lookup for each eligible signed notice.</summary>
    internal async Task DrainAsync(CancellationToken ct)
    {
        await DrainAsync("openai", ct);
        await DrainAsync("gemini", ct);
    }

    private async Task DrainAsync(string provider, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(configuration[SecretSetting(provider)])) return;
        var now = time.GetUtcNow();
        var cutoff = now.AddMinutes(-1);
        await using var db = await factory.CreateDbContextAsync(ct);
        var due = db.Set<AiPilotCompletionNoticeRow>().Where(item => item.Provider == provider && !item.Handled && item.Attempts < MaximumLookups &&
            (item.CheckedAt == null || item.CheckedAt <= cutoff));
        var candidates = await due.OrderBy(item => item.ReceivedAt).ThenBy(item => item.ResponseId)
            .Select(item => item.ResponseId).Take(8).ToArrayAsync(ct);
        foreach (var id in candidates)
        {
            var claimed = await due.Where(item => item.ResponseId == id).ExecuteUpdateAsync(set => set
                .SetProperty(item => item.Attempts, item => item.Attempts + 1)
                .SetProperty(item => item.CheckedAt, time.GetUtcNow()), ct);
            if (claimed != 1) continue;
            bool handled;
            try { handled = await store.RecoverPilotNoticeAsync(provider, id, ct); }
            catch (Exception error) when (error is IOException or HttpRequestException or TimeoutException) { continue; }
            if (handled)
                await db.Set<AiPilotCompletionNoticeRow>().Where(item => item.Provider == provider && item.ResponseId == id)
                    .ExecuteUpdateAsync(set => set.SetProperty(item => item.Handled, true), ct);
        }
    }

    private static string? Header(HttpRequest request, string key) => request.Headers.TryGetValue(key, out var value) &&
        value.Count == 1 ? value[0] : null;

    private static string? Text(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static bool IsIdentifier(string? value) => value is { Length: > 0 and <= 250 } &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');

    private bool Verify(string provider, string secret, string? delivery, string? timestamp, string? signature, ReadOnlySpan<byte> body)
    {
        if (!IsIdentifier(delivery) || timestamp is not { Length: > 0 and <= 20 } ||
            !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
            signature is not { Length: > 0 and <= 1024 }) return false;
        var now = time.GetUtcNow();
        var current = now.ToUnixTimeSeconds();
        if (seconds < current - 300 || seconds > current + 300) return false;
        var prefix = Encoding.ASCII.GetBytes($"{delivery}.{timestamp}.");
        if (Matches(secret, signature, prefix, body, out var currentKeyUsable)) return true;
        var section = $"Analysis:ProviderPilot:{provider}";
        return currentKeyUsable && configuration[$"{section}:PreviousWebhookSecret"] is { } previous &&
            previous != secret && ReadUtc(configuration[$"{section}:PreviousWebhookSecretFromUtc"], out var start) &&
            ReadUtc(configuration[$"{section}:PreviousWebhookSecretUntilUtc"], out var deadline) &&
            now >= start && now < deadline && deadline - start <= TimeSpan.FromHours(24) &&
            Matches(previous, signature, prefix, body, out _);

        static bool ReadUtc(string? value, out DateTimeOffset result) =>
            DateTimeOffset.TryParseExact(value, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result);
    }

    private static bool Matches(string secret, string signature, byte[] prefix, ReadOnlySpan<byte> body, out bool keyUsable)
    {
        keyUsable = false;
        if (!secret.StartsWith("whsec_", StringComparison.Ordinal) || secret.Length > 256) return false;
        Span<byte> key = stackalloc byte[192];
        if (!Convert.TryFromBase64Chars(secret.AsSpan(6), key, out var keyLength) || keyLength == 0)
        {
            CryptographicOperations.ZeroMemory(key);
            return false;
        }
        keyUsable = true;
        try
        {
            using var mac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key[..keyLength]);
            mac.AppendData(prefix);
            mac.AppendData(body);
            Span<byte> expected = stackalloc byte[32];
            mac.GetHashAndReset(expected);
            Span<byte> received = stackalloc byte[32];
            foreach (var item in signature.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (item.StartsWith("v1,", StringComparison.Ordinal) &&
                    Convert.TryFromBase64Chars(item.AsSpan(3), received, out var length) && length == expected.Length &&
                    CryptographicOperations.FixedTimeEquals(expected, received)) return true;
            return false;
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
