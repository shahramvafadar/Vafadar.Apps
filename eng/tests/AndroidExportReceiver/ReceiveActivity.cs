using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;

namespace Vafadar.Zanance.Verification.ExportReceiver;

/// <summary>Receives explicitly granted original accountant ZIPs, report PDFs or ledger CSVs into isolated test storage.</summary>
[Activity(Name = "pro.vafadar.zanance.exportreview.ReceiveActivity", Label = "Zanance export review",
    Exported = true, NoHistory = true, ExcludeFromRecents = true)]
[IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "application/zip")]
[IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "application/pdf")]
[IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "text/csv")]
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
            var pdf = intent.Type == "application/pdf";
            var csv = intent.Type == "text/csv";
            if (intent.Action != Intent.ActionSend || (!pdf && !csv && intent.Type != "application/zip")
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
            var pattern = csv ? "^zanance-export-[0-9]{8}-[0-9]{4}\\.csv$" : pdf ? "^zanance-report-[0-9a-f]{32}\\.pdf$" : "^zanance-accountant-[0-9a-f]{32}\\.zip$";
            if (!Regex.IsMatch(name, pattern, RegexOptions.CultureInvariant))
                throw new InvalidOperationException();
            var token = csv ? name[..^4] : name[(pdf ? 15 : 19)..^4];
            var destination = Path.Combine(root, "received-" + token + (csv ? ".csv" : pdf ? ".pdf" : ".zip"));
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
            string[] entries = [];
            if (csv)
            {
                // Accept only the bounded original ledger-export header; parsing and financial checks stay on the host.
                var text = new System.Text.UTF8Encoding(false, true).GetString(packet).TrimStart('\uFEFF');
                if (!text.StartsWith("id,date,kind,amount,currency,account,", StringComparison.Ordinal)) throw new InvalidOperationException();
            }
            else if (pdf)
            {
                // Validate the owned file kind only; the host renders and inspects the original PDF pages.
                if (!packet.AsSpan().StartsWith("%PDF-"u8)) throw new InvalidOperationException();
            }
            else
            {
                using var archive = new ZipArchive(new MemoryStream(packet), ZipArchiveMode.Read);
                string[] allowed = ["accounts.csv", "categories.csv", "transactions.csv", "accountant-review.csv",
                    "recorded-totals.csv", "README.txt", "manifest.json"];
                var names = archive.Entries.Select(entry => entry.FullName).ToArray();
                var document = archive.Entries.SingleOrDefault(entry => Regex.IsMatch(entry.FullName,
                    "^documents/[0-9a-f]{32}\\.pdf$", RegexOptions.CultureInvariant));
                var expected = document is null ? allowed : [.. allowed, "documents.csv", document.FullName];
                if (names.Length != expected.Length || names.Distinct(StringComparer.Ordinal).Count() != names.Length
                    || !names.ToHashSet(StringComparer.Ordinal).SetEquals(expected)
                    || archive.Entries.Any(entry => entry.Length > Limit)
                    || archive.Entries.Sum(entry => entry.Length) > Limit) throw new InvalidOperationException();
                if (document is not null)
                {
                    // D-380: accept only the exact previously qualified fictitious PDF, never arbitrary document grants.
                    if (document.Length != 13881) throw new InvalidOperationException();
                    using var stored = document.Open(); var content = new byte[13881];
                    await stored.ReadExactlyAsync(content);
                    if (stored.ReadByte() != -1 || Convert.ToHexStringLower(SHA256.HashData(content)) !=
                        "6f6dcffa10e283897f71d7892733bb2632986810346153aa78f728b2d86a6919")
                        throw new InvalidOperationException();
                }
                entries = names;
            }
            // Nothing is extracted, executed or forwarded. The host checks original content separately.
            await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await output.WriteAsync(packet);
            var report = new { Received = true, Name = name, Bytes = packet.Length,
                Sha256 = Convert.ToHexStringLower(SHA256.HashData(packet)),
                ReadGrant = true, MediaType = intent.Type, Entries = entries };
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
