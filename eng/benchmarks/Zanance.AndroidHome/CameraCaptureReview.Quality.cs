#if CAMERA_CAPTURE_REVIEW
using Android.App;
using BitmapFactory = Android.Graphics.BitmapFactory;
using System.Security.Cryptography;
using Vafadar.Zanance.App.Features.Receipts;

namespace Vafadar.Zanance.App;

/// <summary>Separates fixed-input OCR from emulated camera framing without repeating accepted cancellation or financial operations.</summary>
internal static partial class CameraCaptureReview
{
    private const string QualityInputHash = "4901841bb569d381d69df760e5d448446102697d377c42b860bea4080eee6bac";

    /// <summary>Reads the unchanged original fixture and one newly acquired quality frame through the original reader.</summary>
    private static async Task RunQualityAsync(Activity activity, IReceiptRuntime runtime)
    {
        await using var input = await FileSystem.OpenAppPackageFileAsync("camera-quality-control.png");
        using var memory = new MemoryStream();
        await input.CopyToAsync(memory);
        var bytes = memory.ToArray();
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != QualityInputHash)
            throw new InvalidDataException("Exact original fictitious quality input required.");
        await ReadQualityAsync(runtime, "direct-input", bytes, "image/png");
        Emit("quality-photo-source-open", new { FinancialCommands = 0, CancellationNotRepeated = true });
        var picked = await runtime.PickAsync();
        if (picked is null || picked.ContentType != "image/jpeg" || picked.Data.Length == 0
            || picked.RecognitionData is not { Length: > 0 } recognition || CameraFiles(activity) != 0
            || Results.Count != 1 || Results[0] != (int)Result.Ok)
            throw new InvalidDataException("One genuine new camera quality result required.");
        // Only the original metadata-free stored copy is exported, never the raw camera frame.
        var exportRoot = activity.GetExternalFilesDir(null)?.AbsolutePath
            ?? throw new IOException("Fresh owned external directory required.");
        var name = "camera-quality-" + _run + ".jpg";
        using (var output = new FileStream(Path.Combine(exportRoot, name), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await output.WriteAsync(picked.Data);
        Emit("quality-frame", new { ExportName = name, Stored = ImageIdentity(picked.Data),
            Recognition = ImageIdentity(recognition), NativeResults = Results.ToArray(), CameraFiles = 0,
            RawCameraExported = false, FinancialCommands = 0 });
        await ReadQualityAsync(runtime, "camera-recognition", recognition, "image/jpeg");
        await ReadQualityAsync(runtime, "camera-stored", picked.Data, "image/jpeg");
        Emit("quality-complete", new { Observations = 3, UnchangedInput = QualityInputHash,
            WholeTextAcceptanceRequiresActualComparison = true, FinancialCommands = 0, ProfileCommands = 0 });
    }

    /// <summary>Records complete actual text without substituting expected words or total-only acceptance.</summary>
    private static async Task ReadQualityAsync(IReceiptRuntime runtime, string source, byte[] bytes, string contentType)
    {
        var document = await runtime.ReadAsync(bytes, contentType);
        Emit("quality-" + source, new { Image = ImageIdentity(bytes), OriginalText = document.Text,
            document.IsAmbiguous, CompleteHeader = document.Text?.Contains("ZANANCE DEMO STORE", StringComparison.Ordinal) == true,
            CompleteFooter = document.Text?.Contains("FICTITIOUS CAMERA SAMPLE", StringComparison.Ordinal) == true,
            FinancialCommands = 0 });
    }

    /// <summary>Reads only bounds and a digest from the owned synthetic frame; it never exposes metadata or pixel memory.</summary>
    private static object ImageIdentity(byte[] bytes)
    {
        using var options = new BitmapFactory.Options { InJustDecodeBounds = true };
        using var ignored = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options);
        if (options.OutWidth <= 0 || options.OutHeight <= 0)
            throw new InvalidDataException("Actual decodable image bounds required.");
        return new { Bytes = bytes.Length, Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)),
            Width = options.OutWidth, Height = options.OutHeight };
    }
}
#endif
