using System.Security.Cryptography;
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
internal static partial class AttachmentFiles
{
    private const float MaxEdge = 1600;

    public static async Task<PreparedAttachment?> PickAsync(string title, bool forRecognition = false)
    {
#if ANDROID
        PreparedAttachment? prepared = null;
        await AndroidDocumentPicker.UseAsync(title, ["application/pdf", "image/*"], async (input, name, contentType) =>
        {
            var bytes = await Task.Run(() => ReadSourceAsync(input));
            prepared = await PrepareAsync(name, ResolveContentType(name, contentType), bytes, forRecognition);
        });
        return prepared;
#else
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = title,
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.iOS] = ["public.image", "com.adobe.pdf"],
                [DevicePlatform.WinUI] = [".jpg", ".jpeg", ".png", ".heic", ".pdf"],
            }),
        });
        if (file is null)
        {
            return null;
        }

        await using var stream = await file.OpenReadAsync();
        return await PrepareAsync(file.FileName, ResolveContentType(file.FileName, file.ContentType),
            await ReadSourceAsync(stream), forRecognition);
#endif
    }

    /// <summary>Prepares a camera photo for storage, retaining the transient original only for recognition.</summary>
    public static Task<PreparedAttachment> FromCameraAsync(string name, byte[] data) =>
        PrepareAsync(name + ".jpg", "image/jpeg", data, forRecognition: true);

    private static async Task<PreparedAttachment> PrepareAsync(string name, string contentType, byte[] data, bool forRecognition)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            var transferredOriginal = false;
            try
            {
                var smaller = await DocumentImages.PrepareAsync(data, (int)MaxEdge, 0.8f);
                var result = new PreparedAttachment(Path.ChangeExtension(name, ".jpg"), "image/jpeg",
                    smaller, forRecognition ? data : null);
                transferredOriginal = forRecognition;
                return result;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // Never retain a camera original with location metadata as the stored-copy fallback.
                throw new InvalidDataException("The photo could not be re-encoded.", ex);
            }
            finally
            {
                // Only a completed recognition handoff owns the original after preparation.
                if (!transferredOriginal) CryptographicOperations.ZeroMemory(data);
            }
        }

        return new(name, contentType, data);
    }

    private static string CacheFolder => Path.Combine(FileSystem.CacheDirectory, "attachments");

    /// <summary>Removes original temporary viewer/export copies and abandoned camera originals on startup and after data deletion.</summary>
    public static void ClearCache(string? resetProfileId = null)
    {
        AttachmentCache.Clear(FileSystem.CacheDirectory);
        if (resetProfileId is not null) ClearBackupShares(resetProfileId);
    }

    /// <summary>Expires temporary shares after the startup surface has been published.</summary>
    internal static void ClearExpiredShares(DateTimeOffset now)
    {
        var cache = FileSystem.CacheDirectory;
        AttachmentCache.ClearExpiredDataShares(cache, now);
#if ANDROID
        AttachmentCache.ClearExpiredBackupShares(cache, now);
        if (LegacyExternalCache is { Length: > 0 } legacyCache)
            AttachmentCache.ClearExpiredBackupShares(legacyCache, now);
#endif
    }

#if ANDROID
    // Older Android versions of Zanance let MAUI prefer its app-specific external cache.
    private static string? LegacyExternalCache => Android.App.Application.Context.ExternalCacheDir?.AbsolutePath;
#endif

    /// <summary>Removes only temporary portable-backup shares for a reset or deleted profile.</summary>
    internal static void ClearBackupShares(string profileId)
    {
#if ANDROID
        AttachmentCache.ClearBackupShares(FileSystem.CacheDirectory, profileId);
        if (LegacyExternalCache is { Length: > 0 } legacyCache)
            AttachmentCache.ClearBackupShares(legacyCache, profileId);
#endif
    }

    /// <summary>Writes the attachment to the cache and opens it with the app the device chooses.</summary>
    /// <returns><see langword="false"/> when no app could open it.</returns>
    public static Task<bool> OpenAsync(EntryAttachment attachment, Func<Task<bool>>? mayOpen = null)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        var fileName = attachment.FileName; var contentType = ResolveContentType(fileName, attachment.ContentType);
        return StageForViewerAsync(CacheFolder, attachment,
            path => Launcher.Default.OpenAsync(new OpenFileRequest(fileName, new ReadOnlyFile(path, contentType))), mayOpen);
    }

}
