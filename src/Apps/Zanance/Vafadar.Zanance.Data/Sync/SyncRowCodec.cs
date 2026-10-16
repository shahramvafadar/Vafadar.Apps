using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Zanance.Core.Sync;

namespace Vafadar.Zanance.Data.Sync;

/// <summary>Deterministic conversion of captured native values to reviewed content-addressed wire rows.</summary>
internal static class SyncRowCodec
{
    /// <summary>Canonicalizes complete rows and optionally retains bounded immutable attachment bytes.</summary>
    internal static JsonElement? ToWire(SyncRecordKey key, string? body, Action<string, byte[]>? retainBlob = null)
    {
        if (body is null) return null;
        var table = SyncSchema.Describe(key.Table);
        using var source = JsonDocument.Parse(body); using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output))
        {
            writer.WriteStartObject();
            foreach (var column in table.Columns.OrderBy(column => column.Name, StringComparer.Ordinal))
            {
                writer.WritePropertyName(column.Name); var cell = source.RootElement.GetProperty(column.Name);
                if (column.Kind != "blob" || cell.ValueKind == JsonValueKind.Null) { cell.WriteTo(writer); continue; }
                if (!cell.TryGetProperty("$blobHex", out var hex) || hex.ValueKind != JsonValueKind.String ||
                    hex.GetString() is not { } value || value.Length > 2L * SyncGraphValidation.MaximumProfileBytes)
                    throw new InvalidDataException("The captured attachment is invalid.");
                byte[] bytes;
                try { bytes = Convert.FromHexString(value); }
                catch (FormatException) { throw new InvalidDataException("The captured attachment is invalid."); }
                try
                {
                    var hash = Convert.ToHexStringLower(SHA256.HashData(bytes)); retainBlob?.Invoke(hash, bytes);
                    writer.WriteStartObject(); writer.WriteString("sha256", hash); writer.WriteNumber("length", bytes.Length); writer.WriteEndObject();
                }
                finally { CryptographicOperations.ZeroMemory(bytes); }
            }
            writer.WriteEndObject();
        }
        using var result = JsonDocument.Parse(output.ToArray()); var row = result.RootElement.Clone();
        if (!SyncGraphValidation.ValidIdentity(table, key) || SyncGraphValidation.ValidateRow(table, key, row).Count != 0)
            throw new InvalidDataException("The captured financial row violates the personal sync contract.");
        return row;
    }

    /// <summary>Reads an already canonical received row without treating a tombstone as an empty object.</summary>
    internal static JsonElement? Parse(string? body)
    {
        if (body is null) return null;
        using var document = JsonDocument.Parse(body); return document.RootElement.Clone();
    }

    /// <summary>Splits complete proposals by their actual typed transport size, never through a row or transaction.</summary>
    internal static SyncRoundPart[] Parts(IReadOnlyList<SyncProposal> proposals, ProfileSynchronizationKind kind = ProfileSynchronizationKind.Personal)
    {
        var maximumPartBytes = kind == ProfileSynchronizationKind.Household ? Core.Sharing.HouseholdFinancialFraming.MaximumRecordBytes + 4096 : 1024 * 1024;
        var maximumRoundBytes = kind == ProfileSynchronizationKind.Household ? Core.Sharing.HouseholdFinancialFraming.MaximumStreamBytes : SyncGraphValidation.MaximumProfileBytes;
        var parts = new List<SyncRoundPart>(); var current = new List<SyncProposal>(); long bytes = 0;
        foreach (var proposal in proposals.OrderBy(row => row.Record.Table, StringComparer.Ordinal).ThenBy(row => row.Record.Key, StringComparer.Ordinal))
        {
            current.Add(proposal);
            var candidate = new SyncRoundPart(parts.Count, current);
            var size = JsonSerializer.SerializeToUtf8Bytes(candidate, PersonalSyncJsonContext.Default.SyncRoundPart).Length;
            if (size > maximumPartBytes || current.Count > 128)
            {
                current.RemoveAt(current.Count - 1);
                if (current.Count == 0) throw new InvalidDataException("A complete financial row exceeds the personal sync part bound.");
                var sealedPart = new SyncRoundPart(parts.Count, current.ToArray()); parts.Add(sealedPart);
                bytes += JsonSerializer.SerializeToUtf8Bytes(sealedPart, PersonalSyncJsonContext.Default.SyncRoundPart).Length;
                current = [proposal];
                if (JsonSerializer.SerializeToUtf8Bytes(new SyncRoundPart(parts.Count, current), PersonalSyncJsonContext.Default.SyncRoundPart).Length > maximumPartBytes)
                    throw new InvalidDataException("A complete financial row exceeds the personal sync part bound.");
            }
        }
        if (current.Count != 0)
        {
            var part = new SyncRoundPart(parts.Count, current.ToArray()); parts.Add(part);
            bytes += JsonSerializer.SerializeToUtf8Bytes(part, PersonalSyncJsonContext.Default.SyncRoundPart).Length;
        }
        if (parts.Count > 16384 || proposals.Count > SyncGraphValidation.MaximumRows || bytes > maximumRoundBytes)
            throw new InvalidDataException("The personal sync round exceeds its technical bound.");
        return parts.ToArray();
    }
}
