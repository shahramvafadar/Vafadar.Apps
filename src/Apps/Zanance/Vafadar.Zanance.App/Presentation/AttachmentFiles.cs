using Vafadar.Documents.Maui;
using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>A picked file that is attached to an entry once the entry is saved (a receipt read on Home, D-37).</summary>
internal sealed record PendingAttachment(string FileName, string ContentType, byte[] Data);

/// <summary>A metadata-free stored copy, with a transient original only while recognition is in progress.</summary>
internal sealed record PreparedAttachment(string Name, string ContentType, byte[] Data, byte[]? RecognitionData = null);

/// <summary>
/// Picks and prepares attachments (F2-TX-04). Photos are stored as metadata-free JPEGs of at most 1600 px. Recognition
/// uses a separate bounded, upright copy of the transient original (D-64), never included in PendingAttachment.
/// </summary>
internal static class AttachmentFiles
{
    private const float MaxEdge = 1600;

    /// <summary>The encoded source-file limit, before preparing the much smaller stored photo.</summary>
    internal const int MaxSourceBytes = 64 * 1024 * 1024;

    public static async Task<PreparedAttachment?> PickAsync(string title, bool forRecognition = false)
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = title,
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.Android] = ["image/*", "application/pdf"],
                [DevicePlatform.iOS] = ["public.image", "com.adobe.pdf"],
                [DevicePlatform.WinUI] = [".jpg", ".jpeg", ".png", ".heic", ".pdf"],
            }),
        });
        if (file is null)
        {
            return null;
        }

        await using var stream = await file.OpenReadAsync();
        return await PrepareAsync(file.FileName, string.IsNullOrEmpty(file.ContentType) ? Guess(file.FileName) : file.ContentType,
            await ReadSourceAsync(stream), forRecognition);
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

    /// <summary>Prepares a camera photo for storage, retaining the transient original only for recognition.</summary>
    public static Task<PreparedAttachment> FromCameraAsync(string name, byte[] data) =>
        PrepareAsync(name + ".jpg", "image/jpeg", data, forRecognition: true);

    private static async Task<PreparedAttachment> PrepareAsync(string name, string contentType, byte[] data, bool forRecognition)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var smaller = await DocumentImages.PrepareAsync(data, (int)MaxEdge, 0.8f);
                return new(Path.ChangeExtension(name, ".jpg"), "image/jpeg", smaller, forRecognition ? data : null);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // Never retain a camera original with location metadata as the stored-copy fallback.
                throw new InvalidDataException("The photo could not be re-encoded.", ex);
            }
        }

        return new(name, contentType, data);
    }

    private static string CacheFolder => Path.Combine(FileSystem.CacheDirectory, "attachments");

    /// <summary>Removes original temporary viewer/export copies and abandoned camera originals on startup and after data deletion.</summary>
    public static void ClearCache() => AttachmentCache.Clear(FileSystem.CacheDirectory);

    /// <summary>Writes the attachment to the cache and opens it with the app the device chooses.</summary>
    /// <returns><see langword="false"/> when no app could open it.</returns>
    public static async Task<bool> OpenAsync(EntryAttachment attachment, Func<Task<bool>>? mayOpen = null)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        var folder = CacheFolder;
        Directory.CreateDirectory(folder);
        // Imported profiles may retain the same attachment id. Each explicit open owns a new cache file,
        // rather than overwriting a pending viewer's copy from another profile or revision.
        var fileName = attachment.FileName; var contentType = attachment.ContentType;
        var path = Path.Combine(folder, $"{attachment.Id:N}-{Guid.NewGuid():N}{Path.GetExtension(fileName)}");
        var bytes = attachment.Data.ToArray(); var dispatched = false; var created = false;
        try
        {
            await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                created = true;
                await output.WriteAsync(bytes);
            }
            if (mayOpen is not null && !await mayOpen()) return false;
            dispatched = true;
            return await Launcher.Default.OpenAsync(new OpenFileRequest(fileName, new ReadOnlyFile(path, contentType)));
        }
        finally
        {
            // Only this newly created, never-dispatched file is removed. A possibly accepted native
            // request keeps the existing startup cache policy; no URI-grant revocation is implied.
            if (created && !dispatched && File.Exists(path)) File.Delete(path);
        }
    }

    private static string Guess(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".heic" => "image/heic",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream",
    };

}
