using Vafadar.Localization;
using Vafadar.Zanance.App.Security;

namespace Vafadar.Zanance.App.Features.DataFiles;

/// <summary>Private bounded native ZIP cache. Successful share targets may keep their own independent copies.</summary>
internal sealed class MauiAccountantExportRuntime(AppLockService appLock, Translator translator, TimeProvider time) : IAccountantExportRuntime
{
    private string Root => Path.Combine(FileSystem.CacheDirectory, "accountant-export-v1");
    /// <inheritdoc />
    public void EnsureAvailable() { if (appLock.IsLocked) throw new InvalidOperationException("The application is covered."); }
    /// <inheritdoc />
    public Task<bool> ConfirmAsync() => appLock.ConfirmAsync(translator["Lock_ConfirmExport"]);
    /// <inheritdoc />
    public async Task<string> WriteAsync(Func<Stream, Task> write)
    {
        EnsureAvailable(); Directory.CreateDirectory(Root);
        if ((File.GetAttributes(Root) & FileAttributes.ReparsePoint) != 0) throw new IOException("The owned cache is not an ordinary directory.");
        var files = Directory.GetFiles(Root, "zanance-accountant-*.zip");
        foreach (var file in files)
            if (Owned(file) && File.GetLastWriteTimeUtc(file) < time.GetUtcNow().UtcDateTime.AddHours(-24)) Remove(file);
        if (Directory.GetFiles(Root, "zanance-accountant-*.zip").Length >= 16) throw new IOException("The private export cache is full; retry after expiry.");
        var path = Path.Combine(Root, "zanance-accountant-" + Guid.NewGuid().ToString("N") + ".zip");
        try
        {
            await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous))
            { await write(stream); await stream.FlushAsync(); }
            EnsureAvailable(); return path;
        }
        catch { Remove(path); throw; }
    }
    /// <inheritdoc />
    public Task ShareAsync(string path)
    {
        EnsureAvailable(); if (!Owned(path)) throw new IOException("The export is not an owned ordinary cache file.");
        // Android consumers can read after RequestAsync returns; expiry is applied on the next export, not here.
        return Share.Default.RequestAsync(new ShareFileRequest { Title = translator["Tax_Title"], File = new ShareFile(path, "application/zip") });
    }
    /// <inheritdoc />
    public void Remove(string path) { if (Owned(path)) File.Delete(path); }
    private bool Owned(string path)
    {
        var name = Path.GetFileName(path);
        return Path.GetFullPath(Path.GetDirectoryName(path)!) == Path.GetFullPath(Root)
            && name.StartsWith("zanance-accountant-", StringComparison.Ordinal) && name.EndsWith(".zip", StringComparison.Ordinal)
            && Guid.TryParseExact(name[19..^4], "N", out _) && File.Exists(path)
            && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0;
    }
}
