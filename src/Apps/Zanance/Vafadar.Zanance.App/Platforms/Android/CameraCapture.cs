using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Provider;

namespace Vafadar.Zanance.App;

/// <summary>
/// Takes a receipt photo with the device's camera app (D-38). The camera app writes the photo into a file of Zanance's
/// cache through <see cref="CameraFileProvider"/>; Zanance itself declares no camera or storage permission, which is the
/// way Android recommends for apps that only need a picture. The file is deleted as soon as it is read.
/// </summary>
internal static class CameraCapture
{
    // D1230: never recycle a code during this process: Android can deliver the old result to a recreated Activity.
    // This camera-only range ends before the household document-picker range and excludes sign-in codes.
    private const int FirstRequestCode = 0x4000, LastRequestCode = 0x5AFF;
    private static int _requestCode = FirstRequestCode - 1;
    private static Pending? _pending;
    private static readonly SemaphoreSlim CaptureOwner = new(1, 1);

    /// <summary>Retains the exact native owner and result identity until original output cleanup finishes.</summary>
    private sealed record Pending(Activity Owner, int Code, TaskCompletionSource<bool> Completion);

    /// <summary>Gets a value indicating whether the device has a camera.</summary>
    public static bool IsAvailable => Platform.AppContext.PackageManager?.HasSystemFeature(PackageManager.FeatureCameraAny) == true;

    /// <summary>Returns the photo, or <see langword="null"/> when the user went back without taking one.</summary>
    /// <exception cref="ActivityNotFoundException">No camera app is installed.</exception>
    /// <exception cref="InvalidOperationException">Another capture still owns the native result, or no activity is available.</exception>
    public static async Task<byte[]?> TakePhotoAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        // D-804: the native request code belongs to one operation through file cleanup.
        // A second caller must not cancel its predecessor or redirect that predecessor's result.
        if (!CaptureOwner.Wait(0))
            throw new InvalidOperationException("Another camera capture is already pending.");
        Pending? own = null;
        Java.IO.File? file = null;
        var launched = false;
        var received = false;
        try
        {
            var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("No activity to start the camera from.");
            if (_requestCode >= LastRequestCode)
                throw new InvalidOperationException("The camera request identity range is exhausted.");
            own = new Pending(activity, ++_requestCode, new(TaskCreationOptions.RunContinuationsAsynchronously));
            using var folder = new Java.IO.File(activity.CacheDir, Presentation.AttachmentCache.CameraDirectoryName);
            folder.Mkdirs();
            file = new Java.IO.File(folder, $"{Guid.NewGuid():N}.jpg");
            using var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(activity, $"{activity.PackageName}.camera", file);
            using var intent = new Intent(MediaStore.ActionImageCapture);
            intent.PutExtra(MediaStore.ExtraOutput, uri);
            intent.AddFlags(ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);

            ct.ThrowIfCancellationRequested();
            Volatile.Write(ref _pending, own);
            activity.StartActivityForResult(intent, own.Code);
            launched = true;
            var accepted = await own.Completion.Task.WaitAsync(ct);
            received = true;
            ct.ThrowIfCancellationRequested();
            if (!accepted || !file.Exists() || file.Length() == 0)
                return null;

            if (file.Length() > Presentation.AttachmentFiles.MaxSourceBytes)
                throw new InvalidDataException("The camera source file is too large.");
            return await Presentation.AttachmentFiles.ReadCameraSourceAsync(file.AbsolutePath, ct);
        }
        finally
        {
            // A retired page can stop waiting before the camera returns. The original
            // callback still owns the output path and the slot until cleanup completes.
            if (launched && !received && own is not null) _ = DrainAbandonedCaptureAsync(own, file);
            else Release(own, file);
        }
    }

    private static async Task DrainAbandonedCaptureAsync(Pending own, Java.IO.File? file)
    {
        try { await own.Completion.Task; }
        catch (OperationCanceledException) { /* The original Activity was destroyed. */ }
        finally { Release(own, file); }
    }

    private static void Release(Pending? own, Java.IO.File? file)
    {
        try
        {
            if (own is not null) Interlocked.CompareExchange(ref _pending, null, own);
            using (file) file?.Delete();
        }
        finally { CaptureOwner.Release(); }
    }

    /// <summary>Completes a pending capture; returns whether the result belonged to it.</summary>
    public static bool OnActivityResult(Activity activity, int requestCode, Result resultCode)
    {
        if (requestCode < FirstRequestCode || requestCode > Volatile.Read(ref _requestCode))
        {
            return false;
        }

        // Consume late camera results without sending them to OAuth or a replacement capture.
        if (Volatile.Read(ref _pending) is { } own && own.Code == requestCode && ReferenceEquals(own.Owner, activity))
            own.Completion.TrySetResult(resultCode == Result.Ok);
        return true;
    }

    /// <summary>Ends only the destroyed Activity's native wait, retaining the gate until its file cleanup completes.</summary>
    public static void OnDestroyed(Activity activity)
    {
        if (Volatile.Read(ref _pending) is { } own && ReferenceEquals(own.Owner, activity))
            own.Completion.TrySetCanceled();
    }
}

/// <summary>Shares only the camera folder of the cache with the camera app (<c>Resources/xml/camera_paths.xml</c>).</summary>
[Android.Runtime.Register("pro.vafadar.zanance.CameraFileProvider")]
internal sealed class CameraFileProvider : AndroidX.Core.Content.FileProvider
{
}
