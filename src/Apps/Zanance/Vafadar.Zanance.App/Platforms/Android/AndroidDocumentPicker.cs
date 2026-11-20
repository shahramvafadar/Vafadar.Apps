using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Provider;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

/// <summary>Reads one document selected by the Android system picker, using its transient read grant.</summary>
internal static class AndroidDocumentPicker
{
    // Keep this range below the camera and household document ranges.
    private const int FirstCode = 0x2000, LastCode = 0x3FFF;
    private static int lastCode = FirstCode - 1;
    private static Pending? pending;
    private static readonly SemaphoreSlim PickerOwner = new(1, 1);

    private sealed record Pending(Activity Activity, int Code, TaskCompletionSource<Android.Net.Uri?> Completion,
        CancellationTokenSource Lifetime);

    /// <summary>Uses the selected document within its read grant and closes its stream before releasing the picker.</summary>
    internal static async Task UseAsync(string title, string[]? mimeTypes, Func<Stream, string, string?, CancellationToken, Task> use,
        Action? validateOwner = null, string[]? virtualMimeTypes = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!PickerOwner.Wait(0))
            throw new InvalidOperationException("An attachment picker is already open.");

        Pending? own = null;
        try
        {
            var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("No activity to open the document picker.");
            if (lastCode >= LastCode)
                throw new InvalidOperationException("The attachment request identity range is exhausted.");

            own = new(activity, ++lastCode, new(TaskCreationOptions.RunContinuationsAsynchronously),
                CancellationTokenSource.CreateLinkedTokenSource(ct));
            var lifetime = own.Lifetime.Token;
            Volatile.Write(ref pending, own);
            using var intent = new Intent(Intent.ActionOpenDocument);
            intent.SetType("*/*");
            if (mimeTypes is not null)
                intent.PutExtra(Intent.ExtraMimeTypes, mimeTypes);
            using var chooser = Intent.CreateChooser(intent, title);
            activity.StartActivityForResult(chooser, own.Code);
            using var uri = await own.Completion.Task;
            if (uri is null || activity.IsDestroyed)
                return;

            // MAUI's generic picker copies to a physical path first. Its copy can query Position
            // on a provider pipe and it runs before the app can apply its byte limit.
            validateOwner?.Invoke();
            var document = await Task.Run(() => Open(activity, uri, virtualMimeTypes ?? mimeTypes, lifetime), lifetime);
            using var asset = document.Asset;
            using var input = document.Input;
            lifetime.ThrowIfCancellationRequested();
            validateOwner?.Invoke();
            lifetime.ThrowIfCancellationRequested();
            await use(input, document.Name, document.ContentType, lifetime);
            lifetime.ThrowIfCancellationRequested();
        }
        catch (Android.OS.OperationCanceledException error)
        {
            if (own?.Lifetime.IsCancellationRequested == true)
                throw new OperationCanceledException("The selected document request retired.", error, own.Lifetime.Token);
            throw new IOException("The provider cancelled the selected document request.", error);
        }
        catch (Java.IO.IOException error)
        {
            throw new IOException("The selected document could not be read.", error);
        }
        catch (Java.Lang.SecurityException error)
        {
            throw new UnauthorizedAccessException("The selected document is unavailable.", error);
        }
        finally
        {
            if (own is not null)
            {
                Interlocked.CompareExchange(ref pending, null, own);
                own.Lifetime.Dispose();
            }
            PickerOwner.Release();
        }
    }

    private static (string Name, string? ContentType, Stream Input, AssetFileDescriptor Asset) Open(
        Activity activity, Android.Net.Uri uri, string[]? virtualMimeTypes, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
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
        string[] projection = DocumentsContract.IsDocumentUri(activity, uri)
            ? [IOpenableColumns.DisplayName, DocumentsContract.Document.ColumnFlags]
            : [IOpenableColumns.DisplayName];
        using var signal = new Android.OS.CancellationSignal();
        using var cancellation = ct.Register(signal.Cancel);
        using (var cursor = resolver.Query(uri, projection, null, null, null, signal))
        {
            ct.ThrowIfCancellationRequested();
            if (cursor?.MoveToFirst() == true)
            {
                var nameColumn = cursor.GetColumnIndex(IOpenableColumns.DisplayName);
                if (nameColumn >= 0)
                    name = cursor.GetString(nameColumn);
                var flagsColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnFlags);
                virtualFile = flagsColumn >= 0 && (cursor.GetLong(flagsColumn) & (long)DocumentContractFlags.VirtualDocument) != 0;
            }
        }

        ct.ThrowIfCancellationRequested();
        var contentType = resolver.GetType(uri);
        ct.ThrowIfCancellationRequested();
        AssetFileDescriptor? asset;
        if (virtualFile)
        {
            var filters = virtualMimeTypes ?? ["*/*"];
            contentType = filters.SelectMany(filter =>
            {
                ct.ThrowIfCancellationRequested();
                return resolver.GetStreamTypes(uri, filter) ?? [];
            }).FirstOrDefault()
                ?? throw new IOException("The selected virtual document offers no supported format.");
            ct.ThrowIfCancellationRequested();
            asset = resolver.OpenTypedAssetFileDescriptor(uri, contentType, null, signal);
        }
        else
        {
            // Android's OpenInputStream uses this descriptor for an admitted content URI.
            asset = resolver.OpenAssetFileDescriptor(uri, "r", signal);
        }

        if (asset is null) throw new IOException("The selected document is unavailable.");
        try
        {
            ct.ThrowIfCancellationRequested();
            return (GetName(name, contentType, converted: virtualFile), contentType,
                asset.CreateInputStream() ?? throw new IOException("The selected document is unavailable."), asset);
        }
        catch
        {
            using (asset) asset.Close();
            throw;
        }
    }

    private static string GetName(string? displayName, string? contentType, bool converted = false)
    {
        var extension = Android.Webkit.MimeTypeMap.Singleton?.GetExtensionFromMimeType(contentType);
        return AttachmentFiles.NormalizePickedName(displayName, extension, converted);
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
        {
            own.Completion.TrySetResult(null);
            own.Lifetime.Cancel();
        }
    }
}
