using System.Text;
using System.Text.Json;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Environment = System.Environment;

namespace Vafadar.Zanance.RetainedReader;

/// <summary>Creates only an owned diagnostic window; original application startup and migration never run.</summary>
[Activity(Name = "pro.vafadar.zanance.androidhomereview.RetainedReaderActivity", MainLauncher = true,
    Exported = true, ExcludeFromRecents = true)]
public sealed class ReaderActivity : Activity
{
    /// <summary>The existing fictitious caller identity that owns the retained source and wrapping keys.</summary>
    internal const string ExpectedPackage = "pro.vafadar.zanance.androidhomereview";

    /// <inheritdoc />
    protected override async void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.AddFlags(WindowManagerFlags.Secure);
        var label = new TextView(this) { Text = "Retained encrypted read diagnosis; no financial actions." };
        SetContentView(label);
        var run = Intent?.GetStringExtra("review_run");
        var comparison = Intent?.GetStringExtra("review_pass");
        if (savedInstanceState is not null || PackageName != ExpectedPackage
            || Intent?.GetStringExtra("review_phase") != "retained-reader"
            || run != RetainedReadStages.ExpectedRun || !Guid.TryParseExact(comparison, "N", out var pass)
            || pass == Guid.Empty || !ExpectedRuntime())
        { label.Text = "Explicit original retained-source identity required; no source opened."; return; }
        var files = new Java.IO.File(FilesDir!.AbsolutePath).CanonicalPath;
        var evidence = Path.Combine(files, "retained-reader-diagnostics", comparison!);
        if (Directory.Exists(evidence))
        { label.Text = "Preserve the original diagnostic process and receipt; no operations repeated."; return; }
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "started.json"), JsonSerializer.Serialize(new
        { Run = run, Comparison = comparison, ProcessId = Environment.ProcessId }));
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        object report;
        try
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
            report = await Task.Run(() => RetainedReadStages.RunAsync(files, evidence, comparison!, deadline.Token));
            label.Text = "Original retained read diagnosis complete. Restore the original independent APK.";
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // A provider exception message can contain key-bearing connection text. Persist only type/stack.
            report = new { Failed = true, Comparison = comparison, ProcessId = Environment.ProcessId,
                ErrorType = error.GetType().FullName, Stack = error.StackTrace,
                Refusal = error is RetainedReadRefusal refusal ? refusal.Message : null };
            label.Text = "Diagnosis refused; preserve source, process and original evidence.";
        }
        var json = JsonSerializer.Serialize(report);
        await using (var output = new FileStream(Path.Combine(evidence, "result.json"), FileMode.CreateNew))
            await output.WriteAsync(Encoding.UTF8.GetBytes(json));
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        const int chunkSize = 1800;
        var count = (encoded.Length + chunkSize - 1) / chunkSize;
        for (var index = 0; index < count; index++)
            Android.Util.Log.Info("ZananceRetainedReader", $"{run}|{comparison}|{index}/{count}|"
                + encoded.Substring(index * chunkSize, Math.Min(chunkSize, encoded.Length - index * chunkSize)));
    }

    /// <summary>Compile-time target admission keeps the emulator source distinct from the retained phone source.</summary>
    private static bool ExpectedRuntime()
    {
#if RETAINED_READER_EMULATOR
        return Build.Hardware == "ranchu" && Build.SupportedAbis?.Contains("x86_64") == true;
#else
        return Build.Model == "SM-A176B";
#endif
    }
}

/// <summary>Names only fixed, non-secret refusal reasons belonging to this explicit independent reader.</summary>
internal sealed class RetainedReadRefusal(string message) : IOException(message);
