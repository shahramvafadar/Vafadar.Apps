using Android.App;
using Android.OS;
using Android.Widget;
using Vafadar.SqlCipher.Compatibility;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Independent original-schema probe with no access to Zanance storage or credentials.</summary>
[Activity(Label = "Native schema compatibility", MainLauncher = true, Exported = true)]
public sealed class MainActivity : Activity
{
    /// <inheritdoc />
    protected override async void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var label = new TextView(this) { Text = "Independent fictitious schema check running" };
        SetContentView(label);
        foreach (var name in new[] { "result.json", "error.txt" })
            File.Delete(Path.Combine(FilesDir!.AbsolutePath, name));
        var owned = Path.Combine(FilesDir!.AbsolutePath, "native-compatibility");
        var evidence = Path.Combine(owned, "run-" + Guid.NewGuid().ToString("N"));
        try
        {
            var keyPhase = Intent?.GetStringExtra("key_phase");
            if (keyPhase is not null && keyPhase is not ("keys-create" or "keys-reopen" or "keys-negative"))
                throw new ArgumentException("Unknown independent key phase.");
            var keyRoot = owned;
            if (keyPhase is not null)
            {
                if (!Guid.TryParseExact(Intent?.GetStringExtra("key_run"), "N", out var keyRun) || keyRun == Guid.Empty)
                    throw new ArgumentException("Independent key check requires a fresh run identity.");
                keyRoot = Path.Combine(owned, "key-proofs", keyRun.ToString("N"));
            }
            var result = keyPhase is null
                ? await Task.Run(() => SchemaCompatibility.RunAsync(evidence, owned))
                : await Task.Run(() => AndroidKeyCompatibility.RunAsync(keyRoot, keyPhase));
            await File.WriteAllTextAsync(Path.Combine(FilesDir.AbsolutePath, "result.json"), result);
            label.Text = "Independent schema check passed";
        }
        catch (Exception error)
        {
            // Report type/stack without a message that might include a library's key-bearing command.
            Directory.CreateDirectory(owned);
            await File.WriteAllTextAsync(Path.Combine(FilesDir.AbsolutePath, "error.txt"), error.GetType().FullName + "\n" + error.StackTrace);
            var result = "{\"Failed\":true}";
            await File.WriteAllTextAsync(Path.Combine(FilesDir.AbsolutePath, "result.json"), result);
            label.Text = "Independent schema check failed";
        }
    }
}
