using System.Text;
using Vafadar.Zanance.Data.Analysis;

namespace Vafadar.Zanance.App.Features.Analysis;

/// <summary>Device-private bounded analysis continuation, outside all portable preferences and financial synchronization.</summary>
internal sealed class SecureAiStorage : IAiPrivateStorage
{
    private const string Prefix = "analysis.private.v1.";
    /// <inheritdoc />
    public async Task<string?> ReadAsync(string scope, CancellationToken ct)
    {
        var marker = Marker(scope); ct.ThrowIfCancellationRequested(); string? value;
        try { value = await SecureStorage.Default.GetAsync(Prefix + scope); }
        catch (System.Security.Cryptography.CryptographicException) { throw new IOException("Original private analysis state unavailable."); }
#if ANDROID
        catch (Java.Security.GeneralSecurityException) { throw new IOException("Original private analysis state unavailable."); }
#endif
        ct.ThrowIfCancellationRequested();
        if (value is null && Attributes(marker) is not null) throw new IOException("Original private analysis state was lost; never assume safe resend.");
        if (value is not null && Encoding.UTF8.GetByteCount(value) > 65536) throw new IOException("Original private analysis state bound exceeded.");
        return value;
    }
    /// <inheritdoc />
    public async Task WriteAsync(string scope, string value, CancellationToken ct)
    {
        var marker = Marker(scope); ct.ThrowIfCancellationRequested();
        if (Encoding.UTF8.GetByteCount(value) > 65536) throw new IOException("Private analysis state bound exceeded.");
        await SecureStorage.Default.SetAsync(Prefix + scope, value);
        if (await SecureStorage.Default.GetAsync(Prefix + scope) != value) throw new IOException("Original private analysis state publication was not confirmed.");
        if (Attributes(marker) is null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
            await using var file = new FileStream(marker, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128, FileOptions.WriteThrough);
            await file.WriteAsync("Zanance private analysis state v1"u8.ToArray(), ct); file.Flush(flushToDisk: true);
        }
        ct.ThrowIfCancellationRequested();
    }
    private static string Marker(string scope)
    {
        if (scope.Length != 64 || !scope.All(char.IsAsciiHexDigit)) throw new ArgumentException("Original private analysis scope required.");
        var root = Path.Combine(FileSystem.AppDataDirectory, "analysis-state");
        if (Attributes(root) is { } directory && ((directory & FileAttributes.Directory) == 0 || (directory & FileAttributes.ReparsePoint) != 0))
            throw new IOException("Private analysis directory refused.");
        var path = Path.Combine(root, scope + ".initialized");
        if (Attributes(path) is { } marker && (marker & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new IOException("Private analysis marker refused.");
        return path;
    }

    /// <summary>Distinguishes an absent path from unavailable private-state metadata before any backing mutation.</summary>
    private static FileAttributes? Attributes(string path)
    {
        // D-1003: Exists also returns false on denied access. That is not evidence of a fresh scope.
        try { return File.GetAttributes(path); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }
}
