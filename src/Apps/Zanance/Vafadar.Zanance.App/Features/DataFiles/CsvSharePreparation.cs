using System.Text;

namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>One fresh private CSV copy; offering transfers its lifetime to the existing asynchronous native receiver.</summary>
public sealed class CsvSharePreparation
{
    private readonly string directory;
    private bool offered;
    /// <summary>Gets the unique owned path while retaining the original neutral display filename.</summary>
    public string Path { get; }
    private CsvSharePreparation(string folder, string path) { directory = folder; Path = path; }
    /// <summary>Creates a BOM-preserving original CSV without overwriting an earlier offered copy, then rechecks its original owner.</summary>
    public static async Task<CsvSharePreparation> CreateAsync(string cache, string name, string content, Action validateOwner, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content); ArgumentNullException.ThrowIfNull(validateOwner);
        if (string.IsNullOrEmpty(name) || System.IO.Path.GetFileName(name) != name || !name.StartsWith("zanance-", StringComparison.Ordinal)
            || !name.EndsWith(".csv", StringComparison.Ordinal) || name.Any(value => !char.IsAsciiLetterOrDigit(value) && value is not ('-' or '_' or '.')))
            throw new ArgumentException("A neutral owned CSV filename is required.", nameof(name));
        ct.ThrowIfCancellationRequested(); validateOwner();
        Directory.CreateDirectory(cache); OrdinaryDirectory(cache);
        var root = System.IO.Path.Combine(System.IO.Path.GetFullPath(cache), "csv-export-v1");
        Directory.CreateDirectory(root); OrdinaryDirectory(root);
        var directory = System.IO.Path.Combine(root, Guid.NewGuid().ToString("N"));
        if (Directory.Exists(directory) || File.Exists(directory)) throw new IOException("The fresh CSV cache identity already exists.");
        Directory.CreateDirectory(directory); OrdinaryDirectory(directory);
        var result = new CsvSharePreparation(directory, System.IO.Path.Combine(directory, name));
        try
        {
            await using (var stream = new FileStream(result.Path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous))
            await using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), 65536, leaveOpen: true))
            { await writer.WriteAsync(content.AsMemory(), ct); await writer.FlushAsync(ct); await stream.FlushAsync(ct); }
            ct.ThrowIfCancellationRequested(); validateOwner(); return result;
        }
        catch { result.DiscardUnoffered(); throw; }
    }
    /// <summary>Marks only the immediate native handoff; its receiver may read after the sharing task returns.</summary>
    public void MarkOffered() => offered = true;
    /// <summary>Removes only this fresh unpublished file, never an earlier offered or unrelated cache copy.</summary>
    public void DiscardUnoffered()
    {
        if (offered || !Directory.Exists(directory)) return;
        OrdinaryDirectory(directory);
        if (File.Exists(Path))
        {
            if ((File.GetAttributes(Path) & FileAttributes.ReparsePoint) != 0) throw new IOException("The CSV cache file was redirected.");
            File.Delete(Path);
        }
        if (!Directory.EnumerateFileSystemEntries(directory).Any()) Directory.Delete(directory);
    }
    private static void OrdinaryDirectory(string path)
    { if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("The private CSV cache directory was redirected."); }
}
