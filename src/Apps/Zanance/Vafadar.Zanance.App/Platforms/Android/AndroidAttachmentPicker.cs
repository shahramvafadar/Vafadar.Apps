using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

/// <summary>Reads one document selected by the Android system picker, using its transient read grant.</summary>
internal static class AndroidAttachmentPicker
{
    // Keep this range below the camera and household document ranges.
    private const int FirstCode = 0x2000, LastCode = 0x3FFF;
    private static int lastCode = FirstCode - 1;
    private static Pending? pending;
    private static readonly SemaphoreSlim PickerOwner = new(1, 1);

    private sealed record Pending(Activity Activity, int Code, TaskCompletionSource<Android.Net.Uri?> Completion);

    /// <summary>Returns the selected name, reported MIME type and bounded bytes, or null on cancellation.</summary>
    internal static async Task<(string Name, string? ContentType, byte[] Bytes)?> PickAsync(string title)
    {
        if (!PickerOwner.Wait(0))
            throw new InvalidOperationException("An attachment picker is already open.");

        Pending? own = null;
        try
        {
            var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("No activity to open the document picker.");
            if (lastCode >= LastCode)
                throw new InvalidOperationException("The attachment request identity range is exhausted.");

            own = new(activity, ++lastCode, new(TaskCreationOptions.RunContinuationsAsynchronously));
            Volatile.Write(ref pending, own);
            using var intent = new Intent(Intent.ActionOpenDocument);
            intent.SetType("*/*");
            intent.PutExtra(Intent.ExtraMimeTypes, new[] { "image/*", "application/pdf" });
            using var chooser = Intent.CreateChooser(intent, title);
            activity.StartActivityForResult(chooser, own.Code);
            using var uri = await own.Completion.Task;
            if (uri is null || activity.IsDestroyed)
                return null;

            // MAUI's generic picker copies to a physical path first. Its copy can query Position
            // on a provider pipe and it runs before the app can apply its byte limit.
            return await Task.Run(() => ReadAsync(activity, uri));
        }
        finally
        {
            if (own is not null)
                Interlocked.CompareExchange(ref pending, null, own);
            PickerOwner.Release();
        }
    }

    private static async Task<(string Name, string? ContentType, byte[] Bytes)> ReadAsync(Activity activity, Android.Net.Uri uri)
    {
        var resolver = activity.ContentResolver ?? throw new IOException("The selected document is unavailable.");
        var authority = uri.Authority;
        var provider = string.IsNullOrEmpty(authority) ? null : activity.PackageManager?.ResolveContentProvider(authority, 0);
        if (uri.Scheme != ContentResolver.SchemeContent || string.IsNullOrEmpty(authority) ||
            provider?.ApplicationInfo?.Uid == Android.OS.Process.MyUid() ||
            activity.CheckUriPermission(uri, Android.OS.Process.MyPid(), Android.OS.Process.MyUid(),
                ActivityFlags.GrantReadUriPermission) != Permission.Granted)
            throw new UnauthorizedAccessException("The selected document has no external read grant.");

        string? name = null;
        bool virtualFile = false;
        using (var cursor = resolver.Query(uri, [IOpenableColumns.DisplayName, DocumentsContract.Document.ColumnFlags], null, null, null))
        {
            if (cursor?.MoveToFirst() == true)
            {
                var nameColumn = cursor.GetColumnIndex(IOpenableColumns.DisplayName);
                if (nameColumn >= 0)
                    name = cursor.GetString(nameColumn);
                var flagsColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnFlags);
                virtualFile = flagsColumn >= 0 && (cursor.GetLong(flagsColumn) & (long)DocumentContractFlags.VirtualDocument) != 0;
            }
        }

        var contentType = resolver.GetType(uri);
        if (virtualFile)
        {
            contentType = resolver.GetStreamTypes(uri, "application/pdf")?.FirstOrDefault()
                ?? resolver.GetStreamTypes(uri, "image/*")?.FirstOrDefault()
                ?? throw new IOException("The selected virtual document offers no supported format.");
            using var asset = resolver.OpenTypedAssetFileDescriptor(uri, contentType, null)
                ?? throw new IOException("The selected document is unavailable.");
            using var stream = asset.CreateInputStream() ?? throw new IOException("The selected document is unavailable.");
            return (GetName(name, contentType), contentType, await AttachmentFiles.ReadSourceAsync(stream));
        }

        using var input = resolver.OpenInputStream(uri) ?? throw new IOException("The selected document is unavailable.");
        return (GetName(name, contentType), contentType, await AttachmentFiles.ReadSourceAsync(input));
    }

    private static string GetName(string? displayName, string? contentType)
    {
        var name = Path.GetFileName(displayName?.Replace('\\', '/'));
        if (!string.IsNullOrWhiteSpace(name) && name is not "." and not "..")
            return name;

        var extension = Android.Webkit.MimeTypeMap.Singleton?.GetExtensionFromMimeType(contentType);
        return string.IsNullOrEmpty(extension) ? "Attachment" : $"Attachment.{extension}";
    }

    /// <summary>Completes the exact native request, swallowing any late callback from a retired owner.</summary>
    internal static bool OnActivityResult(Activity activity, int code, Result result, Intent? data)
    {
        if (code < FirstCode || code > Volatile.Read(ref lastCode))
            return false;

        if (Volatile.Read(ref pending) is { } own && own.Code == code && ReferenceEquals(own.Activity, activity))
            own.Completion.TrySetResult(result == Result.Ok ? data?.Data : null);
        return true;
    }

    /// <summary>Ends the destroyed Activity's pending document selection.</summary>
    internal static void OnDestroyed(Activity activity)
    {
        if (Volatile.Read(ref pending) is { } own && ReferenceEquals(own.Activity, activity))
            own.Completion.TrySetResult(null);
    }
}
