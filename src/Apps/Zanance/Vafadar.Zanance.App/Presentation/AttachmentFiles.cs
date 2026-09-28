using Vafadar.Zanance.Core.Ledger;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Picks and prepares attachments (F2-TX-04). The system file picker needs no storage permission; photos are always
/// re-encoded as JPEG of at most 1600 px, which keeps receipts small in the database and backups and drops metadata such
/// as the location.
/// </summary>
internal static class AttachmentFiles
{
    private const float MaxEdge = 1600;

    public static async Task<(string Name, string ContentType, byte[] Data)?> PickAsync(string title)
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
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        var data = buffer.ToArray();
        var contentType = string.IsNullOrEmpty(file.ContentType) ? Guess(file.FileName) : file.ContentType;
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            if (Shrink(data) is { } smaller)
            {
                return (Path.ChangeExtension(file.FileName, ".jpg"), "image/jpeg", smaller);
            }

#if ANDROID || IOS
            // A photo that cannot be re-encoded would keep its metadata (e.g. the location): it is not stored.
            throw new InvalidDataException("The photo could not be re-encoded.");
#endif
        }

        return (file.FileName, contentType, data);
    }

    private static string CacheFolder => Path.Combine(FileSystem.CacheDirectory, "attachments");

    /// <summary>
    /// Removes the copies written for opening attachments. They are plain files, so they are deleted on every start and
    /// after "Delete all data" instead of staying in the cache.
    /// </summary>
    public static void ClearCache()
    {
        try
        {
            if (Directory.Exists(CacheFolder))
            {
                Directory.Delete(CacheFolder, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A file still open in another app is removed on a later start.
        }
    }

    /// <summary>Writes the attachment to the cache and opens it with the app the device chooses.</summary>
    public static async Task OpenAsync(EntryAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        var folder = CacheFolder;
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{attachment.Id:N}{Path.GetExtension(attachment.FileName)}");
        await File.WriteAllBytesAsync(path, attachment.Data);
        await Launcher.Default.OpenAsync(new OpenFileRequest(attachment.FileName, new ReadOnlyFile(path, attachment.ContentType)));
    }

    private static string Guess(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".heic" => "image/heic",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream",
    };

    private static byte[]? Shrink(byte[] data)
    {
#if ANDROID || IOS
        try
        {
            using var input = new MemoryStream(data);
            var image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(input);
            var scaled = image.Width > MaxEdge || image.Height > MaxEdge ? image.Downsize(MaxEdge, disposeOriginal: true) : image;
            using var output = new MemoryStream();
            scaled.Save(output, ImageFormat.Jpeg, 0.8f);
            return output.ToArray();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return null;
        }
#else
        return null;
#endif
    }
}