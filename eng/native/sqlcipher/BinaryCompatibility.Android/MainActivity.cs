using Android.App;
using Android.OS;
using Android.Widget;
using Vafadar.SqlCipher.BinaryCompatibility;
using Vafadar.Zanance.App.Security;

namespace Vafadar.SqlCipher.BinaryCompatibility.Android;

/// <summary>Isolated actual-assembly caller; never opens the product package or its files.</summary>
[Activity(Label = "Binary native compatibility", MainLauncher = true, Exported = true)]
public sealed class MainActivity : Activity
{
    /// <inheritdoc />
    protected override async void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState); SetContentView(new TextView(this) { Text = "Independent binary fixture running" });
        var directory = FilesDir!.AbsolutePath;
        var result = Path.Combine(directory, "result.json"); File.Delete(result); File.Delete(Path.Combine(directory, "error.txt"));
        try
        {
            var phase = Intent?.GetStringExtra("phase"); var id = Intent?.GetStringExtra("fixture");
            if (phase is not ("create" or "reopen") || !Guid.TryParseExact(id, "N", out var run) || run == Guid.Empty || id != run.ToString("N"))
                throw new IOException("Explicit independent binary fixture identity required.");
            var root = Path.Combine(directory, "binary-host", id);
            if (phase == "create") { if (Directory.Exists(root)) throw new IOException("Use a fresh binary fixture."); Directory.CreateDirectory(root); }
            await Task.Run(() => BinaryPersistenceCompatibility.RunAsync(root, phase == "create",
                folder => new AndroidGenerationKeyStore(folder), folder => new AndroidGenerationFileDurability(folder)));
            await File.WriteAllTextAsync(result, "{\"ActualPersistenceAssemblies\":true,\"Original32Migrations\":true,\"NegativeMoneyPreserved\":true,\"ActualBinaryPortableSource\":true,\"WrongKeyRefused\":true}");
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "error.txt"), error.GetType().FullName + "\n" + error.StackTrace);
            await File.WriteAllTextAsync(result, "{\"Failed\":true}");
        }
    }
}
