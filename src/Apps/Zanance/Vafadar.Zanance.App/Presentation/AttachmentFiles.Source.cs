namespace Vafadar.Zanance.App.Presentation;

internal static partial class AttachmentFiles
{
    /// <summary>Preserves an existing suffix or fills in the known platform suffix for a provider's display name.</summary>
    internal static string NormalizePickedName(string? displayName, string? extension)
    {
        var name = Path.GetFileName(displayName?.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(name) || name is "." or "..")
            name = "Attachment";

        return !Path.HasExtension(name) && !string.IsNullOrEmpty(extension)
            ? Path.ChangeExtension(name, extension)
            : name;
    }

    /// <summary>Uses the established extension mapping when the picker supplies no specific media type.</summary>
    internal static string ResolveContentType(string name, string? contentType)
    {
        const string binary = "application/octet-stream";
        if (!string.IsNullOrWhiteSpace(contentType) && !string.Equals(contentType, binary, StringComparison.OrdinalIgnoreCase))
            return contentType;

        return Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".heic" => "image/heic",
            ".pdf" => "application/pdf",
            _ => binary,
        };
    }

    /// <summary>The encoded source-file limit, before preparing the much smaller stored photo.</summary>
    internal const int MaxSourceBytes = 64 * 1024 * 1024;

    /// <summary>Reads the original owned camera output after native acceptance; it never deletes or changes that file.</summary>
    internal static async Task<byte[]> ReadCameraSourceAsync(string path)
    {
        // D-613: an external camera can grow its output after the adapter's early stat; bound actual reads too.
        await using var stream = File.OpenRead(path);
        return await ReadSourceAsync(stream);
    }

    /// <summary>Reads a picked/captured source with an encoded-byte bound, including non-seekable picker streams.</summary>
    internal static async Task<byte[]> ReadSourceAsync(Stream stream)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk)) > 0)
        {
            if (buffer.Length + read > MaxSourceBytes)
            {
                throw new InvalidDataException("The source file is too large.");
            }

            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

}
