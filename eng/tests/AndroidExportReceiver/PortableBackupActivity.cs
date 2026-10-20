using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;

namespace Vafadar.Zanance.Verification.ExportReceiver;

/// <summary>Receives only explicitly shared fictitious portable backups from the isolated restore application.</summary>
[Activity(Name = "pro.vafadar.zanance.exportreview.PortableBackupActivity", Label = "Zanance portable review",
    Exported = true, NoHistory = true, ExcludeFromRecents = true)]
[IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "application/octet-stream")]
public sealed class PortableBackupActivity : Activity
{
    private const int Limit = 8 * 1024 * 1024;

    /// <summary>Copies one bounded native read grant without opening the source application's private storage.</summary>
    protected override async void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var root = FilesDir?.AbsolutePath ?? throw new IOException("Owned receiver storage is unavailable.");
        try
        {
            var intent = Intent ?? throw new InvalidOperationException();
            if (intent.Action != Intent.ActionSend || intent.Type != "application/octet-stream"
                || (intent.Flags & ActivityFlags.GrantReadUriPermission) == 0) throw new InvalidOperationException();
            var uri = intent.ClipData?.ItemCount == 1 ? intent.ClipData.GetItemAt(0)?.Uri
                : intent.GetParcelableExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(Android.Net.Uri))) as Android.Net.Uri;
            if (uri?.Scheme != "content" || uri.Authority != "pro.vafadar.zanance.portablerestorereview.fileProvider")
                throw new InvalidOperationException();
            var resolver = ContentResolver ?? throw new InvalidOperationException();
            using var metadata = resolver.Query(uri, [IOpenableColumns.DisplayName], null, null, null)
                ?? throw new InvalidOperationException();
            if (!metadata.MoveToFirst()) throw new InvalidOperationException();
            var name = metadata.GetString(0) ?? throw new InvalidOperationException();
            if (!Regex.IsMatch(name, "^pro\\.vafadar\\.zanance(?:~[a-zA-Z0-9-]+)?_[0-9]{8}T[0-9]{6}Z\\.vbak$",
                    RegexOptions.CultureInvariant)) throw new InvalidOperationException();
            var destination = Path.Combine(root, name);
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
            // This fixture accepts only unprotected synthetic exports; no password is entered or retained.
            using var archive = new ZipArchive(new MemoryStream(packet), ZipArchiveMode.Read);
            var names = archive.Entries.Select(entry => entry.FullName).ToArray();
            if (names.Length < 2 || names.Distinct(StringComparer.Ordinal).Count() != names.Length
                || !names.Contains("manifest.json", StringComparer.Ordinal)
                || names.Any(item => item != "manifest.json" && !Regex.IsMatch(item, "^data/[a-zA-Z0-9_.-]+$", RegexOptions.CultureInvariant))
                || archive.Entries.Any(entry => entry.Length > Limit) || archive.Entries.Sum(entry => entry.Length) > Limit)
                throw new InvalidOperationException();
            await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await output.WriteAsync(packet);
            await File.WriteAllTextAsync(Path.Combine(root, "portable-status.json"), JsonSerializer.Serialize(new
            {
                Received = true, Name = name, Bytes = packet.Length, Sha256 = Convert.ToHexStringLower(SHA256.HashData(packet)),
                ReadGrant = true, Entries = names
            }));
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            await File.WriteAllTextAsync(Path.Combine(root, "portable-status.json"),
                JsonSerializer.Serialize(new { Received = false, FailureType = error.GetType().Name }));
        }
        finally { Finish(); }
    }
}
