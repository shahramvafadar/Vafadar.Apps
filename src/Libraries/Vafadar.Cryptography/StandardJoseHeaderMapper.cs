using System.Text;
using System.Text.Json;
using Jose;

namespace Vafadar.Cryptography;

/// <summary>Bounded reflection-free JOSE header JSON only; no cryptographic algorithm or payload serialization is replaced.</summary>
internal sealed class StandardJoseHeaderMapper : IJsonMapper
{
    /// <inheritdoc />
    public string Serialize(object obj)
    {
        if (obj is not IDictionary<string, object> values || values.Count > 5) throw new JsonException("Unsupported household header.");
        using var bytes = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bytes))
        {
            writer.WriteStartObject();
            foreach (var (name, value) in values)
            {
                if (name is not ("alg" or "enc" or "typ" or "cty" or "kid") || value is not string text || text.Length > 256)
                    throw new JsonException("Unsupported household header.");
                writer.WriteString(name, text);
            }
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(bytes.GetBuffer(), 0, checked((int)bytes.Length));
    }

    /// <inheritdoc />
    public T Parse<T>(string json)
    {
        if (json.Length > 2048 || !typeof(T).IsAssignableFrom(typeof(Dictionary<string, object>))) throw new JsonException("Unsupported household header.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 2 });
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException("Unsupported household header.");
        var values = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var item in document.RootElement.EnumerateObject())
        {
            if (values.Count == 5 || item.Name is not ("alg" or "enc" or "typ" or "cty" or "kid") ||
                item.Value.ValueKind != JsonValueKind.String || item.Value.GetString() is not { Length: <= 256 } text ||
                !values.TryAdd(item.Name, text)) throw new JsonException("Unsupported household header.");
        }
        return (T)(object)values;
    }
}
