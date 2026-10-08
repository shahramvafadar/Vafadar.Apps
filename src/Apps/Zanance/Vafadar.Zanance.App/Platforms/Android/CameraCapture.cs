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
    private const int RequestCode = 0x5A43;
    private static TaskCompletionSource<bool>? _result;

    /// <summary>Gets a value indicating whether the device has a camera.</summary>
    public static bool IsAvailable => Platform.AppContext.PackageManager?.HasSystemFeature(PackageManager.FeatureCameraAny) == true;

    /// <summary>Returns the photo, or <see langword="null"/> when the user went back without taking one.</summary>
    /// <exception cref="ActivityNotFoundException">No camera app is installed.</exception>
    public static async Task<byte[]?> TakePhotoAsync()
    {
        var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("No activity to start the camera from.");
        var folder = new Java.IO.File(activity.CacheDir, "camera");
        folder.Mkdirs();
        var file = new Java.IO.File(folder, $"{Guid.NewGuid():N}.jpg");
        try
        {
            var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(activity, $"{activity.PackageName}.camera", file);
            var intent = new Intent(MediaStore.ActionImageCapture);
            intent.PutExtra(MediaStore.ExtraOutput, uri);
            intent.AddFlags(ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);

            _result?.TrySetResult(false);
            _result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            activity.StartActivityForResult(intent, RequestCode);
            if (!await _result.Task || !file.Exists() || file.Length() == 0)
            {
                return null;
            }

            if (file.Length() > Presentation.AttachmentFiles.MaxSourceBytes)
            {
                throw new InvalidDataException("The camera source file is too large.");
            }

            return await File.ReadAllBytesAsync(file.AbsolutePath);
        }
        finally
        {
            file.Delete();
        }
    }

    /// <summary>Completes a pending capture; returns whether the result belonged to it.</summary>
    public static bool OnActivityResult(int requestCode, Result resultCode)
    {
        if (requestCode != RequestCode)
        {
            return false;
        }

        _result?.TrySetResult(resultCode == Result.Ok);
        return true;
    }
}

/// <summary>Shares only the camera folder of the cache with the camera app (<c>Resources/xml/camera_paths.xml</c>).</summary>
[Android.Runtime.Register("pro.vafadar.zanance.CameraFileProvider")]
internal sealed class CameraFileProvider : AndroidX.Core.Content.FileProvider
{
}
