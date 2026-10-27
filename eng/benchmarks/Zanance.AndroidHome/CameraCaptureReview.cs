#if CAMERA_CAPTURE_REVIEW
using Android.App;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Vafadar.Zanance.App.Features.Receipts;
using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.App;

/// <summary>Observes original source choice, external camera results and preparation without financial/profile commands.</summary>
internal static class CameraCaptureReview
{
    private const string Package = "pro.vafadar.zanance.receiptcamerareview";
    private const string Tag = "ZananceCameraCapture";
    private static int _started;
    private static string? _run;
    private static string? _root;
    private static readonly List<int> Results = [];

    /// <summary>Starts a single explicitly requested fresh capture cohort after original covered startup.</summary>
    internal static void Start(MainActivity activity)
    {
        var run = activity.Intent?.GetStringExtra("review_camera_run");
        if (activity.PackageName != Package || !Guid.TryParseExact(run, "N", out _)
            || Interlocked.Exchange(ref _started, 1) != 0) return;
        _run = run;
        _root = activity.FilesDir?.AbsolutePath ?? throw new IOException("Original owned file directory required.");
        _ = RunAsync(activity);
    }

    /// <summary>Copies only results actually forwarded by the original Activity bridge; never fabricates a callback.</summary>
    internal static void RecordNativeResult(int requestCode, Result result)
    {
        if (_run is null || Platform.CurrentActivity?.PackageName != Package) return;
        Results.Add((int)result);
        Android.Util.Log.Info(Tag, JsonSerializer.Serialize(new { Run = _run, Stage = "native-result",
            Pid = Android.OS.Process.MyPid(), RequestCode = requestCode, Result = (int)result, Count = Results.Count }));
    }

    private static async Task RunAsync(MainActivity activity)
    {
        try
        {
            var journal = Path.Combine(_root!, "original-camera-cohort.json");
            if (File.Exists(journal)) throw new InvalidDataException("Original capture cohort already exists; preserve its checkpoint.");
            using (var output = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                JsonSerializer.Serialize(output, new { Run = _run, Stage = "admitted", Pid = Android.OS.Process.MyPid() });
            var app = (App)Microsoft.Maui.Controls.Application.Current!;
            await app.WaitForStartupAsync();
            if (!CameraCapture.IsAvailable || Platform.CurrentActivity != activity)
                throw new InvalidDataException("Original ready camera Activity required.");
            var runtime = IPlatformApplication.Current!.Services.GetRequiredService<IReceiptRuntime>();
            if (runtime is not MauiReceiptRuntime) throw new InvalidDataException("Original receipt runtime required.");
            if (CameraFiles(activity) != 0) throw new InvalidDataException("Fresh owned camera source required.");

            Emit("cancel-source-open", new { FinancialCommands = 0 });
            var cancelled = await runtime.PickAsync();
            if (cancelled is not null || CameraFiles(activity) != 0 || Results.Count != 1 || Results[0] != (int)Result.Canceled)
                throw new InvalidDataException("Original external camera cancellation required.");
            Emit("cancel-accepted", new { NativeResults = Results.ToArray(), CameraFiles = 0 });

            Emit("photo-source-open", new { FinancialCommands = 0 });
            var picked = await runtime.PickAsync();
            if (picked is null || picked.ContentType != "image/jpeg" || picked.Data.Length == 0
                || picked.RecognitionData is not { Length: > 0 } recognition || CameraFiles(activity) != 0
                || Results.Count != 2 || Results[1] != (int)Result.Ok)
                throw new InvalidDataException("Original external camera result and prepared source required.");
            Emit("photo-prepared", new { StoredBytes = picked.Data.Length, RecognitionBytes = recognition.Length,
                picked.ContentType, CameraFiles = 0, NativeResults = Results.ToArray(), FinancialCommands = 0 });
            var document = await runtime.ReadAsync(recognition, picked.ContentType);
            var suggestion = ReceiptParser.Parse(document.Text, DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime), document.IsAmbiguous);
            var candidates = suggestion.Candidates.ToArray();
            var originalMinor = candidates.Length == 1 && candidates[0].TryMinor("EUR", out var minor) ? minor : (long?)null;
            Emit("source-read-result", new { Status = suggestion.AmountStatus.ToString(), suggestion.Amount,
                OriginalMinor = originalMinor, document.IsAmbiguous, OriginalText = document.Text,
                ExpectedMinor = 1490, FinancialCommands = 0 });
            if (suggestion.AmountStatus != ReceiptAmountStatus.Found || originalMinor != 1490)
                throw new InvalidDataException("Preserve actual camera/OCR result; expected original total was not established.");
            Emit("complete", new { OriginalSourceCameraCancelAndPhoto = true, OriginalPreparedAndRead = true,
                ExpectedMinor = 1490, Currency = "EUR", FinancialCommands = 0, ProfileCommands = 0 });
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Emit("failed", new { ExceptionType = exception.GetType().Name,
                NativeResults = Results.ToArray(), NoAutomaticRestartOrReplay = true });
        }
    }

    private static int CameraFiles(Activity activity)
    {
        var path = Path.Combine(activity.CacheDir!.AbsolutePath, Presentation.AttachmentCache.CameraDirectoryName);
        return Directory.Exists(path) ? Directory.EnumerateFiles(path).Count() : 0;
    }

    private static void Emit(string stage, object result)
    {
        var packet = JsonSerializer.Serialize(new { Run = _run, Stage = stage, Pid = Android.OS.Process.MyPid(), Result = result });
        Android.Util.Log.Info(Tag, packet);
        File.WriteAllText(Path.Combine(_root!, "original-camera-" + stage + ".json"), packet);
    }
}
#endif
