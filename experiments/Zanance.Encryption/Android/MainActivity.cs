using Android.App;
using Android.OS;
using Android.Widget;
using Zanance.Encryption.Probe;

namespace Zanance.Encryption.Android;

/// <summary>Runs only fictitious data in this harness package's private files directory.</summary>
[Activity(Name = "pro.vafadar.zanance.encryptionproof.MainActivity", MainLauncher = true, Exported = true)]
public sealed class MainActivity : Activity
{
    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var label = new TextView(this) { Text = "Running isolated encryption proof..." };
        SetContentView(label);
        _ = Task.Run(() =>
        {
            var root = Path.Combine(FilesDir!.AbsolutePath, "fictitious-proof");
            Directory.CreateDirectory(root);
            try
            {
                // A new process must never expose the preceding run's PASS report as its own result.
                foreach (var report in new[] { "result.json", "failure.txt", "key-wrapping.txt", "platform.txt" })
                    File.Delete(Path.Combine(root, report));
                var result = EncryptionProbe.Run(root);
                File.WriteAllText(Path.Combine(root, "key-wrapping.txt"), KeyWrappingProof.Run(root));
                File.WriteAllText(Path.Combine(root, "platform.txt"), $"Android API {(int)Build.VERSION.SdkInt}; ABI {Build.SupportedAbis?[0]}; Release AOT/trimmed harness");
                File.WriteAllText(Path.Combine(root, "result.json"), result.ToJson());
                RunOnUiThread(() => label.Text = "PASS - isolated proof; see result.json");
            }
            catch (Exception error)
            {
                // Never serialize an exception message that could include connection/key details.
                File.WriteAllText(Path.Combine(root, "failure.txt"), error.GetType().Name);
                RunOnUiThread(() => label.Text = "FAIL - " + error.GetType().Name);
            }
        });
    }
}
