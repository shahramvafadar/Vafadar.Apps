using System.Security.Cryptography;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Presentation;

internal static partial class AttachmentFiles
{
    /// <summary>Writes a private viewer copy and asks its original owner before opening it.</summary>
    internal static async Task<bool> StageForViewerAsync(string folder, EntryAttachment attachment,
        Func<string, Task<bool>> open, Func<Task<bool>>? mayOpen)
    {
        Directory.CreateDirectory(folder);
        // Imported profiles may retain the same attachment id. Each open owns a new cache file.
        var path = Path.Combine(folder, $"{attachment.Id:N}-{Guid.NewGuid():N}{Path.GetExtension(attachment.FileName)}");
        var bytes = attachment.Data.ToArray();
        var keep = false;
        var created = false;
        try
        {
            try
            {
                await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                created = true;
                await output.WriteAsync(bytes);
            }
            finally { CryptographicOperations.ZeroMemory(bytes); }
            if (mayOpen is not null && !await mayOpen()) return false;
            // A throwing native handoff might already have dispatched the file. Keep it for
            // normal startup cleanup unless the viewer explicitly declines it.
            keep = true;
            keep = await open(path);
            return keep;
        }
        finally
        {
            if (created && !keep && File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>Uses the known platform suffix for missing extensions and for converted representations.</summary>
    internal static string NormalizePickedName(string? displayName, string? extension, bool converted = false)
    {
        var name = Path.GetFileName(displayName?.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(name) || name is "." or "..")
            name = "Attachment";

        return !string.IsNullOrEmpty(extension) && (converted || !Path.HasExtension(name))
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
    internal static Task<byte[]> ReadSourceAsync(Stream stream) =>
        BoundedPrivateFileReader.ReadAsync(stream, MaxSourceBytes, CancellationToken.None,
            static () => new InvalidDataException("The source file is too large."));

}
