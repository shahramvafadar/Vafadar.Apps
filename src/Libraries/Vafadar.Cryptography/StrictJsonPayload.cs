using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Vafadar.Cryptography;

/// <summary>One bounded duplicate-free source-generated parser for protected content; parsing itself never establishes signature, key or membership authority.</summary>
public static class StrictJsonPayload
{
    /// <summary>Parses only the caller-selected contract, with finite depth/size and no alternate interpretation of duplicate property names.</summary>
    public static T Deserialize<T>(ReadOnlyMemory<byte> payload, JsonTypeInfo<T> json, int maximumBytes) where T : class
    {
        // Public ciphertext manifests/events can exceed the much smaller clear-message bound.
        // Each caller still supplies its own limit; signing and decryption retain theirs.
        if (maximumBytes is < 1 or > 16 * 1024 * 1024 || payload.Length is < 1 || payload.Length > maximumBytes) throw Refusal();
        try
        {
            using (var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 16 })) RejectDuplicates(document.RootElement);
            return JsonSerializer.Deserialize(payload.Span, json) ?? throw Refusal();
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException)
        { throw Refusal(); }
    }

    /// <summary>Both public authority and nested financial objects reject duplicate names instead of depending on parser precedence.</summary>
    private static void RejectDuplicates(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            { if (!names.Add(property.Name)) throw Refusal(); RejectDuplicates(property.Value); }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var child in value.EnumerateArray()) RejectDuplicates(child);
    }
    /// <summary>No content, path, original parser exception or private material enters diagnostics.</summary>
    private static JsonException Refusal() => new("Bounded protected JSON was refused.");
}
