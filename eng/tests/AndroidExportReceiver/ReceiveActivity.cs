using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;

namespace Vafadar.Zanance.Verification.ExportReceiver;

/// <summary>Receives only an explicitly granted original accountant ZIP into its own isolated test storage.</summary>
[Activity(Name = "pro.vafadar.zanance.exportreview.ReceiveActivity", Label = "Zanance export review",
    Exported = true, NoHistory = true, ExcludeFromRecents = true)]
[IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "application/zip")]
public sealed class ReceiveActivity : Activity
{
    private const int Limit = 2 * 1024 * 1024;
    /// <summary>Consumes one native grant, publishes bounded local evidence and returns without forwarding data.</summary>
    protected override async void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var root = FilesDir?.AbsolutePath ?? throw new IOException("Owned receiver storage is unavailable.");
        try
        {
            var intent = Intent ?? throw new InvalidOperationException();
            if (intent.Action != Intent.ActionSend || intent.Type != "application/zip"
                || (intent.Flags & ActivityFlags.GrantReadUriPermission) == 0) throw new InvalidOperationException();
            var uri = intent.ClipData?.ItemCount == 1 ? intent.ClipData.GetItemAt(0)?.Uri
                : intent.GetParcelableExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(Android.Net.Uri))) as Android.Net.Uri;
            if (uri?.Scheme != "content" || !string.Equals(uri.Authority, "pro.vafadar.zanance.fileProvider", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException();
            var resolver = ContentResolver ?? throw new InvalidOperationException();
            using var metadata = resolver.Query(uri, [IOpenableColumns.DisplayName], null, null, null)
                ?? throw new InvalidOperationException();
            if (!metadata.MoveToFirst()) throw new InvalidOperationException();
            var name = metadata.GetString(0) ?? throw new InvalidOperationException();
            if (!Regex.IsMatch(name, "^zanance-accountant-[0-9a-f]{32}\\.zip$", RegexOptions.CultureInvariant))
                throw new InvalidOperationException();
            var token = name[19..^4];
            var destination = Path.Combine(root, "received-" + token + ".zip");
            if (File.Exists(destination)) throw new InvalidOperationException();
            using var input = resolver.OpenInputStream(uri) ?? throw new IOException();
            using var bytes = new MemoryStream();
            var buffer = new byte[16384];
            int read;
            while ((read = await input.ReadAsync(buffer)) != 0)
            {
                if (bytes.Length + read > Limit) throw new IOException();
                await bytes.WriteAsync(buffer.AsMemory(0, read));
            }
            var packet = bytes.ToArray();
            using var archive = new ZipArchive(new MemoryStream(packet), ZipArchiveMode.Read);
            string[] allowed = ["accounts.csv", "categories.csv", "transactions.csv", "accountant-review.csv",
                "recorded-totals.csv", "README.txt", "manifest.json"];
            if (archive.Entries.Count != allowed.Length || archive.Entries.Select(entry => entry.FullName).Distinct().Count() != allowed.Length
                || archive.Entries.Any(entry => !allowed.Contains(entry.FullName) || entry.Length > Limit)
                || archive.Entries.Sum(entry => entry.Length) > Limit) throw new InvalidOperationException();
            // Nothing is extracted or executed. The host checks actual CSV/manifest bytes separately.
            await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await output.WriteAsync(packet);
            var report = new { Received = true, Name = name, Bytes = packet.Length,
                Sha256 = Convert.ToHexStringLower(SHA256.HashData(packet)),
                ReadGrant = true, Entries = archive.Entries.Select(entry => entry.FullName).ToArray() };
            await File.WriteAllTextAsync(Path.Combine(root, "status.json"), JsonSerializer.Serialize(report));
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            await File.WriteAllTextAsync(Path.Combine(root, "status.json"),
                JsonSerializer.Serialize(new { Received = false, FailureType = error.GetType().Name }));
        }
        finally { Finish(); }
    }
}
