using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Api.Analysis;

/// <summary>Authenticates completion notices and queues bounded reads for the isolated OpenAI project.</summary>
internal sealed class AiPilotOpenAiNotices(IConfiguration configuration, IDbContextFactory<ServiceDbContext> factory,
    AiRequestStore store, TimeProvider time)
{
    private const int MaximumBodyBytes = 8192;
    private const int MaximumNotices = 128;
    private const int MaximumLookups = 3;
    private const string SecretSetting = "Analysis:ProviderPilot:openai:WebhookSecret";

    /// <summary>Maps the receiver only for an explicitly configured, isolated pilot.</summary>
    internal static void Map(WebApplication app)
    {
        if (app.Services.GetService<AiPilotOpenAiNotices>() is null ||
            string.IsNullOrWhiteSpace(app.Configuration[SecretSetting])) return;
        app.MapPost("/analysis/pilot/openai/completions",
            (HttpContext http, AiPilotOpenAiNotices inbox, CancellationToken ct) => inbox.ReceiveAsync(http, ct));
    }

    private async Task<IResult> ReceiveAsync(HttpContext http, CancellationToken ct)
    {
        var request = http.Request;
        if (!request.IsHttps || configuration[SecretSetting] is not { } secret) return Results.NotFound();
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
            if (!Verify(secret, delivery, timestamp, signature, body.Span, time.GetUtcNow()))
                return Results.BadRequest();
            try
            {
                using var document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 12 });
                var json = document.RootElement;
                if (Text(json, "type") != "response.completed") return Results.NoContent();
                if (!json.TryGetProperty("data", out var data) || Text(data, "id") is not { } response ||
                    !IsIdentifier(response) || !response.StartsWith("resp_", StringComparison.Ordinal))
                    return Results.BadRequest();
                return await QueueAsync(response, delivery!, ct) ? Results.NoContent() : Results.StatusCode(503);
            }
            catch (JsonException) { return Results.BadRequest(); }
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    private async Task<bool> QueueAsync(string response, string delivery, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Serialize the small inbox limit and retain one row for either duplicate key.
        await AiPilotProvider.LockAsync(db, ct);
        var queue = db.Set<AiPilotOpenAiNoticeRow>();
        if (await queue.AnyAsync(item => item.ResponseId == response || item.DeliveryId == delivery, ct)) return true;
        if (await queue.CountAsync(ct) >= MaximumNotices) return false;
        db.Add(new AiPilotOpenAiNoticeRow { ResponseId = response, DeliveryId = delivery, ReceivedAt = time.GetUtcNow() });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    /// <summary>Claims a limited status-only lookup for each eligible signed notice.</summary>
    internal async Task DrainAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(configuration[SecretSetting])) return;
        var now = time.GetUtcNow();
        var cutoff = now.AddMinutes(-1);
        await using var db = await factory.CreateDbContextAsync(ct);
        var due = db.Set<AiPilotOpenAiNoticeRow>().Where(item => !item.Handled && item.Attempts < MaximumLookups &&
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
            try { handled = await store.RecoverPilotOpenAiAsync(id, ct); }
            catch (Exception error) when (error is IOException or HttpRequestException or TimeoutException) { continue; }
            if (handled)
                await db.Set<AiPilotOpenAiNoticeRow>().Where(item => item.ResponseId == id)
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

    private static bool Verify(string secret, string? delivery, string? timestamp, string? signature, ReadOnlySpan<byte> body, DateTimeOffset now)
    {
        if (!IsIdentifier(delivery) || timestamp is not { Length: > 0 and <= 20 } ||
            !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
            signature is not { Length: > 0 and <= 1024 } || !secret.StartsWith("whsec_", StringComparison.Ordinal) ||
            secret.Length > 256) return false;
        var current = now.ToUnixTimeSeconds();
        if (seconds < current - 300 || seconds > current + 300) return false;
        Span<byte> key = stackalloc byte[192];
        if (!Convert.TryFromBase64Chars(secret.AsSpan(6), key, out var keyLength) || keyLength == 0)
        {
            CryptographicOperations.ZeroMemory(key);
            return false;
        }
        try
        {
            using var mac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key[..keyLength]);
            mac.AppendData(Encoding.ASCII.GetBytes($"{delivery}.{timestamp}."));
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
