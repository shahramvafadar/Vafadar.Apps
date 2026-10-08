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

    // Files written for sharing: CSV exports and PDF reports (ImportExportViewModel, ReportsViewModel).
    private static readonly string[] SharedFilePatterns = ["zanance-export-*.csv", "zanance-holdings-*.csv", "zanance-report-*.pdf"];

    /// <summary>
    /// Removes the copies written for opening attachments and the files written for sharing (exports and reports). They
    /// are plain, unencrypted files, so they are deleted on every start and after "Delete all data" instead of staying in
    /// the cache until the system clears it.
    /// </summary>
    public static void ClearCache()
    {
        try
        {
            if (Directory.Exists(CacheFolder))
            {
                Directory.Delete(CacheFolder, recursive: true);
            }

            foreach (var file in SharedFilePatterns.SelectMany(pattern => Directory.EnumerateFiles(FileSystem.CacheDirectory, pattern)))
            {
                File.Delete(file);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A file still open in another app is removed on a later start.
        }
    }

    /// <summary>Writes the attachment to the cache and opens it with the app the device chooses.</summary>
    /// <returns><see langword="false"/> when no app could open it.</returns>
    public static async Task<bool> OpenAsync(EntryAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        var folder = CacheFolder;
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{attachment.Id:N}{Path.GetExtension(attachment.FileName)}");
        await File.WriteAllBytesAsync(path, attachment.Data);
        return await Launcher.Default.OpenAsync(new OpenFileRequest(attachment.FileName, new ReadOnlyFile(path, attachment.ContentType)));
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
