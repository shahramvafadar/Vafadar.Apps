#if CAMERA_CAPTURE_REVIEW
using System.Security.Cryptography;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class CameraCaptureReview
{
    /// <summary>Qualifies only the changed original byte acquisition, without repeating OCR, cancellation or financial work.</summary>
    private static async Task RunBoundedSourceAsync(MainActivity activity)
    {
        if (AppInfo.Current.PackageName != Package || (int)Android.OS.Build.VERSION.SdkInt != 36
            || Android.OS.Build.Hardware != "ranchu" || Environment.SystemPageSize != 4096)
            throw new InvalidDataException("Only the fresh owned API36 emulator may run camera-byte qualification.");
        Emit("bounded-photo-source-open", new { NoOCRSaveSeedOrPriorCancellationReplay = true });
        var bytes = await CameraCapture.TakePhotoAsync();
        if (bytes is not { Length: > 4 } || bytes.Length > AttachmentFiles.MaxSourceBytes
            || bytes[0] != 0xFF || bytes[1] != 0xD8 || bytes[^2] != 0xFF || bytes[^1] != 0xD9
            || CameraFiles(activity) != 0 || Results.Count != 1 || Results[0] != (int)Android.App.Result.Ok)
            throw new InvalidDataException("One original external photo result, complete bounded JPEG and empty owned cache required.");
        using var options = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
        Android.Graphics.BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options);
        if (options.OutWidth <= 0 || options.OutHeight <= 0 || Platform.CurrentActivity != activity)
            throw new InvalidDataException("Original decoded image dimensions and owned Activity required.");
        Emit("bounded-photo-complete", new { SourceBytes = bytes.Length, OriginalSourceSHA256 = Convert.ToHexStringLower(SHA256.HashData(bytes)),
            Width = options.OutWidth, Height = options.OutHeight, CameraFiles = 0, ActualResultCount = Results.Count,
            EncodedLimitBytes = AttachmentFiles.MaxSourceBytes, OriginalCallbackAndFileRetirement = true,
            NoImageTextFramingFinancialOrWholeDeviceAcceptance = true, NoOCRSaveSeedOrPriorCancellationReplay = true });
    }
}
#endif
