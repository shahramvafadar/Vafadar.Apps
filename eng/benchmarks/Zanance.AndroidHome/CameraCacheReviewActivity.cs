#if CAMERA_CACHE_REVIEW
using Android.App;
using Android.OS;
using System.Text.Json;

namespace Vafadar.Zanance.App;

/// <summary>Stages only fictitious camera-cache outputs, then observes cleanup by a fresh original application startup.</summary>
[Activity(Name = "pro.vafadar.zanance.review.CameraCacheReviewActivity", Exported = true,
    Permission = Android.Manifest.Permission.Dump)]
public sealed class CameraCacheReviewActivity : Activity
{
    private const string OwnedPackage = "pro.vafadar.zanance.cameracachereview";
    private const string Tag = "ZananceCameraCache";
    private static readonly byte[] Original = [0xFF, 0xD8, 0x45, 0x58, 0x49, 0x46, 0xFF, 0xD9];
    private static readonly byte[] Unrelated = [7, 3, 9];

    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        try
        {
            var run = Intent?.GetStringExtra("camera_cache_run");
            var phase = Intent?.GetStringExtra("camera_cache_phase");
            if (PackageName != OwnedPackage || savedInstanceState is not null
                || !Guid.TryParseExact(run, "N", out _) || phase is not ("stage" or "verify"))
                throw new InvalidDataException("Explicit fresh camera-cache review identity required.");
            var root = CacheDir?.AbsolutePath ?? throw new IOException("Owned cache unavailable.");
            var journal = Path.Combine(FilesDir!.AbsolutePath, "camera-cache-run.json");
            var camera = Path.Combine(root, Presentation.AttachmentCache.CameraDirectoryName);
            var raw = Path.Combine(camera, run + ".jpg");
            var other = Path.Combine(camera, "unrelated.txt");
            var outside = Path.Combine(root, run + ".jpg");
            if (phase == "stage")
            {
                if (File.Exists(journal) || Directory.Exists(camera))
                    throw new InvalidDataException("Camera staging was already used.");
                Directory.CreateDirectory(camera);
                File.WriteAllBytes(raw, Original); File.WriteAllBytes(other, Unrelated);
                File.WriteAllBytes(outside, Unrelated);
                File.WriteAllText(journal, JsonSerializer.Serialize(new { Run = run, StagePid = Android.OS.Process.MyPid() }));
                Android.Util.Log.Info(Tag, JsonSerializer.Serialize(new { Run = run, Phase = phase,
                    Pid = Android.OS.Process.MyPid(), RawExists = File.Exists(raw), RawBytes = new FileInfo(raw).Length,
                    OriginalFileMatch = File.ReadAllBytes(raw).SequenceEqual(Original) }));
            }
            else
            {
                using var original = JsonDocument.Parse(File.ReadAllText(journal));
                if (original.RootElement.GetProperty("Run").GetString() != run
                    || original.RootElement.GetProperty("StagePid").GetInt32() == Android.OS.Process.MyPid())
                    throw new InvalidDataException("Same original fixture and a genuinely new process required.");
                Android.Util.Log.Info(Tag, JsonSerializer.Serialize(new { Run = run, Phase = phase,
                    Pid = Android.OS.Process.MyPid(), RawAbsent = !File.Exists(raw),
                    UnrelatedMatch = File.ReadAllBytes(other).SequenceEqual(Unrelated),
                    OutsideMatch = File.ReadAllBytes(outside).SequenceEqual(Unrelated) }));
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // No native exception text or private path is copied into the observation stream.
            Android.Util.Log.Error(Tag, JsonSerializer.Serialize(new { Refused = true, ExceptionType = exception.GetType().Name }));
        }
        finally { Finish(); }
    }
}
#endif
